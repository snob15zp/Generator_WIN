using GeneratorWindowsApp.Device;
using GeneratorWindowsApp.Utils;
using GeneratorWindowsApp.ViewModel;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace GeneratorWindowsApp.Forms
{
    public partial class VersionUpdateForm : Form
    {
        private readonly ProgressFormViewModel progressFormViewModel;

        public VersionUpdateForm()
        {
            InitializeComponent();

            progressFormViewModel = new ProgressFormViewModel(this);
            SetupBindings();

            progressFormViewModel.CheckForUpdates();
        }

        private void SetupBindings()
        {
            actionLabel.DataBindings.Add(new Binding("Text", progressFormViewModel, "DeviceStatusMessage"));
            
            infoLabel.DataBindings.Add(new Binding("Text", progressFormViewModel, "DeviceInfoMessage"));
            infoLabel.DataBindings.Add(Bindings.VisibleNullableBinding(progressFormViewModel, "DeviceInfoMessage"));

            progressBar.DataBindings.Add(new Binding("Visible", progressFormViewModel, "InProgress"));
            updateButton.DataBindings.Add(new Binding("Visible", progressFormViewModel, "UpdateIsReady"));
            okButton.DataBindings.Add(new Binding("Visible", progressFormViewModel, "IsFinished"));

            resultPictureBox.DataBindings.Add(Bindings.VisibleNullableBinding(progressFormViewModel, "Icon"));
            resultPictureBox.DataBindings.Add(new Binding("Image", progressFormViewModel, "Icon", true));

            cancelButton.DataBindings.Add(Bindings.NegativeVisibleBinding(progressFormViewModel, "IsFinished"));
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void updateButton_Click(object sender, EventArgs args)
        {
            progressFormViewModel.DownloadFirmware();
        }

        private void VersionUpdateForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            progressFormViewModel.Dispose();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Dispose();
        }
    }
}
