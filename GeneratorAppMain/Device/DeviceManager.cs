using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

        Task<DeviceVersionInfo> CheckForUpdates(CancellationToken cancellationToken);

        Task<string> GetLatestVersion();

        Task<string> GetDeviceVersion(CancellationToken cancellationToken);

        Task AwaitDeviceConnection(int timeout, CancellationToken cancellationToken);

        Task DownloadFirmware(string version, CancellationToken cancellationToken);
        Task DownloadPrograms(string url, CancellationToken cancellationToken);
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

        public static DeviceUpdateStatusEventArgs Updating(long current = -1, long total = 1)
        {
            return new DeviceUpdateStatusEventArgs(DeviceUpdateStatus.Updating, (int)(current * 100.0 / total));
        }

        public static DeviceUpdateStatusEventArgs Downloading()
        {
            return new DeviceUpdateStatusEventArgs(DeviceUpdateStatus.Downloading);
        }

        public static DeviceUpdateStatusEventArgs Rebooting()
        {
            return new DeviceUpdateStatusEventArgs(DeviceUpdateStatus.Rebooting);
        }
    }

    public enum DeviceUpdateStatus
    {
        Downloading,
        Updating,
        Rebooting,
        Ready
    }

    internal class DeviceManager : IDeviceManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DeviceManager));
        private static readonly int BootloaderTimeout = (int)TimeSpan.FromSeconds(180).TotalMilliseconds;
        private readonly IGeneratorApi _api;

        private readonly IDeviceConnectionFactory _deviceConnectionFactory;
        private long _totalBytes;

        private long _totalBytesSend;

        public DeviceManager(IDeviceConnectionFactory deviceFactory, IGeneratorApi api)
        {
            _deviceConnectionFactory = deviceFactory;
            _api = api;
        }

        public event EventHandler<DeviceUpdateStatusEventArgs> DeviceUpdateStatusEvent;

        public async Task<DeviceVersionInfo> CheckForUpdates(CancellationToken cancellationToken)
        {
            using (var device = _deviceConnectionFactory.Connect())
            {
                var latestVersion = await GetLatestVersion();
                var currentVersion = await GetDeviceVersion(cancellationToken);

                return new DeviceVersionInfo
                {
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion
                };
            }
        }

        public async Task<string> GetDeviceVersion(CancellationToken cancellationToken)
        {
            using (var device = _deviceConnectionFactory.Connect())
            {
                if (device.Version == null) throw new DeviceNotConnectedException();
                return await Task.Run(() => device.Version, cancellationToken);
            }
        }

        public async Task DownloadFirmware(string version, CancellationToken cancellationToken)
        {
            DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Downloading());
            var tempFileName = Path.GetTempFileName();
            try
            {
                await _api.DonwloadFirmware(version, tempFileName, cancellationToken);

                var tempPath = Path.GetTempPath() + Path.GetRandomFileName();
                await Task.Run(() => ZipFile.ExtractToDirectory(tempFileName, tempPath), cancellationToken);
                await UpdateFirmware(tempPath, cancellationToken);
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
        }

        public async Task DownloadPrograms(string url, CancellationToken cancellationToken)
        {
            DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Downloading());
            var tempFileName = Path.GetTempFileName();
            try
            {
                await _api.DownloadPrograms(url, tempFileName, cancellationToken);

                var tempPath = Path.GetTempPath() + Path.GetRandomFileName();
                await Task.Run(() => ZipFile.ExtractToDirectory(tempFileName, tempPath), cancellationToken);

                await ClearAllPrograms(cancellationToken);

                var files = Directory.GetFiles(tempPath).OrderByDescending(Path.GetExtension).ToArray();
                await Task.Run(() => ImportFiles(files, cancellationToken, false), cancellationToken);
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
        }

        public async Task<string> GetLatestVersion()
        {
            try
            {
                var firmware = await _api.GetLatestVersion();
                return firmware?.version;
            }
            catch (Exception e)
            {
                Logger.Error("Unable to get the latest fimrware version", e);
                throw new DeviceException(handleApiException(e));
            }
        }

        private async Task ClearAllPrograms(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                using (var device = _deviceConnectionFactory.Connect())
                {
                    device.EraseByExt("txt");
                    device.EraseByExt("pls");
                }
            }, cancellationToken);
        }

        private async Task UpdateFirmware(string path, CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating());

                var files = Directory.GetFiles(path);
                var cpuFile = files.FirstOrDefault(fname => fname.EndsWith(".bf"));
                if (cpuFile != null)
                {
                    ImportCpuFile(cpuFile, cancellationToken);
                    await AwaitDeviceConnection(BootloaderTimeout, cancellationToken);
                }

                var otherFwFiles = files.Where(fname => !fname.EndsWith(".bf")).ToArray();
                if (otherFwFiles.Length > 0)
                {
                    ImportFiles(otherFwFiles, cancellationToken, true);
                    await AwaitDeviceConnection(BootloaderTimeout, cancellationToken);
                }
            }, cancellationToken);
        }

        private void ImportFiles(string[] files, CancellationToken cancellationToken, bool isRebootNeeded)
        {
            using (var device = _deviceConnectionFactory.Connect())
            {
                _totalBytes = files.Sum(FileLength);
                if (_totalBytes == 0) return;
                DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating());

                device.OnPutFilePart += Device_OnPutFilePart;
                _totalBytesSend = 0;
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = device.PutFile(Path.GetFileName(file), File.ReadAllBytes(file), false);
                    if (result != ErrorCodes.NoError) throw new DeviceUpdateException(result);

                    _totalBytesSend += FileLength(file);
                }

                device.OnPutFilePart -= Device_OnPutFilePart;

                if (isRebootNeeded)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Rebooting());
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

        private void ImportCpuFile(string file, CancellationToken cancellationToken)
        {
            using (var device = _deviceConnectionFactory.Connect())
            {
                device.BootloaderReset();
                var doc = new XmlDocument();
                doc.Load(file);

                var chunks = doc.DocumentElement?.SelectSingleNode("chunks");
                if (chunks == null) return;

                var total = chunks.ChildNodes.Count;
                var current = 0;
                foreach (XmlNode chunk in chunks)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!device.BootloaderUploadMcuFwChunk(Convert.FromBase64String(chunk.InnerText)))
                        throw new DeviceUpdateException();
                    DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating(current++, total));
                }

                DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Rebooting());
                device.BootloaderRunMcuFw();
            }
        }

        private void Device_OnPutFilePart(object sender, Tuple<string, int, int> args)
        {
            DeviceUpdateStatusEvent?.Invoke(this,
                DeviceUpdateStatusEventArgs.Updating(_totalBytesSend + args.Item3, _totalBytes));
        }

        public Task AwaitDeviceConnection(int timeout, CancellationToken cancellationToken)
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
                    await Task.Delay(2000, cancellationToken);
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