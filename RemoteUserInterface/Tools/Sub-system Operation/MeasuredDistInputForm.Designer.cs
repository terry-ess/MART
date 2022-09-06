namespace Sub_system_Operation
	{
	partial class MeasuredDistInputForm
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
			this.label1 = new System.Windows.Forms.Label();
			this.MDNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.button1 = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.MDNumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(61, 17);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(170, 20);
			this.label1.TabIndex = 0;
			this.label1.Text = "Measured distance (in)";
			// 
			// MDNumericUpDown
			// 
			this.MDNumericUpDown.DecimalPlaces = 2;
			this.MDNumericUpDown.Location = new System.Drawing.Point(86, 55);
			this.MDNumericUpDown.Maximum = new decimal(new int[] {
            150,
            0,
            0,
            0});
			this.MDNumericUpDown.Name = "MDNumericUpDown";
			this.MDNumericUpDown.Size = new System.Drawing.Size(120, 26);
			this.MDNumericUpDown.TabIndex = 1;
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(109, 99);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 25);
			this.button1.TabIndex = 2;
			this.button1.Text = "OK";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// MeasuredDistInputForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(293, 139);
			this.ControlBox = false;
			this.Controls.Add(this.button1);
			this.Controls.Add(this.MDNumericUpDown);
			this.Controls.Add(this.label1);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.Name = "MeasuredDistInputForm";
			((System.ComponentModel.ISupportInitialize)(this.MDNumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown MDNumericUpDown;
		private System.Windows.Forms.Button button1;
		}
	}