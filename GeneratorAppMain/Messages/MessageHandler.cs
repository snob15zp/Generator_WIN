using System;

namespace GeneratorAppMain.Messages
{
    public interface IMessageHandler
    {
        event EventHandler<string> MessageReceived;

        void Handle(string message);
    }

    public class MessageHandler : IMessageHandler
    {

        public event EventHandler<string> MessageReceived;

        public void Handle(string message)
        {
            MessageReceived?.Invoke(this, message);
        }
    }
}
