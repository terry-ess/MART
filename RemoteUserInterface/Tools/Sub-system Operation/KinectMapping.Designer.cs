namespace Sub_system_Operation
	{
	partial class KinectMapping
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
			{
			System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
			this.SaveButton = new System.Windows.Forms.Button();
			this.MapChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.MRadioButton = new System.Windows.Forms.RadioButton();
			this.DMRadioButton = new System.Windows.Forms.RadioButton();
			this.DepthPictureBox = new System.Windows.Forms.PictureBox();
			this.SDButton = new System.Windows.Forms.Button();
			this.KPGroupBox = new System.Windows.Forms.GroupBox();
			this.PanNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.TiltNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label10 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.SPButton = new System.Windows.Forms.Button();
			this.ODGroupBox = new System.Windows.Forms.GroupBox();
			this.HLNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label2 = new System.Windows.Forms.Label();
			this.HCNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.CDCheckBox = new System.Windows.Forms.CheckBox();
			this.ShowNYCheckBox = new System.Windows.Forms.CheckBox();
			this.FCNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.FLNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.DOButton = new System.Windows.Forms.Button();
			this.OATextBox = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this.MapChart)).BeginInit();
			this.groupBox3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.DepthPictureBox)).BeginInit();
			this.KPGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.PanNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TiltNumericUpDown)).BeginInit();
			this.ODGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.HLNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.HCNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.FCNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.FLNumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// SaveButton
			// 
			this.SaveButton.AutoSize = true;
			this.SaveButton.Enabled = false;
			this.SaveButton.Location = new System.Drawing.Point(93, 559);
			this.SaveButton.Margin = new System.Windows.Forms.Padding(2);
			this.SaveButton.Name = "SaveButton";
			this.SaveButton.Size = new System.Drawing.Size(110, 30);
			this.SaveButton.TabIndex = 87;
			this.SaveButton.Text = "Save data";
			this.SaveButton.UseVisualStyleBackColor = true;
			this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
			// 
			// MapChart
			// 
			chartArea1.BackColor = System.Drawing.SystemColors.Control;
			chartArea1.Name = "ChartArea1";
			this.MapChart.ChartAreas.Add(chartArea1);
			this.MapChart.Location = new System.Drawing.Point(97, 13);
			this.MapChart.Name = "MapChart";
			this.MapChart.Size = new System.Drawing.Size(640, 480);
			this.MapChart.TabIndex = 86;
			this.MapChart.Text = "chart1";
			this.MapChart.Visible = false;
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.MRadioButton);
			this.groupBox3.Controls.Add(this.DMRadioButton);
			this.groupBox3.Location = new System.Drawing.Point(312, 507);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(203, 45);
			this.groupBox3.TabIndex = 85;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Visible Image";
			// 
			// MRadioButton
			// 
			this.MRadioButton.AutoSize = true;
			this.MRadioButton.Location = new System.Drawing.Point(129, 19);
			this.MRadioButton.Name = "MRadioButton";
			this.MRadioButton.Size = new System.Drawing.Size(58, 24);
			this.MRadioButton.TabIndex = 2;
			this.MRadioButton.Text = "Map";
			this.MRadioButton.UseVisualStyleBackColor = true;
			this.MRadioButton.CheckedChanged += new System.EventHandler(this.RadioButton_CheckedChanged);
			// 
			// DMRadioButton
			// 
			this.DMRadioButton.AutoSize = true;
			this.DMRadioButton.Checked = true;
			this.DMRadioButton.Location = new System.Drawing.Point(20, 18);
			this.DMRadioButton.Name = "DMRadioButton";
			this.DMRadioButton.Size = new System.Drawing.Size(71, 24);
			this.DMRadioButton.TabIndex = 0;
			this.DMRadioButton.TabStop = true;
			this.DMRadioButton.Text = "Depth";
			this.DMRadioButton.UseVisualStyleBackColor = true;
			this.DMRadioButton.CheckedChanged += new System.EventHandler(this.RadioButton_CheckedChanged);
			// 
			// DepthPictureBox
			// 
			this.DepthPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.DepthPictureBox.Location = new System.Drawing.Point(97, 13);
			this.DepthPictureBox.Margin = new System.Windows.Forms.Padding(2);
			this.DepthPictureBox.Name = "DepthPictureBox";
			this.DepthPictureBox.Size = new System.Drawing.Size(640, 480);
			this.DepthPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.DepthPictureBox.TabIndex = 84;
			this.DepthPictureBox.TabStop = false;
			// 
			// SDButton
			// 
			this.SDButton.AutoSize = true;
			this.SDButton.Enabled = false;
			this.SDButton.Location = new System.Drawing.Point(91, 507);
			this.SDButton.Margin = new System.Windows.Forms.Padding(2);
			this.SDButton.Name = "SDButton";
			this.SDButton.Size = new System.Drawing.Size(110, 30);
			this.SDButton.TabIndex = 83;
			this.SDButton.Text = "Depth data";
			this.SDButton.UseVisualStyleBackColor = true;
			this.SDButton.Click += new System.EventHandler(this.SDButton_Click);
			// 
			// KPGroupBox
			// 
			this.KPGroupBox.Controls.Add(this.PanNumericUpDown);
			this.KPGroupBox.Controls.Add(this.TiltNumericUpDown);
			this.KPGroupBox.Controls.Add(this.label10);
			this.KPGroupBox.Controls.Add(this.label11);
			this.KPGroupBox.Controls.Add(this.SPButton);
			this.KPGroupBox.Location = new System.Drawing.Point(48, 599);
			this.KPGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.KPGroupBox.Name = "KPGroupBox";
			this.KPGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.KPGroupBox.Size = new System.Drawing.Size(195, 145);
			this.KPGroupBox.TabIndex = 82;
			this.KPGroupBox.TabStop = false;
			this.KPGroupBox.Text = "Kinect Position";
			// 
			// PanNumericUpDown
			// 
			this.PanNumericUpDown.Location = new System.Drawing.Point(77, 28);
			this.PanNumericUpDown.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
			this.PanNumericUpDown.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            -2147483648});
			this.PanNumericUpDown.Name = "PanNumericUpDown";
			this.PanNumericUpDown.Size = new System.Drawing.Size(85, 26);
			this.PanNumericUpDown.TabIndex = 10;
			// 
			// TiltNumericUpDown
			// 
			this.TiltNumericUpDown.Location = new System.Drawing.Point(77, 64);
			this.TiltNumericUpDown.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
			this.TiltNumericUpDown.Minimum = new decimal(new int[] {
            60,
            0,
            0,
            -2147483648});
			this.TiltNumericUpDown.Name = "TiltNumericUpDown";
			this.TiltNumericUpDown.Size = new System.Drawing.Size(85, 26);
			this.TiltNumericUpDown.TabIndex = 9;
			// 
			// label10
			// 
			this.label10.Location = new System.Drawing.Point(32, 67);
			this.label10.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(41, 16);
			this.label10.TabIndex = 7;
			this.label10.Text = "Tilt (°)";
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point(32, 30);
			this.label11.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(47, 16);
			this.label11.TabIndex = 5;
			this.label11.Text = "Pan (°)";
			// 
			// SPButton
			// 
			this.SPButton.AutoSize = true;
			this.SPButton.Location = new System.Drawing.Point(60, 98);
			this.SPButton.Name = "SPButton";
			this.SPButton.Size = new System.Drawing.Size(75, 30);
			this.SPButton.TabIndex = 3;
			this.SPButton.Text = "Set";
			this.SPButton.UseVisualStyleBackColor = true;
			this.SPButton.Click += new System.EventHandler(this.SPButton_Click);
			// 
			// ODGroupBox
			// 
			this.ODGroupBox.Controls.Add(this.HLNumericUpDown);
			this.ODGroupBox.Controls.Add(this.label2);
			this.ODGroupBox.Controls.Add(this.HCNumericUpDown);
			this.ODGroupBox.Controls.Add(this.label1);
			this.ODGroupBox.Controls.Add(this.CDCheckBox);
			this.ODGroupBox.Controls.Add(this.ShowNYCheckBox);
			this.ODGroupBox.Controls.Add(this.FCNumericUpDown);
			this.ODGroupBox.Controls.Add(this.FLNumericUpDown);
			this.ODGroupBox.Controls.Add(this.label8);
			this.ODGroupBox.Controls.Add(this.label9);
			this.ODGroupBox.Controls.Add(this.DOButton);
			this.ODGroupBox.Enabled = false;
			this.ODGroupBox.Location = new System.Drawing.Point(582, 507);
			this.ODGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.ODGroupBox.Name = "ODGroupBox";
			this.ODGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.ODGroupBox.Size = new System.Drawing.Size(225, 237);
			this.ODGroupBox.TabIndex = 81;
			this.ODGroupBox.TabStop = false;
			this.ODGroupBox.Text = "Obstacle Map";
			// 
			// HLNumericUpDown
			// 
			this.HLNumericUpDown.DecimalPlaces = 1;
			this.HLNumericUpDown.Location = new System.Drawing.Point(162, 109);
			this.HLNumericUpDown.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
			this.HLNumericUpDown.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            -2147483648});
			this.HLNumericUpDown.Name = "HLNumericUpDown";
			this.HLNumericUpDown.Size = new System.Drawing.Size(55, 26);
			this.HLNumericUpDown.TabIndex = 19;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(8, 112);
			this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(149, 21);
			this.label2.TabIndex = 18;
			this.label2.Text = "Height limit (in)";
			// 
			// HCNumericUpDown
			// 
			this.HCNumericUpDown.DecimalPlaces = 1;
			this.HCNumericUpDown.Location = new System.Drawing.Point(162, 80);
			this.HCNumericUpDown.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
			this.HCNumericUpDown.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            -2147483648});
			this.HCNumericUpDown.Name = "HCNumericUpDown";
			this.HCNumericUpDown.Size = new System.Drawing.Size(55, 26);
			this.HCNumericUpDown.TabIndex = 17;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 83);
			this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(149, 21);
			this.label1.TabIndex = 16;
			this.label1.Text = "Height correct (in)";
			// 
			// CDCheckBox
			// 
			this.CDCheckBox.AutoSize = true;
			this.CDCheckBox.Checked = true;
			this.CDCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.CDCheckBox.Location = new System.Drawing.Point(39, 165);
			this.CDCheckBox.Name = "CDCheckBox";
			this.CDCheckBox.Size = new System.Drawing.Size(162, 24);
			this.CDCheckBox.TabIndex = 15;
			this.CDCheckBox.Text = "Corrected distance";
			this.CDCheckBox.UseVisualStyleBackColor = true;
			// 
			// ShowNYCheckBox
			// 
			this.ShowNYCheckBox.AutoSize = true;
			this.ShowNYCheckBox.Checked = true;
			this.ShowNYCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ShowNYCheckBox.Location = new System.Drawing.Point(39, 138);
			this.ShowNYCheckBox.Name = "ShowNYCheckBox";
			this.ShowNYCheckBox.Size = new System.Drawing.Size(147, 24);
			this.ShowNYCheckBox.TabIndex = 14;
			this.ShowNYCheckBox.Text = "Show negative Y";
			this.ShowNYCheckBox.UseVisualStyleBackColor = true;
			// 
			// FCNumericUpDown
			// 
			this.FCNumericUpDown.DecimalPlaces = 1;
			this.FCNumericUpDown.Location = new System.Drawing.Point(147, 51);
			this.FCNumericUpDown.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
			this.FCNumericUpDown.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            -2147483648});
			this.FCNumericUpDown.Name = "FCNumericUpDown";
			this.FCNumericUpDown.Size = new System.Drawing.Size(55, 26);
			this.FCNumericUpDown.TabIndex = 13;
			// 
			// FLNumericUpDown
			// 
			this.FLNumericUpDown.Location = new System.Drawing.Point(149, 22);
			this.FLNumericUpDown.Maximum = new decimal(new int[] {
            160,
            0,
            0,
            0});
			this.FLNumericUpDown.Minimum = new decimal(new int[] {
            40,
            0,
            0,
            0});
			this.FLNumericUpDown.Name = "FLNumericUpDown";
			this.FLNumericUpDown.Size = new System.Drawing.Size(55, 26);
			this.FLNumericUpDown.TabIndex = 12;
			this.FLNumericUpDown.Value = new decimal(new int[] {
            120,
            0,
            0,
            0});
			// 
			// label8
			// 
			this.label8.Location = new System.Drawing.Point(16, 54);
			this.label8.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(126, 21);
			this.label8.TabIndex = 8;
			this.label8.Text = "Tilt correction (°)";
			// 
			// label9
			// 
			this.label9.Location = new System.Drawing.Point(16, 24);
			this.label9.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(133, 16);
			this.label9.TabIndex = 6;
			this.label9.Text = "Forward limit (in)";
			// 
			// DOButton
			// 
			this.DOButton.AutoSize = true;
			this.DOButton.Location = new System.Drawing.Point(15, 192);
			this.DOButton.Margin = new System.Windows.Forms.Padding(2);
			this.DOButton.Name = "DOButton";
			this.DOButton.Size = new System.Drawing.Size(194, 30);
			this.DOButton.TabIndex = 4;
			this.DOButton.Text = "Create Obstacle Map";
			this.DOButton.UseVisualStyleBackColor = true;
			this.DOButton.Click += new System.EventHandler(this.DOButton_Click);
			// 
			// OATextBox
			// 
			this.OATextBox.BackColor = System.Drawing.Color.White;
			this.OATextBox.Location = new System.Drawing.Point(255, 561);
			this.OATextBox.Margin = new System.Windows.Forms.Padding(2);
			this.OATextBox.Multiline = true;
			this.OATextBox.Name = "OATextBox";
			this.OATextBox.ReadOnly = true;
			this.OATextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.OATextBox.Size = new System.Drawing.Size(316, 186);
			this.OATextBox.TabIndex = 80;
			// 
			// KinectMapping
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.SaveButton);
			this.Controls.Add(this.MapChart);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.DepthPictureBox);
			this.Controls.Add(this.SDButton);
			this.Controls.Add(this.KPGroupBox);
			this.Controls.Add(this.ODGroupBox);
			this.Controls.Add(this.OATextBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "KinectMapping";
			this.Size = new System.Drawing.Size(845, 763);
			((System.ComponentModel.ISupportInitialize)(this.MapChart)).EndInit();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.DepthPictureBox)).EndInit();
			this.KPGroupBox.ResumeLayout(false);
			this.KPGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.PanNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TiltNumericUpDown)).EndInit();
			this.ODGroupBox.ResumeLayout(false);
			this.ODGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.HLNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.HCNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.FCNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.FLNumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.Button SaveButton;
		private System.Windows.Forms.DataVisualization.Charting.Chart MapChart;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.RadioButton MRadioButton;
		private System.Windows.Forms.RadioButton DMRadioButton;
		private System.Windows.Forms.PictureBox DepthPictureBox;
		private System.Windows.Forms.Button SDButton;
		private System.Windows.Forms.GroupBox KPGroupBox;
		private System.Windows.Forms.NumericUpDown PanNumericUpDown;
		private System.Windows.Forms.NumericUpDown TiltNumericUpDown;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Button SPButton;
		private System.Windows.Forms.GroupBox ODGroupBox;
		private System.Windows.Forms.CheckBox CDCheckBox;
		private System.Windows.Forms.CheckBox ShowNYCheckBox;
		private System.Windows.Forms.NumericUpDown FCNumericUpDown;
		private System.Windows.Forms.NumericUpDown FLNumericUpDown;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Button DOButton;
		private System.Windows.Forms.TextBox OATextBox;
		private System.Windows.Forms.NumericUpDown HCNumericUpDown;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown HLNumericUpDown;
		private System.Windows.Forms.Label label2;
		}
	}
