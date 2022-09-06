namespace Sub_system_Operation
	{
	partial class KinectOp
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
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage4 = new System.Windows.Forms.TabPage();
			this.AboutTextBox = new System.Windows.Forms.TextBox();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.KinectDistCalCtl = new Sub_system_Operation.KinectDistCal();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.KinectMappingCntl = new Sub_system_Operation.KinectMapping();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.TargetCalCntl = new Sub_system_Operation.KinectTargetCal();
			this.tabControl1.SuspendLayout();
			this.tabPage4.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Alignment = System.Windows.Forms.TabAlignment.Bottom;
			this.tabControl1.Controls.Add(this.tabPage4);
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(1200, 802);
			this.tabControl1.TabIndex = 0;
			this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
			// 
			// tabPage4
			// 
			this.tabPage4.Controls.Add(this.AboutTextBox);
			this.tabPage4.Location = new System.Drawing.Point(4, 4);
			this.tabPage4.Name = "tabPage4";
			this.tabPage4.Size = new System.Drawing.Size(1192, 769);
			this.tabPage4.TabIndex = 3;
			this.tabPage4.Text = "About";
			this.tabPage4.UseVisualStyleBackColor = true;
			// 
			// AboutTextBox
			// 
			this.AboutTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.AboutTextBox.Location = new System.Drawing.Point(341, 329);
			this.AboutTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.AboutTextBox.Multiline = true;
			this.AboutTextBox.Name = "AboutTextBox";
			this.AboutTextBox.Size = new System.Drawing.Size(510, 110);
			this.AboutTextBox.TabIndex = 2;
			this.AboutTextBox.Text = "Kinect Operations and Calibration";
			this.AboutTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.KinectDistCalCtl);
			this.tabPage1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.tabPage1.Location = new System.Drawing.Point(4, 4);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(1192, 776);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Distance calibartion";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// KinectDistCalCtl
			// 
			this.KinectDistCalCtl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.KinectDistCalCtl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.KinectDistCalCtl.Location = new System.Drawing.Point(3, 3);
			this.KinectDistCalCtl.Name = "KinectDistCalCtl";
			this.KinectDistCalCtl.Size = new System.Drawing.Size(1186, 770);
			this.KinectDistCalCtl.TabIndex = 0;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.KinectMappingCntl);
			this.tabPage2.Location = new System.Drawing.Point(4, 4);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Size = new System.Drawing.Size(1192, 776);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Mapping";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// KinectMappingCntl
			// 
			this.KinectMappingCntl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.KinectMappingCntl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.KinectMappingCntl.Location = new System.Drawing.Point(0, 0);
			this.KinectMappingCntl.Name = "KinectMappingCntl";
			this.KinectMappingCntl.Size = new System.Drawing.Size(1192, 776);
			this.KinectMappingCntl.TabIndex = 0;
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.TargetCalCntl);
			this.tabPage3.Location = new System.Drawing.Point(4, 4);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Size = new System.Drawing.Size(1192, 769);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "Target calibration";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// TargetCalCntl
			// 
			this.TargetCalCntl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TargetCalCntl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TargetCalCntl.Location = new System.Drawing.Point(0, 0);
			this.TargetCalCntl.Name = "TargetCalCntl";
			this.TargetCalCntl.Size = new System.Drawing.Size(1192, 769);
			this.TargetCalCntl.TabIndex = 0;
			// 
			// KinectOp
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.tabControl1);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "KinectOp";
			this.Size = new System.Drawing.Size(1200, 802);
			this.tabControl1.ResumeLayout(false);
			this.tabPage4.ResumeLayout(false);
			this.tabPage4.PerformLayout();
			this.tabPage1.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.tabPage3.ResumeLayout(false);
			this.ResumeLayout(false);

			}

		#endregion

		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private KinectDistCal KinectDistCalCtl;
		private System.Windows.Forms.TabPage tabPage2;
		private KinectMapping KinectMappingCntl;
		private System.Windows.Forms.TabPage tabPage3;
		private KinectTargetCal TargetCalCntl;
		private System.Windows.Forms.TabPage tabPage4;
		private System.Windows.Forms.TextBox AboutTextBox;
		}
	}
