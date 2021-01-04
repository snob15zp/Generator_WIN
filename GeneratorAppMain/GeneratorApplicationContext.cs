using GeneratorApiLibrary;
using GeneratorWindowsApp.Device;
using GeneratorWindowsApp.Forms;
using GeneratorWindowsApp.Messages;
using GeneratorWindowsApp.Properties;
using System;
using System.Linq;
using System.Windows.Forms;
using Unity;

namespace GeneratorWindowsApp
{
    class GeneratorApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;

        private readonly IMessageHandler messageHandler = UnityConfiguration.Resolve<IMessageHandler>();

        public GeneratorApplicationContext()
        {
            trayIcon = new NotifyIcon()
            {
                Icon = Resources.AppIcon,
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("Check For Updates", CheckForUpdates),
                    new MenuItem("Exit", Exit)
                }),
                Visible = true
            };

            messageHandler.MessageReceived += MessageHandler_MessageReceived;
        }

        private void MessageHandler_MessageReceived(object sender, string e)
        {
            if (Application.OpenForms.OfType<DownloadForm>().Count() > 0)
            {
                MessageBox.Show("Download already in progress", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var downloadForm = new DownloadForm(e);
            downloadForm.ShowDialog();
        }

        private void CheckForUpdates(object sender, EventArgs e)
        {
            (sender as MenuItem).Enabled = false;
            var form = new VersionUpdateForm();
            form.FormClosed += delegate (Object obj, FormClosedEventArgs args)
            {
                (sender as MenuItem).Enabled = true;
            };
            form.ShowDialog();
        }

        private void Exit(object sender, EventArgs e)
        {
            trayIcon.Dispose();
            Application.Exit();
        }
    }
}
