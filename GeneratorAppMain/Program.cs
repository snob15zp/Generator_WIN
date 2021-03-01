using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using GeneratorAppMain.Messages;
using log4net;
using log4net.Config;

namespace GeneratorAppMain
{
    internal static class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));


        [DllImport("Shcore.dll")]
        private static extern int SetProcessDpiAwareness(int PROCESS_DPI_AWARENESS);

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            using (var mutex = new Mutex(true, "GeneratorAppMain", out var createdNew))
            {
                if (createdNew)
                {
                    XmlConfigurator.Configure();

                    Logger.Info("Application started");

                    UnityConfiguration.RegisterComponents();

                    var server = UnityConfiguration.Resolve<IMessageServer>();
                    server.Start();

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    SetProcessDpiAwareness((int) DpiAwareness.PerMonitorAware);

                    var skipWelcome = args.Contains("skip-welcome");
                    Application.Run(new GeneratorApplicationContext(skipWelcome));

                    server.Stop();
                    LogManager.Shutdown();
                }
            }
        }

        // According to https://msdn.microsoft.com/en-us/library/windows/desktop/dn280512(v=vs.85).aspx
        private enum DpiAwareness
        {
            None = 0,
            SystemAware = 1,
            PerMonitorAware = 2
        }
    }
}