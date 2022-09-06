using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using AutoRobotControl;
using Constants;

namespace Person_Move_PID_Calibration
	{
	public class PersonPIDCal : ToolsInterface,CommandHandlerInterface
		{
		private const string GRAMMAR = "PPID";
		private const string RUN = "run";
		private const string AGAIN = "again";
		private const string DONE = "done";

		public struct CommandOccur
		{
			public AutoResetEvent evnt;
			public string msg;

			public CommandOccur(bool set)
			{
				evnt = new AutoResetEvent(set);
				msg = "";
			}
		};

		private UiConnection connect = null;
		private Thread srvr = null;
		private UiConnection kaconnect = null;
		private Thread kasrvr = null;
		private bool run = false;
		private UiConnection status_feed = null;
		private IPEndPoint st_recvr;
		private Thread exec_thread = null;
		private bool run_ppid = false;
		private string last_run_ds = "";
		private CommandOccur co;
		private AutoRobotControl.Move mov = new Move();
		private AutoRobotControl.ComeHere ch = new ComeHere();


		public bool Open(params object[] obj)

		{
			bool rtn = false;

			if (SharedData.head_assembly_operational && SharedData.kinect_operational && SharedData.front_lidar_operational 
			&& SharedData.motion_controller_operational && SharedData.navdata_operational)
				{
				co = new CommandOccur(false);
				connect = new UiConnection(Constants.UiConstants.TOOL_PORT_NO);
				kaconnect = new UiConnection(Constants.UiConstants.TOOL_KEEP_ALIVE_PORT_NO);
				if (connect.Connected() && kaconnect.Connected())
					{
					run = true;
					srvr = new Thread(UdpServer);
					srvr.Start();
					kasrvr = new Thread(KaServer);
					kasrvr.Start();
					AddPPIDGrammar();
					co.evnt.Reset();
					co.msg = "";
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
			if ((exec_thread != null) && exec_thread.IsAlive)
				exec_thread.Abort();
			if (status_feed != null)
				{
				status_feed.Close();
				status_feed = null;
				}
			Speech.UnloadGrammar(GRAMMAR);
		}



		public void RegisterCommandSpeech()

		{

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



		private void SendStatusMessage(string msg)

		{
			status_feed.SendResponse(UiConstants.CAL_STATUS + "," + msg,st_recvr,true);
		}



		private void MoveToRecharge(NavData.location cl)

		{
			RechargeDock rcd = null;

			if (Turn.TurnAngle(180))
				{
				rcd = new RechargeDock();
				if (rcd.StartDocking(NavData.GetRechargeStation(cl.rm_name), ref cl, false))
					{
					cl.loc_name = SharedData.RECHARGE_LOC_NAME;
					NavData.SetCurrentLocation(cl);
					Speech.SpeakAsync("I have docked at the recharge station.");
					}
				else
					Speech.SpeakAsync("Docking attempt failed.");
				}
			else
				Speech.SpeakAsync("Docking attempt failed.");
		}



		private bool ReturnToStart(int dist)

		{
			int sdist, ldist;
			double cdist = 0;
			string rsp;
			int pdist = 0,pdirect = -1;
			bool rtn = false;

			if (ch.TurnToPerson(ref pdist, ref pdirect))
				{
				sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.REAR);
				if (LS02CLidar.RearClearence(ref cdist, SharedData.ROBOT_WIDTH + 1))
					{
					ldist = (int)Math.Round(cdist);
					sdist = Math.Min(sdist, ldist);
					}
				if (sdist < dist + SharedData.ROBOT_LENGTH + SharedData.REAR_SONAR_MIN_CLEARANCE)
					dist = sdist - (SharedData.ROBOT_LENGTH + SharedData.REAR_SONAR_MIN_CLEARANCE);
				rsp = AutoRobotControl.MotionControl.SendCommand(SharedData.BACKWARD + " " + dist, 20000);
				if (rsp.StartsWith("ok"))
					rtn = true;
				}
			return(rtn);
		}



		private void PPIDCalThread()

		{
			NavData.location cl;
			int dist = 0,dist2 = 0,direct = 0,runs = 0;
			string cmd;
			NavData.room_pt rp;
			Room rm = SharedData.current_rm;

			try
				{
			cl = NavData.GetCurrentLocation();
			if (cl.loc_name == SharedData.RECHARGE_LOC_NAME)
				{
				rp.name = cl.rm_name;
				rp.coord = new Point(cl.coord.X + 30,cl.coord.Y);
				rm.Run = true;
				if (mov.GoToRoomPoint(rp,new Point(0,0)))
					{
					rm.Run = false;
					while (run_ppid)
						{
						Speech.SpeakAsync("Ready");
						do
							{
							co.evnt.WaitOne();
							cmd = co.msg;
							co.msg = "";
							}
						while((cmd != DONE) && (cmd != RUN));
						if (cmd == RUN)
							{
							Speech.SpeakAsync("Starting move to person");
							if (ch.TurnToPerson(ref dist,ref direct))
								{
								last_run_ds = "";
								if (ch.MoveToPerson(ref dist2,ref last_run_ds))
									{
									runs += 1;
									SendStatusMessage(UiConstants.CAL_RUN_COMPLETED + "," + runs);
									Speech.SpeakAsync("Move to person completed. Again or done?");
									do
										{
										co.evnt.WaitOne();
										cmd = co.msg;
										co.msg = "";
										}
									while ((cmd != DONE) && (cmd != AGAIN));
									if (cmd == DONE)
										{
										Speech.SpeakAsync("Returning to recharge.");
										MoveToRecharge(cl);
										run_ppid = false;
										SendStatusMessage(UiConstants.CAL_RUN_DONE);
										}
									else
										{
										if (!ReturnToStart(dist - dist2))
											{
											Speech.SpeakAsync("Attempt to return to start position failed. Run aborted.");
											run_ppid = false;
											SendStatusMessage(UiConstants.CAL_RUN_ABORTED);
											}
										}
									}
								else
									{
									Speech.SpeakAsync("Move to person failed. Returning to recharge.");
									MoveToRecharge(cl);
									}
								}
							}
						else
							{
							run_ppid = false;
							Speech.SpeakAsync("Returning to recharge.");
							MoveToRecharge(cl);
							}
						}
					}
				else
					{
					rm.Run = false;
					Speech.SpeakAsync("Attempt to face person failed. Run aborted.");
					run_ppid = false;
					SendStatusMessage(UiConstants.CAL_RUN_ABORTED);
					}
				}
			else
				{
				Speech.SpeakAsync("Not at recharge station. Run aborted.");
				SendStatusMessage(UiConstants.CAL_RUN_ABORTED);
				}
			}

			catch (ThreadAbortException)
			{
			run_ppid = false;
			}

			catch(Exception ex)
			{
			run_ppid = false;
			SendStatusMessage(UiConstants.CAL_RUN_ABORTED);
			Speech.SpeakAsync("Exception occured during run");
			Log.LogEntry("PPIDCalThread exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			status_feed.Close();
		}



		private void UdpServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg,rsp = "";
			string[] values;
			double pgain, igain, dgain;

			Log.LogEntry("Person PID cal server started.");
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
					else if (msg.StartsWith(UiConstants.UPDATE_PID_PARAM))
						{
						values = msg.Split(',');
						if (values.Length == 4)
							{

							try
							{
							pgain = double.Parse(values[1]);
							igain = double.Parse(values[2]);
							dgain = double.Parse(values[3]);
							ch.LoadPIDParam(pgain,igain,dgain);
							rsp = UiConstants.OK;
							}

							catch(Exception)
							{
							rsp = UiConstants.FAIL + ", bad parameter";
							}

							}
						else
							rsp = UiConstants.FAIL + ", wrong number of parametes";
						}
					else if (msg == UiConstants.START_PPID_CAL)
						{
						if ((exec_thread == null) || (!exec_thread.IsAlive))
							{
							status_feed = new UiConnection(UiConstants.TOOL_FEED_PORT_NO);
							if (status_feed.Connected())
								{
								st_recvr = new IPEndPoint(ep.Address, UiConstants.TOOL_FEED_PORT_NO);
								Log.LogEntry("Tool status feed connection open with end point: " + st_recvr.ToString());
								run_ppid = true;
								exec_thread = new Thread(PPIDCalThread);
								exec_thread.Start();
								rsp = UiConstants.OK;
								}
							else
								rsp = UiConstants.FAIL + ", could not open status feed connection";
							}
						else
							rsp = UiConstants.FAIL + ", PPID calibration thread is running";
						}
					else if (msg == UiConstants.SEND_LAST_DS)
						{
						string cmd;
						StreamReader sr;
						MemoryStream ms = new MemoryStream();

						if (last_run_ds.Length > 0)
							{
							connect.SendResponse(UiConstants.OK, ep);
							sr = new StreamReader(last_run_ds);
							sr.BaseStream.CopyTo(ms);
							cmd = UiConstants.LAST_SENSOR_DS + "," + ms.Length;
							rsp = connect.SendCommand(cmd, 20, ep);
							if (rsp.StartsWith(UiConstants.OK))
								connect.SendStream(ms, ep);
							rsp = "";
							}
						else
							rsp = UiConstants.FAIL + ",could not download PPID data set";
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
				Log.LogEntry("Person PID cal server exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				}
			Log.LogEntry("Person PID cal server stopped.");
		}



		public void SpeechHandler(string msg)

		{
			co.msg = msg;
			co.evnt.Set();
		}



		public void AddPPIDGrammar()

		{
			TextReader tr;
			TextWriter tw1;
			string rfname,wfname1,line;

			rfname = Application.StartupPath + SharedData.CAL_SUB_DIR + "basecommands.txt";
			wfname1 = Application.StartupPath + SharedData.CAL_SUB_DIR + GRAMMAR + ".xml";
			if (File.Exists(rfname))
				{
				tr = File.OpenText(rfname);
				tw1 = File.CreateText(wfname1);
				while ((line = tr.ReadLine()) != null)
					{
					if (line.Equals("</grammar>"))
						{
						tw1.WriteLine("  <rule id=\"rootRule\">");
						tw1.WriteLine("    <one-of>");
						tw1.WriteLine("       <item>" + RUN + "</item>");
						tw1.WriteLine("       <item>" + DONE + "</item>");
						tw1.WriteLine("       <item>" + AGAIN + "</item>");
						tw1.WriteLine("    </one-of>");
						tw1.WriteLine("  </rule>");
						tw1.WriteLine();
						tw1.WriteLine(line);
						break;
						}
					else
						tw1.WriteLine(line);
					}
				tr.Close();
				tw1.Close();
				Speech.AddGrammerHandler(wfname1);
				Speech.RegisterHandler(GRAMMAR,SpeechHandler,null);
				Speech.EnableCommand(GRAMMAR);
				}
			else
				Log.LogEntry("AddPPIDGrammar: could not add grammar.");
		}

		}
	}
