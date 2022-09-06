namespace RemoteUserInterface
	{
	partial class MainForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.ToolsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.label1 = new System.Windows.Forms.Label();
			this.MainCntrl = new RemoteUserInterface.MainControl();
			this.SuspendLayout();
			// 
			// ToolsFlowLayoutPanel
			// 
			this.ToolsFlowLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ToolsFlowLayoutPanel.AutoScroll = true;
			this.ToolsFlowLayoutPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.ToolsFlowLayoutPanel.Location = new System.Drawing.Point(12, 804);
			this.ToolsFlowLayoutPanel.Name = "ToolsFlowLayoutPanel";
			this.ToolsFlowLayoutPanel.Size = new System.Drawing.Size(1461, 44);
			this.ToolsFlowLayoutPanel.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Enabled = false;
			this.label1.Location = new System.Drawing.Point(11, 780);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(47, 20);
			this.label1.TabIndex = 1;
			this.label1.Text = "Tools";
			// 
			// MainCntrl
			// 
			this.MainCntrl.Dock = System.Windows.Forms.DockStyle.Top;
			this.MainCntrl.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MainCntrl.Location = new System.Drawing.Point(0, 0);
			this.MainCntrl.Name = "MainCntrl";
			this.MainCntrl.Size = new System.Drawing.Size(1484, 773);
			this.MainCntrl.TabIndex = 0;
			// 
			// MainForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(1484, 860);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.ToolsFlowLayoutPanel);
			this.Controls.Add(this.MainCntrl);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.Text = "Remote Admin Interface";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private MainControl MainCntrl;
		private System.Windows.Forms.FlowLayoutPanel ToolsFlowLayoutPanel;
		private System.Windows.Forms.Label label1;
		}
	}

