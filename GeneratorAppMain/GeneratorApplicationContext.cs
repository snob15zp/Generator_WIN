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
                    new MenuItem("Download", (sender, e)=>MessageHandler_MessageReceived(sender, "/folders/4gGejPZxmJ56/download/ZXlKcGRpSTZJbXBMTlVWbldFMDFXV0pSTTBKVWEwcDRhMFE0TW5jOVBTSXNJblpoYkhWbElqb2lkVW95VDNSM1RUWnlXSEZZUzBkQlQzcGFiV2M0YVZGclZrOVFOa1pyVm1SWFVtOXROMVJ0ZEVGR1p6MGlMQ0p0WVdNaU9pSTNabU5pWm1SbU16STRNbU5pT1dKbE9EYzBPRGRsTW1RMllUTmpaVEl6WlRObFlUVXhOakF3T1dJNE56bGpZMkl5Wm1KbU1qTmpZelU0WVRBeU5HVTRJbjA9")),
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
