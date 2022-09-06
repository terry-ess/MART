namespace Sub_system_Operation
	{
	partial class HeadAssemblyOp
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
			this.ServoGroupBox = new System.Windows.Forms.GroupBox();
			this.ErrStatTextBox = new System.Windows.Forms.TextBox();
			this.CEButton = new System.Windows.Forms.Button();
			this.StatusButton = new System.Windows.Forms.Button();
			this.PosGroupBox = new System.Windows.Forms.GroupBox();
			this.PanNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.TiltNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label10 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.SPButton = new System.Windows.Forms.Button();
			this.MHGroupBox = new System.Windows.Forms.GroupBox();
			this.GHButton = new System.Windows.Forms.Button();
			this.MHTextBox = new System.Windows.Forms.TextBox();
			this.label15 = new System.Windows.Forms.Label();
			this.LAGroupBox = new System.Windows.Forms.GroupBox();
			this.GLAButton = new System.Windows.Forms.Button();
			this.LATextBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.RestartButton = new System.Windows.Forms.Button();
			this.ServoGroupBox.SuspendLayout();
			this.PosGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.PanNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.TiltNumericUpDown)).BeginInit();
			this.MHGroupBox.SuspendLayout();
			this.LAGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// ServoGroupBox
			// 
			this.ServoGroupBox.Controls.Add(this.ErrStatTextBox);
			this.ServoGroupBox.Controls.Add(this.CEButton);
			this.ServoGroupBox.Controls.Add(this.StatusButton);
			this.ServoGroupBox.Controls.Add(this.PosGroupBox);
			this.ServoGroupBox.Location = new System.Drawing.Point(50, 75);
			this.ServoGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.ServoGroupBox.Name = "ServoGroupBox";
			this.ServoGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.ServoGroupBox.Size = new System.Drawing.Size(336, 372);
			this.ServoGroupBox.TabIndex = 0;
			this.ServoGroupBox.TabStop = false;
			this.ServoGroupBox.Text = "Pan - Tilt Servos";
			// 
			// ErrStatTextBox
			// 
			this.ErrStatTextBox.BackColor = System.Drawing.Color.White;
			this.ErrStatTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ErrStatTextBox.Location = new System.Drawing.Point(25, 301);
			this.ErrStatTextBox.Multiline = true;
			this.ErrStatTextBox.Name = "ErrStatTextBox";
			this.ErrStatTextBox.ReadOnly = true;
			this.ErrStatTextBox.Size = new System.Drawing.Size(286, 53);
			this.ErrStatTextBox.TabIndex = 76;
			// 
			// CEButton
			// 
			this.CEButton.Location = new System.Drawing.Point(119, 193);
			this.CEButton.Name = "CEButton";
			this.CEButton.Size = new System.Drawing.Size(99, 30);
			this.CEButton.TabIndex = 75;
			this.CEButton.Text = "Clear Error";
			this.CEButton.UseVisualStyleBackColor = true;
			this.CEButton.Click += new System.EventHandler(this.CEButton_Click);
			// 
			// StatusButton
			// 
			this.StatusButton.Location = new System.Drawing.Point(111, 247);
			this.StatusButton.Name = "StatusButton";
			this.StatusButton.Size = new System.Drawing.Size(114, 30);
			this.StatusButton.TabIndex = 74;
			this.StatusButton.Text = "Error Status";
			this.StatusButton.UseVisualStyleBackColor = true;
			this.StatusButton.Click += new System.EventHandler(this.StatusButton_Click);
			// 
			// PosGroupBox
			// 
			this.PosGroupBox.Controls.Add(this.PanNumericUpDown);
			this.PosGroupBox.Controls.Add(this.TiltNumericUpDown);
			this.PosGroupBox.Controls.Add(this.label10);
			this.PosGroupBox.Controls.Add(this.label11);
			this.PosGroupBox.Controls.Add(this.SPButton);
			this.PosGroupBox.Location = new System.Drawing.Point(71, 24);
			this.PosGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.PosGroupBox.Name = "PosGroupBox";
			this.PosGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.PosGroupBox.Size = new System.Drawing.Size(195, 145);
			this.PosGroupBox.TabIndex = 73;
			this.PosGroupBox.TabStop = false;
			this.PosGroupBox.Text = "Position";
			// 
			// PanNumericUpDown
			// 
			this.PanNumericUpDown.Location = new System.Drawing.Point(88, 28);
			this.PanNumericUpDown.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
			this.PanNumericUpDown.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            -2147483648});
			this.PanNumericUpDown.Name = "PanNumericUpDown";
			this.PanNumericUpDown.Size = new System.Drawing.Size(85, 26);
			this.PanNumericUpDown.TabIndex = 10;
			// 
			// TiltNumericUpDown
			// 
			this.TiltNumericUpDown.Location = new System.Drawing.Point(88, 64);
			this.TiltNumericUpDown.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
			this.TiltNumericUpDown.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            -2147483648});
			this.TiltNumericUpDown.Name = "TiltNumericUpDown";
			this.TiltNumericUpDown.Size = new System.Drawing.Size(85, 26);
			this.TiltNumericUpDown.TabIndex = 9;
			// 
			// label10
			// 
			this.label10.Location = new System.Drawing.Point(21, 67);
			this.label10.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(52, 16);
			this.label10.TabIndex = 7;
			this.label10.Text = "Tilt (°)";
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point(21, 30);
			this.label11.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(67, 16);
			this.label11.TabIndex = 5;
			this.label11.Text = "Pan (°)";
			// 
			// SPButton
			// 
			this.SPButton.AutoSize = true;
			this.SPButton.Location = new System.Drawing.Point(60, 98);
			this.SPButton.Name = "SPButton";
			this.SPButton.Size = new System.Drawing.Size(75, 30);
			this.SPButton.TabIndex = 3;
			this.SPButton.Text = "Set";
			this.SPButton.UseVisualStyleBackColor = true;
			this.SPButton.Click += new System.EventHandler(this.SPButton_Click);
			// 
			// MHGroupBox
			// 
			this.MHGroupBox.Controls.Add(this.GHButton);
			this.MHGroupBox.Controls.Add(this.MHTextBox);
			this.MHGroupBox.Controls.Add(this.label15);
			this.MHGroupBox.Location = new System.Drawing.Point(468, 75);
			this.MHGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.MHGroupBox.Name = "MHGroupBox";
			this.MHGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.MHGroupBox.Size = new System.Drawing.Size(260, 117);
			this.MHGroupBox.TabIndex = 75;
			this.MHGroupBox.TabStop = false;
			this.MHGroupBox.Text = "Heading";
			// 
			// GHButton
			// 
			this.GHButton.Location = new System.Drawing.Point(93, 68);
			this.GHButton.Name = "GHButton";
			this.GHButton.Size = new System.Drawing.Size(75, 30);
			this.GHButton.TabIndex = 17;
			this.GHButton.Text = "Get Heading";
			this.GHButton.UseVisualStyleBackColor = true;
			this.GHButton.Click += new System.EventHandler(this.GHButton_Click);
			// 
			// MHTextBox
			// 
			this.MHTextBox.Location = new System.Drawing.Point(167, 29);
			this.MHTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.MHTextBox.Name = "MHTextBox";
			this.MHTextBox.ReadOnly = true;
			this.MHTextBox.Size = new System.Drawing.Size(58, 26);
			this.MHTextBox.TabIndex = 16;
			// 
			// label15
			// 
			this.label15.AutoSize = true;
			this.label15.Location = new System.Drawing.Point(36, 32);
			this.label15.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(127, 20);
			this.label15.TabIndex = 15;
			this.label15.Text = "Mag. Heading (°)";
			// 
			// LAGroupBox
			// 
			this.LAGroupBox.Controls.Add(this.GLAButton);
			this.LAGroupBox.Controls.Add(this.LATextBox);
			this.LAGroupBox.Controls.Add(this.label2);
			this.LAGroupBox.Location = new System.Drawing.Point(468, 202);
			this.LAGroupBox.Margin = new System.Windows.Forms.Padding(2);
			this.LAGroupBox.Name = "LAGroupBox";
			this.LAGroupBox.Padding = new System.Windows.Forms.Padding(2);
			this.LAGroupBox.Size = new System.Drawing.Size(260, 117);
			this.LAGroupBox.TabIndex = 77;
			this.LAGroupBox.TabStop = false;
			this.LAGroupBox.Text = "Light Amplitude";
			// 
			// GLAButton
			// 
			this.GLAButton.Location = new System.Drawing.Point(93, 63);
			this.GLAButton.Name = "GLAButton";
			this.GLAButton.Size = new System.Drawing.Size(75, 30);
			this.GLAButton.TabIndex = 17;
			this.GLAButton.Text = "Get Applitude";
			this.GLAButton.UseVisualStyleBackColor = true;
			this.GLAButton.Click += new System.EventHandler(this.GLAButton_Click);
			// 
			// LATextBox
			// 
			this.LATextBox.Location = new System.Drawing.Point(162, 29);
			this.LATextBox.Margin = new System.Windows.Forms.Padding(2);
			this.LATextBox.Name = "LATextBox";
			this.LATextBox.ReadOnly = true;
			this.LATextBox.Size = new System.Drawing.Size(58, 26);
			this.LATextBox.TabIndex = 16;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(41, 32);
			this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(117, 20);
			this.label2.TabIndex = 15;
			this.label2.Text = "Light amplitude";
			// 
			// StatusTextBox
			// 
			this.StatusTextBox.BackColor = System.Drawing.Color.White;
			this.StatusTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.StatusTextBox.Location = new System.Drawing.Point(467, 329);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ReadOnly = true;
			this.StatusTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.StatusTextBox.Size = new System.Drawing.Size(260, 117);
			this.StatusTextBox.TabIndex = 78;
			// 
			// RestartButton
			// 
			this.RestartButton.Location = new System.Drawing.Point(342, 25);
			this.RestartButton.Name = "RestartButton";
			this.RestartButton.Size = new System.Drawing.Size(99, 30);
			this.RestartButton.TabIndex = 77;
			this.RestartButton.Text = "Restart";
			this.RestartButton.UseVisualStyleBackColor = true;
			this.RestartButton.Click += new System.EventHandler(this.RestartButton_Click);
			// 
			// HeadAssemblyOp
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.RestartButton);
			this.Controls.Add(this.StatusTextBox);
			this.Controls.Add(this.LAGroupBox);
			this.Controls.Add(this.MHGroupBox);
			this.Controls.Add(this.ServoGroupBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.Name = "HeadAssemblyOp";
			this.Size = new System.Drawing.Size(783, 476);
			this.ServoGroupBox.ResumeLayout(false);
			this.ServoGroupBox.PerformLayout();
			this.PosGroupBox.ResumeLayout(false);
			this.PosGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.PanNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.TiltNumericUpDown)).EndInit();
			this.MHGroupBox.ResumeLayout(false);
			this.MHGroupBox.PerformLayout();
			this.LAGroupBox.ResumeLayout(false);
			this.LAGroupBox.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.GroupBox ServoGroupBox;
		private System.Windows.Forms.TextBox ErrStatTextBox;
		private System.Windows.Forms.Button CEButton;
		private System.Windows.Forms.Button StatusButton;
		private System.Windows.Forms.GroupBox PosGroupBox;
		private System.Windows.Forms.NumericUpDown PanNumericUpDown;
		private System.Windows.Forms.NumericUpDown TiltNumericUpDown;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Button SPButton;
		private System.Windows.Forms.GroupBox MHGroupBox;
		private System.Windows.Forms.Button GHButton;
		private System.Windows.Forms.TextBox MHTextBox;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.GroupBox LAGroupBox;
		private System.Windows.Forms.Button GLAButton;
		private System.Windows.Forms.TextBox LATextBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.Button RestartButton;
		}
	}
