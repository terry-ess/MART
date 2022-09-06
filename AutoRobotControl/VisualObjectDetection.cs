using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;


namespace AutoRobotControl
	{
	public static class VisualObjectDetection
		{

		private const string PARAM_FILE = "vodserver.param";
		private const string CLIENT_IP = "127.0.0.1";
		private const string TEMP_IMAGE_FILE_NAME = "temporary.bmp";
		private const int MAX_TRIES = 10;

		public struct visual_detected_object
			{
			public string name;
			public int object_id;
			public int prob;
			public int x;
			public int y;
			public int width;
			public int height;
			};

		private static bool initialized = false;
		private static string cmd_file,server_ip;
		private static int server_port;
		private static Process proc = null;
		private static DeviceComm dc = new DeviceComm();


		public static bool Open()

		{
			string rsp;
			ProcessStartInfo psi;

			if (ReadServerData())
				{

				try
				{
				psi = new ProcessStartInfo(cmd_file);
				psi.UseShellExecute = false;
				proc = Process.Start(psi);
				if (proc != null)
					{
					Thread.Sleep(10000);
					if (proc.HasExited)
						{
						Log.LogEntry("OV server process has exited with exit code " + proc.ExitCode);
						}
					else if (dc.Open(CLIENT_IP, server_port - 1, server_ip, server_port))
						{
						rsp = dc.SendCommand("hello", 100);
						if (rsp.StartsWith("OK"))
							{
							initialized = true;
							Log.LogEntry("Inference server connection established");
							SharedData.visual_obj_detect_operational = true;
							}
						else
							{
							Log.LogEntry("Inference server connection not established.");
							dc.SendCommand("exit", 100);
							dc.Close();
							}
						}
					else
						Log.LogEntry("Could not open connection.");
					}
				else
					Log.LogEntry("Could not start inference process.");
				}

				catch (Exception ex)
				{
				Log.LogEntry("Open exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				}
			else
				Log.LogEntry("VisualObjectDetect: could not read parameter file");
			return(initialized);
		}



		public static void Close()

		{
			if (initialized)
				{
				dc.SendCommand("exit", 100);
				dc.Close();
				initialized = false;
				proc.WaitForExit();
				SharedData.visual_obj_detect_operational = false;
				}
		}



		public static bool Initialized()

		{
			return(initialized);
		}



		public static ArrayList DetectObject(Bitmap bm,string name, double score_limit,int id,bool log = true)

		{
			ArrayList al = new ArrayList();
			string rsp;
			string[] boxes, values;
			visual_detected_object vdo = new visual_detected_object();
			int i;

			rsp = Detect(bm,score_limit,name,id,log);
			if (rsp.StartsWith("OK"))
				{
				boxes = rsp.Split('[');
				if (boxes.Length > 1)
					for (i = 1;i < boxes.Length;i++)
						{
						values = boxes[i].Split(',');
						if (values.Length == 5)
							{
							values[4] = values[4].Substring(0, values[4].Length - 1);
							vdo.name = name;
							vdo.prob = int.Parse(values[0]);
							vdo.x = int.Parse(values[1]);
							vdo.y = int.Parse(values[2]);
							vdo.width = int.Parse(values[3]);
							vdo.height = int.Parse(values[4]);
							al.Add(vdo);
							}
						else if (values.Length == 6)
							{
							values[5] = values[5].Substring(0, values[5].Length - 1);
							vdo.name = name;
							vdo.prob = int.Parse(values[0]);
							vdo.object_id = int.Parse(values[1]);
							vdo.x = int.Parse(values[2]);
							vdo.y = int.Parse(values[3]);
							vdo.width = int.Parse(values[4]);
							vdo.height = int.Parse(values[5]);
							al.Add(vdo);
							}
						}
				}
			else
				Log.LogEntry("Visual object detection failed.");
			return (al);
		}


		private static string Detect(Bitmap bm,double score_limit,string name,int id,bool log = true)

		{
			string rsp = "",cmd,fname;

			if (initialized)
				{
				fname = Application.StartupPath + "\\" + TEMP_IMAGE_FILE_NAME;
				bm.Save(fname, System.Drawing.Imaging.ImageFormat.Bmp);
				if (File.Exists(fname))
					{
					cmd = name + "," + fname + "," + score_limit + "," + id;
					rsp = dc.SendCommand(cmd,5000);
					if (log)
						{
						Log.LogEntry(cmd);
						Log.LogEntry(rsp);
						}
					File.Delete(fname);
					}
				else
					rsp = "FAIL could not save image";
				}
			else
				rsp = "FAIL connection not open";
			return(rsp);
		}



		private static bool ReadServerData()

		{
			string fname;
			TextReader tr;
			bool rtn = false;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);

				try
				{
				cmd_file = tr.ReadLine();
				server_ip = tr.ReadLine();
				server_port = int.Parse(tr.ReadLine());
				rtn = true;
				}

				catch(Exception ex)
				{
				Log.LogEntry("Exception:" + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				rtn = false;
				}

				tr.Close();
				}
			return(rtn);
		}

		}
	}
