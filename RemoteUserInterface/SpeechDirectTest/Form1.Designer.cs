namespace SpeechDirectTest
	{
	partial class Form1
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
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.SSButton = new System.Windows.Forms.Button();
			this.SaveButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// StatusTextBox
			// 
			this.StatusTextBox.Location = new System.Drawing.Point(24, 9);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.StatusTextBox.Size = new System.Drawing.Size(547, 633);
			this.StatusTextBox.TabIndex = 0;
			// 
			// SSButton
			// 
			this.SSButton.Location = new System.Drawing.Point(150, 660);
			this.SSButton.Name = "SSButton";
			this.SSButton.Size = new System.Drawing.Size(75, 25);
			this.SSButton.TabIndex = 1;
			this.SSButton.Text = "Start";
			this.SSButton.UseVisualStyleBackColor = true;
			this.SSButton.Click += new System.EventHandler(this.SSButton_Click);
			// 
			// SaveButton
			// 
			this.SaveButton.Location = new System.Drawing.Point(370, 660);
			this.SaveButton.Name = "SaveButton";
			this.SaveButton.Size = new System.Drawing.Size(75, 25);
			this.SaveButton.TabIndex = 2;
			this.SaveButton.Text = "Save";
			this.SaveButton.UseVisualStyleBackColor = true;
			this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
			// 
			// Form1
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(594, 708);
			this.Controls.Add(this.SaveButton);
			this.Controls.Add(this.SSButton);
			this.Controls.Add(this.StatusTextBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "Form1";
			this.Text = "Speech Direction Test";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.Button SSButton;
		private System.Windows.Forms.Button SaveButton;
		}
	}

