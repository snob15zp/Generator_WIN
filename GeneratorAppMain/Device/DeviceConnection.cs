using FTD2XX_NET;
using GenLib;
using System;
using System.Collections.Generic;
using System.Threading;

namespace GeneratorAppMain.Device
{
    interface IDeviceConnection : IGenerator, IDisposable { }

    class DeviceConnection : GenG070V1, IDeviceConnection
    {
        public DeviceConnection() : base(GetSerialPort())
        {
        }

        public void Dispose()
        {
            Disconnect();
        }

        private static string GetSerialPort()
        {
            FTDI ftdi = new FTDI();
            ftdi.OpenByIndex(0);
            FTDI.FT_STATUS status = ftdi.GetCOMPort(out string port);
            ftdi.Close();
            if (status == FTDI.FT_STATUS.FT_OK)
            {
                return port;
            }
            else
            {
                throw new DeviceNotConnectedException();
            }
        }
    }

    class FakeDeviceConnection : IDeviceConnection
    {
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

        public ErrorCodes Erase(string filename)
        {
            return ErrorCodes.NoError;
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

        public ErrorCodes PutFile(string fileName, IEnumerable<byte> content)
        {
            Thread.Sleep(1000);
            return ErrorCodes.NoError;
        }

        public void Dispose()
        {
        }
    }
}
