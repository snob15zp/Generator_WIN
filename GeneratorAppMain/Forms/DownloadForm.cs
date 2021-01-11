using GeneratorWindowsApp.Device;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeneratorWindowsApp.Forms
{
    public partial class DownloadForm : Form
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IDeviceManager deviceManager = UnityConfiguration.Resolve<IDeviceManager>();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public DownloadForm(string url) : base()
        {
            InitializeComponent();
            deviceManager.DeviceUpdateStatusEvent += DeviceManager_DeviceUpdateStatusEvent;
            startDownload(url);
        }

        private void DeviceManager_DeviceUpdateStatusEvent(object sender, DeviceUpdateStatusEventArgs args)
        {
            string message = null;
            switch (args.Status)
            {
                case DeviceUpdateStatus.Downloading:
                    message = "Downloading...";
                    break;
                case DeviceUpdateStatus.Updating:
                    if (args.Progress >= 0)
                    {
                        message = message = $"Device update in progress ({args.Progress}%)";
                    }
                    else
                    {
                        message = "Device update in progress...";
                    }
                    break;
                case DeviceUpdateStatus.Rebooting:
                    message = "Wait for device rebooting...";
                    break;
                case DeviceUpdateStatus.Ready:
                    break;
            }

            if (InvokeRequired)
            {
                Invoke(new Action<string>((text) => actionLabel.Text = text), new object[] { message });
            }
            else
            {
                actionLabel.Text = message;
            }
        }

        private async void startDownload(string url)
        {
            switchToInProgressState("Downloading...");
            try
            {
                await deviceManager.DonwloadPrograms(url, cancellationTokenSource.Token);
                switchToSuccessState("Prograams import successfully.");
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cancellationTokenSource.Token)
            {
                logger.Info("Download was canceled.");
                switchToErrorState("Operation was canceled.");
            }
            catch (DeviceException e)
            {
                logger.Error(e, "Unable to download programs");
                switchToErrorState(e.Message);
            }
            catch (Exception e)
            {
                logger.Error(e, "Unable to download programs");
                switchToErrorState("Somethig wrong happened.");
            }
        }

        private void switchToInProgressState(string actionText)
        {
            infoLabel.Visible = false;
            resultPictureBox.Visible = false;
            progressBar.Visible = true;

            actionLabel.Text = actionText;
            cancelButton.Text = "Cancel";
        }

        private void switchToSuccessState(string message)
        {
            switchToFinishState(message, null, Properties.Resources.StatusOK_48x);
        }

        private void switchToErrorState(string error)
        {
            switchToFinishState("Error", error, Properties.Resources.StatusInvalid_48x);
        }

        private void switchToFinishState(string action, string message, Image icon)
        {
            progressBar.Visible = false;

            resultPictureBox.Visible = icon != null;
            resultPictureBox.Image = icon;

            cancelButton.Text = "OK";
            actionLabel.Text = action;

            infoLabel.Visible = message != null;
            infoLabel.Text = message;
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void DownloadForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            cancellationTokenSource.Cancel(true);
            deviceManager.DeviceUpdateStatusEvent -= DeviceManager_DeviceUpdateStatusEvent;
        }
    }
}
