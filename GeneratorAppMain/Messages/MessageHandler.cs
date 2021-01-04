using GeneratorApiLibrary;
using GeneratorWindowsApp.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneratorWindowsApp.Messages
{
    public interface IMessageHandler {
        event EventHandler<String> MessageReceived;

        void handle(string message);
    }

    public class MessageHandler: IMessageHandler
    {

        public event EventHandler<string> MessageReceived;

        private readonly IDeviceManager deviceManager;
        private readonly IGeneratorApi api;

        public MessageHandler(IDeviceManager deviceManager, IGeneratorApi api)
        {
            this.deviceManager = deviceManager;
            this.api = api;
        }


        public void handle(string message)
        {
            MessageReceived?.Invoke(this, message);
        }
    }
}
