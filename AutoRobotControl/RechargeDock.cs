using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Blob;

namespace AutoRobotControl
	{
	public class RechargeDock
		{

		public const string THRESHOLD_ERROR = "Could not determine thresholds.";

		private const string TTPID_PARAM_FILE = "ttpid";

		private const int MOVE_TO_TARGET_CLEARANCE = 48;
		private const int OFFSET_FOR_SECOND_MOVE = 24;
		private const int OFFSET_FOR_FINAL_MOVE = 6;
		private const int CORRECT_DELAY = 200;
		private const int RETRY_LIMIT = 2;
		private const int MIN_SIDE_CLEAR = 1;
		private const int START_SPEED = 30;


		public struct dblob
			{
			public int x;
			public int y;
			public uint area;
			public Rectangle rect;
			public double ra;
			public double dist;
			public int no_blobs;
			public int bcount;
			};


		private byte[] videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
		private dblob adb;
		private IplImage gs = new IplImage(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight, BitDepth.U8, 1);
		private IplImage img = new IplImage(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight, BitDepth.F32, 1);
		private CvBlobs blobs = new CvBlobs();
		private double ttpgain, ttigain, ttdgain;
		private Thread exec = null;
		private bool run = false;
		private ArrayList ts = new ArrayList();
		private ArrayList dist = new ArrayList();
		private ArrayList rangle = new ArrayList();
		private ArrayList nblobs = new ArrayList();
		private ArrayList bc = new ArrayList();
		private Stopwatch sw = new Stopwatch();
		private string error = "";
		private double ds_depth,ptp_dist,sensor_offset;
		private Bitmap bm = null;
		private NavData.location dloc = new NavData.location();
		private Target.target_data td = new Target.target_data(-1);
		private int brthreshold,bluthreshold,la;
		private string last_data_file = "";
		private bool initialized = false;
		private int initial_expect_dist = -1;
		private int docking_retrys = 0;

		private SensorFusion sf = new SensorFusion();
		private Target tar = new Target();


		private bool ReadRefParameters()

		{
			string fname,line;
			TextReader tr;
			bool rtn = false;

			if (tar.ReadParameters(ref td))
				{
				fname = Application.StartupPath + SharedData.CAL_SUB_DIR + TTPID_PARAM_FILE + SharedData.CAL_FILE_EXT;
				if (File.Exists(fname))
					{
					tr = File.OpenText(fname);
					line = tr.ReadLine();
					ttpgain = double.Parse(line);
					line = tr.ReadLine();
					ttigain = double.Parse(line);
					line = tr.ReadLine();
					ttdgain = double.Parse(line);
					tr.Close();
					rtn = true;
					}
				else
					Log.LogEntry("Could not read " + TTPID_PARAM_FILE);
				}
			return(rtn);
		}



        private bool ProcessFrame(bool turn)
            
        {
			CvBlob b;
			IplImage pic; 
			int i, color,bcount;
			double bright;
			bool rtn = false;
			double ra;

			for (i = 0; i < Kinect.nui.ColorStream.FramePixelDataLength; i += 4)
				{
				bright = (videodata[i + 2] + videodata[i + 1] + videodata[i])/3;
				color = (videodata[i] - videodata[i + 1]) + (videodata[i] - videodata[i + 2]); //blue
				if ((bright < brthreshold) && (color > bluthreshold))
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
						if (((b.Rect.Y + b.Rect.Height) > Kinect.nui.ColorStream.FrameHeight/2) && (!turn || (b.Rect.Y < Kinect.nui.ColorStream.FrameHeight/2)))
							{
							ra = Kinect.VideoHorDegrees((int) Math.Round(b.Centroid.X - ((double)Kinect.nui.ColorStream.FrameWidth / 2)));
							if (!turn && (ra > 5))
								{
								blobs.Remove(blobs.GreaterBlob());
								bcount += 1;
								}
							else
								{
								adb.area = b.Area;
								adb.x = (int) Math.Round(b.Centroid.X);
								adb.y = (int) Math.Round(b.Centroid.Y);
								adb.rect.X = b.Rect.X;
								adb.rect.Y = b.Rect.Y;
								adb.rect.Width = b.Rect.Width;
								adb.rect.Height = b.Rect.Height;
								adb.ra = ra;
								adb.dist = td.target_height / adb.rect.Height;
								adb.no_blobs = blobs.Count;
								adb.bcount = bcount + 1;
								rtn = true;
								}
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
					Log.LogEntry("Could not find target in " + bcount + " trys.");
					if (SharedData.log_operations)
						{
						string fname;

						fname = Log.LogDir() + "Target picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.PIC_FILE_EXT;
						bm.Save(fname);
						Log.LogEntry("Saved " + fname);
						}
					}
				else if (!rtn)
					{
					Log.LogEntry("Could not find target.");
					if (b != null)
						{
						Log.LogEntry("Largest blob area: " + b.Area);
						Log.LogEntry("Largest blob rectangle: " + b.Rect);
						}
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
				{
				adb.area = 0;
				Log.LogEntry("Could not find target. No blobs found.");
				}
			return (rtn);
        }


		private bool LocateTarget(bool turn,ref bool no_frame)

		{
			bool rtn = false;

			bm = null;
			if (Kinect.GetColorFrame(ref videodata, 60))
				{
				rtn = ProcessFrame(turn);
				no_frame = false;
				}
			else
				{
				Log.LogEntry("Could not get color frame");
				no_frame = true;
				}
			return(rtn);
		}



		private string SendCommand(string command,int timeout)

		{
			string rtn = "";

			rtn = MotionControl.SendCommand(command,timeout/10);
			Log.LogEntry(command + "," + rtn);
			return(rtn);
		}



		private bool Turn(int angle,ref string error)

		{
			string rsp;
			bool rtn = false;

			if (angle < 0)
				rsp = SendCommand(SharedData.RIGHT_TURN +  " " + Math.Abs(angle),2000);
			else
				rsp = SendCommand(SharedData.LEFT_TURN + " " + angle,2000);
			if (rsp.StartsWith("ok"))
				rtn = true;
			else
				error = rsp.Substring(4);
			return(rtn);
		}



		private bool DetermineThresholds(ref Target.target_data td,ref int brthreshold,ref int bluthreshold)

		{
			bool rtn = false;
			int i;
			string reply;

			for (i = 0;i < 2;i++)
				{
				la = HeadAssembly.GetLightAmplitude();
				Log.LogEntry("Light amplitude: " + la);
				if ((rtn = tar.DetermineThresholds(la, ref td, ref brthreshold, ref bluthreshold)))
					break;
				else if (i == 0)
					{
					reply = Speech.Conversation("There is insufficient light to locate the target.  Can a light be turned on?","responseyn",5000,false);
					if (reply == "yes")
						{
						Speech.SpeakAsync("thanks");
						Thread.Sleep(10000);
						}
					else
						break;
					}
				}
			return(rtn);
		}



		private bool TurnToTarget()

		{
			bool rtn = true;
			double ra;
			string rsp;
			bool no_frame = false;
			int frames_lost = 0;

			if (DetermineThresholds(ref td, ref brthreshold, ref bluthreshold))
				{			
				if (LocateTarget(true,ref no_frame))
					{
					ra = adb.ra;
					while (Math.Abs(ra) > 2)
						{
						rsp = "";
						if (Turn((int) ra,ref rsp))
							{
							if (LocateTarget(true,ref no_frame))
								ra = adb.ra;
							else
								{
								error = "Location target failed, target lost";
								rtn = false;
								break;
								}
							}
						else
							{ 
							if (rsp.Length > 0)
								error = "Locate target failed with: " + rsp;
							else
								error = "Locate target failed with command error";
							rtn = false;
							break;
							}
						}
					}
				else if (no_frame)
					frames_lost += 1;
				else
					{
					rtn = false;
					error = "Locate target failed, target not found";
					}
				}
			else
				{
				rtn = false;
				error = THRESHOLD_ERROR;
				}
			return (rtn);
		}



		private bool MoveToTarget()
		
		{
			bool rtn = true;
			string rsp,status = "";
			bool not_done = true;
			double sum_ra = 0,last_ra = 400;
			int applied_correction = 0,frames_lost = 0;
			long start_time;
			string fname;
			bool no_frame = false;

			last_data_file = "";
			Thread.CurrentThread.Priority = ThreadPriority.Highest;
			if (DetermineThresholds(ref td, ref brthreshold, ref bluthreshold))
				{
				UiCom.SetVideoSuspend(true);
				Supervisor.SetPollRead(false);
				rsp = SendCommand(SharedData.REF_MOVE_START + " " + START_SPEED, 500);
				if (rsp.StartsWith("fail"))
					{
					if (rsp.Contains("receive timedout"))
						{
						SendCommand(SharedData.REF_MOVE_STOP, 1000);
						}
					if (rsp.Length > 5)
						error = "MoveToTarget failed with: " + rsp;
					else
						error = "MoveToTarget failed with command error";
					UiCom.SetVideoSuspend(false);
					Supervisor.SetPollRead(true);
					rtn = false;
					}
				else
					{
					start_time = sw.ElapsedMilliseconds;
					while (not_done && run)
						{
						if (LocateTarget(false,ref no_frame) && run)
							{
							frames_lost = 0;
							ts.Add(sw.ElapsedMilliseconds - start_time);
							rangle.Add(adb.ra);
							dist.Add(adb.dist);
							nblobs.Add(adb.no_blobs);
							bc.Add(adb.bcount);
							if (adb.dist <= MOVE_TO_TARGET_CLEARANCE)
								{
								SendCommand(SharedData.REF_MOVE_STOP, 1000);
								not_done = false;
								status = "move complete";
								}
							else if ((last_ra != 400) && (Math.Abs(adb.ra - last_ra) > 5))
								{
								SendCommand(SharedData.REF_MOVE_STOP, 1000);
								not_done = false;
								error = "Target confusion detected.";
								if (SharedData.log_operations)
									{
									Graphics g;

									fname = Log.LogDir() + "Target picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.PIC_FILE_EXT;
									g = System.Drawing.Graphics.FromImage(bm);
									g.DrawLine(Pens.Red, 0, 239, 639, 239);
									g.DrawLine(Pens.Red, 0, 240, 639, 240);
									g.DrawLine(Pens.Red, 319, 0, 319, 479);
									g.DrawLine(Pens.Red, 320, 0, 320, 479);
									fname = Log.LogDir() + "Target picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.PIC_FILE_EXT;
									bm.Save(fname);
									Log.LogEntry("Saved: " + fname);
									}
								status = error;
								rtn = false;
								}
							if (not_done)
								{
								int correction,delta_correct;
								string msg = "";

								if ((sw.ElapsedMilliseconds - start_time) > CORRECT_DELAY)
									{
									sum_ra += adb.ra;
									correction = (int) ((ttpgain * adb.ra) + (ttigain * sum_ra));
									if (last_ra != 400)
										correction += (int) (ttdgain * (adb.ra - last_ra));
									delta_correct = correction - applied_correction;
									applied_correction += delta_correct;
									if (delta_correct != 0)
										{
										msg = SharedData.REF_CHG_SPEED + " " + delta_correct.ToString() + " " + (-delta_correct).ToString();
										rsp = MotionControl.SendCommand(msg,200);
										if (rsp.StartsWith("fail"))
											{
											SendCommand(SharedData.REF_MOVE_STOP, 1000);
											rtn = false;
											error = "MoveToTarget failed, CS command failed.";
											status = "CS command failed";
											break;
											}
										}
									}
								}
							last_ra = adb.ra;
							}
						else if (no_frame && (frames_lost == 0))
							frames_lost += 1;
						else
							{
							SendCommand(SharedData.REF_MOVE_STOP, 1000);
							rtn = false;
							if (run)
								{
								error = "MoveToTarget failed, target lost.";
								status = "target lost";
								if ((bm != null) && SharedData.log_operations)
									{
									Graphics g;

									fname = Log.LogDir() + "Target picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.PIC_FILE_EXT;
									g = System.Drawing.Graphics.FromImage(bm);
									g.DrawLine(Pens.Red, 0, 239, 639, 239);
									g.DrawLine(Pens.Red, 0, 240, 639, 240);
									g.DrawLine(Pens.Red, 319, 0, 319, 479);
									g.DrawLine(Pens.Red, 320, 0, 320, 479);
									bm.Save(fname);
									Log.LogEntry("Saved " + fname);
									}
								}
							else
								{
								error = "MoveToTarget stopped.";
								status = "stopped";
								}
							break;
							}
						}
					last_data_file = SaveVisionProfile("DockingMoveToTarget", "Move status: " + status);
					UiCom.SetVideoSuspend(false);
					Supervisor.SetPollRead(true);
					}
				}
			else
				{
				rtn = false;
				error = THRESHOLD_ERROR;
				}
			return(rtn);
		}



		public string SaveVisionProfile(string title,string data_line)

		{
			int i;
			TextWriter sw;
			string fname;

			fname = Log.LogDir() + title + " " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
			sw = File.CreateText(fname);
			if (sw != null)
				{
				sw.WriteLine( title + " : " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				sw.WriteLine(data_line);
				sw.WriteLine("Vision parameters:");
				sw.WriteLine("   light amplitude - " + la);
				sw.WriteLine("   intensity threshold - " + brthreshold);
				sw.WriteLine("   blue threshold - " + bluthreshold);
				sw.WriteLine("   min. blob area - " + td.min_blob_area);
				sw.WriteLine("Turn correction gains:");
				sw.WriteLine("   pgain - " + ttpgain);
				sw.WriteLine("   dgain - " + ttdgain);
				sw.WriteLine("   igain - " + ttigain);
				sw.WriteLine();
				sw.WriteLine("Elapsed time (ms),Relative Angle (°),Distance (in),No blobs detected,No blobs. scanned");
				for (i = 0;i < ts.Count;i++)
					{
					try
					{
					sw.Write(ts[i].ToString() + ",");
					sw.Write(((double) rangle[i]).ToString("F1") + ",");
					sw.Write(((double) dist[i]).ToString("F1") + ",");
					sw.Write(nblobs[i].ToString() + ",");
					sw.Write(bc[i]);
					}

					catch(Exception)
					{
					}

					sw.WriteLine();
					sw.Flush();
					}
				sw.Close();
				Log.LogEntry("Saved " + fname);
				}
			else
				fname = "";
			ts.Clear();
			rangle.Clear();
			dist.Clear();
			nblobs.Clear();
			return(fname);
		}



		private bool DetermineLocation(int pa,double lp_dx,double rp_dx,double cdist,ref NavData.location loc)

		{
			bool rtn = false;
			NavData.location cloc;
			NavData.recharge recharge_station;

			cloc = NavData.GetCurrentLocation();
			recharge_station = NavData.GetRechargeStation(cloc.rm_name);
			if (!recharge_station.coord.IsEmpty)
				{
				rtn = true;
				if (recharge_station.direction == 270)
					{
					cloc.orientation = 270 + pa;
					cloc.coord.Y = recharge_station.coord.Y;
					if (Math.Abs(lp_dx) > Math.Abs(rp_dx))
						cloc.coord.Y += (int) Math.Round(Math.Abs(lp_dx) - ((Math.Abs(lp_dx) + Math.Abs(rp_dx))/2));
					else
						cloc.coord.Y -= (int) Math.Round(Math.Abs(rp_dx) - ((Math.Abs(lp_dx) + Math.Abs(rp_dx)) / 2));
					cloc.coord.X = recharge_station.coord.X + ((int) Math.Round(cdist + SharedData.FLIDAR_OFFSET));
					cloc.ls = NavData.LocationStatus.VERIFIED;
					}
				else if (recharge_station.direction == 180)
					{
					cloc.orientation = 180 + pa;
					cloc.coord.X = recharge_station.coord.X;
					if (Math.Abs(lp_dx) > Math.Abs(rp_dx))
						cloc.coord.X += (int)Math.Round(Math.Abs(lp_dx) - ((Math.Abs(lp_dx) + Math.Abs(rp_dx)) / 2));
					else
						cloc.coord.X -= (int)Math.Round(Math.Abs(rp_dx) - ((Math.Abs(lp_dx) + Math.Abs(rp_dx)) / 2));
					cloc.coord.Y = recharge_station.coord.Y - (int) Math.Round(cdist + SharedData.FLIDAR_OFFSET);
					cloc.ls = NavData.LocationStatus.VERIFIED;
					}
				else if (recharge_station.direction == 90)
					{
					cloc.orientation = 90 + pa;
					cloc.coord.Y = recharge_station.coord.Y;
					if (Math.Abs(lp_dx) > Math.Abs(rp_dx))
						cloc.coord.Y -= (int)Math.Round(Math.Abs(lp_dx) - ((Math.Abs(lp_dx) + Math.Abs(rp_dx)) / 2));
					else
						cloc.coord.Y += (int)Math.Round(Math.Abs(rp_dx) - ((Math.Abs(lp_dx) + Math.Abs(rp_dx)) / 2));
					cloc.coord.X = recharge_station.coord.X - (int) Math.Round(cdist + SharedData.FLIDAR_OFFSET);
					cloc.ls = NavData.LocationStatus.VERIFIED;
					}
				else if (recharge_station.direction == 0)
					{
					cloc.orientation = pa;
					if (cloc.orientation < 0)
						cloc.orientation += 360;
					cloc.coord.X = recharge_station.coord.X;
					if (Math.Abs(lp_dx) > Math.Abs(rp_dx))
						cloc.coord.X -= (int)Math.Round(Math.Abs(lp_dx) - ((Math.Abs(lp_dx) + Math.Abs(rp_dx)) / 2));
					else
						cloc.coord.X += (int)Math.Round(Math.Abs(rp_dx) - ((Math.Abs(lp_dx) + Math.Abs(rp_dx)) / 2));
					cloc.coord.Y = recharge_station.coord.Y + ((int) Math.Round(cdist + SharedData.FLIDAR_OFFSET));
					cloc.ls = NavData.LocationStatus.VERIFIED;
					}
				else
					rtn = false;
				if (rtn)
					{
					loc = cloc;
					MotionMeasureProb.CompleteLocalize(new MotionMeasureProb.Pose(cloc.coord, cloc.orientation));
					LocationMessage(cloc);
					}
				}
			return(rtn);
		}



		private bool AdjustProcessLidarScan(double expected,ref ArrayList sdata,ref int pangle,ref double lp_dx,ref double rp_dx,ref double center_dist,ref string lines)

		{
			bool rtn = false;
			double cdist = 0,mdist,xdist,ydist,ldist;
			int i,j,cindex = -1,lpindex = -1,rpindex = -1,cutoff_angle,index,coa,samples,pa,lwindex = -1,rwindex = -1;
			Rplidar.scan_data sd;
			double sx2 = 0, sxy = 0, sx = 0, sy = 0, m = 0, b = 0, y, x,ly,lx,ry,rx,angle,width_allow,see = 0,nsee = 0;
			ArrayList xydata = new ArrayList();
			double[] xy;
			int madist = 10,adist;
			const int DIST_VAR = 2;

			for (i = 0;i < sdata.Count;i++)
				{
				sd = (Rplidar.scan_data) sdata[i];
				if (sd.angle == 0)
					{
					ydist = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD)) + SharedData.FLIDAR_OFFSET;
					cindex = i;
					cdist = sd.dist;
					break;
					}
				else if ((adist = NavCompute.AngularDistance(0,sd.angle)) < madist)
					{
					ydist = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD)) + SharedData.FLIDAR_OFFSET;
					madist = adist;
					cindex = i;
					cdist = sd.dist;
					}
				}
			if (cindex >= 0)
				{
				cutoff_angle = (int) (Math.Atan((ptp_dist/2)/(cdist - ds_depth)) * SharedData.RAD_TO_DEG);
				cutoff_angle *= 2;
				lines += "Center: index " + cindex + "   dist (in) " + cdist + "   cutoff angle " + cutoff_angle + "°\r\n";
				coa = 360 -cutoff_angle;
				mdist = cdist;
				for (i = 1;;i++)
					{
					index = cindex - i;
					if (index < 0)
						index += sdata.Count;
					sd = (Rplidar.scan_data)sdata[index];
					if (sd.angle <= coa)
						break;
					if (sd.dist < mdist)
						{
						lpindex = index;
						mdist = sd.dist;
						}
					}
				lines += "Left post inner edge index: " + lpindex + "\r\n";
				if (lpindex >= 0)
					{
					coa = cutoff_angle;
					mdist = cdist;
					for (i = 1;;i++)
						{
						index = (cindex + i) % sdata.Count;
						sd = (Rplidar.scan_data)sdata[index];
						if (sd.angle >= coa)
							break;
						if (sd.dist < mdist)
							{
							rpindex = index;
							mdist = sd.dist;
							}
						}
					lines += "Right post inner edge index: " + rpindex + "\r\n";
					if (rpindex >= 0)
						{
						ldist = ((Rplidar.scan_data) sdata[lpindex]).dist;
						mdist = 0;
						for (i = 1;;i++)
							{
							index = (lpindex + i) % sdata.Count;
							if (index == cindex)
								{
								if (lwindex == -1)
									lwindex = index;
								break;
								}
							sd = (Rplidar.scan_data)sdata[index];
							if (Math.Abs(ldist - sd.dist) > mdist)
								{
								mdist = Math.Abs(ldist - sd.dist);
								lwindex = index;
								}
							ldist = sd.dist;
							}
						ldist = ((Rplidar.scan_data)sdata[rpindex]).dist;
						mdist = 0;
						for (i = 1;; i++)
							{
							index = rpindex - i;
							if (index < 0)
								index += sdata.Count;
							if (index == cindex)
								{
								if (rwindex == -1)
									rwindex = index;
								break;
								}
							sd = (Rplidar.scan_data)sdata[index];
							if (Math.Abs(ldist - sd.dist) > mdist)
								{
								mdist = Math.Abs(ldist - sd.dist);
								rwindex = index;
								}
							ldist = sd.dist;
							}
						if ((lwindex != -1) && (rwindex != -1))
							{
							for (j = 0;j < 3;j++)
								{
								samples = 0;
								lwindex  = (lwindex + j) % sdata.Count;
								rwindex -= j;
								if (rwindex < 0)
									rwindex = sdata.Count + rwindex;
								for (i = 0;;i++)
									{
									samples += 1;
									index = (lwindex + i) % sdata.Count;
									sd = (Rplidar.scan_data)sdata[index];
									x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
									sx += x;
									sx2 += x * x;
									y = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD));
									sy += y;
									sxy += x * y;
									xy = new double[2];
									xy[0] = x;
									xy[1] = y;
									xydata.Add(xy);
									if (index == rwindex)
										break;
									}
								m = ((samples * sxy) - (sx * sy)) / ((samples * sx2) - Math.Pow(sx, 2));
								b = (sy / samples) - ((m * sx) / samples);
								for (i = 0;i < xydata.Count;i++)
									{
									xy = (double[]) xydata[i];
									x = xy[0];
									y = (x * m ) + b;
									see += Math.Pow(xy[1] - y,2);
									}
								see = Math.Sqrt(see/xydata.Count);
								nsee = see / (sy / xydata.Count);
								if (nsee <= sf.GetNeseThreshold())
									break;
								}
							if (nsee <= sf.GetNeseThreshold())
								{
								pa = (int) Math.Round(Math.Atan(m) * SharedData.RAD_TO_DEG);
								sd = (Rplidar.scan_data)sdata[lpindex];
								angle = sd.angle + pa;
								lx = (sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD));
								ly = (sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD));
								sd = (Rplidar.scan_data)sdata[rpindex];
								angle = sd.angle + pa;
								rx = (sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD));
								ry = (sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD));
								if (lx < 0)
									xdist = rx - lx;
								else
									xdist = rx + lx;
								ydist = (ry + ly)/2;
								lines += "Calculated data: perp. angle " + pa + "°   left post inner edge (x y) " + lx.ToString("F1") + "  " + ly.ToString("F1") + "   right post inner edge (x y) " + rx.ToString("F1") + "  " + ry.ToString("F1") + "   post to post X distance " + xdist.ToString("F2") + " in" + "   Y center distance " + ydist.ToString("F2") + " in\r\n";
								width_allow = (cdist * Math.Tan(SharedData.DEG_TO_RAD)) * 4;
								if (xdist <= ptp_dist - width_allow)
									lines += "Post to post X distance less then expected.";
								else if (xdist >= ptp_dist + width_allow)
									lines += "Post to post X distance greater then expected.";
								else if (cdist - ydist < ds_depth - DIST_VAR)
									lines += "Center Y distance less then expected.";
								else if (cdist - ydist > ds_depth + DIST_VAR)
									lines += "Center Y distance greater then expected.";
								else
									{
									rtn = true; 
									pangle = pa;
									lp_dx = lx;
									rp_dx = rx;
									center_dist = ydist;
									}
								}
							else
								lines += "Line calculation does not meet error threshold.";
							}
						else
							lines += "Could not determine indexes required to calculate data.";
						}
					}
				}
			else
				lines += "Cound not find reasonable center.";
			return (rtn);
		}



		private void LocationMessage(NavData.location loc)

		{
			Rectangle rect;
			string msg;
			
			rect = MotionMeasureProb.PdfRectangle();
			msg = loc.rm_name + "," + loc.coord.X + "," + loc.coord.Y + "," + loc.orientation + ","  + loc.loc_name + "," + loc.ls + "," + rect.Height + "," + rect.Width + "," + rect.X + "," + rect.Y;
			UiCom.SendLocMessage(msg);
		}




		private bool InitialAdjustMove()

		{
			bool rtn = false;
			ArrayList sdata = new ArrayList();
			double lp_dx = 0,rp_dx = 0,center_dist = 0;
			int pangle = 0;
			int turn,dist;
			string data_lines = "Initial LIDAR adjustment\r\n", rsp;
			NavData.location cloc = new NavData.location();
			bool data_saved = false;

			if (Rplidar.CaptureScan(ref sdata,false))
				{
				try
				{
				if (AdjustProcessLidarScan(MOVE_TO_TARGET_CLEARANCE, ref sdata,ref pangle,ref lp_dx,ref rp_dx,ref center_dist,ref data_lines))
					{
					dist = (int) (center_dist - OFFSET_FOR_SECOND_MOVE + SharedData.FLIDAR_OFFSET);
					if (dist >= ((MOVE_TO_TARGET_CLEARANCE - OFFSET_FOR_SECOND_MOVE)/2))
						{
						turn = (int) (pangle - (Math.Atan(((rp_dx + lp_dx)/2) / dist) * SharedData.RAD_TO_DEG));
						data_lines += "Initial LIDAR adjustment: turn " + turn + "° forward " + dist + " in";
						rsp = "";
						if (DetermineLocation(pangle,lp_dx,rp_dx,center_dist,ref cloc))
							NavData.SetCurrentLocation(cloc);
						if (Math.Abs(turn) >= 1)
							rtn = Turn(turn,ref rsp);
						else
							rtn = true;
						if (rtn)
							{
							rsp = SendCommand(SharedData.FORWARD_SLOW_NCC + " " + dist,5000);
							if (rsp.StartsWith("ok"))
								{
								initial_expect_dist = dist;
								rtn = true;
								}
							else
								{
								rtn = false;
								error = "Move failed: " + rsp.Substring(4);
								}
							}
						else
							error = "Turn failed: " + rsp;
						}
					else
						{
						error = "Move to target distance discrepancy indicates target location problems.";
						rtn = false;
						}
					}
				else
					{
					error = "Could not process LIDAR scan.";
					Log.LogEntry("Could not process LIDAR scan");
					Rplidar.SaveLidarScan(ref sdata,data_lines);
					data_saved = true;
					rtn = DockingRetry(0,12,1);
					}
				}

				catch(Exception ex)
				{
				error = "Exception: " + ex.Message;
				Log.LogEntry("Exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				rtn = false;
				}

				if (!data_saved)
					Rplidar.SaveLidarScan(ref sdata,data_lines);
				}
			else
				error = "Could not capture LIDAR scan";
			return(rtn);
		}



		private bool SecondAdjustMove()

		{
			bool rtn = false;
			ArrayList sdata = new ArrayList();
			double lp_dx = 0,rp_dx = 0,center_dist = 0;
			int pangle = 0;
			int turn,dist;
			string data_lines = "Second LIDAR adjustment\r\n", rsp;
			NavData.location cloc = new NavData.location(),lloc;
			bool data_saved = false;

			TurnToTarget();
			if (Rplidar.CaptureScan(ref sdata,false))
				{
				try
				{
				if (AdjustProcessLidarScan(OFFSET_FOR_SECOND_MOVE + ds_depth, ref sdata,ref pangle,ref lp_dx,ref rp_dx,ref center_dist,ref data_lines))
					{
					error = "";
					dist = (int) center_dist + SharedData.FLIDAR_OFFSET - OFFSET_FOR_FINAL_MOVE;
					turn = (int) -(Math.Atan(((rp_dx + lp_dx)/2) / dist) * SharedData.RAD_TO_DEG);
					data_lines += "Second adjust move: turn " + turn + "° forward " + dist + " in";
					rsp = "";
					lloc = NavData.GetCurrentLocation();
					if (DetermineLocation(pangle, lp_dx, rp_dx, center_dist, ref cloc))
						{
						NavData.SetCurrentLocation(cloc);
						if (initial_expect_dist != -1)
							{
							int adist;

							adist = NavCompute.DistancePtToPt(lloc.coord,cloc.coord);
							dist -= adist - initial_expect_dist;
							}
						}
					if (Math.Abs(turn) >= 1)
						rtn = Turn(turn, ref rsp);
					else
						rtn = true;
					if (rtn)
						{
						rsp = SendCommand(SharedData.FORWARD_SLOW_NCC + " " + dist, 5000);
						if (rsp.StartsWith("ok"))
							rtn = true;
						else
							{
							rtn = false;
							error = "Move failed: " + rsp.Substring(4);
							}
						}
					else
						error = "Turn failed: " + rsp;
					}
				else
					{
					error = "Could not process LIDAR scan.";
					Log.LogEntry("Could not process LIDAR scan");
					Rplidar.SaveLidarScan(ref sdata,data_lines);
					data_saved = true;
					rtn = DockingRetry(0,30,2);
					}
				}

				catch(Exception ex)
				{
				error = "Exception: " + ex.Message;
				Log.LogEntry("Exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				Log.LogEntry("Source: " + ex.Source);
				rtn = false;
				}

				if (!data_saved)
					Rplidar.SaveLidarScan(ref sdata,data_lines);
				}
			else
				error = "Could not capture LIDAR scan";
			return(rtn);
		}



		private bool DockProcessLidarScan(ref ArrayList sdata,ref int pangle,ref double center_dist,ref string lines)

		{
			bool rtn = true;
			double cdist = 0;
			int i,cindex = 0,lindex = 0,rindex,index,samples,angle;
			Rplidar.scan_data sd;
			double sx2 = 0, sxy = 0, sx = 0, sy = 0, m = 0,x,y;
			int adist,madist = 10,rcount;
			const int DIST_DIFF_LIMIT = 1;

			for (i = 0;i < sdata.Count;i++)
				{
				sd = (Rplidar.scan_data) sdata[i];
				if (sd.angle == 0)
					{
					cindex = i;
					cdist = sd.dist;
					break;
					}
				else if ((adist = NavCompute.AngularDistance(0,sd.angle)) < madist)
					{
					madist = adist;
					cindex = i;
					cdist = sd.dist;
					}
				}
			lines += "Center: index " + cindex + "   dist (in) " + cdist + "\r\n";
			lindex = (cindex - 10);
			if (lindex < 0)
				lindex += sdata.Count;
			for (i = 1;i < 11 ; i++)
				{
				index = cindex - i;
				if (index < 0)
					index += sdata.Count;
				sd = (Rplidar.scan_data)sdata[index];
				if (Math.Abs(sd.dist - cdist) > DIST_DIFF_LIMIT )
					{
					lindex = (index + 1) % sdata.Count;
					break;
					}
				}
			lines += "Left index: " + lindex + "\r\n";
			rcount = 20 - (i - 1);
			rindex = (cindex + rcount) % sdata.Count;
			for (i = 1; i < rcount + 1; i++)
				{
				index = (cindex + i) % sdata.Count;
				sd = (Rplidar.scan_data)sdata[index];
				if (Math.Abs(sd.dist - cdist) > DIST_DIFF_LIMIT)
					{
					rindex = index - 1;
					if (rindex < 0)
						rindex += sdata.Count;
					break;
					}
				}
			lines += "Right index: " + rindex + "\r\n";
			samples = 0;
			for (i = 0;;i++)
				{
				samples += 1;
				index = (lindex + i) % sdata.Count;
				sd = (Rplidar.scan_data)sdata[index];
				x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
				sx += x;
				sx2 += x * x;
				y = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD));
				sy += y;
				sxy += x * y;
				if (index == rindex)
					break;
				}
			m = ((samples * sxy) - (sx * sy)) / ((samples * sx2) - Math.Pow(sx, 2));
			pangle = (int) Math.Round(Math.Atan(m) * SharedData.RAD_TO_DEG);
			sd = (Rplidar.scan_data)sdata[cindex];
			angle = sd.angle + pangle;
			y = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD));
			center_dist = y;
			lines += "Calculated data: perp. angle " + pangle + "°" + "   wall distance " + center_dist.ToString("F2") + " in\r\n";
			return (rtn);
		}



		private bool DetermineLocation(int pa,double cdist,ref NavData.location loc)

		{
			bool rtn = false;
			NavData.location cloc;
			NavData.recharge recharge_station;

			cloc = NavData.GetCurrentLocation();
			recharge_station = NavData.GetRechargeStation(cloc.rm_name);
			if (!recharge_station.coord.IsEmpty)
				{
				rtn = true;
				if (recharge_station.direction == 270)
					{
					cloc.orientation = 270 + pa;
					cloc.coord.Y = recharge_station.coord.Y;
					cloc.coord.X = (int) Math.Round(cdist + SharedData.FLIDAR_OFFSET);
					cloc.ls = NavData.LocationStatus.VERIFIED;
					MotionMeasureProb.CompleteLocalize(new MotionMeasureProb.Pose(cloc.coord, cloc.orientation)); 
					}
				else if (recharge_station.direction == 180)
					{
					cloc.orientation = 180 + pa;
					cloc.coord.X = recharge_station.coord.X;
					cloc.coord.Y = NavData.rd.rect.Height - (int) Math.Round(cdist + SharedData.FLIDAR_OFFSET);
					cloc.ls = NavData.LocationStatus.VERIFIED;
					MotionMeasureProb.CompleteLocalize(new MotionMeasureProb.Pose(cloc.coord, cloc.orientation)); 
					}
				else if (recharge_station.direction == 90)
					{
					cloc.orientation = 90 + pa;
					cloc.coord.Y = recharge_station.coord.Y;
					cloc.coord.X = NavData.rd.rect.Width - (int) Math.Round(cdist + SharedData.FLIDAR_OFFSET);
					cloc.ls = NavData.LocationStatus.VERIFIED;
					MotionMeasureProb.CompleteLocalize(new MotionMeasureProb.Pose(cloc.coord, cloc.orientation)); 
					}
				else if (recharge_station.direction == 0)
					{
					cloc.orientation = pa;
					if (cloc.orientation < 0)
						cloc.orientation += 360;
					cloc.coord.X = recharge_station.coord.X;
					cloc.coord.Y = (int) Math.Round(cdist + SharedData.FLIDAR_OFFSET);
					cloc.ls = NavData.LocationStatus.VERIFIED;
					MotionMeasureProb.CompleteLocalize(new MotionMeasureProb.Pose(cloc.coord, cloc.orientation)); 
					}
				else
					rtn = false;
				if (rtn )
					loc = cloc;
				}
			if (rtn)
				LocationMessage(cloc);
			return(rtn);
		}



		private bool DockRetryL1()

		{
			bool rtn = false;

			if (TurnToTarget())
				if (MoveToTarget())
					rtn = InitialAdjustMove();
			return(rtn);
		}



		private bool DockRetryL2()

		{
			bool rtn = false;

			if (DockRetryL1())
				rtn = SecondAdjustMove();
			return (rtn);
		}



		private bool DockRetryL3()

		{
			bool rtn = false;

			if (DockRetryL2())
				rtn = DockingMove();
			return (rtn);
		}



		private bool DockingRetry(int tangle,int dist,int level)

		{
			bool rtn = false;
			int sdist,direc,ldist;
			string rsp;
			NavData.location clocation;
			double cdist = 0;

			docking_retrys += 1;
			if (docking_retrys <= RETRY_LIMIT)
				{
				if (AutoRobotControl.Turn.TurnAngle(tangle))
					{
					clocation = NavData.GetCurrentLocation();
					direc = (clocation.orientation - tangle) % 360;
					if (direc < 0)
						direc += 360;
					clocation.orientation = direc;
					clocation.coord = NavCompute.MapPoint(AutoRobotControl.Turn.TurnImpactPosition(tangle), clocation.orientation, clocation.coord);
					clocation.ls = NavData.LocationStatus.DR;
					NavData.SetCurrentLocation(clocation);
					sdist = MotionControl.GetSonarReading(SharedData.RobotLocation.REAR);
					if (LS02CLidar.RearClearence(ref cdist, SharedData.ROBOT_WIDTH + MIN_SIDE_CLEAR))
						{
						ldist = (int)Math.Round(cdist);
						sdist = Math.Min(sdist,ldist);
						}
					if (sdist >= dist + SharedData.REAR_SONAR_MIN_CLEARANCE)
						{
						rsp = SendCommand(SharedData.BACKWARD + " " + dist,20000);
						if (rsp.StartsWith("ok"))
							{
							clocation.coord = NavCompute.MapPoint(new Point(0, -dist), clocation.orientation, clocation.coord);
							clocation.ls = NavData.LocationStatus.DR;
							clocation.loc_name = "";
							NavData.SetCurrentLocation(clocation);
							if (level == 1)
								rtn = DockRetryL1();
							else if (level == 2)
								rtn = DockRetryL2();
							else if (level == 3)
								rtn = DockRetryL3();
							}
						else
							{
							Log.LogEntry("DockingRetry: backward move failed with response " + rsp);
							error += "  DockingRetry: backward move failed with response " + rsp;
							}
						}
					else
						{
						Log.LogEntry("DockingRetry: insufficent space to back up " + dist);
						error += "  DockingRetry: insufficent space to back up " + dist;
						}
					}
				else
					{
					Log.LogEntry("DockingRetry: could not make turn to be perpendicular to the back wall.");
					error += "  DockingRetry: could not make turn to be perpendicular to the back wall.";
					}
				}
			else
				{
				Log.LogEntry("DockingRetry: retry limit exceeded.");
				error += "  DockingRetry: retry limit exceeded.";
				}
			return(rtn);
		}



		private bool DockingMove()

		{
			bool rtn = false;
			ArrayList sdata = new ArrayList();
			string data_lines = "Docking\r\n", rsp = "";
			double dist = 0;
			int pangle = 0;
			NavData.location cloc = new NavData.location();

			if (Rplidar.CaptureScan(ref sdata,false))
				{
				if (DockProcessLidarScan(ref sdata,ref pangle,ref dist,ref data_lines))
					{
					if (DetermineLocation(pangle,dist,ref cloc))
						NavData.SetCurrentLocation(cloc);
					dist = dist - ds_depth + sensor_offset;
					if (pangle <= 5)
						{
						data_lines += "Docking move: turn " + pangle + "° forward " + dist + " in";
						if (Math.Abs(pangle) >= 1)
							rtn = Turn(pangle, ref rsp);
						else
							rtn = true;
						if (rtn)
							{
							rsp = SendCommand(SharedData.FORWARD_DOCK +  " " + dist.ToString("F0"), 5000);
							if (rsp.StartsWith("ok"))
								rtn = true;
							else
								{
								rtn = false;
								error = "Move failed: " + rsp.Substring(4);
								}
							}
						else
							error = "Turn failed: " + rsp;
						Rplidar.SaveLidarScan(ref sdata, data_lines);
						}
					else
						{
						data_lines += "Docking move parametrers (turn " + pangle + "°   forward " + dist + " in) not within limits; will attempt retry";
						Rplidar.SaveLidarScan(ref sdata, data_lines);
						rtn = DockingRetry(pangle, (int)Math.Round(MOVE_TO_TARGET_CLEARANCE - (dist + ds_depth - sensor_offset) + 12),3);
						}
					}
				else
					{
					error = "Could not process LIDAR scan.";
					Rplidar.SaveLidarScan(ref sdata, data_lines);
					}
				}
			else
				error = "Could not capture LIDAR scan";
			return (rtn);
		}



		private void ExecThread()

		{
			ArrayList sdata = new ArrayList();

			sw.Start();
			docking_retrys = 0;
			if (run && TurnToTarget())
				{
				Thread.Sleep(1000);
				if (run && MoveToTarget())
					{
					if (run && InitialAdjustMove())
						if (run && SecondAdjustMove())
							if (run && DockingMove())
								{
								Thread.Sleep(1000);
								if (!AtRechargeDock(ref dloc))
									error = "Could not confirm docking.";
								}
					}
				}
			sw.Reset();
		}



		private void TestExecThread()

		{
			ArrayList sdata = new ArrayList();

			sw.Start();
			if (TurnToTarget())
				MoveToTarget();
			sw.Reset();
		}



		private bool DockedProcessLidarScan(ref ArrayList sdata,ref int pangle,ref double center_dist,ref double width,ref string lines)

		{
			bool rtn = false;
			double cdist = 0,mdist,xdist;
			int i, cindex = 0, lpindex = -1, rpindex = -1, index, samples, pa, lwindex = -1, mangle = 180,ad;
			Rplidar.scan_data sd;
			double sx2 = 0, sxy = 0, sx = 0, sy = 0, m = 0, b = 0, y, x,lx,rx,angle;

			for (i = 0;i < sdata.Count;i++)
				{
				sd = (Rplidar.scan_data) sdata[i];
				if ((sd.angle == 0) || (sd.angle == 360))
					{
					cindex = i;
					cdist = sd.dist;
					break;
					}
				if ((ad = NavCompute.AngularDistance(0,sd.angle)) < mangle)
					{
					cindex = i;
					cdist = sd.dist;
					mangle = ad;
					}
				}
			lines += "Center: index " + cindex + "   dist (in) " + cdist + "\r\n";
			mdist = cdist;
			for (i = 1;;i++)
				{
				index = cindex - i;
				if (index < 0)
					index += sdata.Count;
				sd = (Rplidar.scan_data)sdata[index];
				if (sd.angle <= 270)
					break;
				if (sd.dist < mdist)
					{
					lpindex = index;
					mdist = sd.dist;
					}
				}
			lines += "Left post inner edge index: " + lpindex + "\r\n";
			if (lpindex >= 0)
				{
				mdist = cdist;
				for (i = 1;;i++)
					{
					index = (cindex + i) % sdata.Count;
					sd = (Rplidar.scan_data)sdata[index];
					if (sd.angle >= 90)
						break;
					if (sd.dist < mdist)
						{
						rpindex = index;
						mdist = sd.dist;
						}
					}
				lines += "Right post inner edge index: " + rpindex + "\r\n";
				if (rpindex >= 0)
					{
					samples = 0;
					lwindex = cindex - 10;
					if (lwindex < 0)
						lwindex += sdata.Count;
					for (i = 0;i <= 20;i++)
						{
						index = (lwindex + i) % sdata.Count;
						sd = (Rplidar.scan_data)sdata[index];
						samples += 1;
						x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
						sx += x;
						sx2 += x * x;
						y = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD));
						sy += y;
						sxy += x * y;
						}
					m = ((samples * sxy) - (sx * sy)) / ((samples * sx2) - Math.Pow(sx, 2));
					b = (sy / samples) - ((m * sx) / samples);
					pa = (int) Math.Round(Math.Atan(m) * SharedData.RAD_TO_DEG);
					sd = (Rplidar.scan_data)sdata[lpindex];
					angle = sd.angle + pa;
					lx = (sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD));
					sd = (Rplidar.scan_data)sdata[rpindex];
					angle = sd.angle + pa;
					rx = (sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD));
					if (lx < 0)
						xdist = rx - lx;
					else
						xdist = rx + lx;
					lines += "Calculated data: perp. angle " + pa + "°      post to post X distance " + xdist.ToString("F2") + " in" + "    Center distance " + cdist.ToString("F2") + " in\r\n";
					rtn = true; 
					pangle = pa;
					center_dist = cdist;
					width = xdist;
					}
				}
			return (rtn);
		}




		public bool StartDocking(NavData.recharge rc_station,ref NavData.location cloc,bool test)

		{
			bool rtn = false;

			if (initialized && (Kinect.nui != null) && Kinect.nui.IsRunning && MotionControl.Connected() && Rplidar.Connected())
				{
				blobs.Clear();
				ts.Clear();
				rangle.Clear();
				dist.Clear();
				nblobs.Clear();
				ds_depth = rc_station.depth;
				ptp_dist = rc_station.ptp_width;
				sensor_offset = rc_station.sensor_offset;
				error = "";
				if (test)
					exec = new Thread(TestExecThread);
				else
					exec = new Thread(ExecThread);
				run = true;
				exec.Start();
				exec.Join();
				if (error.Length == 0)
					{
					rtn = true;
					cloc = dloc;
					}
				}
			else
				error = "Do not have resources need to perform operation.";
			return(rtn);
		}



		public void StopDocking()

		{
			run = false;
		}



		public string LastError()

		{
			return(error);
		}



		public bool AtRechargeDock(ref NavData.location cloc)

		{
			bool rtn = false;
			NavData.recharge recharge_station;
			ArrayList sdata = new ArrayList();
			int pa = -1;
			double cdist = -1,width = -1,ptp_dist,ds_depth;
			string lines = "Docked check\r\n";
			bool sc_capture = false;
			int tries = 0;

			Log.LogEntry("AtRechargeDock");
			recharge_station = NavData.GetRechargeStation(NavData.rd.name);
			if (!recharge_station.coord.IsEmpty)
				{
				if (MotionControl.Docked())
					{
					do
						{
						tries += 1;
						if ((sc_capture = Rplidar.CaptureScan(ref sdata,false)))
							{
							if (DockedProcessLidarScan(ref sdata,ref pa,ref cdist,ref width,ref lines))
								{
								ptp_dist = recharge_station.ptp_width;
								ds_depth = recharge_station.depth;
								if ((width > ptp_dist - 1) && (width < ptp_dist + 1) && (cdist >= ds_depth - 1))
									{
									rtn = true;
									Log.LogEntry("At recharge dock confirmed.");
									lines += "At recharge dock confirmed.\r\n";
									cloc.rm_name = NavData.rd.name;
									if (recharge_station.direction == 270)
										{
										cloc.orientation = 270 + pa;
										cloc.coord.Y = recharge_station.coord.Y;
										cloc.coord.X = (int) Math.Round(cdist + SharedData.FLIDAR_OFFSET);
										cloc.ls = NavData.LocationStatus.VERIFIED;
										MotionMeasureProb.CompleteLocalize(new MotionMeasureProb.Pose(cloc.coord, cloc.orientation)); 
										}
									else if (recharge_station.direction == 180)
										{
										cloc.orientation = 180 + pa;
										cloc.coord.X = recharge_station.coord.X;
										cloc.coord.Y = NavData.rd.rect.Height - (int) Math.Round(cdist + SharedData.FLIDAR_OFFSET);
										cloc.ls = NavData.LocationStatus.VERIFIED;
										MotionMeasureProb.CompleteLocalize(new MotionMeasureProb.Pose(cloc.coord, cloc.orientation)); 
										}
									else if (recharge_station.direction == 90)
										{
										cloc.orientation = 90 + pa;
										cloc.coord.Y = recharge_station.coord.Y;
										cloc.coord.X = NavData.rd.rect.Width - (int) Math.Round(cdist + SharedData.FLIDAR_OFFSET);
										cloc.ls = NavData.LocationStatus.VERIFIED;
										MotionMeasureProb.CompleteLocalize(new MotionMeasureProb.Pose(cloc.coord, cloc.orientation)); 
										}
									else if (recharge_station.direction == 0)
										{
										cloc.orientation = pa;
										if (cloc.orientation < 0)
											cloc.orientation += 360;
										cloc.coord.X = recharge_station.coord.X;
										cloc.coord.Y = (int) Math.Round(cdist + SharedData.FLIDAR_OFFSET);
										cloc.ls = NavData.LocationStatus.VERIFIED;
										MotionMeasureProb.CompleteLocalize(new MotionMeasureProb.Pose(cloc.coord, cloc.orientation)); 
										}
									else
										rtn = false;
									}
								else
									{
									Log.LogEntry("LIDAR scan data does not match:  width " + width.ToString("F2") + " in., center distance " + cdist.ToString("F1") + " in");
									lines += "LIDAR scan data does not match:  width " + width.ToString("F2") + " in., center distance " + cdist.ToString("F1") + " in\r\n";
									}
								}
							Rplidar.SaveLidarScan(ref sdata, lines);
							NavData.SetCurrentLocation(cloc);
							}
						}
					while(!sc_capture && (tries < 2));
					}
				else
					Log.LogEntry("Docked switch not set.");
				}
			else
				Log.LogEntry("Could not find recharge station information.");
			if (rtn)
				LocationMessage(cloc);
			return(rtn);
		}



		public string LastDataFile()

		{
			return(last_data_file);
		}



		public void ModifyPIDGains(double pgain,double igain,double dgain)

		{
			ttpgain = pgain;
			ttigain = igain;
			ttdgain = dgain;
		}



		public RechargeDock()

		{
			initialized = ReadRefParameters();
		}


		}
	}
