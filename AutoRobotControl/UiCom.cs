using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Coding4Fun.Kinect.WinForm;
using Constants;


namespace AutoRobotControl
	{
	public static class UiCom
		{

		private const int FPS = 10;
		private const bool LOG = true;
		private const int FEED_MONITOR_WAIT_TIME = 10000;

		private static UiConnection tools = null;
		private static Thread tool_cntl_srvr = null;
		private static string tool_in_progress = "";

		private static UiConnection status = null;
		private static Thread stat_cntl_srvr = null;
		private static UiConnection status_feed = null;
		private static bool statfeed_run = false;
		private static Thread stat_feed = null;
		private static IPEndPoint st_recvr;
		private static Stopwatch st_sw = new Stopwatch();
		private static int status_rpts = 0;

		private static UiConnection video = null;
		private static Thread vid_cntl_srvr = null;
		private static byte[] videodata = null;
		private static UiConnection video_feed = null;
		private static bool vidfeed_run = false;
		private static Thread vid_feed = null;
		private static bool suspend_video_stream = false;
		private static IPEndPoint vf_recvr;
		private static Stopwatch vid_sw = new Stopwatch();
		private static int videos = 0;
		private static int waiting = 0;

		private static UiConnection activity = null;
		private static Thread act_cntl_srvr = null;
		private static object act_feed_obj = new object();
		private static UiConnection act_feed = null;
		private static bool actfeed_run = false;
		private static IPEndPoint af_recvr;

		private static UiConnection location = null;
		private static Thread loc_cntl_srvr = null;
		private static object loc_feed_obj = new object();
		private static UiConnection loc_feed = null;
		private static bool locfeed_run = false;
		private static IPEndPoint lf_recvr;


		private static bool run = false;

		private static LiteLog com_log = null;
		private static System.Timers.Timer feedtimer = null;


		public static void Start()

		{
			string fname;
			DateTime dtn = DateTime.Now;

			if (SharedData.log_operations && LOG)
				{
				fname = "Remote interface communication log " + dtn.Month + "." + dtn.Day + "." + dtn.Year + " " + dtn.Hour + "." + dtn.Minute + "." + dtn.Second + SharedData.TEXT_TILE_EXT;
				com_log = new LiteLog(fname);
				feedtimer = new System.Timers.Timer(FEED_MONITOR_WAIT_TIME);
				feedtimer.Elapsed += FeedMonitor;
				feedtimer.Enabled = true;
				}
			run = true;
			StartToolsCntrl();
			StartStatusCntrl();
			StartVideoCntl();
			StartActCntl();
			StartLocCntl();
		}



		public static void Stop()

		{
			run = false;
			statfeed_run = false;
			vidfeed_run = false;
			actfeed_run = false;
			locfeed_run = false;
			if (feedtimer != null)
				{
				feedtimer.Enabled = false;
				feedtimer.Close();
				feedtimer = null;
				}
			if ((tool_cntl_srvr != null) && tool_cntl_srvr.IsAlive)
				tool_cntl_srvr.Join();
			if ((stat_cntl_srvr != null) && stat_cntl_srvr.IsAlive)
				stat_cntl_srvr.Join();
			if ((vid_cntl_srvr != null) && vid_cntl_srvr.IsAlive)
				vid_cntl_srvr.Join();
			if ((act_cntl_srvr != null) && act_cntl_srvr.IsAlive)
				act_cntl_srvr.Join();
			if ((loc_cntl_srvr != null) && loc_cntl_srvr.IsAlive)
				loc_cntl_srvr.Join();
			if ((stat_feed != null) && stat_feed.IsAlive)
				stat_feed.Abort();
			if ((vid_feed != null) && vid_feed.IsAlive)
				vid_feed.Abort();
			if (com_log != null)
				{
				com_log.LogEntry("Communication log closed.");
				com_log.Close();
				}
		}



		private static void FeedMonitor(Object source,System.Timers.ElapsedEventArgs e)

		{
			if ((feedtimer != null) && feedtimer.Enabled)
				{ 
				if (st_sw.IsRunning)
					{
					Log("Status feed averaged " + (((double)status_rpts / st_sw.ElapsedMilliseconds) * 1000).ToString("F1") + " reports per second");
					}
				if (vid_sw.IsRunning)
					{
					Log("Averaged " + (((double)videos / vid_sw.ElapsedMilliseconds) * 1000).ToString("F1") + " video frames per second");
					if ((videos == 0) && (waiting > 0))
						Log("Video feed thread stuck.");
					}
				}
		}



		private static void Log(string msg)

		{
			if (com_log != null)
				com_log.LogEntry(msg);
		}



		private static void ToolsCntlServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg,rsp = "",ucmd;
			string[] values;

			Log("Tools control server started.");
			while (run)
				{
				msg = tools.ReceiveCmd(ref ep);
				if (msg.Length > 0)
					{
					if (msg == UiConstants.HELLO)
						rsp = UiConstants.OK;
					else if (msg.StartsWith(UiConstants.START))
						{
						values = msg.Split(',');
						if (values.Length == 2)
							{
							ucmd = values[1].Replace(' ', '_');
							ucmd = ucmd.Replace('-','_');
							Tools.OpenTool(Application.StartupPath + SharedData.TOOLS_SUB_DIR + values[1] + ".dll", ucmd + Tools.TOOL_TYPE_NAME);
							if (Tools.OpenFailed())
								rsp = UiConstants.FAIL;
							else
								{
								tool_in_progress = values[1];
								rsp = UiConstants.OK;
								}
							}
						else
							rsp = UiConstants.FAIL;
						}
					else if (msg.StartsWith(UiConstants.STOP))
						{
						values = msg.Split(',');
						if (values.Length == 2)
							{
							if (values[1] == tool_in_progress)
								{
								Tools.CloseTool();
								tool_in_progress = "";
								rsp = UiConstants.OK;
								}
							else
								rsp = UiConstants.FAIL;
							}
						else
							rsp = UiConstants.FAIL;
						}
					else
						rsp = UiConstants.FAIL + ",unknown command";
					if (rsp.Length > 0)
						tools.SendResponse(rsp, ep,true);
					}
				else
					Thread.Sleep(10);
				}
			Log("Tools control server stopped.");
		}



		private static bool StartToolsCntrl()

		{
			bool rtn = false;

			tools = new UiConnection(UiConstants.TOOL_CNTL_PORT_NO);
			if (tools.Connected())
				{
				tool_cntl_srvr = new Thread(ToolsCntlServer);
				tool_cntl_srvr.Start();
				rtn = true;
				}
			else
				Log("Could not open tools control connection.");
			return (rtn);
		}



		private static void StatFeedServer()

		{
			int wait_ms = 0;
			Stopwatch sw = new Stopwatch();
			string status;

			Log("Starting status streaming thread");
			wait_ms = 1000;
			while (statfeed_run)
				{
				sw.Restart();
				status = Constants.UiConstants.STATUS + ",";
				status += SharedData.status + ",";
				status += Convert.ToByte(SharedData.kinect_operational) + ",";
				status += Convert.ToByte(SharedData.head_assembly_operational) + ",";
				status += Convert.ToByte(SharedData.motion_controller_operational) + ",";
				status += Convert.ToByte(SharedData.front_lidar_operational) + ",";
				status += Convert.ToByte(SharedData.rear_lidar_operational) + ",";
				status += Convert.ToByte(SharedData.arm_operational) + ",";
				status += Convert.ToByte(SharedData.navdata_operational) + ",";
				status += Convert.ToByte(SharedData.speech_recognition_active) + ",";
				status += Convert.ToByte(SharedData.visual_obj_detect_operational) + ",";
				status += Convert.ToByte(SharedData.speech_direct_operational) + ",";
				status += SharedData.main_battery_volts;
				status_feed.SendResponse(status,st_recvr);
				status_rpts += 1;
				sw.Stop();
				if (wait_ms > sw.ElapsedMilliseconds)
					Thread.Sleep(wait_ms - (int) sw.ElapsedMilliseconds);
				}
			Log("Status feed thread stopped.");
		}



		private static void Shutdown()

		{
			Supervisor.Stop(true);
		}



		private static void Exit()

		{
			Supervisor.Stop(false);
		}



		private static void StatCntlServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg,rsp = "";

			Log("Status control server started.");
			while (run)
				{
				msg = status.ReceiveCmd(ref ep);
				if (msg.Length > 0)
					{
					if (msg == UiConstants.HELLO)
						rsp = UiConstants.OK;
					else if (msg == UiConstants.HW_DIAG)
						{
						Thread hw_diag = new Thread(Supervisor.HwDiagnostics);
						hw_diag.Start();
						rsp = UiConstants.OK;
						}
					else if (msg == UiConstants.SHUTDOWN)
						{
						Thread shutdwn = new Thread(Shutdown);
						shutdwn.Start();
						rsp = UiConstants.OK;
						}
					else if (msg == UiConstants.EXIT)
						{
						Thread exit = new Thread(Exit);
						exit.Start();
						rsp = UiConstants.OK;
						}
					else if (msg == UiConstants.EMERGENCY_STOP)
						{
						MotionControl.SendStopCommand("SL");
						MotionControl.SendStopCommand("SS");
						Thread shutdwn = new Thread(Shutdown);
						shutdwn.Start();
						rsp = UiConstants.OK;
						}
					else if (msg == UiConstants.RESTART)
						{
						Supervisor.Restart();
						rsp = UiConstants.OK;
						}
					else if (msg == UiConstants.START)
						{
						if (!statfeed_run)
							{
							status_feed = new UiConnection(UiConstants.STATUS_FEED_PORT_NO);
							if (status_feed.Connected())
								{
								st_recvr = new IPEndPoint(ep.Address,UiConstants.STATUS_FEED_PORT_NO);
								statfeed_run = true;
								stat_feed = new Thread(StatFeedServer);
								st_sw.Restart();
								status_rpts = 0;
								stat_feed.Start();
								rsp = UiConstants.OK;
								}
							else
								rsp = UiConstants.FAIL;
							}
						else
							rsp = UiConstants.OK;
						}
					else if (msg == UiConstants.STOP)
						{
						if (statfeed_run)
							{
							statfeed_run = false;
							if ((stat_feed != null) && stat_feed.IsAlive)
								stat_feed.Join();
							status_feed.Close();
							status_feed = null;
							st_sw.Stop();
							Log("Status feed averaged " + (((double) status_rpts/st_sw.ElapsedMilliseconds) * 1000).ToString("F1") + " reports per second");
							}
						rsp = UiConstants.OK;
						}
					else
						rsp = UiConstants.FAIL + ",unknown command";
					if (rsp.Length > 0)
						status.SendResponse(rsp, ep,true);
					}
				else
					Thread.Sleep(10);
				}
			if (statfeed_run)
				{
				statfeed_run = false;
				if ((stat_feed != null) && stat_feed.IsAlive)
					stat_feed.Join();
				}
			Log("Status control server stopped.");
		}



		private static bool StartStatusCntrl()

		{
			bool rtn = false;

			status = new UiConnection(UiConstants.STATUS_CNTL_PORT_NO);
			if (status.Connected())
				{
				stat_cntl_srvr = new Thread(StatCntlServer);
				stat_cntl_srvr.Start();
				rtn = true;
				}
			else
				Log("Could not open status control connection.");
			return (rtn);
		}



		public static void SetVideoSuspend(bool val)

		{
			suspend_video_stream = val;
			Log("Set video suspend: " + val);
		}


		
		private static void VidFeedServer()

		{
			int wait_ms = 0;
			Stopwatch sw = new Stopwatch();
			Bitmap bm;
			string cmd,rsp;
			MemoryStream ms;

			Log("Starting video streaming thread");
			wait_ms = 1000/FPS;
			while (vidfeed_run)
				{
				sw.Restart();
				if (!suspend_video_stream)
					{
					if ((Kinect.nui != null) && (Kinect.nui.IsRunning))
						{
						waiting += 1;
						if (Kinect.GetColorFrame(ref videodata,40))
							{
							bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
							ms = new MemoryStream();
							bm.Save(ms,System.Drawing.Imaging.ImageFormat.Jpeg);
							cmd = UiConstants.VIDEO_FRAME + "," + ms.Length;
							rsp = video_feed.SendCommand(cmd, 20,vf_recvr);
							if (rsp.StartsWith(UiConstants.OK))
								{
								video_feed.SendStream(ms,vf_recvr);
								videos += 1;
								}
							else
								Log("Video frame rejected.");
							}
						else
							Log("Color frame not available.");
						waiting -= 1;
						}
					else
						Log("Kinect not available.");
					}
				else
					Log("Video feed suspended.");
				sw.Stop();
				if (wait_ms > sw.ElapsedMilliseconds)
					Thread.Sleep(wait_ms - (int) sw.ElapsedMilliseconds);
				}
			Log("Video streaming thread stopped.");
		}



		private static void VidCntlServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg,rsp = "";

			Log("Video control server started.");
			while (run)
				{
				msg = video.ReceiveCmd(ref ep);
				if (msg.Length > 0)
					{
					if (msg == UiConstants.HELLO)
						rsp = UiConstants.OK;
					else if (msg == UiConstants.START)
						{
						if ((Kinect.nui != null) && Kinect.nui.IsRunning)
							{
							if (!vidfeed_run)
								{
								video_feed = new UiConnection(UiConstants.VIDEO_FEED_PORT_NO);
								if (video_feed.Connected())
									{
									if (videodata == null)
										videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
									vf_recvr = new IPEndPoint(ep.Address,UiConstants.VIDEO_FEED_PORT_NO);
									vidfeed_run = true;
									suspend_video_stream = false;
									videos = 0;
									waiting = 0;
									vid_sw.Restart();
									vid_feed = new Thread(VidFeedServer);
									vid_feed.Start();
									rsp = UiConstants.OK;
									}
								else
									rsp = UiConstants.FAIL;
								}
							else
								rsp = UiConstants.OK;
							}
						else
							rsp = UiConstants.FAIL + ",Kinect not operational";
						}
					else if (msg == UiConstants.STOP)
						{
						if (vidfeed_run)
							{
							vidfeed_run = false;
							suspend_video_stream = true;
							vid_sw.Stop();
							if ((vid_feed != null) && vid_feed.IsAlive)
								vid_feed.Join();
							video_feed.Close();
							video_feed = null;
							Log("Averaged " + (((double) videos / vid_sw.ElapsedMilliseconds) * 1000).ToString("F1") + " video frames per second");
							if ((videos == 0) && (waiting > 0))
								Log("Video feed thread stuck.");
							}
						rsp = UiConstants.OK;
						}
					else if (msg.StartsWith(UiConstants.SUSPEND))
						{
						if (vidfeed_run)
							{
							if (msg.Contains(true.ToString()))
								suspend_video_stream = true;
							else if (msg.Contains(false.ToString()))
								suspend_video_stream = false;
							}
						rsp = UiConstants.OK;
						}
					else
						rsp = UiConstants.FAIL + ",unknown command";
					if (rsp.Length > 0)
						video.SendResponse(rsp, ep,true);
					}
				else
					Thread.Sleep(10);
				}
			if (vidfeed_run)
				{
				vidfeed_run = false;
				if ((vid_feed != null) && vid_feed.IsAlive)
					vid_feed.Join();
				}
			Log("Video control server stopped.");
		}



		private static bool StartVideoCntl()

		{
			bool rtn = false;

			video = new UiConnection(Constants.UiConstants.VIDEO_CNTL_PORT_NO);
			if (video.Connected())
				{
				vid_cntl_srvr = new Thread(VidCntlServer);
				vid_cntl_srvr.Start();
				rtn = true;
				}
			else
				Log("Could not open video control connection.");
			return(rtn);
		}



		public static void SendActMessage(string msg)

		{
			if (actfeed_run)
				{

				lock(act_feed_obj)
				{
				act_feed.SendResponse(msg,af_recvr);
				}

				}
		}


		private static void ActCntlServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg,rsp = "";

			Log("Activity control server started.");
			while (run)
				{
				msg = activity.ReceiveCmd(ref ep);
				if (msg.Length > 0)
					{
					if (msg == UiConstants.HELLO)
						rsp = UiConstants.OK;
					else if (msg == UiConstants.START)
						{
						if (!actfeed_run)
							{
							act_feed = new UiConnection(UiConstants.ACTIVITY_FEED_PORT_NO);
							if (act_feed.Connected())
								{
								af_recvr = new IPEndPoint(ep.Address, UiConstants.ACTIVITY_FEED_PORT_NO);
								actfeed_run = true;
								rsp = UiConstants.OK;
								}
							else
								rsp = UiConstants.FAIL;
							}
						else
							rsp = UiConstants.OK;
						}
					else if (msg == UiConstants.STOP)
						{
						if (actfeed_run)
							{
							actfeed_run = false;
							act_feed.Close();
							act_feed = null;
							}
						rsp = UiConstants.OK;
						}
					else
						rsp = UiConstants.FAIL + ",unknown command";
					if (rsp.Length > 0)
						activity.SendResponse(rsp, ep,true);
					}
				else
					Thread.Sleep(10);
				}
			Log("Activity control server stopped.");
		}



		private static bool StartActCntl()

		{
			bool rtn = false;

			activity = new UiConnection(Constants.UiConstants.ACTIVITY_CNTL_PORT_NO);
			if (activity.Connected())
				{
				act_cntl_srvr = new Thread(ActCntlServer);
				act_cntl_srvr.Start();
				rtn = true;
				}
			else
				Log("Could not open action control connection.");
			return(rtn);
		}



		public static void SendLocMessage(string msg)

		{
			if (locfeed_run)
				{

				lock(loc_feed_obj)
				{
				loc_feed.SendResponse(UiConstants.LOCATION + "," + msg,lf_recvr);
				}

				}
		}


		private static void LocCntlServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg,rsp = "";
			string[] values;

			Log("Location control server started.");
			while (run)
				{
				msg = location.ReceiveCmd(ref ep);
				if (msg.Length > 0)
					{
					if (msg == UiConstants.HELLO)
						rsp = UiConstants.OK;
					else if (msg == UiConstants.START)
						{
						if (!locfeed_run)
							{
							loc_feed = new UiConnection(UiConstants.LOC_FEED_PORT_NO);
							if (loc_feed.Connected())
								{
								lf_recvr = new IPEndPoint(ep.Address, UiConstants.LOC_FEED_PORT_NO);
								locfeed_run = true;
								rsp = UiConstants.OK;
								}
							else
								rsp = UiConstants.FAIL;
							}
						else
							rsp = UiConstants.OK;
						}
					else if (msg == UiConstants.STOP)
						{
						if (locfeed_run)
							{
							locfeed_run = false;
							loc_feed.Close();
							loc_feed = null;
							}
						rsp = UiConstants.OK;
						}
					else if (msg.StartsWith(UiConstants.SET_LOCATION))
						{
						values = msg.Split(',');
						if (values.Length == 5)
							{
							NavData.location loc = new NavData.location();
							loc.loc_name = values[1];
							loc.coord.X = int.Parse(values[2]);
							loc.coord.Y = int.Parse(values[3]);
							loc.orientation = int.Parse(values[4]);
							loc.ls = NavData.LocationStatus.USR;
							NavData.SetCurrentLocation(loc);
							}
						else
							rsp = UiConstants.FAIL;
						}
					else
						rsp = UiConstants.FAIL + ",unknown command";
					if (rsp.Length > 0)
						location.SendResponse(rsp, ep,true);
					}
				else
					Thread.Sleep(10);
				}
			Log("Location control server stopped.");
		}



		private static bool StartLocCntl()

		{
			bool rtn = false;

			location = new UiConnection(Constants.UiConstants.LOC_CNTL_PORT_NO);
			if (location.Connected())
				{
				loc_cntl_srvr = new Thread(LocCntlServer);
				loc_cntl_srvr.Start();
				rtn = true;
				}
			else
				Log("Could not open location control connection.");
			return(rtn);
		}


		}

	}
