using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Blob;
using AutoRobotControl;
using Constants;

namespace Turn_Calibration
	{
	static class CommonLib
		{

		public const int MECH_DELAY = 3000;
		public const string START_POINT = "calibrate";

		private const int MAX_ANGLE_DIFF = 15;
		private const int MIN_ANGLE_DIFF = 2;

		private struct dblob
			{
			public int x;
			public int y;
			public uint area;
			public Rectangle rect;
			public double ra;
			public double dist;
			};

		private static byte[] videodata;
		private static DepthImagePixel[] depthdata;
		private static DepthImagePoint[] dips;
		private static dblob adb;
		private static IplImage pic, gs, img;
		private static CvBlobs blobs = null;
		private static Bitmap bm;
		private static int brthreshold, blthreshold;
		private static Target.target_data td = new Target.target_data(-1);
		private static Target tar = new Target();
		private static bool inited = false;
		private static string error = "";
		public static NavData.recharge rchg = NavData.GetCurrentRoomRechargeStation();


		static CommonLib()

		{
		}



		public static bool Open()

		{
			if (!rchg.coord.IsEmpty  && ReadParameters())
				{
				videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
				depthdata = new DepthImagePixel[Kinect.nui.DepthStream.FramePixelDataLength];
				dips = new DepthImagePoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];
				gs = new IplImage(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight, BitDepth.U8, 1);
				img = new IplImage(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight, BitDepth.F32, 1);
				blobs = new CvBlobs();
				inited = true;
				}
			else
				inited = false;
			return(inited);
		}



		public static void Close()

		{
			if (inited)
				{
				videodata = null;
				depthdata = null;
				dips = null;
				pic = null;
				gs = null;
				img = null;
				blobs = null;
				inited = false;
				}
		}



		public static bool LocateTarget(bool determine_dist)

		{
			bool rtn = false;

			if (inited && Kinect.GetColorFrame(ref videodata, 40) && tar.DetermineThresholds(HeadAssembly.GetLightAmplitude(), ref td, ref brthreshold, ref blthreshold))
				{
				rtn = ProcessFrame(determine_dist);
				}
			else if (inited)
				error = "Could not get color frame";
			else
				error = "Not initialized.";
			return(rtn);
		}



		public static bool TurnToTarget()

		{
			bool rtn = true,turn;
			double ra;
			string fname = Log.LogDir() + "Target picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.PIC_FILE_EXT;
			NavData.location cloc;
			SharedData.MotionErrorType met;

			if (inited && LocateTarget(false))
				{
				cloc = NavData.GetCurrentLocation();
				ra = adb.ra;
				while ((Math.Abs(ra) >= MIN_ANGLE_DIFF) && (Math.Abs(ra) < MAX_ANGLE_DIFF))
					{
					turn = Turn.TurnAngle((int) ra);
					if (!turn)
						{
						met = Turn.LastError();
						if ((met != SharedData.MotionErrorType.TURN_NOT_SAFE) && (met != SharedData.MotionErrorType.STOP_TIMEOUT))
							{
							Speech.SpeakAsync("Attempting to recover from MC failure.");
							if (AutoRobotControl.MotionControl.RestartMC())
								turn = Turn.TurnAngle((int)ra);
							}
						}
					if (turn)
						{
						cloc.orientation -= (int) ra;
						if (cloc.orientation < 0)
							cloc.orientation += 360;
						else if (cloc.orientation > 360)
							cloc.orientation -= 360;
						NavData.SetCurrentLocation(cloc);
						if (LocateTarget(false))
							ra = adb.ra;
						else
							{
							error = "Locate target failed, target lost";
							if (SharedData.log_operations)
								{
								bm.Save(fname);
								Log.LogEntry("Saved " + fname);
								}
							rtn = false;
							break;
							}
						}
					else
						{ 
						error = "Turn failed";
						rtn = false;
						break;
						}
					}
				if (Math.Abs(ra) >= MAX_ANGLE_DIFF)
					{
					rtn = false;
					error = "Locate target failed, target confusion detected.";
					if (SharedData.log_operations)
						{
						bm.Save(fname);
						Log.LogEntry("Saved " + fname);
						}
					}
				}
			else
				{
				rtn = false;
				if (inited)
					{
					error = "Locate target failed, target not found";
					if (SharedData.log_operations)
						{
						bm.Save(fname);
						Log.LogEntry("Saved " + fname);
						}
					}
				else
					error = "Not initialized.";
				}
			return (rtn);
		}



		private static string SendCommand(string command,int timeout)

		{
			string rtn = "";

			rtn = AutoRobotControl.MotionControl.SendCommand(command,timeout/10);
			Log.LogEntry(command + "," + rtn);
			return(rtn);
		}



		public static string LastError()

		{
			return(error);
		}

		

		public static bool GetAngle(ref double tangle)

		{
			bool rtn = false;

			if ((rtn = LocateTarget(false)))
				tangle = adb.ra;
			return(rtn);
		}



		private static bool ProcessFrame(bool determine_dist)
            
      {
			CvBlob b;
			int i, color,bcount;
			double bright;
			bool rtn = false;

			for (i = 0; i < Kinect.nui.ColorStream.FramePixelDataLength; i += 4)
				{
				bright = (videodata[i + 2] + videodata[i + 1] + videodata[i])/3;
				color = (videodata[i] - videodata[i + 1]) + (videodata[i] - videodata[i + 2]); //blue
				if ((bright < brthreshold) && (color > blthreshold))
					{ 
					videodata[i] = 255;
					videodata[i + 1] = 255;
					videodata[i + 2] = 255;
					}
				else
					{
					videodata[i] = 0;
					videodata[i + 1] = 0;
					videodata[i + 2] = 0;
					}
				}
			bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
			pic = bm.ToIplImage();
			Cv.CvtColor(pic,gs,ColorConversion.BgrToGray);
			blobs.Label(gs,img);
			if (blobs.Count > 0)
				{
				bcount = 0;
				do
					{
					b = (CvBlob) blobs[blobs.GreaterBlob()];
					if (b.Area > td.min_blob_area)
						{
						if (((b.Rect.Y + b.Rect.Height) > Kinect.nui.ColorStream.FrameHeight/2) && (b.Rect.Y < Kinect.nui.ColorStream.FrameHeight/2))
							{
							adb.area = b.Area;
							adb.x = (int) b.Centroid.X;
							adb.y = (int) b.Centroid.Y;
							adb.rect.X = b.Rect.X;
							adb.rect.Y = b.Rect.Y;
							adb.rect.Width = b.Rect.Width;
							adb.rect.Height = b.Rect.Height;
							adb.ra = Kinect.VideoHorDegrees((int)Math.Round(b.Centroid.X - ((double)Kinect.nui.ColorStream.FrameWidth / 2)));
							if (determine_dist)
								{
								double dist;

								if (Kinect.GetDepthFrame(ref depthdata,40))
									{
									Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format,Kinect.nui.DepthStream.Format,depthdata,dips);
									dist = dips[(adb.y * Kinect.nui.ColorStream.FrameWidth) + adb.x].Depth * SharedData.MM_TO_IN;
									dist = Kinect.CorrectedDistance(dist);
									adb.dist = dist;
									rtn = true;
									}
								}
							else
								rtn = true;
							}
						else
							{
							blobs.Remove(blobs.GreaterBlob());
							bcount += 1;
							}
						}
					else
						adb.area = 0;
					}
				while (!rtn && (b.Area > td.min_blob_area) && (bcount < 5));
				if (bcount == 5)
					{
					error = "Could not find target in " + bcount + " trys.";
					if (SharedData.log_operations)
						{
						string fname;

						fname = Log.LogDir() + "Target picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.PIC_FILE_EXT;
						bm.Save(fname);
						Log.LogEntry("Saved " + fname);
						}
					}
				}
			else
				adb.area = 0;
			return(rtn);
      }



		private static bool ReadParameters()

		{
			return(tar.ReadParameters(ref td));
		}

		}
	}
