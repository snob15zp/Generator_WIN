using GeneratorAppMain.Messages;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;

namespace GeneratorAppMain
{
    static class Program
    {
        [DllImport("Shcore.dll")]
        static extern int SetProcessDpiAwareness(int PROCESS_DPI_AWARENESS);

        // According to https://msdn.microsoft.com/en-us/library/windows/desktop/dn280512(v=vs.85).aspx
        private enum DpiAwareness
        {
            None = 0,
            SystemAware = 1,
            PerMonitorAware = 2
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            UnityConfiguration.RegisterComponents();

            var server = UnityConfiguration.Resolve<IMessageServer>();
            server.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SetProcessDpiAwareness((int)DpiAwareness.PerMonitorAware); SetProcessDpiAwareness((int)DpiAwareness.PerMonitorAware);

            Application.Run(new GeneratorApplicationContext());

            server.Stop();
        }
    }
}
