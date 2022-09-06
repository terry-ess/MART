namespace Linear_Motion_Calibration
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
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.MSNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.AccelNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.OpGroupBox = new System.Windows.Forms.GroupBox();
			this.WACheckBox = new System.Windows.Forms.CheckBox();
			this.MTNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.NCNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label10 = new System.Windows.Forms.Label();
			this.StopButton = new System.Windows.Forms.Button();
			this.StartButton = new System.Windows.Forms.Button();
			this.SaveDataButton = new System.Windows.Forms.Button();
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.DataListView = new System.Windows.Forms.ListView();
			this.Run = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Volt = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.CDist = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.TT = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.AT = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.DT = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.TopSpd = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Encoder = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.PPCheckBox = new System.Windows.Forms.CheckBox();
			this.DGNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.IGNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.PGNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label14 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.label15 = new System.Windows.Forms.Label();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MSNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.AccelNumericUpDown)).BeginInit();
			this.OpGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MTNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.NCNumericUpDown)).BeginInit();
			this.groupBox5.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.DGNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.IGNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.PGNumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.MSNumericUpDown);
			this.groupBox2.Controls.Add(this.AccelNumericUpDown);
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Controls.Add(this.label6);
			this.groupBox2.Location = new System.Drawing.Point(21, 21);
			this.groupBox2.Margin = new System.Windows.Forms.Padding(2);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Padding = new System.Windows.Forms.Padding(2);
			this.groupBox2.Size = new System.Drawing.Size(236, 108);
			this.groupBox2.TabIndex = 69;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Motor Controller Param";
			// 
			// MSNumericUpDown
			// 
			this.MSNumericUpDown.Location = new System.Drawing.Point(156, 63);
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
			this.MSNumericUpDown.Size = new System.Drawing.Size(50, 26);
			this.MSNumericUpDown.TabIndex = 75;
			this.MSNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.MSNumericUpDown.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
			// 
			// AccelNumericUpDown
			// 
			this.AccelNumericUpDown.Location = new System.Drawing.Point(157, 25);
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
			this.AccelNumericUpDown.Size = new System.Drawing.Size(50, 26);
			this.AccelNumericUpDown.TabIndex = 74;
			this.AccelNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.AccelNumericUpDown.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(9, 65);
			this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(131, 20);
			this.label5.TabIndex = 35;
			this.label5.Text = "Max speed factor";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(9, 28);
			this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(142, 20);
			this.label6.TabIndex = 33;
			this.label6.Text = "Acceleration factor";
			// 
			// OpGroupBox
			// 
			this.OpGroupBox.Controls.Add(this.WACheckBox);
			this.OpGroupBox.Controls.Add(this.MTNumericUpDown);
			this.OpGroupBox.Controls.Add(this.label3);
			this.OpGroupBox.Controls.Add(this.NCNumericUpDown);
			this.OpGroupBox.Controls.Add(this.label10);
			this.OpGroupBox.Controls.Add(this.StopButton);
			this.OpGroupBox.Controls.Add(this.StartButton);
			this.OpGroupBox.Location = new System.Drawing.Point(21, 304);
			this.OpGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.OpGroupBox.Name = "OpGroupBox";
			this.OpGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.OpGroupBox.Size = new System.Drawing.Size(236, 149);
			this.OpGroupBox.TabIndex = 70;
			this.OpGroupBox.TabStop = false;
			this.OpGroupBox.Text = "Operate";
			// 
			// WACheckBox
			// 
			this.WACheckBox.AutoSize = true;
			this.WACheckBox.Checked = true;
			this.WACheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.WACheckBox.Location = new System.Drawing.Point(63, 51);
			this.WACheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.WACheckBox.Name = "WACheckBox";
			this.WACheckBox.Size = new System.Drawing.Size(110, 24);
			this.WACheckBox.TabIndex = 75;
			this.WACheckBox.Text = "Wheel align";
			this.WACheckBox.UseVisualStyleBackColor = true;
			// 
			// MTNumericUpDown
			// 
			this.MTNumericUpDown.DecimalPlaces = 1;
			this.MTNumericUpDown.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
			this.MTNumericUpDown.Location = new System.Drawing.Point(149, 22);
			this.MTNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.MTNumericUpDown.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
			this.MTNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.MTNumericUpDown.Name = "MTNumericUpDown";
			this.MTNumericUpDown.Size = new System.Drawing.Size(62, 26);
			this.MTNumericUpDown.TabIndex = 73;
			this.MTNumericUpDown.Value = new decimal(new int[] {
            7,
            0,
            0,
            0});
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(26, 23);
			this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(120, 20);
			this.label3.TabIndex = 72;
			this.label3.Text = "Move time (sec)";
			// 
			// NCNumericUpDown
			// 
			this.NCNumericUpDown.Location = new System.Drawing.Point(136, 80);
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
			this.label10.Location = new System.Drawing.Point(53, 83);
			this.label10.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(83, 20);
			this.label10.TabIndex = 33;
			this.label10.Text = "No. Cycles";
			// 
			// StopButton
			// 
			this.StopButton.Enabled = false;
			this.StopButton.Location = new System.Drawing.Point(133, 112);
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
			this.StartButton.Location = new System.Drawing.Point(48, 111);
			this.StartButton.Margin = new System.Windows.Forms.Padding(2);
			this.StartButton.Name = "StartButton";
			this.StartButton.Size = new System.Drawing.Size(56, 26);
			this.StartButton.TabIndex = 21;
			this.StartButton.Text = "Run";
			this.StartButton.UseVisualStyleBackColor = true;
			this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
			// 
			// SaveDataButton
			// 
			this.SaveDataButton.Location = new System.Drawing.Point(78, 465);
			this.SaveDataButton.Margin = new System.Windows.Forms.Padding(2);
			this.SaveDataButton.Name = "SaveDataButton";
			this.SaveDataButton.Size = new System.Drawing.Size(123, 26);
			this.SaveDataButton.TabIndex = 71;
			this.SaveDataButton.Text = "Save Data";
			this.SaveDataButton.UseVisualStyleBackColor = true;
			this.SaveDataButton.Click += new System.EventHandler(this.SaveDataButton_Click);
			// 
			// StatusTextBox
			// 
			this.StatusTextBox.Location = new System.Drawing.Point(291, 397);
			this.StatusTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.StatusTextBox.Size = new System.Drawing.Size(1167, 94);
			this.StatusTextBox.TabIndex = 73;
			// 
			// DataListView
			// 
			this.DataListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Run,
            this.Volt,
            this.CDist,
            this.TT,
            this.AT,
            this.DT,
            this.TopSpd,
            this.Encoder});
			this.DataListView.FullRowSelect = true;
			this.DataListView.GridLines = true;
			this.DataListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.DataListView.HideSelection = false;
			this.DataListView.Location = new System.Drawing.Point(291, 21);
			this.DataListView.Margin = new System.Windows.Forms.Padding(2);
			this.DataListView.MultiSelect = false;
			this.DataListView.Name = "DataListView";
			this.DataListView.Size = new System.Drawing.Size(1167, 371);
			this.DataListView.TabIndex = 74;
			this.DataListView.UseCompatibleStateImageBehavior = false;
			this.DataListView.View = System.Windows.Forms.View.Details;
			// 
			// Run
			// 
			this.Run.Text = "Run";
			this.Run.Width = 50;
			// 
			// Volt
			// 
			this.Volt.Text = "Volts";
			// 
			// CDist
			// 
			this.CDist.Text = "Move Dist. (in)";
			this.CDist.Width = 114;
			// 
			// TT
			// 
			this.TT.Text = "Total time (ms)";
			this.TT.Width = 120;
			// 
			// AT
			// 
			this.AT.Text = "Accel time (ms)";
			this.AT.Width = 120;
			// 
			// DT
			// 
			this.DT.Text = "Decel time (ms)";
			this.DT.Width = 125;
			// 
			// TopSpd
			// 
			this.TopSpd.Text = "Top speed (f/s)";
			this.TopSpd.Width = 120;
			// 
			// Encoder
			// 
			this.Encoder.Text = "Encoder";
			this.Encoder.Width = 80;
			// 
			// groupBox5
			// 
			this.groupBox5.Controls.Add(this.PPCheckBox);
			this.groupBox5.Controls.Add(this.DGNumericUpDown);
			this.groupBox5.Controls.Add(this.IGNumericUpDown);
			this.groupBox5.Controls.Add(this.PGNumericUpDown);
			this.groupBox5.Controls.Add(this.label14);
			this.groupBox5.Controls.Add(this.label12);
			this.groupBox5.Controls.Add(this.label15);
			this.groupBox5.Location = new System.Drawing.Point(21, 141);
			this.groupBox5.Margin = new System.Windows.Forms.Padding(2);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Padding = new System.Windows.Forms.Padding(2);
			this.groupBox5.Size = new System.Drawing.Size(236, 151);
			this.groupBox5.TabIndex = 75;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Linear drift correct PID param";
			// 
			// PPCheckBox
			// 
			this.PPCheckBox.AutoSize = true;
			this.PPCheckBox.Checked = true;
			this.PPCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.PPCheckBox.Location = new System.Drawing.Point(52, 118);
			this.PPCheckBox.Name = "PPCheckBox";
			this.PPCheckBox.Size = new System.Drawing.Size(133, 24);
			this.PPCheckBox.TabIndex = 64;
			this.PPCheckBox.Text = "Set PID param";
			this.PPCheckBox.UseVisualStyleBackColor = true;
			// 
			// DGNumericUpDown
			// 
			this.DGNumericUpDown.Location = new System.Drawing.Point(152, 86);
			this.DGNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.DGNumericUpDown.Name = "DGNumericUpDown";
			this.DGNumericUpDown.Size = new System.Drawing.Size(62, 26);
			this.DGNumericUpDown.TabIndex = 63;
			this.DGNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.DGNumericUpDown.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
			// 
			// IGNumericUpDown
			// 
			this.IGNumericUpDown.DecimalPlaces = 2;
			this.IGNumericUpDown.Location = new System.Drawing.Point(152, 54);
			this.IGNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.IGNumericUpDown.Name = "IGNumericUpDown";
			this.IGNumericUpDown.Size = new System.Drawing.Size(62, 26);
			this.IGNumericUpDown.TabIndex = 62;
			this.IGNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.IGNumericUpDown.Value = new decimal(new int[] {
            2,
            0,
            0,
            65536});
			// 
			// PGNumericUpDown
			// 
			this.PGNumericUpDown.Location = new System.Drawing.Point(152, 25);
			this.PGNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.PGNumericUpDown.Name = "PGNumericUpDown";
			this.PGNumericUpDown.Size = new System.Drawing.Size(62, 26);
			this.PGNumericUpDown.TabIndex = 61;
			this.PGNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.PGNumericUpDown.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(23, 89);
			this.label14.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(120, 20);
			this.label14.TabIndex = 59;
			this.label14.Text = "Differential gain";
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(23, 56);
			this.label12.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(97, 20);
			this.label12.TabIndex = 57;
			this.label12.Text = "Integral gain";
			// 
			// label15
			// 
			this.label15.AutoSize = true;
			this.label15.Location = new System.Drawing.Point(23, 27);
			this.label15.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(128, 20);
			this.label15.TabIndex = 55;
			this.label15.Text = "Proportional gain";
			// 
			// BasicParamControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.groupBox5);
			this.Controls.Add(this.DataListView);
			this.Controls.Add(this.StatusTextBox);
			this.Controls.Add(this.SaveDataButton);
			this.Controls.Add(this.OpGroupBox);
			this.Controls.Add(this.groupBox2);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "BasicParamControl";
			this.Size = new System.Drawing.Size(1488, 509);
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.MSNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.AccelNumericUpDown)).EndInit();
			this.OpGroupBox.ResumeLayout(false);
			this.OpGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.MTNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.NCNumericUpDown)).EndInit();
			this.groupBox5.ResumeLayout(false);
			this.groupBox5.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.DGNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.IGNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.PGNumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.NumericUpDown MSNumericUpDown;
		private System.Windows.Forms.NumericUpDown AccelNumericUpDown;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.GroupBox OpGroupBox;
		private System.Windows.Forms.CheckBox WACheckBox;
		private System.Windows.Forms.NumericUpDown MTNumericUpDown;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NumericUpDown NCNumericUpDown;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Button StopButton;
		private System.Windows.Forms.Button StartButton;
		private System.Windows.Forms.Button SaveDataButton;
		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.ListView DataListView;
		private System.Windows.Forms.ColumnHeader Run;
		private System.Windows.Forms.ColumnHeader Volt;
		private System.Windows.Forms.ColumnHeader CDist;
		private System.Windows.Forms.ColumnHeader TT;
		private System.Windows.Forms.ColumnHeader AT;
		private System.Windows.Forms.ColumnHeader DT;
		private System.Windows.Forms.ColumnHeader TopSpd;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.CheckBox PPCheckBox;
		private System.Windows.Forms.NumericUpDown DGNumericUpDown;
		private System.Windows.Forms.NumericUpDown IGNumericUpDown;
		private System.Windows.Forms.NumericUpDown PGNumericUpDown;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.ColumnHeader Encoder;
		}
	}
