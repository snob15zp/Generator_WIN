using GeneratorAppMain.Device;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;

namespace GeneratorAppMain.ViewModel
{
    class ProgressFormViewModel : INotifyPropertyChanged
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IDeviceManager _deviceManager = UnityConfiguration.Resolve<IDeviceManager>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ISynchronizeInvoke _syncObject;

        private string _latestVersion;

        public event PropertyChangedEventHandler PropertyChanged;


        private string _deviceStatusMessage;
        public string DeviceStatusMessage
        {
            get => _deviceStatusMessage;
            private set
            {
                _deviceStatusMessage = value;
                NotifyPropertyChanged("DeviceStatusMessage");
            }
        }

        private string _deviceInfoMessage;
        public string DeviceInfoMessage
        {
            get => _deviceInfoMessage;
            private set
            {
                _deviceInfoMessage = value;
                NotifyPropertyChanged("DeviceInfoMessage");
            }
        }

        private bool _updateIsReady = false;
        public bool UpdateIsReady
        {
            get => _updateIsReady;
            private set
            {
                _updateIsReady = value;
                NotifyPropertyChanged("UpdateIsReady");
            }
        }

        private bool _inProgress = false;
        public bool InProgress
        {
            get => _inProgress;
            private set
            {
                _inProgress = value;
                NotifyPropertyChanged("InProgress");
            }
        }

        private bool _hasError = false;
        public bool HasError
        {
            get => _hasError;
            private set
            {
                _hasError = value;
                NotifyPropertyChanged("HasError");
            }
        }

        private bool _isFinished = false;
        public bool IsFinished
        {
            get => _isFinished;
            private set
            {
                _isFinished = value;
                NotifyPropertyChanged("IsFinished");
            }
        }

        private Bitmap _icon;
        public Bitmap Icon
        {
            get => _icon;
            private set
            {
                _icon = value;
                NotifyPropertyChanged("Icon");
            }
        }

        public ProgressFormViewModel(ISynchronizeInvoke syncObject)
        {
            _syncObject = syncObject;
            _deviceManager.DeviceUpdateStatusEvent += DeviceManager_DeviceUpdateStatusEvent;
        }

        private void DeviceManager_DeviceUpdateStatusEvent(object sender, DeviceUpdateStatusEventArgs args)
        {
            switch (args.Status)
            {
                case DeviceUpdateStatus.Downloading:
                    DeviceStatusMessage = "Downloading...";
                    break;
                case DeviceUpdateStatus.Updating:
                    DeviceStatusMessage = args.Progress >= 0 ? $"Device update in progress ({args.Progress}%)" : "Device update in progress...";
                    break;
                case DeviceUpdateStatus.Rebooting:
                    DeviceStatusMessage = "Wait for device rebooting...";
                    break;
                case DeviceUpdateStatus.Ready:
                    break;
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;

            var handler = PropertyChanged;
            var eventArgs = new PropertyChangedEventArgs(propertyName);
            if (_syncObject.InvokeRequired)
            {
                _syncObject.BeginInvoke(handler, new object[] { this, eventArgs });
            }
            else
            {
                handler.Invoke(this, eventArgs);
            }
        }

        public async void DownloadPrograms(string url)
        {
            SwitchToInProgressState("Downloading...");
            try
            {
                await _deviceManager.DownloadPrograms(url, _cancellationTokenSource.Token);
                SwitchToSuccessState("Programs import successfully.");
            }
            catch (OperationCanceledException e) when (e.CancellationToken == _cancellationTokenSource.Token)
            {
                Logger.Info("Download was canceled.");
                SwitchToErrorState("Operation was canceled.");
            }
            catch (DeviceException e)
            {
                Logger.Error(e, "Unable to download programs");
                SwitchToErrorState(e.Message);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unable to download programs");
                SwitchToErrorState("Something wrong happened.");
            }
        }

        public async void CheckForUpdates(bool forceUpdate)
        {
            SwitchToInProgressState("Checking for updates...");
            try
            {
                var versionInfo = await _deviceManager.CheckForUpdates(_cancellationTokenSource.Token);
                UpdateIsReady = versionInfo.IsUpdateAvailable;
                if (forceUpdate)
                {
                    _latestVersion = versionInfo.LatestVersion;
                    var currentVersion = versionInfo.CurrentVersion;
                    SwitchToFinishState($"Latest stable version {_latestVersion}, current version {currentVersion}", "Do you want to update your firmware?", null);
                    return;
                }

                if (versionInfo.IsUpdateAvailable)
                {
                    _latestVersion = versionInfo.LatestVersion.ToString();
                    var currentVersion = versionInfo.CurrentVersion.ToString();
                    SwitchToFinishState("Updates available.", $"Current version is {currentVersion}, latest version is {_latestVersion}", null);
                }
                else
                {
                    SwitchToFinishState("Latest version already installed", null, null);
                }
            }
            catch (DeviceException e)
            {
                SwitchToErrorState(e.Message);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == _cancellationTokenSource.Token)
            {
                SwitchToErrorState("Operation was canceled.");
            }
        }

        public async void DownloadFirmware()
        {
            SwitchToInProgressState("Updating...");
            try
            {
                await _deviceManager.DownloadFirmware(_latestVersion, _cancellationTokenSource.Token);
                SwitchToSuccessState("Device updated successfully.");
                DeviceInfoMessage = $"Current version is {_latestVersion}";
            }
            catch (DeviceException e)
            {
                SwitchToErrorState(e.Message);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == _cancellationTokenSource.Token)
            {
                SwitchToErrorState("Operation was canceled.");
            }
        }

        private void SwitchToInProgressState(string status)
        {
            InProgress = true;
            IsFinished = false;
            HasError = false;
            UpdateIsReady = false;
            DeviceStatusMessage = status;
            DeviceInfoMessage = null;
        }

        private void SwitchToSuccessState(string message)
        {
            HasError = false;
            IsFinished = true;
            SwitchToFinishState(message, null, Properties.Resources.StatusOK_48x);
        }

        private void SwitchToErrorState(string error)
        {
            HasError = true;
            IsFinished = true;
            SwitchToFinishState("Error", error, Properties.Resources.StatusInvalid_48x);
        }

        private void SwitchToFinishState(string status, string info, Bitmap icon)
        {
            InProgress = false;
            DeviceStatusMessage = status;
            DeviceInfoMessage = info;
            Icon = icon;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel(true);
            _deviceManager.DeviceUpdateStatusEvent -= DeviceManager_DeviceUpdateStatusEvent;
        }
    }
}
