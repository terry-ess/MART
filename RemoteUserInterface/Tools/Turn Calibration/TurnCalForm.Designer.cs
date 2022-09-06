namespace Turn_Calibration
	{
	partial class TurnCalForm
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
			this.BasicParamCntl = new Turn_Calibration.BasicParamControl();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.TurnTestCtrl = new Turn_Calibration.TurnTestControl();
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
			this.tabControl1.Size = new System.Drawing.Size(1530, 563);
			this.tabControl1.TabIndex = 0;
			this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.AboutTextBox);
			this.tabPage1.Location = new System.Drawing.Point(4, 4);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(1522, 530);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "About";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// AboutTextBox
			// 
			this.AboutTextBox.Location = new System.Drawing.Point(535, 159);
			this.AboutTextBox.Multiline = true;
			this.AboutTextBox.Name = "AboutTextBox";
			this.AboutTextBox.Size = new System.Drawing.Size(403, 212);
			this.AboutTextBox.TabIndex = 2;
			this.AboutTextBox.Text = "Turn Calibration";
			this.AboutTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.BasicParamCntl);
			this.tabPage2.Location = new System.Drawing.Point(4, 4);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(1522, 530);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Basic Parameters";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// BasicParamCntl
			// 
			this.BasicParamCntl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.BasicParamCntl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.BasicParamCntl.Location = new System.Drawing.Point(3, 3);
			this.BasicParamCntl.Name = "BasicParamCntl";
			this.BasicParamCntl.Size = new System.Drawing.Size(1516, 524);
			this.BasicParamCntl.TabIndex = 0;
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.TurnTestCtrl);
			this.tabPage3.Location = new System.Drawing.Point(4, 4);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Size = new System.Drawing.Size(1522, 530);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "Turn Test";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// TurnTestCtrl
			// 
			this.TurnTestCtrl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TurnTestCtrl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TurnTestCtrl.Location = new System.Drawing.Point(0, 0);
			this.TurnTestCtrl.Name = "TurnTestCtrl";
			this.TurnTestCtrl.Size = new System.Drawing.Size(1522, 530);
			this.TurnTestCtrl.TabIndex = 0;
			// 
			// TurnCalForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(1530, 563);
			this.Controls.Add(this.tabControl1);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.Name = "TurnCalForm";
			this.Text = "Turn Calibration";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TurnCalForm_FormClosing);
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
		private TurnTestControl TurnTestCtrl;
		}
	}

