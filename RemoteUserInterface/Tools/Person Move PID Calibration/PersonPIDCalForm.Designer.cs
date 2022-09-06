namespace Person_Move_PID_Calibration
	{
	partial class PersonPIDCalForm
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
			System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
			System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
			System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
			this.PIDGroupBox = new System.Windows.Forms.GroupBox();
			this.PPCheckBox = new System.Windows.Forms.CheckBox();
			this.DGNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.IGNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.PGNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label14 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.label15 = new System.Windows.Forms.Label();
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.DataListView = new System.Windows.Forms.ListView();
			this.Run = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Pgain = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Igain = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.Dgain = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.DataFile = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ProfileChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
			this.StartButton = new System.Windows.Forms.Button();
			this.PIDGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.DGNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.IGNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.PGNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.ProfileChart)).BeginInit();
			this.SuspendLayout();
			// 
			// PIDGroupBox
			// 
			this.PIDGroupBox.Controls.Add(this.PPCheckBox);
			this.PIDGroupBox.Controls.Add(this.DGNumericUpDown);
			this.PIDGroupBox.Controls.Add(this.IGNumericUpDown);
			this.PIDGroupBox.Controls.Add(this.PGNumericUpDown);
			this.PIDGroupBox.Controls.Add(this.label14);
			this.PIDGroupBox.Controls.Add(this.label12);
			this.PIDGroupBox.Controls.Add(this.label15);
			this.PIDGroupBox.Location = new System.Drawing.Point(20, 37);
			this.PIDGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.PIDGroupBox.Name = "PIDGroupBox";
			this.PIDGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.PIDGroupBox.Size = new System.Drawing.Size(236, 151);
			this.PIDGroupBox.TabIndex = 76;
			this.PIDGroupBox.TabStop = false;
			this.PIDGroupBox.Text = "PID params";
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
			// 
			// IGNumericUpDown
			// 
			this.IGNumericUpDown.DecimalPlaces = 3;
			this.IGNumericUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
			this.IGNumericUpDown.Location = new System.Drawing.Point(152, 54);
			this.IGNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.IGNumericUpDown.Name = "IGNumericUpDown";
			this.IGNumericUpDown.Size = new System.Drawing.Size(62, 26);
			this.IGNumericUpDown.TabIndex = 62;
			// 
			// PGNumericUpDown
			// 
			this.PGNumericUpDown.DecimalPlaces = 1;
			this.PGNumericUpDown.Location = new System.Drawing.Point(152, 25);
			this.PGNumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.PGNumericUpDown.Name = "PGNumericUpDown";
			this.PGNumericUpDown.Size = new System.Drawing.Size(62, 26);
			this.PGNumericUpDown.TabIndex = 61;
			this.PGNumericUpDown.Value = new decimal(new int[] {
            1,
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
			// StatusTextBox
			// 
			this.StatusTextBox.Location = new System.Drawing.Point(20, 596);
			this.StatusTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.StatusTextBox.Size = new System.Drawing.Size(1039, 109);
			this.StatusTextBox.TabIndex = 79;
			// 
			// DataListView
			// 
			this.DataListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Run,
            this.Pgain,
            this.Igain,
            this.Dgain,
            this.DataFile});
			this.DataListView.FullRowSelect = true;
			this.DataListView.GridLines = true;
			this.DataListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.DataListView.HideSelection = false;
			this.DataListView.Location = new System.Drawing.Point(372, 18);
			this.DataListView.Margin = new System.Windows.Forms.Padding(2);
			this.DataListView.MultiSelect = false;
			this.DataListView.Name = "DataListView";
			this.DataListView.Size = new System.Drawing.Size(687, 188);
			this.DataListView.TabIndex = 80;
			this.DataListView.UseCompatibleStateImageBehavior = false;
			this.DataListView.View = System.Windows.Forms.View.Details;
			this.DataListView.SelectedIndexChanged += new System.EventHandler(this.DataListView_SelectedIndexChanged);
			// 
			// Run
			// 
			this.Run.Text = "Run";
			this.Run.Width = 50;
			// 
			// Pgain
			// 
			this.Pgain.Text = "Prop. gain";
			this.Pgain.Width = 88;
			// 
			// Igain
			// 
			this.Igain.Text = "Int gain";
			this.Igain.Width = 81;
			// 
			// Dgain
			// 
			this.Dgain.Text = "Diff gain";
			this.Dgain.Width = 83;
			// 
			// DataFile
			// 
			this.DataFile.Text = "Data File";
			this.DataFile.Width = 371;
			// 
			// ProfileChart
			// 
			chartArea1.Name = "ChartArea1";
			chartArea1.Position.Auto = false;
			chartArea1.Position.Height = 94F;
			chartArea1.Position.Width = 94F;
			chartArea1.Position.X = 1F;
			chartArea1.Position.Y = 1F;
			this.ProfileChart.ChartAreas.Add(chartArea1);
			legend1.Enabled = false;
			legend1.Name = "Legend1";
			this.ProfileChart.Legends.Add(legend1);
			this.ProfileChart.Location = new System.Drawing.Point(20, 223);
			this.ProfileChart.Margin = new System.Windows.Forms.Padding(2);
			this.ProfileChart.Name = "ProfileChart";
			this.ProfileChart.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.None;
			series1.ChartArea = "ChartArea1";
			series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
			series1.Color = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
			series1.Legend = "Legend1";
			series1.Name = "Series1";
			this.ProfileChart.Series.Add(series1);
			this.ProfileChart.Size = new System.Drawing.Size(1039, 356);
			this.ProfileChart.TabIndex = 81;
			this.ProfileChart.Text = "chart1";
			// 
			// StartButton
			// 
			this.StartButton.Location = new System.Drawing.Point(282, 100);
			this.StartButton.Name = "StartButton";
			this.StartButton.Size = new System.Drawing.Size(75, 25);
			this.StartButton.TabIndex = 82;
			this.StartButton.Text = "Start";
			this.StartButton.UseVisualStyleBackColor = true;
			this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
			// 
			// PersonPIDCalForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(1088, 724);
			this.Controls.Add(this.StartButton);
			this.Controls.Add(this.ProfileChart);
			this.Controls.Add(this.DataListView);
			this.Controls.Add(this.StatusTextBox);
			this.Controls.Add(this.PIDGroupBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PersonPIDCalForm";
			this.ShowIcon = false;
			this.Text = "Person Move PID Controller Calibration";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PersonPIDCalForm_FormClosing);
			this.PIDGroupBox.ResumeLayout(false);
			this.PIDGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.DGNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.IGNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.PGNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.ProfileChart)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.GroupBox PIDGroupBox;
		private System.Windows.Forms.CheckBox PPCheckBox;
		private System.Windows.Forms.NumericUpDown DGNumericUpDown;
		private System.Windows.Forms.NumericUpDown IGNumericUpDown;
		private System.Windows.Forms.NumericUpDown PGNumericUpDown;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.ListView DataListView;
		private System.Windows.Forms.ColumnHeader Run;
		private System.Windows.Forms.ColumnHeader Pgain;
		private System.Windows.Forms.ColumnHeader Igain;
		private System.Windows.Forms.ColumnHeader Dgain;
		private System.Windows.Forms.ColumnHeader DataFile;
		private System.Windows.Forms.DataVisualization.Charting.Chart ProfileChart;
		private System.Windows.Forms.Button StartButton;
		}
	}

