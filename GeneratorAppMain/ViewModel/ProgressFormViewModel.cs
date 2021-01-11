using GeneratorWindowsApp.Device;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeneratorWindowsApp.ViewModel
{
    class ProgressFormViewModel : INotifyPropertyChanged
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IDeviceManager deviceManager = UnityConfiguration.Resolve<IDeviceManager>();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ISynchronizeInvoke syncObjec;
        
        private string latestVersion = null;

        public event PropertyChangedEventHandler PropertyChanged;


        private string _deviceStatusMessage;
        public string DeviceStatusMessage
        {
            get { return _deviceStatusMessage; }
            private set
            {
                _deviceStatusMessage = value;
                NotifyPropertyChanged("DeviceStatusMessage");
            }
        }

        private string _deviceInfoMessage;
        public string DeviceInfoMessage
        {
            get { return _deviceInfoMessage; }
            private set
            {
                _deviceInfoMessage = value;
                NotifyPropertyChanged("DeviceInfoMessage");
            }
        }

        private bool _updateIsReady = false;
        public bool UpdateIsReady
        {
            get { return _updateIsReady; }
            private set
            {
                _updateIsReady = value;
                NotifyPropertyChanged("UpdateIsReady");
            }
        }

        private bool _inProgress = false;
        public bool InProgress
        {
            get { return _inProgress; }
            private set
            {
                _inProgress = value;
                NotifyPropertyChanged("InProgress");
            }
        }

        private bool _hasError = false;
        public bool HasError
        {
            get { return _hasError; }
            private set
            {
                _hasError = value;
                NotifyPropertyChanged("HasError");
            }
        }

        private bool _isFinished = false;
        public bool IsFinished
        {
            get { return _isFinished; }
            private set
            {
                _isFinished = value;
                NotifyPropertyChanged("IsFinished");
            }
        }

        private Bitmap _icon;
        public Bitmap Icon
        {
            get { return _icon; }
            private set
            {
                _icon = value;
                NotifyPropertyChanged("Icon");
            }
        }

        public ProgressFormViewModel(ISynchronizeInvoke syncObjec)
        {
            this.syncObjec = syncObjec;
            deviceManager.DeviceUpdateStatusEvent += DeviceManager_DeviceUpdateStatusEvent;
        }

        private void DeviceManager_DeviceUpdateStatusEvent(object sender, DeviceUpdateStatusEventArgs args)
        {
            switch (args.Status)
            {
                case DeviceUpdateStatus.Downloading:
                    DeviceStatusMessage = "Downloading...";
                    break;
                case DeviceUpdateStatus.Updating:
                    if (args.Progress >= 0)
                    {
                        DeviceStatusMessage = $"Device update in progress ({args.Progress}%)";
                    }
                    else
                    {
                        DeviceStatusMessage = "Device update in progress...";
                    }
                    break;
                case DeviceUpdateStatus.Rebooting:
                    DeviceStatusMessage = "Wait for device rebooting...";
                    break;
                case DeviceUpdateStatus.Ready:
                    break;
            }
        }

        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged == null) return;

            var handler = PropertyChanged;
            var eventArgs = new PropertyChangedEventArgs(propertyName);
            if (syncObjec.InvokeRequired)
            {
                syncObjec.BeginInvoke(handler, new object[] { this, eventArgs });
            }
            else
            {
                handler.Invoke(this, eventArgs);
            }
        }

        public async void DownloadPrograms(string url)
        {
            switchToInProgressState("Downloading...");
            try
            {
                await deviceManager.DonwloadPrograms(url, cancellationTokenSource.Token);
                switchToSuccessState("Programs import successfully.");
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cancellationTokenSource.Token)
            {
                logger.Info("Download was canceled.");
                switchToErrorState("Operation was canceled.");
            }
            catch (DeviceException e)
            {
                logger.Error(e, "Unable to download programs");
                switchToErrorState(e.Message);
            }
            catch (Exception e)
            {
                logger.Error(e, "Unable to download programs");
                switchToErrorState("Somethig wrong happened.");
            }
        }

        public async void CheckForUpdates()
        {
            switchToInProgressState("Checking for updates...");
            try
            {
                var versionInfo = await deviceManager.CheckForUpdates(cancellationTokenSource.Token);
                UpdateIsReady = versionInfo.isUpdateAvialable;
                if (versionInfo.isUpdateAvialable)
                {
                    latestVersion = versionInfo.latestVersion.ToString();
                    var currentVersion = versionInfo.currentVersion.ToString();
                    switchToFinishState("Updates available.", $"Current version is {currentVersion}, latest version is {latestVersion}", null);
                }
                else
                {
                    switchToFinishState("Latest version already installed", null, null);
                }
            }
            catch (DeviceException e)
            {
                switchToErrorState(e.Message);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cancellationTokenSource.Token)
            {
                switchToErrorState("Operation was canceled.");
            }
        }

        public async void DownloadFirmware()
        {
            switchToInProgressState("Updating...");
            try
            {
                await deviceManager.DownloadFirmware(latestVersion, cancellationTokenSource.Token);
                switchToSuccessState("Device updated successfully.");
                DeviceInfoMessage = $"Current version is {latestVersion}";
            }
            catch (DeviceException e)
            {
                switchToErrorState(e.Message);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cancellationTokenSource.Token)
            {
                switchToErrorState("Operation was canceled.");
            }
        }

        private void switchToInProgressState(string status)
        {
            InProgress = true;
            IsFinished = false;
            HasError = false;
            UpdateIsReady = false;
            DeviceStatusMessage = status;
            DeviceInfoMessage = null;
        }

        private void switchToSuccessState(string message)
        {
            HasError = false;
            IsFinished = true;
            switchToFinishState(message, null, Properties.Resources.StatusOK_48x);
        }

        private void switchToErrorState(string error)
        {
            HasError = true;
            IsFinished = true;
            switchToFinishState("Error", error, Properties.Resources.StatusInvalid_48x);
        }

        private void switchToFinishState(string status, string info, Bitmap icon)
        {
            InProgress = false;
            DeviceStatusMessage = status;
            DeviceInfoMessage = info;
            Icon = icon;
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel(true);
            deviceManager.DeviceUpdateStatusEvent -= DeviceManager_DeviceUpdateStatusEvent;
        }
    }
}
