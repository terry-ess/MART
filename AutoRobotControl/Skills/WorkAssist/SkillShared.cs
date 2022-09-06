using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using AutoRobotControl;
using Microsoft.Kinect;
using MathNet.Numerics.LinearAlgebra.Double;
using Coding4Fun.Kinect.WinForm;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Blob;


namespace Work_Assist
	{
	static class SkillShared
		{

		public enum work_space_arrange { NONE, SAME_SIDE, OPPOSITE_SIDE, EDGE };

		public const int MIN_SIDE_CLEAR = 1;
		public const int MIN_KINECT_FLOOR_DIST = 17;	//closest distance at floor level beyond arm perch with Kinect tilt of -55
		public const int MIN_KINECT_LIDAR_FLOOR_DIST = 13; //closest distance 18 in above floor with Kinect tilt of -55
		public const int TOP_MAGRIN = 8;
		public const int ARM_PERCH_WIDTH = 4;
		public const int PERSON_RADIUS = 12;
		public const int MAX_FINAL_MOVE_DIST = 24;
		public const double HEIGHT_CORRECT = 0;
		public const int MIN_DIST_LIMIT = 10;  //eliminates the Arm perch
		public const int HEAD_CENTER_DIST = 2;
		public const int MOVE_KINECT_TILT = -55;
		public const int MOVE_KINECT_TILT_CORRECT = -4;

		private const int MONITOR_WAIT = 100;
		private const double MIN_HEIGHT_LIMIT = 2;
		private const double M_TO_IN = 1000 * SharedData.MM_TO_IN;
		private const double ROBOT_WIDTH = ((double) SharedData.ROBOT_WIDTH / 2 + .5);	//.5 rather then 1 because of slight aligment diff between Kinect & front LIDAR

		public struct lidar_obstacle
		{
			public Rplidar.scan_data sd;
			public int indx;
		};

		public struct Dpt
		{
		public double X;
		public double Y;

		public Dpt(double x,double y)

		{
			this.X = x;
			this.Y = y;
		}


		public override string ToString()

		{
			string rtn = "";

			rtn = "(" + this.X.ToString("F3") + ", " + this.Y.ToString("F3") + ")";
			return(rtn);
		}

		};

		public struct work_space
		{
			public string name;
			public string room;
			public bool existing_area;
			public work_space_arrange arrange;
			public double top_height;
			public double front_edge_dist;
			public double side_edge_dist;
			public int edge_perp_direct;
			public Point center_workspace_edge;
			public MotionMeasureProb.Pose work_loc;
			public NavData.location initial_robot_loc;
			public SharedData.RobotLocation side;
			public Point person_coord;
			public int prime_direct;
			public bool tight_quarters;

			public void LogWSD()

			{
				Log.LogEntry("Work space data:");
				Log.LogEntry("  name - " + name);
				Log.LogEntry("  room - " + room);
				Log.LogEntry("  existing area: " + existing_area);
				Log.LogEntry("  arrange - " + arrange);
				Log.LogEntry("  side - " + side);
				Log.LogEntry("  person coord (RmC) - " + person_coord);
				Log.LogEntry("  edge perp direct - " + edge_perp_direct);
				Log.LogEntry("  top height - " + top_height.ToString("F2"));
				Log.LogEntry("  prime direct - " + prime_direct);
				Log.LogEntry("  front edge distance - " + front_edge_dist.ToString("F2"));
				Log.LogEntry("  side edge distance - " + side_edge_dist.ToString("F2"));
				Log.LogEntry("  work location (RmC) - " + work_loc.coord);
				Log.LogEntry("  center workspace edge - " + center_workspace_edge);
				Log.LogEntry("  initial robot coord (RmC) - " + initial_robot_loc);
			}
		};


		public static byte[,] ws_map;
		public static PersonDetect pd = new PersonDetect();
		public static Move mov = new AutoRobotControl.Move();
		public static work_space wsd = new work_space();
		public static DepthImagePixel[] depthdata = new DepthImagePixel[Kinect.nui.DepthStream.FramePixelDataLength];
		public static DepthImagePoint[] dips = new DepthImagePoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];
		public static byte[] videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
		public static SkeletonPoint[] sips = new SkeletonPoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];

		public static bool at_work_loc = false;

		private static byte[] bdata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
		public static CvBlobs blobs = new CvBlobs();
		public static IplImage pic = new IplImage(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight, BitDepth.U8, 3);
		public static IplImage gs = new IplImage(pic.Size, BitDepth.U8, 1);
		public static IplImage img = new IplImage(pic.Size, BitDepth.F32, 1);
		public static ArrayList al = new ArrayList();


		public static bool AvgLast(double value,ref double result)

		{
			const int Q_SIZE = 5;
			bool rtn = false;
			double total = 0;
			int i;

			al.Add(value);
			if (al.Count > Q_SIZE)
				al.RemoveAt(0);
			if (al.Count == Q_SIZE)
				{
				for (i = 0;i < Q_SIZE;i++)
					total += (double) al[i];
				rtn = true;
				result = total/Q_SIZE;
				}
			return(rtn);
		}



		public static int PrimeDirection(int direct)

		{
			int pdirect = 400;

			if ((direct > 315) || (direct < 45))
				pdirect = 0;
			else if ((direct > 45) && (direct < 135))	
				pdirect = 90;
			else if ((direct > 135) && (direct < 225))
				pdirect = 180;
			else if ((direct > 225) && (direct < 315))
				pdirect = 270;
			return(pdirect);
		}


		public static void OutputSpeech(string output)

		{
			Speech.Speak(output);
			Log.LogEntry(output);
		}



		public static string SendCommand(string command,int timeout_count)

		{
			string rsp = "";

			Log.LogEntry(command);
			rsp = AutoRobotControl.MotionControl.SendCommand(command,timeout_count);
			Log.LogEntry(rsp);
			return(rsp);
		}



		public static bool SaveDipsData(string title,DepthImagePoint[] dips)

		{
			string fname;
			DateTime now = DateTime.Now;
			BinaryWriter bw;
			int i;
			bool rtn = false;

			fname = Log.LogDir() + title + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".bin";
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



		public static bool RecordWorkSpaceData(string title,bool wsd_write = true)

		{
			bool rtn = false;
			Bitmap bm;
			string fname;
			DateTime now = DateTime.Now;
			TextWriter tw;
			ArrayList scan = new ArrayList();

			if (Kinect.GetColorFrame(ref videodata, 40) && (Kinect.GetDepthFrame(ref depthdata, 40)))
				{
				bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, depthdata, dips);
				fname = Log.LogDir() + title + " pic " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				bm.RotateFlip(RotateFlipType.Rotate180FlipY);
				bm.Save(fname, ImageFormat.Jpeg);
				Log.LogEntry("Saved " + fname);
				SaveDipsData(title + " depth data ",dips);
				if (Rplidar.CaptureScan(ref scan, true))
					Rplidar.SaveLidarScan(ref scan, title);
				AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.FRONT);
				if (wsd_write)
					{
					fname = Log.LogDir() + title + " work space data " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".txt";
					tw = File.CreateText(fname);
					if (tw != null)
						{
						tw.WriteLine(title + " work space data");
						tw.WriteLine(now.ToShortDateString() + "  " + now.ToShortTimeString());
						tw.WriteLine();
						tw.WriteLine("Edge distance: " + wsd.front_edge_dist);
						tw.WriteLine("Top height: " + wsd.top_height);
						tw.WriteLine("Edge perp. direction: " + wsd.edge_perp_direct);
						tw.WriteLine("At work location: " + at_work_loc);
						tw.WriteLine("Person location: " + SpeakerData.Face.rm_location);
						tw.WriteLine("Kinect tilt: " + HeadAssembly.TiltAngle());
						tw.WriteLine("Kinect pan: " + HeadAssembly.PanAngle());
						tw.Close();
						Log.LogEntry("Saved: " + fname);
						}
					}
				rtn = true;
				}
			return (rtn);
		}



		public static string ObsArrayListToString(ArrayList al)

		{
			string stg = "";
			int i;
			lidar_obstacle lo;

			for (i = 0;i < al.Count;i++)
				{
				lo = (lidar_obstacle) al[i];
				stg += lo.indx + "  ";
				}
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



		public static string SaveMap(string title,Bitmap map)

		{
			string fname;

			fname = Log.LogDir() + "Work space " + title + " map " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".bmp";
			map.Save(fname, System.Drawing.Imaging.ImageFormat.Bmp);
			Log.LogEntry("Saved " + fname);
			return(fname);
		}



		public static string SaveMap(string title,byte[,] map)

		{
			Bitmap bm;

			bm = SkillShared.MapToBitmap(map);
			return(SaveMap(title,bm));
		}



		public static byte[,] ScanMap(ArrayList scan,NavData.location pose)

		{
			int i,j,height, width,angle;
			Rplidar.scan_data sd;
			double x,y;
			byte[,] map;

			height = NavData.rd.rect.Height;
			width = NavData.rd.rect.Width;
			map = new byte[width + 1,height + 1];
			for (i = 0;i < width + 1;i++)
				for (j = 0;j < height + 1;j++)
					map[i,j] = (byte) AutoRobotControl.Room.MapCode.CLEAR;
			for (i = 0; i < scan.Count; i++)
				{
				sd = (Rplidar.scan_data)scan[i];
				angle = (sd.angle + pose.orientation) % 360;
				y = pose.coord.Y - (sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD) + SharedData.FLIDAR_OFFSET);
				x = sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD) + pose.coord.X;

				try
				{
				map[(int) Math.Round(x), (int) Math.Round(y)] = (byte)AutoRobotControl.Room.MapCode.BLOCKED;
				}

				catch(Exception)
				{
				}

				}
			return(map);
		}



		public static void AddCircleToMap(ref byte[,] map,Point center,int radius)

		{
			int i,j;

			for (i = - radius;i < radius + 1;i++)
				for (j = -radius;j < radius + 1;j++)
					if (Math.Sqrt(Math.Pow(i,2) + Math.Pow(j,2)) <= radius)
						{
						try
						{
						map[i + center.X,j + center.Y] = (byte) AutoRobotControl.Room.MapCode.BLOCKED;
						}

						catch(Exception)
						{
						}

						}
		}




		public static void DisplayWorkSpace(Point cloc, Pen pen,Graphics g,int wsdirect)

		{
			Point rloc = new Point();
			int w = 0,h = 0;

			if ((wsdirect >= 45) && (wsdirect < 135))
				{
				rloc.X = cloc.X;
				rloc.Y = cloc.Y - 19;
				w = 10;
				h = 38;
				}
			else if ((wsdirect >= 135) && (wsdirect < 225))
				{
				rloc.X = cloc.X - 19;
				rloc.Y = cloc.Y - 10;
				w = 38;
				h = 10;
				}
			else if ((wsdirect >= 225) && (wsdirect < 315))
				{
				rloc.X = cloc.X - 10;
				rloc.Y = cloc.Y - 19;
				w = 10;
				h = 38;
				}
			else
				{
				rloc.X = cloc.X - 19;
				rloc.Y = cloc.Y - 10;
				w = 38;
				h = 10;
				}
			g.DrawRectangle(pen, rloc.X, rloc.Y, w,h);
		}



		public static int FindObstacles(int shift_angle,int dist,ArrayList sdata,double side_width,double side_clear,ref ArrayList obs)

		{
			int i,angle,min_dist;
			double x,y,dx,dy;
			Rplidar.scan_data sd;
			double width_band;
			lidar_obstacle ld = new lidar_obstacle();

			Log.LogEntry("FindObstacles: " + shift_angle + "  " + dist + "  " + (side_width + side_clear));
			if (dist != -1)
				min_dist = dist + 1;
			else
				min_dist = 236;
			width_band = side_width + side_clear;
			dx = (SharedData.FRONT_PIVOT_PT_OFFSET + SharedData.FLIDAR_OFFSET) * Math.Sin(shift_angle * SharedData.DEG_TO_RAD);
			dy = ((SharedData.FRONT_PIVOT_PT_OFFSET + SharedData.FLIDAR_OFFSET) * Math.Cos(shift_angle * SharedData.DEG_TO_RAD)) - SharedData.FRONT_PIVOT_PT_OFFSET;
			for (i = 0;i < sdata.Count;i++)
				{
				sd = (Rplidar.scan_data) sdata[i];
				angle = (sd.angle - shift_angle) %360;
				if (angle < 0)
					angle += 360;
				y = sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD) + dy;
				x = sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD) - dx;
				if (dist == -1)
					{
					if ((y > 0) && (Math.Abs(x) <= width_band))
						{
						ld.sd = sd;
						ld.indx = i;
						obs.Add(ld);
						if (y < min_dist)
							min_dist = (int) Math.Ceiling(y);
						}
					}
				else
					{
					if ((y > 0) && (y <= dist) && (Math.Abs(x) <= width_band))
						{
						ld.sd = sd;
						ld.indx = i;
						obs.Add(ld);
						if (y < min_dist)
							min_dist = (int) Math.Ceiling(y);
						}
					}
				}
			Log.LogEntry("Min obstacle distance: " + min_dist);
			Rplidar.SaveLidarScan(ref sdata, "FindObstacles: " + shift_angle + "  " + dist + "\r\nObstacles: [" + obs.Count + "]   " + ObsArrayListToString(obs));
			return (min_dist);
		}



		public static int FindObstacles(int shift_angle, int dist, ArrayList sdata, double side_clear, ref ArrayList obs)

		{
			return(FindObstacles(shift_angle,dist,sdata,(double) SharedData.ROBOT_WIDTH/2,side_clear,ref obs));
		}



		public static bool ObstacleAdjustAngle(ArrayList sdata,ArrayList obs,int odist,int shift_angle,double side_clear,ref int angle)

		{
			bool rtn = false,right_obs = false,left_obs = false,center_obs = false;
			int i,sangle, tangle, maxtangle = 0;
			Rplidar.scan_data sd;
			double x,y,mtax = 0,dx,dy;
			ArrayList obs2 = new ArrayList();
			lidar_obstacle ld;

			Log.LogEntry("ObstacleAdjustAngle: " + obs.Count + " obstacles, " + shift_angle + " shift angle");
			dx = (SharedData.FRONT_PIVOT_PT_OFFSET + SharedData.FLIDAR_OFFSET) * Math.Sin(shift_angle * SharedData.DEG_TO_RAD);
			dy = ((SharedData.FRONT_PIVOT_PT_OFFSET + SharedData.FLIDAR_OFFSET) * Math.Cos(shift_angle * SharedData.DEG_TO_RAD)) - SharedData.FRONT_PIVOT_PT_OFFSET;
			for (i = 0;i < obs.Count;i++)
				{
				ld = (lidar_obstacle) obs[i];
				sd = ld.sd;
				if (sd.dist < odist)
					{
					sangle = (sd.angle - shift_angle) % 360;
					if (sangle < 0)
						sangle += 360;
					x = sd.dist * Math.Sin(sangle * SharedData.DEG_TO_RAD) - dx;
					if (x > 0)
						right_obs = true;
					else if (x < 0)
						left_obs = true;
					else
						center_obs = true;
					y = Math.Cos(sangle * SharedData.DEG_TO_RAD) * sd.dist + dy;
					tangle = (int) Math.Ceiling(Math.Abs(Math.Atan((((double)SharedData.ROBOT_WIDTH / 2) + side_clear - Math.Abs(x)) / y) * SharedData.RAD_TO_DEG));
					if (tangle > maxtangle)
						{
						maxtangle = tangle;
						mtax = x;
						}
					}
				}
			if (left_obs && right_obs)
				{
				angle = 0;
				rtn = false;
				}
			else if (!left_obs && !right_obs && !center_obs)
				{
				angle = 0;
				FindObstacles(shift_angle, odist, sdata, side_clear, ref obs2);
				if (obs2.Count == 0)
					rtn = true;
				else
					rtn = false;
				}
			else
				{
				if (right_obs)
					FindObstacles(shift_angle - maxtangle, odist, sdata, side_clear, ref obs2);
				else
					FindObstacles(shift_angle + maxtangle, odist, sdata, side_clear, ref obs2);
				if (obs2.Count == 0)
					{
					angle = maxtangle;
					if (left_obs)
						angle *= -1;
					rtn = true;
					}
				else
					{
					if (right_obs)
						FindObstacles(shift_angle - (maxtangle + 1), odist, sdata, side_clear, ref obs2);
					else
						FindObstacles(shift_angle + (maxtangle + 1), odist, sdata, side_clear, ref obs2);
					if (obs2.Count == 0)
						{
						angle = maxtangle + 1;
						if (left_obs)
							angle *= -1;
						rtn = true;
						}
					else
						{
						angle = 0;
						rtn = false;
						}
					}
				}
			Log.LogEntry("Obstacle adjust angle: " + rtn + "  " + angle + "  " + maxtangle);
			return (rtn);
		}



		public static int RemoveObstacles(ref ArrayList sdata,ref ArrayList obs,int rdist,int mdist)

		{
			SkillShared.lidar_obstacle lo;
			int i,min_dist = rdist,dist;

			for (i = obs.Count - 1; i >= 0; i--)
				{
				lo = (SkillShared.lidar_obstacle) obs[i];
				dist = (int) Math.Ceiling(lo.sd.dist * Math.Cos(lo.sd.angle * SharedData.DEG_TO_RAD));
				if ((dist <= rdist) && (dist >= mdist))
					{
					sdata.RemoveAt(lo.indx);
					Log.LogEntry("Removed LIDAR scan data item: " + lo.indx);
					obs.RemoveAt(i);
					Log.LogEntry("Removed obstacle: " + i);
					}
				else
					if (dist < min_dist)
						min_dist = dist;
				}
			return(min_dist);
		}



		public static int RemoveLidarAnomalies(int dist,int ldist,ref ArrayList obs,ref ArrayList sdata,ref int no_kobs,bool tilt)

		{
			int kdist = 0;
			string msg,reply;

			KinectFrontClear(dist + SharedData.ARM_PERCH_OFFSET + 1, ref kdist,ref no_kobs,tilt);
			Log.LogEntry("Front clearence Kinect: " + kdist);
			if ((ldist < kdist) && (obs.Count > 0))
				{
				ldist = RemoveObstacles(ref sdata, ref obs, kdist, MIN_KINECT_LIDAR_FLOOR_DIST);
				if (obs.Count == 0)
					ldist = kdist;
				}
			if (ldist < MIN_KINECT_LIDAR_FLOOR_DIST)
				{
				msg = obs.Count + " possible LIDAR anomalies have been detected " + ldist + " inches ahead of me.  Is my path clear?";
				reply = AutoRobotControl.Speech.Conversation(msg, "responseyn", 5000, true);
				if (reply == "yes")
					{
					RemoveObstacles(ref sdata,ref obs,kdist,0);
					ldist = kdist;
					}
				}
			return (Math.Min(ldist,kdist));
		}



		public static Arm.Loc3D DPtLocation(int row, int col, double tilt_correct)

		{
			int pixel;
			double ray, rax, depth, x;
			Arm.Loc3D loc;


			pixel = (row * Kinect.nui.ColorStream.FrameWidth) + (Kinect.nui.ColorStream.FrameWidth - col - 1);
			if (SkillShared.dips[pixel].Depth > 0)
				{
				depth = Kinect.CorrectedDistance(SkillShared.dips[pixel].Depth * SharedData.MM_TO_IN);
				ray = Kinect.VideoVerDegrees((Kinect.nui.ColorStream.FrameHeight / 2) - row);
				rax = Kinect.VideoHorDegrees(col - (Kinect.nui.ColorStream.FrameWidth / 2));
				x = depth * Math.Tan(rax * SharedData.DEG_TO_RAD);
				loc = Arm.MapKCToRC(x, depth, tilt_correct, ray);
				}
			else
				loc = new Arm.Loc3D(0, 0, 0);
			return (loc);
		}



		public static Arm.Loc3D PtLocation(int row,int col,double ktilt_correct)

		{
			Arm.Loc3D loc = new Arm.Loc3D();
			int pixel;
			DenseMatrix mat;
			DenseVector result,vec;
			double tilt,kfdd;

			pixel = (row * Kinect.nui.ColorStream.FrameWidth) + (Kinect.nui.ColorStream.FrameWidth - col - 1);
			loc.x = -(sips[pixel].X * M_TO_IN);
			tilt = HeadAssembly.TiltAngle();
			tilt += ktilt_correct;
			kfdd = Arm.KinectForwardDist(tilt);
			mat = DenseMatrix.OfArray(new[,] { { Math.Cos(-tilt * SharedData.DEG_TO_RAD), -Math.Sin(-tilt * SharedData.DEG_TO_RAD) }, { Math.Sin(-tilt * SharedData.DEG_TO_RAD), Math.Cos(-tilt * SharedData.DEG_TO_RAD) } });
			vec = new DenseVector(new[] {(double) sips[pixel].Z,(double) sips[pixel].Y});
			result = vec * mat;
			loc.z = (result.Values[0] * M_TO_IN) + kfdd;
			loc.y = result.Values[1] * M_TO_IN;
			loc.y += HEIGHT_CORRECT;
			return (loc);
		}



		public static bool KinectFrontClear(int dist,ref int min_dist,ref int obs_no,bool tilt)

		{
			bool rtn = true;
			int row,col,no_obs = 0,mdist = dist + 1,i,pixel,no_samples = 0;
			double value,kh,htilt;
			Arm.Loc3D target_loc = new Arm.Loc3D();
			string fname = "";
			BinaryWriter bw = null;
			ArrayList obs = new ArrayList();
			Bitmap blob_pic = null;
			CvBlob b = null;
			Point mincol = new Point(640, 0), maxcol = new Point(0, 0);
			double maxx, minx;
			Arm.Loc3D pt;
			string msg,reply;

			Log.LogEntry("KinectFrontClear: " + dist);
			if (tilt)
				HeadAssembly.Tilt(MOVE_KINECT_TILT,true);
			if (Kinect.GetDepthFrame(ref depthdata,60))
				{
				Kinect.nui.CoordinateMapper.MapDepthFrameToSkeletonFrame(Kinect.nui.DepthStream.Format, depthdata, sips);
				rtn = true;
				min_dist = dist + 1;
				if (SharedData.log_operations)
					{
					fname = Log.LogDir() + "Kinect floor scan " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".pc";
					bw = new BinaryWriter(File.Open(fname, FileMode.Create));
					}
				htilt = HeadAssembly.TiltAngle();
				htilt += MOVE_KINECT_TILT_CORRECT;
				kh = Arm.KinectHeight(htilt);
				for (row = 0; row < Kinect.nui.ColorStream.FrameHeight; row++)
					{
					for (col = 0; col < Kinect.nui.ColorStream.FrameWidth; col++)
						{
						target_loc = PtLocation(row,col,MOVE_KINECT_TILT_CORRECT);
						if (target_loc.z > 0)
							{
							pixel = ((row * Kinect.nui.ColorStream.FrameWidth) + (Kinect.nui.ColorStream.FrameWidth - col - 1)) * 4;
							bdata[pixel] = 0;
							bdata[pixel + 1] = 0;
							bdata[pixel + 2] = 0;
							if (Math.Abs(target_loc.x) <= ROBOT_WIDTH)
								{
								if (target_loc.z > dist)
									{
									if (target_loc.z > MIN_DIST_LIMIT)
										{
										no_samples += 1;
										if ((kh + target_loc.y) > MIN_HEIGHT_LIMIT)
											{
											no_obs += 1;
											obs.Add(new Point(col,row));
											if (target_loc.z < mdist)
												mdist = (int) Math.Floor(target_loc.z);
											bdata[pixel] = 255;
											bdata[pixel + 1] = 255;
											bdata[pixel + 2] = 255;
											if (col < mincol.X)
												{
												mincol.X = col;
												mincol.Y = row;
												}
											if (col > maxcol.X)
												{
												maxcol.X = col;
												maxcol.Y = row;
												}
											}
										}
									}
								}
							}
						}
					}
				Log.LogEntry("Found " + no_obs + " obtacles in " + no_samples + " samples with min distance of " + mdist + " in.");
				if ((no_obs > 0) && (((double) no_obs/no_samples) < .01))
					{
					blob_pic = bdata.ToBitmap(Kinect.nui.ColorStream.FrameWidth,Kinect.nui.ColorStream.FrameHeight);
					blob_pic.RotateFlip(RotateFlipType.Rotate180FlipY);
					pic = blob_pic.ToIplImage();
					Cv.CvtColor(pic, gs, ColorConversion.BgrToGray);
					blobs.Label(gs, img);
					if (blobs.Count == 1)
						{
						b = blobs[0];
						pt = PtLocation(mincol.Y, mincol.X, MOVE_KINECT_TILT_CORRECT);
						minx = pt.x;
						pt = PtLocation(maxcol.Y, maxcol.X, MOVE_KINECT_TILT_CORRECT);
						maxx = pt.x;
						if ((Math.Abs(minx) < ROBOT_WIDTH - .1) && (Math.Abs(maxx) < ROBOT_WIDTH - .1))
							{
							msg = "I have detected a possible connect anomaly " + ((int) Math.Round(pt.z)) + " inches ahead of me.  Is my path clear?";
							reply = AutoRobotControl.Speech.Conversation(msg, "responseyn", 5000, true);
							if (reply == "yes")
								{
								no_obs = 0;
								mdist = dist + 1;
								}
							}
						else
							Log.LogEntry("Obstacle blob not isolated.");
						}
					else
						Log.LogEntry("Number obstacle blobs: " + blobs.Count);
					if (no_obs > 0)
						rtn = false;
					}
				min_dist = mdist;
				obs_no = no_obs;
				if (bw != null)
					{
					for (i = 0;i < sips.Length;i++)
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
					fname = fname.Replace(".pc",".bmp");
					blob_pic.Save(fname,ImageFormat.Bmp);
					Log.LogEntry("Saved: " + fname);
					}
				}
			else
				Log.LogEntry("KinectFrontClearance could not obtain depth frame.");
			if (tilt)
				HeadAssembly.Tilt(0,true);
			return(rtn);
		}



		public static bool TwoStepPlan(int direc,int cangle,ref ArrayList path)

		{
			bool rtn = false;
			int direct,angle,dist;
			Room.rm_location rl;

			if (((wsd.work_loc.orient >= 45) && (wsd.work_loc.orient < 135)) || ((wsd.work_loc.orient >= 225) && (wsd.work_loc.orient < 315)))
				{
				direct = (direc - cangle) % 360;
				if (direct < 0)
					direct += 360;
				Point midpt = new Point(0, wsd.work_loc.coord.Y);
				if (wsd.work_loc.coord.Y < wsd.initial_robot_loc.coord.Y)
					angle = NavCompute.AngularDistance(direct, 0);
				else
					angle = NavCompute.AngularDistance(direct, 180);
				int dx = (int)Math.Round(Math.Abs(wsd.work_loc.coord.Y - wsd.initial_robot_loc.coord.Y) * Math.Tan(angle * SharedData.DEG_TO_RAD));
				if (wsd.work_loc.coord.X < wsd.initial_robot_loc.coord.X)
					dx *= -1;
				midpt.X = wsd.initial_robot_loc.coord.X + dx;
				path.Add(midpt);
				dist = NavCompute.DistancePtToPt(midpt, wsd.work_loc.coord);
				if (dist > MAX_FINAL_MOVE_DIST)
					{
					direct = NavCompute.DetermineHeadingPtToPt(wsd.work_loc.coord,midpt);
					rl = NavCompute.PtDistDirectApprox(midpt, direct, dist - SkillShared.MAX_FINAL_MOVE_DIST);
					path.Add(rl.coord);
					}
				path.Add(wsd.work_loc.coord);
				rtn = true;
				}
			else
				{
				Log.LogEntry("Unimplemented straight two move sector");
				}
			return (rtn);
		}



		public static bool TurnAngle(int angle)

		{
			bool rtn = false;
			string rsp,command;
			int timeout;

			Log.LogEntry("TurnAngle: " + angle.ToString());
			timeout = (int) (((double) Math.Abs(angle)/180) * 5000) + 1000;
			if (angle < 0)
				command = SharedData.RIGHT_TURN + " SLOW " + Math.Abs(angle).ToString();
			else
				command = SharedData.LEFT_TURN + " SLOW " + Math.Abs(angle).ToString();
			rsp = SendCommand(command,timeout);
			if (rsp.StartsWith("ok"))
				rtn = true;
			return(rtn);
		}



		public static bool TurnToFaceMP(Point mp)

		{
			bool rtn = false;
			NavCompute.pt_to_pt_data ppd;
			bool turn_safe;
			int angle;
			NavData.location clocation;
			ArrayList obs = new ArrayList();

			Log.LogEntry("TurnToFaceMP: " + mp.ToString());
			clocation = NavData.GetCurrentLocation();
			ppd = NavCompute.DetermineRaDirectDistPtToPt(mp, clocation.coord);
			angle = clocation.orientation - ppd.direc;
			if (angle > 180)
				angle -= 360;
			else if (angle < -180)
				angle += 360;
			if (Math.Abs(angle) >= SharedData.MIN_TURN_ANGLE)
				{
				if (!(turn_safe = Turn.TurnSafeMulti(angle)))
					{
					if (Math.Abs(angle) > 135)
						{
						if (angle < 0)
							angle += 360;
						else
							angle -= 360;
						turn_safe = Turn.TurnSafeMulti(angle);
						}
					}
				if ((!turn_safe) && (obs.Count == 1))
					{
					string msg,reply;

					msg = "A possible LIDAR anomaly have been detected .  Is my turn clear?";
					reply = AutoRobotControl.Speech.Conversation(msg, "responseyn", 5000, true);
					if (reply == "yes")
						{
						turn_safe = true;
						}
					}
				if (turn_safe)
					{
					rtn = TurnAngle(angle);
					if (rtn)
						{
						clocation.orientation = ppd.direc;
						clocation.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(angle),ppd.direc,clocation.coord);
						clocation.ls = NavData.LocationStatus.DR;
						NavData.SetCurrentLocation(clocation);
						MotionMeasureProb.Move(new MotionMeasureProb.Pose(clocation.coord,clocation.orientation));
						}
					else
						{
						SharedData.med.mt = SharedData.MoveType.SPIN;
						SharedData.med.et = Turn.LastError();
						SharedData.med.ob_descript = null;
						}
					}
				}
			else
				rtn = true;
			return(rtn);
		}



		public static bool DirectMove(Point pt,bool check_clear = true)

		{
			bool rtn = false;
			NavData.location cl,ecloc;
			string rply;
			NavCompute.pt_to_pt_data ppd;
			int mov_dist = 0;
			Point npt;
			ArrayList obs = new ArrayList(),scan = new ArrayList();

			cl = NavData.GetCurrentLocation();
			if (Navigate.PathClear(cl.coord, pt))
				{
				if (TurnToFaceMP(pt))
					{
					cl = NavData.GetCurrentLocation();
					ppd = NavCompute.DetermineRaDirectDistPtToPt(pt, cl.coord);
					if (Rplidar.FrontClear(ppd.dist,SharedData.ROBOT_WIDTH + 1,ref obs,ref mov_dist,ref scan))
						mov_dist = ppd.dist;
					if (check_clear)
						rply = SendCommand(SharedData.FORWARD_SLOW + " " + mov_dist, 8000);
					else
						rply = SendCommand(SharedData.FORWARD_SLOW_NCC + " " + mov_dist,8000);
					if (rply.StartsWith("ok") || rply.Contains(SharedData.INSUFFICENT_FRONT_CLEARANCE))
						{
						ecloc = cl;
						rply = SendCommand(SharedData.DIST_MOVED,200);
						if (rply.StartsWith("ok"))
							{
							mov_dist = int.Parse(rply.Substring(3));
							npt = new Point(0, mov_dist);
							ecloc.coord = NavCompute.MapPoint(npt, cl.orientation, cl.coord);
							}
						else
							ecloc.coord = pt;
						ecloc.ls = NavData.LocationStatus.DR;
						ecloc.loc_name = "";
						Log.LogEntry("Expected location: " + ecloc.ToString());
						NavData.SetCurrentLocation(ecloc);
						MotionMeasureProb.Move(new MotionMeasureProb.Pose(ecloc.coord, ecloc.orientation));
						rtn = true;
						}
					else
						Log.LogEntry("Move error: " + rply);
					}
				else
					Log.LogEntry("Turn failed.");
				}
			else
				Log.LogEntry("Path no clear.");
			return(rtn);
		}



		public static bool MoveBackward(int dist,NavData.location cl)

		{
			bool rtn = false;
			string rsp;
			NavData.location eloc;
			Point pt;

			rsp = SkillShared.SendCommand(SharedData.BACKWARD_SLOW_NCC + " " + dist, 8000);
			if (rsp.StartsWith("ok") || ((rsp.StartsWith("fail") && (rsp.Contains(SharedData.INSUFFICENT_REAR_CLEARANCE)))))
				{
				rsp = SkillShared.SendCommand(SharedData.DIST_MOVED, 200);
				if (rsp.StartsWith("ok"))
					dist = int.Parse(rsp.Substring(3));
				pt = new Point(0, -dist);
				eloc = cl;
				eloc.coord = NavCompute.MapPoint(pt, cl.orientation, cl.coord);
				eloc.ls = NavData.LocationStatus.DR;
				eloc.loc_name = "";
				Log.LogEntry("Expected location: " + eloc.ToString());
				NavData.SetCurrentLocation(eloc);
				MotionMeasureProb.Move(new MotionMeasureProb.Pose(eloc.coord, eloc.orientation));
				rtn = true;
				}
			return(rtn);
		}



		public static void SetPoint(ref Bitmap bmp,int x, int y,Brush br)

		{
			Graphics g;

			try
			{
			g = Graphics.FromImage(bmp);
			g.FillRectangle(br, x - 1, y - 1, 2, 2);
			}

			catch(Exception)
			{
			Log.LogEntry("SetPoint exception @ " + x + " " + y);
			}
		}



		public static int FindMapObstacle(ref byte[,] map,Point spt,int direct,ref Bitmap bm,double width)

		{
			int mdist = -1,i,j,hwdist,pdirect;
			bool obs_found = false;
			Point npt,fpt;

			Log.LogEntry("FindMapObstacle: (" + spt.X + " " + spt.Y + ") " + direct);
			if (bm != null)
				SetPoint(ref bm, spt.X, spt.Y, Brushes.Blue);
			hwdist = (int) Math.Ceiling(width/ 2);
			pdirect = (direct + 90) %360;
			for (i = 0;;i++)
				{
				npt = new Point((int) Math.Round(spt.X + (i * Math.Sin(direct * SharedData.DEG_TO_RAD))),(int) Math.Round(spt.Y - (i * Math.Cos(direct * SharedData.DEG_TO_RAD))));
				for (j = -hwdist; j < hwdist;j++ )
					{
					fpt = new Point((int) Math.Round(npt.X + (j * Math.Sin(pdirect * SharedData.DEG_TO_RAD))), (int)Math.Round(npt.Y - (j * Math.Cos(pdirect * SharedData.DEG_TO_RAD))));

					try
					{
					if (map[fpt.X, fpt.Y] == (byte)AutoRobotControl.Room.MapCode.BLOCKED)
						{
						obs_found = true;
						mdist = i;
						Log.LogEntry("Obstacle @ (" + fpt.X + " " + fpt.Y + ")  " + mdist);
						if (bm!= null)
							SetPoint(ref bm,fpt.X,fpt.Y,Brushes.Red);
						break;
						}
					}

					catch (Exception)
					{
					obs_found = true;
					mdist = i;
					Log.LogEntry("Edge @ (" + fpt.X + " " + fpt.Y + ")  " + mdist);
					if (bm != null)
						SetPoint(ref bm,fpt.X,fpt.Y,Brushes.Red);
					break;
					}

					}
				if (obs_found)
					break;
				}
			return (mdist);
		}



		public static int FindMapObstacle(ref byte[,] map,Point spt,int direct,ref Bitmap bm)

		{
			return(FindMapObstacle(ref map,spt,direct,ref bm,SharedData.ROBOT_WIDTH));
		}



		public static bool FindSpeaker(int pan,ref PersonDetect.scan_data pdd)

		{
			NavData.location cl;
			double angle;
			Room.rm_location rl;
			bool rtn = false;

			if (pd.NearestHCLPerson(false, ref pdd))
				{
				cl = NavData.GetCurrentLocation();
				angle = (cl.orientation + pan - pdd.angle) % 360;
				if (angle < 0)
				angle += 360;
				rl = NavCompute.PtDistDirectApprox(cl.coord, (int)Math.Round(angle), (int)Math.Round(pdd.dist));
				pdd.rm_location = rl.coord;
				Log.LogEntry("Speaker @ " + pdd.rm_location + " (RmC)");
				pdd.ts = SharedData.app_time.ElapsedMilliseconds;
				rtn = true;
				}
			else
				SpeakerData.Person = PersonDetect.Empty();
			return (rtn);
		}



		public static bool FindSpeakerFace(int pan, ref PersonDetect.scan_data pdd, ref PersonDetect.scan_data fd)

		{
			NavData.location cl;
			double angle;
			Room.rm_location rl;
			bool rtn = false;
			
			if (pd.NearestHCLPersonFace(false, ref pdd,ref fd))
				{
				cl = NavData.GetCurrentLocation();
				angle = (cl.orientation + pan - pdd.angle) % 360;
				if (angle < 0)
					angle += 360;
				rl = NavCompute.PtDistDirectApprox(cl.coord, (int)Math.Round(angle), (int)Math.Round(pdd.dist));
				pdd.rm_location = rl.coord;
				pdd.ts = SharedData.app_time.ElapsedMilliseconds;
				Log.LogEntry("Speaker @ " + pdd.rm_location + " (RmC)");
				if (fd.detected)
					{
					angle = (cl.orientation + pan - fd.angle) % 360;
					if (angle < 0)
						angle += 360;
					rl = NavCompute.PtDistDirectApprox(cl.coord, (int)Math.Round(angle), (int)Math.Round(fd.dist));
					fd.rm_location = rl.coord;
					fd.ts = SharedData.app_time.ElapsedMilliseconds;
					Log.LogEntry("Speaker face @ " + fd.rm_location + " (room coord)");
					}
				else
					Log.LogEntry("Speaker face not found.");
				rtn = true;
				}
			return (rtn);
		}



		public static bool FindFace(int pan,ref PersonDetect.scan_data fd,bool near)

		{
			bool rtn = false;
			NavData.location cl;
			double angle;
			Room.rm_location rl;

			if (pd.FindFace(near,ref fd))
				{
				cl = NavData.GetCurrentLocation();
				angle = (cl.orientation + pan - Math.Round(fd.angle)) % 360;
				if (angle < 0)
					angle += 360;
				rl = NavCompute.PtDistDirectApprox(cl.coord, (int)Math.Round(angle), (int)Math.Round(fd.dist));
				fd.rm_location = rl.coord;
				fd.ts = SharedData.app_time.ElapsedMilliseconds;
				Log.LogEntry("Face: @ " + fd.rm_location + " (room coord)");
				rtn = true;
				}
			else
				Log.LogEntry("Speaker face not found.");
			return (rtn);
		}



		public static Point RotatePoint(Point pt,int angle)

		{
			Point rpt = new Point(0,0);

			angle %= 360;
			rpt.X = (int) Math.Round(pt.X * Math.Cos(angle * SharedData.DEG_TO_RAD) - pt.Y * Math.Sin(angle * SharedData.DEG_TO_RAD));
			rpt.Y = (int) Math.Round(pt.X * Math.Sin(angle * SharedData.DEG_TO_RAD) + pt.Y * Math.Cos(angle * SharedData.DEG_TO_RAD));
			return (rpt);
		}



		public static Dpt RotateDPoint(Dpt pt,int angle)

		{
			Dpt rpt = new Dpt(0,0);

			angle %= 360;
			rpt.X = pt.X * Math.Cos(angle * SharedData.DEG_TO_RAD) - pt.Y * Math.Sin(angle * SharedData.DEG_TO_RAD);
			rpt.Y = pt.X * Math.Sin(angle * SharedData.DEG_TO_RAD) + pt.Y * Math.Cos(angle * SharedData.DEG_TO_RAD);
			return (rpt);
		}



		public static double DistanceDPtToDPt(Dpt p1,Dpt p2)

		{
			return (Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)));
		}



		public static bool FindEdges(int direct,int mdist) 

		{
			bool rtn = false;
			int i, start, len,index = 0,count = 0,erow = 0,pixel,no_samples = 0,tilt;
			double dist,ph,dh,last_dist = -1,last_ph = 0,avg_last = 0,total = 0,dhthres,thres_factor,top_start = 0,fe_dist,tilt_correct,top_end = 0;
			bool top_found = false,edge_found = false,front_edge_dist_set = false;
			int state_trans = 0;
			Arm.Loc3D loc = new Arm.Loc3D();
			int missed = 0;
			const int MAX_MISS = 5;
			const int ROW_CORRECT = -10;

			Log.LogEntry("FindEdges: " + direct + "," + mdist);
			SkillShared.al.Clear();
			thres_factor = (1 / 531.54) / 2;
			tilt = HeadAssembly.TiltAngle();
			tilt_correct = ((double)tilt / SkillShared.MOVE_KINECT_TILT) * SkillShared.MOVE_KINECT_TILT_CORRECT;
			len = Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth;
			start = 320;
			for (i = start; i < len; i += Kinect.nui.ColorStream.FrameWidth)
				{
				pixel = (index * Kinect.nui.ColorStream.FrameWidth) + (Kinect.nui.ColorStream.FrameWidth - 320 - 1);
				if (SkillShared.dips[pixel].Depth > 0)
					{
					missed = 0;
					dist = Kinect.CorrectedDistance(SkillShared.dips[pixel].Depth * SharedData.MM_TO_IN);
					if (dist <= mdist)
						{
						no_samples += 1;
						dhthres = dist * thres_factor;
						loc = SkillShared.DPtLocation(index,320,tilt_correct);
						ph = loc.y;
						dh = ph - last_ph;
						if (!top_found)
							{
							if ((no_samples > 1) && SkillShared.AvgLast(dh, ref avg_last))
								{
								if (Math.Abs(avg_last) < dhthres)
									{
									state_trans += 1;
									if (state_trans == 2)
										{
										top_found = true;
										total = ph;
										count = 1;
										state_trans = 0;
										top_start = loc.z;
										}
									}
								else
									state_trans = 0;
								}
							}
						else if (top_found && !edge_found)
							{
							if (SkillShared.AvgLast(dh, ref avg_last))
								{
								if (Math.Abs(avg_last) > dhthres)
									{
									state_trans += 1;
									if (state_trans == 2)
										{
										if (top_start - loc.z >= EDWorkSpaceInfo.MIN_TOP_LEN )
											{
											edge_found = true;
											top_end = loc.z;
											if (SkillShared.wsd.existing_area)
												{
												if (Math.Abs((total / count) - SkillShared.wsd.top_height) > AAShare.TOP_HI_LO_DIFF)
													Log.LogEntry("Possible top height descrepency found " + (total / count).ToString("F2") + " vs " + SkillShared.wsd.top_height.ToString("F2"));
												}
											SkillShared.wsd.top_height = total / count;
											Log.LogEntry("Top height: " + (total / count).ToString("F2") + " @ row " + (index - 1));
											erow = index - 1;
											total = loc.z;
											count = 1;
											}
										else
											{
											top_found = false;
											state_trans = 0;
											}
										}
									}
								else if (state_trans == 0)
									{
									total += ph;
									count += 1;
									}
								else
									state_trans = 0;
								}
							}
						else if (top_found && edge_found)
							{
							if (Math.Abs(dh) > (3 *dhthres))
								{
								fe_dist = total/count;
								Log.LogEntry("Front edge distance: " + fe_dist.ToString("F2") + " @ row " + index);
								SkillShared.wsd.front_edge_dist = fe_dist;
								front_edge_dist_set = true;
								break;
								}
							else
								{
								total += loc.z;
								count += 1;
								}
							}
						last_dist = dist;
						last_ph = ph;
						}
					}
				else
					{
					if (top_found && !edge_found)
						{
						missed += 1;
						if (missed == MAX_MISS)
							{
							if (top_start - loc.z >= EDWorkSpaceInfo.MIN_TOP_LEN)
								{
								edge_found = true;
								if (SkillShared.wsd.existing_area)
									{
									if (Math.Abs((total / count) - SkillShared.wsd.top_height) > AAShare.TOP_HI_LO_DIFF)
										Log.LogEntry("Possible top height descrepency found " + (total / count).ToString("F2") + " vs " + SkillShared.wsd.top_height.ToString("F2"));
									}
								SkillShared.wsd.top_height = total / count;
								Log.LogEntry("Top height (missed): " + (total / count).ToString("F2") + "  @ row " + (index - missed));
								SkillShared.wsd.front_edge_dist = loc.z;
								Log.LogEntry("Front edge distance (missed): " + loc.z.ToString("F2") + " @ row " + (index - missed));
								front_edge_dist_set = true;
								erow = index - missed;
								break;
								}
							}
						}
					}
				index += 1;
				}
			if (top_found && edge_found)
				{
				no_samples = 0;
				if (!front_edge_dist_set)
					{
					fe_dist = total / count;
					Log.LogEntry("Front edge distance: " + fe_dist.ToString("F2") + " @ row " + index);
					SkillShared.wsd.front_edge_dist = fe_dist;
					}
				erow += ROW_CORRECT;
				start = erow * Kinect.nui.ColorStream.FrameWidth + 320;
				SkillShared.al.Clear();
				thres_factor = (1 / 399.43) / 2;
				for (i = 0; i < Kinect.nui.ColorStream.FrameWidth / 2; i ++)
					{
					pixel = (erow * Kinect.nui.ColorStream.FrameWidth) + (Kinect.nui.ColorStream.FrameWidth - (320 + i) - 1);
					if (SkillShared.dips[pixel].Depth > 0)
						{
						no_samples += 1;
						missed = 0;
						dist = Kinect.CorrectedDistance(SkillShared.dips[pixel].Depth * SharedData.MM_TO_IN);
						dhthres = dist * thres_factor;
						loc = SkillShared.DPtLocation(erow, 320 + i,tilt_correct);
						ph = loc.y;
						dh = ph - last_ph;
						if ((no_samples > 1) && SkillShared.AvgLast(dh, ref avg_last))
							{
							if (Math.Abs(avg_last) > dhthres)
								{
								do
									{
									pixel += 1;
									i -= 1;
									if (SkillShared.dips[pixel].Depth > 0)
										{
										loc = SkillShared.DPtLocation(erow, 320 + i, tilt_correct);
										Log.LogEntry("Side edge distance: " + loc.x.ToString("F2") + " @ row " + erow + " col " + (320 + i));
										SkillShared.wsd.side_edge_dist = loc.x;
										}
									}
								while (SkillShared.dips[pixel].Depth <= 0);
								rtn = true;
								break;
								}
							}
						last_ph = ph;
						}
					else
						{
						missed += 1;
						if (missed == MAX_MISS)
							{
							loc = SkillShared.DPtLocation(erow, 320 + i - missed, tilt_correct);
							Log.LogEntry("Side edge distance (missed): " + loc.x.ToString("F2") + " @ row " + erow + " col " + (320 + i - missed));
							SkillShared.wsd.side_edge_dist = loc.x;
							rtn = true;
							break;
							}
						}
					}
				if (!rtn)
					SkillShared.OutputSpeech("Could not find the side edge");
				}
			else
				{
				if (!top_found)
					SkillShared.OutputSpeech("Could not find the work space top.");
				else
					SkillShared.OutputSpeech("Could not find the work space front edge.");
				}
			return (rtn);
		}


		}
	}
