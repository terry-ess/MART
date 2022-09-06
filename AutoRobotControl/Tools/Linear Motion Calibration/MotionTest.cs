using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using AutoRobotControl;
using Constants;

namespace Linear_Motion_Calibration
	{

	class MotionTest
		{
		private const int TOP_SPEED = 6;

		private static UiConnection connect = null;
		private static UiConnection status_feed = null;
		private static IPEndPoint st_recvr = null;
		private static Thread rexec;
		private static bool test_run = false;
		private static int no_cycles;
		private static int move_dist;
		private static bool wheel_align;
		private static bool slow;
		private static Point start_pos;


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
				if (msg.StartsWith(UiConstants.RUN_MT))
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
							move_dist = int.Parse(values[2]);
							wheel_align = bool.Parse(values[3]);
							slow = bool.Parse(values[4]);
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
						rsp = UiConstants.FAIL + ",test already running";
					}
				else if (msg == UiConstants.STOP_MT)
					{
					test_run = false;
					rsp = UiConstants.OK;
					}
				else
					rsp = UiConstants.FAIL + ",unknown command";
				}
			else
				rsp = UiConstants.FAIL + ",not initialized";
			return (rsp);
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



		private static void TestRunExec()

		{
			int cycles_run = 0;
			string command;
			int timeout;
			double dist,volts;
			string rtn;
			CommonLib.scan_analysis sa1 = new CommonLib.scan_analysis(), sa2 = new CommonLib.scan_analysis();

			try
			{
			SendStatusMessage("Starting test runs");
			timeout = (int) ((double) (move_dist/TOP_SPEED) * 1200);
			while (test_run && (cycles_run < no_cycles))
				{
				if (wheel_align)
					{
					Thread.Sleep(250);
					if (!CommonLib.LinearMove(SharedData.FORWARD + " 12", 33000))
						{
						SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Wheel alignment forward move error: " + CommonLib.LastError());
						break;
						}
					}
				Thread.Sleep(1000);
				if (!CommonLib.TurnToTarget())
					{
					SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Turn to target error: " + CommonLib.LastError());
					break;
					}
				Thread.Sleep(CommonLib.MECH_DELAY);
				if (!CommonLib.GetPostion(CommonLib.rchg, ref sa1))
					{
					SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Could not determine current location.");
					break;
					}
				volts = AutoRobotControl.MotionControl.GetVoltage();
				if (slow)
					command = "TF SLOW ";
				else
					command = "TF ";
				command += move_dist;
				rtn = SendCommand(command,timeout);
				if (rtn.Contains("fail"))
					{
					SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Forward move error: " + rtn);
					break;
					}
				Thread.Sleep(CommonLib.MECH_DELAY);
				if (!CommonLib.GetPostion(CommonLib.rchg, ref sa2))
					{
					SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Could not determine current location.");
					break;
					}
				else
					{
					dist = Math.Sqrt(Math.Pow(sa1.cdbw - sa2.cdbw,2) + Math.Pow(sa1.cdlw - sa2.cdlw,2));
					SendStatusMessage((cycles_run + 1) + "," + volts.ToString("F2") + "," + dist.ToString("F4"));
					SendStatusMessage("Measurements captured.");
					}
				if (!CommonLib.BackwardMoveToPoint(start_pos))
					{
					SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Could not return to my start position.");
					break;
					}
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
