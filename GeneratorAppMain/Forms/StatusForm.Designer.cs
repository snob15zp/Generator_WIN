
namespace GeneratorAppMain.Forms
{
    partial class StatusForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StatusForm));
            this.okButton = new System.Windows.Forms.Button();
            this.latestFirmwareVersionLabel = new System.Windows.Forms.Label();
            this.deviceFirmwareVesrionLabel = new System.Windows.Forms.Label();
            this.installButton = new System.Windows.Forms.Button();
            this.refreshButton = new System.Windows.Forms.Button();
            this.infoPanel = new System.Windows.Forms.Panel();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.infoPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(459, 98);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 28);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "Cancel";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // latestFirmwareVersionLabel
            // 
            this.latestFirmwareVersionLabel.AutoSize = true;
            this.latestFirmwareVersionLabel.Location = new System.Drawing.Point(3, 0);
            this.latestFirmwareVersionLabel.Name = "latestFirmwareVersionLabel";
            this.latestFirmwareVersionLabel.Size = new System.Drawing.Size(198, 17);
            this.latestFirmwareVersionLabel.TabIndex = 1;
            this.latestFirmwareVersionLabel.Text = "Latest firmware version:  1.0.4";
            // 
            // deviceFirmwareVesrionLabel
            // 
            this.deviceFirmwareVesrionLabel.AutoSize = true;
            this.deviceFirmwareVesrionLabel.Location = new System.Drawing.Point(3, 29);
            this.deviceFirmwareVesrionLabel.Name = "deviceFirmwareVesrionLabel";
            this.deviceFirmwareVesrionLabel.Size = new System.Drawing.Size(198, 17);
            this.deviceFirmwareVesrionLabel.TabIndex = 5;
            this.deviceFirmwareVesrionLabel.Text = "Device firmware version: 1.0.0";
            // 
            // installButton
            // 
            this.installButton.Location = new System.Drawing.Point(97, 98);
            this.installButton.Name = "installButton";
            this.installButton.Size = new System.Drawing.Size(75, 28);
            this.installButton.TabIndex = 6;
            this.installButton.Text = "Flash";
            this.installButton.UseVisualStyleBackColor = true;
            this.installButton.Click += new System.EventHandler(this.installButton_Click);
            // 
            // refreshButton
            // 
            this.refreshButton.Location = new System.Drawing.Point(16, 98);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(75, 28);
            this.refreshButton.TabIndex = 7;
            this.refreshButton.Text = "Refresh";
            this.refreshButton.UseVisualStyleBackColor = true;
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // infoPanel
            // 
            this.infoPanel.Controls.Add(this.deviceFirmwareVesrionLabel);
            this.infoPanel.Controls.Add(this.latestFirmwareVersionLabel);
            this.infoPanel.Location = new System.Drawing.Point(16, 14);
            this.infoPanel.Name = "infoPanel";
            this.infoPanel.Size = new System.Drawing.Size(518, 56);
            this.infoPanel.TabIndex = 8;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(0, 0);
            this.progressBar.MarqueeAnimationSpeed = 10;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(552, 8);
            this.progressBar.Step = 30;
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 9;
            // 
            // StatusForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(551, 142);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.infoPanel);
            this.Controls.Add(this.refreshButton);
            this.Controls.Add(this.installButton);
            this.Controls.Add(this.okButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "StatusForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Device Status";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.StatusForm_FormClosed);
            this.infoPanel.ResumeLayout(false);
            this.infoPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label latestFirmwareVersionLabel;
        private System.Windows.Forms.Label deviceFirmwareVesrionLabel;
        private System.Windows.Forms.Button installButton;
        private System.Windows.Forms.Button refreshButton;
        private System.Windows.Forms.Panel infoPanel;
        private System.Windows.Forms.ProgressBar progressBar;
    }
}