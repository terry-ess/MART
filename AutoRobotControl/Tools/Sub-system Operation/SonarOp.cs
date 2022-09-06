using System;
using System.IO;
using System.Net;
using System.Threading;
using AutoRobotControl;
using Constants;


namespace Sub_system_Operation
	{
	static class SonarOp
		{

		private static UiConnection connect = null,status_feed;
		private static IPEndPoint data_recvr;
		private static string fname = "";
		private static Thread rcthread = null;


		public static bool Open(UiConnection con)

		{
			bool rtn = false;

			if (AutoRobotControl.MotionControl.Connected())
				{
				status_feed = new UiConnection(UiConstants.TOOL_FEED_PORT_NO);
				if (status_feed.Connected())
					connect = con;
				rcthread = null;
				}
			return (rtn);
		}



		public static void Close()

		{
			if ((rcthread != null) && (rcthread.IsAlive))
				{
				rcthread.Abort();
				AutoRobotControl.MotionControl.StopRecordSonar();
				}
			connect = null;
		}



		private static void SendStatusMessage(string msg)

		{
			if (data_recvr != null)
				status_feed.SendResponse(msg,data_recvr,true);
		}



		private static void RecordThread(object record_time)

		{
			try 
			{
			Thread.Sleep(((int) record_time * 60000) + 1000);
			AutoRobotControl.MotionControl.DownloadLastSonarRecordFile(ref fname);
			SendStatusMessage(UiConstants.SONAR_DATA_READY);
			}

			catch(Exception)
			{
			}

			rcthread = null;

		}



		public static string MessageHandler(string msg, IPEndPoint ep)

		{
			string rsp = "";
			string[] values;
			int record_time;
			SharedData.RobotLocation loc;

			if (connect != null)
				{
				if (msg.StartsWith(UiConstants.RECORD_FRONT_SONAR) || msg.StartsWith(UiConstants.RECORD_REAR_SONAR))
					{
					values = msg.Split(',');
					if (values.Length == 2)
						{
						data_recvr = new IPEndPoint(ep.Address, UiConstants.TOOL_FEED_PORT_NO);
						if (msg.StartsWith(UiConstants.RECORD_FRONT_SONAR))
							loc = SharedData.RobotLocation.FRONT;
						else
							loc = SharedData.RobotLocation.REAR;

						try
						{
						record_time = int.Parse(values[1]);
						AutoRobotControl.MotionControl.RecordSonar(loc,record_time);
						rcthread = new Thread(RecordThread);
						rcthread.Start(record_time);
						rsp = UiConstants.OK;
						fname = "";
						}

						catch(Exception)
						{
						rsp = UiConstants.FAIL + ",bad format";
						}

						}
					}
				else if (msg == UiConstants.STOP_RECORD)
					{
					if ((rcthread != null) && (rcthread.IsAlive))
						{
						rcthread.Abort();
						AutoRobotControl.MotionControl.StopRecordSonar();
						}
					rsp = UiConstants.OK;
					}
				else if (msg == UiConstants.SEND_LAST_DS)
					{
					if (fname.Length > 0)
						{
						string cmd;
						StreamReader sr;
						MemoryStream ms = new MemoryStream();

						connect.SendResponse(UiConstants.OK, ep);
						sr = new StreamReader(fname);
						sr.BaseStream.CopyTo(ms);
						cmd = UiConstants.LAST_SENSOR_DS + "," + ms.Length;
						rsp = connect.SendCommand(cmd, 20, ep);
						if (rsp.StartsWith(UiConstants.OK))
							connect.SendStream(ms, ep);
						rsp = "";
						}
					else
						rsp = UiConstants.FAIL + ",no data set available";
					}
				else
					rsp = UiConstants.FAIL + ",unknown command";
				}
			else
				rsp = UiConstants.FAIL + ",not initialized";
			return (rsp);
		}

		}
	}
