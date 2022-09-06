namespace Sensor_Alignment
	{
	partial class SensorAlignControl
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
			System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
			System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
			System.Windows.Forms.DataVisualization.Charting.Title title1 = new System.Windows.Forms.DataVisualization.Charting.Title();
			System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
			System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
			System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
			System.Windows.Forms.DataVisualization.Charting.Title title2 = new System.Windows.Forms.DataVisualization.Charting.Title();
			this.VideoPictureBox = new System.Windows.Forms.PictureBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.HATextBox = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.ColNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label2 = new System.Windows.Forms.Label();
			this.ShootButton = new System.Windows.Forms.Button();
			this.FrontScanChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
			this.RearScanChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
			this.FLGroupBox = new System.Windows.Forms.GroupBox();
			this.FShiftButton = new System.Windows.Forms.Button();
			this.FSANumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label5 = new System.Windows.Forms.Label();
			this.FLSButton = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.RShiftButton = new System.Windows.Forms.Button();
			this.RSANumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.RLSButton = new System.Windows.Forms.Button();
			this.SaveButton = new System.Windows.Forms.Button();
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).BeginInit();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.ColNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.FrontScanChart)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.RearScanChart)).BeginInit();
			this.FLGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.FSANumericUpDown)).BeginInit();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.RSANumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// VideoPictureBox
			// 
			this.VideoPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.VideoPictureBox.Location = new System.Drawing.Point(15, 16);
			this.VideoPictureBox.Name = "VideoPictureBox";
			this.VideoPictureBox.Size = new System.Drawing.Size(640, 480);
			this.VideoPictureBox.TabIndex = 0;
			this.VideoPictureBox.TabStop = false;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.HATextBox);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.ColNumericUpDown);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.ShootButton);
			this.groupBox1.Location = new System.Drawing.Point(200, 508);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(260, 175);
			this.groupBox1.TabIndex = 1;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Kinect";
			// 
			// HATextBox
			// 
			this.HATextBox.Location = new System.Drawing.Point(146, 111);
			this.HATextBox.Name = "HATextBox";
			this.HATextBox.Size = new System.Drawing.Size(77, 26);
			this.HATextBox.TabIndex = 10;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(37, 114);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(103, 20);
			this.label4.TabIndex = 9;
			this.label4.Text = "Hor  Angle (°)";
			// 
			// ColNumericUpDown
			// 
			this.ColNumericUpDown.Location = new System.Drawing.Point(107, 77);
			this.ColNumericUpDown.Maximum = new decimal(new int[] {
            640,
            0,
            0,
            0});
			this.ColNumericUpDown.Name = "ColNumericUpDown";
			this.ColNumericUpDown.Size = new System.Drawing.Size(85, 26);
			this.ColNumericUpDown.TabIndex = 5;
			this.ColNumericUpDown.Value = new decimal(new int[] {
            320,
            0,
            0,
            0});
			this.ColNumericUpDown.Click += new System.EventHandler(this.RowColChange);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(68, 80);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(32, 20);
			this.label2.TabIndex = 4;
			this.label2.Text = "Col";
			// 
			// ShootButton
			// 
			this.ShootButton.Location = new System.Drawing.Point(66, 39);
			this.ShootButton.Name = "ShootButton";
			this.ShootButton.Size = new System.Drawing.Size(129, 28);
			this.ShootButton.TabIndex = 2;
			this.ShootButton.Text = "Shoot";
			this.ShootButton.UseVisualStyleBackColor = true;
			this.ShootButton.Click += new System.EventHandler(this.ShootButton_Click);
			// 
			// FrontScanChart
			// 
			this.FrontScanChart.BorderlineColor = System.Drawing.Color.Black;
			this.FrontScanChart.BorderlineWidth = 2;
			chartArea1.AxisX.Interval = 2D;
			chartArea1.AxisX.Maximum = 10D;
			chartArea1.AxisX.Minimum = -10D;
			chartArea1.AxisY.Interval = 2D;
			chartArea1.AxisY.Maximum = 20D;
			chartArea1.AxisY.Minimum = 0D;
			chartArea1.Name = "ChartArea1";
			chartArea1.Position.Auto = false;
			chartArea1.Position.Height = 94F;
			chartArea1.Position.Width = 94F;
			chartArea1.Position.X = 1F;
			chartArea1.Position.Y = 1F;
			this.FrontScanChart.ChartAreas.Add(chartArea1);
			legend1.Enabled = false;
			legend1.Name = "Legend1";
			this.FrontScanChart.Legends.Add(legend1);
			this.FrontScanChart.Location = new System.Drawing.Point(701, 16);
			this.FrontScanChart.Margin = new System.Windows.Forms.Padding(2);
			this.FrontScanChart.Name = "FrontScanChart";
			this.FrontScanChart.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.None;
			series1.BorderWidth = 3;
			series1.ChartArea = "ChartArea1";
			series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
			series1.Color = System.Drawing.Color.RoyalBlue;
			series1.Legend = "Legend1";
			series1.Name = "Series1";
			this.FrontScanChart.Series.Add(series1);
			this.FrontScanChart.Size = new System.Drawing.Size(350, 350);
			this.FrontScanChart.TabIndex = 69;
			this.FrontScanChart.Text = "chart1";
			title1.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
			title1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			title1.Name = "Title1";
			title1.Text = "Distance (in) from front LIDAR center";
			this.FrontScanChart.Titles.Add(title1);
			// 
			// RearScanChart
			// 
			this.RearScanChart.BorderlineColor = System.Drawing.Color.Black;
			this.RearScanChart.BorderlineWidth = 2;
			chartArea2.AxisX.Interval = 2D;
			chartArea2.AxisX.Maximum = 10D;
			chartArea2.AxisX.Minimum = -10D;
			chartArea2.AxisY.Interval = 2D;
			chartArea2.AxisY.Maximum = 20D;
			chartArea2.AxisY.Minimum = 0D;
			chartArea2.Name = "ChartArea1";
			chartArea2.Position.Auto = false;
			chartArea2.Position.Height = 94F;
			chartArea2.Position.Width = 94F;
			chartArea2.Position.X = 1F;
			chartArea2.Position.Y = 1F;
			this.RearScanChart.ChartAreas.Add(chartArea2);
			legend2.Enabled = false;
			legend2.Name = "Legend1";
			this.RearScanChart.Legends.Add(legend2);
			this.RearScanChart.Location = new System.Drawing.Point(1097, 16);
			this.RearScanChart.Margin = new System.Windows.Forms.Padding(2);
			this.RearScanChart.Name = "RearScanChart";
			this.RearScanChart.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.None;
			series2.BorderWidth = 3;
			series2.ChartArea = "ChartArea1";
			series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
			series2.Color = System.Drawing.Color.RoyalBlue;
			series2.Legend = "Legend1";
			series2.Name = "Series1";
			this.RearScanChart.Series.Add(series2);
			this.RearScanChart.Size = new System.Drawing.Size(350, 350);
			this.RearScanChart.TabIndex = 70;
			this.RearScanChart.Text = "chart1";
			title2.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
			title2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			title2.Name = "Title1";
			title2.Text = "Distance (in) from rear LIDAR center";
			this.RearScanChart.Titles.Add(title2);
			// 
			// FLGroupBox
			// 
			this.FLGroupBox.Controls.Add(this.FShiftButton);
			this.FLGroupBox.Controls.Add(this.FSANumericUpDown);
			this.FLGroupBox.Controls.Add(this.label5);
			this.FLGroupBox.Controls.Add(this.FLSButton);
			this.FLGroupBox.Location = new System.Drawing.Point(776, 389);
			this.FLGroupBox.Name = "FLGroupBox";
			this.FLGroupBox.Size = new System.Drawing.Size(200, 124);
			this.FLGroupBox.TabIndex = 71;
			this.FLGroupBox.TabStop = false;
			this.FLGroupBox.Text = "Front LIDAR";
			// 
			// FShiftButton
			// 
			this.FShiftButton.Enabled = false;
			this.FShiftButton.Location = new System.Drawing.Point(127, 68);
			this.FShiftButton.Margin = new System.Windows.Forms.Padding(2);
			this.FShiftButton.Name = "FShiftButton";
			this.FShiftButton.Size = new System.Drawing.Size(56, 34);
			this.FShiftButton.TabIndex = 17;
			this.FShiftButton.Text = "Shift";
			this.FShiftButton.UseVisualStyleBackColor = true;
			this.FShiftButton.Click += new System.EventHandler(this.FShiftButton_Click);
			// 
			// FSANumericUpDown
			// 
			this.FSANumericUpDown.DecimalPlaces = 1;
			this.FSANumericUpDown.Location = new System.Drawing.Point(126, 28);
			this.FSANumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.FSANumericUpDown.Maximum = new decimal(new int[] {
            180,
            0,
            0,
            0});
			this.FSANumericUpDown.Minimum = new decimal(new int[] {
            180,
            0,
            0,
            -2147483648});
			this.FSANumericUpDown.Name = "FSANumericUpDown";
			this.FSANumericUpDown.Size = new System.Drawing.Size(60, 26);
			this.FSANumericUpDown.TabIndex = 16;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(14, 31);
			this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(104, 20);
			this.label5.TabIndex = 15;
			this.label5.Text = "Shift angle (°)";
			// 
			// FLSButton
			// 
			this.FLSButton.Location = new System.Drawing.Point(17, 68);
			this.FLSButton.Margin = new System.Windows.Forms.Padding(2);
			this.FLSButton.Name = "FLSButton";
			this.FLSButton.Size = new System.Drawing.Size(56, 34);
			this.FLSButton.TabIndex = 14;
			this.FLSButton.Text = "Scan";
			this.FLSButton.UseVisualStyleBackColor = true;
			this.FLSButton.Click += new System.EventHandler(this.FLSButton_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.RShiftButton);
			this.groupBox2.Controls.Add(this.RSANumericUpDown);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.RLSButton);
			this.groupBox2.Location = new System.Drawing.Point(1172, 389);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(200, 124);
			this.groupBox2.TabIndex = 72;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Rear LIDAR";
			// 
			// RShiftButton
			// 
			this.RShiftButton.Enabled = false;
			this.RShiftButton.Location = new System.Drawing.Point(127, 68);
			this.RShiftButton.Margin = new System.Windows.Forms.Padding(2);
			this.RShiftButton.Name = "RShiftButton";
			this.RShiftButton.Size = new System.Drawing.Size(56, 34);
			this.RShiftButton.TabIndex = 17;
			this.RShiftButton.Text = "Shift";
			this.RShiftButton.UseVisualStyleBackColor = true;
			this.RShiftButton.Click += new System.EventHandler(this.RShiftButton_Click);
			// 
			// RSANumericUpDown
			// 
			this.RSANumericUpDown.DecimalPlaces = 1;
			this.RSANumericUpDown.Location = new System.Drawing.Point(126, 28);
			this.RSANumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.RSANumericUpDown.Maximum = new decimal(new int[] {
            180,
            0,
            0,
            0});
			this.RSANumericUpDown.Minimum = new decimal(new int[] {
            180,
            0,
            0,
            -2147483648});
			this.RSANumericUpDown.Name = "RSANumericUpDown";
			this.RSANumericUpDown.Size = new System.Drawing.Size(60, 26);
			this.RSANumericUpDown.TabIndex = 16;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(14, 31);
			this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(104, 20);
			this.label1.TabIndex = 15;
			this.label1.Text = "Shift angle (°)";
			// 
			// RLSButton
			// 
			this.RLSButton.Location = new System.Drawing.Point(17, 68);
			this.RLSButton.Margin = new System.Windows.Forms.Padding(2);
			this.RLSButton.Name = "RLSButton";
			this.RLSButton.Size = new System.Drawing.Size(56, 34);
			this.RLSButton.TabIndex = 14;
			this.RLSButton.Text = "Scan";
			this.RLSButton.UseVisualStyleBackColor = true;
			this.RLSButton.Click += new System.EventHandler(this.RLSButton_Click);
			// 
			// SaveButton
			// 
			this.SaveButton.Location = new System.Drawing.Point(988, 542);
			this.SaveButton.Name = "SaveButton";
			this.SaveButton.Size = new System.Drawing.Size(195, 28);
			this.SaveButton.TabIndex = 73;
			this.SaveButton.Text = "Save align offset angles";
			this.SaveButton.UseVisualStyleBackColor = true;
			this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
			// 
			// StatusTextBox
			// 
			this.StatusTextBox.BackColor = System.Drawing.Color.White;
			this.StatusTextBox.Location = new System.Drawing.Point(701, 588);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ReadOnly = true;
			this.StatusTextBox.Size = new System.Drawing.Size(746, 95);
			this.StatusTextBox.TabIndex = 74;
			// 
			// SensorAlignControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.StatusTextBox);
			this.Controls.Add(this.SaveButton);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.FLGroupBox);
			this.Controls.Add(this.RearScanChart);
			this.Controls.Add(this.FrontScanChart);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.VideoPictureBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "SensorAlignControl";
			this.Size = new System.Drawing.Size(1478, 710);
			((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.ColNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.FrontScanChart)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.RearScanChart)).EndInit();
			this.FLGroupBox.ResumeLayout(false);
			this.FLGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.FSANumericUpDown)).EndInit();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.RSANumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.PictureBox VideoPictureBox;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.TextBox HATextBox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.NumericUpDown ColNumericUpDown;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button ShootButton;
		private System.Windows.Forms.DataVisualization.Charting.Chart FrontScanChart;
		private System.Windows.Forms.DataVisualization.Charting.Chart RearScanChart;
		private System.Windows.Forms.GroupBox FLGroupBox;
		private System.Windows.Forms.Button FShiftButton;
		private System.Windows.Forms.NumericUpDown FSANumericUpDown;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button FLSButton;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Button RShiftButton;
		private System.Windows.Forms.NumericUpDown RSANumericUpDown;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button RLSButton;
		private System.Windows.Forms.Button SaveButton;
		private System.Windows.Forms.TextBox StatusTextBox;
		}
	}
