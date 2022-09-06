namespace Sub_system_Operation
	{
	partial class SubSystemOpForm
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
			this.tabPage4 = new System.Windows.Forms.TabPage();
			this.tabPage5 = new System.Windows.Forms.TabPage();
			this.HdAssembly = new Sub_system_Operation.HeadAssemblyOp();
			this.KinectOpCtl = new Sub_system_Operation.KinectOp();
			this.RoboticArmOpCntl = new Sub_system_Operation.RoboticArmOp();
			this.SonarOpCntl = new Sub_system_Operation.SonarOp();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.tabPage4.SuspendLayout();
			this.tabPage5.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Alignment = System.Windows.Forms.TabAlignment.Bottom;
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Controls.Add(this.tabPage4);
			this.tabControl1.Controls.Add(this.tabPage5);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(1276, 841);
			this.tabControl1.TabIndex = 0;
			this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.AboutTextBox);
			this.tabPage1.Location = new System.Drawing.Point(4, 4);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(1268, 808);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "About";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// AboutTextBox
			// 
			this.AboutTextBox.Location = new System.Drawing.Point(433, 283);
			this.AboutTextBox.Multiline = true;
			this.AboutTextBox.Name = "AboutTextBox";
			this.AboutTextBox.Size = new System.Drawing.Size(403, 212);
			this.AboutTextBox.TabIndex = 0;
			this.AboutTextBox.Text = "Sub-system Operation/Diagnostics";
			this.AboutTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.HdAssembly);
			this.tabPage2.Location = new System.Drawing.Point(4, 4);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(1268, 808);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Head Assembly";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.KinectOpCtl);
			this.tabPage3.Location = new System.Drawing.Point(4, 4);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Size = new System.Drawing.Size(1268, 808);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "Kinect";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// tabPage4
			// 
			this.tabPage4.Controls.Add(this.RoboticArmOpCntl);
			this.tabPage4.Location = new System.Drawing.Point(4, 4);
			this.tabPage4.Name = "tabPage4";
			this.tabPage4.Size = new System.Drawing.Size(1268, 808);
			this.tabPage4.TabIndex = 3;
			this.tabPage4.Text = "Arm";
			this.tabPage4.UseVisualStyleBackColor = true;
			// 
			// tabPage5
			// 
			this.tabPage5.Controls.Add(this.SonarOpCntl);
			this.tabPage5.Location = new System.Drawing.Point(4, 4);
			this.tabPage5.Name = "tabPage5";
			this.tabPage5.Size = new System.Drawing.Size(1268, 808);
			this.tabPage5.TabIndex = 4;
			this.tabPage5.Text = "Sonars";
			this.tabPage5.UseVisualStyleBackColor = true;
			// 
			// HdAssembly
			// 
			this.HdAssembly.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.HdAssembly.Location = new System.Drawing.Point(246, 22);
			this.HdAssembly.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.HdAssembly.Name = "HdAssembly";
			this.HdAssembly.Size = new System.Drawing.Size(777, 476);
			this.HdAssembly.TabIndex = 0;
			// 
			// KinectOpCtl
			// 
			this.KinectOpCtl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.KinectOpCtl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.KinectOpCtl.Location = new System.Drawing.Point(0, 0);
			this.KinectOpCtl.Name = "KinectOpCtl";
			this.KinectOpCtl.Size = new System.Drawing.Size(1268, 808);
			this.KinectOpCtl.TabIndex = 0;
			// 
			// RoboticArmOpCntl
			// 
			this.RoboticArmOpCntl.Enabled = false;
			this.RoboticArmOpCntl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.RoboticArmOpCntl.Location = new System.Drawing.Point(330, 77);
			this.RoboticArmOpCntl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.RoboticArmOpCntl.Name = "RoboticArmOpCntl";
			this.RoboticArmOpCntl.Size = new System.Drawing.Size(608, 654);
			this.RoboticArmOpCntl.TabIndex = 0;
			// 
			// SonarOpCntl
			// 
			this.SonarOpCntl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SonarOpCntl.Location = new System.Drawing.Point(185, 137);
			this.SonarOpCntl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.SonarOpCntl.Name = "SonarOpCntl";
			this.SonarOpCntl.Size = new System.Drawing.Size(899, 534);
			this.SonarOpCntl.TabIndex = 0;
			// 
			// SubSystemOpForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1276, 841);
			this.Controls.Add(this.tabControl1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SubSystemOpForm";
			this.ShowIcon = false;
			this.Text = "Sub-system Operations/Diagnostics";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SubSystemOpForm_FormClosing);
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.tabPage2.ResumeLayout(false);
			this.tabPage3.ResumeLayout(false);
			this.tabPage4.ResumeLayout(false);
			this.tabPage5.ResumeLayout(false);
			this.ResumeLayout(false);

			}

		#endregion

		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.TextBox AboutTextBox;
		private HeadAssemblyOp HdAssembly;
		private System.Windows.Forms.TabPage tabPage3;
		private KinectOp KinectOpCtl;
		private System.Windows.Forms.TabPage tabPage4;
		private RoboticArmOp RoboticArmOpCntl;
		private System.Windows.Forms.TabPage tabPage5;
		private SonarOp SonarOpCntl;
		}
	}