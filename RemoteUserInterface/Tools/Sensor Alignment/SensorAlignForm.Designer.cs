namespace Sensor_Alignment
	{
	partial class SensorAlignForm
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
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.AboutTextBox = new System.Windows.Forms.TextBox();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.HeadTiltAlignCntrl = new Sensor_Alignment.HeadTiltAlignControl();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.SensorAlignCntrl = new Sensor_Alignment.SensorAlignControl();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Alignment = System.Windows.Forms.TabAlignment.Bottom;
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(1584, 841);
			this.tabControl1.TabIndex = 0;
			this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.AboutTextBox);
			this.tabPage1.Location = new System.Drawing.Point(4, 4);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(1576, 808);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "About";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// AboutTextBox
			// 
			this.AboutTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.AboutTextBox.Location = new System.Drawing.Point(483, 352);
			this.AboutTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.AboutTextBox.Multiline = true;
			this.AboutTextBox.Name = "AboutTextBox";
			this.AboutTextBox.Size = new System.Drawing.Size(510, 110);
			this.AboutTextBox.TabIndex = 1;
			this.AboutTextBox.Text = "Sensor Alignment/Calibration Using Alignment Jig";
			this.AboutTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.HeadTiltAlignCntrl);
			this.tabPage2.Location = new System.Drawing.Point(4, 4);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(1576, 815);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Head Tilt";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// HeadTiltAlignCntrl
			// 
			this.HeadTiltAlignCntrl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.HeadTiltAlignCntrl.Location = new System.Drawing.Point(135, 95);
			this.HeadTiltAlignCntrl.Name = "HeadTiltAlignCntrl";
			this.HeadTiltAlignCntrl.Size = new System.Drawing.Size(1206, 625);
			this.HeadTiltAlignCntrl.TabIndex = 0;
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.SensorAlignCntrl);
			this.tabPage3.Location = new System.Drawing.Point(4, 4);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage3.Size = new System.Drawing.Size(1576, 808);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "Visual sensor align";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// SensorAlignCntrl
			// 
			this.SensorAlignCntrl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SensorAlignCntrl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SensorAlignCntrl.Location = new System.Drawing.Point(3, 3);
			this.SensorAlignCntrl.Name = "SensorAlignCntrl";
			this.SensorAlignCntrl.Size = new System.Drawing.Size(1570, 802);
			this.SensorAlignCntrl.TabIndex = 0;
			// 
			// SensorAlignForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(1584, 841);
			this.Controls.Add(this.tabControl1);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SensorAlignForm";
			this.ShowIcon = false;
			this.Text = "Sensor Alignment";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SensorAlignForm_FormClosing);
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.tabPage2.ResumeLayout(false);
			this.tabPage3.ResumeLayout(false);
			this.ResumeLayout(false);

			}

		#endregion

		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.TabPage tabPage1;
		private HeadTiltAlignControl HeadTiltAlignCntrl;
		private System.Windows.Forms.TextBox AboutTextBox;
		private System.Windows.Forms.TabPage tabPage3;
		private SensorAlignControl SensorAlignCntrl;
		}
	}