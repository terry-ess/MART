using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Microsoft.Kinect;
using System.Net;
using System.Threading;
using AutoRobotControl;
using Constants;
using Coding4Fun.Kinect.WinForm;


namespace Manual_Control
	{
	class ManualControl: ToolsInterface
		{
		private const string RIGHT_TURN = "R SLOW";
		private const string LEFT_TURN = "L SLOW";
		private const string FORWARD = "TF 0";
		private const string BACKWARD = "TB 0";
		private const string STOP_COMMAND = "SL";
		private const string STOP_SPIN_COMMAND = "SM";

		private UiConnection connect = null;
		private Thread srvr = null;
		private UiConnection kaconnect = null;
		private Thread kasrvr = null;
		private bool run = false;
		private UiConnection status_feed = null;
		private Thread stat_feed = null;
		private IPEndPoint st_recvr;
		private bool sf_run = false;
		private object st_feed_obj = new object();
		private Thread mtn_thread = null;
		private bool motion_in_progress = false;
		private string motion_cmd = "";
		private static byte[] videodata = null;
		private static int dist;


		public bool Open(params object[] obj)

		{
			bool rtn = false;

			if (SharedData.head_assembly_operational && SharedData.kinect_operational && SharedData.front_lidar_operational 
			&& SharedData.motion_controller_operational && SharedData.navdata_operational)
				{
				videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
				connect = new UiConnection(Constants.UiConstants.TOOL_PORT_NO);
				kaconnect = new UiConnection(Constants.UiConstants.TOOL_KEEP_ALIVE_PORT_NO);
				if (connect.Connected() && kaconnect.Connected())
					{
					run = true;
					srvr = new Thread(UdpServer);
					srvr.Start();
					kasrvr = new Thread(KaServer);
					kasrvr.Start();
					rtn = true;
					}
				else
					{
					if (connect.Connected())
						connect.Close();
					connect = null;
					kaconnect = null;
					}
				}
			return (rtn);
		}



		public void Close()

		{
			run = false;
			sf_run = false;
			if ((srvr != null) && srvr.IsAlive)
				srvr.Join();
			if ((kasrvr != null) && kasrvr.IsAlive)
				kasrvr.Join();
			if (connect != null)
				{
				connect.Close();
				connect = null;
				}
			if (kaconnect != null)
				{
				kaconnect.Close();
				kaconnect = null;
				}
			if ((stat_feed != null) && stat_feed.IsAlive)
				stat_feed.Join();
			if (status_feed != null)
				{
				status_feed.Close();
				status_feed = null;
				}
		}



		private void SendStatusMessage(string msg)

		{
			if (sf_run)
				{

				lock (st_feed_obj)
				{
				status_feed.SendResponse(msg, st_recvr);
				}

				}
		}



		private void StatFeed()

		{
			int wait_ms = 2000;
			Stopwatch sw = new Stopwatch();
			string msg;

			Log.LogEntry("Manual control status feed started.");
			while (sf_run)
				{
				sw.Restart();
				msg = UiConstants.SENSOR_DATA + ",";
				msg += HeadAssembly.GetMagneticHeading() + ",";
				msg += AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.FRONT,false) + ",";
				msg += AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.REAR,false);
				SendStatusMessage(msg);
				sw.Stop();
				if (wait_ms > sw.ElapsedMilliseconds)
					Thread.Sleep(wait_ms - (int) sw.ElapsedMilliseconds);
				}
			Log.LogEntry("Manual control status feed stopped.");
			stat_feed = null;
		}



		private void MotionCommand()

		{
			string rsp;
			string[] values;

			rsp = AutoRobotControl.MotionControl.SendCommand(motion_cmd);
			Log.LogEntry("manual motion command response: " + rsp);
			if (rsp.StartsWith(UiConstants.FAIL))
				SendStatusMessage(UiConstants.STATUS + "," + rsp);
			else
				{
				values = rsp.Split(' ');
				if (values.Length == 2)
					dist = int.Parse(values[1]);
				else
					dist = 0;
				}
			motion_in_progress = false;
		}



		private void DeterminLocation()

		{
			NavData.location loc;
			string msg;
			string new_rm = "";
			MotionMeasureProb.Pose epose;
			Location nloc = new Location();
			ArrayList sdata = new ArrayList();
			NavCompute.out_of_bounds ob;

			loc = NavData.GetCurrentLocation();
			if (loc.ls != NavData.LocationStatus.UNKNOWN)
				{
				loc.ls = NavData.LocationStatus.DR;
				if ((motion_cmd == FORWARD) || (motion_cmd == BACKWARD))
					{
					int direct;

					if (motion_cmd == FORWARD)
						direct = loc.orientation;
					else
						direct = (loc.orientation + 180) % 360;
					loc.loc_name = "";
					loc.coord.X += (int)Math.Round(dist * Math.Sin(direct * SharedData.DEG_TO_RAD));
					loc.coord.Y -= (int)Math.Round(dist * Math.Cos(direct * SharedData.DEG_TO_RAD));
					ob = NavCompute.LocationOutOfBounds(loc.coord);
					if (ob != NavCompute.out_of_bounds.NOT)
						{
						Point new_loc = new Point(),x_loc = new Point(),org = new Point(),ox_loc = new Point();
						NavData.connection oconnector = new NavData.connection(), nconnector = new NavData.connection();
						int mdist = 0;

						new_rm = NavCompute.DetermineNewRoom(loc.coord,ob,direct,ref new_loc,ref x_loc,ref ox_loc,ref oconnector,ref nconnector,ref mdist);
						if (new_rm.Length > 0)
							{
							org = loc.coord;
							Log.LogEntry("I just entered " + new_rm);
							loc.rm_name = new_rm;
							loc.coord = x_loc;
							loc.loc_name = "";
							loc.ls = NavData.LocationStatus.DR;
							NavData.SetCurrentLocation(loc);
							epose.coord = x_loc;
							epose.orient = loc.orientation;
							MotionMeasureProb.ConnectionLocalize(epose,oconnector.direction,oconnector.exit_width,mdist);
							Navigate.rmi.Open(nconnector);
							loc.coord = new_loc;
							}
						else
							Log.LogEntry("Could not determine new room");
						}
					epose.coord = loc.coord;
					epose.orient = loc.orientation;
					MotionMeasureProb.Move(epose);
					nloc.DetermineDRLocation(ref loc, false, new Point(0, 0));
					}																					
				else if ((motion_cmd == RIGHT_TURN) || (motion_cmd == LEFT_TURN))
					{
					loc.orientation -= dist;
					if (loc.orientation > 359)
						loc.orientation %= 360;
					else if (loc.orientation < 0)
						loc.orientation += 360;
					loc.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(dist),loc.orientation, loc.coord);
					epose.coord = loc.coord;
					epose.orient = loc.orientation;
					MotionMeasureProb.Move(epose);
					if (Rplidar.CaptureScan(ref sdata, true))
						Rplidar.SaveLidarScan(ref sdata);
					}
				Rectangle rect = MotionMeasureProb.PdfRectangle();
				msg = loc.rm_name + "," + loc.coord.X + "," + loc.coord.Y + "," + loc.orientation + "," + loc.loc_name + "," + loc.ls + "," + rect.Height + "," + rect.Width + "," + rect.X + "," + rect.Y;
				UiCom.SendLocMessage(msg);
				NavData.SetCurrentLocation(loc);
				if (new_rm.Length == 0)
					{
					SharedData.current_rm.ClearLastRobotLocation();
					SharedData.current_rm.ClearLastPDFEllipse();
					}
				SharedData.current_rm.DisplayRobotLoc(loc.coord, Brushes.Yellow, loc.orientation);
				SharedData.current_rm.DisplayPDFEllipse();
				SharedData.current_rm.DisplayRmMap();
				}
			}



		private void KaServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg;
			int no_msg_count = 0;

			Log.LogEntry("Manual control keep alive server started.");
			while (run)
				{
				msg = kaconnect.ReceiveCmd(ref ep);
				if (run && (msg.Length > 0))
					{
					if (msg == UiConstants.KEEP_ALIVE)
						no_msg_count = 0;
					Thread.Sleep(10);
					}
				else
					{
					if (run)
						{
						no_msg_count += 1;
						if (no_msg_count < UiConstants.INTF_TIMEOUT_COUNT)
							Thread.Sleep(10);
						else
							{
							Log.LogEntry("Manual control timed out.");
							run = false;
							if ((srvr != null) && srvr.IsAlive)
								srvr.Join();
							kasrvr = null;
							Tools.CloseTool();
							}
						}
					}
				}
			Log.LogEntry("Manual control keep alive server closed");
		}



		private void StreamVideo(IPEndPoint rcvr)

		{
			Bitmap bm;
			MemoryStream ms;
			String cmd,rsp;

			if (Kinect.GetColorFrame(ref videodata,40))
				{
				bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				ms = new MemoryStream();
				bm.Save(ms,System.Drawing.Imaging.ImageFormat.Jpeg);
//				if (ms.Length > 20000)
					{
					cmd = UiConstants.VIDEO_FRAME + "," + ms.Length;
					rsp = connect.SendCommand(cmd, 20,rcvr);
					if (rsp.StartsWith(UiConstants.OK))
						connect.SendStream(ms,rcvr);
					}
//				else
//					connect.SendResponse(UiConstants.FAIL + ",short image",rcvr,true);
				}
			else
				connect.SendResponse(UiConstants.FAIL + ",could not obtain image",rcvr,true);
		}



		private void UdpServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg,rsp = "";
			int val;
			string[] values;
			MemoryStream ms;

			Log.LogEntry("Manual control server started.");
			while (run)
				{

				try
				{
				msg = connect.ReceiveCmd(ref ep);
				if (run && (msg.Length > 0))
					{
					if (msg == UiConstants.HELLO)
						rsp = UiConstants.OK;
					else if (msg == UiConstants.CLOSE)
						{
						connect.Close();
						run = false;
						rsp = "";
						}
					else if (msg == UiConstants.START)
						{
						status_feed = new UiConnection(UiConstants.TOOL_FEED_PORT_NO);
						if (status_feed.Connected())
							{
							sf_run = true;
							st_recvr = new IPEndPoint(ep.Address,UiConstants.TOOL_FEED_PORT_NO);
							stat_feed = new Thread(StatFeed);
							stat_feed.Start();
							Log.LogEntry("Manual control status feed connection open with end point: " + st_recvr.ToString());
							rsp = UiConstants.OK;
							}
						else
							rsp = UiConstants.FAIL + ", could not create status feed connection";
						}
					else if (msg == UiConstants.STOP)
						{
						if (status_feed.Connected())
							{
							sf_run = false;
							if ((stat_feed != null) && (stat_feed.IsAlive))
								stat_feed.Join();
							status_feed.Close();
							status_feed = null;
							}
						rsp = UiConstants.OK;
						}
					else if (msg == UiConstants.FORWARD)
						{
						values = msg.Split(',');
						if (values.Length == 1)
							{
							motion_in_progress = true;
							motion_cmd = FORWARD;
							mtn_thread = new Thread(MotionCommand);
							mtn_thread.Start();
							rsp = UiConstants.OK;
							}
						else
							{

							}
						}
					else if (msg == UiConstants.BACKWARD)
						{
						motion_in_progress = true;
						motion_cmd = BACKWARD;
						mtn_thread = new Thread(MotionCommand);
						mtn_thread.Start();
						rsp = UiConstants.OK;
						}
					else if (msg == UiConstants.RIGHT_TURN)
						{
						motion_in_progress = true;
						motion_cmd = RIGHT_TURN;
						rsp = AutoRobotControl.MotionControl.SendCommand(RIGHT_TURN);
						if (rsp.StartsWith(UiConstants.OK))
							rsp = UiConstants.OK;
						else
							rsp = UiConstants.FAIL;
						}
					else if (msg == UiConstants.LEFT_TURN)
						{
						motion_in_progress = true;
						motion_cmd = LEFT_TURN;
						rsp = AutoRobotControl.MotionControl.SendCommand(LEFT_TURN);
						if (rsp.StartsWith(UiConstants.OK))
							rsp = UiConstants.OK;
						else
							rsp = UiConstants.FAIL;
						}
					else if (msg == UiConstants.STOP_MOTION)
						{
						if (motion_in_progress)
							{
							if ((motion_cmd == RIGHT_TURN) || (motion_cmd == LEFT_TURN))
								{
								rsp = AutoRobotControl.MotionControl.SendCommand(STOP_SPIN_COMMAND);
								if (rsp.StartsWith(UiConstants.OK))
									{
									values = rsp.Split(' ');
									if (values.Length == 2)
										dist = int.Parse(values[1]);
									else
										dist = 0;
									DeterminLocation();
									rsp = UiConstants.OK;
									}
								else
									rsp = UiConstants.FAIL;
								}
							else
								{
								if (AutoRobotControl.MotionControl.SendStopCommand(STOP_COMMAND))
									{
									mtn_thread.Join();
									DeterminLocation();
									rsp = UiConstants.OK;
									}
								else
									rsp = UiConstants.FAIL;
								}
							motion_in_progress = false;
							motion_cmd = "";
							}
						else
							{
							motion_cmd = "";
							rsp = UiConstants.FAIL + ",no motion in progress";
							}
						}
					else if (msg == UiConstants.CURRENT_PAN)
						{
						val = HeadAssembly.PanAngle();
						rsp = UiConstants.OK + "," + val;
						}
					else if (msg == UiConstants.CURRENT_TILT)
						{
						val = HeadAssembly.TiltAngle();
						rsp = UiConstants.OK + "," + val;
						}
					else if (msg.StartsWith(UiConstants.SET_PAN_TILT + ","))
						{
						values = msg.Split(',');
						if (values.Length == 3)
							{
							if (HeadAssembly.Pan(int.Parse(values[1]), true))
								if (HeadAssembly.Tilt(int.Parse(values[2]), false))
									rsp = UiConstants.OK + "," + values[1] + "," + values[2];
								else
									rsp = UiConstants.FAIL + ",tilt command exeuction failed";
							else
								rsp = UiConstants.FAIL + ",pan command execution failed";
							}
						else
							rsp = UiConstants.FAIL + ",bad format";
						}
					else if (msg == UiConstants.SEND_LIDAR)
						{
						ms = new MemoryStream();
						if (Lidar.RCaptureScan(ref ms))
							{
							connect.SendResponse(UiConstants.OK,ep,true);
							msg = UiConstants.LIDAR_FRAME + "," + ms.Length;
							rsp = connect.SendCommand(msg, 20,ep);
							if (rsp.StartsWith(UiConstants.OK))
								connect.SendStream(ms,ep);
							}
						else
							rsp = UiConstants.FAIL;
						}
					else if (msg == UiConstants.SEND_VIDEO_PARAM)
						{
						if ((Kinect.nui != null) && (Kinect.nui.IsRunning))
							rsp = UiConstants.OK + "," + Kinect.nui.ColorStream.FrameWidth + "," + Kinect.nui.ColorStream.FrameHeight + "," + Kinect.nui.ColorStream.NominalHorizontalFieldOfView + "," + Kinect.nui.ColorStream.NominalVerticalFieldOfView;
						else
							rsp = UiConstants.FAIL;
						}
					else if (msg == UiConstants.SEND_VIDEO)
						{
						rsp = UiConstants.OK + ",latter";
						connect.SendResponse(rsp, ep,true);
						StreamVideo(ep);
						rsp = "";
						}
					else if (msg.StartsWith(UiConstants.CHECK_TURN))
						{
						values = msg.Split(',');
						if (values.Length == 3)
							{
							int angle,mod = 0,modd = 0;

							angle = int.Parse(values[2]);
							if (values[1] == UiConstants.LEFT_TURN)
								angle *= -1;
							if (!Turn.TurnSafe(angle, ref mod, ref modd))
								rsp = UiConstants.FAIL + ",turn not safe";
							else
								rsp = UiConstants.OK + ",turn safe";
							}
						else
							rsp = UiConstants.FAIL + ",bad format";
						}
					else
						{
						rsp = UiConstants.FAIL + ",unknown command";
						}
					if (rsp.Length > 0)
						connect.SendResponse(rsp,ep,true);
					}
				else
					{
					if (run)
						Thread.Sleep(10);
					}
				}

				catch(Exception ex)
				{
				Log.LogEntry("Manual control server exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				}
			Log.LogEntry("Manual control server stopped.");
		}

		}
	}
