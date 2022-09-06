using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using RobotConnection;
using Constants;
using DBMap;
using BuildingDataBase;
using Renci.SshNet;


namespace RemoteUserInterface
	{
	public partial class MainControl : UserControl
		{

		public enum RobotStatus { NONE, STARTUP, LIMITED_RUNNING, NORMAL_RUNNING, SKILL_RUNNING, SHUTTING_DOWN };
		private enum LocationStatus { UNKNOWN, DR, VERIFIED, USR };

		private Recorder record = new Recorder();

		Connection status_cntl = null;
		Connection status_feed = null;
		private Thread srcv = null;
		private bool st_run = false;
		private delegate void StatusUpdate(string status);
		private StatusUpdate su;
		private string last_stat = "";
		private MainForm.ToolsEnableUpdate tool_update = null;
		private MainForm.RemoteIntfInvoke ri_invoke = null;

		Connection video_cntl = null;
		Connection video_feed = null;
		private Thread vrcv = null;
		private bool vf_run = false;
		private MemoryStream[] ms = new MemoryStream[2];
		private delegate void VideoUpdate(int indx);
		private VideoUpdate vu;
		private delegate void VUFail();
		private VUFail vuf;
		private Bitmap blank;

		Connection act_cntl = null;
		Connection act_feed = null;
		private Thread arcv = null;
		private bool af_run = false;
		private delegate void ActUpdate(string status);
		private ActUpdate au;

		Connection loc_cntl = null;
		Connection loc_feed = null;
		private Thread lrcv = null;
		private bool lf_run = false;
		private delegate void LocUpdate(string status);
		private LocUpdate lu;
		RoomData rmdata;
		private byte[,] detail_map;
		private Bitmap brbm = null;
		private string rm_name = "";

		private SshClient client;
		private Thread robot;
		private Thread ovs;

		static public string run_ab_cmd = "";
		static public string run_ovs_cmd = "";
		static public string robot_ip_address = "";


		public MainControl()

		{
			int i,j;
			string fname;
			DateTime fdt;

			InitializeComponent();
			fname = Application.StartupPath + "\\" + Application.ProductName + ".exe";
			fdt = File.GetLastWriteTime(fname);
			this.Text += " " + fdt.ToShortDateString() + " " + fdt.ToShortTimeString();
			LocTextBox.Lines = new string[] {""};
			su = StatUpdate;
			vu = VidUpdate;
			vuf = VidUpdateFailed;
			au = ActivityUpdate;
			lu = LocationUpdate;
			blank = new Bitmap(640, 480, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			for (i = 0; i < 640; i++)
				for (j = 0; j < 480; j++)
					blank.SetPixel(i, j, Color.White);
			VideoPictureBox.Image = blank;
		}



		public bool Open(MainForm mnf)

		{
			bool rtn = true;

			tool_update = mnf.SetToolsEnable;
			ri_invoke = mnf.InvokeRemoteInterface;
			SUButton.Enabled = true;
			return (rtn);
		}


		
		public bool Close(bool app_close)

		{
			bool rtn = true;

			if (app_close)
				{
				st_run = false;
				vf_run = false;
				af_run = false;
				lf_run = false;
				if ((vrcv != null) && (vrcv.IsAlive))
					vrcv.Abort();
				if ((srcv != null) && srcv.IsAlive)
					srcv.Abort();
				if ((arcv != null) && srcv.IsAlive)
					arcv.Abort();
				if ((lrcv != null) && lrcv.IsAlive)
					lrcv.Abort();
				if ((robot != null) && robot.IsAlive)
					robot.Abort();
				}
			return(rtn);
		}



		public string RobotOpStatus()

		{
			return(StatusTextBox.Text);
		}




		private bool Open(bool status_opened = false)

		{
			bool rtn = false;

			if (status_opened || StatusOpen())
				{
				rtn = true;
				StatGroupBox.Enabled = true;
				if (SSSButton.Text.StartsWith("Start"))
					SSSButton_Click(null, null);
				if (ActivityOpen())
					{
					ActGroupBox.Enabled = true;
					if (ActSSButton.Text.StartsWith("Start"))
						ActSSButton_Click(null, null);
					}
				else
					ActGroupBox.Enabled = false;
				if (LocOpen())
					{
					LocGroupBox.Enabled = true;
					if (LocSSButton.Text.StartsWith("Start"))
						LocSSButton_Click(null, null);
					}
				else
					LocGroupBox.Enabled = false;
				}
			return (rtn);
		}


		private bool VideoOpen()

		{
			bool rtn = false;

			if (video_cntl == null)
				{
				video_cntl = new Connection(robot_ip_address,UiConstants.VIDEO_CNTL_PORT_NO,true);
				if (video_cntl.Connected())
					rtn = true;
				else
					video_cntl = null;
				}
			else
				rtn = true;
			return(rtn);
		}



		private void VidUpdate(int indx)

		{
			Image img;

			try
			{
			img = Image.FromStream(ms[indx]);
			img.RotateFlip(RotateFlipType.Rotate180FlipY);
			VideoPictureBox.Image = img;
			}

			catch(Exception ex)
			{
			Log.LogEntry("VidUpdate exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			Log.LogEntry("Index: " + indx);
			}
		}



		private void VidUpdateFailed()

		{
			VideoPictureBox.Image = blank;
		}



		private void ReSynch()

		{
			string rsp;

			Log.LogEntry("VideoReceive: attempting re-synch");
			rsp = video_cntl.SendCommand(UiConstants.SUSPEND + "," + true.ToString(), 20);
			if (rsp.StartsWith(UiConstants.OK))
				{
				Thread.Sleep(1000);
				video_feed.ClearReceive();
				rsp = video_cntl.SendCommand(UiConstants.SUSPEND + "," + false.ToString(), 20);
				}
		}



		private void VideoReceive()

		{
			string msg;
			string[] val;
			int vlen,rvlen,indx = 0,videos = 0;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

			while (vf_run)	
				{

				try
				{
				msg = video_feed.ReceiveResponse(100,ref ep);
				if (msg.StartsWith(UiConstants.VIDEO_FRAME) && vf_run)
					{
					val = msg.Split(',');
					if (val.Length == 2)
						{
						video_feed.Send(UiConstants.OK,ep);
						vlen = int.Parse(val[1]);
						ms[indx] = new MemoryStream();
						rvlen = video_feed.ReceiveStream(ref ms[indx],vlen);
						if ((vlen == rvlen) && vf_run)
							{
							this.Invoke(vu,indx);
							indx = (indx + 1) % 2;
							videos += 1;
							}
						else
							{
							this.BeginInvoke(vuf);
							Log.LogEntry("VideoReceive: incorrect video stream length (expected: " + vlen + "    received: " + rvlen + ")");
							ReSynch();
							}
						}
					else
						{
						this.BeginInvoke(vuf);
						video_feed.Send(UiConstants.FAIL,ep);
						Log.LogEntry("VideoReceive: bad response format");
						}
					}
				else if (!msg.StartsWith(UiConstants.FAIL) && vf_run)
					{
					this.BeginInvoke(vuf);
					Log.LogEntry("VideoReceive: incorrect message");
					ReSynch();
					}
				}

				catch(ThreadAbortException)
				{
				vf_run = false;
				}

				catch(Exception ex)
				{
				Log.LogEntry("VideoReceive exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				vf_run = false;
				}

				}
		}



		private void VidSSButton_Click(object sender, EventArgs e)

		{
			string rsp;

			if (VidSSButton.Text.StartsWith("Start"))
				{
				if (video_feed == null)
					{
					video_feed = new Connection(robot_ip_address, UiConstants.VIDEO_FEED_PORT_NO,false);
					if (video_feed.Connected())
						{
						rsp = video_cntl.SendCommand(UiConstants.START,20);
						if (rsp.StartsWith(UiConstants.OK))
							{
							vf_run = true;
							vrcv = new Thread(VideoReceive);
							vrcv.Start();
							VidSSButton.Text = "Stop";
							VidSSButton.BackColor = Color.OrangeRed;
							}
						else
							{
							video_feed.Close();
							video_feed = null;
							}
						}
					else
						video_feed = null;
					}
				else
					{
					video_feed.ClearReceive();
					rsp = video_cntl.SendCommand(UiConstants.START, 20);
					if (rsp.StartsWith(UiConstants.OK))
						{
						vf_run = true;
						vrcv = new Thread(VideoReceive);
						vrcv.Start();
						VidSSButton.Text = "Stop";
						VidSSButton.BackColor = Color.OrangeRed;
						}
					}
				}
			else
				{
				if (video_feed != null)
					{
					video_cntl.SendCommand(UiConstants.STOP, 100);
					}
				vf_run = false;
				if ((vrcv != null) && vrcv.IsAlive)
					{
					if (!vrcv.Join(2000))
						vrcv.Abort();
					}
				if (video_feed != null)
					{
					video_feed.Close();
					video_feed = null;
					}
				VidSSButton.Text = "Start";
				VidSSButton.BackColor = Color.LightGreen;
				VideoPictureBox.Image = blank;
				}
		}



		private bool StatusOpen(bool initial = false)

		{
			bool rtn = false;

			if (status_cntl == null)
				{
				if (initial)
					status_cntl = new RobotConnection.Connection(robot_ip_address, UiConstants.STATUS_CNTL_PORT_NO, true,1,false);
				else
					status_cntl = new RobotConnection.Connection(robot_ip_address, UiConstants.STATUS_CNTL_PORT_NO,true,10);
				if (status_cntl.Connected())
					rtn = true;
				else
					status_cntl = null;
				}
			else
				rtn = true;
			return(rtn);
		}



		private void StatUpdate(string status)

		{
			string[] val;
			bool stat_change = false;

			val = status.Split(',');
			if (val.Length == 13)
				{
				stat_change = StatusTextBox.Text.Equals(val[1]);
				StatusTextBox.Text = val[1];
				if ( stat_change && (tool_update != null))
					{
					if ((val[1] == RobotStatus.LIMITED_RUNNING.ToString()) || (val[1] == RobotStatus.NORMAL_RUNNING.ToString()))
						{
						RSButton.Enabled = true;
						SDButton.Enabled = true;
						EXButton.Enabled = true;
						HDButton.Enabled = true;
						tool_update(true);
						}
					else if (val[1] == RobotStatus.SKILL_RUNNING.ToString())
						{
						tool_update(false);
						RSButton.Enabled = false;
						SDButton.Enabled = false;
						EXButton.Enabled = false;
						HDButton.Enabled = false;
						}
					else if (val[1] == RobotStatus.SHUTTING_DOWN.ToString())
						{
						RSButton.Enabled = false;
						SDButton.Enabled = false;
						EXButton.Enabled = false;
						HDButton.Enabled = false;
						if (SSSButton.Text.StartsWith("Stop"))
							SSSButton_Click(null, null);
						if (ActSSButton.Text.StartsWith("Stop"))
							ActSSButton_Click(null, null);
						if (LocSSButton.Text.StartsWith("Stop"))
							LocSSButton_Click(null, null);
						if (VidSSButton.Text.StartsWith("Stop"))
							VidSSButton_Click(null, null);
						tool_update(false);
						}
					else
						{
						RSButton.Enabled = false;
						tool_update(false);
						}
					}
				KCheckBox.Checked = Convert.ToBoolean(int.Parse(val[2]));
				if (KCheckBox.Checked && !VideoGroupBox.Enabled)
					{
					if (VideoOpen())
						VideoGroupBox.Enabled = true;
					}
				HACheckBox.Checked = Convert.ToBoolean(int.Parse(val[3]));
				CTCCheckBox.Checked = Convert.ToBoolean(int.Parse(val[4]));
				FLDCheckBox.Checked = Convert.ToBoolean(int.Parse(val[5]));
				RLDCheckBox.Checked = Convert.ToBoolean(int.Parse(val[6]));
				ACheckBox.Checked = Convert.ToBoolean(int.Parse(val[7]));
				NDBCheckBox.Checked = Convert.ToBoolean(int.Parse(val[8]));
				SRECheckBox.Checked = Convert.ToBoolean(int.Parse(val[9]));
				VODCheckBox.Checked = Convert.ToBoolean(int.Parse(val[10]));
				SDOCheckBox.Checked = Convert.ToBoolean(int.Parse(val[11]));
				if (val[12] == "-1")
					VoltTextBox.Text = "na";
				else
					VoltTextBox.Text = val[12];
				}
			else
				{
				StatusTextBox.Text = "unk";
				KCheckBox.Checked = false;
				HACheckBox.Checked = false;
				CTCCheckBox.Checked = false;
				FLDCheckBox.Checked = false;
				NDBCheckBox.Checked = false;
				SRECheckBox.Checked = false;
				VODCheckBox.Checked = false;
				VoltTextBox.Text = "unk";
				}
		}



		private void StatusReceive()

		{
			string msg;

			while (st_run)
				{

				try
				{
				msg = status_feed.ReceiveResponse(100);
				if (msg.StartsWith(UiConstants.STATUS))
					{
					if (!last_stat.Equals(msg))
						{
						last_stat = msg;
						record.Entry(msg);
						}
					if (this.IsHandleCreated)
						this.BeginInvoke(su,msg);
					}
				}

				catch(ThreadAbortException)
				{
				st_run = false;
				}

				catch(Exception ex)
				{
				Log.LogEntry("StatusReceive exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}
				}
		}



		private bool StartStatusFeed()

		{
			bool rtn = false;
			string rsp;

			rsp = status_cntl.SendCommand(UiConstants.START, 20);
			if (rsp.StartsWith(UiConstants.OK))
				{
				st_run = true;
				srcv = new Thread(StatusReceive);
				srcv.Start();
				rtn = true;
				}
			return (rtn);
		}



		private void SSSButton_Click(object sender, EventArgs e)

		{
			if (SSSButton.Text.StartsWith("Start"))
				{
				if (status_feed == null)
					{
					status_feed = new Connection(robot_ip_address, UiConstants.STATUS_FEED_PORT_NO,false);
					if (status_feed.Connected())
						{
						if (StartStatusFeed())
							{
							SSSButton.Text = "Stop";
							SSSButton.BackColor = Color.OrangeRed;
							}
						}
					else
						status_feed = null;
					}
				else
					{
					if (StartStatusFeed())
						{
						SSSButton.Text = "Stop";
						SSSButton.BackColor = Color.OrangeRed;
						}
					}
				}
			else
				{
				if (status_feed != null)
					{
					status_cntl.SendCommand(UiConstants.STOP, 20);
					st_run = false;
					}
				SSSButton.Text = "Start";
				SSSButton.BackColor = Color.LightGreen;
				}
		}



		private void ActivityUpdate(string status)

		{
			string[] val;

			val = status.Split(',');
			if (val.Length > 1)
				{
				if (val[0] == UiConstants.VOICE_INPUT)
					ActTextBox.AppendText("\r\n[hear] " + status.Substring(val[0].Length + 1) + "\r\n");
				else if (val[0] == UiConstants.SPEECH_OUTPUT)
					ActTextBox.AppendText("\t [speak] " + status.Substring(val[0].Length + 1) + "\r\n");
				else if ((val[0] == UiConstants.COMMAND_INPUT) && (val[1] == UiConstants.ARM_MANUAL_MODE))
					{
					ri_invoke(UiConstants.ARM_MANUAL_MODE);
					}
				}
		}



		private void ActReceive()

		{
			string msg;

			while (af_run)
				{

				try
				{
				msg = act_feed.ReceiveResponse(100);
				if (msg.StartsWith(UiConstants.VOICE_INPUT) || msg.StartsWith(UiConstants.SPEECH_OUTPUT) || msg.StartsWith(UiConstants.COMMAND_INPUT))
					{
					record.Entry(msg);
					if (this.IsHandleCreated)
						this.BeginInvoke(au, msg);
					}
				else if (!msg.StartsWith(UiConstants.FAIL))
					Log.LogEntry("ActReceive bad format: " + msg);
				}

				catch(ThreadAbortException)
				{
				af_run = false;
				}

				catch(Exception ex)
				{
				Log.LogEntry("ActReceive exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				af_run = false;
				}

				}
		}



		private bool StartActFeed()

		{
			bool rtn = false;
			string rsp;

			rsp = act_cntl.SendCommand(UiConstants.START, 20);
			if (rsp.StartsWith(UiConstants.OK))
				{
				af_run = true;
				arcv = new Thread(ActReceive);
				arcv.Start();
				rtn = true;
				}
			return (rtn);
		}



		private bool ActivityOpen()

		{
			bool rtn = false;

			if (act_cntl == null)
				{
				act_cntl = new RobotConnection.Connection(robot_ip_address, UiConstants.ACTIVITY_CNTL_PORT_NO,true);
				if (act_cntl.Connected())
					rtn = true;
				else
					act_cntl = null;
				}
			else
				rtn = true;
			return(rtn);
		}



		private void ActSSButton_Click(object sender, EventArgs e)

		{
			if (ActSSButton.Text.StartsWith("Start"))
				{
				if (act_feed == null)
					{
					act_feed = new Connection(robot_ip_address, UiConstants.ACTIVITY_FEED_PORT_NO,false);
					if (act_feed.Connected())
						{
						if (StartActFeed())
							{
							ActSSButton.Text = "Stop";
							ActSSButton.BackColor = Color.OrangeRed;
							}
						}
					else
						act_feed = null;
					}
				else
					{
					if (StartActFeed())
						{
						ActSSButton.Text = "Stop";
						ActSSButton.BackColor = Color.OrangeRed;
						}
					}
				}
			else
				{
				if (act_feed != null)
					{
					act_cntl.SendCommand(UiConstants.STOP, 20);
					af_run = false;
					}
				ActSSButton.Text = "Start";
				ActSSButton.BackColor = Color.LightGreen;
				}
		}



		private void DisplayRobotLoc(Point loc, Brush br,int direction,Graphics g)

		{
			Point rloc = new Point();

			if ((direction >= 45) && (direction < 135))
				{
				rloc.X = loc.X - 14;
				rloc.Y = loc.Y - 7;
				}
			else if ((direction >= 35) && (direction < 225))
				{
				rloc.X = loc.X - 7;
				rloc.Y = loc.Y - 14;
				}
			else if ((direction >= 225) && (direction < 315))
				{
				rloc.X = loc.X;
				rloc.Y = loc.Y - 7;
				}
			else
				{
				rloc.X = loc.X - 7;
				rloc.Y = loc.Y;
				}
			g.FillRectangle(br, rloc.X, rloc.Y, 14, 14);
		}



		private void LocationUpdate(string ldata)

		{
			string[] val;
			Graphics g;
			Bitmap bm;
			int x,y;
			string loc,rdb;

			val = ldata.Split(',');
			if (val.Length == 11)
				{

				try 
				{
				if (val[6] == LocationStatus.UNKNOWN.ToString())
					{
					PoseTextBox.Text = "Unknown";
					if (brbm != null)
						MapPictureBox.Image = (Bitmap) brbm.Clone();
					}
				else
					{
					loc = "Robot pose (" + val[2] + "," + val[3] + ")  " + val[4] + "°";
					if (val[5].Length > 0)
						loc += "  [" + val[5] + "]";
					if (rm_name != val[1])
						{
						LocTextBox.Clear();
						rdb = Application.StartupPath + DataBase.DATA_BASE_DIR + DataBase.ROOM_DBS + val[1] + "." + DataBase.DB_FILE_EXT;
						rmdata = new RoomData();
						if (rmdata.LoadRoomData(rdb, LocTextBox))
							{
							detail_map = new byte[rmdata.rd.rect.Width, rmdata.rd.rect.Height];
							rmdata.CreateRoomMap(rmdata.rd, ref detail_map, ref brbm);
							rm_name = val[1];
							}
						}
					PoseTextBox.Text = loc;
					bm = (Bitmap) brbm.Clone();
					g = Graphics.FromImage(bm);

					try
					{
					x = int.Parse(val[2]);
					y = int.Parse(val[3]);
					DisplayRobotLoc(new Point(x,y),Brushes.Blue,int.Parse(val[4]),g);
					g.DrawEllipse(Pens.Red,int.Parse(val[9]),int.Parse(val[10]),int.Parse(val[8]),int.Parse(val[7]));
					}

					catch (Exception ex)
					{
					LocTextBox.AppendText(ex.Message + "\r\n");
					}

					MapPictureBox.Image = bm;
					}
				}

				catch(ThreadAbortException)
				{

				}

				}
			else
				Log.LogEntry("LocationUpdate bad format message: " + ldata);
			}



		private void LocReceive()

		{
			string msg;

			while (lf_run)
				{
				try
				{
				msg = loc_feed.ReceiveResponse(100);
				if (msg.StartsWith(UiConstants.LOCATION))
					{
					record.Entry(msg);
					if (this.IsHandleCreated)
						this.BeginInvoke(lu, msg);
					}
				}

				catch(ThreadAbortException)
				{
				lf_run = false;
				}

				catch(Exception ex)
				{
				Log.LogEntry("LocReceive exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				lf_run = false;
				}
				}
		}



		private bool StartLocFeed()

		{
			bool rtn = false;
			string rsp;

			rsp = loc_cntl.SendCommand(UiConstants.START, 20);
			if (rsp.StartsWith(UiConstants.OK))
				{
				lf_run = true;
				lrcv = new Thread(LocReceive);
				lrcv.Start();
				rtn = true;
				}
			return (rtn);
		}



		private bool LocOpen()

		{
			bool rtn = false;

			if (loc_cntl == null)
				{
				loc_cntl = new RobotConnection.Connection(robot_ip_address, UiConstants.LOC_CNTL_PORT_NO,true);
				if (loc_cntl.Connected())
					rtn = true;
				else
					loc_cntl = null;
				}
			else
				rtn = true;
			return(rtn);
		}



		private void LocSSButton_Click(object sender, EventArgs e)

		{
			if (LocSSButton.Text.StartsWith("Start"))
				{
				if (loc_feed == null)
					{
					loc_feed = new Connection(robot_ip_address, UiConstants.LOC_FEED_PORT_NO,false);
					if (loc_feed.Connected())
						{
						if (StartLocFeed())
							{
							LocSSButton.Text = "Stop";
							LocSSButton.BackColor = Color.OrangeRed;
							}
						}
					else
						loc_feed = null;
					}
				else
					{
					if (StartLocFeed())
						{
						LocSSButton.Text = "Stop";
						LocSSButton.BackColor = Color.OrangeRed;
						}
					}
				}
			else
				{
				if (loc_feed != null)
					{
					loc_cntl.SendCommand(UiConstants.STOP, 20);
					lf_run = false;
					}
				LocSSButton.Text = "Start";
				LocSSButton.BackColor = Color.LightGreen;
				}
		}




		private void RecCheckBox_CheckedChanged(object sender, EventArgs e)

		{
			string fname;
			DateTime dt = DateTime.Now;

			if (RecCheckBox.Checked)
				{
				if (!record.IsOpen())
					{
					record = new Recorder();
					fname = Log.LogDir() + "Main control record " + dt.Month + "." + dt.Day + "." + dt.Year + " " + dt.Hour + "." + dt.Minute + "." + dt.Second + ".log";
					record.OpenSession(fname,true);
					}
				}
			else
				{
				if (record.IsOpen())
					record.CloseSession();
				}
		}



		private void RSButton_Click(object sender, EventArgs e)

		{
			if (VidSSButton.Text == "Stop")
				VidSSButton_Click(null,null);
			status_cntl.SendCommand(UiConstants.RESTART, 10);
		}




		private void SDButton_Click(object sender, EventArgs e)

		{
			if (!MainForm.in_tool)
				{
				status_cntl.SendCommand(UiConstants.SHUTDOWN,10);
				SDButton.Enabled = false;
				EXButton.Enabled = false;
				}
			else
				MessageBox.Show("A tool is active.","Error");
		}



		private void RunAutoRobot()

		{
			client.RunCommand(run_ab_cmd);
		}



		private void SUButton_Click(object sender, EventArgs e)

		{
			LoginForm pwf;
			DialogResult rslt;
			string password,user,host;
			
			pwf = new LoginForm();
			rslt = pwf.ShowDialog();
			if (rslt == DialogResult.OK)
				{
				host = pwf.host;
				user = pwf.user;
				password = pwf.password;
				run_ab_cmd = pwf.run_ab_cmd;
				robot_ip_address = pwf.robot_ip_address;
				if (StatusOpen(true))
					{
					Open(true);
					SUButton.Enabled = false;
					ESButton.Enabled = true;
					EXButton.Enabled = true;
					HostTextBox.Text = host;
					}
				else
					{
					try
					{
					client = new SshClient(host,user,password);
					client.Connect();
					if (client.IsConnected)
						{
						robot = new Thread(RunAutoRobot);
						robot.Start();
						Thread.Sleep(1000);
						if (robot.IsAlive)
							{
							Open();
							SUButton.Enabled = false;
							ESButton.Enabled = true;
							EXButton.Enabled = true;
							HostTextBox.Text = host;
							}
						else
							MessageBox.Show("Could not start robot control program", "Error");
						}
					else
						MessageBox.Show("Could not connect with host SSH","Error");
					}

					catch (Exception ex)
					{
					MessageBox.Show(ex.Message,"Error");
					}

					}
				}
		}


		private void ESButton_Click(object sender, EventArgs e)

		{
			status_cntl.SendCommand(UiConstants.EMERGENCY_STOP,10);
			ESButton.Enabled = false;
		}



		private void EXButton_Click(object sender, EventArgs e)

		{
			if (!MainForm.in_tool)
				{
				status_cntl.SendCommand(UiConstants.EXIT,10);
				SDButton.Enabled = false;
				EXButton.Enabled = false;
				}
			else
				MessageBox.Show("A tool is active.","Error");
		}




		private void SLButton_Click(object sender, EventArgs e)

		{
			SetLocationForm slf;
			DialogResult rslt;
			string name,cmd,rsp;
			Point coord;
			int orient;

			slf = new SetLocationForm();
			rslt = slf.ShowDialog();
			if (rslt == DialogResult.OK)
				{
				name = slf.room;
				coord = slf.coord;
				orient = slf.orientation;
				cmd = UiConstants.SET_LOCATION + "," + name + "," + coord.X + "," + coord.Y + "," + orient;
				rsp = loc_cntl.SendCommand(cmd,10);
				if (rsp.StartsWith(UiConstants.FAIL))
					MessageBox.Show(rsp,"Error");
				}
		}



		private void HDButton_Click(object sender, EventArgs e)

		{
			status_cntl.SendCommand(UiConstants.HW_DIAG, 10);
		}


		}
	}
