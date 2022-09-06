using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;
using Coding4Fun.Kinect.WinForm;


namespace Sub_system_Operation
	{
	public partial class KinectDistCal : UserControl
		{

		const string PARAM_FILE = "kinectcal";

		private short[] depthmap;
		private delegate void VideoUpdate(int indx);
		private VideoUpdate vu;
		private delegate void VUFail(string err);
		private VUFail vuf;
		private MemoryStream[] ms = new MemoryStream[2];
		private int indx = 0;
		private Image vimg;
		private delegate void DMUFail();
		private DMUFail duf;
		private delegate void DepthUpdate();
		private DepthUpdate du;


		public KinectDistCal()

		{
			InitializeComponent();
		}



		public void Open()

		{
			StatusTextBox.Clear();
			DepthPictureBox.Image = KinectOp.blank;
			VideoPictureBox.Image = KinectOp.blank;
			depthmap = new short[KinectOp.video_frame_width * KinectOp.video_frame_height];
			NextPosButton.Enabled = false;
			ShootButton.Enabled = false;
			MeasureButton.Enabled = false;
			vu = VidUpdate;
			vuf = VidUpdateFail;
			duf = DepthFail;
			du = DUpdate;
		}



		public void Close()

		{

		}




		private void StartPosButton_Click(object sender, EventArgs e)

		{
			string rsp;

			rsp = SubSystemOpForm.SendToolCommand(UiConstants.START_POS + "," + ((int) OffsetNumericUpDown.Value),700);
			if (rsp.StartsWith(UiConstants.OK))
				{
				StartPosButton.Enabled = false;
				ShootButton.Enabled = true;
				}
		}



		private void NextPosButton_Click(object sender, EventArgs e)

		{
			string rsp;

			if (DistListView.Items[DistListView.Items.Count - 1].SubItems[0].Text == "0")
				{
				MessageBox.Show("You must enter the actual distance for the last measurement.","Error");
				}
			else
				{
				rsp = SubSystemOpForm.SendToolCommand(UiConstants.NEXT_POS,300);
				if (rsp.StartsWith(UiConstants.OK))
					{
					ShootButton.Enabled = true;
					NextPosButton.Enabled = false;
					}
				}
		}



		private void DisplayImage(Image img)

		{
			Graphics g;
			int row,col;

			g = System.Drawing.Graphics.FromImage(img);
			row = (int) Math.Round((double)KinectOp.video_frame_height /2);
			g.DrawLine(Pens.Red, 0, row,img.Width, row);
			col = (int) ColNumericUpDown.Value;
			g.DrawLine(Pens.Red,col,0,col,img.Height);
			VideoPictureBox.Image = img;
		}



		private void VidUpdate(int indx)

		{
			Image img;

			img = Image.FromStream(ms[indx]);
			img.RotateFlip(RotateFlipType.Rotate180FlipY);
			vimg = (Image) img.Clone();
			DisplayImage(img);
		}



		private void VidUpdateFail(string err)

		{
			VideoPictureBox.Image = KinectOp.blank;
			StatusTextBox.AppendText(err + "\r\n");
			ShootButton.Enabled = true;
		}



		private bool VideoReceive()

		{
			string msg;
			string[] val;
			int vlen,rvlen;
			object[] obj = new object[2];
			bool rtn = false;

			msg = SubSystemOpForm.tconnect.ReceiveResponse(100,true);
			if (msg.StartsWith(UiConstants.VIDEO_FRAME))
				{
				val = msg.Split(',');
				if (val.Length == 2)
					{
					SubSystemOpForm.tconnect.Send(UiConstants.OK);
					vlen = int.Parse(val[1]);
					ms[indx] = new MemoryStream();
					rvlen = SubSystemOpForm.tconnect.ReceiveStream(ref ms[indx],vlen);
					if (vlen == rvlen)
						{
						rtn = true;
						}
					else
						{
						this.BeginInvoke(vuf, "VideoReceive: Incorrect video stream length");
						Log.LogEntry("incorrect video stream length");
						}
					}
				else
					{
					SubSystemOpForm.tconnect.Send(UiConstants.FAIL);
					this.BeginInvoke(vuf, "VideoReceive: Incorrect response format");
					Log.LogEntry("bad response format");
					}
				}
			else
				{
				this.BeginInvoke(vuf,"VideoReceive: No video stream to receive");
				Log.LogEntry("no video stream to receive");
				}
			return(rtn);
		}



		private void DisplayDepth()

		{
			Bitmap bm;
			Graphics g;
			int row, col;

			bm = depthmap.ToBitmap(KinectOp.video_frame_width, KinectOp.video_frame_height, 0, Color.White);
			g = System.Drawing.Graphics.FromImage(bm);
			bm.RotateFlip(RotateFlipType.Rotate180FlipY);
			row = (int)Math.Round((double)KinectOp.video_frame_height / 2);
			g.DrawLine(Pens.Red, 0, row, KinectOp.video_frame_width, row);
			col = (int)ColNumericUpDown.Value;
			g.DrawLine(Pens.Red, col, 0, col, KinectOp.video_frame_height);
			DepthPictureBox.Image = bm;
			bm = null;
		}



		private void DUpdate()

		{
			DisplayDepth();
			MeasureButton.Enabled = true;
		}



		private void DepthFail()

		{
			StatusTextBox.AppendText("Depth map download failed.\r\n");
			ShootButton.Enabled = true;
		}



		private bool DepthMapReceive()

		{
			string msg;
			string[] val;
			int vlen,rvlen;
			MemoryStream ms = new MemoryStream();
			BinaryReader br;
			int i;
			bool rtn = false;

			msg = SubSystemOpForm.tconnect.ReceiveResponse(200,true);
			if (msg.StartsWith(UiConstants.DEPTH_FRAME))
				{
				val = msg.Split(',');
				if (val.Length == 2)
					{
					vlen = int.Parse(val[1]);
					if (vlen == depthmap.Length * 2)
						{
						SubSystemOpForm.tconnect.Send(UiConstants.OK);
						rvlen = SubSystemOpForm.tconnect.ReceiveStream(ref ms,vlen);
						if (vlen == rvlen)
							{
							ms.Seek(0,SeekOrigin.Begin);
							br = new BinaryReader(ms);
							for (i = 0;i < depthmap.Length;i++)
								depthmap[i] = br.ReadInt16();
							rtn = true;
							}
						else
							{
							this.BeginInvoke(duf);
							Log.LogEntry("DepthMapReceive incorrect length");
							}
						}
					else
						{
						SubSystemOpForm.tconnect.Send(UiConstants.FAIL);
						this.BeginInvoke(duf);
						Log.LogEntry("DepthMapReceive bad stream length");
						}
					}
				else
					{
					SubSystemOpForm.tconnect.Send(UiConstants.FAIL);
					this.BeginInvoke(duf);
					Log.LogEntry("DepthMapRecieve bad response format");
					}
				}
			else
				{
				this.BeginInvoke(duf);
				Log.LogEntry("DepthMapRecieve no depth map to receive");
				}
			return(rtn);
		}



		private void ReciveVDM()

		{
			string rsp;

			if (VideoReceive())
				{
				rsp = SubSystemOpForm.tconnect.SendCommand(UiConstants.SEND_DEPTH_MAP, 100);
				if (rsp.StartsWith(UiConstants.OK))
					{
					if (DepthMapReceive())
						{
						this.BeginInvoke(du);
						}
					}
				this.Invoke(vu, indx);
				indx = (indx + 1) % 2;
				SubSystemOpForm.tool_access.Release();
				}
			else
				SubSystemOpForm.tool_access.Release();
		}



		private void ShootButton_Click(object sender, EventArgs e)

		{
			string rsp;
			Thread rcv;

			SubSystemOpForm.tool_access.Wait();
			rsp = SubSystemOpForm.tconnect.SendCommand(UiConstants.SEND_VIDEO,100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				ShootButton.Enabled = false;
				rcv = new Thread(ReciveVDM);
				rcv.Start();
				}
			else
				{
				SubSystemOpForm.tool_access.Release();
				VideoPictureBox.Image = KinectOp.blank;
				DepthPictureBox.Image = KinectOp.blank;
				StatusTextBox.AppendText(rsp + "\r\n");
				}
		}



		public double VideoHorDegrees(int no_pixel)

		{
			double val = 0;
			double adj;

			adj = ((double)KinectOp.video_frame_width / 2) / Math.Tan((KinectOp.video_hor_fov /2) * MathConvert.DEG_TO_RAD);
			val = Math.Atan(no_pixel/adj) * MathConvert.RAD_TO_DEG;
			return (val);
		}



		private void MeasureButton_Click(object sender, EventArgs e)

		{
			int row,col,hdist;
			double dist,rax;
			ListViewItem itm;
			MeasuredDistInputForm mdif;

			row = (int) Math.Round((double)KinectOp.video_frame_height /2);
			col = (int) ColNumericUpDown.Value;
			hdist = depthmap[(row * KinectOp.video_frame_width) + (KinectOp.video_frame_width - col)];
			if (hdist > 0)
				{
				dist = hdist * MathConvert.MM_TO_IN;
				rax = Math.Abs(VideoHorDegrees(col - (KinectOp.video_frame_width / 2)));
				dist = dist/Math.Cos(rax * MathConvert.DEG_TO_RAD);
				itm = DistListView.Items.Add(dist.ToString("F2"));
				Application.DoEvents();
				mdif = new MeasuredDistInputForm();
				mdif.StartPosition = FormStartPosition.CenterParent;
				if (mdif.ShowDialog() == DialogResult.OK)
					{
					dist = mdif.measured_dist;
					itm.SubItems.Add(dist.ToString());
					MeasureButton.Enabled = false;
					NextPosButton.Enabled = true;
					}
				else
					DistListView.Items.Remove(itm);
				}
			else
				StatusTextBox.AppendText("No distance measurement available.\r\n");
		}



		private void SaveButton_Click(object sender, EventArgs e)

		{
			TextWriter tw;
			string fname;
			DateTime now = DateTime.Now;

			fname = Application.StartupPath + Folders.CAL_SUB_DIR + PARAM_FILE + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + " .param";
			tw = File.CreateText(fname);
			if (tw != null)
				{
				tw.WriteLine(DistListView.Items.Count);
				foreach (ListViewItem itm in DistListView.Items)
					{
					tw.WriteLine(itm.Text + "," + itm.SubItems[1].Text);
					}
				tw.Close();
				}
			else
				StatusTextBox.AppendText("Could not open parameter file.\r\n");
		}




		private void ColNumericUpDown_ValueChanged(object sender, EventArgs e)

		{
			DisplayDepth();
			DisplayImage((Bitmap) vimg.Clone());
		}

		}
	}
