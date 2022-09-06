using System;
using System.Diagnostics;
using System.Windows.Forms;


namespace AutoRobotControl
	{
	class ProcessCommands : CommandHandlerInterface
		{

		private const string GRAMMAR = "processcommands";
		private const string TOOL_BOX_PROCESS = "..\\MotionDataCollection\\MotionDataCollection.exe";
		private const string MC_INTERFACE_PROCESS = "..\\MCInterface\\MCInterface.exe";


		public ProcessCommands()

		{
			RegisterCommandSpeech();
		}



		private void OutputSpeech(string output)

		{
			Speech.Speak(output);
			Log.LogEntry(output);
		}



		private void SSStartup()

		{
			string rsp = "";

			OutputSpeech("Restarting key sub-systems.");
			OutputSpeech("Checking head assembly.");
			HeadAssembly.Open();
			if ((SharedData.head_assembly_operational = HeadAssembly.Operational()))
				OutputSpeech("Head assembly is operational.");
			else
				OutputSpeech("Head assembly is not operational.");
			OutputSpeech("Checking connect.");
			Kinect.Open();
			if ((SharedData.kinect_operational = Kinect.Operational()))
				OutputSpeech("Connect is operational.");
			else
				OutputSpeech("Connect is not operational.");
			OutputSpeech("Checking motion controller.");
			MotionControl.Open();
			if ((SharedData.motion_controller_operational = MotionControl.Operational(ref rsp)))
				OutputSpeech("Motion controller is operational.");
			else
				OutputSpeech("Motion controller is not operational.");
			OutputSpeech("Checking LIDAR.");
			Rplidar.Open();
			if ((SharedData.front_lidar_operational = Rplidar.Operational()))
				OutputSpeech("LIDAR is operational.");
			else
				OutputSpeech("LIDAR is not operational.");
			if (!Speech.StartSpeechRecognition())
				OutputSpeech("Speech recognition failed initialization. I am not operational.");
		}



		private void SSShutdown()

		{
			HeadAssembly.Close();
			MotionControl.Close(false);
			Speech.StopSpeechRecognition();
			Kinect.Close();
			Rplidar.Close();
		}


		public void SpeechHandler(string msg)

		{
			Process proc;
			Form tform;

			Speech.DisableAllCommands();

			try
			{
			if (msg == "tool box")
				{
				OutputSpeech ("ok");
				SSShutdown();
				tform = Application.OpenForms["MainForm"];
				tform.WindowState = FormWindowState.Minimized;
				proc = Process.Start(TOOL_BOX_PROCESS);
				if (proc != null)
					{
					proc.WaitForExit();
					}
				else
					OutputSpeech("Could not start tool box.");
				tform.WindowState = FormWindowState.Normal;
				SSStartup();
				}
			else if (msg == "mc interface")
				{
				OutputSpeech ("ok");
				tform = Application.OpenForms["MainForm"];
				tform.WindowState = FormWindowState.Minimized;
				proc = Process.Start(MC_INTERFACE_PROCESS);
				if (proc != null)
					{
					proc.WaitForExit();
					}
				else
					OutputSpeech("Could not start MC interface.");
				tform.WindowState = FormWindowState.Normal;
				}
			}

			catch(Exception ex)
			{
			OutputSpeech("Process command exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			Speech.EnableAllCommands();
		}



		public void RegisterCommandSpeech()

		{
			Speech.RegisterHandler(GRAMMAR,SpeechHandler,null);
		}

		}
	}
