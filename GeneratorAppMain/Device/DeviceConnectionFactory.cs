using System.IO;

namespace GeneratorAppMain.Device
{
    internal interface IDeviceConnectionFactory
    {
        IDeviceConnection Connect();
    }

    internal class DeviceConnectionFactory : IDeviceConnectionFactory
    {
        public IDeviceConnection Connect()
        {
            var deviceConnection =  new DeviceConnection();
            return deviceConnection;
            //return new FakeDeviceConnection();
        }
    }
}