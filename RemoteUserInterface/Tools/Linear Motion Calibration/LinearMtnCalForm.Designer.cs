namespace Linear_Motion_Calibration
	{
	partial class LinearMtnCalForm
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
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.BasicParamCntl = new Linear_Motion_Calibration.BasicParamControl();
			this.MotionTestCntl = new Linear_Motion_Calibration.MotionTestControl();
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
			this.tabControl1.Size = new System.Drawing.Size(1492, 537);
			this.tabControl1.TabIndex = 0;
			this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.AboutTextBox);
			this.tabPage1.Location = new System.Drawing.Point(4, 4);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(1484, 504);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "About";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// AboutTextBox
			// 
			this.AboutTextBox.Location = new System.Drawing.Point(541, 146);
			this.AboutTextBox.Multiline = true;
			this.AboutTextBox.Name = "AboutTextBox";
			this.AboutTextBox.Size = new System.Drawing.Size(403, 212);
			this.AboutTextBox.TabIndex = 1;
			this.AboutTextBox.Text = "Linear Motion Calibration";
			this.AboutTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.BasicParamCntl);
			this.tabPage2.Location = new System.Drawing.Point(4, 4);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(1484, 504);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Basic Parameters";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.MotionTestCntl);
			this.tabPage3.Location = new System.Drawing.Point(4, 4);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Size = new System.Drawing.Size(1484, 504);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "Motion Test";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// BasicParamCntl
			// 
			this.BasicParamCntl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.BasicParamCntl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.BasicParamCntl.Location = new System.Drawing.Point(3, 3);
			this.BasicParamCntl.Name = "BasicParamCntl";
			this.BasicParamCntl.Size = new System.Drawing.Size(1478, 498);
			this.BasicParamCntl.TabIndex = 0;
			// 
			// MotionTestCntl
			// 
			this.MotionTestCntl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MotionTestCntl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MotionTestCntl.Location = new System.Drawing.Point(0, 0);
			this.MotionTestCntl.Name = "MotionTestCntl";
			this.MotionTestCntl.Size = new System.Drawing.Size(1484, 504);
			this.MotionTestCntl.TabIndex = 0;
			// 
			// LinearMtnCalForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(1492, 537);
			this.Controls.Add(this.tabControl1);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LinearMtnCalForm";
			this.ShowIcon = false;
			this.Text = "Linear Motion Calibration";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LinearMtnCalForm_FormClosing);
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.tabPage2.ResumeLayout(false);
			this.tabPage3.ResumeLayout(false);
			this.ResumeLayout(false);

			}

		#endregion

		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.TextBox AboutTextBox;
		private BasicParamControl BasicParamCntl;
		private System.Windows.Forms.TabPage tabPage3;
		private MotionTestControl MotionTestCntl;
		}
	}