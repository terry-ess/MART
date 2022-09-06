using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace AutoRobotControl
	{
	static class Supervisor
		{

		private static AutoResetEvent poll_event = new AutoResetEvent(false);
		private static Thread poll_thread = null;
		private static bool poll_read = true;


		public static void Start()

		{
			Thread su;
			string dir;

			try
			{
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptHandler);
			SharedData.app_time.Start();
			dir = Application.StartupPath + SharedData.DATA_SUB_DIR;
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			Log.OpenLog("Operation log " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second + SharedData.TEXT_TILE_EXT, true);
			dir = Application.StartupPath + SharedData.CAL_SUB_DIR;
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			su = new Thread(StartUpThread);
			dir = Application.StartupPath + SharedData.TOOLS_SUB_DIR;
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			UiCom.Start();
			su.Start();
			}

			catch(Exception ex)
			{
			Log.LogEntry("Supervisor detected exception: " + ex.Message);
			}
		}



		public static void Closing(bool shutdown)

		{
			if ((poll_thread != null) && poll_thread.IsAlive)
				{
				poll_event.Set();
				poll_thread.Join();
				}
			HeadAssembly.Close();
			MotionControl.Close(shutdown);
			Kinect.Close();
			Rplidar.Close();
			Navigate.Close();
			Lidar.Close();
			Speech.StopSpeechRecognition();
			VisualObjectDetection.Close();
			SpeechDirection.Close();
			CloseRelays();
		}



		public static void Stop(bool shutdown)

		{
			SharedData.status = SharedData.RobotStatus.SHUTTING_DOWN;
			Thread.Sleep(1000);
			Closing(shutdown);
			if (Tools.ToolInProgress())
				Tools.CloseTool(true);
			UiCom.Stop();
			if (SharedData.log_operations)
				{
				Log.LogEntry("Main battery voltage: " + SharedData.main_battery_volts);
				Log.CloseLog();
				}
//			if (shutdown)
//				Process.Start("shutdown.bat"); // command file that issues system shutdown ("shutdown /s /t 5")
			Program.appl_exit.Set();
		}



		public static void Restart()

		{
			Thread rs_thread;

			rs_thread = new Thread(ReStartThread);
			rs_thread.Start();
		}



		private static void UnhandledExceptHandler(object sender,UnhandledExceptionEventArgs args)

		{
			Exception ex = (Exception) args.ExceptionObject;
			OutputSpeech("Unhandled exception occured, MART will shutdown.");
			Log.LogEntry("Unhandled exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			SharedData.status = SharedData.RobotStatus.SHUTTING_DOWN;

			try
			{
			MotionControl.SendStopCommand("SL");
			MotionControl.SendStopCommand("SS");
			MotionControl.Close(true);
			}

			catch (Exception)
			{ }

			try
			{
			SpeechDirection.Close();
			}

			catch (Exception)
			{ }

			try
			{
			VisualObjectDetection.Close();
			}

			catch (Exception)
			{ }

			try
			{
			CloseRelays();
			}

			catch(Exception)
			{}

			Thread.Sleep(100);

			try
			{
			UiCom.Stop();
			}

			catch(Exception) 
			{}


			Program.appl_exit.Set();
		}


		
		private static void OutputSpeech(string output)

		{
			Speech.Speak(output);
			Log.LogEntry(output);
		}



		private static void StartUpThread()

		{
			SharedData.status = SharedData.RobotStatus.STARTUP;
			StartUp();
			Skills.InitSkills();
			if (!SharedData.log_operations)
				{
				Log.CloseLog();
				}
		}



		private static void ReStartThread()

		{
			OutputSpeech("Running restart.  Closing key sub-systems.");
			SharedData.status = SharedData.RobotStatus.NONE;
			Closing(false);
			StartUp();
		}



		private static bool OpenRelays()

		{
			bool rtn = false;

			if (Relays.RCRelay(false))
				{
				if (Relays.HARelay(false))
					{
					Thread.Sleep(1000);
					if (Relays.SSRelay(false))
						{
						rtn = true;
						}
					}
				}
			return (rtn);
		}



		private static void CloseRelays()

		{
			Relays.Relay(true);
			Relays.Close();
		}



		public static void HwDiagnostics()

		{
			string rsp = "";

			try
			{
			OutputSpeech("Checking head assembly.");
			if ((SharedData.head_assembly_operational = HeadAssembly.Operational()))
				OutputSpeech("Head assembly is operational.");
			else
				OutputSpeech("Head assembly is not operational.");
			OutputSpeech("Checking connect.");
			if ((SharedData.kinect_operational = Kinect.Operational()))
				OutputSpeech("Connect is operational.");
			else
				OutputSpeech("Connect is not operational.");
			OutputSpeech("Checking LIDAR.");
			if ((SharedData.front_lidar_operational = Rplidar.Operational()))
				OutputSpeech("Front LIDAR is operational.");
			else
				OutputSpeech("Front LIDAR is not operational.");
			if (SharedData.rear_lidar_operational)
				OutputSpeech("Rear LIDAR is operational.");
			else
				OutputSpeech("Rear LIDAR is not operational.");
			OutputSpeech("Checking robotic arm.");
			Arm.OpenServoController();
			if (SharedData.arm_operational)
				OutputSpeech("Robotic arm is operational.");
			else
				OutputSpeech("Robotic arm is not operational.");
			Arm.CloseServoController();   //only want arm powered when its in use to minimize power drain
			OutputSpeech("Checking motion controller.");
			if ((SharedData.motion_controller_operational = MotionControl.Operational(ref rsp)))
				OutputSpeech("Motion controller is operational.");
			else
				OutputSpeech("Motion controller is not operational. " + rsp);
			}

			catch(Exception ex)
			{
			OutputSpeech("Hardware diagnostics exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}
		}



		private static void StartUp()

		{
			int no_op_ss = 0;
			string rsp = "";

			try
			{
			OutputSpeech("Starting key sub-systems.");
			OutputSpeech("Closing relays");
			if (Relays.Open())
				{
				Thread.Sleep(100);
				if (OpenRelays())
					{
					OutputSpeech("Checking head assembly.");
					HeadAssembly.Open();
					if ((SharedData.head_assembly_operational = HeadAssembly.Operational()))
						{
						OutputSpeech("Head assembly is operational.");
						no_op_ss += 1;
						}
					else
						{
						OutputSpeech("Head assembly is not operational.");
						}
					OutputSpeech("Checking connect.");
					Kinect.Open();
					if ((SharedData.kinect_operational = Kinect.Operational()))
						{
						OutputSpeech("Connect is operational.");
						no_op_ss += 1;
						}
					else
						{
						OutputSpeech("Connect is not operational.");
						}
					OutputSpeech("Checking LIDAR.");
					Lidar.Open();
					if ((SharedData.front_lidar_operational = Rplidar.Operational()))
						{
						OutputSpeech("Front LIDAR is operational.");
						no_op_ss += 1;
						}
					else
						{
						OutputSpeech("Front LIDAR is not operational.");
						}
					if (SharedData.rear_lidar_operational)
						OutputSpeech("Rear LIDAR is operational.");
					else
						OutputSpeech("Rear LIDAR is not operational.");
					OutputSpeech("Checking robotic arm.");
					Arm.OpenServoController();
					if (SharedData.arm_operational)
						OutputSpeech("Robotic arm is operational.");
					else
						OutputSpeech("Robotic arm is not operationl.");
					Arm.CloseServoController();	//only want arm powered when its in use to minimize power drain
					OutputSpeech("Checking navigation data base.");
					if ((SharedData.navdata_operational = NavData.Connected()))
						OutputSpeech("Navigation data base is connected.");
					else
						OutputSpeech("Navigation data base is not connected.");
					OutputSpeech("Initializing visual object detection.");
					if (VisualObjectDetection.Open())
						OutputSpeech("Visual object detection is operational");
					else
						OutputSpeech("Visual object detection is not operational");
					OutputSpeech("Checking motion controller.");
					MotionControl.Open();
					if ((SharedData.motion_controller_operational = MotionControl.Operational(ref rsp)))
						{
						OutputSpeech("Motion controller is operational.");
						no_op_ss += 1;
						poll_read = true;
						poll_thread = new Thread(PollThread);
						poll_thread.Start();
						}
					else
						{
						OutputSpeech("Motion controller is not operational. " + rsp);
						if ((poll_thread != null) && poll_thread.IsAlive)
							{
							poll_event.Set();
							poll_thread.Join();
							}
						}
					SmartMotionCommands.AddSmartMotionGrammar();
					Speech.StartSpeechCommandHandlers();
					if (!Speech.StartSpeechRecognition())
						{
						OutputSpeech("Speech recognition failed initialization. I am not operational.");
						SharedData.status = SharedData.RobotStatus.LIMITED_RUNNING;
						}
					else if (SharedData.navdata_operational)
						{
						OutputSpeech("Starting speech direction service.");
						if (SpeechDirection.Open())
							OutputSpeech("Speech direction service is operational");
						else
							OutputSpeech("Speech direction service is not operational");
						OutputSpeech("Determining current location.");
						if (!Navigate.Open())
							{
							OutputSpeech("Can not determine my current location.");
							if (no_op_ss < 4)
								SharedData.status = SharedData.RobotStatus.LIMITED_RUNNING;
							else
								SharedData.status = SharedData.RobotStatus.NORMAL_RUNNING;
							}
						else if (no_op_ss < 4)
							{
							OutputSpeech("I have only limited operational capabilities.");
							SharedData.status = SharedData.RobotStatus.LIMITED_RUNNING;
							}
						else
							{
							OutputSpeech("I am fully operational.");
							SharedData.status = SharedData.RobotStatus.NORMAL_RUNNING;
							}
						}
					else
						{
						OutputSpeech("I have only limited operational capabilities.");
						SharedData.status = SharedData.RobotStatus.LIMITED_RUNNING;
						}
					}
				else
					{
					OutputSpeech("Could not close relays. I am not operational.");
					SharedData.status = SharedData.RobotStatus.LIMITED_RUNNING;
					}
				}
			else
				{
				OutputSpeech("Could not open connection with relay controler. I am not operational.");
				SharedData.status = SharedData.RobotStatus.LIMITED_RUNNING;
				Relays.Close();
				}
			}

			catch(Exception ex)
			{
			SharedData.status = SharedData.RobotStatus.LIMITED_RUNNING;
			OutputSpeech("Startup experienced an exception.  I am exiting.");
			Log.LogEntry("Startup exception: " + ex.Message);
			Log.LogEntry("Prior exception: " + ex.InnerException.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			Stop(false);
			}
		}



		public static void SetPollRead(bool val)

		{
			poll_read = val;
		}



		private static void PollThread()

		{
			double volts;

			while (true)
				{
				if (poll_read)
					{

					try
					{
					volts = MotionControl.GetVoltage(false);
					if (volts == -1)
						OutputSpeech("Could not determine voltage.");
					else if (volts <= SharedData.MIN_BATTERY_VOLTAGE)
						OutputSpeech("Main battery voltage is only " + volts + " volts");
					SharedData.main_battery_volts =  volts;
					}

					catch(Exception ex)
					{
					Log.LogEntry("PollThread exception: " + ex.Message);
					Log.LogEntry("Stack trace: " + ex.StackTrace);
					}

					}
				if (poll_event.WaitOne(60000))
					break;
				}
		}


		}
	}
