using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using log4net.Config;

namespace GeneratorAppManager
{
    class Program
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(Program));

        private static readonly string Prefix = Prefix = "generator://inhealion.gr/generator";
        private static readonly string MainProgramName = "inhealion";

        private static readonly int RunPocessWaitingTimeoutMs = 1000;
        private static readonly int ConnectToPocessWaitingTimeoutMs = 1000;

        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            if (args.Length == 0 || !args[0].StartsWith(Prefix))
            {
                Logger.Warn("Download link is missing");
                return;
            }

            var process = Process.GetProcessesByName(MainProgramName).FirstOrDefault();
            if (process == null)
            {
                var fileName = Process.GetCurrentProcess().MainModule?.FileName;
                var path = fileName != null ? Path.GetDirectoryName(fileName) : ".";
                process = new Process
                {
                    StartInfo = new ProcessStartInfo($"{path}\\{MainProgramName}.exe", "skip-welcome")
                    {
                        WorkingDirectory = args[0]
                    }
                };

                Logger.Info($"Start main process {path}");
                try
                {
                    process.Start();
                    Thread.Sleep(RunPocessWaitingTimeoutMs);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to start process", e);
                    MessageBox.Show($"Unable to start import.\r\n{e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            using (NamedPipeClientStream namedPipeClient = new NamedPipeClientStream("GeneratorPipe"))
            {
                try
                {
                    Logger.Info($"Connect to pipe");
                    namedPipeClient.Connect(ConnectToPocessWaitingTimeoutMs);
                    string url = args[0].Substring(Prefix.Length);
                    byte[] messageBytes = Encoding.UTF8.GetBytes(url);
                    namedPipeClient.Write(messageBytes, 0, messageBytes.Length);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to connect to the process", e);
                    MessageBox.Show("Unnable to run import", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }
    }
}
