namespace RemoteUserInterface
	{
	partial class MainControl
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
			this.VideoGroupBox = new System.Windows.Forms.GroupBox();
			this.VidSSButton = new System.Windows.Forms.Button();
			this.VideoPictureBox = new System.Windows.Forms.PictureBox();
			this.StatGroupBox = new System.Windows.Forms.GroupBox();
			this.SDOCheckBox = new System.Windows.Forms.CheckBox();
			this.SRECheckBox = new System.Windows.Forms.CheckBox();
			this.VODCheckBox = new System.Windows.Forms.CheckBox();
			this.ACheckBox = new System.Windows.Forms.CheckBox();
			this.RLDCheckBox = new System.Windows.Forms.CheckBox();
			this.HostTextBox = new System.Windows.Forms.TextBox();
			this.SSSButton = new System.Windows.Forms.Button();
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.VoltTextBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.CTCCheckBox = new System.Windows.Forms.CheckBox();
			this.FLDCheckBox = new System.Windows.Forms.CheckBox();
			this.NDBCheckBox = new System.Windows.Forms.CheckBox();
			this.KCheckBox = new System.Windows.Forms.CheckBox();
			this.HACheckBox = new System.Windows.Forms.CheckBox();
			this.LocGroupBox = new System.Windows.Forms.GroupBox();
			this.SLButton = new System.Windows.Forms.Button();
			this.PoseTextBox = new System.Windows.Forms.TextBox();
			this.LocTextBox = new System.Windows.Forms.TextBox();
			this.LocSSButton = new System.Windows.Forms.Button();
			this.MapPictureBox = new System.Windows.Forms.PictureBox();
			this.ActGroupBox = new System.Windows.Forms.GroupBox();
			this.ActSSButton = new System.Windows.Forms.Button();
			this.ActTextBox = new System.Windows.Forms.TextBox();
			this.RecCheckBox = new System.Windows.Forms.CheckBox();
			this.RSButton = new System.Windows.Forms.Button();
			this.SDButton = new System.Windows.Forms.Button();
			this.SUButton = new System.Windows.Forms.Button();
			this.ESButton = new System.Windows.Forms.Button();
			this.EXButton = new System.Windows.Forms.Button();
			this.HDButton = new System.Windows.Forms.Button();
			this.VideoGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).BeginInit();
			this.StatGroupBox.SuspendLayout();
			this.LocGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MapPictureBox)).BeginInit();
			this.ActGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// VideoGroupBox
			// 
			this.VideoGroupBox.Controls.Add(this.VidSSButton);
			this.VideoGroupBox.Controls.Add(this.VideoPictureBox);
			this.VideoGroupBox.Enabled = false;
			this.VideoGroupBox.Location = new System.Drawing.Point(433, 3);
			this.VideoGroupBox.Name = "VideoGroupBox";
			this.VideoGroupBox.Size = new System.Drawing.Size(486, 420);
			this.VideoGroupBox.TabIndex = 0;
			this.VideoGroupBox.TabStop = false;
			this.VideoGroupBox.Text = "Video";
			// 
			// VidSSButton
			// 
			this.VidSSButton.BackColor = System.Drawing.Color.LightGreen;
			this.VidSSButton.Location = new System.Drawing.Point(195, 364);
			this.VidSSButton.Name = "VidSSButton";
			this.VidSSButton.Size = new System.Drawing.Size(96, 34);
			this.VidSSButton.TabIndex = 1;
			this.VidSSButton.Text = "Start";
			this.VidSSButton.UseVisualStyleBackColor = false;
			this.VidSSButton.Click += new System.EventHandler(this.VidSSButton_Click);
			// 
			// VideoPictureBox
			// 
			this.VideoPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.VideoPictureBox.Location = new System.Drawing.Point(30, 24);
			this.VideoPictureBox.Name = "VideoPictureBox";
			this.VideoPictureBox.Size = new System.Drawing.Size(427, 320);
			this.VideoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.VideoPictureBox.TabIndex = 0;
			this.VideoPictureBox.TabStop = false;
			// 
			// StatGroupBox
			// 
			this.StatGroupBox.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.StatGroupBox.Controls.Add(this.SDOCheckBox);
			this.StatGroupBox.Controls.Add(this.SRECheckBox);
			this.StatGroupBox.Controls.Add(this.VODCheckBox);
			this.StatGroupBox.Controls.Add(this.ACheckBox);
			this.StatGroupBox.Controls.Add(this.RLDCheckBox);
			this.StatGroupBox.Controls.Add(this.HostTextBox);
			this.StatGroupBox.Controls.Add(this.SSSButton);
			this.StatGroupBox.Controls.Add(this.StatusTextBox);
			this.StatGroupBox.Controls.Add(this.VoltTextBox);
			this.StatGroupBox.Controls.Add(this.label1);
			this.StatGroupBox.Controls.Add(this.CTCCheckBox);
			this.StatGroupBox.Controls.Add(this.FLDCheckBox);
			this.StatGroupBox.Controls.Add(this.NDBCheckBox);
			this.StatGroupBox.Controls.Add(this.KCheckBox);
			this.StatGroupBox.Controls.Add(this.HACheckBox);
			this.StatGroupBox.Enabled = false;
			this.StatGroupBox.Location = new System.Drawing.Point(13, 3);
			this.StatGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.StatGroupBox.Name = "StatGroupBox";
			this.StatGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.StatGroupBox.Size = new System.Drawing.Size(380, 420);
			this.StatGroupBox.TabIndex = 5;
			this.StatGroupBox.TabStop = false;
			this.StatGroupBox.Text = "Operational Status";
			// 
			// SDOCheckBox
			// 
			this.SDOCheckBox.AutoSize = true;
			this.SDOCheckBox.Enabled = false;
			this.SDOCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SDOCheckBox.Location = new System.Drawing.Point(75, 307);
			this.SDOCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.SDOCheckBox.Name = "SDOCheckBox";
			this.SDOCheckBox.Size = new System.Drawing.Size(230, 24);
			this.SDOCheckBox.TabIndex = 22;
			this.SDOCheckBox.Text = "Speech direction operational";
			this.SDOCheckBox.UseVisualStyleBackColor = true;
			// 
			// SRECheckBox
			// 
			this.SRECheckBox.AutoSize = true;
			this.SRECheckBox.Enabled = false;
			this.SRECheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SRECheckBox.Location = new System.Drawing.Point(85, 279);
			this.SRECheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.SRECheckBox.Name = "SRECheckBox";
			this.SRECheckBox.Size = new System.Drawing.Size(210, 24);
			this.SRECheckBox.TabIndex = 21;
			this.SRECheckBox.Text = "Speech recognition active";
			this.SRECheckBox.UseVisualStyleBackColor = true;
			// 
			// VODCheckBox
			// 
			this.VODCheckBox.AutoSize = true;
			this.VODCheckBox.Enabled = false;
			this.VODCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.VODCheckBox.Location = new System.Drawing.Point(55, 223);
			this.VODCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.VODCheckBox.Name = "VODCheckBox";
			this.VODCheckBox.Size = new System.Drawing.Size(271, 24);
			this.VODCheckBox.TabIndex = 20;
			this.VODCheckBox.Text = "Visual object detection operational";
			this.VODCheckBox.UseVisualStyleBackColor = true;
			// 
			// ACheckBox
			// 
			this.ACheckBox.AutoSize = true;
			this.ACheckBox.Enabled = false;
			this.ACheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ACheckBox.Location = new System.Drawing.Point(120, 167);
			this.ACheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.ACheckBox.Name = "ACheckBox";
			this.ACheckBox.Size = new System.Drawing.Size(140, 24);
			this.ACheckBox.TabIndex = 19;
			this.ACheckBox.Text = "Arm operational";
			this.ACheckBox.UseVisualStyleBackColor = true;
			// 
			// RLDCheckBox
			// 
			this.RLDCheckBox.AutoSize = true;
			this.RLDCheckBox.Enabled = false;
			this.RLDCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.RLDCheckBox.Location = new System.Drawing.Point(91, 139);
			this.RLDCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.RLDCheckBox.Name = "RLDCheckBox";
			this.RLDCheckBox.Size = new System.Drawing.Size(199, 24);
			this.RLDCheckBox.TabIndex = 18;
			this.RLDCheckBox.Text = "Rear LIDAR operational";
			this.RLDCheckBox.UseVisualStyleBackColor = true;
			// 
			// HostTextBox
			// 
			this.HostTextBox.Location = new System.Drawing.Point(65, 29);
			this.HostTextBox.Name = "HostTextBox";
			this.HostTextBox.Size = new System.Drawing.Size(69, 26);
			this.HostTextBox.TabIndex = 17;
			this.HostTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// SSSButton
			// 
			this.SSSButton.BackColor = System.Drawing.Color.LightGreen;
			this.SSSButton.Location = new System.Drawing.Point(142, 359);
			this.SSSButton.Name = "SSSButton";
			this.SSSButton.Size = new System.Drawing.Size(96, 34);
			this.SSSButton.TabIndex = 16;
			this.SSSButton.Text = "Start";
			this.SSSButton.UseVisualStyleBackColor = false;
			this.SSSButton.Click += new System.EventHandler(this.SSSButton_Click);
			// 
			// StatusTextBox
			// 
			this.StatusTextBox.Location = new System.Drawing.Point(155, 29);
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.Size = new System.Drawing.Size(160, 26);
			this.StatusTextBox.TabIndex = 15;
			this.StatusTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// VoltTextBox
			// 
			this.VoltTextBox.Enabled = false;
			this.VoltTextBox.Location = new System.Drawing.Point(231, 332);
			this.VoltTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.VoltTextBox.Name = "VoltTextBox";
			this.VoltTextBox.Size = new System.Drawing.Size(50, 26);
			this.VoltTextBox.TabIndex = 12;
			this.VoltTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Enabled = false;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(99, 335);
			this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(132, 20);
			this.label1.TabIndex = 11;
			this.label1.Text = "Main battery volts";
			// 
			// CTCCheckBox
			// 
			this.CTCCheckBox.AutoSize = true;
			this.CTCCheckBox.Enabled = false;
			this.CTCCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CTCCheckBox.Location = new System.Drawing.Point(76, 251);
			this.CTCCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.CTCCheckBox.Name = "CTCCheckBox";
			this.CTCCheckBox.Size = new System.Drawing.Size(228, 24);
			this.CTCCheckBox.TabIndex = 8;
			this.CTCCheckBox.Text = "Motion controller operational";
			this.CTCCheckBox.UseVisualStyleBackColor = true;
			// 
			// FLDCheckBox
			// 
			this.FLDCheckBox.AutoSize = true;
			this.FLDCheckBox.Enabled = false;
			this.FLDCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FLDCheckBox.Location = new System.Drawing.Point(89, 111);
			this.FLDCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.FLDCheckBox.Name = "FLDCheckBox";
			this.FLDCheckBox.Size = new System.Drawing.Size(202, 24);
			this.FLDCheckBox.TabIndex = 10;
			this.FLDCheckBox.Text = "Front LIDAR operational";
			this.FLDCheckBox.UseVisualStyleBackColor = true;
			// 
			// NDBCheckBox
			// 
			this.NDBCheckBox.AutoSize = true;
			this.NDBCheckBox.Enabled = false;
			this.NDBCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.NDBCheckBox.Location = new System.Drawing.Point(86, 195);
			this.NDBCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.NDBCheckBox.Name = "NDBCheckBox";
			this.NDBCheckBox.Size = new System.Drawing.Size(208, 24);
			this.NDBCheckBox.TabIndex = 9;
			this.NDBCheckBox.Text = "Navigation DB connected";
			this.NDBCheckBox.UseVisualStyleBackColor = true;
			// 
			// KCheckBox
			// 
			this.KCheckBox.Enabled = false;
			this.KCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.KCheckBox.Location = new System.Drawing.Point(112, 87);
			this.KCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.KCheckBox.Name = "KCheckBox";
			this.KCheckBox.Size = new System.Drawing.Size(156, 20);
			this.KCheckBox.TabIndex = 6;
			this.KCheckBox.Text = "Kinect operational";
			this.KCheckBox.UseVisualStyleBackColor = true;
			// 
			// HACheckBox
			// 
			this.HACheckBox.AutoSize = true;
			this.HACheckBox.Enabled = false;
			this.HACheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.HACheckBox.Location = new System.Drawing.Point(80, 59);
			this.HACheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.HACheckBox.Name = "HACheckBox";
			this.HACheckBox.Size = new System.Drawing.Size(220, 24);
			this.HACheckBox.TabIndex = 7;
			this.HACheckBox.Text = "Head assembly operational";
			this.HACheckBox.UseVisualStyleBackColor = true;
			// 
			// LocGroupBox
			// 
			this.LocGroupBox.Controls.Add(this.SLButton);
			this.LocGroupBox.Controls.Add(this.PoseTextBox);
			this.LocGroupBox.Controls.Add(this.LocTextBox);
			this.LocGroupBox.Controls.Add(this.LocSSButton);
			this.LocGroupBox.Controls.Add(this.MapPictureBox);
			this.LocGroupBox.Location = new System.Drawing.Point(959, 3);
			this.LocGroupBox.Name = "LocGroupBox";
			this.LocGroupBox.Size = new System.Drawing.Size(520, 757);
			this.LocGroupBox.TabIndex = 6;
			this.LocGroupBox.TabStop = false;
			this.LocGroupBox.Text = "Location";
			// 
			// SLButton
			// 
			this.SLButton.BackColor = System.Drawing.SystemColors.Control;
			this.SLButton.Location = new System.Drawing.Point(201, 658);
			this.SLButton.Name = "SLButton";
			this.SLButton.Size = new System.Drawing.Size(119, 34);
			this.SLButton.TabIndex = 4;
			this.SLButton.Text = "Set Location";
			this.SLButton.UseVisualStyleBackColor = false;
			this.SLButton.Click += new System.EventHandler(this.SLButton_Click);
			// 
			// PoseTextBox
			// 
			this.PoseTextBox.BackColor = System.Drawing.Color.White;
			this.PoseTextBox.Location = new System.Drawing.Point(23, 620);
			this.PoseTextBox.Name = "PoseTextBox";
			this.PoseTextBox.ReadOnly = true;
			this.PoseTextBox.Size = new System.Drawing.Size(474, 26);
			this.PoseTextBox.TabIndex = 3;
			// 
			// LocTextBox
			// 
			this.LocTextBox.BackColor = System.Drawing.Color.White;
			this.LocTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.LocTextBox.Location = new System.Drawing.Point(23, 437);
			this.LocTextBox.Multiline = true;
			this.LocTextBox.Name = "LocTextBox";
			this.LocTextBox.ReadOnly = true;
			this.LocTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.LocTextBox.Size = new System.Drawing.Size(474, 171);
			this.LocTextBox.TabIndex = 2;
			this.LocTextBox.WordWrap = false;
			// 
			// LocSSButton
			// 
			this.LocSSButton.BackColor = System.Drawing.Color.LightGreen;
			this.LocSSButton.Enabled = false;
			this.LocSSButton.Location = new System.Drawing.Point(212, 704);
			this.LocSSButton.Name = "LocSSButton";
			this.LocSSButton.Size = new System.Drawing.Size(96, 34);
			this.LocSSButton.TabIndex = 1;
			this.LocSSButton.Text = "Start";
			this.LocSSButton.UseVisualStyleBackColor = false;
			this.LocSSButton.Click += new System.EventHandler(this.LocSSButton_Click);
			// 
			// MapPictureBox
			// 
			this.MapPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.MapPictureBox.Location = new System.Drawing.Point(60, 25);
			this.MapPictureBox.Name = "MapPictureBox";
			this.MapPictureBox.Size = new System.Drawing.Size(400, 400);
			this.MapPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.MapPictureBox.TabIndex = 0;
			this.MapPictureBox.TabStop = false;
			// 
			// ActGroupBox
			// 
			this.ActGroupBox.Controls.Add(this.ActSSButton);
			this.ActGroupBox.Controls.Add(this.ActTextBox);
			this.ActGroupBox.Location = new System.Drawing.Point(174, 452);
			this.ActGroupBox.Name = "ActGroupBox";
			this.ActGroupBox.Size = new System.Drawing.Size(745, 308);
			this.ActGroupBox.TabIndex = 7;
			this.ActGroupBox.TabStop = false;
			this.ActGroupBox.Text = "Speech Activity";
			// 
			// ActSSButton
			// 
			this.ActSSButton.BackColor = System.Drawing.Color.LightGreen;
			this.ActSSButton.Location = new System.Drawing.Point(324, 257);
			this.ActSSButton.Name = "ActSSButton";
			this.ActSSButton.Size = new System.Drawing.Size(96, 34);
			this.ActSSButton.TabIndex = 17;
			this.ActSSButton.Text = "Start";
			this.ActSSButton.UseVisualStyleBackColor = false;
			this.ActSSButton.Click += new System.EventHandler(this.ActSSButton_Click);
			// 
			// ActTextBox
			// 
			this.ActTextBox.BackColor = System.Drawing.Color.White;
			this.ActTextBox.Location = new System.Drawing.Point(16, 25);
			this.ActTextBox.Multiline = true;
			this.ActTextBox.Name = "ActTextBox";
			this.ActTextBox.ReadOnly = true;
			this.ActTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.ActTextBox.Size = new System.Drawing.Size(723, 217);
			this.ActTextBox.TabIndex = 0;
			// 
			// RecCheckBox
			// 
			this.RecCheckBox.AutoSize = true;
			this.RecCheckBox.Location = new System.Drawing.Point(39, 429);
			this.RecCheckBox.Name = "RecCheckBox";
			this.RecCheckBox.Size = new System.Drawing.Size(80, 24);
			this.RecCheckBox.TabIndex = 8;
			this.RecCheckBox.Text = "Record";
			this.RecCheckBox.UseVisualStyleBackColor = true;
			this.RecCheckBox.CheckedChanged += new System.EventHandler(this.RecCheckBox_CheckedChanged);
			// 
			// RSButton
			// 
			this.RSButton.BackColor = System.Drawing.Color.Yellow;
			this.RSButton.Enabled = false;
			this.RSButton.Location = new System.Drawing.Point(12, 549);
			this.RSButton.Name = "RSButton";
			this.RSButton.Size = new System.Drawing.Size(141, 34);
			this.RSButton.TabIndex = 9;
			this.RSButton.Text = "RESTART";
			this.RSButton.UseVisualStyleBackColor = false;
			this.RSButton.Click += new System.EventHandler(this.RSButton_Click);
			// 
			// SDButton
			// 
			this.SDButton.BackColor = System.Drawing.Color.OrangeRed;
			this.SDButton.Enabled = false;
			this.SDButton.Location = new System.Drawing.Point(12, 633);
			this.SDButton.Name = "SDButton";
			this.SDButton.Size = new System.Drawing.Size(141, 34);
			this.SDButton.TabIndex = 10;
			this.SDButton.Text = "SHUTDOWN";
			this.SDButton.UseVisualStyleBackColor = false;
			this.SDButton.Click += new System.EventHandler(this.SDButton_Click);
			// 
			// SUButton
			// 
			this.SUButton.BackColor = System.Drawing.Color.LightGreen;
			this.SUButton.Enabled = false;
			this.SUButton.Location = new System.Drawing.Point(12, 465);
			this.SUButton.Name = "SUButton";
			this.SUButton.Size = new System.Drawing.Size(141, 34);
			this.SUButton.TabIndex = 11;
			this.SUButton.Text = "START UP";
			this.SUButton.UseVisualStyleBackColor = false;
			this.SUButton.Click += new System.EventHandler(this.SUButton_Click);
			// 
			// ESButton
			// 
			this.ESButton.BackColor = System.Drawing.Color.Red;
			this.ESButton.Enabled = false;
			this.ESButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ESButton.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
			this.ESButton.Location = new System.Drawing.Point(12, 675);
			this.ESButton.Name = "ESButton";
			this.ESButton.Size = new System.Drawing.Size(141, 84);
			this.ESButton.TabIndex = 12;
			this.ESButton.Text = "EMERGENCY STOP";
			this.ESButton.UseVisualStyleBackColor = false;
			this.ESButton.Click += new System.EventHandler(this.ESButton_Click);
			// 
			// EXButton
			// 
			this.EXButton.BackColor = System.Drawing.Color.LightSalmon;
			this.EXButton.Enabled = false;
			this.EXButton.Location = new System.Drawing.Point(12, 591);
			this.EXButton.Name = "EXButton";
			this.EXButton.Size = new System.Drawing.Size(141, 34);
			this.EXButton.TabIndex = 13;
			this.EXButton.Text = "EXIT";
			this.EXButton.UseVisualStyleBackColor = false;
			this.EXButton.Click += new System.EventHandler(this.EXButton_Click);
			// 
			// HDButton
			// 
			this.HDButton.BackColor = System.Drawing.Color.Yellow;
			this.HDButton.Enabled = false;
			this.HDButton.Location = new System.Drawing.Point(12, 507);
			this.HDButton.Name = "HDButton";
			this.HDButton.Size = new System.Drawing.Size(141, 34);
			this.HDButton.TabIndex = 14;
			this.HDButton.Text = "HW DIAG";
			this.HDButton.UseVisualStyleBackColor = false;
			this.HDButton.Click += new System.EventHandler(this.HDButton_Click);
			// 
			// MainControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.HDButton);
			this.Controls.Add(this.EXButton);
			this.Controls.Add(this.ESButton);
			this.Controls.Add(this.SUButton);
			this.Controls.Add(this.SDButton);
			this.Controls.Add(this.RSButton);
			this.Controls.Add(this.RecCheckBox);
			this.Controls.Add(this.ActGroupBox);
			this.Controls.Add(this.LocGroupBox);
			this.Controls.Add(this.StatGroupBox);
			this.Controls.Add(this.VideoGroupBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "MainControl";
			this.Size = new System.Drawing.Size(1490, 780);
			this.VideoGroupBox.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).EndInit();
			this.StatGroupBox.ResumeLayout(false);
			this.StatGroupBox.PerformLayout();
			this.LocGroupBox.ResumeLayout(false);
			this.LocGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.MapPictureBox)).EndInit();
			this.ActGroupBox.ResumeLayout(false);
			this.ActGroupBox.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.GroupBox VideoGroupBox;
		private System.Windows.Forms.PictureBox VideoPictureBox;
		private System.Windows.Forms.Button VidSSButton;
		private System.Windows.Forms.GroupBox StatGroupBox;
		private System.Windows.Forms.Button SSSButton;
		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.TextBox VoltTextBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.CheckBox FLDCheckBox;
		private System.Windows.Forms.CheckBox NDBCheckBox;
		private System.Windows.Forms.CheckBox CTCCheckBox;
		private System.Windows.Forms.CheckBox HACheckBox;
		private System.Windows.Forms.CheckBox KCheckBox;
		private System.Windows.Forms.GroupBox LocGroupBox;
		private System.Windows.Forms.Button LocSSButton;
		private System.Windows.Forms.PictureBox MapPictureBox;
		private System.Windows.Forms.GroupBox ActGroupBox;
		private System.Windows.Forms.Button ActSSButton;
		private System.Windows.Forms.TextBox ActTextBox;
		private System.Windows.Forms.CheckBox RecCheckBox;
		private System.Windows.Forms.Button RSButton;
		private System.Windows.Forms.Button SDButton;
		private System.Windows.Forms.Button SUButton;
		private System.Windows.Forms.TextBox LocTextBox;
		private System.Windows.Forms.TextBox PoseTextBox;
		private System.Windows.Forms.TextBox HostTextBox;
		private System.Windows.Forms.Button ESButton;
		private System.Windows.Forms.Button EXButton;
		private System.Windows.Forms.CheckBox ACheckBox;
		private System.Windows.Forms.CheckBox RLDCheckBox;
		private System.Windows.Forms.CheckBox SRECheckBox;
		private System.Windows.Forms.CheckBox VODCheckBox;
		private System.Windows.Forms.CheckBox SDOCheckBox;
		private System.Windows.Forms.Button SLButton;
		private System.Windows.Forms.Button HDButton;
		}
	}
