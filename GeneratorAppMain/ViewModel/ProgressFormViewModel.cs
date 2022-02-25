using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using GeneratorAppMain.Device;
using GeneratorAppMain.Properties;
using log4net;

namespace GeneratorAppMain.ViewModel
{
    internal class ProgressFormViewModel : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgressFormViewModel));
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly IDeviceManager _deviceManager = UnityConfiguration.Resolve<IDeviceManager>();
        private readonly ISynchronizeInvoke _syncObject;

        private bool _canceled;

        private string _deviceInfoMessage;

        private string _deviceStatusMessage;

        private bool _hasError;

        private Bitmap _icon;

        private bool _inProgress;

        private bool _isFinished;

        private string _latestVersion;

        private bool _updateIsReady;

        private bool _putFileInProgress;

        public string Version { get { return _latestVersion; } }

        public ProgressFormViewModel(ISynchronizeInvoke syncObject)
        {
            _syncObject = syncObject;
            _deviceManager.DeviceUpdateStatusEvent += DeviceManager_DeviceUpdateStatusEvent;
        }

        public string DeviceStatusMessage
        {
            get => _deviceStatusMessage;
            private set
            {
                _deviceStatusMessage = value;
                NotifyPropertyChanged("DeviceStatusMessage");
            }
        }

        public string DeviceInfoMessage
        {
            get => _deviceInfoMessage;
            private set
            {
                _deviceInfoMessage = value;
                NotifyPropertyChanged("DeviceInfoMessage");
            }
        }

        public bool UpdateIsReady
        {
            get => _updateIsReady;
            private set
            {
                _updateIsReady = value;
                NotifyPropertyChanged("UpdateIsReady");
            }
        }

        public bool InProgress
        {
            get => _inProgress;
            private set
            {
                _inProgress = value;
                NotifyPropertyChanged("InProgress");
            }
        }

        public bool PutFileInProgress
        {
            get => _putFileInProgress;
            private set
            {
                _putFileInProgress = value;
                NotifyPropertyChanged("PutFileInProgress");
            }
        }

        public bool HasError
        {
            get => _hasError;
            private set
            {
                _hasError = value;
                NotifyPropertyChanged("HasError");
            }
        }

        public bool IsFinished
        {
            get => _isFinished;
            private set
            {
                _isFinished = value;
                NotifyPropertyChanged("IsFinished");
            }
        }

        public Bitmap Icon
        {
            get => _icon;
            private set
            {
                _icon = value;
                NotifyPropertyChanged("Icon");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void DeviceManager_DeviceUpdateStatusEvent(object sender, DeviceUpdateStatusEventArgs args)
        {
            switch (args.Status)
            {
                case DeviceUpdateStatus.Downloading:
                    DeviceStatusMessage = "Downloading...";
                    break;
                case DeviceUpdateStatus.ImportBatteryCalibration: UpdateImportStatusMessage("Import battery calibration", args.Progress); break;
                case DeviceUpdateStatus.ImportFpga: UpdateImportStatusMessage("Import FPGA file", args.Progress); break;
                case DeviceUpdateStatus.ImportFrequancy: UpdateImportStatusMessage("Import programs", args.Progress); break;
                case DeviceUpdateStatus.ImportUsbCharger: UpdateImportStatusMessage("Import usb charger", args.Progress); break;
                case DeviceUpdateStatus.ImportMcu: UpdateImportStatusMessage("Import MCU firmware", args.Progress); break;
                case DeviceUpdateStatus.FormatFs:
                    DeviceStatusMessage = "Formating flash...";
                    break;
                case DeviceUpdateStatus.Rebooting:
                    DeviceStatusMessage = "Wait for device rebooting...";
                    break;
                case DeviceUpdateStatus.Ready:
                    break;
            }
        }

        private void UpdateImportStatusMessage(string message, int progress)
        {
            PutFileInProgress = progress >= 0;
            if (_canceled) return;

            DeviceStatusMessage = progress >= 0 ? $"{message} ({progress}%)" : $"{message}...";
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;

            var handler = PropertyChanged;
            var eventArgs = new PropertyChangedEventArgs(propertyName);
            if (_syncObject.InvokeRequired)
                _syncObject.BeginInvoke(handler, new object[] { this, eventArgs });
            else
                handler.Invoke(this, eventArgs);
        }

        public async void DownloadPrograms(string url)
        {
            _canceled = false;
            SwitchToInProgressState("Downloading...");
            try
            {
                await _deviceManager.DownloadPrograms(url);
                SwitchToSuccessState("Programs import successfully.");
            }
            catch (OperationCanceledException e)
            {
                Logger.Info("Download was canceled.");
                SwitchToErrorState("Operation was canceled.");
            }
            catch (DeviceException e)
            {
                Logger.Error("Unable to download programs", e);
                SwitchToErrorState(e.Message);
            }
            catch (Exception e)
            {
                Logger.Error("Unable to download programs", e);
                SwitchToErrorState("Something wrong happened.");
            }
            finally
            {
                _canceled = false;
            }
        }

        public async void CheckForUpdates()
        {
            SwitchToInProgressState("Checking for updates...");
            try
            {
                var versionInfo = await _deviceManager.CheckForUpdates();
                UpdateIsReady = versionInfo.IsUpdateAvailable;
                //if (forceUpdate)
                //{
                //    _latestVersion = versionInfo.LatestVersion;
                //    var currentVersion = versionInfo.CurrentVersion;
                //    SwitchToFinishState($"Latest stable version {_latestVersion}, current version {currentVersion}",
                //        "Do you want to update your firmware?", null);
                //    return;
                //}

                if (versionInfo.IsUpdateAvailable)
                {
                    _latestVersion = versionInfo.LatestVersion;
                    var currentVersion = versionInfo.CurrentVersion;
                    SwitchToFinishState("Updates available.",
                        $"Current version is {currentVersion}, latest version is {_latestVersion}", null);
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

        public async void DownloadFirmware(string version)
        {
            SwitchToInProgressState("Updating...");
            try
            {
                await _deviceManager.DownloadFirmware(version);
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

        public void Cancel()
        {
            _canceled = true;
            SwitchToInProgressState("Canceling...");
            _deviceManager.Cancel();
            SwitchToErrorState("Operation canceled");
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
            SwitchToFinishState(message, null, Resources.StatusOK_48x);
        }

        private void SwitchToErrorState(string error)
        {
            HasError = true;
            IsFinished = true;
            SwitchToFinishState("Error", error, Resources.StatusInvalid_48x);
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
            _cancellationTokenSource.Cancel();
            _deviceManager.Close();
            _deviceManager.DeviceUpdateStatusEvent -= DeviceManager_DeviceUpdateStatusEvent;
        }
    }
}