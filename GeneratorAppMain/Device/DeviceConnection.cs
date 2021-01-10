using FTD2XX_NET;
using GenLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GeneratorWindowsApp.Device
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
            FTDI ftdi_dev = new FTDI();
            ftdi_dev.OpenByIndex(0);
            FTDI.FT_STATUS status = ftdi_dev.GetCOMPort(out string port);
            ftdi_dev.Close();
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
            Thread.Sleep(1000);
            return ErrorCodes.FatalError;
        }

        public void Dispose()
        {
        }
    }
}
