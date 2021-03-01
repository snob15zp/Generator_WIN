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

    internal class FakeDeviceConnection : IDeviceConnection
    {
        public bool Ready { get; }
        public string Version => "0.0.1";

        public byte[] Serial => throw new NotImplementedException();

        public event EventHandler<Tuple<string, int, int>> OnPutFilePart;

        public void BootloaderReset()
        {
            Thread.Sleep(1000);
        }

        public void BootloaderRunMcuFw()
        {
            Thread.Sleep(1000);
        }

        public bool BootloaderUploadMcuFwChunk(byte[] chunk)
        {
            Thread.Sleep(100);
            return true;
        }

        public void Disconnect()
        {
        }

        public bool TryToInit()
        {
            return true;
        }

        public ErrorCodes EraseAll()
        {
            return ErrorCodes.NoError;
        }

        public ErrorCodes EraseByExt(string Ext)
        {
            Thread.Sleep(1000);
            return ErrorCodes.NoError;
        }

        public ErrorCodes PutFile(string fileName, IEnumerable<byte> content, bool encrypted)
        {
            Thread.Sleep(1000);
            return ErrorCodes.NoError;
        }

        public void Dispose()
        {
        }

        public ErrorCodes Erase(string filename)
        {
            return ErrorCodes.NoError;
        }

        public void BootloaderSetVersion(Version version)
        {
            throw new NotImplementedException();
        }
    }
}