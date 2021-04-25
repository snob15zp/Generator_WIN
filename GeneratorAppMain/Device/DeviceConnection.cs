using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GenLib;

namespace GeneratorAppMain.Device
{
    internal interface IDeviceConnection : IGenerator, IDisposable
    {
    }

    internal class DeviceConnection : GenG070V1, IDeviceConnection
    {
        public DeviceConnection() : base(GetSerialPortDescription()) { }

        public void Dispose()
        {
            Disconnect();
        }

        private static string GetSerialPortDescription()
        {
            var devices = ListFtdiDevices().ToArray();
            if (devices.Length > 0)
                return devices[0];
            throw new DeviceNotConnectedException();
        }
    }
}