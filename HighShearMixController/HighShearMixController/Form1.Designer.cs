namespace HighShearMixController
{
    partial class Form1
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
      this.tabControl1 = new System.Windows.Forms.TabControl();
      this.tabPage1 = new System.Windows.Forms.TabPage();
      this.label16 = new System.Windows.Forms.Label();
      this.label15 = new System.Windows.Forms.Label();
      this.batchTextBox = new System.Windows.Forms.TextBox();
      this.workOrderTextBox = new System.Windows.Forms.TextBox();
      this.label14 = new System.Windows.Forms.Label();
      this.label13 = new System.Windows.Forms.Label();
      this.label11 = new System.Windows.Forms.Label();
      this.label10 = new System.Windows.Forms.Label();
      this.groupBox3 = new System.Windows.Forms.GroupBox();
      this.alarmStatusLabel = new System.Windows.Forms.Label();
      this.pollButton = new System.Windows.Forms.Button();
      this.speedLabel = new System.Windows.Forms.Label();
      this.tempLabel = new System.Windows.Forms.Label();
      this.vfdStatusLabel = new System.Windows.Forms.Label();
      this.thermStatusLabel = new System.Windows.Forms.Label();
      this.label6 = new System.Windows.Forms.Label();
      this.label5 = new System.Windows.Forms.Label();
      this.groupBox2 = new System.Windows.Forms.GroupBox();
      this.label7 = new System.Windows.Forms.Label();
      this.label4 = new System.Windows.Forms.Label();
      this.label3 = new System.Windows.Forms.Label();
      this.autStartButton = new System.Windows.Forms.Button();
      this.autStopButton = new System.Windows.Forms.Button();
      this.tempTextBox = new System.Windows.Forms.TextBox();
      this.groupBox1 = new System.Windows.Forms.GroupBox();
      this.label12 = new System.Windows.Forms.Label();
      this.manStopButton = new System.Windows.Forms.Button();
      this.manStartButton = new System.Windows.Forms.Button();
      this.label2 = new System.Windows.Forms.Label();
      this.label1 = new System.Windows.Forms.Label();
      this.mixSpeedTextBox = new System.Windows.Forms.TextBox();
      this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
      this.tabControl1.SuspendLayout();
      this.tabPage1.SuspendLayout();
      this.groupBox3.SuspendLayout();
      this.groupBox2.SuspendLayout();
      this.groupBox1.SuspendLayout();
      this.SuspendLayout();
      // 
      // tabControl1
      // 
      this.tabControl1.Controls.Add(this.tabPage1);
      this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tabControl1.Location = new System.Drawing.Point(0, 0);
      this.tabControl1.Name = "tabControl1";
      this.tabControl1.SelectedIndex = 0;
      this.tabControl1.Size = new System.Drawing.Size(632, 340);
      this.tabControl1.TabIndex = 0;
      // 
      // tabPage1
      // 
      this.tabPage1.Controls.Add(this.label16);
      this.tabPage1.Controls.Add(this.label15);
      this.tabPage1.Controls.Add(this.batchTextBox);
      this.tabPage1.Controls.Add(this.workOrderTextBox);
      this.tabPage1.Controls.Add(this.label14);
      this.tabPage1.Controls.Add(this.label13);
      this.tabPage1.Controls.Add(this.label11);
      this.tabPage1.Controls.Add(this.label10);
      this.tabPage1.Controls.Add(this.groupBox3);
      this.tabPage1.Controls.Add(this.groupBox2);
      this.tabPage1.Controls.Add(this.groupBox1);
      this.tabPage1.Location = new System.Drawing.Point(4, 22);
      this.tabPage1.Name = "tabPage1";
      this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
      this.tabPage1.Size = new System.Drawing.Size(624, 314);
      this.tabPage1.TabIndex = 0;
      this.tabPage1.UseVisualStyleBackColor = true;
      // 
      // label16
      // 
      this.label16.AutoSize = true;
      this.label16.ForeColor = System.Drawing.Color.Red;
      this.label16.Location = new System.Drawing.Point(142, 213);
      this.label16.Name = "label16";
      this.label16.Size = new System.Drawing.Size(94, 13);
      this.label16.TabIndex = 10;
      this.label16.Text = "Batch (A, B, etc...)";
      // 
      // label15
      // 
      this.label15.AutoSize = true;
      this.label15.ForeColor = System.Drawing.Color.Red;
      this.label15.Location = new System.Drawing.Point(164, 187);
      this.label15.Name = "label15";
      this.label15.Size = new System.Drawing.Size(72, 13);
      this.label15.TabIndex = 9;
      this.label15.Text = "Work Order #";
      // 
      // batchTextBox
      // 
      this.batchTextBox.Location = new System.Drawing.Point(242, 210);
      this.batchTextBox.Name = "batchTextBox";
      this.batchTextBox.Size = new System.Drawing.Size(83, 20);
      this.batchTextBox.TabIndex = 8;
      this.batchTextBox.TextChanged += new System.EventHandler(this.batchTextBox_TextChanged);
      // 
      // workOrderTextBox
      // 
      this.workOrderTextBox.Location = new System.Drawing.Point(242, 184);
      this.workOrderTextBox.Name = "workOrderTextBox";
      this.workOrderTextBox.Size = new System.Drawing.Size(83, 20);
      this.workOrderTextBox.TabIndex = 7;
      this.workOrderTextBox.TextChanged += new System.EventHandler(this.workOrderTextBox_TextChanged);
      // 
      // label14
      // 
      this.label14.AutoSize = true;
      this.label14.Location = new System.Drawing.Point(345, 243);
      this.label14.Name = "label14";
      this.label14.Size = new System.Drawing.Size(0, 13);
      this.label14.TabIndex = 6;
      // 
      // label13
      // 
      this.label13.AutoSize = true;
      this.label13.ForeColor = System.Drawing.SystemColors.ControlText;
      this.label13.Location = new System.Drawing.Point(345, 226);
      this.label13.Name = "label13";
      this.label13.Size = new System.Drawing.Size(0, 13);
      this.label13.TabIndex = 5;
      // 
      // label11
      // 
      this.label11.AutoSize = true;
      this.label11.ForeColor = System.Drawing.Color.Red;
      this.label11.Location = new System.Drawing.Point(345, 213);
      this.label11.Name = "label11";
      this.label11.Size = new System.Drawing.Size(0, 13);
      this.label11.TabIndex = 4;
      // 
      // label10
      // 
      this.label10.AutoSize = true;
      this.label10.Location = new System.Drawing.Point(345, 192);
      this.label10.Name = "label10";
      this.label10.Size = new System.Drawing.Size(71, 13);
      this.label10.TabIndex = 3;
      this.label10.Text = "Alarm Level 0";
      // 
      // groupBox3
      // 
      this.groupBox3.Controls.Add(this.alarmStatusLabel);
      this.groupBox3.Controls.Add(this.pollButton);
      this.groupBox3.Controls.Add(this.speedLabel);
      this.groupBox3.Controls.Add(this.tempLabel);
      this.groupBox3.Controls.Add(this.vfdStatusLabel);
      this.groupBox3.Controls.Add(this.thermStatusLabel);
      this.groupBox3.Controls.Add(this.label6);
      this.groupBox3.Controls.Add(this.label5);
      this.groupBox3.Location = new System.Drawing.Point(339, 6);
      this.groupBox3.Name = "groupBox3";
      this.groupBox3.Size = new System.Drawing.Size(268, 170);
      this.groupBox3.TabIndex = 2;
      this.groupBox3.TabStop = false;
      this.groupBox3.Text = "System Status";
      // 
      // alarmStatusLabel
      // 
      this.alarmStatusLabel.AutoSize = true;
      this.alarmStatusLabel.ForeColor = System.Drawing.Color.Red;
      this.alarmStatusLabel.Location = new System.Drawing.Point(6, 102);
      this.alarmStatusLabel.Name = "alarmStatusLabel";
      this.alarmStatusLabel.Size = new System.Drawing.Size(102, 13);
      this.alarmStatusLabel.TabIndex = 7;
      this.alarmStatusLabel.Text = "Alarm Disconnected";
      // 
      // pollButton
      // 
      this.pollButton.Enabled = false;
      this.pollButton.Location = new System.Drawing.Point(178, 129);
      this.pollButton.Name = "pollButton";
      this.pollButton.Size = new System.Drawing.Size(83, 26);
      this.pollButton.TabIndex = 6;
      this.pollButton.Text = "Poll";
      this.pollButton.UseVisualStyleBackColor = true;
      this.pollButton.Visible = false;
      this.pollButton.Click += new System.EventHandler(this.pollButton_Click);
      // 
      // speedLabel
      // 
      this.speedLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.speedLabel.AutoSize = true;
      this.speedLabel.ForeColor = System.Drawing.Color.Red;
      this.speedLabel.Location = new System.Drawing.Point(201, 44);
      this.speedLabel.Name = "speedLabel";
      this.speedLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
      this.speedLabel.Size = new System.Drawing.Size(29, 13);
      this.speedLabel.TabIndex = 6;
      this.speedLabel.Text = "0 Hz";
      this.speedLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
      // 
      // tempLabel
      // 
      this.tempLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.tempLabel.AutoSize = true;
      this.tempLabel.ForeColor = System.Drawing.Color.Red;
      this.tempLabel.Location = new System.Drawing.Point(201, 22);
      this.tempLabel.Name = "tempLabel";
      this.tempLabel.Size = new System.Drawing.Size(53, 13);
      this.tempLabel.TabIndex = 5;
      this.tempLabel.Text = "Unknown";
      this.tempLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
      // 
      // vfdStatusLabel
      // 
      this.vfdStatusLabel.AutoSize = true;
      this.vfdStatusLabel.ForeColor = System.Drawing.Color.Red;
      this.vfdStatusLabel.Location = new System.Drawing.Point(6, 142);
      this.vfdStatusLabel.Name = "vfdStatusLabel";
      this.vfdStatusLabel.Size = new System.Drawing.Size(97, 13);
      this.vfdStatusLabel.TabIndex = 4;
      this.vfdStatusLabel.Text = "VFD Disconnected";
      // 
      // thermStatusLabel
      // 
      this.thermStatusLabel.AutoSize = true;
      this.thermStatusLabel.ForeColor = System.Drawing.Color.Red;
      this.thermStatusLabel.Location = new System.Drawing.Point(6, 122);
      this.thermStatusLabel.Name = "thermStatusLabel";
      this.thermStatusLabel.Size = new System.Drawing.Size(138, 13);
      this.thermStatusLabel.TabIndex = 3;
      this.thermStatusLabel.Text = "Thermometer Disconnected";
      // 
      // label6
      // 
      this.label6.AutoSize = true;
      this.label6.Location = new System.Drawing.Point(6, 44);
      this.label6.Name = "label6";
      this.label6.Size = new System.Drawing.Size(94, 13);
      this.label6.TabIndex = 1;
      this.label6.Text = "Current Mix Speed";
      // 
      // label5
      // 
      this.label5.AutoSize = true;
      this.label5.Location = new System.Drawing.Point(6, 22);
      this.label5.Name = "label5";
      this.label5.Size = new System.Drawing.Size(67, 13);
      this.label5.TabIndex = 0;
      this.label5.Text = "Temperature";
      // 
      // groupBox2
      // 
      this.groupBox2.Controls.Add(this.label7);
      this.groupBox2.Controls.Add(this.label4);
      this.groupBox2.Controls.Add(this.label3);
      this.groupBox2.Controls.Add(this.autStartButton);
      this.groupBox2.Controls.Add(this.autStopButton);
      this.groupBox2.Controls.Add(this.tempTextBox);
      this.groupBox2.Location = new System.Drawing.Point(8, 94);
      this.groupBox2.Name = "groupBox2";
      this.groupBox2.Size = new System.Drawing.Size(325, 82);
      this.groupBox2.TabIndex = 1;
      this.groupBox2.TabStop = false;
      this.groupBox2.Text = "Automatic";
      // 
      // label7
      // 
      this.label7.AutoSize = true;
      this.label7.Location = new System.Drawing.Point(9, 60);
      this.label7.Name = "label7";
      this.label7.Size = new System.Drawing.Size(0, 13);
      this.label7.TabIndex = 11;
      // 
      // label4
      // 
      this.label4.AutoSize = true;
      this.label4.Location = new System.Drawing.Point(196, 22);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(14, 13);
      this.label4.TabIndex = 5;
      this.label4.Text = "C";
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(6, 22);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(101, 13);
      this.label3.TabIndex = 4;
      this.label3.Text = "Target Temperature";
      // 
      // autStartButton
      // 
      this.autStartButton.Location = new System.Drawing.Point(234, 15);
      this.autStartButton.Name = "autStartButton";
      this.autStartButton.Size = new System.Drawing.Size(83, 26);
      this.autStartButton.TabIndex = 3;
      this.autStartButton.Text = "Start Mix";
      this.autStartButton.UseVisualStyleBackColor = true;
      this.autStartButton.Click += new System.EventHandler(this.autStartButton_Click);
      // 
      // autStopButton
      // 
      this.autStopButton.Enabled = false;
      this.autStopButton.Location = new System.Drawing.Point(234, 47);
      this.autStopButton.Name = "autStopButton";
      this.autStopButton.Size = new System.Drawing.Size(83, 26);
      this.autStopButton.TabIndex = 2;
      this.autStopButton.Text = "Stop Mix";
      this.autStopButton.UseVisualStyleBackColor = true;
      this.autStopButton.Click += new System.EventHandler(this.autStopButton_Click);
      // 
      // tempTextBox
      // 
      this.tempTextBox.Enabled = false;
      this.tempTextBox.Location = new System.Drawing.Point(113, 19);
      this.tempTextBox.Name = "tempTextBox";
      this.tempTextBox.Size = new System.Drawing.Size(77, 20);
      this.tempTextBox.TabIndex = 0;
      this.tempTextBox.TextChanged += new System.EventHandler(this.tempTextBox_TextChanged);
      // 
      // groupBox1
      // 
      this.groupBox1.Controls.Add(this.label12);
      this.groupBox1.Controls.Add(this.manStopButton);
      this.groupBox1.Controls.Add(this.manStartButton);
      this.groupBox1.Controls.Add(this.label2);
      this.groupBox1.Controls.Add(this.label1);
      this.groupBox1.Controls.Add(this.mixSpeedTextBox);
      this.groupBox1.Location = new System.Drawing.Point(8, 6);
      this.groupBox1.Name = "groupBox1";
      this.groupBox1.Size = new System.Drawing.Size(325, 82);
      this.groupBox1.TabIndex = 0;
      this.groupBox1.TabStop = false;
      this.groupBox1.Text = "Manual Control";
      // 
      // label12
      // 
      this.label12.AutoSize = true;
      this.label12.Location = new System.Drawing.Point(120, 54);
      this.label12.Name = "label12";
      this.label12.Size = new System.Drawing.Size(78, 13);
      this.label12.TabIndex = 5;
      this.label12.Text = "7.5Hz - 60.0Hz";
      // 
      // manStopButton
      // 
      this.manStopButton.Enabled = false;
      this.manStopButton.Location = new System.Drawing.Point(234, 47);
      this.manStopButton.Name = "manStopButton";
      this.manStopButton.Size = new System.Drawing.Size(83, 26);
      this.manStopButton.TabIndex = 3;
      this.manStopButton.Text = "Stop Mix";
      this.manStopButton.UseVisualStyleBackColor = true;
      this.manStopButton.Click += new System.EventHandler(this.manStopButton_Click);
      // 
      // manStartButton
      // 
      this.manStartButton.Location = new System.Drawing.Point(234, 15);
      this.manStartButton.Name = "manStartButton";
      this.manStartButton.Size = new System.Drawing.Size(83, 26);
      this.manStartButton.TabIndex = 2;
      this.manStartButton.Text = "Start Mix";
      this.manStartButton.UseVisualStyleBackColor = true;
      this.manStartButton.Click += new System.EventHandler(this.manStartButton_Click);
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(196, 22);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(20, 13);
      this.label2.TabIndex = 1;
      this.label2.Text = "Hz";
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(6, 22);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(57, 13);
      this.label1.TabIndex = 1;
      this.label1.Text = "Mix Speed";
      // 
      // mixSpeedTextBox
      // 
      this.mixSpeedTextBox.Location = new System.Drawing.Point(113, 19);
      this.mixSpeedTextBox.Name = "mixSpeedTextBox";
      this.mixSpeedTextBox.Size = new System.Drawing.Size(77, 20);
      this.mixSpeedTextBox.TabIndex = 0;
      this.mixSpeedTextBox.TextChanged += new System.EventHandler(this.mixSpeedTextBox_TextChanged);
      // 
      // Form1
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(632, 340);
      this.Controls.Add(this.tabControl1);
      this.Name = "Form1";
      this.Text = "Form1";
      this.tabControl1.ResumeLayout(false);
      this.tabPage1.ResumeLayout(false);
      this.tabPage1.PerformLayout();
      this.groupBox3.ResumeLayout(false);
      this.groupBox3.PerformLayout();
      this.groupBox2.ResumeLayout(false);
      this.groupBox2.PerformLayout();
      this.groupBox1.ResumeLayout(false);
      this.groupBox1.PerformLayout();
      this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button autStartButton;
        private System.Windows.Forms.Button autStopButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tempTextBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button manStopButton;
        private System.Windows.Forms.Button manStartButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox mixSpeedTextBox;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label vfdStatusLabel;
        private System.Windows.Forms.Label thermStatusLabel;
        private System.Windows.Forms.Label speedLabel;
        private System.Windows.Forms.Label tempLabel;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Button pollButton;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox batchTextBox;
        private System.Windows.Forms.TextBox workOrderTextBox;
    private System.Windows.Forms.Label alarmStatusLabel;
  }
}

