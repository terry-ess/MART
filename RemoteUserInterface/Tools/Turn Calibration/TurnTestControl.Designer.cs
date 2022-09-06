namespace Turn_Calibration
	{
	partial class TurnTestControl
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
			this.STextBox = new System.Windows.Forms.TextBox();
			this.DataListView = new System.Windows.Forms.ListView();
			this.Run = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Direct = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Volt = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Accel = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.MaxSpeed = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SOffset = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.RAng = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.CAng = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.MAngle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.OpGroupBox = new System.Windows.Forms.GroupBox();
			this.LSDCheckBox = new System.Windows.Forms.CheckBox();
			this.TANumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.StopButton = new System.Windows.Forms.Button();
			this.StartButton = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.NCNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.SDButton = new System.Windows.Forms.Button();
			this.CDButton = new System.Windows.Forms.Button();
			this.label6 = new System.Windows.Forms.Label();
			this.OpGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TANumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.NCNumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// STextBox
			// 
			this.STextBox.Location = new System.Drawing.Point(149, 288);
			this.STextBox.Name = "STextBox";
			this.STextBox.ReadOnly = true;
			this.STextBox.Size = new System.Drawing.Size(64, 26);
			this.STextBox.TabIndex = 89;
			this.STextBox.Text = "0";
			this.STextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// DataListView
			// 
			this.DataListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Run,
            this.Direct,
            this.Volt,
            this.Accel,
            this.MaxSpeed,
            this.SOffset,
            this.RAng,
            this.CAng,
            this.MAngle});
			this.DataListView.FullRowSelect = true;
			this.DataListView.GridLines = true;
			this.DataListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.DataListView.HideSelection = false;
			this.DataListView.Location = new System.Drawing.Point(281, 36);
			this.DataListView.Margin = new System.Windows.Forms.Padding(2);
			this.DataListView.MultiSelect = false;
			this.DataListView.Name = "DataListView";
			this.DataListView.Size = new System.Drawing.Size(829, 371);
			this.DataListView.TabIndex = 87;
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
			// SOffset
			// 
			this.SOffset.Text = "Stop Offset (°)";
			this.SOffset.Width = 120;
			// 
			// RAng
			// 
			this.RAng.Text = "Request angle (°)";
			this.RAng.Width = 140;
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
			this.StatusTextBox.Location = new System.Drawing.Point(281, 426);
			this.StatusTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.StatusTextBox.Size = new System.Drawing.Size(829, 94);
			this.StatusTextBox.TabIndex = 86;
			// 
			// OpGroupBox
			// 
			this.OpGroupBox.Controls.Add(this.LSDCheckBox);
			this.OpGroupBox.Controls.Add(this.TANumericUpDown);
			this.OpGroupBox.Controls.Add(this.label3);
			this.OpGroupBox.Controls.Add(this.NCNumericUpDown);
			this.OpGroupBox.Controls.Add(this.label2);
			this.OpGroupBox.Controls.Add(this.StopButton);
			this.OpGroupBox.Controls.Add(this.StartButton);
			this.OpGroupBox.Location = new System.Drawing.Point(28, 36);
			this.OpGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.OpGroupBox.Name = "OpGroupBox";
			this.OpGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.OpGroupBox.Size = new System.Drawing.Size(217, 181);
			this.OpGroupBox.TabIndex = 83;
			this.OpGroupBox.TabStop = false;
			this.OpGroupBox.Text = "Operate";
			// 
			// LSDCheckBox
			// 
			this.LSDCheckBox.AutoSize = true;
			this.LSDCheckBox.Location = new System.Drawing.Point(9, 102);
			this.LSDCheckBox.Name = "LSDCheckBox";
			this.LSDCheckBox.Size = new System.Drawing.Size(198, 24);
			this.LSDCheckBox.TabIndex = 74;
			this.LSDCheckBox.Text = "Get last sensor data set";
			this.LSDCheckBox.UseVisualStyleBackColor = true;
			// 
			// TANumericUpDown
			// 
			this.TANumericUpDown.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
			this.TANumericUpDown.Location = new System.Drawing.Point(142, 27);
			this.TANumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.TANumericUpDown.Maximum = new decimal(new int[] {
            25,
            0,
            0,
            0});
			this.TANumericUpDown.Name = "TANumericUpDown";
			this.TANumericUpDown.Size = new System.Drawing.Size(62, 26);
			this.TANumericUpDown.TabIndex = 73;
			this.TANumericUpDown.Value = new decimal(new int[] {
            25,
            0,
            0,
            0});
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(35, 27);
			this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(103, 20);
			this.label3.TabIndex = 72;
			this.label3.Text = "Turn angle (°)";
			// 
			// StopButton
			// 
			this.StopButton.Enabled = false;
			this.StopButton.Location = new System.Drawing.Point(123, 142);
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
			this.StartButton.Location = new System.Drawing.Point(38, 142);
			this.StartButton.Margin = new System.Windows.Forms.Padding(2);
			this.StartButton.Name = "StartButton";
			this.StartButton.Size = new System.Drawing.Size(56, 26);
			this.StartButton.TabIndex = 21;
			this.StartButton.Text = "Run";
			this.StartButton.UseVisualStyleBackColor = true;
			this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(49, 65);
			this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(83, 20);
			this.label2.TabIndex = 33;
			this.label2.Text = "No. Cycles";
			// 
			// NCNumericUpDown
			// 
			this.NCNumericUpDown.Location = new System.Drawing.Point(132, 62);
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
			// SDButton
			// 
			this.SDButton.Location = new System.Drawing.Point(75, 385);
			this.SDButton.Margin = new System.Windows.Forms.Padding(2);
			this.SDButton.Name = "SDButton";
			this.SDButton.Size = new System.Drawing.Size(123, 26);
			this.SDButton.TabIndex = 84;
			this.SDButton.Text = "Save Data";
			this.SDButton.UseVisualStyleBackColor = true;
			this.SDButton.Click += new System.EventHandler(this.SaveDataButton_Click);
			// 
			// CDButton
			// 
			this.CDButton.Enabled = false;
			this.CDButton.Location = new System.Drawing.Point(81, 485);
			this.CDButton.Margin = new System.Windows.Forms.Padding(2);
			this.CDButton.Name = "CDButton";
			this.CDButton.Size = new System.Drawing.Size(110, 26);
			this.CDButton.TabIndex = 85;
			this.CDButton.Text = "Clear Data";
			this.CDButton.UseVisualStyleBackColor = true;
			this.CDButton.Click += new System.EventHandler(this.ClearDataButton_Click);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(75, 291);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(71, 20);
			this.label6.TabIndex = 88;
			this.label6.Text = "Samples";
			// 
			// TurnTestControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.STextBox);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.DataListView);
			this.Controls.Add(this.StatusTextBox);
			this.Controls.Add(this.CDButton);
			this.Controls.Add(this.SDButton);
			this.Controls.Add(this.OpGroupBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "TurnTestControl";
			this.Size = new System.Drawing.Size(1141, 556);
			this.OpGroupBox.ResumeLayout(false);
			this.OpGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.TANumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.NCNumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.TextBox STextBox;
		private System.Windows.Forms.ListView DataListView;
		private System.Windows.Forms.ColumnHeader Run;
		private System.Windows.Forms.ColumnHeader Direct;
		private System.Windows.Forms.ColumnHeader Volt;
		private System.Windows.Forms.ColumnHeader Accel;
		private System.Windows.Forms.ColumnHeader MaxSpeed;
		private System.Windows.Forms.ColumnHeader SOffset;
		private System.Windows.Forms.ColumnHeader RAng;
		private System.Windows.Forms.ColumnHeader CAng;
		private System.Windows.Forms.ColumnHeader MAngle;
		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.GroupBox OpGroupBox;
		private System.Windows.Forms.CheckBox LSDCheckBox;
		private System.Windows.Forms.NumericUpDown TANumericUpDown;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NumericUpDown NCNumericUpDown;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button StopButton;
		private System.Windows.Forms.Button StartButton;
		private System.Windows.Forms.Button SDButton;
		private System.Windows.Forms.Button CDButton;
		private System.Windows.Forms.Label label6;
		}
	}
