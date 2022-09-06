namespace Sensor_Alignment
	{
	partial class HeadTiltAlignControl
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
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.PanNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label5 = new System.Windows.Forms.Label();
			this.TiltNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.SPButton = new System.Windows.Forms.Button();
			this.VideoPictureBox = new System.Windows.Forms.PictureBox();
			this.ShootButton = new System.Windows.Forms.Button();
			this.LocGroupBox = new System.Windows.Forms.GroupBox();
			this.AVATextBox = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.VATextBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.MeasureButton = new System.Windows.Forms.Button();
			this.RowNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label10 = new System.Windows.Forms.Label();
			this.ParamListView = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.ARButton = new System.Windows.Forms.Button();
			this.SendButton = new System.Windows.Forms.Button();
			this.ClrButton = new System.Windows.Forms.Button();
			this.groupBox3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.PanNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TiltNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).BeginInit();
			this.LocGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.RowNumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.PanNumericUpDown);
			this.groupBox3.Controls.Add(this.label5);
			this.groupBox3.Controls.Add(this.TiltNumericUpDown);
			this.groupBox3.Controls.Add(this.label1);
			this.groupBox3.Controls.Add(this.SPButton);
			this.groupBox3.Location = new System.Drawing.Point(716, 41);
			this.groupBox3.Margin = new System.Windows.Forms.Padding(2);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Padding = new System.Windows.Forms.Padding(2);
			this.groupBox3.Size = new System.Drawing.Size(176, 163);
			this.groupBox3.TabIndex = 68;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Kinect Position";
			// 
			// PanNumericUpDown
			// 
			this.PanNumericUpDown.Location = new System.Drawing.Point(69, 67);
			this.PanNumericUpDown.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
			this.PanNumericUpDown.Minimum = new decimal(new int[] {
            40,
            0,
            0,
            -2147483648});
			this.PanNumericUpDown.Name = "PanNumericUpDown";
			this.PanNumericUpDown.Size = new System.Drawing.Size(85, 26);
			this.PanNumericUpDown.TabIndex = 10;
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(23, 72);
			this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(41, 16);
			this.label5.TabIndex = 9;
			this.label5.Text = "Pan (°)";
			// 
			// TiltNumericUpDown
			// 
			this.TiltNumericUpDown.Location = new System.Drawing.Point(69, 23);
			this.TiltNumericUpDown.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
			this.TiltNumericUpDown.Minimum = new decimal(new int[] {
            40,
            0,
            0,
            -2147483648});
			this.TiltNumericUpDown.Name = "TiltNumericUpDown";
			this.TiltNumericUpDown.Size = new System.Drawing.Size(85, 26);
			this.TiltNumericUpDown.TabIndex = 8;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(23, 28);
			this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(41, 16);
			this.label1.TabIndex = 7;
			this.label1.Text = "Tilt (°)";
			// 
			// SPButton
			// 
			this.SPButton.AutoSize = true;
			this.SPButton.Location = new System.Drawing.Point(17, 111);
			this.SPButton.Name = "SPButton";
			this.SPButton.Size = new System.Drawing.Size(142, 30);
			this.SPButton.TabIndex = 3;
			this.SPButton.Text = "Set";
			this.SPButton.UseVisualStyleBackColor = true;
			this.SPButton.Click += new System.EventHandler(this.SPButton_Click);
			// 
			// VideoPictureBox
			// 
			this.VideoPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.VideoPictureBox.Location = new System.Drawing.Point(22, 41);
			this.VideoPictureBox.Name = "VideoPictureBox";
			this.VideoPictureBox.Size = new System.Drawing.Size(640, 480);
			this.VideoPictureBox.TabIndex = 66;
			this.VideoPictureBox.TabStop = false;
			// 
			// ShootButton
			// 
			this.ShootButton.Location = new System.Drawing.Point(733, 223);
			this.ShootButton.Name = "ShootButton";
			this.ShootButton.Size = new System.Drawing.Size(142, 30);
			this.ShootButton.TabIndex = 67;
			this.ShootButton.Text = "Shoot";
			this.ShootButton.UseVisualStyleBackColor = true;
			this.ShootButton.Click += new System.EventHandler(this.ShootButton_Click);
			// 
			// LocGroupBox
			// 
			this.LocGroupBox.Controls.Add(this.AVATextBox);
			this.LocGroupBox.Controls.Add(this.label7);
			this.LocGroupBox.Controls.Add(this.VATextBox);
			this.LocGroupBox.Controls.Add(this.label2);
			this.LocGroupBox.Controls.Add(this.MeasureButton);
			this.LocGroupBox.Controls.Add(this.RowNumericUpDown);
			this.LocGroupBox.Controls.Add(this.label10);
			this.LocGroupBox.Location = new System.Drawing.Point(679, 272);
			this.LocGroupBox.Name = "LocGroupBox";
			this.LocGroupBox.Size = new System.Drawing.Size(251, 248);
			this.LocGroupBox.TabIndex = 68;
			this.LocGroupBox.TabStop = false;
			this.LocGroupBox.Text = "Location";
			// 
			// AVATextBox
			// 
			this.AVATextBox.Location = new System.Drawing.Point(164, 188);
			this.AVATextBox.Name = "AVATextBox";
			this.AVATextBox.Size = new System.Drawing.Size(77, 26);
			this.AVATextBox.TabIndex = 16;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(17, 191);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(142, 20);
			this.label7.TabIndex = 15;
			this.label7.Text = "Actual Tilt Angle (°)";
			// 
			// VATextBox
			// 
			this.VATextBox.Location = new System.Drawing.Point(141, 134);
			this.VATextBox.Name = "VATextBox";
			this.VATextBox.Size = new System.Drawing.Size(77, 26);
			this.VATextBox.TabIndex = 8;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(32, 137);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(103, 20);
			this.label2.TabIndex = 7;
			this.label2.Text = "Vert Angle (°)";
			// 
			// MeasureButton
			// 
			this.MeasureButton.Location = new System.Drawing.Point(61, 78);
			this.MeasureButton.Name = "MeasureButton";
			this.MeasureButton.Size = new System.Drawing.Size(142, 28);
			this.MeasureButton.TabIndex = 6;
			this.MeasureButton.Text = "Measure";
			this.MeasureButton.UseVisualStyleBackColor = true;
			this.MeasureButton.Click += new System.EventHandler(this.MeasureButton_Click);
			// 
			// RowNumericUpDown
			// 
			this.RowNumericUpDown.Location = new System.Drawing.Point(107, 24);
			this.RowNumericUpDown.Maximum = new decimal(new int[] {
            480,
            0,
            0,
            0});
			this.RowNumericUpDown.Name = "RowNumericUpDown";
			this.RowNumericUpDown.Size = new System.Drawing.Size(85, 26);
			this.RowNumericUpDown.TabIndex = 1;
			this.RowNumericUpDown.Value = new decimal(new int[] {
            240,
            0,
            0,
            0});
			this.RowNumericUpDown.ValueChanged += new System.EventHandler(this.RowColChange);
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(58, 27);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(41, 20);
			this.label10.TabIndex = 0;
			this.label10.Text = "Row";
			// 
			// ParamListView
			// 
			this.ParamListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.ParamListView.Enabled = false;
			this.ParamListView.GridLines = true;
			this.ParamListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.ParamListView.Location = new System.Drawing.Point(972, 210);
			this.ParamListView.Name = "ParamListView";
			this.ParamListView.Size = new System.Drawing.Size(166, 201);
			this.ParamListView.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.ParamListView.TabIndex = 69;
			this.ParamListView.UseCompatibleStateImageBehavior = false;
			this.ParamListView.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Set";
			this.columnHeader1.Width = 81;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Actual";
			this.columnHeader2.Width = 80;
			// 
			// StatusTextBox
			// 
			this.StatusTextBox.BackColor = System.Drawing.Color.White;
			this.StatusTextBox.Location = new System.Drawing.Point(928, 41);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ReadOnly = true;
			this.StatusTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.StatusTextBox.Size = new System.Drawing.Size(254, 163);
			this.StatusTextBox.TabIndex = 70;
			// 
			// ARButton
			// 
			this.ARButton.Location = new System.Drawing.Point(984, 453);
			this.ARButton.Name = "ARButton";
			this.ARButton.Size = new System.Drawing.Size(142, 30);
			this.ARButton.TabIndex = 73;
			this.ARButton.Text = "Add/Replace";
			this.ARButton.UseVisualStyleBackColor = true;
			this.ARButton.Click += new System.EventHandler(this.ARButton_Click);
			// 
			// SendButton
			// 
			this.SendButton.Location = new System.Drawing.Point(984, 489);
			this.SendButton.Name = "SendButton";
			this.SendButton.Size = new System.Drawing.Size(142, 30);
			this.SendButton.TabIndex = 74;
			this.SendButton.Text = "Save";
			this.SendButton.UseVisualStyleBackColor = true;
			this.SendButton.Click += new System.EventHandler(this.SendButton_Click);
			// 
			// ClrButton
			// 
			this.ClrButton.Location = new System.Drawing.Point(984, 417);
			this.ClrButton.Name = "ClrButton";
			this.ClrButton.Size = new System.Drawing.Size(142, 30);
			this.ClrButton.TabIndex = 75;
			this.ClrButton.Text = "Clear";
			this.ClrButton.UseVisualStyleBackColor = true;
			this.ClrButton.Click += new System.EventHandler(this.ClrButton_Click);
			// 
			// HeadTiltAlignControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.ClrButton);
			this.Controls.Add(this.SendButton);
			this.Controls.Add(this.ARButton);
			this.Controls.Add(this.StatusTextBox);
			this.Controls.Add(this.ParamListView);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.LocGroupBox);
			this.Controls.Add(this.ShootButton);
			this.Controls.Add(this.VideoPictureBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "HeadTiltAlignControl";
			this.Size = new System.Drawing.Size(1206, 563);
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.PanNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TiltNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).EndInit();
			this.LocGroupBox.ResumeLayout(false);
			this.LocGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.RowNumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button SPButton;
		private System.Windows.Forms.PictureBox VideoPictureBox;
		private System.Windows.Forms.Button ShootButton;
		private System.Windows.Forms.GroupBox LocGroupBox;
		private System.Windows.Forms.TextBox VATextBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button MeasureButton;
		private System.Windows.Forms.NumericUpDown RowNumericUpDown;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.NumericUpDown TiltNumericUpDown;
		private System.Windows.Forms.ListView ParamListView;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.Button ARButton;
		private System.Windows.Forms.Button SendButton;
		private System.Windows.Forms.Button ClrButton;
		private System.Windows.Forms.NumericUpDown PanNumericUpDown;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox AVATextBox;
		private System.Windows.Forms.Label label7;
		}
	}
