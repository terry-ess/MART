using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;
using Coding4Fun.Kinect.WinForm;
using MathNet.Numerics.LinearAlgebra.Double;


namespace Manual_Control
	{
	public partial class ManualControlForm : Form
		{

		private const double MAX_DRIFT_ANGLE = 1.34;
		private static DenseMatrix mat90 = DenseMatrix.OfArray(new[,] { { 0.0, -1.0 }, { 1.0, 0.0 } });

		public struct FPoint
		{
			public float X,Y;

			public FPoint(float x,float y)
			{
			X = x;
			Y = y;
			}
		};


		private Connection tcntl = null;
		private string tool_name;

		private Connection tconnect = null;
		private SemaphoreSlim tool_access = new SemaphoreSlim(1);

		private Connection kaconnect = null;
		private System.Timers.Timer katimer = null;
		private bool keep_alive_active = false;
		private IPEndPoint karcvr;

		private Connection sensor_feed = null;
		private bool sf_run = false;
		private Thread sf_feed = null;
		private delegate void SensorStatusUpdate(string status);
		private SensorStatusUpdate ssupdate;
		private delegate void StatusTextUpdate(string update);
		private StatusTextUpdate stupdate;


		private double flidar_max_y;

		private MemoryStream lms = null;

		private delegate void VideoUpdate(int indx);
		private VideoUpdate vu;
		private delegate void VUFail(string err);
		private VUFail vuf;
		private MemoryStream[] ms = new MemoryStream[2];
		private int indx = 0;
		private Image vimg;
		private Bitmap blank;
		private bool running = false;
		private bool shoot = false;


		public ManualControlForm()

		{
			string rsp;
			bool started = false;
			string fname;
			DateTime fdt;
			int i,j;

			InitializeComponent();
			running = false;
			tool_name = new AssemblyName(Assembly.GetCallingAssembly().FullName).Name;
			fname = Application.StartupPath + Folders.TOOLS_SUB_DIR + tool_name + ".dll";
			fdt = File.GetLastWriteTime(fname);
			this.Text = this.Text + "  " + fdt.ToShortDateString() + "  " + fdt.ToShortTimeString();
			tcntl = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_CNTL_PORT_NO,true);
			if (tcntl.Connected())
				{
				StatusTextBox.AppendText("Connected with " + UiConstants.TOOL_CNTL_PORT_NO + "\r\n");
				rsp = tcntl.SendCommand(UiConstants.START + "," + tool_name,UiConstants.TOOL_START_WAIT_COUNT);
				if (rsp.StartsWith(UiConstants.OK))
					{
					StatusTextBox.AppendText("Tool connection opened.\r\n");
					tconnect = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_PORT_NO,true,1,true);
					kaconnect = new Connection(MainControl.robot_ip_address,UiConstants.TOOL_KEEP_ALIVE_PORT_NO,false);
					if (tconnect.Connected() && kaconnect.Connected())
						{
						StatusTextBox.AppendText("Tool connections opened.\r\n");
						keep_alive_active = true;
						karcvr = new IPEndPoint(IPAddress.Parse(MainControl.robot_ip_address), UiConstants.TOOL_KEEP_ALIVE_PORT_NO);
						katimer = new System.Timers.Timer(500);
						katimer.Elapsed += KeepAliveTimer;
						katimer.Enabled = true;
						started = true;
						PanTiltUpdate();
						KPGroupBox.Enabled = true;
						FLGroupBox.Enabled = true;
						blank = new Bitmap(VideoPictureBox.Width,VideoPictureBox.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
						for (i = 0; i < VideoPictureBox.Width; i++)
							for (j = 0; j < VideoPictureBox.Height; j++)
								blank.SetPixel(i, j, Color.White);
						VideoPictureBox.Image = blank;
						vu = VidUpdate;
						vuf = VidUpdateFail;
						sensor_feed = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_FEED_PORT_NO, false);
						if (sensor_feed.Connected())
							{
							ssupdate = SensorUpdate;
							stupdate = TextUpdate;
							StatusTextBox.AppendText("Status feed connection opened.\r\n");
							rsp = SendToolCommand(UiConstants.START, 100);
							if (rsp.StartsWith(UiConstants.OK))
								{
								sf_run = true;
								sf_feed = new Thread(SensorStatusReceive);
								sf_feed.Start();
								}
							else
								{
								sensor_feed.Close();
								sensor_feed = null;
								Log.LogEntry("Could not start status feed.");
								StatusTextBox.AppendText("Could not start status feed.\r\n");
								}
							}
						else
							{
							StatusTextBox.AppendText("Could not open sensor feed port.\r\n");
							Log.LogEntry("Could not open sensor feed port.");
							}
						}
					else
						{
						tcntl.Close();
						tcntl = null;
						if (tconnect.Connected())
							tconnect.Close();
						tconnect = null;
						kaconnect = null;
						Log.LogEntry("Could not open tool connections.");
						StatusTextBox.AppendText("Could not open tool connections.\r\n");
						}
					}
				else
					{
					tcntl.Close();
					tcntl = null;
					this.Enabled = false;
					Log.LogEntry("Could not start tool.");
					StatusTextBox.AppendText("Could not start tool.\r\n");
					}
				}
			if (!started)
				{
				this.Enabled = false;
				Log.LogEntry("Could not establish a connection with the tool.");
				StatusTextBox.AppendText("Could not establish a connection with the tool.\r\n");
				}
		}



		private string SendToolCommand(string msg,int timeout_count,bool log = true)


		{
			string rtn = "";

			tool_access.Wait();
			rtn = tconnect.SendCommand(msg,timeout_count,log);
			tool_access.Release();
			return(rtn);
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



		private void SensorUpdate(string msg)

		{
			string[] val;

			val = msg.Split(',');
			if (val.Length == 3)
				{
				MHTextBox.Text = val[0];
				FSTextBox.Text = val[1];
				RSTextBox.Text = val[2];
				}
			else
				{
				MHTextBox.Clear();
				FSTextBox.Clear();
				RSTextBox.Clear();
				Log.LogEntry("SensorUpdate, wrong parameter count");
				}
		}



		private void TextUpdate(string msg)

		{
			StatusTextBox.AppendText(msg + "\r\n");
		}



		private void SensorStatusReceive()

		{
			string msg;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string[] val;

			Log.LogEntry("Manual control sensor receiver started.");
			while (sf_run)	
				{

				try
				{
				msg = sensor_feed.ReceiveResponse(20,ref ep);
				if (msg.StartsWith(UiConstants.SENSOR_DATA) && sf_run)
					{
					val = msg.Split(',');
					if (val.Length > 1)
						{
						this.BeginInvoke(ssupdate,msg.Substring(val[0].Length + 1));
						}
					else
						Log.LogEntry("SensorStatusReceive, bad parameter format: " + msg);
					}
				else if (msg.StartsWith(UiConstants.STATUS))
					{
					val = msg.Split(',');
					if (val.Length > 1)
						{
						this.BeginInvoke(stupdate,msg.Substring(val[0].Length + 1));
						}
					else
						Log.LogEntry("SensorStatusReceive, bad parameter format: " + msg);
					}
				else if (!msg.StartsWith(UiConstants.FAIL))
					Log.LogEntry("SensorStatusReceive, unexpected message: " + msg);
				}

				catch(ThreadAbortException)
				{
				sf_run = false;
				}

				catch(Exception ex)
				{
				Log.LogEntry("SensorStatusReceive exception: " + ex.Message);
				}

				}
			sf_feed = null;
			Log.LogEntry("Manual control sensor receiver closed.");
		}



		private void KeepAliveTimer(Object source,System.Timers.ElapsedEventArgs e)

		{
			if (keep_alive_active)
				{
				kaconnect.Send(UiConstants.KEEP_ALIVE,karcvr,false);
				}
		}



		private void ForwardButton_Click(object sender, EventArgs e)

		{
			string rsp;

			if (!running)
				{
				rsp = SendToolCommand(UiConstants.FORWARD, 100);
				if (rsp.StartsWith(UiConstants.FAIL))
					{
					StatusTextBox.AppendText("Forward command failed.\r\n");
					Log.LogEntry("Forward command failed.");
					}
				else
					{
					MHTextBox.Clear();
					FSTextBox.Clear();
					RSTextBox.Clear();
					running = true;
					ForwardButton.Enabled = false;
					BackwardButton.Enabled = false;
					RightButton.Enabled = false;
					LeftButton.Enabled = false;
					}
				}
		}



		private void RightButton_Click(object sender, EventArgs e)

		{
			string rsp;

			if (!running)
				{
				rsp = SendToolCommand(UiConstants.RIGHT_TURN, 100);
				if (rsp.StartsWith(UiConstants.FAIL))
					{
					StatusTextBox.AppendText("Right turn command failed.\r\n");
					Log.LogEntry("Right turn command failed.");
					}
				else
					{
					running = true;
					ForwardButton.Enabled = false;
					BackwardButton.Enabled = false;
					RightButton.Enabled = false;
					LeftButton.Enabled = false;
					}
				}
		}



		private void BackwardButton_Click(object sender, EventArgs e)

		{
			string rsp;

			if (!running)
				{
				rsp = SendToolCommand(UiConstants.BACKWARD, 100);
				if (rsp.StartsWith(UiConstants.FAIL))
					{
					StatusTextBox.AppendText("Backward command failed.\r\n");
					Log.LogEntry("Backward command failed.");
					}
				else
					{
					MHTextBox.Clear();
					FSTextBox.Clear();
					RSTextBox.Clear();
					running = true;
					ForwardButton.Enabled = false;
					BackwardButton.Enabled = false;
					RightButton.Enabled = false;
					LeftButton.Enabled = false;
					}
				}
		}



		private void LeftButton_Click(object sender, EventArgs e)

		{
			string rsp;

			if (!running)
				{
				rsp = SendToolCommand(UiConstants.LEFT_TURN, 100);
				if (rsp.StartsWith(UiConstants.FAIL))
					{
					StatusTextBox.AppendText("Left turn command failed.\r\n");
					Log.LogEntry("Left turn command failed.");
					}
				else
					{ 
					running = true;
					ForwardButton.Enabled = false;
					BackwardButton.Enabled = false;
					RightButton.Enabled = false;
					LeftButton.Enabled = false;
					}
				}
		}



		private void StopButton_Click(object sender, EventArgs e)

		{
			string rsp;

			if (running)
				{
				rsp = SendToolCommand(UiConstants.STOP_MOTION, 200);
				if (rsp.StartsWith(UiConstants.FAIL))
					{
					StatusTextBox.AppendText("Stop motion command failed.\r\n");
					Log.LogEntry("Stop motion command failed.");
					running = false;
					this.Refresh();
					}
				else
					{
					running = false;
					ForwardButton.Enabled = true;
					BackwardButton.Enabled = true;
					RightButton.Enabled = true;
					LeftButton.Enabled = true;
					}
				}
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


		
		private bool LlidarReceive(ref MemoryStream ms,string frame_title)

		{
			string msg;
			string[] val;
			int vlen, rvlen;
			bool rtn = false;

			msg = tconnect.ReceiveResponse(100, true);
			if (msg.StartsWith(frame_title))
				{
				val = msg.Split(',');
				if (val.Length == 2)
					{
					tconnect.Send(UiConstants.OK);
					vlen = int.Parse(val[1]);
					rvlen = tconnect.ReceiveStream(ref ms, vlen);
					if (vlen == rvlen)
						rtn = true;
					else
						{
						StatusTextBox.AppendText("LidarReceive incorrect length\r\n");
						Log.LogEntry("LidarReceive incorrect length");
						}
					}
				else
					{
					StatusTextBox.AppendText("LidarRecieve bad response format\r\n");
					Log.LogEntry("LidarRecieve bad response format");
					}
				}
			else
				{
				StatusTextBox.AppendText("LidarRecieve no depth stream to receive\r\n");
				Log.LogEntry("LidarRecieve no depth stream to receive");
				}
			return(rtn);
		}



		private int AngularDistance(int a1,int a2)

		{
			int ad;

			ad = Math.Abs(a1 - a2);
			if (ad > 180)
				ad = 360 - ad;
			return(ad);
		}



		private FPoint RotatePoint(FPoint start,double offset,int shift_angle)

		{
			FPoint end;
			DenseMatrix mat;
			DenseVector vec;
			DenseVector result;
			int rangle;

			if (shift_angle < 0)
				shift_angle += 360;
			rangle = shift_angle % 90;
			mat = DenseMatrix.OfArray(new[,] { { Math.Cos(rangle * MathConvert.DEG_TO_RAD), -Math.Sin(rangle * MathConvert.DEG_TO_RAD) }, { Math.Sin(rangle * MathConvert.DEG_TO_RAD), Math.Cos(rangle * MathConvert.DEG_TO_RAD) } });
			vec = new DenseVector(new[] { start.X,(start.Y + offset) });
			if (shift_angle >= 90)
				vec = vec * mat90;
			if (shift_angle >= 180)
				vec = vec * mat90;
			if (shift_angle >= 270)
				vec = vec * mat90;
			result = vec * mat;
			end = new FPoint((float) result.Values[0],(float) (result.Values[1] - offset));
			return (end);
		}



		private void DisplayLidar(int shift_angle = 0)

		{
			BinaryReader br;
			int i, sl;
			double dist0 = -1,dist180 = -1;
			float x,y, min_ax,min_ax2;
			FPoint sapt;

			if (lms != null)
				{
				br = new BinaryReader(lms);
				br.BaseStream.Seek(0, SeekOrigin.Begin);
				ScanChart.Series[2].Points.Clear();
				min_ax2 = min_ax = sl = (int)SLNumericUpDown.Value;
				ScanChart.ChartAreas[0].AxisX.Maximum = sl;
				ScanChart.ChartAreas[0].AxisX.Minimum = -sl;
				ScanChart.ChartAreas[0].AxisY.Maximum = sl;
				ScanChart.ChartAreas[0].AxisY.Minimum = -sl;
				flidar_max_y = -sl;
				for (i = 0; i < lms.Length / 8; i++)
					{
					x = br.ReadSingle();
					y = br.ReadSingle();
					if (shift_angle != 0)
						{
						sapt = RotatePoint(new FPoint(x,y),RobotMeasurements.FRONT_PIVOT_PT_OFFSET,-shift_angle);
						x = sapt.X;
						y = sapt.Y;
						}
					if ((x == 0) && (y > 0))
						{
						min_ax = 0;
						dist0 = y;
						}
					else if ((y > 0) && (min_ax > 0) && (Math.Abs(x) < min_ax) && (Math.Abs(x) < 5))
						{
						min_ax = Math.Abs(x);
						dist0 = Math.Sqrt((x * x) + (y * y));
						}
					if ((x == 0) && (y < 0))
						{
						min_ax2 = 0;
						dist180 = -y;
						}
					else if ((y < 0) && (min_ax2 > 0) && (Math.Abs(x) < min_ax2) && (Math.Abs(x) < 5))
						{
						min_ax2 = Math.Abs(x);
						dist180 = Math.Sqrt((x * x) + (y * y));
						}

					if ((Math.Abs(x) < sl) && (Math.Abs(y) < sl))
						{
						ScanChart.Series[2].Points.AddXY(x, y);
						if (y > flidar_max_y)
							flidar_max_y = y;
						}
					}
				if (dist0 > 0)
					Dist0TextBox.Text = dist0.ToString("F1");
				else
					Dist0TextBox.Text = "";
				if (dist180 > 0)
					Dist180TextBox.Text = dist180.ToString("F1");
				else
					Dist180TextBox.Text = "";
				}
		}



		private void DisplayRobot()

		{
			double x,y,offset;

			if (lms != null)
				{
				ScanChart.Series[1].Points.Clear();
				offset = (double) RobotMeasurements.ROBOT_WIDTH / 2;
				for (y = 0;y > - RobotMeasurements.ROBOT_LENGTH;y--)
					{
					x = offset;
					ScanChart.Series[1].Points.AddXY(x, y);
					x = -offset;
					ScanChart.Series[1].Points.AddXY(x, y);
					}
				for (x = -offset;x <= offset;x++)
					{
					y = 0;
					ScanChart.Series[1].Points.AddXY(x, y);
					y = -RobotMeasurements.ROBOT_LENGTH;
					ScanChart.Series[1].Points.AddXY(x, y);
					}
				}
		}



		private void DisplayPath()

		{
			int y;
			double x,offset,drift;

			offset = (double) RobotMeasurements.ROBOT_WIDTH/2;
			ScanChart.Series[0].Points.Clear();
			for (y = 0;y < flidar_max_y;y++)
				{
				drift = y * Math.Tan(MAX_DRIFT_ANGLE * MathConvert.DEG_TO_RAD);
				x = (offset + drift);
				ScanChart.Series[0].Points.AddXY(x, y);
				x = -(offset + drift);
				ScanChart.Series[0].Points.AddXY(x, y);
				}
		}



		private void Shoot()

		{
			string rsp;
			int sl;

			try
			{
			tool_access.Wait();
			ScanChart.Series[0].Points.Clear();
			ScanChart.Series[1].Points.Clear();
			ScanChart.Series[2].Points.Clear();
			rsp = tconnect.SendCommand(UiConstants.SEND_LIDAR, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				lms = new MemoryStream();
				if (LlidarReceive(ref lms,UiConstants.LIDAR_FRAME))
					{
					tool_access.Release();
					DisplayLidar();
					DisplayRobot();
					YNumericUpDown.Value = 0;
					XNumericUpDown.Value = 0;
					sl = (int)SLNumericUpDown.Value;
					YNumericUpDown.Maximum = sl;
					YNumericUpDown.Minimum = -sl;
					XNumericUpDown.Maximum = sl;
					XNumericUpDown.Minimum = -sl;
					MarkerValueChanged(null,null);
					ShiftBbutton.Enabled = true;
					PathButton.Enabled = true;
					SaveButton.Enabled = true;
					SANumericUpDown.Value = 0;
					}
				else
					{
					tool_access.Release();
					StatusTextBox.AppendText("LIDAR data transfer failed.\r\n");
					ShiftBbutton.Enabled = false;
					PathButton.Enabled = false;
					SaveButton.Enabled = false;
					}
				}
			else
				{
				tool_access.Release();
				ShiftBbutton.Enabled = false;
				PathButton.Enabled = false;
				SaveButton.Enabled = false;
				StatusTextBox.AppendText("LIDAR request failed.\r\n");
				}
			}

			catch(Exception ex)
			{
			if (tool_access.CurrentCount == 0)
				tool_access.Release();
			ScanChart.Series[0].Points.Clear();
			ScanChart.Series[1].Points.Clear();
			ScanChart.Series[2].Points.Clear();
			Log.LogEntry("LIDAR shoot exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}
		}


		private void ShootButton_Click(object sender, EventArgs e)

		{
			Shoot();
		}



		private void AutoButton_Click(object sender, EventArgs e)

		{
			if (AutoTimer.Enabled)
				{
				shoot = false;
				AutoTimer.Enabled = false;
				AutoButton.BackColor = Color.LightGreen;
				}
			else
				{
				shoot = true;
				AutoTimer.Enabled = true;
				AutoButton.BackColor = Color.OrangeRed;
				}
		}



		private void AutoTimer_Tick(object sender, EventArgs e)

		{
			if (shoot)
				Shoot();
		}




		private void SLNumericUpDown_ValueChanged(object sender, EventArgs e)

		{
			DisplayLidar();
		}



		private void ShiftBbutton_Click(object sender, EventArgs e)

		{
			DisplayLidar((int) SANumericUpDown.Value);
			if (ScanChart.Series[1].Points.Count > 0)
				DisplayRobot();
			if (ScanChart.Series[0].Points.Count > 0)
				DisplayPath();
		}



		private void PathButton_Click(object sender, EventArgs e)

		{
			DisplayPath();
		}



		private void ManualControlForm_FormClosing(object sender, FormClosingEventArgs e)

		{
			SendToolCommand(UiConstants.SET_PAN_TILT + ",0,0", 1000);
			if (katimer != null)
				{
				katimer.Enabled = false;
				katimer.Close();
				katimer = null;
				keep_alive_active = false;
				}
			if ((sensor_feed != null) && sensor_feed.Connected())
				{
				SendToolCommand(UiConstants.STOP, 10);
				sf_run = false;
				if ((sf_feed != null) && (sf_feed.IsAlive))
					sf_feed.Join();
				sensor_feed.Close();
				sensor_feed = null;
				}
			if ((tcntl != null) && tcntl.Connected())
				{
				tcntl.SendCommand(UiConstants.STOP + "," + tool_name, 20);
				tcntl.Close();
				tcntl = null;
				}
			if ((tconnect != null) && tconnect.Connected())
				{
				tconnect.Close();
				tconnect = null;
				}
			if ((kaconnect != null) && kaconnect.Connected())
				{
				kaconnect.Close();
				kaconnect = null;
				}
		}



		private void VidUpdate(int indx)

		{
			Image img;

			img = Image.FromStream(ms[indx]);
			img.RotateFlip(RotateFlipType.Rotate180FlipY);
			vimg = (Image) img.Clone();
			VideoPictureBox.Image = img;
			VSButton.Enabled = true;
			VSaveButton.Enabled = true;
		}



		private void VidUpdateFail(string err)

		{
			VideoPictureBox.Image = blank;
			StatusTextBox.AppendText(err + "\r\n");
			VSButton.Enabled = true;
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



		private void ReciveVDM()

		{
			if (VideoReceive())
				{
				this.BeginInvoke(vu, indx);
				indx = (indx + 1) % 2;
				tool_access.Release();
				}
			else
				{
				this.BeginInvoke(vuf);
				tool_access.Release();
				}
		}




		private void VSButton_Click(object sender, EventArgs e)

		{
			string rsp;
			Thread rcv;

			tool_access.Wait();
			rsp = tconnect.SendCommand(UiConstants.SEND_VIDEO,100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				rcv = new Thread(ReciveVDM);
				rcv.Start();
				VSButton.Enabled = false;
				}
			else
				{
				tool_access.Release();
				VideoPictureBox.Image = blank;
				StatusTextBox.AppendText(rsp + "\r\n");
				}

		}



		private string SaveDataFile()

		{
			string fname;
			DateTime now = DateTime.Now;
			TextWriter tw;
			int i;
			double x,y;

			fname = Log.LogDir() + "LIDAR scan " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + ".csv";
			tw = File.CreateText(fname);
			if (tw != null)
				{
				tw.WriteLine(now.ToShortDateString() + "  " + now.ToShortTimeString() + "\r\n");
				tw.WriteLine("X,Y");
				for (i = 0; i < ScanChart.Series[2].Points.Count;i++)
					{
					x = ScanChart.Series[2].Points[i].XValue;
					y = ScanChart.Series[2].Points[i].YValues[0];
					tw.WriteLine(x.ToString("F4") + "," + y.ToString("F4"));
					}
				tw.Close();
				}
			else
				fname = "";
			return(fname);
		}



		private void SaveButton_Click(object sender, EventArgs e)

		{
			string fname,tfname;
			DateTime now = DateTime.Now;
			TextWriter tw;

			fname = Log.LogDir() + "LIDAR scan " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + ".bmp";
			ScanChart.SaveImage(fname, System.Windows.Forms.DataVisualization.Charting.ChartImageFormat.Bmp);
			tfname = fname.Replace(".bmp",".txt");
			tw = File.CreateText(tfname);
			if (tw != null)
				{
				tw.WriteLine(now.ToShortDateString() + "  " + now.ToShortTimeString() + "\r\n");
				tw.WriteLine("Scan limit (in): " + SLNumericUpDown.Value);
				tw.WriteLine("Shift angle (°): " + SANumericUpDown.Value);
				tw.WriteLine("Distance at 0° (in): " + Dist0TextBox.Text);
				tw.WriteLine("Distance at 180° (in): " + Dist180TextBox.Text);
				tw.WriteLine("Marker at: " + XNumericUpDown.Value + "," + YNumericUpDown.Value);
				tw.WriteLine("Front SONAR (in): " + FSTextBox.Text);
				tw.WriteLine("Rear SONAR (in): " + RSTextBox.Text);
				tw.WriteLine("Chart file: " + fname);
				fname = SaveDataFile();
				if (fname.Length > 0)
					tw.WriteLine("Data file: " + fname);
				tw.Close();
				}
		}


		private void MarkerValueChanged(object sender, EventArgs e)

		{
			ScanChart.Series[3].Points.Clear();
			ScanChart.Series[3].Points.AddXY(XNumericUpDown.Value,YNumericUpDown.Value);
			MDistTextBox.Text = Math.Sqrt(Math.Pow((double) XNumericUpDown.Value,2) + Math.Pow((double) YNumericUpDown.Value,2)).ToString("F1");
		}



		private void VSaveButton_Click(object sender, EventArgs e)

		{
			Bitmap bm;
			DateTime now = DateTime.Now;
			string fname;

			bm = (Bitmap) VideoPictureBox.Image;
			fname = Log.LogDir() + "LIDAR scan " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + ".jpg";
			bm.Save(fname,ImageFormat.Jpeg);
		}



		private void CTButton_Click(object sender, EventArgs e)
			
		{
			string msg, rsp;

			if (TANumericUpDown.Value > 0)
				{
				msg = UiConstants.CHECK_TURN;
				if (RTRadioButton.Checked)
					msg += "," + UiConstants.RIGHT_TURN;
				else
					msg += "," + UiConstants.LEFT_TURN;
				msg += "," + TANumericUpDown.Value;
				rsp = tconnect.SendCommand(msg, 100);
				StatusTextBox.AppendText(rsp + "\r\n");
				}
			else
				StatusTextBox.AppendText("No turn value.\r\n");
		}

		}
	}
