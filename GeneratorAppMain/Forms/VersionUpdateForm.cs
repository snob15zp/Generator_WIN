using System;
using System.Windows.Forms;
using GeneratorAppMain.Utils;
using GeneratorAppMain.ViewModel;

namespace GeneratorAppMain.Forms
{
    public partial class VersionUpdateForm : Form
    {
        private readonly ProgressFormViewModel _progressFormViewModel;
        private readonly string _version;

        public VersionUpdateForm(string version = null)
        {
            InitializeComponent();

            _version = version;
            _progressFormViewModel = new ProgressFormViewModel(this);

            SetupBindings();
        }

        private void SetupBindings()
        {
            actionLabel.DataBindings.Add(new Binding("Text", _progressFormViewModel, "DeviceStatusMessage"));

            infoLabel.DataBindings.Add(new Binding("Text", _progressFormViewModel, "DeviceInfoMessage"));
            infoLabel.DataBindings.Add(Bindings.VisibleNullableBinding(_progressFormViewModel, "DeviceInfoMessage"));

            progressBar.DataBindings.Add(new Binding("Visible", _progressFormViewModel, "InProgress"));
            updateButton.DataBindings.Add(new Binding("Visible", _progressFormViewModel, "UpdateIsReady"));
            okButton.DataBindings.Add(new Binding("Visible", _progressFormViewModel, "IsFinished"));

            resultPictureBox.DataBindings.Add(Bindings.VisibleNullableBinding(_progressFormViewModel, "Icon"));
            resultPictureBox.DataBindings.Add(new Binding("Image", _progressFormViewModel, "Icon", true));

            cancelButton.DataBindings.Add(Bindings.NegativeVisibleBinding(_progressFormViewModel, "InProgress"));
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void updateButton_Click(object sender, EventArgs args)
        {
            _progressFormViewModel.DownloadFirmware(_progressFormViewModel.Version);
        }

        private void VersionUpdateForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _progressFormViewModel.Dispose();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void VersionUpdateForm_Load(object sender, EventArgs e)
        {
            if (_version != null)
            {
                _progressFormViewModel.DownloadFirmware(_version);
            }
            else
            {
                _progressFormViewModel.CheckForUpdates();
            }
        }
    }
}