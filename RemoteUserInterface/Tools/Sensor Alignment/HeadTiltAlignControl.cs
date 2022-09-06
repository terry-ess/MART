using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;
using Coding4Fun.Kinect.WinForm;


namespace Sensor_Alignment
	{
	public partial class HeadTiltAlignControl : UserControl
		{
		private const string TOOL_NAME = "Head tilt alignment";
		private const string PARAM_FILE = "head tilt";


		private delegate void VideoUpdate(int indx);
		private VideoUpdate vu;
		private delegate void VUFail(string err);
		private VUFail vuf;
		private MemoryStream[] ms = new MemoryStream[2];
		private int indx = 0;
		private Image vimg;
		private Bitmap blank;


		public HeadTiltAlignControl()

		{
			InitializeComponent();
			ParamListView.ListViewItemSorter = new  ListViewComparer();
		}



		public void Open()

		{
			int i,j;

			ParamListView.Items.Clear();
			if (PanTiltUpdate())
				{
				blank = new Bitmap(VideoPictureBox.Width,VideoPictureBox.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				for (i = 0; i < VideoPictureBox.Width; i++)
					for (j = 0; j < VideoPictureBox.Height; j++)
						blank.SetPixel(i, j, Color.White);
				VideoPictureBox.Image = blank;
				}
			else
				{
				TiltNumericUpDown.Value = 0;
				PanNumericUpDown.Value = 0;
				StatusTextBox.AppendText("Could not establish link with robot.\r\n");
				this.Enabled = false;
				}
			vu = VidUpdate;
			vuf = VidUpdateFail;
		}



		public void Close()

		{
			SensorAlignForm.SendToolCommand(UiConstants.SET_PAN_TILT + ",0,0",10);
		}



		private bool PanTiltUpdate()

		{
			string rsp;
			string[] values;
			bool rtn = false;

			rsp = SensorAlignForm.SendToolCommand(UiConstants.CURRENT_PAN, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 2)
					{
					PanNumericUpDown.Value = int.Parse(values[1]);
					rtn = true;
					}
				else
					{
					PanNumericUpDown.Value = 0;
					Log.LogEntry((UiConstants.CURRENT_PAN + " failed, bad reply format"));
					StatusTextBox.AppendText(UiConstants.CURRENT_PAN + " failed, bad reply format\r\n");
					}
				}
			else
				StatusTextBox.AppendText("Could not obtain current pan.\r\n");
			if (rtn)
				{
				rtn = false;
				rsp = SensorAlignForm.SendToolCommand(UiConstants.CURRENT_TILT, 100);
				if (rsp.StartsWith(UiConstants.OK))
					{
					values = rsp.Split(',');
					if (values.Length == 2)
						{
						TiltNumericUpDown.Value = int.Parse(values[1]);
						rtn = true;
						}
					else
						{
						TiltNumericUpDown.Value = 0;
						Log.LogEntry((UiConstants.CURRENT_TILT + " failed, bad reply format"));
						StatusTextBox.AppendText(UiConstants.CURRENT_TILT + " failed, bad reply format\r\n");
						}
					}
				}
			else
				StatusTextBox.AppendText("Could not obtain current tilt.\r\n");
			return (rtn);
		}



		private void DisplayImage(Image img)

		{
			Graphics g;
			int row;

			g = System.Drawing.Graphics.FromImage(img);
			row = (int)RowNumericUpDown.Value;
			g.DrawLine(Pens.Red, 0, row,img.Width, row);
			VideoPictureBox.Image = img;
		}



		private void VidUpdate(int indx)

		{
			Image img;

			img = Image.FromStream(ms[indx]);
			img.RotateFlip(RotateFlipType.Rotate180FlipY);
			vimg = (Image) img.Clone();
			DisplayImage(img);
			ShootButton.Enabled = true;
			MeasureButton.Enabled = true;
		}



		private void VidUpdateFail(string err)

		{
			VideoPictureBox.Image = blank;
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

			msg = SensorAlignForm.tconnect.ReceiveResponse(100,true);
			if (msg.StartsWith(UiConstants.VIDEO_FRAME))
				{
				val = msg.Split(',');
				if (val.Length == 2)
					{
					SensorAlignForm.tconnect.Send(UiConstants.OK);
					vlen = int.Parse(val[1]);
					ms[indx] = new MemoryStream();
					rvlen = SensorAlignForm.tconnect.ReceiveStream(ref ms[indx],vlen);
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
					SensorAlignForm.tconnect.Send(UiConstants.FAIL);
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



		private void ReciveVD()

		{
			if (VideoReceive())
				{
				this.BeginInvoke(vu, indx);
				indx = (indx + 1) % 2;
				SensorAlignForm.tool_access.Release();
				}
			else
				SensorAlignForm.tool_access.Release();
		}



		private void ShootButton_Click(object sender, EventArgs e)

		{
			string rsp;
			Thread rcv;

			SensorAlignForm.tool_access.Wait();
			rsp = SensorAlignForm.tconnect.SendCommand(UiConstants.SEND_VIDEO,100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				rcv = new Thread(ReciveVD);
				rcv.Start();
				}
			else
				{
				SensorAlignForm.tool_access.Release();
				VideoPictureBox.Image = VideoPictureBox.ErrorImage;
				StatusTextBox.AppendText(rsp + "\r\n");
				}
		}



		private void RowColChange(object sender,EventArgs e)

		{
			Image img;

			img = (Image) vimg.Clone();
			DisplayImage(img);
		}



		private void SPButton_Click(object sender, EventArgs e)

		{
			string rsp;
			string[] values;

			rsp = SensorAlignForm.SendToolCommand(UiConstants.SET_PAN_TILT + "," + PanNumericUpDown.Text + "," + TiltNumericUpDown.Text,1000);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 3)	
					{
					TiltNumericUpDown.Value = int.Parse(values[2]);
					}
				else
					{
					TiltNumericUpDown.Value = 0;
					StatusTextBox.AppendText(UiConstants.SET_PAN_TILT + " incorrect response format\r\n");
					}
				}
			else
				{
				TiltNumericUpDown.Value = 0;
				StatusTextBox.AppendText(rsp + "\r\n");
				}
		}



		private void MeasureButton_Click(object sender, EventArgs e)

		{
			int dr, row;
			string rsp;
			string[] values;

			row = (int) RowNumericUpDown.Value;
			dr = (int)Math.Round(((double)vimg.Height / 2) - row);
			rsp = SensorAlignForm.SendToolCommand(UiConstants.DETERMINE_ANGLES + ",0," + dr, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 3)
					{
					VATextBox.Text = values[2];
					AVATextBox.Text = ((int) TiltNumericUpDown.Value + double.Parse(values[2])).ToString("F4");
					}
				else
					{
					VATextBox.Text = "unk";
					StatusTextBox.AppendText(UiConstants.DETERMINE_ANGLES + " incorrect response format\r\n");
					}
				}
			else
				{
				VATextBox.Text = "unk";
				StatusTextBox.AppendText(rsp + "\r\n");
				}
		}


		
		private void ARButton_Click(object sender, EventArgs e)

		{
			int tilt;
			ListViewItem item;

			try
			{
			tilt = (int) TiltNumericUpDown.Value;
			if (ParamListView.Items.ContainsKey(tilt.ToString()))
				ParamListView.Items.RemoveByKey(tilt.ToString());
			item = new ListViewItem(tilt.ToString());
			item.Name = tilt.ToString();
			item.SubItems.Add(AVATextBox.Text);
			ParamListView.Items.Add(item);
			}

			catch (Exception ex)
			{
			StatusTextBox.AppendText("Exception: " + ex.Message + "\r\n");
			}

		}


		
		private void SendButton_Click(object sender, EventArgs e)

		{
			TextWriter tw;
			string fname,nfname;
			DateTime now = DateTime.Now;

			fname = Application.StartupPath + Folders.CAL_SUB_DIR + PARAM_FILE + ".param";
			if (File.Exists(fname))
				{
				nfname = fname + "." + DateTime.Now.Ticks;
				File.Copy(fname, nfname);
				File.Delete(fname);
				}
			tw = File.CreateText(fname);
			if (tw != null)
				{
				tw.WriteLine(ParamListView.Items.Count);
				foreach (ListViewItem itm in ParamListView.Items)
					tw.WriteLine(itm.Text + "," + itm.SubItems[1].Text);
				tw.Close();
				}
			else
				StatusTextBox.AppendText("Could not open parameter file.\r\n");
		}



		private void ClrButton_Click(object sender, EventArgs e)

		{
			ParamListView.Items.Clear();
		}


		}



	class ListViewComparer:IComparer

		{
			public int Compare(object x,object y)

			{
				int a,b;

				a = int.Parse(((ListViewItem) x).Text);
				b = int.Parse(((ListViewItem) y).Text);
				return(a - b);
			}
		}
	}
