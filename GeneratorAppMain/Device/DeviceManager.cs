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
        Task<DeviceVersionInfo> CheckForUpdates(CancellationToken cancellationToken);
        Task DownloadFirmware(string version);
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

    internal class DeviceManager : IDeviceManager
    {
        private readonly IDeviceFactory deviceFactory;
        private readonly IGeneratorApi api;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public DeviceManager(IDeviceFactory deviceFactory, IGeneratorApi api)
        {
            this.deviceFactory = deviceFactory;
            this.api = api;
        }

        public async Task<DeviceVersionInfo> CheckForUpdates(CancellationToken cancellationToken)
        {
            IGenerator device = null;
            try
            {
                device = await connectIfDeviceReady();
                var latestVersion = await GetLatestVersion();
                var currentVersion = device.Version;

                return new DeviceVersionInfo()
                {
                    currentVersion = new Version(currentVersion),
                    latestVersion = new Version(latestVersion)
                };
            }
            finally
            {
                device?.Disconnect();
            }
        }

        public async Task<string> GetLatestVersion()
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

        public async Task DownloadFirmware(string version)
        {
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
                var tempPath = Path.GetTempPath() + version;
                ZipFile.ExtractToDirectory(tempFileName, tempPath);
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
            await Task.Run(async () =>
             {
                 IGenerator device = null;
                 try
                 {
                     device = await connectIfDeviceReady();
                     foreach (string file in Directory.GetFiles(path).Where(fname => fname.EndsWith("bf")))
                     {
                         XmlDocument doc = new XmlDocument();
                         doc.Load(file);

                         List<byte> Content = new List<byte>();

                         foreach (XmlNode chunk in doc.DocumentElement.SelectSingleNode("chunks"))
                         {
                             Content.AddRange(Convert.FromBase64String(chunk.InnerText));
                         }

                         var result = device.PutFile(Path.GetFileName(file), Content);
                         if (result != ErrorCodes.NoError)
                         {
                             throw new DeviceUpdateException(result);
                         }
                     }
                 }
                 finally
                 {
                     device?.Disconnect();
                 }
             });
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

        private Task<IGenerator> connectIfDeviceReady()
        {
            return Task.Run(() =>
            {
                FTDI ftdi_dev = new FTDI();
                ftdi_dev.OpenByIndex(0);
                FTDI.FT_STATUS status = ftdi_dev.GetCOMPort(out string port);
                ftdi_dev.Close();
                if (status == FTDI.FT_STATUS.FT_OK)
                {
                    return deviceFactory.create(port);
                }
                else
                {
                    throw new DeviceNotConnectedException();
                }
            });
        }
    }
}
