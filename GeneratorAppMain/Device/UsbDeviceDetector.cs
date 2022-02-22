using System;
using System.Diagnostics;
using System.Management;
using System.Threading;

namespace GeneratorAppMain.Device
{
    enum DeviceStatus
    {
        Connected,
        Disconnected
    }

    interface IUsbDeviceDetector
    {

        event EventHandler<DeviceStatus> DeviceStatusEvent;

    }

    class UsbDeviceDetector: IUsbDeviceDetector
    {
        private ManagementEventWatcher watcher;
        private DeviceManager _deviceManager;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public event EventHandler<DeviceStatus> DeviceStatusEvent;

        public UsbDeviceDetector(DeviceManager deviceManager)
        {
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
            watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += OnInstanceCreated;
            watcher.Start();

            query = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
            watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += OnInstanceDeleted;
            watcher.Start();


            _deviceManager = deviceManager;
        }

        private async void OnInstanceCreated(object sender, EventArrivedEventArgs args)
        {
            if (IsInstanceValid(args))
            {
                try
                {
                    await _deviceManager.AwaitDeviceConnection(10000);
                    Debug.WriteLine("Device connected");
                    DeviceStatusEvent?.Invoke(this, DeviceStatus.Connected);
                }
                catch (Exception e)
                {
                    Debug.Fail($"Device is not connected {e.Message}", e.StackTrace);
                    //Ignore
                }
            }
        }
        private void OnInstanceDeleted(object sender, EventArrivedEventArgs args)
        {
            if (IsInstanceValid(args))
            {
                DeviceStatusEvent?.Invoke(this, DeviceStatus.Disconnected);
            }
        }

        private bool IsInstanceValid(EventArrivedEventArgs e)
        {
            var targetInstance = e.NewEvent.Properties["TargetInstance"].Value as ManagementBaseObject;
            return targetInstance.Properties["DeviceID"].Value.ToString().StartsWith("FTDIBUS");
        }
    }
}
