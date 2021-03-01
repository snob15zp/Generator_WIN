using System;
using System.Linq;
using System.Management;
using System.Threading;
using System.Windows.Forms;
using GeneratorAppMain.Device;
using GeneratorAppMain.Forms;
using GeneratorAppMain.Messages;
using GeneratorAppMain.Properties;
using Microsoft.VisualBasic;

namespace GeneratorAppMain
{
    internal class GeneratorApplicationContext : ApplicationContext
    {
        private readonly IMessageHandler _messageHandler = UnityConfiguration.Resolve<IMessageHandler>();
        private readonly IUsbDeviceDetector _usbDeviceDetector = UnityConfiguration.Resolve<IUsbDeviceDetector>();
        private readonly NotifyIcon _trayIcon;


        public GeneratorApplicationContext(bool skipWelcome = false)
        {
            _trayIcon = new NotifyIcon
            {
                Icon = Resources.AppIcon,
                ContextMenu = new ContextMenu(new[]
                {
                    new MenuItem("Check For Updates", CheckForUpdates),
                    new MenuItem("Download", DownloadPrograms),
                    new MenuItem("Exit", Exit)
                }),
                Visible = true
            };

            _trayIcon.MouseDoubleClick += _trayIcon_Click;
            _usbDeviceDetector.DeviceStatusEvent += OnDeviceStatusChanged;
            _messageHandler.MessageReceived += MessageHandler_MessageReceived;

            if (!skipWelcome)
            {
                showStatusForm();
            }
        }

        private void OnDeviceStatusChanged(object sender, DeviceStatus e)
        {
            if (e == DeviceStatus.Connected)
            {
                showStatusForm();
            }
        }

        private void showStatusForm()
        {
            var forms = Application.OpenForms.OfType<StatusForm>();
            Form form;
            if (!forms.Any())
            {
                form = new StatusForm();
                form.ShowDialog();

            }
            else
            {
                form = forms.ToArray()[0];
            }

            if (form.InvokeRequired)
            {
                Action action = delegate () { form.BringToFront(); };
                form.Invoke(action, new object[] { });
            }
            else
            {
                form.BringToFront();
            }
        }

        private void _trayIcon_Click(object sender, EventArgs e)
        {
            showStatusForm();
        }

        private void DownloadPrograms(object sender, EventArgs e)
        {
            var form = new InputForm()
            {
                Text = "Download program",
                Message = "Download URL",
                Placeholder = "generator://inhelion.gr/generator/folders/{id}/{hash}"
            };
            var result = form.ShowDialog();
            if (result != DialogResult.OK) return;

            var url = form.InputText;
            var host = new Uri(Settings.Default.siteUrl);
            var prefix = $"generator://{host.Authority}/generator";
            if (!url.StartsWith(prefix))
            {
                MessageBox.Show("The download link is not valid", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageHandler_MessageReceived(this, url.Substring(prefix.Length));
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