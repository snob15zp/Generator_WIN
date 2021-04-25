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

        private event EventHandler<bool> StateEvent;
        private bool _isDeviceBusy = false;


        public GeneratorApplicationContext(bool skipWelcome = false)
        {
            _trayIcon = new NotifyIcon
            {
                Icon = Resources.AppIcon,
                ContextMenu = new ContextMenu(new[]
                {
                    new MenuItem("Check For Updates", CheckForUpdates),
                    new MenuItem("Download", DownloadPrograms),
                    new MenuItem("About", About),
                    new MenuItem("Exit", Exit)
                }),
                Visible = true
            };

            _trayIcon.MouseDoubleClick += _trayIcon_Click;
            _usbDeviceDetector.DeviceStatusEvent += OnDeviceStatusChanged;
            _messageHandler.MessageReceived += MessageHandler_MessageReceived;

            StateEvent += GeneratorApplicationContext_StateEvent;

            if (!skipWelcome)
            {
                ShowStatusForm();
            }
        }

        private void GeneratorApplicationContext_StateEvent(object sender, bool busy)
        {
            _isDeviceBusy = busy;
            _trayIcon.ContextMenu.MenuItems[0].Enabled = !busy;
            _trayIcon.ContextMenu.MenuItems[1].Enabled = !busy;
            _trayIcon.ContextMenu.MenuItems[2].Enabled = !busy;
            _trayIcon.ContextMenu.MenuItems[3].Enabled = !busy;
        }

        private void About(object sender, EventArgs e) => ShowDialog(delegate () { return new AboutForm(); });

        private void OnDeviceStatusChanged(object sender, DeviceStatus e)
        {
            if (e == DeviceStatus.Connected && !_isDeviceBusy)
            {
                ShowStatusForm();
            }
        }

        private void ShowStatusForm() => ShowDialog(delegate () { return new StatusForm(); });

        private void _trayIcon_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms.Count == 0)
            {
                ShowStatusForm();
            }
            else
            {
                InvokeFormActivate<Form>();
            }
        }

        private void DownloadPrograms(object sender, EventArgs e)
        {
            DisposeAllExcept(typeof(InputForm), typeof(DownloadForm));

            if (InvokeFormActivate<InputForm>() || InvokeFormActivate<DownloadForm>())
            {
                return;
            }

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

            ShowDownloadForm(url.Substring(prefix.Length));
        }

        private void MessageHandler_MessageReceived(object sender, string e)
        {
            ShowDownloadForm(e);
        }

        private void ShowDownloadForm(string url)
        {
            ShowDialog(delegate ()
            {
                StateEvent?.Invoke(this, true);
                var form = new DownloadForm(url);
                form.FormClosed += delegate
                {
                    StateEvent?.Invoke(this, false);
                };
                return form;
            });
        }

        private void CheckForUpdates(object sender, EventArgs e)
        {
            ShowDialog(delegate ()
           {
               StateEvent?.Invoke(this, true);
               var form = new VersionUpdateForm();
               form.FormClosed += delegate
               {
                   StateEvent?.Invoke(this, false);
               };
               return form;
           });
        }

        private void Exit(object sender, EventArgs e)
        {
            _trayIcon.Dispose();
            Application.Exit();
        }

        private void ShowDialog<T>(Func<T> createAction) where T : Form
        {
            if (!_isDeviceBusy)
            {
                DisposeAllExcept(typeof(T));
            }

            var form = Application.OpenForms.OfType<T>().FirstOrDefault();
            if (form == null)
            {
                form = createAction.Invoke();
                form.TopMost = true;
                form.ShowDialog();
            }
            else
            {
                InvokeFormActivate<T>();
            }
        }

        private void DisposeAllExcept(params Type[] types)
        {
            for (var i = 0; i < Application.OpenForms.Count; i++)
            {
                if (!types.Where(t => t == Application.OpenForms[i].GetType()).Any())
                {
                    InvokeIfNeeded(Application.OpenForms[i], delegate (Form form) { form.Dispose(); });
                }
            }
        }

        private bool InvokeFormActivate<T>() where T : Form
        {
            var form = Application.OpenForms.OfType<T>().FirstOrDefault();
            if (form == null) return false;

            InvokeIfNeeded(form, delegate (Form f)
            {
                f.Activate();
                f.TopMost = true;
                f.BringToFront();
            });
            return true;
        }

        private void InvokeIfNeeded(Form form, Action<Form> action)
        {
            if (form.InvokeRequired)
            {
                form.Invoke(action, new object[] { form });
            }
            else
            {
                action(form);
            }
        }
    }
}