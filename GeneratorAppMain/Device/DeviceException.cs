using GenLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneratorWindowsApp.Device
{
    class DeviceException : Exception
    {
        public DeviceException(string message) : base(message)
        {
        }
    }


    class DeviceNotConnectedException : DeviceException
    {
        public DeviceNotConnectedException() : base("Device is not connected")
        {
        }
    }

    class DeviceUpdateException : DeviceException
    {
        public ErrorCodes ErrorCode { get; }

        public DeviceUpdateException(ErrorCodes errorCode) : base("Unable to update device")
        {
            this.ErrorCode = errorCode;
        }
    }
}
