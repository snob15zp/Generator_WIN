using System;
using System.Windows.Forms;

namespace GeneratorAppMain.Forms
{
    public partial class InputForm : Form
    {
        private string _inputText;
        public string InputText
        {
            get { return _inputText; }
        }

        public string Message
        {
            get { return label.Text; }
            internal set
            {
                label.Text = value;
            }
        }
        public string Placeholder
        {
            get { return placeholderLabel.Text; }
            internal set
            {
                placeholderLabel.Text = value;
            }
        }

        public InputForm()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            _inputText = textBox.Text;
            Dispose();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Dispose();
        }
    }
}
