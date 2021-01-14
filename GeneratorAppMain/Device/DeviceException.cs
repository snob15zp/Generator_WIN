using GenLib;
using System;

namespace GeneratorAppMain.Device
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

        public DeviceUpdateException() : base("Unable to update device")
        {
        }


        public DeviceUpdateException(ErrorCodes errorCode) : base("Unable to update device")
        {
            this.ErrorCode = errorCode;
        }
    }
}
