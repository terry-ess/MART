namespace Sub_system_Operation
	{
	partial class KinectTargetCal
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.MBANumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.ABFPButton = new System.Windows.Forms.Button();
			this.label17 = new System.Windows.Forms.Label();
			this.UDButton = new System.Windows.Forms.Button();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.label5 = new System.Windows.Forms.Label();
			this.LATextBox = new System.Windows.Forms.TextBox();
			this.BTNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.ITNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.LVButton = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.BPListView = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.label13 = new System.Windows.Forms.Label();
			this.MBATextBox = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.TWFTextBox = new System.Windows.Forms.TextBox();
			this.label9 = new System.Windows.Forms.Label();
			this.THFTextBox = new System.Windows.Forms.TextBox();
			this.ErrorTextBox = new System.Windows.Forms.TextBox();
			this.ShootButton = new System.Windows.Forms.Button();
			this.VideoPicBox = new System.Windows.Forms.PictureBox();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.DITextBox = new System.Windows.Forms.TextBox();
			this.label10 = new System.Windows.Forms.Label();
			this.RATextBox = new System.Windows.Forms.TextBox();
			this.label18 = new System.Windows.Forms.Label();
			this.ATextBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.HTextBox = new System.Windows.Forms.TextBox();
			this.label11 = new System.Windows.Forms.Label();
			this.RTextBox = new System.Windows.Forms.TextBox();
			this.label12 = new System.Windows.Forms.Label();
			this.CLTextBox = new System.Windows.Forms.TextBox();
			this.BluePicBox = new System.Windows.Forms.PictureBox();
			this.ProcButton = new System.Windows.Forms.Button();
			this.DIButton = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MBANumericUpDown)).BeginInit();
			this.groupBox3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.BTNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.ITNumericUpDown)).BeginInit();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.VideoPicBox)).BeginInit();
			this.groupBox4.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.BluePicBox)).BeginInit();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.MBANumericUpDown);
			this.groupBox1.Controls.Add(this.ABFPButton);
			this.groupBox1.Controls.Add(this.label17);
			this.groupBox1.Controls.Add(this.UDButton);
			this.groupBox1.Controls.Add(this.groupBox3);
			this.groupBox1.Controls.Add(this.LVButton);
			this.groupBox1.Location = new System.Drawing.Point(543, 420);
			this.groupBox1.Margin = new System.Windows.Forms.Padding(2);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Padding = new System.Windows.Forms.Padding(2);
			this.groupBox1.Size = new System.Drawing.Size(480, 160);
			this.groupBox1.TabIndex = 68;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Process Parameters";
			// 
			// MBANumericUpDown
			// 
			this.MBANumericUpDown.Location = new System.Drawing.Point(377, 26);
			this.MBANumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.MBANumericUpDown.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
			this.MBANumericUpDown.Minimum = new decimal(new int[] {
            2000,
            0,
            0,
            0});
			this.MBANumericUpDown.Name = "MBANumericUpDown";
			this.MBANumericUpDown.Size = new System.Drawing.Size(64, 26);
			this.MBANumericUpDown.TabIndex = 38;
			this.MBANumericUpDown.Value = new decimal(new int[] {
            2000,
            0,
            0,
            0});
			// 
			// ABFPButton
			// 
			this.ABFPButton.Location = new System.Drawing.Point(249, 88);
			this.ABFPButton.Margin = new System.Windows.Forms.Padding(2);
			this.ABFPButton.Name = "ABFPButton";
			this.ABFPButton.Size = new System.Drawing.Size(209, 25);
			this.ABFPButton.TabIndex = 61;
			this.ABFPButton.Text = "Add Blue Filter Parameters";
			this.ABFPButton.UseVisualStyleBackColor = true;
			this.ABFPButton.Click += new System.EventHandler(this.ABFPButton_Click);
			// 
			// label17
			// 
			this.label17.AutoSize = true;
			this.label17.Location = new System.Drawing.Point(261, 29);
			this.label17.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label17.Name = "label17";
			this.label17.Size = new System.Drawing.Size(112, 20);
			this.label17.TabIndex = 33;
			this.label17.Text = "Min. Blob Area";
			// 
			// UDButton
			// 
			this.UDButton.Location = new System.Drawing.Point(240, 120);
			this.UDButton.Margin = new System.Windows.Forms.Padding(2);
			this.UDButton.Name = "UDButton";
			this.UDButton.Size = new System.Drawing.Size(227, 25);
			this.UDButton.TabIndex = 60;
			this.UDButton.Text = "Save New Target Param File";
			this.UDButton.UseVisualStyleBackColor = true;
			this.UDButton.Click += new System.EventHandler(this.UDButton_Click);
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.label5);
			this.groupBox3.Controls.Add(this.LATextBox);
			this.groupBox3.Controls.Add(this.BTNumericUpDown);
			this.groupBox3.Controls.Add(this.ITNumericUpDown);
			this.groupBox3.Controls.Add(this.label1);
			this.groupBox3.Controls.Add(this.label4);
			this.groupBox3.Location = new System.Drawing.Point(10, 29);
			this.groupBox3.Margin = new System.Windows.Forms.Padding(2);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Padding = new System.Windows.Forms.Padding(2);
			this.groupBox3.Size = new System.Drawing.Size(220, 117);
			this.groupBox3.TabIndex = 58;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Blue Filter";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(7, 85);
			this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(117, 20);
			this.label5.TabIndex = 39;
			this.label5.Text = "Light amplitude";
			// 
			// LATextBox
			// 
			this.LATextBox.Location = new System.Drawing.Point(156, 82);
			this.LATextBox.Margin = new System.Windows.Forms.Padding(2);
			this.LATextBox.Name = "LATextBox";
			this.LATextBox.ReadOnly = true;
			this.LATextBox.Size = new System.Drawing.Size(57, 26);
			this.LATextBox.TabIndex = 38;
			// 
			// BTNumericUpDown
			// 
			this.BTNumericUpDown.Location = new System.Drawing.Point(156, 51);
			this.BTNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.BTNumericUpDown.Maximum = new decimal(new int[] {
            250,
            0,
            0,
            0});
			this.BTNumericUpDown.Name = "BTNumericUpDown";
			this.BTNumericUpDown.Size = new System.Drawing.Size(56, 26);
			this.BTNumericUpDown.TabIndex = 37;
			this.BTNumericUpDown.Value = new decimal(new int[] {
            120,
            0,
            0,
            0});
			// 
			// ITNumericUpDown
			// 
			this.ITNumericUpDown.Location = new System.Drawing.Point(156, 20);
			this.ITNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.ITNumericUpDown.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.ITNumericUpDown.Name = "ITNumericUpDown";
			this.ITNumericUpDown.Size = new System.Drawing.Size(56, 26);
			this.ITNumericUpDown.TabIndex = 36;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(7, 54);
			this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(149, 20);
			this.label1.TabIndex = 35;
			this.label1.Text = "Blueness Threshold";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(7, 22);
			this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(143, 20);
			this.label4.TabIndex = 33;
			this.label4.Text = "Intensity Threshold";
			// 
			// LVButton
			// 
			this.LVButton.Enabled = false;
			this.LVButton.Location = new System.Drawing.Point(260, 56);
			this.LVButton.Margin = new System.Windows.Forms.Padding(2);
			this.LVButton.Name = "LVButton";
			this.LVButton.Size = new System.Drawing.Size(187, 25);
			this.LVButton.TabIndex = 58;
			this.LVButton.Text = "Load Values from Table";
			this.LVButton.UseVisualStyleBackColor = true;
			this.LVButton.Click += new System.EventHandler(this.LVButton_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.DIButton);
			this.groupBox2.Controls.Add(this.BPListView);
			this.groupBox2.Controls.Add(this.label13);
			this.groupBox2.Controls.Add(this.MBATextBox);
			this.groupBox2.Controls.Add(this.label7);
			this.groupBox2.Controls.Add(this.TWFTextBox);
			this.groupBox2.Controls.Add(this.label9);
			this.groupBox2.Controls.Add(this.THFTextBox);
			this.groupBox2.Location = new System.Drawing.Point(24, 389);
			this.groupBox2.Margin = new System.Windows.Forms.Padding(2);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Padding = new System.Windows.Forms.Padding(2);
			this.groupBox2.Size = new System.Drawing.Size(480, 251);
			this.groupBox2.TabIndex = 67;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Target Parameter File";
			// 
			// BPListView
			// 
			this.BPListView.Activation = System.Windows.Forms.ItemActivation.TwoClick;
			this.BPListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
			this.BPListView.FullRowSelect = true;
			this.BPListView.GridLines = true;
			this.BPListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.BPListView.Location = new System.Drawing.Point(20, 27);
			this.BPListView.Margin = new System.Windows.Forms.Padding(2);
			this.BPListView.MultiSelect = false;
			this.BPListView.Name = "BPListView";
			this.BPListView.Size = new System.Drawing.Size(440, 150);
			this.BPListView.TabIndex = 65;
			this.BPListView.UseCompatibleStateImageBehavior = false;
			this.BPListView.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Light Amplitude";
			this.columnHeader1.Width = 128;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Intensity Threshold";
			this.columnHeader2.Width = 150;
			// 
			// columnHeader3
			// 
			this.columnHeader3.Text = "Blueness Threshold";
			this.columnHeader3.Width = 150;
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(35, 189);
			this.label13.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(108, 20);
			this.label13.TabIndex = 64;
			this.label13.Text = "Min. blob area";
			// 
			// MBATextBox
			// 
			this.MBATextBox.Location = new System.Drawing.Point(144, 186);
			this.MBATextBox.Margin = new System.Windows.Forms.Padding(2);
			this.MBATextBox.Name = "MBATextBox";
			this.MBATextBox.ReadOnly = true;
			this.MBATextBox.Size = new System.Drawing.Size(67, 26);
			this.MBATextBox.TabIndex = 63;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(229, 221);
			this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(141, 20);
			this.label7.TabIndex = 62;
			this.label7.Text = "Target width factor";
			// 
			// TWFTextBox
			// 
			this.TWFTextBox.Location = new System.Drawing.Point(378, 218);
			this.TWFTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.TWFTextBox.Name = "TWFTextBox";
			this.TWFTextBox.ReadOnly = true;
			this.TWFTextBox.Size = new System.Drawing.Size(67, 26);
			this.TWFTextBox.TabIndex = 61;
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(229, 189);
			this.label9.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(148, 20);
			this.label9.TabIndex = 60;
			this.label9.Text = "Target height factor";
			// 
			// THFTextBox
			// 
			this.THFTextBox.Location = new System.Drawing.Point(378, 186);
			this.THFTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.THFTextBox.Name = "THFTextBox";
			this.THFTextBox.ReadOnly = true;
			this.THFTextBox.Size = new System.Drawing.Size(67, 26);
			this.THFTextBox.TabIndex = 59;
			// 
			// ErrorTextBox
			// 
			this.ErrorTextBox.Location = new System.Drawing.Point(543, 594);
			this.ErrorTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.ErrorTextBox.Multiline = true;
			this.ErrorTextBox.Name = "ErrorTextBox";
			this.ErrorTextBox.ReadOnly = true;
			this.ErrorTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.ErrorTextBox.Size = new System.Drawing.Size(480, 147);
			this.ErrorTextBox.TabIndex = 66;
			// 
			// ShootButton
			// 
			this.ShootButton.Location = new System.Drawing.Point(630, 381);
			this.ShootButton.Margin = new System.Windows.Forms.Padding(2);
			this.ShootButton.Name = "ShootButton";
			this.ShootButton.Size = new System.Drawing.Size(83, 25);
			this.ShootButton.TabIndex = 63;
			this.ShootButton.Text = "Shoot";
			this.ShootButton.UseVisualStyleBackColor = true;
			this.ShootButton.Click += new System.EventHandler(this.ShootButton_Click);
			// 
			// VideoPicBox
			// 
			this.VideoPicBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.VideoPicBox.ErrorImage = null;
			this.VideoPicBox.Location = new System.Drawing.Point(543, 7);
			this.VideoPicBox.Name = "VideoPicBox";
			this.VideoPicBox.Size = new System.Drawing.Size(480, 360);
			this.VideoPicBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.VideoPicBox.TabIndex = 62;
			this.VideoPicBox.TabStop = false;
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.label3);
			this.groupBox4.Controls.Add(this.DITextBox);
			this.groupBox4.Controls.Add(this.label10);
			this.groupBox4.Controls.Add(this.RATextBox);
			this.groupBox4.Controls.Add(this.label18);
			this.groupBox4.Controls.Add(this.ATextBox);
			this.groupBox4.Controls.Add(this.label2);
			this.groupBox4.Controls.Add(this.HTextBox);
			this.groupBox4.Controls.Add(this.label11);
			this.groupBox4.Controls.Add(this.RTextBox);
			this.groupBox4.Controls.Add(this.label12);
			this.groupBox4.Controls.Add(this.CLTextBox);
			this.groupBox4.Location = new System.Drawing.Point(24, 655);
			this.groupBox4.Margin = new System.Windows.Forms.Padding(2);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Padding = new System.Windows.Forms.Padding(2);
			this.groupBox4.Size = new System.Drawing.Size(480, 83);
			this.groupBox4.TabIndex = 61;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Selected Blob";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(315, 54);
			this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(98, 20);
			this.label3.TabIndex = 45;
			this.label3.Text = "Distance (in)";
			// 
			// DITextBox
			// 
			this.DITextBox.Location = new System.Drawing.Point(414, 51);
			this.DITextBox.Margin = new System.Windows.Forms.Padding(2);
			this.DITextBox.Name = "DITextBox";
			this.DITextBox.ReadOnly = true;
			this.DITextBox.Size = new System.Drawing.Size(54, 26);
			this.DITextBox.TabIndex = 44;
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(313, 24);
			this.label10.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(97, 20);
			this.label10.TabIndex = 33;
			this.label10.Text = "Rel Angle (°)";
			// 
			// RATextBox
			// 
			this.RATextBox.Location = new System.Drawing.Point(414, 21);
			this.RATextBox.Margin = new System.Windows.Forms.Padding(2);
			this.RATextBox.Name = "RATextBox";
			this.RATextBox.ReadOnly = true;
			this.RATextBox.Size = new System.Drawing.Size(54, 26);
			this.RATextBox.TabIndex = 32;
			// 
			// label18
			// 
			this.label18.AutoSize = true;
			this.label18.Location = new System.Drawing.Point(15, 54);
			this.label18.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size(43, 20);
			this.label18.TabIndex = 43;
			this.label18.Text = "Area";
			// 
			// ATextBox
			// 
			this.ATextBox.Location = new System.Drawing.Point(105, 51);
			this.ATextBox.Margin = new System.Windows.Forms.Padding(2);
			this.ATextBox.Name = "ATextBox";
			this.ATextBox.ReadOnly = true;
			this.ATextBox.Size = new System.Drawing.Size(67, 26);
			this.ATextBox.TabIndex = 42;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(182, 54);
			this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(56, 20);
			this.label2.TabIndex = 41;
			this.label2.Text = "Height";
			// 
			// HTextBox
			// 
			this.HTextBox.Location = new System.Drawing.Point(240, 51);
			this.HTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.HTextBox.Name = "HTextBox";
			this.HTextBox.ReadOnly = true;
			this.HTextBox.Size = new System.Drawing.Size(67, 26);
			this.HTextBox.TabIndex = 40;
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(182, 24);
			this.label11.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(50, 20);
			this.label11.TabIndex = 35;
			this.label11.Text = "Width";
			// 
			// RTextBox
			// 
			this.RTextBox.Location = new System.Drawing.Point(240, 21);
			this.RTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.RTextBox.Name = "RTextBox";
			this.RTextBox.ReadOnly = true;
			this.RTextBox.Size = new System.Drawing.Size(67, 26);
			this.RTextBox.TabIndex = 34;
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(15, 24);
			this.label12.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(87, 20);
			this.label12.TabIndex = 33;
			this.label12.Text = "Center Loc";
			// 
			// CLTextBox
			// 
			this.CLTextBox.Location = new System.Drawing.Point(105, 21);
			this.CLTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.CLTextBox.Name = "CLTextBox";
			this.CLTextBox.ReadOnly = true;
			this.CLTextBox.Size = new System.Drawing.Size(67, 26);
			this.CLTextBox.TabIndex = 32;
			// 
			// BluePicBox
			// 
			this.BluePicBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.BluePicBox.ErrorImage = null;
			this.BluePicBox.Location = new System.Drawing.Point(24, 7);
			this.BluePicBox.Name = "BluePicBox";
			this.BluePicBox.Size = new System.Drawing.Size(480, 360);
			this.BluePicBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.BluePicBox.TabIndex = 60;
			this.BluePicBox.TabStop = false;
			// 
			// ProcButton
			// 
			this.ProcButton.Location = new System.Drawing.Point(819, 381);
			this.ProcButton.Margin = new System.Windows.Forms.Padding(2);
			this.ProcButton.Name = "ProcButton";
			this.ProcButton.Size = new System.Drawing.Size(131, 25);
			this.ProcButton.TabIndex = 69;
			this.ProcButton.Text = "Process Frame";
			this.ProcButton.UseVisualStyleBackColor = true;
			this.ProcButton.Click += new System.EventHandler(this.ProcButton_Click);
			// 
			// DIButton
			// 
			this.DIButton.Location = new System.Drawing.Point(73, 219);
			this.DIButton.Margin = new System.Windows.Forms.Padding(2);
			this.DIButton.Name = "DIButton";
			this.DIButton.Size = new System.Drawing.Size(104, 25);
			this.DIButton.TabIndex = 66;
			this.DIButton.Text = "Delete Item";
			this.DIButton.UseVisualStyleBackColor = true;
			this.DIButton.Click += new System.EventHandler(this.DIButton_Click);
			// 
			// KinectTargetCal
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.ProcButton);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.ErrorTextBox);
			this.Controls.Add(this.ShootButton);
			this.Controls.Add(this.VideoPicBox);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.BluePicBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "KinectTargetCal";
			this.Size = new System.Drawing.Size(1064, 760);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.MBANumericUpDown)).EndInit();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.BTNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.ITNumericUpDown)).EndInit();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.VideoPicBox)).EndInit();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.BluePicBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button ABFPButton;
		private System.Windows.Forms.Button UDButton;
		private System.Windows.Forms.NumericUpDown MBANumericUpDown;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox LATextBox;
		private System.Windows.Forms.NumericUpDown BTNumericUpDown;
		private System.Windows.Forms.NumericUpDown ITNumericUpDown;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.ListView BPListView;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.TextBox MBATextBox;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox TWFTextBox;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.TextBox THFTextBox;
		private System.Windows.Forms.Button LVButton;
		private System.Windows.Forms.TextBox ErrorTextBox;
		private System.Windows.Forms.Button ShootButton;
		private System.Windows.Forms.PictureBox VideoPicBox;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox DITextBox;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.TextBox RATextBox;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.TextBox ATextBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox HTextBox;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.TextBox RTextBox;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.TextBox CLTextBox;
		private System.Windows.Forms.PictureBox BluePicBox;
		private System.Windows.Forms.Button ProcButton;
		private System.Windows.Forms.Button DIButton;
		}
	}
