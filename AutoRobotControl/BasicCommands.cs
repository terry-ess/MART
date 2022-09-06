using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Speech.Recognition;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;



namespace AutoRobotControl
	{
	class BasicCommands: CommandHandlerInterface
		{

		private byte[] videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
		private DepthImagePixel[] depthdata = new DepthImagePixel[Kinect.nui.DepthStream.FramePixelDataLength];
		private DepthImagePoint[] dips = new DepthImagePoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];
		private DepthImagePixel[] depthdata2 = new DepthImagePixel[Kinect.nui.DepthStream.FramePixelDataLength];
		private SkeletonPoint[] sips = new SkeletonPoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];

		private const string GRAMMAR = "basiccommands";

		public BasicCommands()

		{
			RegisterCommandSpeech();
		}



		private void OutputSpeech(string output)

		{
			Speech.Speak(output);
			Log.LogEntry(output);
		}



		private void StatusResponse()

		{
			NavData.location loc = NavData.GetCurrentLocation();
			string rsp = "";
			int val;
			double dval;
			
			if ((loc.rm_name.Length == 0) || (loc.ls == NavData.LocationStatus.UNKNOWN))
				OutputSpeech("Location unknown.");		
			else if (loc.loc_name.Length > 0)
				OutputSpeech("Location " + loc.rm_name + " at " + loc.loc_name + "with " + loc.orientation  + " degree orientation.");
			else
				OutputSpeech("Location " + loc.rm_name + " at coordinates " + loc.coord.X + "," + loc.coord.Y + "with " + loc.orientation  + " degree orientation.");
			if (Kinect.Operational())
				OutputSpeech("Connect is operational.");
			else
				OutputSpeech("Connect is not operational.");
			if (MotionControl.Operational(ref rsp))
				{
				OutputSpeech("Motion controller is operational.");
				dval = MotionControl.GetVoltage();
				if (dval == -1)
					OutputSpeech("Could not determine current battery voltage.");
				else
					OutputSpeech("Current battery reading is " + dval.ToString("F2") + " volts.");
				rsp = MotionControl.SendCommand("MCSTAT",100);
				if (rsp.StartsWith("fail"))
					OutputSpeech("Could not determine current motor controller status.");
				else
					{
					val = int.Parse(rsp.Substring(3));
					if (val == 0)
						OutputSpeech("Current motor controller status is ok.");
					else
						{
						rsp = "";
						if ((val & 128) > 0)
							rsp = "battery under 16 volts.";
						else if ((val & 64) > 0)
							rsp = "battery over 30 volts.";
						else if ((val & 32) > 0)
							rsp = "motor 2 short.";
						else if ((val & 16) > 0)
							rsp = "motor 1 short.";
						else if ((val & 8) > 0)
							rsp = "motor 2 trip.";
						else if ((val & 4) > 0)
							rsp = "motor 1 trip";
						else
							rsp = "unknown";
						OutputSpeech("Current motor controller error status is " + rsp);
						}
					}
				}
			else
				OutputSpeech("Motion controller is not operational.");
			if (Rplidar.Operational())
				OutputSpeech("LIDAR is operational.");
			else
				OutputSpeech("LIDAR is not operational.");
			if (HeadAssembly.Operational())
				OutputSpeech("Head assembly is operational.");
			else
				OutputSpeech("Head assembly is not operational.");
		}



		private void LocalizeThread()

		{
			OutputSpeech("Attempting to determine my location.");

			try
			{
			Navigate.Close();
			if (Navigate.Open())
				{
				NavData.location loc = NavData.GetCurrentLocation();

				if (loc.loc_name.Length > 0)
					OutputSpeech("Current location is " + loc.rm_name + " at " + loc.loc_name + "with " + loc.orientation  + " degree orientation.");
				else
					OutputSpeech("Current location  is " + loc.rm_name + " at coordinates " + loc.coord.X + " " + loc.coord.Y + " with " + loc.orientation  + " degree orientation.");
				}
			else
				OutputSpeech("Can not determine my current location.");
			}

			catch(Exception ex)
			{
			OutputSpeech("Localization exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			Speech.EnableAllCommands();
		}



		private bool SaveDipsData(string fname,DepthImagePoint[] dips)

		{
			BinaryWriter bw;
			int i;
			bool rtn = false;

			bw = new BinaryWriter(File.Open(fname, FileMode.Create));
			if (bw != null)
				{
				for (i = 0; i < dips.Length; i++)
					bw.Write((short)dips[i].Depth);
				rtn = true;
				}
			bw.Close();
			Log.LogEntry("Saved " + fname);
			return(rtn);
		}



		private bool SaveSipsData(string fname,SkeletonPoint[] sips)

		{
			bool rtn = false;
			BinaryWriter bw;
			DateTime now = DateTime.Now;
			int i;
			short value;

			bw = new BinaryWriter(File.Open(fname, FileMode.Create));
			if (bw != null)
				{
				for (i = 0; i < sips.Length; i++)
					{
					value = (short)(sips[i].X * 1000);
					bw.Write((short)value);
					value = (short)(sips[i].Y * 1000);
					bw.Write((short)value);
					value = (short)(sips[i].Z * 1000);
					bw.Write((short)value);
					}
				bw.Close();
				Log.LogEntry("Saved: " + fname);
				rtn = true;
				}
			return (rtn);
		}



		public bool Shoot()

		{
			bool rtn = false;
			bool images = false;
			Bitmap bm;
			string fname;
			DateTime now = DateTime.Now;

			images = Kinect.GetColorFrame(ref videodata,40) && Kinect.GetDepthFrame(ref depthdata, 40) && Kinect.GetDepthFrame(ref depthdata2,40);
			if (images)
				{
				OutputSpeech("Ok");
				bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				fname = Log.LogDir() + "Shoot " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				bm.RotateFlip(RotateFlipType.Rotate180FlipY);
				bm.Save(fname, ImageFormat.Jpeg);
				Log.LogEntry("Saved " + fname);
				Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, depthdata, dips);
				SaveDipsData(fname.Replace(".jpg",".bin"),dips);
				Kinect.nui.CoordinateMapper.MapDepthFrameToSkeletonFrame(Kinect.nui.DepthStream.Format, depthdata,sips);
				SaveSipsData(fname.Replace(".jpg", ".pc"), sips);
				}
			else
				OutputSpeech("Could not obtain a video and or depth frame.");
			return(rtn);
		}



		public void SpeechHandler(string msg)

		{
			Thread lt;

			Speech.DisableAllCommands();
			if (msg == "shutdown")
				{
				if (!Skills.SkillInProgress() && !Tools.ToolInProgress())
					{
					OutputSpeech("Shutting down.");
					Supervisor.Stop(true);
					}
				else
					{
					StackTrace st = new StackTrace(true);
					Log.LogEntry("Bogus shutdown detected");
					Log.LogEntry("Stack trace: " + st);
					Speech.EnableAllCommands();
					}
				return;
				}
			else if (msg == "robot status")
				{
				StatusResponse();
				Speech.EnableAllCommands();
				}
			else if (msg == "localize")
				{
				lt= new Thread(LocalizeThread);
				lt.Start();
				}
			else if (msg == "last error")
				{
				if (SharedData.last_error.Length == 0)
					OutputSpeech("none");
				else
					OutputSpeech(SharedData.last_error);
				Speech.EnableAllCommands();
				}
			else if (msg == "shoot")
				{
				Shoot();
				Speech.EnableAllCommands();
				}
			}



		public void RegisterCommandSpeech()

		{
			Speech.RegisterHandler(GRAMMAR,SpeechHandler,null);
		}

		}
	}
