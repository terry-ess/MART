using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;


namespace RemoteUserInterface
	{
	public partial class HeadTiltAlignControl: Control
		{

		private const string TOOL_NAME = "Head tilt alignment";

		private delegate void VideoUpdate(int indx);
		private VideoUpdate vu;
		private delegate void VUFail();
		private VUFail vuf;
		private MemoryStream[] ms = new MemoryStream[2];
		private Image vimg;
		private Connection tcntl = null;
		private Connection tconnect = null;
		private SemaphoreSlim tool_access = new SemaphoreSlim(1);


		public HeadTiltAlignControl()

		{
			InitializeComponent();
		}



		public bool Open()

		{
			string rsp;
			string[] values;
			bool started = false;

						rsp = SendToolCommand(UiConstants.CURRENT_PAN,100);
						if (rsp.StartsWith(UiConstants.OK))
							{
							values = rsp.Split(',');
							if (values.Length == 2)
								PanTextBox.Text = values[1];
							else
								PanTextBox.Text = "unk";
							}
						else
							PanTextBox.Text = "unk";
						rsp = SendToolCommand(UiConstants.CURRENT_TILT, 100);
						if (rsp.StartsWith(UiConstants.OK))
							{
							values = rsp.Split(',');
							if (values.Length == 2)
								TiltTextBox.Text = values[1];
							else
								TiltTextBox.Text = "unk";
							}
						else
							TiltTextBox.Text = "unk";
						vu = VidUpdate;
						vuf = VidUpdateFail;
						SPButton_Click(null,null);
						started = true;
			return(started);
		}



		private string SendToolCommand(string msg,int timeout_count)


		{
			string rtn = "";

			tool_access.Wait();
			rtn = tconnect.SendCommand(msg,timeout_count);
			tool_access.Release();
			return(rtn);
		}



		private void DisplayImage(Image img)

		{
			Graphics g;
			int row, col;

			g = System.Drawing.Graphics.FromImage(img);
			row = (int)RowNumericUpDown.Value;
			col = (int)ColNumericUpDown.Value;
			g.DrawLine(Pens.Red, 0, row,img.Width, row);
			g.DrawLine(Pens.Red, col, 0, col,img.Height);
			VideoPictureBox.Image = img;
		}



		private void VidUpdate(int indx)

		{
			Image img;

			img = Image.FromStream(ms[indx]);
			img.RotateFlip(RotateFlipType.Rotate180FlipY);
			vimg = (Image) img.Clone();
			DisplayImage(img);
			ShootButton.BackColor = Color.FromKnownColor(KnownColor.Control);
			ShootButton.Enabled = true;
			MeasureButton.Enabled = true;
		}



		private void VidUpdateFail()

		{
			ShootButton.BackColor = Color.Red;
			ShootButton.Enabled = true;
		}


		private void VideoReceive()

		{
			string msg;
			string[] val;
			int vlen,rvlen,indx = 0;
			object[] obj = new object[2];

			msg = tconnect.ReceiveResponse(100,true);
			if (msg.StartsWith(UiConstants.VIDEO_FRAME))
				{
				val = msg.Split(',');
				if (val.Length == 2)
					{
					tconnect.Send(UiConstants.OK);
					vlen = int.Parse(val[1]);
					ms[indx] = new MemoryStream();
					rvlen = tconnect.ReceiveStream(ref ms[indx],vlen);
					if (vlen == rvlen)
						{
						this.BeginInvoke(vu,indx);
						indx = (indx + 1) % 2;
						}
					else
						{
						this.BeginInvoke(vuf);
						Log.LogEntry("incorrect video stream length");
						}
					}
				else
					{
					this.BeginInvoke(vuf);
					Log.LogEntry("bad response format");
					}
				}
			else
				{
				this.BeginInvoke(vuf);
				Log.LogEntry("no video stream to receive");
				}
			tool_access.Release();
		}



		private void ShootButton_Click(object sender, EventArgs e)

		{
			string rsp;
			Thread vrcv;


			tool_access.Wait();
			rsp = tconnect.SendCommand(UiConstants.SEND_VIDEO,100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				vrcv = new Thread(VideoReceive);
				vrcv.Start();
				ShootButton.Enabled = false;
				}
			else
				tool_access.Release();
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

			rsp = SendToolCommand(UiConstants.SET_PAN_TILT + "," + PanTextBox.Text + "," + TiltTextBox.Text,1000);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 3)
					{
					PanTextBox.Text = values[1];
					TiltTextBox.Text = values[2];
					}
				else
					{
					PanTextBox.Text = "unk";
					TiltTextBox.Text = "unk";
					}
				}
		}



		private void TOButton_Click(object sender, EventArgs e)

		{
			string rsp;

			rsp = SendToolCommand(UiConstants.TORQUE_ON, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				TOButton.BackColor = Color.FromKnownColor(KnownColor.Control);
				}
			else
				{
				TOButton.BackColor = Color.Red;
				}
		}



		private void CEButton_Click(object sender, EventArgs e)

		{
			string rsp;

			rsp = SendToolCommand(UiConstants.CLEAR_ERROR, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				CEButton.BackColor = Color.FromKnownColor(KnownColor.Control);
				}
			else
				{
				CEButton.BackColor = Color.Red;
				}
		}



		private void MeasureButton_Click(object sender, EventArgs e)

		{
			int dc,dr,col,row;
			string rsp;
			string[] values;

			col = (int) ColNumericUpDown.Value;
			dc = (int)Math.Round(col - ((double) vimg.Width / 2));
			row = (int) RowNumericUpDown.Value;
			dr = (int)Math.Round(((double) vimg.Height / 2) - row);
			rsp = SendToolCommand(UiConstants.DETERMINE_ANGLES + "," + dc + "," + dr, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 3)
					{
					HATextBox.Text = values[1];
					VATextBox.Text = values[2];
					}
				else
					{
					HATextBox.Text = "unk";
					VATextBox.Text = "unk";
					}
				}
			else
				{
				HATextBox.Text = "unk";
				VATextBox.Text = "unk";
				}
		}


		}
	}
