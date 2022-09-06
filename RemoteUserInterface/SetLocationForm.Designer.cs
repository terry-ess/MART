
namespace RemoteUserInterface
	{
	partial class SetLocationForm
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
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.CoordTextBox = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.DoneButton = new System.Windows.Forms.Button();
			this.OrientNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.NameComboBox = new System.Windows.Forms.ComboBox();
			this.MapPictureBox = new System.Windows.Forms.PictureBox();
			this.RmTextBox = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this.OrientNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MapPictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label6.Location = new System.Drawing.Point(415, 438);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(96, 20);
			this.label6.TabIndex = 13;
			this.label6.Text = "Room name";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label5.Location = new System.Drawing.Point(420, 490);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(95, 20);
			this.label5.TabIndex = 12;
			this.label5.Text = "Coordinates";
			// 
			// CoordTextBox
			// 
			this.CoordTextBox.Enabled = false;
			this.CoordTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CoordTextBox.Location = new System.Drawing.Point(517, 487);
			this.CoordTextBox.Name = "CoordTextBox";
			this.CoordTextBox.Size = new System.Drawing.Size(135, 26);
			this.CoordTextBox.TabIndex = 8;
			this.CoordTextBox.Leave += new System.EventHandler(this.CoordTextBox_Leave);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.Location = new System.Drawing.Point(448, 541);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(87, 20);
			this.label4.TabIndex = 9;
			this.label4.Text = "Orientation";
			// 
			// DoneButton
			// 
			this.DoneButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.DoneButton.Location = new System.Drawing.Point(499, 589);
			this.DoneButton.Name = "DoneButton";
			this.DoneButton.Size = new System.Drawing.Size(75, 30);
			this.DoneButton.TabIndex = 11;
			this.DoneButton.Text = "Done";
			this.DoneButton.UseVisualStyleBackColor = true;
			this.DoneButton.Click += new System.EventHandler(this.DoneButton_Click);
			// 
			// OrientNumericUpDown
			// 
			this.OrientNumericUpDown.Enabled = false;
			this.OrientNumericUpDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.OrientNumericUpDown.Location = new System.Drawing.Point(539, 538);
			this.OrientNumericUpDown.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
			this.OrientNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
			this.OrientNumericUpDown.Name = "OrientNumericUpDown";
			this.OrientNumericUpDown.Size = new System.Drawing.Size(85, 26);
			this.OrientNumericUpDown.TabIndex = 14;
			this.OrientNumericUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
			// 
			// NameComboBox
			// 
			this.NameComboBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.NameComboBox.FormattingEnabled = true;
			this.NameComboBox.Items.AddRange(new object[] {
            ""});
			this.NameComboBox.Location = new System.Drawing.Point(513, 434);
			this.NameComboBox.Name = "NameComboBox";
			this.NameComboBox.Size = new System.Drawing.Size(144, 28);
			this.NameComboBox.TabIndex = 15;
			this.NameComboBox.SelectedIndexChanged += new System.EventHandler(this.NameComboBox_SelectedIndexChanged);
			// 
			// MapPictureBox
			// 
			this.MapPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.MapPictureBox.Location = new System.Drawing.Point(19, 9);
			this.MapPictureBox.Name = "MapPictureBox";
			this.MapPictureBox.Size = new System.Drawing.Size(400, 400);
			this.MapPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.MapPictureBox.TabIndex = 16;
			this.MapPictureBox.TabStop = false;
			// 
			// RmTextBox
			// 
			this.RmTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.RmTextBox.Location = new System.Drawing.Point(473, 13);
			this.RmTextBox.Multiline = true;
			this.RmTextBox.Name = "RmTextBox";
			this.RmTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.RmTextBox.Size = new System.Drawing.Size(581, 396);
			this.RmTextBox.TabIndex = 17;
			this.RmTextBox.WordWrap = false;
			// 
			// SetLocationForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(1072, 643);
			this.ControlBox = false;
			this.Controls.Add(this.RmTextBox);
			this.Controls.Add(this.MapPictureBox);
			this.Controls.Add(this.NameComboBox);
			this.Controls.Add(this.OrientNumericUpDown);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.CoordTextBox);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.DoneButton);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SetLocationForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "SetLocationForm";
			((System.ComponentModel.ISupportInitialize)(this.OrientNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MapPictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox CoordTextBox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button DoneButton;
		private System.Windows.Forms.NumericUpDown OrientNumericUpDown;
		private System.Windows.Forms.ComboBox NameComboBox;
		private System.Windows.Forms.PictureBox MapPictureBox;
		private System.Windows.Forms.TextBox RmTextBox;
		}
	}