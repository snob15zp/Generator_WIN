using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace GeneratorAppManager
{
    class Program
    {
        private static readonly string PREFIX = "generator://inhealion.gr/generator";
        private static readonly string MAIN_PROGRAM_NAME = "generator";

        static void Main(string[] args)
        {
            if (args.Length == 0 || !args[0].StartsWith(PREFIX))
            {
                return;
            }

            var process = Process.GetProcessesByName(MAIN_PROGRAM_NAME).FirstOrDefault();
            if (process == null)
            {
                process = new Process()
                {
                    StartInfo = new ProcessStartInfo { FileName = $"{MAIN_PROGRAM_NAME}.exe" }
                };
                process.Start();
                process.WaitForInputIdle();
            }

            using (NamedPipeClientStream namedPipeClient = new NamedPipeClientStream("GeneratorPipe"))
            {
                namedPipeClient.Connect();
                string url = args[0].Substring(PREFIX.Length);
                byte[] messageBytes = Encoding.UTF8.GetBytes(url);
                namedPipeClient.Write(messageBytes, 0, messageBytes.Length);
            }
        }
    }
}
