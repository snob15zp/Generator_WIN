using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GeneratorAppManager
{
    class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly string Prefix = "generator://inhealion.gr/generator";
        private static readonly string MainProgramName = "generator";

        static void Main(string[] args)
        {
            if (args.Length == 0 || !args[0].StartsWith(Prefix))
            {
                Logger.Warn("Dowload link is missing");
                return;
            }

            var process = Process.GetProcessesByName(MainProgramName).FirstOrDefault();
            if (process == null)
            {
                var fileName = Process.GetCurrentProcess().MainModule?.FileName;
                var path = fileName != null ? Path.GetDirectoryName(fileName) : ".";
                process = new Process
                {
                    StartInfo = new ProcessStartInfo($"{path}\\{MainProgramName}.exe")
                    {
                        WorkingDirectory = args[0]
                    }
                };
                try
                {
                    process.Start();
                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to start process");
                    MessageBox.Show($"Unable to start import.\r\n{e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            using (NamedPipeClientStream namedPipeClient = new NamedPipeClientStream("GeneratorPipe"))
            {
                namedPipeClient.Connect();
                string url = args[0].Substring(Prefix.Length);
                byte[] messageBytes = Encoding.UTF8.GetBytes(url);
                namedPipeClient.Write(messageBytes, 0, messageBytes.Length);
            }
        }
    }
}
