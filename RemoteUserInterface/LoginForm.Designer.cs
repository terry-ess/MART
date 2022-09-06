namespace RemoteUserInterface
	{
	partial class LoginForm
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
			this.PassTextBox = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.UserTextBox = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.HostTextBox = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// PassTextBox
			// 
			this.PassTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.PassTextBox.Location = new System.Drawing.Point(105, 97);
			this.PassTextBox.Name = "PassTextBox";
			this.PassTextBox.PasswordChar = '*';
			this.PassTextBox.Size = new System.Drawing.Size(273, 26);
			this.PassTextBox.TabIndex = 2;
			// 
			// button1
			// 
			this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.button1.Location = new System.Drawing.Point(165, 133);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 30);
			this.button1.TabIndex = 3;
			this.button1.Text = "Done";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.DoneButton_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(20, 100);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(78, 20);
			this.label1.TabIndex = 2;
			this.label1.Text = "Password";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.Location = new System.Drawing.Point(24, 54);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(64, 20);
			this.label2.TabIndex = 4;
			this.label2.Text = "User ID";
			// 
			// UserTextBox
			// 
			this.UserTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.UserTextBox.Location = new System.Drawing.Point(105, 51);
			this.UserTextBox.Name = "UserTextBox";
			this.UserTextBox.Size = new System.Drawing.Size(273, 26);
			this.UserTextBox.TabIndex = 1;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.Location = new System.Drawing.Point(24, 11);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(43, 20);
			this.label3.TabIndex = 6;
			this.label3.Text = "Host";
			// 
			// HostTextBox
			// 
			this.HostTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.HostTextBox.Location = new System.Drawing.Point(105, 8);
			this.HostTextBox.Name = "HostTextBox";
			this.HostTextBox.Size = new System.Drawing.Size(273, 26);
			this.HostTextBox.TabIndex = 0;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.Location = new System.Drawing.Point(23, 100);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(78, 20);
			this.label4.TabIndex = 2;
			this.label4.Text = "Password";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label5.Location = new System.Drawing.Point(23, 57);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(64, 20);
			this.label5.TabIndex = 4;
			this.label5.Text = "User ID";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label6.Location = new System.Drawing.Point(23, 14);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(43, 20);
			this.label6.TabIndex = 6;
			this.label6.Text = "Host";
			// 
			// LoginForm
			// 
			this.AcceptButton = this.button1;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(396, 172);
			this.ControlBox = false;
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.HostTextBox);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.UserTextBox);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.PassTextBox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LoginForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Login";
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.TextBox PassTextBox;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox UserTextBox;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox HostTextBox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		}
	}