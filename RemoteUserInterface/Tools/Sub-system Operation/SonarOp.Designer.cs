namespace Sub_system_Operation
	{
	partial class SonarOp
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
			this.ProfileChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
			this.FRadioButton = new System.Windows.Forms.RadioButton();
			this.RRadioButton = new System.Windows.Forms.RadioButton();
			this.RecButton = new System.Windows.Forms.Button();
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.RTNumericUpDown = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.ProfileChart)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.RTNumericUpDown)).BeginInit();
			this.SuspendLayout();
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
			this.ProfileChart.Location = new System.Drawing.Point(9, 261);
			this.ProfileChart.Name = "ProfileChart";
			this.ProfileChart.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.None;
			series1.ChartArea = "ChartArea1";
			series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
			series1.Color = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
			series1.Legend = "Legend1";
			series1.Name = "Series1";
			this.ProfileChart.Series.Add(series1);
			this.ProfileChart.Size = new System.Drawing.Size(880, 256);
			this.ProfileChart.TabIndex = 82;
			this.ProfileChart.Text = "chart1";
			// 
			// FRadioButton
			// 
			this.FRadioButton.AutoSize = true;
			this.FRadioButton.Checked = true;
			this.FRadioButton.Location = new System.Drawing.Point(339, 15);
			this.FRadioButton.Name = "FRadioButton";
			this.FRadioButton.Size = new System.Drawing.Size(65, 24);
			this.FRadioButton.TabIndex = 83;
			this.FRadioButton.TabStop = true;
			this.FRadioButton.Text = "Front";
			this.FRadioButton.UseVisualStyleBackColor = true;
			// 
			// RRadioButton
			// 
			this.RRadioButton.AutoSize = true;
			this.RRadioButton.Location = new System.Drawing.Point(497, 15);
			this.RRadioButton.Name = "RRadioButton";
			this.RRadioButton.Size = new System.Drawing.Size(62, 24);
			this.RRadioButton.TabIndex = 84;
			this.RRadioButton.Text = "Rear";
			this.RRadioButton.UseVisualStyleBackColor = true;
			// 
			// RecButton
			// 
			this.RecButton.BackColor = System.Drawing.SystemColors.Control;
			this.RecButton.Location = new System.Drawing.Point(412, 81);
			this.RecButton.Name = "RecButton";
			this.RecButton.Size = new System.Drawing.Size(75, 34);
			this.RecButton.TabIndex = 85;
			this.RecButton.Text = "Record";
			this.RecButton.UseVisualStyleBackColor = false;
			this.RecButton.Click += new System.EventHandler(this.RecButton_Click);
			// 
			// StatusTextBox
			// 
			this.StatusTextBox.BackColor = System.Drawing.Color.White;
			this.StatusTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.StatusTextBox.Location = new System.Drawing.Point(9, 126);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ReadOnly = true;
			this.StatusTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.StatusTextBox.Size = new System.Drawing.Size(880, 117);
			this.StatusTextBox.TabIndex = 86;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(342, 48);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(134, 20);
			this.label1.TabIndex = 87;
			this.label1.Text = "Record time (min)";
			// 
			// RTNumericUpDown
			// 
			this.RTNumericUpDown.Location = new System.Drawing.Point(478, 46);
			this.RTNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.RTNumericUpDown.Name = "RTNumericUpDown";
			this.RTNumericUpDown.Size = new System.Drawing.Size(78, 26);
			this.RTNumericUpDown.TabIndex = 88;
			this.RTNumericUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// SonarOp
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.RTNumericUpDown);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.StatusTextBox);
			this.Controls.Add(this.RecButton);
			this.Controls.Add(this.RRadioButton);
			this.Controls.Add(this.FRadioButton);
			this.Controls.Add(this.ProfileChart);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.Name = "SonarOp";
			this.Size = new System.Drawing.Size(899, 534);
			((System.ComponentModel.ISupportInitialize)(this.ProfileChart)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.RTNumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.DataVisualization.Charting.Chart ProfileChart;
		private System.Windows.Forms.RadioButton FRadioButton;
		private System.Windows.Forms.RadioButton RRadioButton;
		private System.Windows.Forms.Button RecButton;
		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown RTNumericUpDown;
		}
	}
