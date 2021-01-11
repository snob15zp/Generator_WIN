using GeneratorWindowsApp.Device;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace GeneratorWindowsApp.Forms
{
    public partial class VersionUpdateForm : Form
    {
        private readonly IDeviceManager deviceManager = UnityConfiguration.Resolve<IDeviceManager>();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private string latestVersion;

        public VersionUpdateForm()
        {
            InitializeComponent();
            CheckForUpdates();

            deviceManager.DeviceUpdateStatusEvent += DeviceManager_DeviceUpdateStatusEvent;
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
                Invoke(
                    new Action<string, int>((text, progress) => UpdateUploadStatus(text, progress)),
                    new object[] { message, args.Progress });
            }
            else
            {
                UpdateUploadStatus(message, args.Progress);
            }
        }

        private void UpdateUploadStatus(string text, int progress)
        {
            actionLabel.Text = text;
            actionLabel.Refresh();
        }

        private async void CheckForUpdates()
        {
            switchToInProgressState("Checking for updates...");
            try
            {
                var versionInfo = await deviceManager.CheckForUpdates(cancellationTokenSource.Token);
                if (versionInfo.isUpdateAvialable)
                {
                    latestVersion = versionInfo.latestVersion.ToString();
                    var currentVersion = versionInfo.currentVersion.ToString();
                    switchToFinishState("Updates available.", $"Current version is {currentVersion}, latest version is {latestVersion}", null);
                    cancelButton.Text = "Cancel";
                    updateButton.Visible = true;
                }
                else
                {
                    switchToFinishState("Latest version already installed", null, null);
                }
            }
            catch (DeviceException e)
            {
                switchToErrorState(e.Message);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cancellationTokenSource.Token)
            {
                switchToErrorState("Operation was canceled.");
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        { 
            Dispose();
        }

        private async void updateButton_Click(object sender, EventArgs args)
        {
            switchToInProgressState("Updating...");
            try
            {
                await deviceManager.DownloadFirmware(latestVersion, cancellationTokenSource.Token);
                switchToSuccessState("Device updated successfully.");
                infoLabel.Visible = true;
                infoLabel.Text = $"Current version is {latestVersion}";
            }
            catch (DeviceException e)
            {
                switchToErrorState(e.Message);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cancellationTokenSource.Token)
            {
                switchToErrorState("Operation was canceled.");
            }
        }

        private void switchToInProgressState(string actionText)
        {
            updateButton.Visible = false;
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
            updateButton.Visible = false;
            progressBar.Visible = false;

            resultPictureBox.Visible = icon != null;
            resultPictureBox.Image = icon;

            cancelButton.Text = "OK";
            actionLabel.Text = action;

            infoLabel.Visible = message != null;
            infoLabel.Text = message;
        }

        private void VersionUpdateForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            cancellationTokenSource.Cancel(true);
            deviceManager.DeviceUpdateStatusEvent -= DeviceManager_DeviceUpdateStatusEvent;
        }
    }
}
