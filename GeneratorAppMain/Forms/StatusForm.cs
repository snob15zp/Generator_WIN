using GeneratorAppMain.Utils;
using GeneratorAppMain.ViewModel;
using System;
using System.Windows.Forms;

namespace GeneratorAppMain.Forms
{
    public partial class StatusForm : Form
    {
        private readonly StatusFormViewModel _statusFormViewModel;

        public StatusForm()
        {
            InitializeComponent();

            _statusFormViewModel = new StatusFormViewModel(this);
            SetupBindings();

            _statusFormViewModel.ReadDeviceStatus();
        }

        private void SetupBindings()
        {
            infoPanel.DataBindings.Add(Bindings.NegativeVisibleBinding(_statusFormViewModel, "InProgress"));
            progressBar.DataBindings.Add(new Binding("Visible", _statusFormViewModel, "InProgress"));

            latestFirmwareVersionLabel.DataBindings.Add(new Binding("Text", _statusFormViewModel, "LatestVersionMessage"));

            deviceFirmwareVesrionLabel.DataBindings.Add(new Binding("Text", _statusFormViewModel, "DeviceVersionMessage"));
            deviceFirmwareVesrionLabel.DataBindings.Add(new Binding("Enabled", _statusFormViewModel, "IsDeviceConnected"));

            refreshButton.DataBindings.Add(Bindings.NegativeBinding("Enabled", _statusFormViewModel, "InProgress"));
            installButton.DataBindings.Add(Bindings.NegativeBinding("Enabled", _statusFormViewModel, "InProgress"));

            DataBindings.Add(new Binding("Text", _statusFormViewModel, "DeviceStatusMessage"));
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void installButton_Click(object sender, EventArgs e)
        {
            Dispose();
            new VersionUpdateForm(_statusFormViewModel.Version).ShowDialog();
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            _statusFormViewModel.ReadDeviceStatus();
        }

        private void StatusForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _statusFormViewModel.Dispose();
        }
    }
}