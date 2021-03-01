using System;
using GenLib;

namespace GeneratorAppMain.Device
{
    internal class DeviceException : Exception
    {
        public DeviceException(string message) : base(message)
        {
        }
    }


    internal class DeviceNotConnectedException : DeviceException
    {
        public DeviceNotConnectedException() : base("Device is not connected")
        {
        }
    }

    internal class DeviceUpdateException : DeviceException
    {
        public DeviceUpdateException() : base("Unable to update device")
        {
        }


        public DeviceUpdateException(ErrorCodes errorCode) : base("Unable to update device")
        {
            ErrorCode = errorCode;
        }

        public ErrorCodes ErrorCode { get; }
    }
}