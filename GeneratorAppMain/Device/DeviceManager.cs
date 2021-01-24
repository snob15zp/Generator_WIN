using GeneratorApiLibrary;
using GeneratorServerApi;
using GenLib;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace GeneratorAppMain.Device
{
    public interface IDeviceManager
    {
        event EventHandler<DeviceUpdateStatusEventArgs> DeviceUpdateStatusEvent;

        Task<DeviceVersionInfo> CheckForUpdates(CancellationToken cancellationToken);
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
        public DeviceUpdateStatus Status { get; }
        public int Progress { get; }

        public DeviceUpdateStatusEventArgs(DeviceUpdateStatus status, int progress = -1)
        {
            Status = status;
            Progress = progress;
        }

        public static DeviceUpdateStatusEventArgs Updating(int current = -1, int total = 1) =>
            new DeviceUpdateStatusEventArgs(DeviceUpdateStatus.Updating, (int)((current * 100.0) / total));

        public static DeviceUpdateStatusEventArgs Downloading() =>
            new DeviceUpdateStatusEventArgs(DeviceUpdateStatus.Downloading);

        public static DeviceUpdateStatusEventArgs Rebooting() =>
            new DeviceUpdateStatusEventArgs(DeviceUpdateStatus.Rebooting);
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
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly double BootloaderTimeout = TimeSpan.FromSeconds(180).TotalMilliseconds;

        private readonly IDeviceConnectionFactory _deviceConnectionFactory;
        private readonly IGeneratorApi _api;

        public event EventHandler<DeviceUpdateStatusEventArgs> DeviceUpdateStatusEvent;

        public DeviceManager(IDeviceConnectionFactory deviceFactory, IGeneratorApi api)
        {
            _deviceConnectionFactory = deviceFactory;
            _api = api;
        }

        public async Task<DeviceVersionInfo> CheckForUpdates(CancellationToken cancellationToken)
        {
            using (var device = _deviceConnectionFactory.Connect())
            {
                var latestVersion = await GetLatestVersion();
                var currentVersion = await Task.Run(() => device.Version, cancellationToken);

                return new DeviceVersionInfo
                {
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion
                };
            }
        }

        private async Task<string> GetLatestVersion()
        {
            try
            {
                var firmware = await _api.GetLatestVersion();
                return firmware?.version;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unable to get the latest fimrware version");
                throw new DeviceException(handleApiException(e));
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
                Logger.Error(e, "Unable to download firmware");
                throw new DeviceException(handleApiException(e));
            }
            catch (SystemException e)
            {
                Logger.Error(e, "Unable to update device");
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
                Logger.Error(e, "Unable to download programs");
                throw new DeviceException(handleApiException(e));
            }
            catch (SystemException e)
            {
                Logger.Error(e, "Unable to import programs");
                throw new DeviceException("Unable to import programs");
            }
        }

        private async Task ClearAllPrograms(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                using (var device = _deviceConnectionFactory.Connect())
                {
                    device.EraseByExt(".txt");
                }
            }, cancellationToken);
        }

        private async Task UpdateFirmware(string path, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
             {
                 DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating());

                 var files = Directory.GetFiles(path);
                 var cpuFile = files.FirstOrDefault(fname => fname.EndsWith(".bf"));
                 if (cpuFile != null)
                 {
                     ImportCpuFile(cpuFile, cancellationToken);
                     AwaitDeviceConnection(cancellationToken);
                 }

                 var otherFwFiles = files.Where(fname => !fname.EndsWith(".bf")).ToArray();
                 if (otherFwFiles.Length > 0)
                 {
                     ImportFiles(otherFwFiles, cancellationToken, true);
                     AwaitDeviceConnection(cancellationToken);
                 }
             }, cancellationToken);
        }

        private void ImportFiles(string[] files, CancellationToken cancellationToken, bool isRebootNeeded)
        {
            using (var device = _deviceConnectionFactory.Connect())
            {
                int total = files.Length;
                if (total > 0)
                {
                    DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating());
                }
                int current = 0;
                foreach (string file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = device.PutFile(Path.GetFileName(file), File.ReadAllBytes(file));
                    if (result != ErrorCodes.NoError)
                    {
                        throw new DeviceUpdateException(result);
                    }
                    DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating(current++, total));
                }

                if (isRebootNeeded)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Rebooting());
                    device.BootloaderReset();
                }
            }
        }

        private void ImportCpuFile(string file, CancellationToken cancellationToken)
        {
            using (var device = _deviceConnectionFactory.Connect())
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);

                XmlNode chunks = doc.DocumentElement?.SelectSingleNode("chunks");
                if (chunks == null) return;

                int total = chunks.ChildNodes.Count;
                int current = 0;
                foreach (XmlNode chunk in chunks)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!device.BootloaderUploadMcuFwChunk(Convert.FromBase64String(chunk.InnerText)))
                    {
                        throw new DeviceUpdateException();
                    }
                    DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating(current++, total));
                }

                DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Rebooting());
                device.BootloaderRunMcuFw();
            }
        }

        private void AwaitDeviceConnection(CancellationToken cancellationToken)
        {
            IDeviceConnection device = null;
            for (int i = 0; i < BootloaderTimeout / 10; i++)
            {
                Task.Delay((int)(BootloaderTimeout / 10), cancellationToken);
                try
                {
                    // Try to connect to device
                    device = _deviceConnectionFactory.Connect();
                    break;
                }
                catch (Exception)
                {
                    // ignored
                }
                finally { device?.Disconnect(); }
            }
            if (device == null)
            {
                throw new DeviceNotConnectedException();
            }
        }

        private string handleApiException(Exception e)
        {
            return e is ApiException ? e.Message : "Something wrong happened";
        }
    }
}
