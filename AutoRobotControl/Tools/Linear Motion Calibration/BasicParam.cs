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
	static class BasicParam
		{

		private static UiConnection connect = null;
		private static UiConnection status_feed = null;
		private static IPEndPoint st_recvr = null;
		private static Thread rexec;
		private static bool cal_run = false;
		private static int no_cycles;
		private static double move_time;
		private static bool wheel_align;
		private static int accel;
		private static int max_speed;
		private static bool pid_gains_set;
		private static double pgain, igain, dgain;
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
				if (msg.StartsWith(UiConstants.RUN_BLM_CAL))
					{
					if (!cal_run)
						{
						values = msg.Split(',');
						if (values.Length == 6)
							{
							st_recvr = new IPEndPoint(ep.Address,UiConstants.TOOL_FEED_PORT_NO);

							try
							{
							no_cycles = int.Parse(values[1]);
							move_time = double.Parse(values[2]);
							wheel_align = bool.Parse(values[3]);
							accel = int.Parse(values[4]);
							max_speed = int.Parse(values[5]);
							pid_gains_set = false;
							cal_run = true;
							rexec = new Thread(CalRunExec);
							rexec.Start();
							rsp = UiConstants.OK;
							}

							catch(Exception)
							{
							rsp = UiConstants.FAIL + ",bad parameter";
							}

							}
						else if (values.Length == 9)
							{
							st_recvr = new IPEndPoint(ep.Address,UiConstants.TOOL_FEED_PORT_NO);

							try
							{
							no_cycles = int.Parse(values[1]);
							move_time = double.Parse(values[2]);
							wheel_align = bool.Parse(values[3]);
							accel = int.Parse(values[4]);
							max_speed = int.Parse(values[5]);
							pgain = double.Parse(values[6]);
							igain = double.Parse(values[7]);
							dgain = double.Parse(values[8]);
							pid_gains_set = true;
							cal_run = true;
							rexec = new Thread(CalRunExec);
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
				else if (msg == UiConstants.STOP_BLM_CAL)
					{
					cal_run = false;
					rsp = UiConstants.OK;
					}
				else
					rsp = UiConstants.FAIL + ",unknown command";
				}
			else
				rsp = UiConstants.FAIL + ",not initialized";
			return (rsp);
		}



		private static string DownloadLastMoveFile()

		{
			string fname = "";
			
			AutoRobotControl.MotionControl.DownloadLastMoveFile(ref fname);
			return(fname);
		}



		private static bool AnalyzeMotionData(string fname,double dist,ref int tt,ref int at,ref int dt,ref double ts,ref double volts,ref int enc)

		{
			bool rtn = false;
			StreamReader tr;
			string line;
			string[] values;
			int start_time = 0,stop_time = 0,stop_issue_time = 0,si_encode = 0;
			double ra;
			bool start_detect = false,stop_issued = false,pulse_rise_detect = false,pos_g = true;
			const int min_no_col = 5;
			const int encode_col = 4;
			const int ra_col = 2;
			const int note_col = 1;
			const int ts_col = 0;
			string vmatch = "Voltage: ";

			if (fname.Length > 0)
				{
				tr = File.OpenText(fname);
				if (tr != null)
					{
					while ((line = tr.ReadLine()) != null)
						{
						if (line.StartsWith(vmatch))
							{
							volts = double.Parse(line.Substring(vmatch.Length));
							}
						else
							{
							values = line.Split(',');
							if ((values != null) && (values.Length >= min_no_col))
								{
								if (!start_detect && !stop_issued && values[note_col].Contains("start detected"))
									{
									start_detect = true;
									start_time = int.Parse(values[ts_col]);
									}
								else if (start_detect)
									{
									if (values[1].Contains("stop issued"))
										{
										stop_issue_time = int.Parse(values[ts_col]);
										stop_time = stop_issue_time;
										si_encode = Math.Abs(int.Parse(values[encode_col]));
										stop_issued  = true;
										start_detect = false;
										}
									}
								else if (stop_issued)
									{
									if (values[note_col].Contains("stop detect"))
										{
										rtn = true;
										break;
										}
									else if (Math.Abs(int.Parse(values[encode_col])) > si_encode)
										{
										stop_time = int.Parse(values[ts_col]);
										si_encode = Math.Abs(int.Parse(values[encode_col]));
										}
									}
								}
							else if (start_detect || stop_issued)
								{
								Log.LogEntry("File format wrong.");
								break;
								}
							}
						}
					if (rtn)
						{
						tt = stop_time - start_time;
						dt = stop_time - stop_issue_time;
						enc = si_encode;
						rtn = false;
						start_detect = false;
						tr.BaseStream.Seek(0,SeekOrigin.Begin);
						while ((line = tr.ReadLine()) != null)
							{
							values = line.Split(',');
							if ((values != null) && (values.Length >= min_no_col))
								{
								if (!start_detect && values[note_col].Contains("start detected"))
									{
									start_detect = true;
									}
								else if (start_detect)
									{
									ra = double.Parse(values[ra_col]);
									if (!pulse_rise_detect && (Math.Abs(ra) > .1))
										{
										pulse_rise_detect = true;
										if (ra > 0)
											pos_g = true;
										else
											pos_g = false;
										}
									if (pulse_rise_detect && ((pos_g && (ra < .01)) || (!pos_g && (ra > -.01))))
										{
										rtn = true;
										at = int.Parse(values[ts_col]) - start_time;
										ts = (dist/12)/(((double) tt - ((at + dt)/2))/1000);
										break;
										}
									}
								}
							}
						}
					tr.Close();
					}
				else
					Log.LogEntry("Could not open move file " + fname);
				}
			else
				Log.LogEntry("No move file " + fname +  " available.");
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



		private static void CalRunExec()

		{
			int cycles_run = 0;
			string command;
			int timeout;
			double dist,volts = -1;
			string rtn;
			ArrayList isdata = new ArrayList(),fsdata = new ArrayList();
			CommonLib.scan_analysis sa1 = new CommonLib.scan_analysis(), sa2 = new CommonLib.scan_analysis();
			string fname;
			int tt = 0,at = 0,dt = 0,enc = 0;
			double ts = 0;

			try
			{
			SendStatusMessage("Starting test runs");
			timeout = (int) Math.Ceiling((move_time * 1000) + 450);
			while (cal_run && (cycles_run < no_cycles))
				{
				isdata.Clear();
				fsdata.Clear();
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
				command = "SLMP " + accel + " " + max_speed;
				rtn = SendCommand(command, timeout);
				if (rtn.Contains("fail"))
					{
					SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",SLMP error: " + rtn);
					break;
					}
				else if (pid_gains_set)
					{
					command = "SLPP " + pgain + " " + igain + " " + dgain;
					rtn = SendCommand(command, timeout);
					if (rtn.Contains("fail"))
						{
						SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",SLPP error: " + rtn);
						break;
						}
					}
				command = "F";
				rtn = SendCommand(command,timeout);
				if (rtn.Contains("fail"))
					{
					SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Forward move error: " + rtn);
					break;
					}
				else
					{
					Thread.Sleep((int) (move_time * 1000));
					rtn = SendCommand("SM",1000);
					}
				if (rtn.StartsWith("ok"))
					{
					fname = DownloadLastMoveFile();
					Thread.Sleep(CommonLib.MECH_DELAY);
					if (!CommonLib.GetPostion(CommonLib.rchg, ref sa2))
						{
						SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Could not determine current location.");
						break;
						}
					else
						{
						dist = Math.Sqrt(Math.Pow(sa1.cdbw - sa2.cdbw,2) + Math.Pow(sa1.cdlw - sa2.cdlw,2));
						if (AnalyzeMotionData(fname, dist, ref tt, ref at, ref dt, ref ts,ref volts,ref enc))
							{
							SendStatusMessage((cycles_run + 1) + "," + volts.ToString("F2") + "," + dist.ToString("F4") + "," + tt + "," + at + "," + dt + "," + ts.ToString("F5") + "," + enc);
							SendStatusMessage("Measurements captured.");
							}
						else
							SendStatusMessage("Could not analyze motion data.");
						}
					}
				else
					{
					SendStatusMessage("Move failed: " + rtn);
					break;
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

			cal_run = false;
		}


		}
	}
