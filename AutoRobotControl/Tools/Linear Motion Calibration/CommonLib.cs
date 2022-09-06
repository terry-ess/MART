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


namespace Linear_Motion_Calibration
	{
	static class CommonLib
		{

		private const double BANISTER_OFFSET = 4.5;
		private const int MAX_DIST_DIFF = 4;
		private const int MAX_ANGLE_DIFF = 15;
		private const int MIN_ANGLE_DIFF = 2;

		public const int MECH_DELAY = 3000;
		public const string START_POINT = "calibrate";

		public struct scan_analysis
			{
			public int orient;
			public double cdbw;
			public double cdlw;
			};

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
		private static Bitmap bm = null;
		private static int brthreshold, blthreshold;
		private static Target.target_data td = new Target.target_data(-1);
		private static Target tar = new Target();
		private static bool inited = false;
		private static string error = "";
		private static SensorFusion sf = new SensorFusion();

		public static NavData.recharge rchg = NavData.GetCurrentRoomRechargeStation();
		public static RechargeDock rd;



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
				rd = new RechargeDock();
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
					if (SharedData.log_operations && (bm != null))
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



		private static void LocationMessage()

		{
			NavData.location loc;
			Rectangle rect;
			string msg;
			
			rect = MotionMeasureProb.PdfRectangle();
			loc = NavData.GetCurrentLocation();
			msg = loc.rm_name + "," + loc.coord.X + "," + loc.coord.Y + "," + loc.orientation + ","  + loc.loc_name + "," + loc.ls + "," + rect.Height + "," + rect.Width + "," + rect.X + "," + rect.Y;
			UiCom.SendLocMessage(msg);
		}



		public static bool GetPostion(NavData.recharge rchg,ref scan_analysis sa)

		{
			bool rtn = false;
			ArrayList sdata = new ArrayList();
			Room.rm_location rl;
			NavData.location cloc;

			if (inited && Rplidar.CaptureScan(ref sdata,true))
				{
				if (AnalyzeLidarScan(ref sdata,ref sa,rchg))
					{
					cloc = NavData.GetCurrentLocation();
					rl = NavCompute.TwoWallApprox((int)Math.Round(sa.cdbw), (int)Math.Round(Math.Abs(sa.cdlw) - BANISTER_OFFSET), rchg.direction, (rchg.direction + 270) % 360);
					cloc.coord = rl.coord;
					cloc.orientation = sa.orient;
					cloc.loc_name = "";
					cloc.ls = NavData.LocationStatus.VERIFIED;
					NavData.SetCurrentLocation(cloc);
					MotionMeasureProb.CompleteLocalize(new MotionMeasureProb.Pose(cloc.coord, cloc.orientation));
					LocationMessage();
					rtn = true;
					}
				else
					Log.LogEntry("Could not analyse LIDAR scan.");
				}
			else if (inited)
				Log.LogEntry("Could not capture LIDAR scan.");
			else
				Log.LogEntry("Not initialized.");
			return(rtn);
		}



		private static string SendCommand(string command,int timeout)

		{
			string rtn = "";

			rtn = AutoRobotControl.MotionControl.SendCommand(command,timeout/10);
			Log.LogEntry(command + "," + rtn);
			return(rtn);
		}



		public static bool BackwardMoveToPoint(Point mp)

		{
			bool rtn = false, wi_mag_limit;
			int mh,mad,tc,dist = 0;
			AutoRobotControl.Move rm = new Move();
			string rsp,fname = "";
			Room.rm_location rl;
			NavCompute.pt_to_pt_data ppd;
			NavData.location cloc;

			cloc = NavData.GetCurrentLocation();
			mh = HeadAssembly.GetMagneticHeading();
			mad = NavCompute.DetermineDirection(mh);
			ppd = NavCompute.DetermineRaDirectDistPtToPt(cloc.coord,mp);
			if ((wi_mag_limit = sf.WithInMagLimit(cloc.orientation, mad)) && Turn.TurnToDirection(cloc.orientation,ppd.direc))
				{
				cloc.orientation = ppd.direc;
				tc = (int)Math.Ceiling((((double) ppd.dist / 12) / .34) * 1000) + 300;
				if (LinearMove(SharedData.BACKWARD + " " + ppd.dist,tc))
					{
					rsp = SendCommand(SharedData.DIST_MOVED,200);
					if (rsp.StartsWith("ok"))
						{
						dist = int.Parse(rsp.Substring(3));
						if ((dist > ppd.dist + MAX_DIST_DIFF) || (dist < ppd.dist - MAX_DIST_DIFF))
							{
							Log.LogEntry("Reported distance traveled does not match with expected distance.");
							dist = 0;
							}
						}
					else
						Log.LogEntry("Could not determine distance traveled.");
					if (dist > 0)
						{
						rl = NavCompute.PtDistDirectApprox(cloc.coord,(cloc.orientation + 180) % 360,dist);
						if (!rl.coord.IsEmpty)
							{
							cloc.coord = rl.coord;
							cloc.ls = NavData.LocationStatus.DR;
							NavData.SetCurrentLocation(cloc);
							rtn = true;
							}
						else
							Log.LogEntry("Could not determine current location.");
						}
					}
				else
					{
					AutoRobotControl.MotionControl.DownloadLastMoveFile(ref fname);
					Log.LogEntry("Backward move failed.");
					}
				}
			else if (wi_mag_limit)
				Log.LogEntry("Current orientation estimate does not match with magnetic heading.");
			else
				{
				AutoRobotControl.MotionControl.DownloadLastTurnFile(ref fname);
				Log.LogEntry("Could make turn to " + ppd.direc);
				}
			return(rtn);
		}



		public static bool LinearMove(string command,int timeout)

		{
			bool rtn = false;
			string rsp,fname = "";

			rsp = SendCommand(command,timeout);
			if (rsp.StartsWith("ok"))
				rtn = true;
			else 
				{
				AutoRobotControl.MotionControl.DownloadLastMoveFile(ref fname);
				if (!rsp.Contains(SharedData.INSUFFICENT_FRONT_CLEARANCE)  && !rsp.Contains(SharedData.EXCESSIVE_GYRO_CORRECT))
					{
					Speech.SpeakAsync("Attempting to recover from MC failure.");
					if (AutoRobotControl.MotionControl.RestartMC())
						{
						rsp = SendCommand(command,timeout);
						if (rsp.StartsWith("ok"))
							rtn = true;
						}
					}
				}
			return(rtn);
		}



		public static string LastError()

		{
			return(error);
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



		// this analysis is specific to the area arround the recharge station
		private static bool AnalyzeLidarScan(ref ArrayList sdata, ref scan_analysis sa, NavData.recharge rchg)

		{
			bool rtn = false;
			string lines = "Anaylze LIDAR scan for location\r\n";
			int indx0 = -1,indx270 = -1,minad0 = 180,minad270 = 180,i,ad,samples,pa,indx,start;
			Rplidar.scan_data sd;
			double refd,x,y,angle,rfd,max_rfd = 0;
			double sx2 = 0, sxy = 0, sx = 0, sy = 0, m = 0, b = 0,cdist;

			//find 0 and 270
			for (i = 0; i < sdata.Count; i++)
				{
				sd = (Rplidar.scan_data) sdata[i];
				if (sd.angle == 0)
					{
					indx0 = i;
					minad0 = 0;
					if (minad270 == 0)
						break;
					}
				else
					{
					if ((minad0 > 0) && ((ad = NavCompute.AngularDistance(0,sd.angle)) < minad0))
						{
						minad0 = ad;
						indx0 = i;
						}
					}
				if (sd.angle == 270)
					{
					indx270 = i;
					minad270 = 0;
					if (minad0 == 0)
						break;
					}
				else
					{
					if ((minad270 > 0) && ((ad = NavCompute.AngularDistance(270,sd.angle)) < minad270))
						{
						minad270 = ad;
						indx270 = i;
						}
					}
				}
			lines += "Indexs: 0° " + indx0 + "  270° " + indx270 + "\r\n";
			if ((indx0 != -1) && (indx270 != -1))
				{
				//determine orientation and cartisan distance to back wall (w simple recharge station filter)
				indx = indx0 - 2;
				if (indx < 0)
					indx += sdata.Count;
				for (i = 0;i < 5;i++)
					{
					rfd = ((Rplidar.scan_data) sdata[(indx + i) % sdata.Count]).dist;
					if (rfd > max_rfd)
						max_rfd = rfd;
					}
				refd = max_rfd;
				samples = 0;
				start = indx0 - 10;
				if (start < 0)
					start += sdata.Count;
				for (i = 0;i < 20;i++)
					{
					indx = (start + i) % sdata.Count;
					sd = (Rplidar.scan_data)sdata[indx];
					if ((sd.angle  >= 350) || (sd.angle <= 10))
						{
						y = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD));
						if (Math.Abs(y - refd) < 5)
							{
							x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
							sx += x;
							sx2 += x * x;
							sy += y;
							sxy += x * y;
							samples += 1;
							}
						}
					}
				lines += "Calculation samples: " + samples + "\r\n";
				if (samples >= 10)
					{
					m = ((samples * sxy) - (sx * sy)) / ((samples * sx2) - Math.Pow(sx, 2));
					b = (sy / samples) - ((m * sx) / samples);
					pa = (int)Math.Round(Math.Atan(m) * SharedData.RAD_TO_DEG);
					lines += "Calculated intermediates:  slope " + m + "   intercept " + b + "   wall perp. angle " + pa + "°\r\n";
					cdist = b * Math.Cos(Math.Atan(m));
					sa.orient = rchg.direction + pa;
					sa.cdbw = cdist;
					//determine cartisan distance to left wall (w simple open wall filter)
					samples = 0;
					sx = 0;
					for (i = 0;i < 20;i++)
						{
						indx = (indx270 + i) % sdata.Count;
						sd = (Rplidar.scan_data)sdata[indx];
						if (sd.dist < 50)
							{
							samples += 1;
							angle = sd.angle + pa;
							sx += (sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD));
							}
						else if (samples > 0)
							break;
						}
					sa.cdlw = sx/samples;
					lines += "Anaylsis data: orient " + sa.orient + "°   cdbw " + sa.cdbw + " in.   cdlw " + sa.cdlw + " in.";
					rtn = true;
					}
				else
					lines += "Insufficent samples to calculate data set.";
				}
			else
				lines += "Could not find 350 and/or 270 index.";
			if (SharedData.log_operations)
				Rplidar.SaveLidarScan(ref sdata,lines);
			return(rtn);
		}


		private static bool ReadParameters()

		{
			return(tar.ReadParameters(ref td));
		}

		}
	}
