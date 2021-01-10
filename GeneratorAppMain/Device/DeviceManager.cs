using FTD2XX_NET;
using GeneratorApiLibrary;
using GeneratorServerApi;
using GenLib;
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
using Unity;

namespace GeneratorWindowsApp.Device
{
    public interface IDeviceManager
    {
        event EventHandler<DeviceUpdateStatusEventArgs> DeviceUpdateStatusEvent;

        Task<DeviceVersionInfo> CheckForUpdates(CancellationToken cancellationToken);
        Task DownloadFirmware(string version, CancellationToken cancellationToken);
    }

    public class DeviceVersionInfo
    {
        public Version currentVersion { get; internal set; }
        public Version latestVersion { get; internal set; }
        public bool isUpdateAvialable
        {
            get
            {
                return currentVersion.CompareTo(latestVersion) < 0;
            }
        }
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
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly double BOOTLOADER_TIMEOUT = TimeSpan.FromSeconds(180).TotalMilliseconds;

        private readonly IDeviceConnectionFactory deviceConnectionFactory;
        private readonly IGeneratorApi api;

        public event EventHandler<DeviceUpdateStatusEventArgs> DeviceUpdateStatusEvent;

        public DeviceManager(IDeviceConnectionFactory deviceFactory, IGeneratorApi api)
        {
            this.deviceConnectionFactory = deviceFactory;
            this.api = api;
        }

        public async Task<DeviceVersionInfo> CheckForUpdates(CancellationToken cancellationToken)
        {
            using (var device = deviceConnectionFactory.connect())
            {
                var latestVersion = await GetLatestVersion();
                var currentVersion = await Task.Run(() => device.Version);

                return new DeviceVersionInfo()
                {
                    currentVersion = new Version(currentVersion),
                    latestVersion = new Version(latestVersion)
                };
            }
        }

        private async Task<string> GetLatestVersion()
        {
            try
            {
                var firmware = await api.GetLatestVersion();
                return firmware?.version;
            }
            catch (Exception e)
            {
                logger.Error(e, "Unable to get the latest fimrware version");
                throw new DeviceException(handleApiException(e));
            }
        }

        public async Task DownloadFirmware(string version, CancellationToken cancellationToken)
        {
            DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Downloading());
            var tempFileName = Path.GetTempFileName();
            try
            {
                await api.DonwloadFirmware(version, tempFileName);
            }
            catch (Exception e)
            {
                logger.Error(e, "Unable to download firmware");
                throw new DeviceException(handleApiException(e));
            }

            try
            {
                var tempPath = Path.GetTempPath() + Path.GetRandomFileName();
                await Task.Run(() => ZipFile.ExtractToDirectory(tempFileName, tempPath));
                await UpdateFirmware(tempPath);
            }
            catch (Exception e)
            {
                logger.Error(e, "Unable to update firmware");
                throw new DeviceException("Something wrong happened");
            }
        }

        public async Task UpdateFirmware(string path)
        {
            await Task.Run(() =>
             {
                 DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating());

                 var files = Directory.GetFiles(path);
                 var cpuFile = files.FirstOrDefault(fname => fname.EndsWith(".bf"));
                 if (cpuFile != null)
                 {
                     ImportCpuFile(cpuFile);
                     awaitDeviceConnection();
                 }

                 using (var device = deviceConnectionFactory.connect())
                 {
                     var otherFwFiles = files.Where(fname => !fname.EndsWith(".bf")).ToArray();
                     int total = otherFwFiles.Length;
                     if (total > 0)
                     {
                         DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating());
                     }
                     int current = 0;
                     foreach (string file in otherFwFiles)
                     {
                         var result = device.PutFile(Path.GetFileName(file), File.ReadAllBytes(file));
                         if (result != ErrorCodes.NoError)
                         {
                             throw new DeviceUpdateException(result);
                         }
                         DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Updating(current++, total));
                     }

                     DeviceUpdateStatusEvent?.Invoke(this, DeviceUpdateStatusEventArgs.Rebooting());
                     device.BootloaderReset();
                 }
                 awaitDeviceConnection();
             });
        }

        private void ImportCpuFile(string file)
        {
            using (var device = deviceConnectionFactory.connect())
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                XmlNode chunks = doc.DocumentElement.SelectSingleNode("chunks");
                int total = chunks.ChildNodes.Count;
                int current = 0;
                foreach (XmlNode chunk in chunks)
                {
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

        private void awaitDeviceConnection()
        {
            IDeviceConnection device = null;
            for (int i = 0; i < BOOTLOADER_TIMEOUT / 10; i++)
            {
                Task.Delay((int)(BOOTLOADER_TIMEOUT / 10));
                try
                {
                    // Try to connect to device
                    device = deviceConnectionFactory.connect();
                    break;
                }
                catch (Exception) { }
                finally { device?.Disconnect(); }
            }
            if (device == null)
            {
                throw new DeviceNotConnectedException();
            }
        }

        private string handleApiException(Exception e)
        {
            if (e is ApiException)
            {
                return e.Message;
            }
            else
            {
                return "Somethig wrong happened";
            }
        }
    }
}
