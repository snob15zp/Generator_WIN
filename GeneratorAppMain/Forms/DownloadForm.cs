using GeneratorWindowsApp.Utils;
using GeneratorWindowsApp.ViewModel;
using System;
using System.Windows.Forms;

namespace GeneratorWindowsApp.Forms
{
    public partial class DownloadForm : Form
    {
        private readonly ProgressFormViewModel progressFormViewModel;

        public DownloadForm(string url) : base()
        {
            InitializeComponent();

            progressFormViewModel = new ProgressFormViewModel(this);
            SetupBindings();
            progressFormViewModel.DownloadPrograms(url);
        }

        private void SetupBindings()
        {
            actionLabel.DataBindings.Add(new Binding("Text", progressFormViewModel, "DeviceStatusMessage"));

            infoLabel.DataBindings.Add(new Binding("Text", progressFormViewModel, "DeviceInfoMessage"));
            infoLabel.DataBindings.Add(Bindings.VisibleNullableBinding(progressFormViewModel, "DeviceInfoMessage"));

            progressBar.DataBindings.Add(new Binding("Visible", progressFormViewModel, "InProgress"));
            okButton.DataBindings.Add(new Binding("Visible", progressFormViewModel, "IsFinished"));

            resultPictureBox.DataBindings.Add(Bindings.VisibleNullableBinding(progressFormViewModel, "Icon"));
            resultPictureBox.DataBindings.Add(new Binding("Image", progressFormViewModel, "Icon", true));

            cancelButton.DataBindings.Add(Bindings.NegativeVisibleBinding(progressFormViewModel, "IsFinished"));
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void DownloadForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            progressFormViewModel.Dispose();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Dispose();
        }
    }
}
