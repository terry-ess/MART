namespace Manual_Control
	{
	partial class ManualControlForm
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ManualControlForm));
			System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
			System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
			System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
			System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
			System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
			System.Windows.Forms.DataVisualization.Charting.Series series4 = new System.Windows.Forms.DataVisualization.Charting.Series();
			System.Windows.Forms.DataVisualization.Charting.Title title1 = new System.Windows.Forms.DataVisualization.Charting.Title();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.RSTextBox = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.FSTextBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.MHTextBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.LeftButton = new System.Windows.Forms.Button();
			this.ForwardButton = new System.Windows.Forms.Button();
			this.RightButton = new System.Windows.Forms.Button();
			this.BackwardButton = new System.Windows.Forms.Button();
			this.StopButton = new System.Windows.Forms.Button();
			this.FLGroupBox = new System.Windows.Forms.GroupBox();
			this.AutoButton = new System.Windows.Forms.Button();
			this.label9 = new System.Windows.Forms.Label();
			this.Dist180TextBox = new System.Windows.Forms.TextBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.label12 = new System.Windows.Forms.Label();
			this.MDistTextBox = new System.Windows.Forms.TextBox();
			this.XNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label7 = new System.Windows.Forms.Label();
			this.YNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label8 = new System.Windows.Forms.Label();
			this.SaveButton = new System.Windows.Forms.Button();
			this.PathButton = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.Dist0TextBox = new System.Windows.Forms.TextBox();
			this.ShiftBbutton = new System.Windows.Forms.Button();
			this.SANumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label5 = new System.Windows.Forms.Label();
			this.SLNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label6 = new System.Windows.Forms.Label();
			this.ShootButton = new System.Windows.Forms.Button();
			this.ScanChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
			this.KPGroupBox = new System.Windows.Forms.GroupBox();
			this.PanNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.TiltNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label10 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.SPButton = new System.Windows.Forms.Button();
			this.VideoPictureBox = new System.Windows.Forms.PictureBox();
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.VSButton = new System.Windows.Forms.Button();
			this.VSaveButton = new System.Windows.Forms.Button();
			this.AutoTimer = new System.Windows.Forms.Timer(this.components);
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.CTButton = new System.Windows.Forms.Button();
			this.label13 = new System.Windows.Forms.Label();
			this.TANumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.RTRadioButton = new System.Windows.Forms.RadioButton();
			this.LTRadioButton = new System.Windows.Forms.RadioButton();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.FLGroupBox.SuspendLayout();
			this.groupBox3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.XNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.YNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.SANumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.SLNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.ScanChart)).BeginInit();
			this.KPGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.PanNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TiltNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).BeginInit();
			this.groupBox4.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TANumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.RSTextBox);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.FSTextBox);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.MHTextBox);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Location = new System.Drawing.Point(29, 17);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(250, 185);
			this.groupBox1.TabIndex = 7;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Sensors";
			// 
			// RSTextBox
			// 
			this.RSTextBox.Location = new System.Drawing.Point(136, 135);
			this.RSTextBox.Name = "RSTextBox";
			this.RSTextBox.Size = new System.Drawing.Size(100, 26);
			this.RSTextBox.TabIndex = 5;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(26, 139);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(91, 20);
			this.label3.TabIndex = 4;
			this.label3.Text = "Rear Sonar";
			// 
			// FSTextBox
			// 
			this.FSTextBox.Location = new System.Drawing.Point(136, 86);
			this.FSTextBox.Name = "FSTextBox";
			this.FSTextBox.Size = new System.Drawing.Size(100, 26);
			this.FSTextBox.TabIndex = 3;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(26, 89);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(91, 20);
			this.label2.TabIndex = 2;
			this.label2.Text = "Front sonar";
			// 
			// MHTextBox
			// 
			this.MHTextBox.Location = new System.Drawing.Point(136, 37);
			this.MHTextBox.Name = "MHTextBox";
			this.MHTextBox.Size = new System.Drawing.Size(100, 26);
			this.MHTextBox.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(26, 40);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(105, 20);
			this.label1.TabIndex = 0;
			this.label1.Text = "Mag. heading";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.LeftButton);
			this.groupBox2.Controls.Add(this.ForwardButton);
			this.groupBox2.Controls.Add(this.RightButton);
			this.groupBox2.Controls.Add(this.BackwardButton);
			this.groupBox2.Controls.Add(this.StopButton);
			this.groupBox2.Location = new System.Drawing.Point(29, 213);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(250, 250);
			this.groupBox2.TabIndex = 8;
			this.groupBox2.TabStop = false;
			// 
			// LeftButton
			// 
			this.LeftButton.Image = ((System.Drawing.Image)(resources.GetObject("LeftButton.Image")));
			this.LeftButton.Location = new System.Drawing.Point(39, 103);
			this.LeftButton.Name = "LeftButton";
			this.LeftButton.Size = new System.Drawing.Size(60, 60);
			this.LeftButton.TabIndex = 15;
			this.LeftButton.UseVisualStyleBackColor = true;
			this.LeftButton.Click += new System.EventHandler(this.LeftButton_Click);
			// 
			// ForwardButton
			// 
			this.ForwardButton.Image = ((System.Drawing.Image)(resources.GetObject("ForwardButton.Image")));
			this.ForwardButton.Location = new System.Drawing.Point(101, 40);
			this.ForwardButton.Name = "ForwardButton";
			this.ForwardButton.Size = new System.Drawing.Size(60, 60);
			this.ForwardButton.TabIndex = 14;
			this.ForwardButton.UseVisualStyleBackColor = true;
			this.ForwardButton.Click += new System.EventHandler(this.ForwardButton_Click);
			// 
			// RightButton
			// 
			this.RightButton.Image = ((System.Drawing.Image)(resources.GetObject("RightButton.Image")));
			this.RightButton.Location = new System.Drawing.Point(164, 103);
			this.RightButton.Name = "RightButton";
			this.RightButton.Size = new System.Drawing.Size(60, 60);
			this.RightButton.TabIndex = 13;
			this.RightButton.UseVisualStyleBackColor = true;
			this.RightButton.Click += new System.EventHandler(this.RightButton_Click);
			// 
			// BackwardButton
			// 
			this.BackwardButton.Image = ((System.Drawing.Image)(resources.GetObject("BackwardButton.Image")));
			this.BackwardButton.Location = new System.Drawing.Point(101, 165);
			this.BackwardButton.Name = "BackwardButton";
			this.BackwardButton.Size = new System.Drawing.Size(60, 60);
			this.BackwardButton.TabIndex = 12;
			this.BackwardButton.UseVisualStyleBackColor = true;
			this.BackwardButton.Click += new System.EventHandler(this.BackwardButton_Click);
			// 
			// StopButton
			// 
			this.StopButton.BackColor = System.Drawing.Color.OrangeRed;
			this.StopButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.StopButton.Location = new System.Drawing.Point(99, 100);
			this.StopButton.Name = "StopButton";
			this.StopButton.Size = new System.Drawing.Size(65, 65);
			this.StopButton.TabIndex = 11;
			this.StopButton.Text = "STOP";
			this.StopButton.UseVisualStyleBackColor = false;
			this.StopButton.Click += new System.EventHandler(this.StopButton_Click);
			// 
			// FLGroupBox
			// 
			this.FLGroupBox.Controls.Add(this.AutoButton);
			this.FLGroupBox.Controls.Add(this.label9);
			this.FLGroupBox.Controls.Add(this.Dist180TextBox);
			this.FLGroupBox.Controls.Add(this.groupBox3);
			this.FLGroupBox.Controls.Add(this.SaveButton);
			this.FLGroupBox.Controls.Add(this.PathButton);
			this.FLGroupBox.Controls.Add(this.label4);
			this.FLGroupBox.Controls.Add(this.Dist0TextBox);
			this.FLGroupBox.Controls.Add(this.ShiftBbutton);
			this.FLGroupBox.Controls.Add(this.SANumericUpDown);
			this.FLGroupBox.Controls.Add(this.label5);
			this.FLGroupBox.Controls.Add(this.SLNumericUpDown);
			this.FLGroupBox.Controls.Add(this.label6);
			this.FLGroupBox.Controls.Add(this.ShootButton);
			this.FLGroupBox.Enabled = false;
			this.FLGroupBox.Location = new System.Drawing.Point(552, 496);
			this.FLGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.FLGroupBox.Name = "FLGroupBox";
			this.FLGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.FLGroupBox.Size = new System.Drawing.Size(450, 208);
			this.FLGroupBox.TabIndex = 67;
			this.FLGroupBox.TabStop = false;
			this.FLGroupBox.Text = "LIDAR";
			// 
			// AutoButton
			// 
			this.AutoButton.BackColor = System.Drawing.Color.LightGreen;
			this.AutoButton.Location = new System.Drawing.Point(107, 158);
			this.AutoButton.Margin = new System.Windows.Forms.Padding(2);
			this.AutoButton.Name = "AutoButton";
			this.AutoButton.Size = new System.Drawing.Size(56, 34);
			this.AutoButton.TabIndex = 74;
			this.AutoButton.Text = "Auto";
			this.AutoButton.UseVisualStyleBackColor = false;
			this.AutoButton.Click += new System.EventHandler(this.AutoButton_Click);
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(12, 125);
			this.label9.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(152, 20);
			this.label9.TabIndex = 73;
			this.label9.Text = "Distance at 180° (in)";
			// 
			// Dist180TextBox
			// 
			this.Dist180TextBox.Enabled = false;
			this.Dist180TextBox.Location = new System.Drawing.Point(165, 122);
			this.Dist180TextBox.Margin = new System.Windows.Forms.Padding(2);
			this.Dist180TextBox.Name = "Dist180TextBox";
			this.Dist180TextBox.Size = new System.Drawing.Size(65, 26);
			this.Dist180TextBox.TabIndex = 72;
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.label12);
			this.groupBox3.Controls.Add(this.MDistTextBox);
			this.groupBox3.Controls.Add(this.XNumericUpDown);
			this.groupBox3.Controls.Add(this.label7);
			this.groupBox3.Controls.Add(this.YNumericUpDown);
			this.groupBox3.Controls.Add(this.label8);
			this.groupBox3.Location = new System.Drawing.Point(241, 14);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(200, 131);
			this.groupBox3.TabIndex = 71;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Marker";
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(17, 99);
			this.label12.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(98, 20);
			this.label12.TabIndex = 70;
			this.label12.Text = "Distance (in)";
			// 
			// MDistTextBox
			// 
			this.MDistTextBox.Enabled = false;
			this.MDistTextBox.Location = new System.Drawing.Point(118, 96);
			this.MDistTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.MDistTextBox.Name = "MDistTextBox";
			this.MDistTextBox.Size = new System.Drawing.Size(65, 26);
			this.MDistTextBox.TabIndex = 69;
			// 
			// XNumericUpDown
			// 
			this.XNumericUpDown.DecimalPlaces = 1;
			this.XNumericUpDown.Location = new System.Drawing.Point(74, 59);
			this.XNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.XNumericUpDown.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            -2147483648});
			this.XNumericUpDown.Name = "XNumericUpDown";
			this.XNumericUpDown.Size = new System.Drawing.Size(76, 26);
			this.XNumericUpDown.TabIndex = 16;
			this.XNumericUpDown.ValueChanged += new System.EventHandler(this.MarkerValueChanged);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(51, 62);
			this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(20, 20);
			this.label7.TabIndex = 15;
			this.label7.Text = "X";
			// 
			// YNumericUpDown
			// 
			this.YNumericUpDown.DecimalPlaces = 1;
			this.YNumericUpDown.Location = new System.Drawing.Point(74, 19);
			this.YNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.YNumericUpDown.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            -2147483648});
			this.YNumericUpDown.Name = "YNumericUpDown";
			this.YNumericUpDown.Size = new System.Drawing.Size(76, 26);
			this.YNumericUpDown.TabIndex = 14;
			this.YNumericUpDown.ValueChanged += new System.EventHandler(this.MarkerValueChanged);
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(51, 22);
			this.label8.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(20, 20);
			this.label8.TabIndex = 13;
			this.label8.Text = "Y";
			// 
			// SaveButton
			// 
			this.SaveButton.Enabled = false;
			this.SaveButton.Location = new System.Drawing.Point(380, 159);
			this.SaveButton.Margin = new System.Windows.Forms.Padding(2);
			this.SaveButton.Name = "SaveButton";
			this.SaveButton.Size = new System.Drawing.Size(56, 34);
			this.SaveButton.TabIndex = 70;
			this.SaveButton.Text = "Save";
			this.SaveButton.UseVisualStyleBackColor = true;
			this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
			// 
			// PathButton
			// 
			this.PathButton.Enabled = false;
			this.PathButton.Location = new System.Drawing.Point(289, 159);
			this.PathButton.Margin = new System.Windows.Forms.Padding(2);
			this.PathButton.Name = "PathButton";
			this.PathButton.Size = new System.Drawing.Size(56, 34);
			this.PathButton.TabIndex = 69;
			this.PathButton.Text = "Path";
			this.PathButton.UseVisualStyleBackColor = true;
			this.PathButton.Click += new System.EventHandler(this.PathButton_Click);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(12, 92);
			this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(134, 20);
			this.label4.TabIndex = 68;
			this.label4.Text = "Distance at 0° (in)";
			// 
			// Dist0TextBox
			// 
			this.Dist0TextBox.Enabled = false;
			this.Dist0TextBox.Location = new System.Drawing.Point(165, 89);
			this.Dist0TextBox.Margin = new System.Windows.Forms.Padding(2);
			this.Dist0TextBox.Name = "Dist0TextBox";
			this.Dist0TextBox.Size = new System.Drawing.Size(65, 26);
			this.Dist0TextBox.TabIndex = 67;
			// 
			// ShiftBbutton
			// 
			this.ShiftBbutton.Enabled = false;
			this.ShiftBbutton.Location = new System.Drawing.Point(198, 159);
			this.ShiftBbutton.Margin = new System.Windows.Forms.Padding(2);
			this.ShiftBbutton.Name = "ShiftBbutton";
			this.ShiftBbutton.Size = new System.Drawing.Size(56, 34);
			this.ShiftBbutton.TabIndex = 13;
			this.ShiftBbutton.Text = "Shift";
			this.ShiftBbutton.UseVisualStyleBackColor = true;
			this.ShiftBbutton.Click += new System.EventHandler(this.ShiftBbutton_Click);
			// 
			// SANumericUpDown
			// 
			this.SANumericUpDown.Location = new System.Drawing.Point(165, 56);
			this.SANumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.SANumericUpDown.Maximum = new decimal(new int[] {
            180,
            0,
            0,
            0});
			this.SANumericUpDown.Minimum = new decimal(new int[] {
            180,
            0,
            0,
            -2147483648});
			this.SANumericUpDown.Name = "SANumericUpDown";
			this.SANumericUpDown.Size = new System.Drawing.Size(60, 26);
			this.SANumericUpDown.TabIndex = 12;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(12, 59);
			this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(104, 20);
			this.label5.TabIndex = 11;
			this.label5.Text = "Shift angle (°)";
			// 
			// SLNumericUpDown
			// 
			this.SLNumericUpDown.Location = new System.Drawing.Point(165, 23);
			this.SLNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.SLNumericUpDown.Maximum = new decimal(new int[] {
            240,
            0,
            0,
            0});
			this.SLNumericUpDown.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
			this.SLNumericUpDown.Name = "SLNumericUpDown";
			this.SLNumericUpDown.Size = new System.Drawing.Size(60, 26);
			this.SLNumericUpDown.TabIndex = 9;
			this.SLNumericUpDown.Value = new decimal(new int[] {
            120,
            0,
            0,
            0});
			this.SLNumericUpDown.ValueChanged += new System.EventHandler(this.SLNumericUpDown_ValueChanged);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(12, 26);
			this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(103, 20);
			this.label6.TabIndex = 8;
			this.label6.Text = "Scan limit (in)";
			// 
			// ShootButton
			// 
			this.ShootButton.Location = new System.Drawing.Point(16, 158);
			this.ShootButton.Margin = new System.Windows.Forms.Padding(2);
			this.ShootButton.Name = "ShootButton";
			this.ShootButton.Size = new System.Drawing.Size(56, 34);
			this.ShootButton.TabIndex = 7;
			this.ShootButton.Text = "Scan";
			this.ShootButton.UseVisualStyleBackColor = true;
			this.ShootButton.Click += new System.EventHandler(this.ShootButton_Click);
			// 
			// ScanChart
			// 
			this.ScanChart.BorderlineColor = System.Drawing.Color.Black;
			this.ScanChart.BorderlineWidth = 2;
			chartArea1.BackColor = System.Drawing.SystemColors.Control;
			chartArea1.Name = "ChartArea1";
			chartArea1.Position.Auto = false;
			chartArea1.Position.Height = 94F;
			chartArea1.Position.Width = 94F;
			chartArea1.Position.X = 1F;
			chartArea1.Position.Y = 1F;
			this.ScanChart.ChartAreas.Add(chartArea1);
			legend1.Enabled = false;
			legend1.Name = "Legend1";
			this.ScanChart.Legends.Add(legend1);
			this.ScanChart.Location = new System.Drawing.Point(551, 17);
			this.ScanChart.Margin = new System.Windows.Forms.Padding(2);
			this.ScanChart.Name = "ScanChart";
			this.ScanChart.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.None;
			series1.ChartArea = "ChartArea1";
			series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
			series1.Color = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(224)))), ((int)(((byte)(192)))));
			series1.Legend = "Legend1";
			series1.Name = "Series1";
			series2.BorderWidth = 3;
			series2.ChartArea = "ChartArea1";
			series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
			series2.Color = System.Drawing.Color.RoyalBlue;
			series2.Legend = "Legend1";
			series2.Name = "Series2";
			series3.ChartArea = "ChartArea1";
			series3.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
			series3.Color = System.Drawing.Color.Black;
			series3.Legend = "Legend1";
			series3.Name = "Series3";
			series4.ChartArea = "ChartArea1";
			series4.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
			series4.Color = System.Drawing.Color.Red;
			series4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			series4.Legend = "Legend1";
			series4.MarkerSize = 8;
			series4.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Cross;
			series4.Name = "Series4";
			this.ScanChart.Series.Add(series1);
			this.ScanChart.Series.Add(series2);
			this.ScanChart.Series.Add(series3);
			this.ScanChart.Series.Add(series4);
			this.ScanChart.Size = new System.Drawing.Size(450, 450);
			this.ScanChart.TabIndex = 68;
			this.ScanChart.Text = "chart1";
			title1.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
			title1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			title1.Name = "Title1";
			title1.Text = "Distance (in) from robot front center";
			this.ScanChart.Titles.Add(title1);
			// 
			// KPGroupBox
			// 
			this.KPGroupBox.Controls.Add(this.PanNumericUpDown);
			this.KPGroupBox.Controls.Add(this.TiltNumericUpDown);
			this.KPGroupBox.Controls.Add(this.label10);
			this.KPGroupBox.Controls.Add(this.label11);
			this.KPGroupBox.Controls.Add(this.SPButton);
			this.KPGroupBox.Location = new System.Drawing.Point(1146, 427);
			this.KPGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.KPGroupBox.Name = "KPGroupBox";
			this.KPGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.KPGroupBox.Size = new System.Drawing.Size(195, 145);
			this.KPGroupBox.TabIndex = 72;
			this.KPGroupBox.TabStop = false;
			this.KPGroupBox.Text = "Kinect Orientation";
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
			// VideoPictureBox
			// 
			this.VideoPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.VideoPictureBox.Location = new System.Drawing.Point(1012, 17);
			this.VideoPictureBox.Margin = new System.Windows.Forms.Padding(2);
			this.VideoPictureBox.Name = "VideoPictureBox";
			this.VideoPictureBox.Size = new System.Drawing.Size(480, 360);
			this.VideoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.VideoPictureBox.TabIndex = 74;
			this.VideoPictureBox.TabStop = false;
			// 
			// StatusTextBox
			// 
			this.StatusTextBox.BackColor = System.Drawing.Color.White;
			this.StatusTextBox.Location = new System.Drawing.Point(29, 496);
			this.StatusTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ReadOnly = true;
			this.StatusTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.StatusTextBox.Size = new System.Drawing.Size(511, 208);
			this.StatusTextBox.TabIndex = 75;
			// 
			// VSButton
			// 
			this.VSButton.Location = new System.Drawing.Point(1102, 394);
			this.VSButton.Name = "VSButton";
			this.VSButton.Size = new System.Drawing.Size(117, 25);
			this.VSButton.TabIndex = 76;
			this.VSButton.Text = "Video Shoot";
			this.VSButton.UseVisualStyleBackColor = true;
			this.VSButton.Click += new System.EventHandler(this.VSButton_Click);
			// 
			// VSaveButton
			// 
			this.VSaveButton.Enabled = false;
			this.VSaveButton.Location = new System.Drawing.Point(1284, 394);
			this.VSaveButton.Name = "VSaveButton";
			this.VSaveButton.Size = new System.Drawing.Size(117, 25);
			this.VSaveButton.TabIndex = 77;
			this.VSaveButton.Text = "Video Save";
			this.VSaveButton.UseVisualStyleBackColor = true;
			this.VSaveButton.Click += new System.EventHandler(this.VSaveButton_Click);
			// 
			// AutoTimer
			// 
			this.AutoTimer.Interval = 2000;
			this.AutoTimer.Tick += new System.EventHandler(this.AutoTimer_Tick);
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.CTButton);
			this.groupBox4.Controls.Add(this.label13);
			this.groupBox4.Controls.Add(this.TANumericUpDown);
			this.groupBox4.Controls.Add(this.RTRadioButton);
			this.groupBox4.Controls.Add(this.LTRadioButton);
			this.groupBox4.Location = new System.Drawing.Point(290, 21);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(250, 185);
			this.groupBox4.TabIndex = 78;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Check Turn";
			// 
			// CTButton
			// 
			this.CTButton.Location = new System.Drawing.Point(81, 133);
			this.CTButton.Name = "CTButton";
			this.CTButton.Size = new System.Drawing.Size(75, 30);
			this.CTButton.TabIndex = 4;
			this.CTButton.Text = "Check";
			this.CTButton.UseVisualStyleBackColor = true;
			this.CTButton.Click += new System.EventHandler(this.CTButton_Click);
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(33, 89);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(103, 20);
			this.label13.TabIndex = 3;
			this.label13.Text = "Turn angle (°)";
			// 
			// TANumericUpDown
			// 
			this.TANumericUpDown.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
			this.TANumericUpDown.Location = new System.Drawing.Point(140, 86);
			this.TANumericUpDown.Maximum = new decimal(new int[] {
            180,
            0,
            0,
            0});
			this.TANumericUpDown.Name = "TANumericUpDown";
			this.TANumericUpDown.Size = new System.Drawing.Size(63, 26);
			this.TANumericUpDown.TabIndex = 2;
			// 
			// RTRadioButton
			// 
			this.RTRadioButton.AutoSize = true;
			this.RTRadioButton.Checked = true;
			this.RTRadioButton.Location = new System.Drawing.Point(129, 38);
			this.RTRadioButton.Name = "RTRadioButton";
			this.RTRadioButton.Size = new System.Drawing.Size(65, 24);
			this.RTRadioButton.TabIndex = 1;
			this.RTRadioButton.TabStop = true;
			this.RTRadioButton.Text = "Right";
			this.RTRadioButton.UseVisualStyleBackColor = true;
			// 
			// LTRadioButton
			// 
			this.LTRadioButton.AutoSize = true;
			this.LTRadioButton.Location = new System.Drawing.Point(38, 38);
			this.LTRadioButton.Name = "LTRadioButton";
			this.LTRadioButton.Size = new System.Drawing.Size(55, 24);
			this.LTRadioButton.TabIndex = 0;
			this.LTRadioButton.Text = "Left";
			this.LTRadioButton.UseVisualStyleBackColor = true;
			// 
			// ManualControlForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(1509, 724);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.VSaveButton);
			this.Controls.Add(this.VSButton);
			this.Controls.Add(this.StatusTextBox);
			this.Controls.Add(this.VideoPictureBox);
			this.Controls.Add(this.KPGroupBox);
			this.Controls.Add(this.ScanChart);
			this.Controls.Add(this.FLGroupBox);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ManualControlForm";
			this.ShowIcon = false;
			this.Text = "Manual Control";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ManualControlForm_FormClosing);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.FLGroupBox.ResumeLayout(false);
			this.FLGroupBox.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.XNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.YNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.SANumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.SLNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.ScanChart)).EndInit();
			this.KPGroupBox.ResumeLayout(false);
			this.KPGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.PanNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TiltNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).EndInit();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.TANumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Button LeftButton;
		private System.Windows.Forms.Button ForwardButton;
		private System.Windows.Forms.Button RightButton;
		private System.Windows.Forms.Button BackwardButton;
		private System.Windows.Forms.Button StopButton;
		private System.Windows.Forms.GroupBox FLGroupBox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox Dist0TextBox;
		private System.Windows.Forms.Button ShiftBbutton;
		private System.Windows.Forms.NumericUpDown SANumericUpDown;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.NumericUpDown SLNumericUpDown;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Button ShootButton;
		private System.Windows.Forms.DataVisualization.Charting.Chart ScanChart;
		private System.Windows.Forms.GroupBox KPGroupBox;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Button SPButton;
		private System.Windows.Forms.PictureBox VideoPictureBox;
		private System.Windows.Forms.NumericUpDown TiltNumericUpDown;
		private System.Windows.Forms.NumericUpDown PanNumericUpDown;
		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.TextBox RSTextBox;
		private System.Windows.Forms.TextBox FSTextBox;
		private System.Windows.Forms.TextBox MHTextBox;
		private System.Windows.Forms.Button PathButton;
		private System.Windows.Forms.Button VSButton;
		private System.Windows.Forms.Button SaveButton;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.NumericUpDown XNumericUpDown;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.NumericUpDown YNumericUpDown;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.TextBox Dist180TextBox;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.TextBox MDistTextBox;
		private System.Windows.Forms.Button VSaveButton;
		private System.Windows.Forms.Button AutoButton;
		private System.Windows.Forms.Timer AutoTimer;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.Button CTButton;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.NumericUpDown TANumericUpDown;
		private System.Windows.Forms.RadioButton RTRadioButton;
		private System.Windows.Forms.RadioButton LTRadioButton;
		}
	}