using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using GeneratorApiLibrary;
using GeneratorServerApi;
using GenLib;
using log4net;

namespace GeneratorAppMain.Device
{
    public interface IDeviceManager
    {
        event EventHandler<DeviceUpdateStatusEventArgs> DeviceUpdateStatusEvent;

        Task<DeviceVersionInfo> CheckForUpdates();

        Task<string> GetLatestVersion();

        Task<string> GetDeviceVersion();

        Task AwaitDeviceConnection(int timeout);

        Task DownloadFirmware(string version);
        Task DownloadPrograms(string url);
        void Cancel();
        void Close();
    }

    public class DeviceVersionInfo
    {
        public string CurrentVersion { get; internal set; }
        public string LatestVersion { get; internal set; }
        public bool IsUpdateAvailable => new Version(CurrentVersion).CompareTo(new Version(LatestVersion)) < 0;
    }

    public class DeviceUpdateStatusEventArgs
    {
        public DeviceUpdateStatusEventArgs(DeviceUpdateStatus status, int progress = -1)
        {
            Status = status;
            Progress = progress;
        }

        public DeviceUpdateStatus Status { get; }
        public int Progress { get; }

        public static DeviceUpdateStatusEventArgs Updating(FileType fileType, long current = -1, long total = 1)
        {
            DeviceUpdateStatus status;
            switch (fileType)
            {
                case FileType.MCU:
                    status = DeviceUpdateStatus.ImportMcu;
                    break;
                case FileType.FREQUENCY:
                    status = DeviceUpdateStatus.ImportFrequancy;
                    break;
                case FileType.BATTERY_CALIBRATION:
                    status = DeviceUpdateStatus.ImportBatteryCalibration;
                    break;
                case FileType.FPGA:
                    status = DeviceUpdateStatus.ImportFpga;
                    break;
                case FileType.USB_CHARGER:
                    status = DeviceUpdateStatus.ImportUsbCharger;
                    break;
                default:
                    throw new DeviceException("Unknown file type");
            }

            return new DeviceUpdateStatusEventArgs(status, (int)(current * 100.0 / total));
        }

        public static DeviceUpdateStatusEventArgs Downloading()
        {
            return new DeviceUpdateStatusEventArgs(DeviceUpdateStatus.Downloading);
        }

        public static DeviceUpdateStatusEventArgs Rebooting()
        {
            return new DeviceUpdateStatusEventArgs(DeviceUpdateStatus.Rebooting);
        }

        public static DeviceUpdateStatusEventArgs FormatFs()
        {
            return new DeviceUpdateStatusEventArgs(DeviceUpdateStatus.FormatFs);
        }
    }

    public enum FileType
    {
        MCU,
        FPGA,
        BATTERY_CALIBRATION,
        USB_CHARGER,
        FREQUENCY
    }

    public enum DeviceUpdateStatus
    {
        Downloading,
        ImportMcu,
        ImportFpga,
        ImportBatteryCalibration,
        ImportUsbCharger,
        ImportFrequancy,
        Rebooting,
        Ready,
        FormatFs
    }

