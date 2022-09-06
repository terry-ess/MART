using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;


namespace AutoRobotControl
	{
	public static class Tools
		{

		public const string TOOL_TYPE_NAME = ".Tool";

		private static ToolsInterface ti = null;
		private static bool open_failed = false;
		private static SharedData.RobotStatus start_stat;


		public static void OpenTool(string file_name, string type_name, params object[] obj)

		{
			open_failed = true;
			Log.LogEntry("OpenTool " + file_name + " " + type_name + " " + obj.Length);
			if ((ti == null) && (File.Exists(file_name)))
				{

				try
				{
				Assembly DLL = Assembly.LoadFrom(file_name);
				Type ctype = DLL.GetType(type_name);
				dynamic c = Activator.CreateInstance(ctype);
				ti = (ToolsInterface) c.Open();
				if (!ti.Open(obj))
					{
					ti.Close();
					OutputSpeech("Tool open failed.");
					ti = null;
					}
				else
					{
					Speech.DisableAllCommands();
					start_stat = SharedData.status;
					SharedData.status = SharedData.RobotStatus.TOOL_RUNNING;
					open_failed = false;
					}
				}

				catch (Exception ex)
				{
				OutputSpeech("Tool open exception " + ex.Message + ". Tool is not enabled.");
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				ti = null;
				Speech.EnableAllCommands();
				}

				}
		}



		public static void CloseTool(bool closing = false)

		{
			if (ti != null)
				{

				try
				{
				ti.Close();
				}

				catch(Exception)
				{
				}

				ti = null;
				if (!closing)
					{
					Speech.EnableAllCommands();
					SharedData.status = start_stat;
					}
				}
		}



		public static bool OpenFailed()

		{
			return(open_failed);
		}



		public static bool ToolInProgress()

		{
			bool rtn = false;

			if (ti != null)
				rtn = true;
			return(rtn);
		}


		private static void OutputSpeech(string output)

		{
			Speech.Speak(output);
			Log.LogEntry(output);
		}
		


		}
	}
