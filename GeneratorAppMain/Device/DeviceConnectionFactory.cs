using GenLib;
using System;
using System.Collections.Generic;

namespace GeneratorWindowsApp.Device
{
    interface IDeviceConnectionFactory
    {
        IDeviceConnection connect();
    }

    class DeviceConnectionFactory : IDeviceConnectionFactory
    {
        public IDeviceConnection connect()
        {
            return new DeviceConnection();
            //return new FakeDeviceConnection();
        }
    }
}