using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using GeneratorAppMain.Device;
using GeneratorAppMain.Utils;
using log4net;

namespace GeneratorAppMain.ViewModel
{
    internal class StatusFormViewModel : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProgressFormViewModel));
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly IDeviceManager _deviceManager = UnityConfiguration.Resolve<IDeviceManager>();
        private readonly IUsbDeviceDetector _usbDeviceDetector = UnityConfiguration.Resolve<IUsbDeviceDetector>();
        private readonly ISynchronizeInvoke _syncObject;

        private string _deviceVersionMessage;

        private string _deviceStatusMessage;
        private bool _isDeviceConnected;

        private bool _hasError;

        private bool _isPorgress;

        private string _latestVersionMessage;

        private bool _isUpdateRequired;

        public string DeviceStatusMessage
        {
            get => _deviceStatusMessage;
            private set
            {
                _deviceStatusMessage = value;
                NotifyPropertyChanged("DeviceStatusMessage");
            }
        }

        public bool IsDeviceConnected
        {
            get => _isDeviceConnected;
            private set
            {
                _isDeviceConnected = value;
                NotifyPropertyChanged("IsDeviceConnected");
            }
        }

        public bool IsUpdateRequired
        {
            get => _isUpdateRequired;
            private set
            {
                _isUpdateRequired = value;
                NotifyPropertyChanged("IsUpdateRequired");
            }
        }

        public bool InProgress
        {
            get => _isPorgress;
            private set
            {
                _isPorgress = value;
                NotifyPropertyChanged("InProgress");
            }
        }

        public string DeviceVersionMessage
        {
            get => _deviceVersionMessage;
            private set
            {
                _deviceVersionMessage = value;
                NotifyPropertyChanged("DeviceVersionMessage");
            }
        }

        public string LatestVersionMessage
        {
            get => _latestVersionMessage;
            private set
            {
                _latestVersionMessage = value;
                NotifyPropertyChanged("LatestVersionMessage");
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

        public string Version { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public StatusFormViewModel(ISynchronizeInvoke syncObject)
        {
            _syncObject = syncObject;
            _usbDeviceDetector.DeviceStatusEvent += OnDeviceStatusChanged;
        }

        private void OnDeviceStatusChanged(object sender, DeviceStatus e)
        {
            ReadDeviceStatus();
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

        public void ReadDeviceStatus()
        {
            InProgress = true;
            Task.Run(async () =>
            {
                try
                {
                    Version = await _deviceManager.GetLatestVersion();
                    LatestVersionMessage = $"Latest firmware version: {Strings.NormalizeVersion(Version)}";
                }
                catch (Exception)
                {
                    HasError = true;
                    LatestVersionMessage = "Unnable to check the latest firmware version";
                }

                try
                {
                    var deviceVersion = await _deviceManager.GetDeviceVersion();
                    IsDeviceConnected = true;
                    DeviceStatusMessage = "Device Status - Connected";
                    DeviceVersionMessage = $"Device firmware version: {Strings.NormalizeVersion(deviceVersion)}";

                    IsUpdateRequired = new Version(deviceVersion).CompareTo(new Version(Version)) != 0;
                }
                catch (Exception)
                {
                    HasError = true;
                    IsDeviceConnected = false;
                    DeviceStatusMessage = "Device status: Disconnected";
                    DeviceVersionMessage = $"Device firmware version: -";
                }

                InProgress = false;
            });
        }

        public void Dispose()
        {
            _usbDeviceDetector.DeviceStatusEvent -= OnDeviceStatusChanged;
            _cancellationTokenSource.Cancel(true);
        }
    }
}