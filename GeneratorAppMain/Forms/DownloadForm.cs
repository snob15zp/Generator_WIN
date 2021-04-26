using System;
using System.Windows.Forms;
using GeneratorAppMain.Utils;
using GeneratorAppMain.ViewModel;

namespace GeneratorAppMain.Forms
{
    public partial class DownloadForm : Form
    {
        private readonly ProgressFormViewModel _progressFormViewModel;

        public DownloadForm(string url)
        {
            InitializeComponent();

            _progressFormViewModel = new ProgressFormViewModel(this);
            SetupBindings();
            _progressFormViewModel.DownloadPrograms(url);
        }

        private void SetupBindings()
        {
            actionLabel.DataBindings.Add(new Binding("Text", _progressFormViewModel, "DeviceStatusMessage"));

            infoLabel.DataBindings.Add(new Binding("Text", _progressFormViewModel, "DeviceInfoMessage"));
            infoLabel.DataBindings.Add(Bindings.VisibleNullableBinding(_progressFormViewModel, "DeviceInfoMessage"));

            progressBar.DataBindings.Add(new Binding("Visible", _progressFormViewModel, "InProgress"));
            okButton.DataBindings.Add(new Binding("Enabled", _progressFormViewModel, "IsFinished"));
            cancelButton.DataBindings.Add(new Binding("Visible", _progressFormViewModel, "InProgress"));

            resultPictureBox.DataBindings.Add(Bindings.VisibleNullableBinding(_progressFormViewModel, "Icon"));
            resultPictureBox.DataBindings.Add(new Binding("Image", _progressFormViewModel, "Icon", true));
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            _progressFormViewModel.Cancel();
        }

        private void DownloadForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _progressFormViewModel.Dispose();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void DownloadForm_Shown(object sender, EventArgs e)
        {
            TopMost = true;
            Activate();
            BringToFront();
            TopMost = false;
        }
    }
}