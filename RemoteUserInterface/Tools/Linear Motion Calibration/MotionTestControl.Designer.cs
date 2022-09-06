namespace Linear_Motion_Calibration
	{
	partial class MotionTestControl
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
			this.Walign = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Slow = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Volts = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.RDist = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ADist = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.ClearDataButton = new System.Windows.Forms.Button();
			this.SaveDataButton = new System.Windows.Forms.Button();
			this.OpGroupBox = new System.Windows.Forms.GroupBox();
			this.SlowCheckBox = new System.Windows.Forms.CheckBox();
			this.WACheckBox = new System.Windows.Forms.CheckBox();
			this.MDNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.NCNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label10 = new System.Windows.Forms.Label();
			this.StopButton = new System.Windows.Forms.Button();
			this.StartButton = new System.Windows.Forms.Button();
			this.OpGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MDNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.NCNumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// DataListView
			// 
			this.DataListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Run,
            this.Walign,
            this.Slow,
            this.Volts,
            this.RDist,
            this.ADist});
			this.DataListView.FullRowSelect = true;
			this.DataListView.GridLines = true;
			this.DataListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.DataListView.HideSelection = false;
			this.DataListView.Location = new System.Drawing.Point(295, 27);
			this.DataListView.Margin = new System.Windows.Forms.Padding(2);
			this.DataListView.MultiSelect = false;
			this.DataListView.Name = "DataListView";
			this.DataListView.Size = new System.Drawing.Size(569, 320);
			this.DataListView.TabIndex = 79;
			this.DataListView.UseCompatibleStateImageBehavior = false;
			this.DataListView.View = System.Windows.Forms.View.Details;
			// 
			// Run
			// 
			this.Run.Text = "Run";
			this.Run.Width = 50;
			// 
			// Walign
			// 
			this.Walign.Text = "Wheel Align";
			this.Walign.Width = 96;
			// 
			// Slow
			// 
			this.Slow.Text = "Slow";
			// 
			// Volts
			// 
			this.Volts.Text = "Volts";
			// 
			// RDist
			// 
			this.RDist.Text = "Req Move Dist (in)";
			this.RDist.Width = 142;
			// 
			// ADist
			// 
			this.ADist.Text = "Act Move Dist (int)";
			this.ADist.Width = 143;
			// 
			// StatusTextBox
			// 
			this.StatusTextBox.Location = new System.Drawing.Point(295, 359);
			this.StatusTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.StatusTextBox.Size = new System.Drawing.Size(569, 94);
			this.StatusTextBox.TabIndex = 78;
			// 
			// ClearDataButton
			// 
			this.ClearDataButton.Enabled = false;
			this.ClearDataButton.Location = new System.Drawing.Point(88, 383);
			this.ClearDataButton.Margin = new System.Windows.Forms.Padding(2);
			this.ClearDataButton.Name = "ClearDataButton";
			this.ClearDataButton.Size = new System.Drawing.Size(110, 26);
			this.ClearDataButton.TabIndex = 77;
			this.ClearDataButton.Text = "Clear Data";
			this.ClearDataButton.UseVisualStyleBackColor = true;
			this.ClearDataButton.Click += new System.EventHandler(this.ClearDataButton_Click);
			// 
			// SaveDataButton
			// 
			this.SaveDataButton.Location = new System.Drawing.Point(82, 319);
			this.SaveDataButton.Margin = new System.Windows.Forms.Padding(2);
			this.SaveDataButton.Name = "SaveDataButton";
			this.SaveDataButton.Size = new System.Drawing.Size(123, 26);
			this.SaveDataButton.TabIndex = 76;
			this.SaveDataButton.Text = "Save Data";
			this.SaveDataButton.UseVisualStyleBackColor = true;
			this.SaveDataButton.Click += new System.EventHandler(this.SaveDataButton_Click);
			// 
			// OpGroupBox
			// 
			this.OpGroupBox.Controls.Add(this.SlowCheckBox);
			this.OpGroupBox.Controls.Add(this.WACheckBox);
			this.OpGroupBox.Controls.Add(this.MDNumericUpDown);
			this.OpGroupBox.Controls.Add(this.label3);
			this.OpGroupBox.Controls.Add(this.NCNumericUpDown);
			this.OpGroupBox.Controls.Add(this.label10);
			this.OpGroupBox.Controls.Add(this.StopButton);
			this.OpGroupBox.Controls.Add(this.StartButton);
			this.OpGroupBox.Location = new System.Drawing.Point(25, 32);
			this.OpGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.OpGroupBox.Name = "OpGroupBox";
			this.OpGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.OpGroupBox.Size = new System.Drawing.Size(236, 213);
			this.OpGroupBox.TabIndex = 75;
			this.OpGroupBox.TabStop = false;
			this.OpGroupBox.Text = "Operate";
			// 
			// SlowCheckBox
			// 
			this.SlowCheckBox.AutoSize = true;
			this.SlowCheckBox.Location = new System.Drawing.Point(87, 97);
			this.SlowCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.SlowCheckBox.Name = "SlowCheckBox";
			this.SlowCheckBox.Size = new System.Drawing.Size(62, 24);
			this.SlowCheckBox.TabIndex = 76;
			this.SlowCheckBox.Text = "Slow";
			this.SlowCheckBox.UseVisualStyleBackColor = true;
			// 
			// WACheckBox
			// 
			this.WACheckBox.AutoSize = true;
			this.WACheckBox.Location = new System.Drawing.Point(63, 58);
			this.WACheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.WACheckBox.Name = "WACheckBox";
			this.WACheckBox.Size = new System.Drawing.Size(110, 24);
			this.WACheckBox.TabIndex = 75;
			this.WACheckBox.Text = "Wheel align";
			this.WACheckBox.UseVisualStyleBackColor = true;
			// 
			// MDNumericUpDown
			// 
			this.MDNumericUpDown.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
			this.MDNumericUpDown.Location = new System.Drawing.Point(140, 22);
			this.MDNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.MDNumericUpDown.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
			this.MDNumericUpDown.Minimum = new decimal(new int[] {
            24,
            0,
            0,
            0});
			this.MDNumericUpDown.Name = "MDNumericUpDown";
			this.MDNumericUpDown.Size = new System.Drawing.Size(63, 26);
			this.MDNumericUpDown.TabIndex = 73;
			this.MDNumericUpDown.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(34, 23);
			this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(102, 20);
			this.label3.TabIndex = 72;
			this.label3.Text = "Move dist (in)";
			// 
			// NCNumericUpDown
			// 
			this.NCNumericUpDown.Location = new System.Drawing.Point(136, 133);
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
			this.label10.Location = new System.Drawing.Point(53, 136);
			this.label10.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(83, 20);
			this.label10.TabIndex = 33;
			this.label10.Text = "No. Cycles";
			// 
			// StopButton
			// 
			this.StopButton.Enabled = false;
			this.StopButton.Location = new System.Drawing.Point(133, 172);
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
			this.StartButton.Location = new System.Drawing.Point(48, 171);
			this.StartButton.Margin = new System.Windows.Forms.Padding(2);
			this.StartButton.Name = "StartButton";
			this.StartButton.Size = new System.Drawing.Size(56, 26);
			this.StartButton.TabIndex = 21;
			this.StartButton.Text = "Run";
			this.StartButton.UseVisualStyleBackColor = true;
			this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
			// 
			// MotionTestControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.DataListView);
			this.Controls.Add(this.StatusTextBox);
			this.Controls.Add(this.ClearDataButton);
			this.Controls.Add(this.SaveDataButton);
			this.Controls.Add(this.OpGroupBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "MotionTestControl";
			this.Size = new System.Drawing.Size(906, 480);
			this.OpGroupBox.ResumeLayout(false);
			this.OpGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.MDNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.NCNumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.ListView DataListView;
		private System.Windows.Forms.ColumnHeader Run;
		private System.Windows.Forms.ColumnHeader Walign;
		private System.Windows.Forms.ColumnHeader Slow;
		private System.Windows.Forms.ColumnHeader Volts;
		private System.Windows.Forms.ColumnHeader RDist;
		private System.Windows.Forms.ColumnHeader ADist;
		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.Button ClearDataButton;
		private System.Windows.Forms.Button SaveDataButton;
		private System.Windows.Forms.GroupBox OpGroupBox;
		private System.Windows.Forms.CheckBox SlowCheckBox;
		private System.Windows.Forms.CheckBox WACheckBox;
		private System.Windows.Forms.NumericUpDown MDNumericUpDown;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NumericUpDown NCNumericUpDown;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Button StopButton;
		private System.Windows.Forms.Button StartButton;
		}
	}
