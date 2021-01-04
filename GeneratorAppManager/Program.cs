using System;
using System.IO.Pipes;
using System.Text;

namespace GeneratorAppManager
{
    class Program
    {
        static void Main(string[] args)
        {
            using (NamedPipeClientStream namedPipeClient = new NamedPipeClientStream("GeneratorPipe"))
            {
                Console.Write("Enter a message to be sent to the server: ");
                string message = Console.ReadLine();
                namedPipeClient.Connect();
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                namedPipeClient.Write(messageBytes, 0, messageBytes.Length);
            }
        }
    }
}
