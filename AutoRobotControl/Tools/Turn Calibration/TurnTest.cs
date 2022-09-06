using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using AutoRobotControl;
using Constants;

namespace Turn_Calibration
	{
	class TurnTest
		{

		private static UiConnection connect = null;
		private static UiConnection status_feed = null;
		private static IPEndPoint st_recvr = null;
		private static Thread rexec;
		private static bool test_run = false;
		private static int no_cycles;
		private static int turn_angle;
		private static Point start_pos;
		private static string last_ds_file = "";


		public static bool Open(UiConnection con)

		{
			bool rtn = false;
			NavData.room_pt sp = new NavData.room_pt();

			if (SharedData.motion_controller_operational && SharedData.front_lidar_operational && SharedData.kinect_operational && SharedData.navdata_operational && NavData.GetCurrentRoomPoint(CommonLib.START_POINT, ref sp) && CommonLib.Open())
				{
				status_feed = new UiConnection(UiConstants.TOOL_FEED_PORT_NO);
				if (status_feed.Connected())
					{
					start_pos = sp.coord;
					connect = con;
					rtn = true;
					}
				}
			return(rtn);
		}



		public static void Close()

		{
			if (connect != null)
				{
				status_feed.Close();
				connect = null;
				}
		}



		public static string MessageHandler(string msg, IPEndPoint ep)

		{
			string rsp = "";
			string[] values;


			if (connect != null)
				{
				if (msg.StartsWith(UiConstants.RUN_TT))
					{
					if (!test_run)
						{
						values = msg.Split(',');
						if (values.Length == 5)
							{
							st_recvr = new IPEndPoint(ep.Address,UiConstants.TOOL_FEED_PORT_NO);

							try
							{
							no_cycles = int.Parse(values[1]);
							turn_angle = int.Parse(values[2]);
							test_run = true;
							rexec = new Thread(TestRunExec);
							rexec.Start();
							rsp = UiConstants.OK;
							}

							catch(Exception)
							{
							rsp = UiConstants.FAIL + ",bad parameter";
							}

							}
						else
							rsp = UiConstants.FAIL + ", bad format";
						}
					else
						rsp = UiConstants.FAIL + ",cal already running";
					}
				else if (msg == UiConstants.STOP_TT)
					{
					test_run = false;
					rsp = UiConstants.OK;
					}
				else if (msg == UiConstants.SEND_LAST_DS)
					{
					string cmd;
					StreamReader sr;
					MemoryStream ms = new MemoryStream();

					if (last_ds_file.Length > 0)
						{
						connect.SendResponse(UiConstants.OK,ep);
						sr = new StreamReader(last_ds_file);
						sr.BaseStream.CopyTo(ms);
						cmd = UiConstants.LAST_SENSOR_DS + "," + ms.Length;
						rsp = connect.SendCommand(cmd, 20, ep);
						if (rsp.StartsWith(UiConstants.OK))
							connect.SendStream(ms,ep);
						rsp = "";
						}
					else
						rsp = UiConstants.FAIL + ",could not download turn data set";
					}
				else
					rsp = UiConstants.FAIL + ",unknown command";
				}
			else
				rsp = UiConstants.FAIL + ",not initialized";
			return (rsp);
		}



		private static string DownloadLastSpinFile()

		{
			string fname = "";
			
			AutoRobotControl.MotionControl.DownloadLastTurnFile(ref fname);
			return(fname);
		}



		private static bool AnalyzeMotionData(string fname,ref int accel, ref int speed, ref double stop_offset, ref double calc_angle, ref double volts)

		{
			bool rtn = false;
			StreamReader tr;
			string line;
			string vmatch = "Voltage: ";
			string amatch = "Accel: ";
			string msmatch = "Max speed: ";
			string camatch = "Calculated motion (°): ";
			string smatch = "Stop offset (°): ";

			if (fname.Length > 0)
				{
				tr = File.OpenText(fname);
				if (tr != null)
					{
					while ((line = tr.ReadLine()) != null)
						{
						if (line.StartsWith(vmatch))
							volts = double.Parse(line.Substring(vmatch.Length));
						else if (line.StartsWith(amatch))
							accel = int.Parse(line.Substring(amatch.Length));
						else if (line.StartsWith(msmatch))
							speed = int.Parse(line.Substring(msmatch.Length));
						else if (line.StartsWith(camatch))
							calc_angle = double.Parse(line.Substring(camatch.Length));
						else if (line.StartsWith(smatch))
							stop_offset = double.Parse(line.Substring(smatch.Length));
						}
					tr.Close();
					}
				else
					Log.LogEntry("Could not open turn file " + fname);
				}
			else
				Log.LogEntry("No turn file " + fname +  " available.");
			return(rtn);
		}



		private static void SendStatusMessage(string msg)

		{
			if (st_recvr != null)
				status_feed.SendResponse(UiConstants.CAL_STATUS + "," + msg,st_recvr,true);
		}



		private static string SendCommand(string command,int timeout)

		{
			string rtn = "";

			rtn = AutoRobotControl.MotionControl.SendCommand(command,timeout/10);
			Log.LogEntry(command + "," + rtn);
			return(rtn);
		}



		private static bool MakeTurn(string cmd,int cycles)

		{
			bool rtn = false;
			double iangle = 0, fangle = 0, tangle;
			int timeout;
			double volts = -1,calc_angle = 0,stop_offset = 0;
			int accel = 0,speed = 0;
			string rsp,fname;

			if (!CommonLib.GetAngle(ref iangle))
				{
				SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Could not determine current angle " + CommonLib.LastError());
				rtn = false;
				}
			else
				{
				timeout = (int) Math.Ceiling((((double) turn_angle/20) * 1000) + 450);
				rsp = SendCommand(cmd,timeout);
				if (rsp.Contains("fail"))
					SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Turn error: " + rtn);
				else
					{
					fname = DownloadLastSpinFile();
					last_ds_file = fname;
					Thread.Sleep(CommonLib.MECH_DELAY);
					if (!CommonLib.GetAngle(ref fangle))
						SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Could not determine current angle " + CommonLib.LastError());
					else
						{
						tangle = Math.Abs(fangle - iangle);
						if (AnalyzeMotionData(fname, ref accel, ref speed,ref stop_offset,ref calc_angle,ref volts))
							{
							SendStatusMessage(cycles + "," + cmd.Substring(1,1) + "," + volts.ToString("F2") + "," + accel + "," + speed + "," + stop_offset.ToString("F3") + "," + calc_angle.ToString("F2") + "," + tangle.ToString("F2"));
							SendStatusMessage("Measurements captured.");
							rtn = true;
							}
						else
							SendStatusMessage("Could not analyze motion data.");
						}
					}
				}
			return(rtn);
		}



		private static void TestRunExec()

		{
			int cycles_run = 0;
			string command;
			int i;
			bool turn_made = false;

			try
			{
			SendStatusMessage("Starting test runs");
			while (test_run && (cycles_run < no_cycles))
				{
				if (!CommonLib.TurnToTarget())
					{
					SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Turn to target error: " + CommonLib.LastError());
					break;
					}
				Thread.Sleep(CommonLib.MECH_DELAY);
				for (i = 0;i < 2;i++)
					{
					if (i == 0)
						command = "MCR";
					else
						command = "MCL";
					turn_made = MakeTurn(command,cycles_run + 1);
					if (!turn_made)
						break;
					}
				if (!turn_made)
					break;
				cycles_run += 1;
				SendStatusMessage("Completed test run " + cycles_run);
				}
			SendStatusMessage(UiConstants.CAL_RUN_COMPLETED);
			}

			catch(Exception ex)
			{
			SendStatusMessage("Exception " + ex.Message);
			Log.LogEntry("Exception " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			test_run = false;
		}

		}
	}
