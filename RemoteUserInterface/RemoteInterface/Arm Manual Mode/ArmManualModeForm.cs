using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;
using Coding4Fun.Kinect.WinForm;

namespace Arm_Manual_Mode
	{
	public partial class ArmManualModeForm : Form
		{

		public struct Loc3D
		{
		public double x;
		public double y;
		public double z;

		public Loc3D(double x,double y,double z)

		{
			this.x = x;
			this.y = y;
			this.z = z;
		}


		public override string ToString()

		{
			string rtn = "";

			rtn = "(" + this.x.ToString("F3") + ", " + this.y.ToString("F3") + ", " + this.z.ToString("F3")  + ")";
			return(rtn);
		}


		};


		public const double TILT_CORRECT = -4.0;
		public const double HEIGHT_CORRECT = .4;
		private const string PARAM_FILE = "kinectcal.param";

		private string tool_name;
		private short[] depthdata;
		public double video_vert_fov = 0;
		public double video_hor_fov = 0;
		public int video_frame_width = 0;
		public int video_frame_height = 0;
		public Bitmap blank;
		private MemoryStream[] ms = new MemoryStream[2];
		private int indx = 0;
		private delegate void VideoUpdate(int indx);
		private VideoUpdate vu;
		private delegate void VUFail(string err);
		private VUFail vuf;
		private delegate void DepthUpdate();
		private DepthUpdate du;
		private delegate void DUFail(string err);
		private DUFail duf;
		private Loc3D target_loc;
		private Image vimg;

		private Connection tconnect = null;
		private SemaphoreSlim tool_access = new SemaphoreSlim(1);

		private Connection kaconnect = null;
		private System.Timers.Timer katimer = null;
		private bool keep_alive_active = false;
		private IPEndPoint karcvr;

		private double arm_height;
		private double arm_full_len;
		private double wp_top_height;
		private double wp_edge_dist;
		private bool arm_wp_data_avail = false;

		private Stopwatch sw = new Stopwatch();


		public ArmManualModeForm()

		{
			string fname,rsp;
			DateTime fdt;
			string[] val;
			int i,j;

			InitializeComponent();
			tool_name = new AssemblyName(Assembly.GetCallingAssembly().FullName).Name;
			fname = Application.StartupPath + Folders.TOOLS_SUB_DIR + tool_name + ".dll";
			fdt = File.GetLastWriteTime(fname);
			this.Text = this.Text + "  " + fdt.ToShortDateString() + "  " + fdt.ToShortTimeString();
			tconnect = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_PORT_NO, true, 1, true);
			kaconnect = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_KEEP_ALIVE_PORT_NO, false);
			if (tconnect.Connected() && kaconnect.Connected())
				{
				StatusTextBox.AppendText("Tool connections opened.\r\n");
				keep_alive_active = true;
				karcvr = new IPEndPoint(IPAddress.Parse(MainControl.robot_ip_address), UiConstants.TOOL_KEEP_ALIVE_PORT_NO);
				katimer = new System.Timers.Timer(500);
				katimer.Elapsed += KeepAliveTimer;
				katimer.Enabled = true;
				rsp = SendToolCommand(UiConstants.SEND_VIDEO_PARAM, 20);
				if (rsp.StartsWith(UiConstants.OK))
					{
					val = rsp.Split(',');
					if (val.Length == 5)
						{
						video_frame_width = int.Parse(val[1]);
						video_frame_height = int.Parse(val[2]);
						video_hor_fov = float.Parse(val[3]);
						video_vert_fov = float.Parse(val[4]);
						depthdata = new short[video_frame_width * video_frame_height];
						blank = new Bitmap(video_frame_width, video_frame_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
						for (i = 0; i < video_frame_width; i++)
							for (j = 0; j < video_frame_height; j++)
								blank.SetPixel(i, j, Color.White);
						vu = VidUpdate;
						vuf = VidUpdateFail;
						du = DepthUD;
						duf = DepthUF;
						PanTiltUpdate();
						rsp = SendToolCommand(UiConstants.SEND_ARM_WP_DATA, 20);
						if (rsp.StartsWith(UiConstants.OK))
							{
							val = rsp.Split(',');
							if (val.Length == 5)
								{
								arm_height = double.Parse(val[1]);
								arm_full_len = double.Parse(val[2]);
								wp_top_height = double.Parse(val[3]);
								wp_edge_dist = double.Parse(val[4]);
								YNumericUpDown.Minimum = (decimal) wp_top_height + 2;
								ZNumericUpDown.Maximum = (decimal) arm_full_len;
								arm_wp_data_avail = true;
								FFPButton.Enabled = true;
								}
							else
								{
								StatusTextBox.AppendText("\r\nArm - work place data response incorrect format.");
								arm_wp_data_avail = false;
								}
							}
						else
							{
							StatusTextBox.AppendText("\r\nCould not get arm - work place data.");
							arm_wp_data_avail = false;
							}
						}
					else
						{
						StatusTextBox.AppendText("\r\nVideo parameters response incorrect format.");
						this.Enabled = false;
						}
					}
				else
					{
					this.Enabled = false;
					StatusTextBox.AppendText("\r\nCould not get video parameters.");
					}

				}
		}



		private void PanTiltUpdate()

		{
			string rsp;
			string[] values;

			rsp = SendToolCommand(UiConstants.CURRENT_PAN, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 2)
					{
					PanNumericUpDown.Value = int.Parse(values[1]);
					}
				else
					{
					PanNumericUpDown.Value = 0;
					Log.LogEntry((UiConstants.CURRENT_PAN + " failed, bad reply format"));
					}
				}
			rsp = SendToolCommand(UiConstants.CURRENT_TILT, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 2)
					{
					TiltNumericUpDown.Value = int.Parse(values[1]);
					}
				else
					{
					TiltNumericUpDown.Value = 0;
					Log.LogEntry((UiConstants.CURRENT_TILT + " failed, bad reply format"));
					}
				}
		}



		private void DisplayImage()

		{
			Graphics g;
			int row,col;
			Image imgc;

			imgc = (Image) vimg.Clone();
			g = Graphics.FromImage(imgc);
			row = (int)RowNumericUpDown.Value;
			col = (int)ColNumericUpDown.Value;
			g.DrawLine(Pens.Red, 0, row,video_frame_width, row);
			g.DrawLine(Pens.Red, col, 0, col,video_frame_height);
			VideoPicBox.Image = imgc;
		}



		private void DisplayDepth()

		{
			Graphics g;
			int row,col,df,i;
			Bitmap dbm;
			short[] tdd;

			df = (int) DFNumericUpDown.Value;
			if (df == 1)
				dbm = depthdata.ToBitmap(video_frame_width, video_frame_height, 0, Color.White);
			else
				{
				tdd = new short[depthdata.Length];
				for (i = 0; i < depthdata.Length; i++)
					tdd[i] = (short)(depthdata[i] * df);
				dbm = tdd.ToBitmap(video_frame_width,video_frame_height, 0, Color.Blue);
				}
			dbm.RotateFlip(RotateFlipType.Rotate180FlipY);
			g = System.Drawing.Graphics.FromImage(dbm);
			row = (int)RowNumericUpDown.Value;
			col = (int)ColNumericUpDown.Value;
			g.DrawLine(Pens.Red, 0, row, video_frame_width, row);
			g.DrawLine(Pens.Red, col, 0, col,video_frame_height);
			DepthPicBox.Image = dbm;
			dbm = null;
		}



		private string SendToolCommand(string msg,int timeout_count,bool log = true)


		{
			string rtn = "";

			tool_access.Wait();
			rtn = tconnect.SendCommand(msg,timeout_count,log);
			tool_access.Release();
			return(rtn);
		}



		private int GetTilt()

		{
			string rsp;
			string[] values;
			int rtn = 0;

			rsp = SendToolCommand(UiConstants.CURRENT_TILT, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 2)
					{
					rtn = int.Parse(values[1]);
					}
				else
					{
					Log.LogEntry(UiConstants.CURRENT_TILT + " failed, bad reply format");
					StatusTextBox.AppendText("GetTilt: failed, bad replay format\r\n");
					}
				}
			else
				StatusTextBox.AppendText("GetTilt: " + rsp);
			return (rtn);
		}



		private void DepthUD()

		{
			DisplayDepth();
			ShootButton.Enabled = true;
			MeasureButton.Enabled = true;
		}



		private void DepthUF(string err)

		{
			DepthPicBox.Image = blank;
			StatusTextBox.AppendText(err + "\r\n");
			ShootButton.Enabled = true;
			MeasureButton.Enabled = false;
		}



		private void DepthReceive()

		{
			string msg;
			string[] val;
			int vlen,rvlen;
			MemoryStream ms = new MemoryStream();
			BinaryReader br;
			int i;

			msg = tconnect.ReceiveResponse(100,true);
			if (msg.StartsWith(UiConstants.DEPTH_MAP))
				{
				val = msg.Split(',');
				if (val.Length == 2)
					{
					tconnect.Send(UiConstants.OK);
					vlen = int.Parse(val[1]);
					if (vlen == depthdata.Length * 2)
						{
						rvlen = tconnect.ReceiveStream(ref ms,vlen);
						if (vlen == rvlen)
							{
							ms.Seek(0,SeekOrigin.Begin);
							br = new BinaryReader(ms);
							for (i = 0;i < depthdata.Length;i++)
								depthdata[i] = br.ReadInt16();
							this.BeginInvoke(du);
							}
						else
							{
							this.BeginInvoke(duf, "DepthReceive incorrect length");
							Log.LogEntry("DepthReceive incorrect length");
							}
						}
					else
						{
						this.BeginInvoke(duf, "DepthReceive bad stream length");
						Log.LogEntry("DepthReceive bad stream length");
						}
					}
				else
					{
					this.BeginInvoke(duf, "DepthRecieve bad response format");
					Log.LogEntry("DepthRecieve bad response format");
					}
				}
			else
				{
				this.BeginInvoke(duf, "DepthRecieve no video stream to receive");
				Log.LogEntry("DepthRecieve no video stream to receive");
				}
			tool_access.Release();
		}



		private void VidUpdate(int indx)

		{
			vimg = Image.FromStream(ms[indx]);
			DisplayImage();
			DepthPicBox.Image = blank;
			ShootButton.Enabled = true;
		}



		private void VidUpdateFail(string err)

		{
			VideoPicBox.Image = blank;
			DepthPicBox.Image = blank;
			StatusTextBox.AppendText(err + "\r\n");
			ShootButton.Enabled = true;
			MeasureButton.Enabled = false;
		}



		private bool VideoReceive()

		{
			string msg;
			string[] val;
			int vlen,rvlen;
			object[] obj = new object[2];
			bool rtn = false;

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
						rtn = true;
						this.BeginInvoke(vu,indx);
						indx = (indx + 1) % 2;
						}
					else
						{
						this.BeginInvoke(vuf, "VideoReceive: Incorrect video stream length");
						Log.LogEntry("incorrect video stream length");
						}
					}
				else
					{
					tconnect.Send(UiConstants.FAIL);
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



		private void ReceiveVD()

		{
			string rsp;

			if (VideoReceive())
				{
				rsp = tconnect.SendCommand(UiConstants.SEND_DEPTH_MAP, 100);
				if (rsp.StartsWith(UiConstants.OK))
					{
					DepthReceive();
					}
				}
			else
				tool_access.Release();
		}



		private void ShootButton_Click(object sender, EventArgs e)

		{
			string rsp;
			Thread rcv;

			tool_access.Wait();
			PosButton.Enabled = false;
			MeasureButton.Enabled = false;
			rsp = tconnect.SendCommand(UiConstants.SEND_VIDEO, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				rcv = new Thread(ReceiveVD);
				rcv.Start();
				}
			else
				{
				tool_access.Release();
				VideoPicBox.Image = blank;
				DepthPicBox.Image = blank;
				StatusTextBox.AppendText(rsp + "\r\n");
				}
		}



		private void MeasureButton_Click(object sender, EventArgs e)

		{
			int row,col;
			string rsp,fname;
			string[] val;

			row = (int) RowNumericUpDown.Value;
			col = (int) ColNumericUpDown.Value;
			rsp = SendToolCommand(UiConstants.LOCATION_CALC + "," + row + "," + col,1000,true);
			if (rsp.StartsWith(UiConstants.OK))
				{
				val = rsp.Split(',');
				if (val.Length == 4)
					{
					RCLocTextBox.Text = val[1] + "," + val[2] + "," + val[3];
					target_loc.x = double.Parse(val[1]);
					target_loc.y = double.Parse(val[2]);
					target_loc.z = double.Parse(val[3]);
					PosButton.Enabled = true;
					fname = Log.LogDir() + "measure video pic .jpg";
					VideoPicBox.Image.Save(fname,ImageFormat.Jpeg);
					Log.LogEntry("Saved: " + fname);
					fname = Log.LogDir() + "measure depth pic .bmp";
					DepthPicBox.Image.Save(fname,ImageFormat.Bmp);
					Log.LogEntry("Saved: " + fname);
					}
				else
					StatusTextBox.AppendText("Bad response format\r\n");
				}
			else
				StatusTextBox.AppendText(rsp + "\r\n");
		}



		private void PosButton_Click(object sender, EventArgs e)

		{
			string rsp,cmd;

			if (RPCheckBox.Checked)
				cmd = UiConstants.RAW_ARM_TO_POSITION + "," + target_loc.x.ToString("F1") + "," + target_loc.y.ToString("F1") + "," + target_loc.z.ToString("F1") + ",true,true,false";
			else
				cmd = UiConstants.ARM_TO_POSITION + "," + target_loc.x.ToString("F1") + "," + target_loc.y.ToString("F1") + "," + target_loc.z.ToString("F1") + "," + PONumericUpDown.Value + ",true";
			rsp = SendToolCommand(cmd,10000);
			if (rsp.StartsWith(UiConstants.FAIL))
				StatusTextBox.AppendText(rsp + "\r\n");
			else
				{
				sw.Start();
				StatusTextBox.AppendText("Arm is positioned.\r\n");
				StopButton.Enabled = true;
				StopButton.BackColor = Color.Red;
				}
			PosButton.Enabled = false;
		}



		private void RWTButton_Click(object sender, EventArgs e)

		{
			string rsp,cmd;

			cmd = UiConstants.TEST_SWEEP + "," + target_loc.x.ToString("F1") + "," + target_loc.y.ToString("F1") + "," + target_loc.z.ToString("F1");
			rsp = SendToolCommand(cmd, 10000);
			if (rsp.StartsWith(UiConstants.FAIL))
				StatusTextBox.AppendText("Test obstacle sweep failed: " + rsp + "\r\n");
			else
				StatusTextBox.AppendText("Test obstacle sweep was successful.\r\n");
		}



		private void MPButton_Click(object sender, EventArgs e)

		{
			string rsp,cmd;

			cmd = UiConstants.RAW_ARM_TO_POSITION + "," + XNumericUpDown.Value + "," + YNumericUpDown.Value + "," + ZNumericUpDown.Value + ",true,true,false";
			rsp = SendToolCommand(cmd,10000);
			if (rsp.StartsWith(UiConstants.FAIL))
				StatusTextBox.AppendText(rsp + "\r\n");
			else
				{
				sw.Start();
				StatusTextBox.AppendText("Arm is positioned.\r\n");
				StopButton.Enabled = true;
				StopButton.BackColor = Color.Red;
				}
		}



		private void FFPButton_Click(object sender, EventArgs e)

		{
			double x,y,z;
			string rsp,cmd;

			if (arm_wp_data_avail)
				{
				x = 0;
				if (arm_height > wp_top_height)
					{
					y = arm_height;
					z = arm_full_len;
					}
				else
					{
					y = z = 0;	//need L2 and l3 to calc y and z, do latter
					StatusTextBox.AppendText("Current implementation can not calc full forward when arm height lower then work place top.");
					}
				if ((y != 0) && (z != 0))
					{
					cmd = UiConstants.RAW_ARM_TO_POSITION + "," + x + "," + y + "," + z + ",true,true,false";
					rsp = SendToolCommand(cmd, 10000);
					if (rsp.StartsWith(UiConstants.FAIL))
						StatusTextBox.AppendText(rsp + "\r\n");
					else
						{
						sw.Start();
						StatusTextBox.AppendText("Arm is positioned.\r\n");
						StopButton.Enabled = true;
						StopButton.BackColor = Color.Red;
						}
					}
				}
			else
				FFPButton.Enabled = false;
		}



		private void ParkButton_Click(object sender, EventArgs e)

		{
			string rsp;

			rsp = SendToolCommand(UiConstants.ARM_TO_PARK + ",true",10000);
			if (rsp.StartsWith(UiConstants.FAIL))
				StatusTextBox.AppendText(rsp + "\r\n");
			else
				{
				sw.Start();
				StopButton.Enabled = true;
				StopButton.BackColor = Color.Red;
				StatusTextBox.AppendText("Arm is parked.\r\n");
				}
		}



		private void StartButton_Click(object sender, EventArgs e)

		{
			string rsp;

			rsp = SendToolCommand(UiConstants.ARM_TO_START,1000);
			if (rsp.StartsWith(UiConstants.OK))
				{
				sw.Start();
				StatusTextBox.AppendText("Arm is at start position.\r\n");
				StopButton.Enabled = true;
				StopButton.BackColor = Color.Red;
				}
			else
				StatusTextBox.AppendText(rsp + "\r\n");
		}



		private void EEButton_Click(object sender, EventArgs e)

		{
			string rsp;

			rsp = SendToolCommand(UiConstants.ARM_TO_EE,1000);
			if (rsp.StartsWith(UiConstants.OK))
				{
				sw.Start();
				StatusTextBox.AppendText("Arm is at entry/exiy position.\r\n");
				StopButton.Enabled = true;
				StopButton.BackColor = Color.Red;
				}
			else
				StatusTextBox.AppendText(rsp + "\r\n");
		}



		private void VisibleImageSelect(object sender,EventArgs e)

		{
			if (((RadioButton) sender).Checked == true)
				{
				if (((RadioButton)sender).Name == "VRadioButton")
					{
					DepthPicBox.Visible = false;
					VideoPicBox.Visible = true;
					DisplayImage();
					}
				else if (((RadioButton)sender).Name == "DRadioButton")
					{
					DepthPicBox.Visible = true;
					VideoPicBox.Visible = false;
					DisplayDepth();
					}
				}
		}



		public double VideoVerDegrees(int no_pixel)

		{
			double val = 0,adj;

			adj = ((double) video_frame_height/2) / Math.Tan((video_vert_fov/ 2) * MathConvert.DEG_TO_RAD);
			val = Math.Atan(no_pixel / adj) * MathConvert.RAD_TO_DEG;
			return (val);
		}



		public double VideoHorDegrees(int no_pixel)

		{
			double val = 0,adj;	//adj = 589.36

			adj = ((double) video_frame_width / 2) / Math.Tan((video_hor_fov / 2) * MathConvert.DEG_TO_RAD);
			val = Math.Atan(no_pixel / adj) * MathConvert.RAD_TO_DEG;
			return (val);
		}



		private void Location_ValueChanged(object sender, EventArgs e)

		{
			int row,col;

			row = (int) RowNumericUpDown.Value;
			col = (int) ColNumericUpDown.Value;
			VATextBox.Text = VideoVerDegrees((video_frame_height / 2) - row).ToString();
			HATextBox.Text = VideoHorDegrees(col - (video_frame_width/2)).ToString();
			if (DepthPicBox.Visible)
				DisplayDepth();
			else if (VideoPicBox.Visible)
				DisplayImage();
		}



		private void SPButton_Click(object sender, EventArgs e)

		{
			string rsp;
			string[] values;

			rsp = SendToolCommand(UiConstants.SET_PAN_TILT + "," + PanNumericUpDown.Text + "," + TiltNumericUpDown.Text,1000);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 3)	
					{
					PanNumericUpDown.Value = int.Parse(values[1]);
					TiltNumericUpDown.Value = int.Parse(values[2]);
					}
				else
					{
					PanNumericUpDown.Value = 0;
					TiltNumericUpDown.Value = 0;
					Log.LogEntry(UiConstants.SET_PAN_TILT + " incorrect response format");
					StatusTextBox.AppendText(UiConstants.SET_PAN_TILT + " incorrect response format");
					}
				}
			else
				{
				PanNumericUpDown.Value = 0;
				TiltNumericUpDown.Value = 0;
				StatusTextBox.AppendText(UiConstants.SET_PAN_TILT + " error: " + rsp + "\r\n");
				}
		}



		private void RMCButton_Click(object sender, EventArgs e)

		{
			string rsp;

			rsp = SendToolCommand(UiConstants.RUN_MAP_CORRECT, 1000);
			if (rsp.StartsWith(UiConstants.OK))
				StatusTextBox.AppendText("Map correct successful.\r\n");
			else
				StatusTextBox.AppendText("Map correct failed: " + rsp);
		}



		private void StopButton_Click(object sender, EventArgs e)

		{
			sw.Stop();
			TimerTextBox.Text = sw.ElapsedMilliseconds.ToString() + " ms";
			sw.Reset();
			StopButton.Enabled = false;
			StopButton.BackColor = System.Windows.Forms.Control.DefaultBackColor;
		}



		private void OffButton_Click(object sender, EventArgs e)

		{
			string rsp;

			rsp = SendToolCommand(UiConstants.ARM_OFF, 1000);
			if (rsp.StartsWith(UiConstants.OK))
				StatusTextBox.AppendText("Arm was turned off.\r\n");
			else
				StatusTextBox.AppendText("Arm off failed: " + rsp);
		}



		private void KeepAliveTimer(Object source,System.Timers.ElapsedEventArgs e)

		{
			if (keep_alive_active)
				{
				kaconnect.Send(UiConstants.KEEP_ALIVE,karcvr,false);
				}
		}



		private void ArmManualModeForm_FormClosing(object sender, FormClosingEventArgs e)
			
		{
			if (katimer != null)
				{
				katimer.Enabled = false;
				katimer.Close();
				katimer = null;
				keep_alive_active = false;
				}
			if ((kaconnect!= null) && kaconnect.Connected())
				{
				kaconnect.Close();
				kaconnect = null;
				}
			if ((tconnect != null) && tconnect.Connected())
				{
				SendToolCommand(UiConstants.CLOSE,10);
				tconnect.Close();
				tconnect = null;
				}
		}

		}
	}
