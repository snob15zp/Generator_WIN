using GeneratorAppMain.Forms;
using GeneratorAppMain.Messages;
using GeneratorAppMain.Properties;
using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Windows.Forms;

namespace GeneratorAppMain
{
    class GeneratorApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;

        private readonly IMessageHandler _messageHandler = UnityConfiguration.Resolve<IMessageHandler>();

        public GeneratorApplicationContext()
        {
            _trayIcon = new NotifyIcon()
            {
                Icon = Resources.AppIcon,
                ContextMenu = new ContextMenu(new[] {
                    new MenuItem("Check For Updates", CheckForUpdates),
                    //TODO: Just for test. Should remove from release build
                    new MenuItem("Download", DownloadPrograms),
                    new MenuItem("Exit", Exit)
                }),
                Visible = true
            };

            _messageHandler.MessageReceived += MessageHandler_MessageReceived;
        }

        private void DownloadPrograms(object sender, EventArgs e)
        {
            string url = Interaction.InputBox("Enter the download link", "Download programs");
            if (!url.StartsWith("generator://inhealion.gr/generator"))
            {
                MessageBox.Show("The download link is not valid", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            MessageHandler_MessageReceived(this, url.Substring(34));
        }

        private void MessageHandler_MessageReceived(object sender, string e)
        {
            if (Application.OpenForms.OfType<DownloadForm>().Any())
            {
                MessageBox.Show("Download already in progress", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var downloadForm = new DownloadForm(e);
            downloadForm.ShowDialog();
        }

        private void CheckForUpdates(object sender, EventArgs e)
        {
            ((MenuItem)sender).Enabled = false;
            var form = new VersionUpdateForm();
            form.FormClosed += delegate { ((MenuItem)sender).Enabled = true; };
            form.ShowDialog();
        }

        private void Exit(object sender, EventArgs e)
        {
            _trayIcon.Dispose();
            Application.Exit();
        }
    }
}
