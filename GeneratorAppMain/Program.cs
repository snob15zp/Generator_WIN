using GeneratorWindowsApp.Device;
using GeneratorWindowsApp.Messages;
using GenLib;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using Unity;

namespace GeneratorWindowsApp
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

        private static void RegisterCustomUriIfNeeded()
        {
            bool isAdmin = true;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                //isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            if(isAdmin)
            {
                RegistryKey key;
                key = Registry.ClassesRoot.CreateSubKey("generator");
                key.SetValue("", "URL: Generator Protocol");
                key.SetValue("URL Protocol", "");

                key = key.CreateSubKey("shell");
                key = key.CreateSubKey("open");
                key = key.CreateSubKey("command");
                key.SetValue("", "C:\\oggsplit.exe");
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //RegisterCustomUriIfNeeded();
            UnityConfiguration.RegisterComponents();

            var server = UnityConfiguration.Resolve<IMessageServer>();
            server.start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SetProcessDpiAwareness((int)DpiAwareness.PerMonitorAware); SetProcessDpiAwareness((int)DpiAwareness.PerMonitorAware);

            Application.Run(new GeneratorApplicationContext());

            server.stop();
        }
    }
}