    internal class DeviceManager : IDeviceManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DeviceManager));
        private static readonly int BootloaderTimeout = (int)TimeSpan.FromSeconds(180).TotalMilliseconds;

        private readonly IGeneratorApi _api;

        private readonly IDeviceConnectionFactory _deviceConnectionFactory;
        private long _totalBytes;

        private long _totalBytesSend;
        private FileType _currentFileType;

        private IDeviceConnection _device;

        private CancellationTokenSource cancellationTokenSource;

        public DeviceManager(IDeviceConnectionFactory deviceFactory, IGeneratorApi api)
        {
            _deviceConnectionFactory = deviceFactory;
            _api = api;
        }

        public event EventHandler<DeviceUpdateStatusEventArgs> DeviceUpdateStatusEvent;

        public async Task<DeviceVersionInfo> CheckForUpdates()
        {
            using (var device = _deviceConnectionFactory.Connect())
            {
                var latestVersion = await GetLatestVersion();
                var currentVersion = await GetDeviceVersion();

                return new DeviceVersionInfo
                {
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion
                };
            }
        }

        public async Task<string> GetDeviceVersion()
        {
            return await Task.Run(() =>
            {
                using (var device = _deviceConnectionFactory.Connect())
                {
                    if (device.Version == null) throw new DeviceNotConnectedException();
                    return device.Version;
                }
            });
        }

        public async Task DownloadFirmware(string version)
        {
            cancellationTokenSource = new CancellationTokenSource();

            DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Downloading());
            var tempFileName = Path.GetTempFileName();
            try
            {
                await _api.DonwloadFirmware(version, tempFileName, cancellationTokenSource.Token);

                var tempPath = Path.GetTempPath() + Path.GetRandomFileName();
                await Task.Run(() => ZipFile.ExtractToDirectory(tempFileName, tempPath), cancellationTokenSource.Token);
                await UpdateFirmware(tempPath, version);
            }
            catch (ApiException e)
            {
                Logger.Error("Unable to download firmware", e);
                throw new DeviceException(handleApiException(e));
            }
            catch (SystemException e)
            {
                Logger.Error("Unable to update device", e);
                throw new DeviceException("Unable to extract data");
            }
            finally
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }

        public async Task DownloadPrograms(string url)
        {
            cancellationTokenSource = new CancellationTokenSource();

            var parts = url.TrimStart('/').Split('/');
            if (parts.Length < 4)
            {
                throw new DeviceException("Malformed download url");
            }
            DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Downloading());
            var tempFileName = Path.GetTempFileName();
            try
            {
                var folder = await _api.GetFolder(parts[1], parts[3], cancellationTokenSource.Token);
                await _api.DownloadPrograms(url, tempFileName, cancellationTokenSource.Token);
                await Task.Run(() =>
                {
                    var tempPath = Path.GetTempPath() + Path.GetRandomFileName();
                    ZipFile.ExtractToDirectory(tempFileName, tempPath);

                    var files = Directory.GetFiles(tempPath).Where(fname => fname.EndsWith(".txt")).ToArray();
                    ImportFiles(files, true);
                }, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
            }
            catch (ApiException e)
            {
                Logger.Error("Unable to download programs", e);
                throw new DeviceException(handleApiException(e));
            }
            catch (SystemException e)
            {
                Logger.Error("Unable to import programs", e);
                throw new DeviceException("Unable to import programs");
            }
            finally
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }

        public async Task<string> GetLatestVersion()
        {
            try
            {
                var firmware = await _api.GetLatestVersion();
                return firmware?.Version;
            }
            catch (Exception e)
            {
                Logger.Error("Unable to get the latest fimrware version", e);
                throw new DeviceException(handleApiException(e));
            }
        }

        public void Cancel()
        {
            cancellationTokenSource?.Cancel();
            if (_device != null && _device.Ready)
            {
                try
                {
                    _device.PutFileCancel();
                }
                catch (Exception)
                {
                    // Ignore
                }
            }
        }

        public void Close()
        {
            if (_device != null && _device.Ready)
            {
                try
                {
                    _device.Disconnect();
                }
                catch (Exception)
                {
                    // Ignore
                }
            }
        }

        private async Task UpdateFirmware(string path, string version)
        {
            await Task.Run(async () =>
            {
                var files = Directory.GetFiles(path);
                var cpuFile = files.FirstOrDefault(fname => fname.EndsWith(".bf"));
                if (cpuFile != null)
                {
                    FormatFlash();
                    await Task.Delay(120000);
                    await AwaitDeviceConnection(BootloaderTimeout);

                    DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating(FileType.MCU));
                    ImportCpuFile(cpuFile, version);
                    await AwaitDeviceConnection(BootloaderTimeout);
                }

                var otherFwFiles = files.Where(fname => !fname.EndsWith(".bf")).ToArray();
                if (otherFwFiles.Length > 0)
                {
                    ImportFiles(otherFwFiles);
                    await AwaitDeviceConnection(BootloaderTimeout);
                }
            });
        }

        private void FormatFlash()
        {
            using (_device = _deviceConnectionFactory.Connect())
            {
                DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.FormatFs());
                _device.FormatFS();
            }
        }

        private void UpdateVersion(string version)
        {
            using (_device = _deviceConnectionFactory.Connect())
            {
                Version ver;
                Version.TryParse(version, out ver);
                _device.BootloaderSetVersion(ver);
            }
        }

        private void ImportFiles(string[] files, bool isEncrypted = false)
        {
            using (_device = _deviceConnectionFactory.Connect())
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    throw new DeviceCanceledException();
                }

                _device.EraseByExt("txt");
                _device.EraseByExt("pls");

                var programFiles = files.Where(fname => fname.EndsWith(".txt")).Select(file => Path.GetFileName(file)).OrderBy(f => f);
                var fileList = programFiles.SelectMany(s =>
                {
                    var bytes = Encoding.UTF8.GetBytes(Path.GetFileNameWithoutExtension(LFOV.TruncateFileName(s))).ToList();
                    bytes.AddRange(Enumerable.Repeat((byte)0x20, GenG070V1.MaxFilenameSz - bytes.Count));
                    return bytes.ToArray();
                }).ToArray();

                _totalBytes = files.Sum(FileLength) + fileList.Length;
                if (_totalBytes == 0) return;

                if (fileList.Length > 0)
                {
                    DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating(FileType.FREQUENCY));
                }

                _device.OnPutFilePart += Device_OnPutFilePart;
                _totalBytesSend = 0;
                var idx = 1;
                var fileName = "";
                foreach (var file in files)
                {
                    var extension = Path.GetExtension(file);
                    switch (extension)
                    {
                        case ".rbf":
                            fileName = "FPGA";
                            _currentFileType = FileType.FPGA;
                            break;
                        case ".srec":
                            fileName = "bq28z610";
                            _currentFileType = FileType.BATTERY_CALIBRATION;
                            break;
                        case ".bin":
                            fileName = "tps65987";
                            _currentFileType = FileType.USB_CHARGER;
                            break;
                        case ".txt":
                            fileName = $"{idx}";
                            _currentFileType = FileType.FREQUENCY;
                            break;
                        default:
                            Logger.Error($"Unknown file type: {file}. Skip file");
                            continue;
                    }

                    if (_currentFileType != FileType.FREQUENCY)
                    {
                        DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating(_currentFileType));
                    }
                    PutFile($"{fileName}{extension}", File.ReadAllBytes(file), false, isEncrypted);
                    idx++;
                    _totalBytesSend += FileLength(file);
                }

                if (programFiles.Count() > 0)
                {
                    PutFile("freq.pls", fileList, false, false);
                }

                _device.TransmitDone();
                _device.OnPutFilePart -= Device_OnPutFilePart;
            }
        }

        private void PutFile(string name, IEnumerable<byte> content, bool encrypted, bool alreadyEncrypted)
        {
            var result = _device.PutFile(name, content, encrypted, alreadyEncrypted);

            if (result != ErrorCodes.NoError)
            {
                if (result == ErrorCodes.Canceled)
                {
                    throw new DeviceCanceledException();
                }
                else
                {
                    Logger.Error($"Put file {name} error: {result}");
                    throw new DeviceUpdateException(result);
                }
            }
        }

        private long FileLength(string file)
        {
            try
            {
                return new FileInfo(file).Length;
            }
            catch (Exception)
            {
                return 0L;
            }
        }

        private void ImportCpuFile(string file, string version)
        {
            using (_device = _deviceConnectionFactory.Connect())
            {
                _device.BootloaderReset();
                var doc = new XmlDocument();
                doc.Load(file);

                var chunks = doc.DocumentElement?.SelectSingleNode("chunks");
                if (chunks == null) return;

                var total = chunks.ChildNodes.Count;
                var current = 0;
                foreach (XmlNode chunk in chunks)
                {
                    if (!_device.BootloaderUploadMcuFwChunk(Convert.FromBase64String(chunk.InnerText)))
                        throw new DeviceUpdateException();
                    DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating(FileType.MCU, current++, total));
                }
                
                Version ver;
                Version.TryParse(version, out ver);
                _device.BootloaderSetVersion(ver);

                DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Rebooting());
                _device.BootloaderRunMcuFw();
            }
        }

        private void Device_OnPutFilePart(object sender, Tuple<string, int, int> args)
        {
            if (_currentFileType == FileType.FREQUENCY)
            {
                DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating(_currentFileType, _totalBytesSend + args.Item3, _totalBytes));
            }
            else
            {
                DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating(_currentFileType, args.Item3, args.Item2));
            }
        }

        public Task AwaitDeviceConnection(int timeout)
        {
            return Task.Run(async () =>
            {
                IDeviceConnection device = null;
                for (var i = 0; i < timeout / 2000; i++)
                {
                    Debug.WriteLine("Try to connect");
                    try
                    {
                        // Try to connect to device
                        device = _deviceConnectionFactory.Connect();
                        Debug.WriteLine($"Connected {device.Version}");
                        if (device.Version != null) return;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                    finally
                    {
                        device?.Disconnect();
                    }
                    await Task.Delay(2000);
                }

                Debug.WriteLine($"Device not found");
                if (device == null) throw new DeviceNotConnectedException();
            });
        }

        private string handleApiException(Exception e)
        {
            return e is ApiException ? e.Message : "Something wrong happened";
        }
    }
}