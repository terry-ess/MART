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
	class BaseParam
		{
		private const int ARRAY_SIZE = 10;

		private static UiConnection connect = null;
		private static UiConnection status_feed = null;
		private static IPEndPoint st_recvr = null;
		private static Thread rexec;
		private static bool cal_run = false;
		private static int no_cycles;
		private static double move_time;
		private static int accel;
		private static int max_speed;
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
				if (msg.StartsWith(UiConstants.RUN_BT_CAL))
					{
					if (!cal_run)
						{
						values = msg.Split(',');
						if (values.Length == 5)
							{
							st_recvr = new IPEndPoint(ep.Address,UiConstants.TOOL_FEED_PORT_NO);

							try
							{
							no_cycles = int.Parse(values[1]);
							move_time = double.Parse(values[2]);
							accel = int.Parse(values[3]);
							max_speed = int.Parse(values[4]);
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
				else if (msg == UiConstants.STOP_BT_CAL)
					{
					cal_run = false;
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
						rsp = UiConstants.FAIL + ",could not download trun data set";
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



		private static bool AnalyzeMotionData(string fname,ref double avg_av,ref double max_av,ref int ss_time,ref int stop_time,ref double angle,ref double si_angle,ref double volts)

		{
			bool rtn = false;
			StreamReader tr;
			string line;
			string[] values;
			int ts,last_ts = 0,i,start_ts = 0,si_ts = 0,cnt;
			double av,last_av = 0,tangle = 0,mav = 0,tav = 0,dt;
			bool start_detect = false,stop_issued = false;
			const int min_no_col = 4;
			const int av_col = 2;
			const int note_col = 1;
			const int ts_col = 0;
			string vmatch = "Voltage: ";
			Queue q = new Queue();

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
								if (!start_detect && values[note_col].Contains("start detected"))
									{
									start_detect = true;
									start_ts = int.Parse(values[ts_col]);
									}
								else if (!start_detect && !values[note_col].Contains("Note"))
									last_ts = int.Parse(values[ts_col]);
								if (start_detect)
									{
									av = double.Parse(values[av_col]);
									if (!stop_issued)
										{
										q.Enqueue(av);
										if (q.Count > ARRAY_SIZE)
											q.Dequeue();
										}
									ts = int.Parse(values[ts_col]);
									dt = ((double) ts - last_ts)/1000;
									last_ts = ts;
									tangle += ((av + last_av)/2) * dt;
									last_av = av;
									if (Math.Abs(av) > mav)
										mav = Math.Abs(av);
									if (values[note_col].Contains("stop issued"))
										{
										si_angle = Math.Abs(tangle);
										si_ts = ts;
										ss_time = si_ts - start_ts;
										stop_issued = true;
										}
									if (values[note_col].Contains("stop detect"))
										{
										cnt = q.Count;
										for (i = 0;i < cnt;i++)
											tav += (double) q.Dequeue();
										avg_av = Math.Abs(tav/cnt);
										max_av = mav;
										angle = Math.Abs(tangle);
										stop_time = ts - si_ts;
										rtn = true;
										break;
										}
									}
								}
							}
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
			int timeout,ss_time = 0,stop_time = 0;
			double volts = -1, avg_av = 0, max_av = 0, si_angle = 0, calc_angle = 0;
			string rsp,fname;

			if (!CommonLib.GetAngle(ref iangle))
				{
				SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Could not determine current angle " + CommonLib.LastError());
				rtn = false;
				}
			else
				{
				timeout = (int)Math.Ceiling((move_time * 1000) + 450);
				rsp = SendCommand(cmd,timeout);
				if (rsp.Contains("fail"))
					SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Turn error: " + rtn);
				else
					{
					Thread.Sleep((int) (move_time * 1000));
					rsp = SendCommand("SM",1000);
					if (rsp.StartsWith("ok"))
						{
						fname = DownloadLastSpinFile();
						last_ds_file = fname;
						Thread.Sleep(CommonLib.MECH_DELAY);
						if (!CommonLib.GetAngle(ref fangle))
							SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",Could not determine current angle " + CommonLib.LastError());
						else
							{
							tangle = Math.Abs(fangle - iangle);
							if (AnalyzeMotionData(fname, ref avg_av, ref max_av,ref ss_time,ref stop_time, ref calc_angle, ref si_angle,ref volts))
								{
								SendStatusMessage(cycles + "," + cmd.Substring(0,1) + "," + volts.ToString("F2") + "," + avg_av.ToString("F4") + "," + max_av.ToString("F4") + "," + ss_time + "," + stop_time + "," + si_angle.ToString("F2") + "," + calc_angle.ToString("F2") + "," + tangle.ToString("F2"));
								SendStatusMessage("Measurements captured.");
								rtn = true;
								}
							else
								SendStatusMessage("Could not analyze motion data.");
							}
						}
					else
						{
						SendStatusMessage("Turn failed: " + rtn);
						rtn = false;
						}
					}
				}
			return(rtn);
		}



		private static void CalRunExec()

		{
			int cycles_run = 0;
			string command;
			int i;
			string rtn;
			bool turn_made = false;

			try
			{
			SendStatusMessage("Starting test runs");
			command = "SSP " + accel + " " + max_speed;
			rtn = SendCommand(command,100);
			if (rtn.Contains("fail"))
				{
				SendStatusMessage(UiConstants.CAL_RUN_ABORTED + ",SSP error: " + rtn);
				cal_run = false;
				}
			while (cal_run && (cycles_run < no_cycles))
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
						command = "R CUSTOM";
					else
						command = "L CUSTOM";
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

			cal_run = false;
		}

		}
	}
