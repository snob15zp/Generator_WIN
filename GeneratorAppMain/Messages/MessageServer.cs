using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeneratorWindowsApp.Messages
{
    public interface IMessageServer {
        void start();
        void stop();
    }


    public class MessageServer: IMessageServer
    {
        private static readonly int BUFFER_SIZE = 1024;
        private static readonly string PIPE_NAME = "GeneratorPipe";


        private readonly NamedPipeServerStream namedPipeServer;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly CancellationToken cancellationToken;
        
        private readonly IMessageHandler messageHandler;

        private Task task = null;

        public MessageServer(IMessageHandler messageHandler)
        {
            PipeSecurity pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
            pipeSecurity.AddAccessRule(new PipeAccessRule("CREATOR OWNER", PipeAccessRights.FullControl, AccessControlType.Allow));
            pipeSecurity.AddAccessRule(new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, AccessControlType.Allow));

            namedPipeServer =
                new NamedPipeServerStream(PIPE_NAME, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.WriteThrough, BUFFER_SIZE, BUFFER_SIZE, pipeSecurity);

            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            this.messageHandler = messageHandler;
        }

        public void start()
        {
            if (task != null && task.Status == TaskStatus.Running) return;

            task = new Task(pipeServerTask, cancellationToken);
            task.Start();
        }

        public void stop()
        {
            cancellationTokenSource.Cancel();
            if (task != null) task.Dispose();
        }

        private async void pipeServerTask()
        {
            Debug.WriteLine("Server started");

            while (!cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine("Wait for connection");
                await namedPipeServer.WaitForConnectionAsync(cancellationToken);

                var message = readMessage(namedPipeServer);
                Debug.WriteLine("Message recived: " + message);

                messageHandler.handle(message);
                namedPipeServer.Disconnect();
            }

            Debug.WriteLine("Server finished");
        }

        private string readMessage(PipeStream pipeStream)
        {
            StringBuilder messageBuilder = new StringBuilder();
            string messageChunk = string.Empty;
            byte[] messageBuffer = new byte[256];
            do
            {
                var size = pipeStream.Read(messageBuffer, 0, messageBuffer.Length);
                messageChunk = Encoding.UTF8.GetString(messageBuffer, 0, size);
                messageBuilder.Append(messageChunk);
            }
            while (!pipeStream.IsMessageComplete);
            return messageBuilder.ToString().TrimEnd();
        }
    }
}
