using GenLib;
using System;
using System.Collections.Generic;

namespace GeneratorWindowsApp.Device
{
    interface IDeviceFactory
    {
        IGenerator create(string port);
    }

    class DeviceFactory : IDeviceFactory
    {
        public IGenerator create(string port)
        {
            //return new FakeDevice();
            return new GenG070V1(port);

        }
    }


    internal class FakeDevice : IGenerator
    {
        public string Version => "0.1.1";

        public byte[] Serial => throw new NotImplementedException();

        public void Disconnect()
        {

        }

        public ErrorCodes Erase(string Filename)
        {
            return ErrorCodes.NoError;
        }

        public ErrorCodes EraseAll()
        {
            return ErrorCodes.NoError;
        }

        public ErrorCodes PutFile(string FileName, IEnumerable<byte> content)
        {
            return ErrorCodes.FatalError;
        }
    }

}
