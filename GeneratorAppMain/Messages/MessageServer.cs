using System.Diagnostics;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GeneratorAppMain.Messages
{
    public interface IMessageServer
    {
        void Start();
        void Stop();
    }

    public class MessageServer : IMessageServer
    {
        private const int BufferSize = 1024;
        private const int MaxNumberOfServers = 10;
        private const string PipeName = "GeneratorPipe";


        private readonly NamedPipeServerStream _namedPipeServer;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;

        private readonly IMessageHandler _messageHandler;

        private Task _task = null;

        public MessageServer(IMessageHandler messageHandler)
        {
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;
            PipeSecurity pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
            pipeSecurity.SetGroup(user);
            pipeSecurity.SetOwner(user);

            _namedPipeServer =
                new NamedPipeServerStream(PipeName,
                    PipeDirection.InOut, MaxNumberOfServers,
                    PipeTransmissionMode.Message, PipeOptions.WriteThrough,
                    BufferSize, BufferSize, pipeSecurity);

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            this._messageHandler = messageHandler;
        }

        public void Start()
        {
            if (_task != null && _task.Status == TaskStatus.Running) return;

            _task = new Task(PipeServerTask, _cancellationToken);
            _task.Start();
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _task?.Dispose();
        }

        private async void PipeServerTask()
        {
            Debug.WriteLine("Server started");

            while (!_cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine("Wait for connection");
                await _namedPipeServer.WaitForConnectionAsync(_cancellationToken);

                var message = readMessage(_namedPipeServer);
                Debug.WriteLine("Message received: " + message);

                _messageHandler.Handle(message);
                _namedPipeServer.Disconnect();
            }

            Debug.WriteLine("Server finished");
        }

        private string readMessage(PipeStream pipeStream)
        {
            StringBuilder messageBuilder = new StringBuilder();
            byte[] messageBuffer = new byte[256];
            do
            {
                var size = pipeStream.Read(messageBuffer, 0, messageBuffer.Length);
                var messageChunk = Encoding.UTF8.GetString(messageBuffer, 0, size);
                messageBuilder.Append(messageChunk);
            }
            while (!pipeStream.IsMessageComplete);
            return messageBuilder.ToString().TrimEnd();
        }
    }
}
