namespace Turn_Calibration
	{
	partial class BasicParamControl
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
			this.DataListView = new System.Windows.Forms.ListView();
			this.Run = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Direct = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Volt = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Accel = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.MaxSpeed = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.TTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.AEAv = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.MaxAV = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SStime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Stime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.CSIAng = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.CAng = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.MAngle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.ClearDataButton = new System.Windows.Forms.Button();
			this.SaveDataButton = new System.Windows.Forms.Button();
			this.OpGroupBox = new System.Windows.Forms.GroupBox();
			this.LSDCheckBox = new System.Windows.Forms.CheckBox();
			this.TTNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.NCNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label10 = new System.Windows.Forms.Label();
			this.StopButton = new System.Windows.Forms.Button();
			this.StartButton = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.MSNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.AccelNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.STextBox = new System.Windows.Forms.TextBox();
			this.OpGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TTNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.NCNumericUpDown)).BeginInit();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MSNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.AccelNumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// DataListView
			// 
			this.DataListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Run,
            this.Direct,
            this.Volt,
            this.Accel,
            this.MaxSpeed,
            this.TTime,
            this.AEAv,
            this.MaxAV,
            this.SStime,
            this.Stime,
            this.CSIAng,
            this.CAng,
            this.MAngle});
			this.DataListView.FullRowSelect = true;
			this.DataListView.GridLines = true;
			this.DataListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.DataListView.HideSelection = false;
			this.DataListView.Location = new System.Drawing.Point(230, 21);
			this.DataListView.Margin = new System.Windows.Forms.Padding(2);
			this.DataListView.MultiSelect = false;
			this.DataListView.Name = "DataListView";
			this.DataListView.Size = new System.Drawing.Size(1272, 371);
			this.DataListView.TabIndex = 80;
			this.DataListView.UseCompatibleStateImageBehavior = false;
			this.DataListView.View = System.Windows.Forms.View.Details;
			// 
			// Run
			// 
			this.Run.Text = "Run";
			this.Run.Width = 50;
			// 
			// Direct
			// 
			this.Direct.Text = "Direct";
			// 
			// Volt
			// 
			this.Volt.Text = "Volts";
			// 
			// Accel
			// 
			this.Accel.Text = "Accel";
			// 
			// MaxSpeed
			// 
			this.MaxSpeed.Text = "Speed";
			this.MaxSpeed.Width = 64;
			// 
			// TTime
			// 
			this.TTime.Text = "Turn time (ms)";
			this.TTime.Width = 120;
			// 
			// AEAv
			// 
			this.AEAv.Text = "Avg end AV (°/s)";
			this.AEAv.Width = 128;
			// 
			// MaxAV
			// 
			this.MaxAV.Text = "Max AV (°/s)";
			this.MaxAV.Width = 103;
			// 
			// SStime
			// 
			this.SStime.Text = "SS time (ms)";
			this.SStime.Width = 109;
			// 
			// Stime
			// 
			this.Stime.Text = "Stop time (ms)";
			this.Stime.Width = 118;
			// 
			// CSIAng
			// 
			this.CSIAng.Text = "Calc SI angle (°)";
			this.CSIAng.Width = 127;
			// 
			// CAng
			// 
			this.CAng.Text = "Calc angle (°)";
			this.CAng.Width = 105;
			// 
			// MAngle
			// 
			this.MAngle.Text = "Measure angle (°)";
			this.MAngle.Width = 139;
			// 
			// StatusTextBox
			// 
			this.StatusTextBox.Location = new System.Drawing.Point(230, 411);
			this.StatusTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.StatusTextBox.Size = new System.Drawing.Size(1272, 94);
			this.StatusTextBox.TabIndex = 79;
			// 
			// ClearDataButton
			// 
			this.ClearDataButton.Enabled = false;
			this.ClearDataButton.Location = new System.Drawing.Point(58, 472);
			this.ClearDataButton.Margin = new System.Windows.Forms.Padding(2);
			this.ClearDataButton.Name = "ClearDataButton";
			this.ClearDataButton.Size = new System.Drawing.Size(110, 26);
			this.ClearDataButton.TabIndex = 78;
			this.ClearDataButton.Text = "Clear Data";
			this.ClearDataButton.UseVisualStyleBackColor = true;
			this.ClearDataButton.Click += new System.EventHandler(this.ClearDataButton_Click);
			// 
			// SaveDataButton
			// 
			this.SaveDataButton.Location = new System.Drawing.Point(52, 411);
			this.SaveDataButton.Margin = new System.Windows.Forms.Padding(2);
			this.SaveDataButton.Name = "SaveDataButton";
			this.SaveDataButton.Size = new System.Drawing.Size(123, 26);
			this.SaveDataButton.TabIndex = 77;
			this.SaveDataButton.Text = "Save Data";
			this.SaveDataButton.UseVisualStyleBackColor = true;
			this.SaveDataButton.Click += new System.EventHandler(this.SaveDataButton_Click);
			// 
			// OpGroupBox
			// 
			this.OpGroupBox.Controls.Add(this.LSDCheckBox);
			this.OpGroupBox.Controls.Add(this.TTNumericUpDown);
			this.OpGroupBox.Controls.Add(this.label3);
			this.OpGroupBox.Controls.Add(this.NCNumericUpDown);
			this.OpGroupBox.Controls.Add(this.label10);
			this.OpGroupBox.Controls.Add(this.StopButton);
			this.OpGroupBox.Controls.Add(this.StartButton);
			this.OpGroupBox.Location = new System.Drawing.Point(5, 164);
			this.OpGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.OpGroupBox.Name = "OpGroupBox";
			this.OpGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.OpGroupBox.Size = new System.Drawing.Size(217, 157);
			this.OpGroupBox.TabIndex = 76;
			this.OpGroupBox.TabStop = false;
			this.OpGroupBox.Text = "Operate";
			// 
			// LSDCheckBox
			// 
			this.LSDCheckBox.AutoSize = true;
			this.LSDCheckBox.Location = new System.Drawing.Point(9, 86);
			this.LSDCheckBox.Name = "LSDCheckBox";
			this.LSDCheckBox.Size = new System.Drawing.Size(198, 24);
			this.LSDCheckBox.TabIndex = 74;
			this.LSDCheckBox.Text = "Get last sensor data set";
			this.LSDCheckBox.UseVisualStyleBackColor = true;
			// 
			// TTNumericUpDown
			// 
			this.TTNumericUpDown.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
			this.TTNumericUpDown.Location = new System.Drawing.Point(140, 22);
			this.TTNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.TTNumericUpDown.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
			this.TTNumericUpDown.Name = "TTNumericUpDown";
			this.TTNumericUpDown.Size = new System.Drawing.Size(62, 26);
			this.TTNumericUpDown.TabIndex = 73;
			this.TTNumericUpDown.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(14, 23);
			this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(127, 20);
			this.label3.TabIndex = 72;
			this.label3.Text = "Turn time (msec)";
			// 
			// NCNumericUpDown
			// 
			this.NCNumericUpDown.Location = new System.Drawing.Point(126, 54);
			this.NCNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.NCNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.NCNumericUpDown.Name = "NCNumericUpDown";
			this.NCNumericUpDown.Size = new System.Drawing.Size(48, 26);
			this.NCNumericUpDown.TabIndex = 35;
			this.NCNumericUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(43, 57);
			this.label10.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(83, 20);
			this.label10.TabIndex = 33;
			this.label10.Text = "No. Cycles";
			// 
			// StopButton
			// 
			this.StopButton.Enabled = false;
			this.StopButton.Location = new System.Drawing.Point(123, 116);
			this.StopButton.Margin = new System.Windows.Forms.Padding(2);
			this.StopButton.Name = "StopButton";
			this.StopButton.Size = new System.Drawing.Size(56, 26);
			this.StopButton.TabIndex = 22;
			this.StopButton.Text = "Stop";
			this.StopButton.UseVisualStyleBackColor = true;
			this.StopButton.Click += new System.EventHandler(this.StopButton_Click);
			// 
			// StartButton
			// 
			this.StartButton.Location = new System.Drawing.Point(38, 116);
			this.StartButton.Margin = new System.Windows.Forms.Padding(2);
			this.StartButton.Name = "StartButton";
			this.StartButton.Size = new System.Drawing.Size(56, 26);
			this.StartButton.TabIndex = 21;
			this.StartButton.Text = "Run";
			this.StartButton.UseVisualStyleBackColor = true;
			this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.MSNumericUpDown);
			this.groupBox2.Controls.Add(this.AccelNumericUpDown);
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Controls.Add(this.label6);
			this.groupBox2.Location = new System.Drawing.Point(5, 21);
			this.groupBox2.Margin = new System.Windows.Forms.Padding(2);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Padding = new System.Windows.Forms.Padding(2);
			this.groupBox2.Size = new System.Drawing.Size(217, 108);
			this.groupBox2.TabIndex = 75;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Motor Controller Param";
			// 
			// MSNumericUpDown
			// 
			this.MSNumericUpDown.Location = new System.Drawing.Point(146, 63);
			this.MSNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.MSNumericUpDown.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
			this.MSNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.MSNumericUpDown.Name = "MSNumericUpDown";
			this.MSNumericUpDown.Size = new System.Drawing.Size(46, 26);
			this.MSNumericUpDown.TabIndex = 75;
			this.MSNumericUpDown.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
			// 
			// AccelNumericUpDown
			// 
			this.AccelNumericUpDown.Location = new System.Drawing.Point(146, 25);
			this.AccelNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.AccelNumericUpDown.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
			this.AccelNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.AccelNumericUpDown.Name = "AccelNumericUpDown";
			this.AccelNumericUpDown.Size = new System.Drawing.Size(46, 26);
			this.AccelNumericUpDown.TabIndex = 74;
			this.AccelNumericUpDown.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(11, 65);
			this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(131, 20);
			this.label5.TabIndex = 35;
			this.label5.Text = "Max speed factor";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(11, 28);
			this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(93, 20);
			this.label6.TabIndex = 33;
			this.label6.Text = "Accel factor";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(47, 356);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(71, 20);
			this.label1.TabIndex = 81;
			this.label1.Text = "Samples";
			// 
			// STextBox
			// 
			this.STextBox.Location = new System.Drawing.Point(122, 355);
			this.STextBox.Name = "STextBox";
			this.STextBox.ReadOnly = true;
			this.STextBox.Size = new System.Drawing.Size(64, 26);
			this.STextBox.TabIndex = 82;
			this.STextBox.Text = "0";
			this.STextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// BasicParamControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.STextBox);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.DataListView);
			this.Controls.Add(this.StatusTextBox);
			this.Controls.Add(this.ClearDataButton);
			this.Controls.Add(this.SaveDataButton);
			this.Controls.Add(this.OpGroupBox);
			this.Controls.Add(this.groupBox2);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "BasicParamControl";
			this.Size = new System.Drawing.Size(1518, 537);
			this.OpGroupBox.ResumeLayout(false);
			this.OpGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.TTNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.NCNumericUpDown)).EndInit();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.MSNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.AccelNumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.ListView DataListView;
		private System.Windows.Forms.ColumnHeader Run;
		private System.Windows.Forms.ColumnHeader Volt;
		private System.Windows.Forms.ColumnHeader Accel;
		private System.Windows.Forms.ColumnHeader MaxSpeed;
		private System.Windows.Forms.ColumnHeader MaxAV;
		private System.Windows.Forms.ColumnHeader CAng;
		private System.Windows.Forms.ColumnHeader CSIAng;
		private System.Windows.Forms.ColumnHeader MAngle;
		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.Button ClearDataButton;
		private System.Windows.Forms.Button SaveDataButton;
		private System.Windows.Forms.GroupBox OpGroupBox;
		private System.Windows.Forms.NumericUpDown TTNumericUpDown;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NumericUpDown NCNumericUpDown;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Button StopButton;
		private System.Windows.Forms.Button StartButton;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.NumericUpDown MSNumericUpDown;
		private System.Windows.Forms.NumericUpDown AccelNumericUpDown;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.ColumnHeader AEAv;
		private System.Windows.Forms.ColumnHeader Direct;
		private System.Windows.Forms.CheckBox LSDCheckBox;
		private System.Windows.Forms.ColumnHeader SStime;
		private System.Windows.Forms.ColumnHeader Stime;
		private System.Windows.Forms.ColumnHeader TTime;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox STextBox;
		}
	}
