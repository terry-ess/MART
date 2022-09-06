using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using AutoRobotControl;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;

namespace Room_Survey
	{
	static class SkillShared
		{

		public const int XMS = 4;    //~ 3 sigma location limits for 24 in move in 0/180 direction
		public const int YMS = 9;
		public const int STD_SIDE_CLEAR = 1;
		public const int MIN_DIST_FOR_SEARCH = 72;
		public const string SURVEY_SUB_DIR = "\\survey\\";
		
		private const int TILT_ANGLE = -30;


		public struct search_data
		{
			public RoomSurvey.entry_strategy es;
			public RoomEntry.search_strategy ss;
			public Point ct_pt;
			public Point entry_pt;
			public Point entry_pt2;
			public Point axis2t_pt;
		};

		public static bool silent = false;
		public static bool run = true;
		public static MotionMeasureProb.Pose ccpose;
		public static ArrayList sdata = new ArrayList();
		public static Point map_shift = new Point();
		public static int cc_orient ;
		public static NavData.connection connect = new NavData.connection();
		public static NavData.connection exit_connect;
		public static search_data searchd = new search_data();
		public static Point fs_map_shift = new Point();
		public static byte[,] fs_map = null;
		public static Point fs_start;
		
		private static byte[] videodata = new byte[AutoRobotControl.Kinect.nui.ColorStream.FramePixelDataLength];


		public static void OutputSpeech(string output,bool must_hear)

		{
			if (!SkillShared.silent || must_hear)
				Speech.Speak(output);
			Log.LogEntry(output);
		}



		public static bool SendCommand(string command,int timeout_count)

		{
			string rsp = "";
			bool rtn;

			Log.LogEntry(command);
			if (timeout_count < 20)
				timeout_count = 20;
			rsp = AutoRobotControl.MotionControl.SendCommand(command,timeout_count);
			Log.LogEntry(rsp);
			if (rsp.Contains("fail"))
				{
				rtn = false;
				}
			else
				rtn = true;
			return(rtn);
		}



		public static string SSendCommand(string command,int timeout_count)

		{
			string rsp = "";

			Log.LogEntry(command);
			if (timeout_count < 20)
				timeout_count = 20;
			rsp = AutoRobotControl.MotionControl.SendCommand(command,timeout_count);
			Log.LogEntry(rsp);
			return(rsp);
		}



		public static string SendCommand(string command)

		{
			string rsp = "";

			Log.LogEntry(command);
			rsp = AutoRobotControl.MotionControl.SendCommand(command,100);
			Log.LogEntry(rsp);
			return(rsp);
		}



		private static ArrayList CaptureRoomScan(MotionMeasureProb.Pose pse,string aline,ref string fname)

		{
			string line = "Room scan (connection centered origin)\r\nEstimated pose: " + pse.ToString() + "\r\nConnect direct: " + connect.direction;
			int i, mangle,pos,nd_count = 0;
			double x, y;
			Rplidar.scan_data sd;
			ArrayList scan = new ArrayList();
			RoomSurvey.ExtScanData esd = new RoomSurvey.ExtScanData();
			bool not_done;

			if (SkillShared.sdata.Count > 0)
				{
				scan.Add(pse);
				for (i = 0; i < SkillShared.sdata.Count; i++)
					{
					sd = (Rplidar.scan_data)SkillShared.sdata[i];
					mangle = (sd.angle + pse.orient) % 360;
					y = sd.dist * Math.Cos(mangle * SharedData.DEG_TO_RAD);
					if (y >= -pse.coord.Y)
						{
						x = sd.dist * Math.Sin(mangle * SharedData.DEG_TO_RAD);
						esd.sd = sd;
						esd.coord = new Point((int)Math.Round(x), (int)Math.Round(y));
						scan.Add(esd);
						}
					}
				if (aline.Length > 0)
					line += aline;
				fname = Rplidar.SaveLidarScan(ref SkillShared.sdata, line);
				pos = fname.LastIndexOf('\\');
				do
					{
					not_done = false;

					try 
					{
					File.Copy(fname, Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + fname.Substring(pos + 1));
					}

					catch(Exception ex)
					{
					if (ex.Message.EndsWith("is denied."))
						{
						nd_count += 1;
						if (nd_count < 3)
							{
							Thread.Sleep(500);
							not_done = true;
							}
						else
							Log.LogEntry("Could not copy " + fname + " to survey directory after 3 tries.");
						}
					else
						Log.LogEntry("Could not copy " + fname + " to survey directory.");
					}

					}
				while(not_done);
				}
			else
				SkillShared.OutputSpeech("No LIDAR scan available.", true);
			return (scan);
		}



		public static bool CaptureRoomScan(MotionMeasureProb.Pose pse,ref ArrayList scans,ref ArrayList fnames)

		{
			bool rtn = false;
			ArrayList scan = new ArrayList();
			string fname = "";
			int pos;

			scan = CaptureRoomScan(pse,"",ref fname);
			if (scan.Count > 0)
				{
				if (scans != null)
					scans.Add(scan);
				if (fnames != null)
					{
					pos = fname.LastIndexOf('\\');
					fnames.Add(fname.Substring(pos + 1));
					}
				rtn = true;
				}
			return(rtn);
		}



		public static string CaptureRoomScan(MotionMeasureProb.Pose pse,string aline)

		{
			ArrayList scan = new ArrayList();
			string fname = "";
			int pos;

			scan = CaptureRoomScan(pse,aline, ref fname);
			pos = fname.LastIndexOf('\\');
			fname = fname.Substring(pos + 1);
			return (fname);
		}



		public static bool CapturePicture(string fname,bool tilt)

		{
			bool rtn = false;
			Bitmap bm;
			string tfname;
			int pos;

			try
			{
			if (Kinect.GetColorFrame(ref videodata,40))
				{
				bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				bm.RotateFlip(RotateFlipType.Rotate180FlipY);
				bm.Save(fname,System.Drawing.Imaging.ImageFormat.Jpeg);
				if (tilt)
					{
					HeadAssembly.Tilt(TILT_ANGLE,true);
					if (Kinect.GetColorFrame(ref videodata,40))
						{
						bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
						bm.RotateFlip(RotateFlipType.Rotate180FlipY);
						pos = fname.LastIndexOf('\\');
						tfname = fname.Substring(0,pos + 1);
						tfname += "Tilted ";
						tfname += fname.Substring(pos + 1);
						bm.Save(tfname,System.Drawing.Imaging.ImageFormat.Jpeg);
						}
					HeadAssembly.Tilt(0,true);
					}
				rtn = true;
				}
			}

			catch (Exception ex)
			{
			HeadAssembly.Tilt(0,true);
			Log.LogEntry("CapturePicture exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}
			
			return (rtn);
		}



		public static bool CapturePicture(string fname)

		{
			return(CapturePicture(fname,false));
		}



		public static string ArrayListToString(ArrayList al)

		{
			string stg = "";
			int i;

			for (i = 0;i < al.Count;i++)
				stg += al[i].ToString() + "\r\n";
			return(stg);
		}



		public static Bitmap MapToBitmap(byte[,] map)

		{
			Bitmap bm;
			int i,j,width,height;

			width = map.GetUpperBound(0);
			height = map.GetUpperBound(1);
			bm = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			for (i = 0; i < width; i++)
				for (j = 0; j < height; j++)
					{
					if (map[i, j] == (byte)AutoRobotControl.Room.MapCode.CLEAR)
						bm.SetPixel(i, j, Color.White);
					else
						bm.SetPixel(i, j, Color.Black);
					}
			return(bm);
		}


		}
	}
