namespace Sub_system_Operation
	{
	partial class KinectDistCal
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
			System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "0",
            "0"}, -1);
			this.VideoPictureBox = new System.Windows.Forms.PictureBox();
			this.DepthPictureBox = new System.Windows.Forms.PictureBox();
			this.ShootButton = new System.Windows.Forms.Button();
			this.ColNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.MeasureButton = new System.Windows.Forms.Button();
			this.StartPosButton = new System.Windows.Forms.Button();
			this.NextPosButton = new System.Windows.Forms.Button();
			this.SaveButton = new System.Windows.Forms.Button();
			this.DistListView = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.label2 = new System.Windows.Forms.Label();
			this.StatusTextBox = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.OffsetNumericUpDown = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.DepthPictureBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.ColNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.OffsetNumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// VideoPictureBox
			// 
			this.VideoPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.VideoPictureBox.Location = new System.Drawing.Point(10, 13);
			this.VideoPictureBox.Name = "VideoPictureBox";
			this.VideoPictureBox.Size = new System.Drawing.Size(320, 240);
			this.VideoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.VideoPictureBox.TabIndex = 0;
			this.VideoPictureBox.TabStop = false;
			// 
			// DepthPictureBox
			// 
			this.DepthPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.DepthPictureBox.Location = new System.Drawing.Point(10, 276);
			this.DepthPictureBox.Name = "DepthPictureBox";
			this.DepthPictureBox.Size = new System.Drawing.Size(320, 240);
			this.DepthPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.DepthPictureBox.TabIndex = 1;
			this.DepthPictureBox.TabStop = false;
			// 
			// ShootButton
			// 
			this.ShootButton.Location = new System.Drawing.Point(398, 299);
			this.ShootButton.Name = "ShootButton";
			this.ShootButton.Size = new System.Drawing.Size(75, 25);
			this.ShootButton.TabIndex = 2;
			this.ShootButton.Text = "Shoot";
			this.ShootButton.UseVisualStyleBackColor = true;
			this.ShootButton.Click += new System.EventHandler(this.ShootButton_Click);
			// 
			// ColNumericUpDown
			// 
			this.ColNumericUpDown.Location = new System.Drawing.Point(403, 394);
			this.ColNumericUpDown.Maximum = new decimal(new int[] {
            640,
            0,
            0,
            0});
			this.ColNumericUpDown.Name = "ColNumericUpDown";
			this.ColNumericUpDown.Size = new System.Drawing.Size(79, 26);
			this.ColNumericUpDown.TabIndex = 3;
			this.ColNumericUpDown.Value = new decimal(new int[] {
            320,
            0,
            0,
            0});
			this.ColNumericUpDown.ValueChanged += new System.EventHandler(this.ColNumericUpDown_ValueChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(368, 397);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(32, 20);
			this.label1.TabIndex = 4;
			this.label1.Text = "Col";
			// 
			// MeasureButton
			// 
			this.MeasureButton.Location = new System.Drawing.Point(389, 490);
			this.MeasureButton.Name = "MeasureButton";
			this.MeasureButton.Size = new System.Drawing.Size(92, 25);
			this.MeasureButton.TabIndex = 5;
			this.MeasureButton.Text = "Measure";
			this.MeasureButton.UseVisualStyleBackColor = true;
			this.MeasureButton.Click += new System.EventHandler(this.MeasureButton_Click);
			// 
			// StartPosButton
			// 
			this.StartPosButton.Location = new System.Drawing.Point(379, 109);
			this.StartPosButton.Name = "StartPosButton";
			this.StartPosButton.Size = new System.Drawing.Size(112, 25);
			this.StartPosButton.TabIndex = 6;
			this.StartPosButton.Text = "Start position";
			this.StartPosButton.UseVisualStyleBackColor = true;
			this.StartPosButton.Click += new System.EventHandler(this.StartPosButton_Click);
			// 
			// NextPosButton
			// 
			this.NextPosButton.Location = new System.Drawing.Point(379, 204);
			this.NextPosButton.Name = "NextPosButton";
			this.NextPosButton.Size = new System.Drawing.Size(112, 25);
			this.NextPosButton.TabIndex = 7;
			this.NextPosButton.Text = "Next position";
			this.NextPosButton.UseVisualStyleBackColor = true;
			this.NextPosButton.Click += new System.EventHandler(this.NextPosButton_Click);
			// 
			// SaveButton
			// 
			this.SaveButton.Location = new System.Drawing.Point(599, 362);
			this.SaveButton.Name = "SaveButton";
			this.SaveButton.Size = new System.Drawing.Size(112, 25);
			this.SaveButton.TabIndex = 8;
			this.SaveButton.Text = "Save table";
			this.SaveButton.UseVisualStyleBackColor = true;
			this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
			// 
			// DistListView
			// 
			this.DistListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.DistListView.GridLines = true;
			this.DistListView.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1});
			this.DistListView.Location = new System.Drawing.Point(553, 45);
			this.DistListView.Name = "DistListView";
			this.DistListView.Size = new System.Drawing.Size(204, 300);
			this.DistListView.TabIndex = 9;
			this.DistListView.UseCompatibleStateImageBehavior = false;
			this.DistListView.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Measured";
			this.columnHeader1.Width = 100;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Actual";
			this.columnHeader2.Width = 100;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(606, 22);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(98, 20);
			this.label2.TabIndex = 10;
			this.label2.Text = "Distance (in)";
			// 
			// StatusTextBox
			// 
			this.StatusTextBox.BackColor = System.Drawing.Color.White;
			this.StatusTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.StatusTextBox.Location = new System.Drawing.Point(525, 399);
			this.StatusTextBox.Multiline = true;
			this.StatusTextBox.Name = "StatusTextBox";
			this.StatusTextBox.ReadOnly = true;
			this.StatusTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.StatusTextBox.Size = new System.Drawing.Size(260, 117);
			this.StatusTextBox.TabIndex = 79;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(373, 16);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(79, 20);
			this.label3.TabIndex = 81;
			this.label3.Text = "Offset (in)";
			// 
			// OffsetNumericUpDown
			// 
			this.OffsetNumericUpDown.Location = new System.Drawing.Point(453, 13);
			this.OffsetNumericUpDown.Maximum = new decimal(new int[] {
            24,
            0,
            0,
            0});
			this.OffsetNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.OffsetNumericUpDown.Name = "OffsetNumericUpDown";
			this.OffsetNumericUpDown.Size = new System.Drawing.Size(47, 26);
			this.OffsetNumericUpDown.TabIndex = 80;
			this.OffsetNumericUpDown.Value = new decimal(new int[] {
            12,
            0,
            0,
            0});
			// 
			// KinectDistCal
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.label3);
			this.Controls.Add(this.OffsetNumericUpDown);
			this.Controls.Add(this.StatusTextBox);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.DistListView);
			this.Controls.Add(this.SaveButton);
			this.Controls.Add(this.NextPosButton);
			this.Controls.Add(this.StartPosButton);
			this.Controls.Add(this.MeasureButton);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.ColNumericUpDown);
			this.Controls.Add(this.ShootButton);
			this.Controls.Add(this.DepthPictureBox);
			this.Controls.Add(this.VideoPictureBox);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "KinectDistCal";
			this.Size = new System.Drawing.Size(803, 544);
			((System.ComponentModel.ISupportInitialize)(this.VideoPictureBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.DepthPictureBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.ColNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.OffsetNumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

			}

		#endregion

		private System.Windows.Forms.PictureBox VideoPictureBox;
		private System.Windows.Forms.PictureBox DepthPictureBox;
		private System.Windows.Forms.Button ShootButton;
		private System.Windows.Forms.NumericUpDown ColNumericUpDown;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button MeasureButton;
		private System.Windows.Forms.Button StartPosButton;
		private System.Windows.Forms.Button NextPosButton;
		private System.Windows.Forms.Button SaveButton;
		private System.Windows.Forms.ListView DistListView;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox StatusTextBox;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NumericUpDown OffsetNumericUpDown;
		}
	}
