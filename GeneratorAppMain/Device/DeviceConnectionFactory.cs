namespace GeneratorAppMain.Device
{
    interface IDeviceConnectionFactory
    {
        IDeviceConnection Connect();
    }

    class DeviceConnectionFactory : IDeviceConnectionFactory
    {
        public IDeviceConnection Connect()
        {
            return new DeviceConnection();
            //return new FakeDeviceConnection();
        }
    }
}