using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Renci.SshNet;
using MotionControl;

namespace AutoRobotControl
	{

	public static class MotionControl
		{

		public const string PARAM_FILE = "ctoc.param";

		private const int BBB_BOOT_TIME = 105000;


		private const string USER = "root";
		private const string PASSWORD = "password";  //THIS SHOULD BE ENCRYPTED!!
		private const string RUN_MOTOR_CNTL = "/app/BBBMotionController";
		private const string SHUTDOWN = "poweroff";

		private static bool connected = false;
		private static string last_error = "";
		private static Object com_access_lock = new Object();
		private static SshClient client = null;
		private static string host = "";



		static MotionControl()

		{
			string fname;
			TextReader tr;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				tr.ReadLine();
				tr.ReadLine();
				host = tr.ReadLine();
				tr.Close();
				}

		}



		public static bool Open()

		{
			bool ssh_connect_done = false;
			int tries = 0,et;
			int try_limit = 2;

			if (host.Length > 0)
				{
				et = (int)SharedData.app_time.ElapsedMilliseconds;
				if (et < BBB_BOOT_TIME)
					try_limit += ((int) Math.Ceiling(((double) BBB_BOOT_TIME - et) / 1000));
				client = new SshClient(host, USER, PASSWORD);
				do
					{

					try
					{
					client.Connect();
					ssh_connect_done = true;
					}

					catch (Exception)
					{
					tries += 1;
					if (tries < try_limit)
						{
						Thread.Sleep(1000);
						}
					else
						ssh_connect_done = true;
					}

					}
				while (!ssh_connect_done);
				Log.LogEntry("No ssh connection tries: " + (tries + 1));
				if (client.IsConnected)
					connected = CoreOpen();
				else
					Log.LogEntry("MotionController.Open: Could not connect ssh client.");
				}
			else
				Log.LogEntry("Host not available.");
			return (connected);
		}



		private static bool CoreOpen()

		{
			bool rtn = false;
			string rsp, command;
			DateTime n = DateTime.UtcNow;
			Thread motor_cntl;
			int tries = 0;

			motor_cntl = new Thread(RunMotorCntl);
			motor_cntl.Start();
			Thread.Sleep(2000);
			if (motor_cntl.IsAlive)
				{
				if (!CtoCCom.Connected())
					CtoCCom.Open();
				if (CtoCCom.Connected())
					{
					lock (com_access_lock)
					{
					do
						{
						tries += 1;
						rsp = CtoCCom.SendCommand("HELLO", 500);
						if (!rsp.StartsWith("ok"))
							Thread.Sleep(1000);
						}
					while(!rsp.StartsWith("ok") && (tries < 2));
					Log.LogEntry("No HELLO tries: " + (tries + 1));
					if (rsp.StartsWith("ok"))
						{
						command = "TIME " + n.Month.ToString() + " " + n.Day.ToString() + " " + n.Year.ToString() + " " + n.Hour.ToString() + " " + n.Minute.ToString() + " " + n.Second.ToString() + " ";
						CtoCCom.SendCommand(command, 1000);
						if ((CtoCCom.SendCommand("LOG", 500).StartsWith("fail")))
							Log.LogEntry("Motion controller could not start log.");
						Log.LogEntry("MotionController.CoreOpen: connection opened.");
						rtn = true;
						}
					else
						{
						Log.LogEntry("MotionController.CoreOpen: 'HELLO' failed.");
						last_error = "HELLO failed";
						SharedData.last_error = last_error;
						CtoCCom.Close();
						connected = false;
						}
					}
					}
				else
					Log.LogEntry("MotionController.CoreOpen: could not establish connection with motion controller.");
				}
			else
				Log.LogEntry("MotionConroller.CoreOpen: could not start motion controller.");
			return (rtn);
		}



		private static void RunMotorCntl()

		{
			SshCommand cmd = null;

			try
			{
			cmd = client.RunCommand(RUN_MOTOR_CNTL);
			}

			catch(Exception ex)
			{
			Log.LogEntry("RunMotorCntl exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			if (cmd != null)
				Log.LogEntry("command: " + cmd.CommandText + "  result: " + cmd.Result);
			}
		}



		public static bool RestartMC()

		{
			bool rtn = false;
			string err = "";

			lock(com_access_lock)
			{

			if (CtoCCom.SendCommand("EXIT",500).StartsWith("ok"))
				rtn = true;
			else
				{
				last_error = "Could not stop motion controller";
				Log.LogEntry(last_error);
				SharedData.last_error = last_error;
				}
			}

			if (rtn)
				if ((rtn = CoreOpen()))
					rtn = Operational(ref err);
			return(rtn);
		}



		public static void Close(bool shutdown)

		{
			string rsp;

			lock(com_access_lock)
			{

			if (connected)
				{
				rsp = CtoCCom.SendCommand("EXIT", 500);
				DownloadOperationLog(rsp,true);
				if (shutdown)
					{
					try
					{
					client.RunCommand(SHUTDOWN);
					}

					catch(Exception)
					{ }

					}
				connected = false;
				}
			if (CtoCCom.Connected())
				CtoCCom.Close();
			}
		}



		public static string LastError()

		{
			return(last_error);
		}



		public static bool Connected()

		{
			return(connected);
		}



		public static bool SendStopCommand(string command)
		
		{
			bool rtn = false;

			if (connected)
				rtn = CtoCCom.SendStopCommand(command);
			return(rtn);
		}



		public static string SendCommand(string command,int timeout_count)

		{
			string rtn = "";

			if (connected)
				lock(com_access_lock)
				{
				rtn = CtoCCom.SendCommand(command,timeout_count);
				}
			return(rtn);
		}



		public static string SendCommand(string command)

		{
			string rtn = "";

			if (connected)
				lock(com_access_lock)
				{
				rtn = CtoCCom.SendCommand(command);
				}
			return(rtn);
		}



		public static int GetSonarReading(SharedData.RobotLocation sl,bool log = true)

		{
			int rtn = -1;
			string cmd = "",rsp;

			if (sl == SharedData.RobotLocation.FRONT)
				cmd = "SFC";
			else if (sl == SharedData.RobotLocation.REAR)
				cmd = "SRC";
			if (cmd.Length > 0)
				{
				if (log)
					Log.LogEntry(cmd);
				rsp = SendCommand(cmd,200);
				if (rsp.StartsWith("ok") && (rsp.Length > 3))
					rtn = int.Parse(rsp.Substring(3));
				else
					rtn = -1;
				if (log)
					Log.LogEntry(rsp);
				}
			return(rtn);
		}



		public static bool RecordSonar(SharedData.RobotLocation sl,int record_time,bool log = true)

		{
			bool rtn = false;
			string cmd = "",rsp;

			if (sl == SharedData.RobotLocation.FRONT)
				cmd = "SFR";
			else if (sl == SharedData.RobotLocation.REAR)
				cmd = "SRR";
			cmd += "," + record_time;
			if (cmd.Length > 0)
				{
				if (log)
					Log.LogEntry(cmd);
				rsp = SendCommand(cmd,200);
				if (rsp.StartsWith("ok"))
					rtn = true;
				if (log)
					Log.LogEntry(rsp);
				}
			return(rtn);
		}



		public static void StopRecordSonar(bool log = true)

		{
			string rsp;

			if (log)
				Log.LogEntry("SSR");
			rsp = SendCommand("SSR",100);
			if (log)
				Log.LogEntry(rsp);
		}



		public static double GetVoltage( bool log)

		{
			double rtn = -1;
			string rsp;

			if (log)
				Log.LogEntry("VOLTS");
			rsp = SendCommand("VOLTS", 200);
			if (log)
				Log.LogEntry(rsp);

			try
			{
			if (rsp.StartsWith("ok"))
				rtn = double.Parse(rsp.Substring(3));
			}

			catch(Exception ex)
			{
			Log.LogEntry("GetVoltage exception: " + ex.Message);
			Log.LogEntry("Response: " + rsp);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			rtn = -1;
			}

			return(rtn);
		}



		public static double GetVoltage()

		{
			return(GetVoltage(true));
		}



		public static bool Docked()

		{
			bool rtn = false;
			string rsp;

			Log.LogEntry("DOCKED");
			rsp = SendCommand("DOCKED", 200);
			if (rsp == "ok true")
				rtn = true;
			Log.LogEntry(rsp);
			return(rtn);
		}



		public static string GetMtrEncoder()

		{
			string rtn = "fail";
			string rsp;

			Log.LogEntry("ENC");
			rsp = SendCommand("ENC", 200);
			if (rsp.StartsWith("ok"))
				rtn = rsp.Substring(3);
			Log.LogEntry(rsp);
			return (rtn);
		}



		public static bool ReceiveFile(string fname,int len)

		{
			bool rtn = false;

			Log.LogEntry("Receive " + fname + "  (" + len + ")");
			if (CtoCCom.ReceiveFile(fname,len) == len)
				rtn = true;
			else
				Log.LogEntry("Receive failed.");
			return(rtn);
		}



		public static bool DownloadLastSonarRecordFile(ref string fname)

		{
			string rsp;
			string[] values;
			int flen;
			bool rtn = false;

			lock(com_access_lock)
			{

			rsp = CtoCCom.SendCommand("LAST SONAR RECORD", 200);
			if (rsp.StartsWith("ok"))
				{
				values = rsp.Split(' ');
				if (values.Length == 3)
					{
					flen = int.Parse(values[2]);
					fname = Log.LogDir() + values[1];
					if (ReceiveFile(fname,flen))
						{
						rtn = true;
						Log.LogEntry("Saved " + fname);
						}
					else
						Log.LogEntry("Last sonar record file download failed.");
					}
				else
					Log.LogEntry("Last sonar record file download failed, bad reply data.");
				}
			else
				Log.LogEntry("Last somar record file download " + rsp);
			}
			return(rtn);
		}




		public static bool DownloadLastMoveFile(ref string fname)

		{
			string rsp;
			string[] values;
			int flen;
			bool rtn = false;

			lock(com_access_lock)
			{

			rsp = CtoCCom.SendCommand("LAST MOVE", 200);
			if (rsp.StartsWith("ok"))
				{
				values = rsp.Split(' ');
				if (values.Length == 3)
					{
					flen = int.Parse(values[2]);
					fname = Log.LogDir() + values[1];
					if (ReceiveFile(fname,flen))
						{
						rtn = true;
						Log.LogEntry("Saved " + fname);
						}
					else
						Log.LogEntry("Last move file download failed.");
					}
				else
					Log.LogEntry("Last move file download failed, bad reply data.");
				}
			else
				Log.LogEntry("Last move file download " + rsp);

			}
			return(rtn);
		}



		public static bool DownloadLastIMURecordFile(ref string fname)

		{
			string rsp;
			string[] values;
			int flen;
			bool rtn = false;

			lock(com_access_lock)
			{

			rsp = CtoCCom.SendCommand("LAST IMU RCD", 200);
			if (rsp.StartsWith("ok"))
				{
				values = rsp.Split(' ');
				if (values.Length == 3)
					{
					flen = int.Parse(values[2]);
					fname = Log.LogDir() + values[1];
					if (ReceiveFile(fname,flen))
						{
						rtn = true;
						Log.LogEntry("Saved " + fname);
						}
					else
						Log.LogEntry("Last IMU record file download failed.");
					}
				else
					Log.LogEntry("Last IMU record file download failed, bad reply data.");
				}
			else
				Log.LogEntry("Last IMU record file download " + rsp);

			}
			return(rtn);
		}



		public static bool DownloadLastTurnFile(ref string fname)

		{
			string rsp;
			string[] values;
			int flen;
			bool rtn = false;

			lock(com_access_lock)
			{

			rsp = CtoCCom.SendCommand("LAST TURN", 200);
			if (rsp.StartsWith("ok"))
				{
				values = rsp.Split(' ');
				if (values.Length == 3)
					{
					flen = int.Parse(values[2]);
					fname = Log.LogDir() + values[1];
					if (ReceiveFile(fname,flen))
						{
						rtn = true;
						Log.LogEntry("Saved " + fname);
						}
					else
						Log.LogEntry("Last turn file download failed.");
					}
				else
					Log.LogEntry("Last turn file download failed, bad reply data.");
				}
			else
				Log.LogEntry("Last turn file download " + rsp);

			}
			return(rtn);
		}



		public static void DownloadOperationLog(string rsp,bool MC)

		{
			string fname;
			string[] values;
			int flen;

			if (rsp.StartsWith("ok"))
				{
				values = rsp.Split(' ');
				if (values.Length == 3)
					{
					flen = int.Parse(values[2]);
					fname = Log.LogDir() + values[1];
					if (ReceiveFile(fname,flen))
						Log.LogEntry("Saved " + fname);
					else
						Log.LogEntry("Operation log download failed.");
					}
				else
					Log.LogEntry("No operation log to download.");
				}
			else
				Log.LogEntry("Operation log download response: " + rsp);
		}



		public static bool Operational(ref string error)

		{
			bool rtn = false;
			string err= "";

			if (Connected())
				if (SendCommand("HELLO", 200).StartsWith("ok"))
					if (SendCommand("SB",500).StartsWith("ok"))
						if (GetSonarReading(SharedData.RobotLocation.FRONT) >= SharedData.FRONT_SONAR_RCMIN_CLEARANCE)
							if (GetSonarReading(SharedData.RobotLocation.REAR) >= SharedData.REAR_SONAR_CLEARANCE)
								if (GetVoltage() != -1)
									if (GetMtrEncoder() != "fail")
										rtn = true;
									else
										err = "Could not obtain motor encoder reading.";
								else
									err = "Could not determine voltage.";
							else
								err = "Did not get valid rear sonar reading.";
						else
							err = "Did not get valid front sonar reading.";
					else
						err = "Setting sensor bias failed.";
				else
					err = "Hello failed.";
			else
				err = "Motion controller not connected.";
			if (err.Length > 0)
				{
				Log.LogEntry(err);
				if (error != null)
					error = err;
				}
			return (rtn);
		}


		}
	}
