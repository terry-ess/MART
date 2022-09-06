namespace Tests
	{
	partial class TestsForm
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
			this.TestsListBox = new System.Windows.Forms.ListBox();
			this.QuietCheckBox = new System.Windows.Forms.CheckBox();
			this.TINumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label4 = new System.Windows.Forms.Label();
			this.SSButton = new System.Windows.Forms.Button();
			this.CITextBox = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.TINumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// TestsListBox
			// 
			this.TestsListBox.FormattingEnabled = true;
			this.TestsListBox.ItemHeight = 20;
			this.TestsListBox.Location = new System.Drawing.Point(15, 25);
			this.TestsListBox.Name = "TestsListBox";
			this.TestsListBox.Size = new System.Drawing.Size(456, 164);
			this.TestsListBox.TabIndex = 1;
			// 
			// QuietCheckBox
			// 
			this.QuietCheckBox.AutoSize = true;
			this.QuietCheckBox.Location = new System.Drawing.Point(210, 259);
			this.QuietCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.QuietCheckBox.Name = "QuietCheckBox";
			this.QuietCheckBox.Size = new System.Drawing.Size(66, 24);
			this.QuietCheckBox.TabIndex = 9;
			this.QuietCheckBox.Text = "Quiet";
			this.QuietCheckBox.UseVisualStyleBackColor = true;
			// 
			// TINumericUpDown
			// 
			this.TINumericUpDown.Location = new System.Drawing.Point(274, 214);
			this.TINumericUpDown.Margin = new System.Windows.Forms.Padding(2);
			this.TINumericUpDown.Maximum = new decimal(new int[] {
            250,
            0,
            0,
            0});
			this.TINumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.TINumericUpDown.Name = "TINumericUpDown";
			this.TINumericUpDown.Size = new System.Drawing.Size(64, 26);
			this.TINumericUpDown.TabIndex = 8;
			this.TINumericUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(149, 217);
			this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(124, 20);
			this.label4.TabIndex = 7;
			this.label4.Text = "Target iterations";
			// 
			// SSButton
			// 
			this.SSButton.BackColor = System.Drawing.Color.LightGreen;
			this.SSButton.Location = new System.Drawing.Point(187, 305);
			this.SSButton.Name = "SSButton";
			this.SSButton.Size = new System.Drawing.Size(113, 35);
			this.SSButton.TabIndex = 10;
			this.SSButton.Text = "Start Test";
			this.SSButton.UseVisualStyleBackColor = false;
			this.SSButton.Click += new System.EventHandler(this.SSButton_Click);
			// 
			// CITextBox
			// 
			this.CITextBox.Location = new System.Drawing.Point(813, 25);
			this.CITextBox.Margin = new System.Windows.Forms.Padding(2);
			this.CITextBox.Name = "CITextBox";
			this.CITextBox.ReadOnly = true;
			this.CITextBox.Size = new System.Drawing.Size(76, 26);
			this.CITextBox.TabIndex = 12;
			this.CITextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(655, 28);
			this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(155, 20);
			this.label5.TabIndex = 11;
			this.label5.Text = "Completed iterations";
			// 
			// StatusTextBox
			// 
			this.StatusTextBox.Location = new System.Drawing.Point(555, 97);
			this.StatusTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.StatusTextBox.Size = new System.Drawing.Size(456, 243);
			this.StatusTextBox.TabIndex = 13;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(555, 72);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(56, 20);
			this.label1.TabIndex = 14;
			this.label1.Text = "Status";
			// 
			// TestsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1038, 365);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.StatusTextBox);
			this.Controls.Add(this.CITextBox);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.SSButton);
			this.Controls.Add(this.QuietCheckBox);
			this.Controls.Add(this.TINumericUpDown);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.TestsListBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "TestsForm";
			this.ShowIcon = false;
			this.Text = "Robot Movement Tests";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TestsForm_FormClosing);
			((System.ComponentModel.ISupportInitialize)(this.TINumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion
		private System.Windows.Forms.ListBox TestsListBox;
		private System.Windows.Forms.CheckBox QuietCheckBox;
		private System.Windows.Forms.NumericUpDown TINumericUpDown;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button SSButton;
		private System.Windows.Forms.TextBox CITextBox;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.Label label1;
		}
	}