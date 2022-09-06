using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;
using MathNet.Numerics.LinearAlgebra.Double;


namespace AutoRobotControl
	{

	public static class Kinect
		{
		public enum EdgeDetectTrigger { NONE, DIST_CHANGE, MIN_DIST,EITHER };

		public static double DEPTH_VERT_FOV = 58.5;		// for 640 X 480 depth the FOVs given by KinectSensor are wrong
		public static double DEPTH_HOT_FOV = 45.6;
		public const int DIPS_MARGIN = 25;

		public const double NEAR_MIN = 400 * SharedData.MM_TO_IN;
		public const double NEAR_MAX = 3000 * SharedData.MM_TO_IN;
		public const double FAR_MIN = 800 * SharedData.MM_TO_IN;
		public const double FAR_MAX = 4000 * SharedData.MM_TO_IN;

		public static KinectSensor nui = null;
		public static double vert_pixals_per_degree = 10.7;
		public static double avg_hor_pixals_per_degree = 10.7;
		public static double depth_hor_pixals_per_degree = 5.4;
		public static double depth_vert_pixals_per_degree = 5.6;
		public static int depth_width = 640;
		public static int depth_height = 480;

		private const string PARAM_FILE = "kinectcal.param";
		private const int PASS_COUNT_LIMIT = 10;
		private const double EDGE_DIST_LIMIT = 12 * SharedData.IN_TO_MM;
		private const double MIN_DX_FACTOR = .5;
		private const double CEILING_ROW_OFFSET = 6 * SharedData.IN_TO_MM;
		private const double TILT_ERROR = 2;
		private const int TILT_ANGLE = -30;
		private const double M_TO_IN = 1000 * SharedData.MM_TO_IN;

		private static double[,] depth_cal;
		private static short[] depthdata = null;
		private static DepthImagePixel[] depthdata2;
		private static DepthImagePoint[] dips;
		private static SkeletonPoint[] sips;
		private static AutoResetEvent frame_complete = new AutoResetEvent(false);
		
		private static object get_video_frame = new object();
		private static object get_depth_frame = new object();


		public static bool Open()

		{
			bool rtn = false;
			int i;

			ReadCalibrationData();
			try
			{
			if (KinectSensor.KinectSensors.Count > 0)
				{
				nui = KinectSensor.KinectSensors[0];
				if (nui.Status == KinectStatus.Connected)
					{
					nui.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
					nui.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
					nui.Start();
					depth_width = nui.DepthStream.FrameWidth;
					depth_height = nui.DepthStream.FrameHeight;
					depth_hor_pixals_per_degree = nui.DepthStream.FrameWidth / DEPTH_HOT_FOV;
					depth_vert_pixals_per_degree = nui.DepthStream.FrameHeight/DEPTH_VERT_FOV;
					avg_hor_pixals_per_degree = nui.ColorStream.FrameWidth / nui.ColorStream.NominalHorizontalFieldOfView;
					vert_pixals_per_degree = nui.ColorStream.FrameHeight / nui.ColorStream.NominalVerticalFieldOfView;
					SharedData.kinect_operational = true;
					depthdata = new short[Kinect.nui.DepthStream.FramePixelDataLength];
					depthdata2 = new DepthImagePixel[Kinect.nui.DepthStream.FramePixelDataLength];
					dips = new DepthImagePoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];
					sips = new SkeletonPoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];
					SetFarRange();
					for (i = 0;i < 10;i++)
						{
						Kinect.nui.DepthStream.OpenNextFrame(100);
						Thread.Sleep(100);
						}
					rtn = true;
					}
				else
					{
					Log.LogEntry("Kinect sensor not connected.");
					SharedData.last_error = "Kinect sensor not connected.";
					nui = null;
					}
				}
			else
				{
				Log.LogEntry("No Kinect found.");
				SharedData.last_error = "No Kinect found.";
				nui = null;
				}
			}

			catch (Exception e)
			{
			Log.LogEntry("Kinect.Open exception: " + e.Message);
			Log.LogEntry("            stack trace: " + e.StackTrace);
			SharedData.last_error = "Kinect open exception: " + e.Message;
			nui = null;
			}

			return(rtn);
		}



		public static void Close()

		{
			if ((nui != null) && (nui.IsRunning))
				{
				nui.Stop();
				nui = null;
				SharedData.kinect_operational = false;
				}
		}



		// the calibration data is in inches
		public static double CorrectedDistance(double dist)

		{
			double cdist = 0,dm,dc,dd;
			int i;

			if ((depth_cal == null) || (depth_cal.Length == 0) || (dist <= 0))
				cdist = dist;
			else
				{
				for (i = 0; i < depth_cal.Length / 2; i++)
					{
					if (dist < depth_cal[0, i])
						{
						dm = depth_cal[0, i] - depth_cal[0, i - 1];
						dd = dist - depth_cal[0, i - 1];
						dc = depth_cal[1, i] - depth_cal[1, i - 1];
						cdist = ((dd / dm) * dc) + depth_cal[1, i - 1];
						break;
						}
					}
				if (i == depth_cal.Length / 2)
					{
					dd = dist - depth_cal[0, i - 1];
					cdist = ((depth_cal[1, i - 1] / depth_cal[0, i - 1]) * dd) + depth_cal[1, i - 1];
					}
				}
			return (cdist);
		}



		public static double VideoVerDegrees(int no_pixel)

		{
			double val = 0,adj;

			adj = ((double) Kinect.nui.ColorStream.FrameHeight/2) / Math.Tan((Kinect.nui.ColorStream.NominalVerticalFieldOfView / 2) * SharedData.DEG_TO_RAD);
			val = Math.Atan(no_pixel / adj) * SharedData.RAD_TO_DEG;
			return (val);
		}



		public static double VideoHorDegrees(int no_pixel)

		{
			double val = 0,adj;  //532.57

			adj = ((double)Kinect.nui.ColorStream.FrameWidth / 2) / Math.Tan((nui.ColorStream.NominalHorizontalFieldOfView / 2) * SharedData.DEG_TO_RAD);
			val = Math.Atan(no_pixel / adj) * SharedData.RAD_TO_DEG;
			return (val);
		}



		public static double DepthVerDegrees(int no_pixel)

		{
			double val = 0,adj;

			adj = ((double) Kinect.nui.DepthStream.FrameHeight/2) / Math.Tan((DEPTH_VERT_FOV/ 2) * SharedData.DEG_TO_RAD);
			val = Math.Atan(no_pixel / adj) * SharedData.RAD_TO_DEG;
			return (val);
		}



		public static double DepthHorDegrees(int no_pixel)

		{
			double val = 0;
			double adj;

			adj = ((double)Kinect.nui.DepthStream.FrameWidth / 2) / Math.Tan((DEPTH_HOT_FOV/2) * SharedData.DEG_TO_RAD);
			val = Math.Atan(no_pixel/adj) * SharedData.RAD_TO_DEG;
			return (val);
		}



		public static void SetNearRange()

		{
			Kinect.nui.DepthStream.Range = DepthRange.Near;
		}



		public static void SetFarRange()

		{
			Kinect.nui.DepthStream.Range = DepthRange.Default;
		}



		public static int FindCeiling(int col, short[] depthdata)

		{
			int i,depth,row = 0,max_depth = -1,pass_count = 0,bad_pixal = 0,rcount = 0;
			TextWriter tw = null;
			string fname = "";

			if (SharedData.log_operations )
				{
				fname = Log.LogDir() + "Kinect Find Ceiling Data Set " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
				tw = File.CreateText(fname);
				tw.WriteLine("Kinect find ceiling data set: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				tw.WriteLine();
				tw.WriteLine("Column: " + col);
				tw.WriteLine();
				tw.WriteLine("Row,Distance (mm)");
				}
			for (i = col;i < Kinect.nui.DepthStream.FramePixelDataLength;i += Kinect.depth_width)
				{
				depth = depthdata[i] >> 3;
				if (tw != null)
					tw.WriteLine(rcount + "," + depth);
				if (depth > 0)
					{
					if (depth == Kinect.nui.DepthStream.TooFarDepth)
						{
						row = -1;
						if (tw != null)
							{
							tw.WriteLine();
							tw.WriteLine("Too far distance encountered looking for ceiling.");
							}
						break;
						}
					if (depth > max_depth)
						{
						max_depth = depth;
						pass_count = 0;
						}
					else
						{
						pass_count += 1;
						if (pass_count > PASS_COUNT_LIMIT)
							{
							row = rcount + 1 - pass_count;
							break;
							}
						}
					}
				else
					{
					bad_pixal += 1;
					if (bad_pixal > Kinect.nui.DepthStream.FramePixelDataLength/2)
						{
						row = -1;
						if (tw != null)
							{
							tw.WriteLine();
							tw.WriteLine("Too many unknown or too near depths in data.");
							}
						else
							Log.KeyLogEntry("FindCeiling: too many unknown or too near depths in data.");
						break;
						}
					}
				rcount += 1;
				}
			if (tw != null)
				{
				if (row >= 0)
					{
					tw.WriteLine();
					tw.WriteLine("Ceiling found at row " + row);
					}
				tw.Close();
				Log.LogEntry("Saved: " + fname);
				}
			return(row);
		}



		public static int FindCeiling(int col, short[] depthdata,ref int row_dist)

		{
			int i,depth,row = 0,max_depth = -1,pass_count = 0;

			for (i = col;i < Kinect.nui.DepthStream.FramePixelDataLength;i += Kinect.depth_width)
				{
				depth = depthdata[i] >> 3;
				if (depth > 0)
					{
					if (depth == Kinect.nui.DepthStream.TooFarDepth)
						{
						row = -1;
//						Log.LogEntry("FindCeiling: too far distance encountered looking for ceiling.");
						break;
						}
					if (depth > max_depth)
						{
						max_depth = depth;
						pass_count = 0;
						}
					else
						{
						pass_count += 1;
						if (pass_count > PASS_COUNT_LIMIT)
							{
							row = ((i - col)/Kinect.nui.DepthStream.FrameWidth) - pass_count;
							break;
							}
						}
					}
				}
			if (row != -1)
				{
				row_dist = (int)Math.Round(max_depth / Math.Cos(((((double)(col - (Kinect.depth_width / 2))) / Kinect.depth_hor_pixals_per_degree)) * SharedData.DEG_TO_RAD));
//				Log.LogEntry("Ceiling found at row " + row + " at depth " + row_dist + " mm");
				}
			return(row);
		}



		// the calibration data is in inches
		private static void ReadCalibrationData()

		{
			string fname;
			TextReader tr;
			int lines,i;
			string line;
			string[] values;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				lines = int.Parse(tr.ReadLine());
				depth_cal = new double[2,lines];
				for (i = 0;i < lines;i++)
					{
					line = tr.ReadLine();
					values = line.Split(',');
					depth_cal[0,i] = double.Parse(values[0]);
					depth_cal[1,i] = double.Parse(values[1]);
					}
				tr.Close();
				}
		}



		public static bool GetDepthFrame(ref short[] data,int wait_time)

		{
			DepthImageFrame dif = null;
			bool rtn = false;

			if (Kinect.nui != null)
				{
				lock(get_depth_frame)
				{
				dif = Kinect.nui.DepthStream.OpenNextFrame(wait_time);
				if (dif != null)
					{
					dif.CopyPixelDataTo(data);
					dif.Dispose();
					rtn = true;
					}
				}

				}
			return(rtn);
		}



		public static bool GetDepthFrame(ref DepthImagePixel[] data,int wait_time)

		{
			DepthImageFrame dif = null;
			bool rtn = false;

			if (Kinect.nui != null)
				{

				lock(get_depth_frame)
				{

				try
				{
				dif = Kinect.nui.DepthStream.OpenNextFrame(wait_time);
				if (dif != null)
					{
					dif.CopyDepthImagePixelDataTo(data);
					dif.Dispose();
					rtn = true;
					}
				}

				catch(Exception ex)
				{
				Log.LogEntry("GetDepthFrame exception: " + ex.Message);
				}

				}

				}
			return(rtn);
		}



		public static bool GetColorFrame(ref byte[] data,int wait_time)

		{
			ColorImageFrame cif = null;
			bool rtn = false;

			if (Kinect.nui != null)
				{

				lock (get_video_frame)
				{

				try
				{
				cif = Kinect.nui.ColorStream.OpenNextFrame(wait_time);
				if (cif != null)
					{
					cif.CopyPixelDataTo(data);
					cif.Dispose();
					rtn = true;
					}
				}

				catch (Exception ex)
				{
				Log.LogEntry("GetColorFrame exception: " + ex.Message);
				}

				}

				}

			return(rtn);
		}



		public static bool MinWallDistance(ref int mwd)

		{
			int row,i,start,end;
			double depth,mdepth = 8001;
			bool rtn = false;

			if (nui != null)
				{
				if (depthdata == null)
					depthdata = new short[Kinect.nui.DepthStream.FramePixelDataLength];
				while (!GetDepthFrame(ref depthdata,40))
					Thread.Sleep(10);
				row = FindCeiling(depth_width/2, depthdata);
				if (row < 0)
					row = 187;
				else
					row += 10;
				Log.LogEntry("MinWallDistance row: " + row);
				start = (row * Kinect.depth_width) + 144;
				end = start + 32;
				for (i = start;i < end;i++)
					{
					depth = depthdata[i] >> 3;
					if ((depth > 0))
						{
						if (depth == Kinect.nui.DepthStream.TooNearDepth)
							depth = 600;
						else if (depth == Kinect.nui.DepthStream.TooFarDepth)
							depth = 6000;
						if (depth < mdepth)
							mdepth = depth;
						}
					}
				rtn = true;
				mwd = (int) CorrectedDistance(mdepth * SharedData.MM_TO_IN);
				}
			Log.LogEntry("Min wall distance (in): " + mwd);
			return(rtn);
		}



		public static int WallDistance()

		{
			int rtn = -1,row,i,start,end,samples = 0;
			double depth,tdepth = 0;

			if (nui != null)
				{
				if (depthdata == null)
					depthdata = new short[Kinect.nui.DepthStream.FramePixelDataLength];
				while (!GetDepthFrame(ref depthdata,40))
					Thread.Sleep(10);
				row = FindCeiling(depth_width/2,depthdata);
				if (row >= 0)
					{
					row += 10;
					start = (row * Kinect.depth_width) + 144;
					end = start + 32;
					for (i = start;i < end;i++)
						{
						depth = depthdata[i] >> 3;
						if ((depth > 0) && (depth != Kinect.nui.DepthStream.TooNearDepth) && (depth != Kinect.nui.DepthStream.TooFarDepth))
							{
							tdepth += depth;
							samples += 1;
							}
						else
							{
							rtn = -1;
							break;
							}
						rtn = (int) CorrectedDistance((tdepth/samples) * SharedData.MM_TO_IN);
						}
					}
				}
			Log.LogEntry("Wall distance (in): " + rtn);
			return(rtn);
		}



		public static bool FindDistDirectPerpToWall(ref short[] depthdata,ref int pa,ref double nsee,ref int cdist,int offset,int min_angle)

		{
			bool rtn = false;
			int row, col,start,end,i,n,dist,scol;
			double angle,rdist,zdist = 0;
			double sx2 = 0, sxy = 0, sx = 0, sy = 0,b,see = 0,ye;
			double m, ra, x, y;
			string dir, fname;
			TextWriter tw;
			int side_angle;

			pa = 0;
			if (min_angle > SharedData.MIN_PERP_ANGLE/2)
				side_angle = min_angle;
			else
				side_angle = SharedData.MIN_PERP_ANGLE/2;
			col = (int)Math.Round((Kinect.depth_width / 2) - (side_angle * Kinect.depth_hor_pixals_per_degree));
			if (Math.Abs(offset) > 0)
				col += (int)Math.Round(offset * Kinect.depth_hor_pixals_per_degree);
			if (col < 0)
				col = 0;
			else if (col > (Kinect.nui.DepthStream.FrameWidth - ((side_angle * 2) * Kinect.depth_hor_pixals_per_degree)))
				col = (int)Math.Round(Kinect.nui.DepthStream.FrameWidth - ((side_angle * 2) * Kinect.depth_hor_pixals_per_degree));
			scol = col;
			row = FindCeiling((int) Math.Round(col + (side_angle  * Kinect.depth_hor_pixals_per_degree)), depthdata);
			n = (int) ((side_angle * 2) * Kinect.depth_hor_pixals_per_degree);
			if ((row >= 0) && (col + n < Kinect.nui.DepthStream.FrameWidth))
				{
				row += 10;
				start = row * Kinect.depth_width + col;
				end = start + n;
				dir = Log.LogDir();
				fname = dir + "Kinect Perp Wall Data Set " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
				tw = File.CreateText(fname);
				tw.WriteLine("Kinect perp wall data set: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				tw.WriteLine();
				tw.WriteLine("Offset angle (°): " + offset);
				tw.WriteLine("Min angle (°): " + min_angle);
				tw.WriteLine("Side angle (°): " + side_angle);
				tw.WriteLine("Row: " + row);
				tw.WriteLine();
				tw.WriteLine("X,Y,RA (°)");
				for (i = start; i < end; i++)
					{
					dist = depthdata[i] >> 3;
					if ((dist == Kinect.nui.DepthStream.UnknownDepth) || (dist == Kinect.nui.DepthStream.TooNearDepth) || (dist == Kinect.nui.DepthStream.TooFarDepth))
						{
						col += 1;
						n -= 1;
						}
					else
						{
						ra = (((double) (col - (Kinect.depth_width / 2)))/Kinect.depth_hor_pixals_per_degree);
						col += 1;
						rdist = dist/Math.Cos(ra * SharedData.DEG_TO_RAD);
						angle = 360 - ra;
						angle %= 360;
						x = (rdist * Math.Sin(angle * SharedData.DEG_TO_RAD));
						y = dist;
						tw.WriteLine(x + "," + y + "," + ra);
						sx += x;
						sx2 += x * x;
						sy += y;
						sxy += x * y;
						}
					}
				if (n >= (end - start) * .75)
					{
					m = ((n * sxy) - (sx * sy)) / ((n * sx2) - Math.Pow(sx, 2));
					b = (sy/n) - ((m * sx)/n);
					zdist = CorrectedDistance(b * SharedData.MM_TO_IN);
					col = scol;
					for (i = start; i < end; i++)
						{
						dist = depthdata[i] >> 3;
						if ((dist != Kinect.nui.DepthStream.UnknownDepth) && (dist != Kinect.nui.DepthStream.TooNearDepth) && (dist != Kinect.nui.DepthStream.TooFarDepth))
							{
							ra = (((double)(col - (Kinect.depth_width / 2)))/ Kinect.depth_hor_pixals_per_degree);
							col += 1;
							rdist = dist/Math.Cos(ra * SharedData.DEG_TO_RAD);
							angle = 360 - ra;
							angle %= 360;
							x = (rdist * Math.Sin(angle * SharedData.DEG_TO_RAD));
							y = dist;
							ye = (x * m ) + b;
							see += Math.Pow(y - ye,2);
							}
						}
					see = Math.Sqrt(see/n);
					pa = (int) Math.Round(Math.Atan(m) * SharedData.RAD_TO_DEG);
					cdist = (int) (Math.Round(zdist * Math.Cos(Math.Atan(m))));
					nsee = see/(sy/n);
					tw.WriteLine();
					tw.WriteLine("number samples - " + n + "   slope - " + m.ToString("F3") + "   intercept - " + b.ToString("F3") + "   wall perp angle (°) - " + pa  + "   wall distance (in) - " + cdist + "   est standard error - " +  see.ToString("F3") + "   Norm est standard error - " + nsee.ToString("F4"));
					rtn = true;
					}
				else
					tw.WriteLine("Insufficent useable samples to determine values.");
				tw.Close();
				Log.LogEntry("Saved " + fname);
				SaveRowDepth(ref depthdata,row,false);
				}
			return(rtn);
		}



		public static bool FindDistDirectPerpToWall(ref int pa,ref double nsee,ref int cdist,int offset,int min_angle)

		{
			if (depthdata == null)
				depthdata = new short[Kinect.nui.DepthStream.FramePixelDataLength];
			while (!GetDepthFrame(ref depthdata, 40))
				Thread.Sleep(10);
			return(FindDistDirectPerpToWall(ref depthdata,ref pa,ref nsee,ref cdist,offset,min_angle));
		}



		public static void SaveRowDepth(ref short[] depthdata,int row,bool shifted_data)

		{
			TextWriter tw;
			string file;
			int start_pos,end_pos,i,no_samples = 0;
			double depth;

			file = Log.LogDir() + "RowDepthData " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".csv";
			tw = File.CreateText(file);
			if (tw != null)
				{

				try
				{
				tw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				tw.WriteLine("Row: " + row);
				tw.WriteLine();
				tw.WriteLine();
				tw.WriteLine("Index,Distance (in)");
				start_pos = (row * Kinect.depth_width);
				end_pos = start_pos + Kinect.depth_width;
				for (i = start_pos;i < end_pos; i++)
					{
					if (shifted_data)
						depth = CorrectedDistance(depthdata[i] * SharedData.MM_TO_IN);
					else
						depth = CorrectedDistance((depthdata[i] >> 3) * SharedData.MM_TO_IN);
					tw.WriteLine(no_samples + "," + depth);
					no_samples += 1;
					}
				}

				catch(Exception)
				{
				}

				tw.Close();
				Log.LogEntry("Saved " + file);
				}
		}



		// logic is good for edges on a wall paralel to the exit, NOT a wall perpendicular to the exit
		public static bool FindEdge(ref short[] depthdata,int shift_angle,ref int ra,ref int edist,int offset,bool high_col)

		{
			bool rtn = false;
			int col,row,start,end,n,i,dist,scol,max_row = 0;
			string dir,fname;
			TextWriter tw;
			double sy = 0,mean = 0,cra,x,y,rdist,angle;
			double min_dx,last_x = 0,avg = 0;
			Queue dx_avg = new Queue(3);
			int mdx_count = 0;

			dir = Log.LogDir();
			col = (int) Math.Round((Kinect.depth_width / 2) - (10 * Kinect.depth_hor_pixals_per_degree));
			if (Math.Abs(offset) >  0)
				col += (int) Math.Round(offset * Kinect.depth_hor_pixals_per_degree);
			if (col < 0)
				col = 0;
			else if (col > (Kinect.nui.DepthStream.FrameWidth - (20 * Kinect.depth_hor_pixals_per_degree)))
				col = (int) Math.Round(Kinect.nui.DepthStream.FrameWidth - (20 * Kinect.depth_hor_pixals_per_degree));
			scol = col;
			row = FindCeiling((int)Math.Round((double)(Kinect.depth_width / 2)), depthdata);
			if (row > max_row)
				max_row = row;
			row = FindCeiling(col, depthdata);
			if (row > max_row)
				max_row = row;
			row = FindCeiling((int)Math.Round(col + (10 * Kinect.depth_hor_pixals_per_degree)), depthdata);
			if (row > max_row)
				max_row = row;
			row = FindCeiling((int)Math.Round(col + (20 * Kinect.depth_hor_pixals_per_degree)), depthdata);
			if (row > max_row)
				max_row = row;
			if (max_row >= 0)
				{
				row = max_row + 10;
				start = row * Kinect.depth_width + col;
				n = (int)(20 * Kinect.depth_hor_pixals_per_degree);
				end = start + n;
				fname = dir + "Kinect Find Edge Data Set " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
				tw = File.CreateText(fname);
				tw.WriteLine("Kinect find edge data set: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				tw.WriteLine();
				tw.WriteLine("Perp. shift angle (°): " + shift_angle);
				tw.WriteLine("Offset angle (°): " + offset);
				tw.WriteLine("High column: " + high_col);
				tw.WriteLine("Row: " + row);
				for (i = start; i < end; i++)
					{
					dist = depthdata[i] >> 3;
					if ((dist == Kinect.nui.DepthStream.UnknownDepth) || (dist == Kinect.nui.DepthStream.TooNearDepth) || (dist == Kinect.nui.DepthStream.TooFarDepth))
						{
						col += 1;
						n -= 1;
						}
					else
						{
						cra = (int) Math.Round(Kinect.DepthHorDegrees((int) Math.Round((double) (col - (Kinect.depth_width / 2)))));
						col += 1;
						rdist = dist/Math.Cos(cra * SharedData.DEG_TO_RAD);
						angle = 360 + cra - shift_angle;
						angle %= 360;
						y = (rdist * Math.Cos(angle * SharedData.DEG_TO_RAD));
						sy += y;
						}
					}
				if (n >= (end - start) * .75)
					{
					mean = sy/n;
					min_dx = (mean * Math.Tan((1/Kinect.depth_hor_pixals_per_degree) * SharedData.DEG_TO_RAD)) * MIN_DX_FACTOR;
					tw.WriteLine("Mean Y (mm): " + mean.ToString("F3"));
					tw.WriteLine("Min dx (mm): " + min_dx.ToString("F3"));
					tw.WriteLine();
					tw.WriteLine("X,Y,RA (°)");
					if (high_col)
						{
						col = (int) (scol + (10 * Kinect.depth_hor_pixals_per_degree));
						for (i = col; i > 0;i--)
							{
							dist = depthdata[row * Kinect.depth_width + i] >> 3;
							if (dist != Kinect.nui.DepthStream.UnknownDepth)
								{
								cra = (int)Math.Round(Kinect.DepthHorDegrees((int)Math.Round((double)(i - (Kinect.depth_width / 2)))));
								rdist = dist / Math.Cos(cra * SharedData.DEG_TO_RAD);
								angle = 360 + cra - shift_angle;
								angle %= 360;
								if (angle < 0)
									angle += 360;
								x = (rdist * Math.Sin(angle * SharedData.DEG_TO_RAD));
								y = (rdist * Math.Cos(angle * SharedData.DEG_TO_RAD));
								if (i < col)
									{
									if (dx_avg.Count == 3)
										{
										avg -= (double) dx_avg.Dequeue();
										dx_avg.Enqueue(x - last_x);
										avg += x - last_x;
										}
									else
										{
										dx_avg.Enqueue(x - last_x);
										avg += x - last_x;
										}
									}
								last_x = x;
								tw.WriteLine(x + "," + y + "," + cra);
								if ((Math.Abs(y - mean) > EDGE_DIST_LIMIT) || ((dx_avg.Count == 3) && (Math.Abs(avg/3) < min_dx)))
									{
									if ((Math.Abs(y - mean) > EDGE_DIST_LIMIT) || (mdx_count > 0))
										{
										tw.WriteLine();
										if (Math.Abs(y - mean) > EDGE_DIST_LIMIT)
											tw.WriteLine("Max dy triggered.");
										else
											tw.WriteLine("Min dx triggered.");
										ra = (int)Math.Round(Kinect.DepthHorDegrees((int)Math.Round((double)((i + 1) - (Kinect.depth_width / 2)))));
										dist = depthdata[row * Kinect.depth_width + (i + 1)] >> 3;
										rdist = Math.Round(CorrectedDistance(dist * SharedData.MM_TO_IN));
										edist = (int)Math.Round(rdist / Math.Cos(cra * SharedData.DEG_TO_RAD));
										tw.WriteLine("Edge RA (°) - " + ra + "   distance (in) - " + edist);
										rtn = true;
										break;
										}
									else
										mdx_count += 1;
									}
								}
							}
						}
					else 
						{
						col = scol;
						for (i = col; i < Kinect.depth_width; i++)
							{
							dist = depthdata[row * Kinect.depth_width + i] >> 3;
							if (dist != Kinect.nui.DepthStream.UnknownDepth)
								{
								cra = (int)Math.Round(Kinect.DepthHorDegrees((int)Math.Round((double)(i - (Kinect.depth_width / 2)))));
								rdist = dist / Math.Cos(cra * SharedData.DEG_TO_RAD);
								angle = 360 + cra - shift_angle;
								angle %= 360;
								if (angle < 0)
									angle += 360;
								x = (rdist * Math.Sin(angle * SharedData.DEG_TO_RAD));
								y = (rdist * Math.Cos(angle * SharedData.DEG_TO_RAD));
								if (i > col)
									{
									if (dx_avg.Count == 3)
										{
										avg -= (double) dx_avg.Dequeue();
										dx_avg.Enqueue(x - last_x);
										avg += x - last_x;
										}
									else
										{
										dx_avg.Enqueue(x - last_x);
										avg += x - last_x;
										}
									}
								last_x = x;
								tw.WriteLine(x + "," + y + "," + cra);
								if ((Math.Abs(y - mean) > EDGE_DIST_LIMIT) || ((dx_avg.Count == 3) && (Math.Abs(avg/3) < min_dx)))
									{
									if ((Math.Abs(y - mean) > EDGE_DIST_LIMIT) || (mdx_count > 0))
										{
										tw.WriteLine();
										if (Math.Abs(y - mean) > EDGE_DIST_LIMIT)
											tw.WriteLine("Max dy triggered.");
										else
											tw.WriteLine("Min dx triggered.");
										ra = (int)Math.Round(Kinect.DepthHorDegrees((int)Math.Round((double)((i + 1) - (Kinect.depth_width / 2)))));
										dist = depthdata[row * Kinect.depth_width + (i + 1)] >> 3;
										rdist = Math.Round(CorrectedDistance(dist * SharedData.MM_TO_IN));
										edist = (int)Math.Round(rdist / Math.Cos(cra * SharedData.DEG_TO_RAD));
										tw.WriteLine("Edge RA (°) - " + ra + "   distance (in) - " + edist);
										rtn = true;
										break;
										}
									else
										mdx_count += 1;
									}
								}
							}
						}
					}
				else
					tw.WriteLine("Insufficent useable samples to determine mean.");
				tw.Close();
				Log.LogEntry("Saved " + fname);
				SaveRowDepth(ref depthdata, row, false);
				}
			return(rtn);
		}



		public static EdgeDetectTrigger FindEdge(ref short[] depthdata,int start_angle,ref int ra,ref int edist,bool high_col)

		{
			int col,row,i,dist,scol,max_row = -1,row_dist = 0,row_inc = 20;
			string dir,fname;
			TextWriter tw;
			double cra,rdist;
			int sample_count = 0,inc_count = 0;;
			double start_dist = 0, last_dist = -1,last_ra = -1;
			bool increase_dist = false, edge_detect = false;
			EdgeDetectTrigger edt = EdgeDetectTrigger.NONE;

			dir = Log.LogDir();
			col = (int) Math.Round((Kinect.depth_width / 2) - (start_angle * Kinect.depth_hor_pixals_per_degree));
			scol = col;
			row = FindCeiling((int) Math.Round((double) (Kinect.depth_width / 2)), depthdata,ref row_dist);
			if (row > max_row)
				{
				max_row = row;
				row_inc = (int)Math.Round(Math.Atan(((double)CEILING_ROW_OFFSET / row_dist)) * SharedData.RAD_TO_DEG * depth_hor_pixals_per_degree);
				}
			row = FindCeiling(col, depthdata,ref row_dist);
			if (row > max_row)
				{
				max_row = row;
				row_inc = (int)Math.Round(Math.Atan(((double)CEILING_ROW_OFFSET / row_dist)) * SharedData.RAD_TO_DEG * depth_hor_pixals_per_degree);
				}
			row = FindCeiling((int)Math.Round(col + (10 * Kinect.depth_hor_pixals_per_degree)), depthdata,ref row_dist);
			if (row > max_row)
				{
				max_row = row;
				row_inc = (int)Math.Round(Math.Atan(((double)CEILING_ROW_OFFSET / row_dist)) * SharedData.RAD_TO_DEG * depth_hor_pixals_per_degree);
				}
			row = FindCeiling((int)Math.Round(col + (20 * Kinect.depth_hor_pixals_per_degree)), depthdata,ref row_dist);
			if (row > max_row)
				{
				max_row = row;
				row_inc = (int)Math.Round(Math.Atan(((double)CEILING_ROW_OFFSET / row_dist)) * SharedData.RAD_TO_DEG * depth_hor_pixals_per_degree);
				}
			if (max_row >= 0)
				{
				row = max_row + row_inc;
				fname = dir + "Kinect Find Edge Data Set " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
				tw = File.CreateText(fname);
				tw.WriteLine("Kinect find edge data set: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				tw.WriteLine();
				tw.WriteLine("Start angle (°): " + start_angle);
				tw.WriteLine("Start column: " + col);
				tw.WriteLine("High column: " + high_col);
				tw.WriteLine("Row: " + row);
				tw.WriteLine();
				tw.WriteLine("RA (°),Dist (mm)");
				tw.Flush();
				if (high_col)
					{
					for (i = col; i > 0;i--)
						{
						dist = depthdata[(row * Kinect.depth_width) + i] >> 3;
						if (dist != Kinect.nui.DepthStream.UnknownDepth)
							{
							sample_count += 1;
							cra = (int)Math.Round(Kinect.DepthHorDegrees((int)Math.Round((double)(i - (Kinect.depth_width / 2)))));
							rdist = dist / Math.Cos(cra * SharedData.DEG_TO_RAD);
							tw.WriteLine(cra + "," + rdist.ToString("F4"));
							tw.Flush();
							if (sample_count == 1)
								start_dist = rdist;
							if (sample_count == 5)
								{
								if (rdist > start_dist)
									increase_dist = true;
								else
									increase_dist = false;
								}
							else if (sample_count > 5)
								{
								if (increase_dist && ((rdist - last_dist) > EDGE_DIST_LIMIT))
									{
									edge_detect = true;
									edt = EdgeDetectTrigger.DIST_CHANGE;
									tw.WriteLine();
									tw.WriteLine("Edge detect trigger: sudden distance change");
									break;
									}
								else if (!increase_dist && (rdist < last_dist))
									{
									inc_count += 1;
									if (inc_count == 2)
										{
										edge_detect = true;
										edt = EdgeDetectTrigger.MIN_DIST;
										tw.WriteLine();
										tw.WriteLine("Edge detect trigger: minmum distance");
										break;
										}
									}
								else if (!increase_dist)
									inc_count = 0;
								}
							last_ra = cra;
							last_dist = rdist;
							}
						}
					}
				else
					{
					for (i = col; i < Kinect.depth_width; i++)
						{
						dist = depthdata[(row * Kinect.depth_width) + i] >> 3;
						if (dist != Kinect.nui.DepthStream.UnknownDepth)
							{
							sample_count += 1;
							cra = (int)Math.Round(Kinect.DepthHorDegrees((int)Math.Round((double)(i - (Kinect.depth_width / 2)))));
							rdist = dist / Math.Cos(cra * SharedData.DEG_TO_RAD);
							tw.WriteLine(cra + "," + rdist.ToString("F4"));
							tw.Flush();
							if (sample_count == 1)
								start_dist = rdist;
							if (sample_count == 5)
								{
								if (rdist > start_dist)
									increase_dist = true;
								else
									increase_dist = false;
								}
							else if (sample_count > 5)
								{
								if (increase_dist && ((rdist - last_dist) > EDGE_DIST_LIMIT))
									{
									edge_detect = true;
									edt = EdgeDetectTrigger.DIST_CHANGE;
									tw.WriteLine();
									tw.WriteLine("Edge detect trigger: sudden distance change");
									break;
									}
								else if (!increase_dist && (rdist < last_dist))
									{
									inc_count += 1;
									if (inc_count == 2)
										{
										edge_detect = true;
										edt = EdgeDetectTrigger.MIN_DIST;
										tw.WriteLine();
										tw.WriteLine("Edge detect trigger: minmum distance");
										break;
										}
									}
								else if (!increase_dist)
									inc_count = 0;
								}
							last_ra = cra;
							last_dist = rdist;
							}
						}
					}
				if (edge_detect )
					{
					if (increase_dist)
						{
						ra = (int) Math.Round(last_ra);
						edist = (int) Math.Round(last_dist * SharedData.MM_TO_IN);
						}
					else
						{
						int inc;

						if (high_col)
							inc = 2;
						else
							inc = -2;
						dist = depthdata[(row * Kinect.depth_width) + (i + inc)] >> 3;
						cra = (int)Math.Round(Kinect.DepthHorDegrees((int)Math.Round((double)((i + inc) - (Kinect.depth_width / 2)))));
						rdist = dist / Math.Cos(cra * SharedData.DEG_TO_RAD);
						ra = (int) Math.Round(cra);
						edist = (int)Math.Round(Math.Round(CorrectedDistance(rdist * SharedData.MM_TO_IN)));
						}
					tw.WriteLine("Edge found with relative angle - " + ra + "°   distance - " + edist + " in.");
					}
				else
					{
					ra = -1;
					edist = -1;
					tw.WriteLine("No edge detected");
					}
				tw.Close();
				Log.LogEntry("Saved " + fname);
				SaveRowDepth(ref depthdata, row, false);
				}
			return(edt);
		}



		private static Arm.Loc3D PtLocation(int row,int col,double tilt_correct, SkeletonPoint[] sips)

		{
			Arm.Loc3D loc = new Arm.Loc3D();
			int pixel;
			DenseMatrix mat;
			DenseVector result,vec;
			double tilt,kfdd;

			pixel = (row * Kinect.nui.ColorStream.FrameWidth) + (Kinect.nui.ColorStream.FrameWidth - col - 1);
			loc.x = -(sips[pixel].X * M_TO_IN);
			tilt = HeadAssembly.TiltAngle();
			tilt += tilt_correct;
			kfdd = Arm.KinectForwardDist(tilt);
			mat = DenseMatrix.OfArray(new[,] { { Math.Cos(-tilt * SharedData.DEG_TO_RAD), -Math.Sin(-tilt * SharedData.DEG_TO_RAD) }, { Math.Sin(-tilt * SharedData.DEG_TO_RAD), Math.Cos(-tilt * SharedData.DEG_TO_RAD) } });
			vec = new DenseVector(new[] {(double) sips[pixel].Z,(double) sips[pixel].Y});
			result = vec * mat;
			loc.z = (result.Values[0] * M_TO_IN) + kfdd;
			loc.y = result.Values[1] * M_TO_IN;
			return (loc);
		}



		public static bool KinectFrontClear(int dist,double tilt_correct,double min_detect_height,ref int min_dist,ref int obs_no)

		{
			bool rtn = true;
			int row,col,no_obs = 0,mdist = dist + 1,i;
			double kh,htilt,value,maxx = 0,minx = 0;
			Arm.Loc3D target_loc = new Arm.Loc3D();
			string fname = "";
			BinaryWriter bw = null;
			ArrayList obs = new ArrayList();
			byte[] videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
			Bitmap bm;
			Graphics g;

			Log.LogEntry("KinectFrontClear: " + dist);
			if (Kinect.GetColorFrame(ref videodata,60) && Kinect.GetDepthFrame(ref depthdata2,60))
				{
				Kinect.nui.CoordinateMapper.MapDepthFrameToSkeletonFrame(Kinect.nui.DepthStream.Format, depthdata2, sips);
				rtn = true;
				min_dist = dist + 1;
				if (SharedData.log_operations)
					{
					fname = Log.LogDir() + "Kinect floor scan " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".pc";
					bw = new BinaryWriter(File.Open(fname, FileMode.Create));
					}
				htilt = HeadAssembly.TiltAngle();
				htilt += tilt_correct;
				kh = Arm.KinectHeight(htilt);
				bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				bm.RotateFlip(RotateFlipType.Rotate180FlipY);
				g = System.Drawing.Graphics.FromImage(bm);
				for (row = 0; row < Kinect.nui.ColorStream.FrameHeight; row++)
					{
					for (col = 0; col < Kinect.nui.ColorStream.FrameWidth; col++)
						{
						target_loc = PtLocation(row,col,tilt_correct,sips);
						if ((target_loc.z > 0) && (target_loc.z <= dist))
							{
							if ((target_loc.x == 0)  && (col != 319))
								{
								//ignor since this is an unknown location
								}
							else if (Math.Abs(target_loc.x) <= ((double) SharedData.ROBOT_WIDTH/2) + 1 )
								{
								if ((kh + target_loc.y) > min_detect_height)
									{
									no_obs += 1;
									g.DrawRectangle(Pens.Red,col,row, 1, 1);
									if (target_loc.z < mdist)
										mdist = (int) Math.Floor(target_loc.z);
									if (target_loc.x > maxx)
										maxx = target_loc.x;
									else if (target_loc.x < minx)
										minx = target_loc.x;
									}
								}
							}
						}
					}
				min_dist = mdist;
				obs_no = no_obs;
				Log.LogEntry("Found " + no_obs + " obstacles with min distance of " + mdist + " in., a min x of " + minx.ToString("F1") + " and max x of " + maxx.ToString("F1"));
				bm.Save(fname.Replace(".pc",".bmp"),ImageFormat.Bmp);
				Log.LogEntry("Saved " + fname.Replace(".pc", ".bmp"));
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
					}
				}
			else
				Log.LogEntry("KinectFrontClearance could not obtain depth frame.");
			return(rtn);
		}



		public static bool Operational()

		{
			bool rtn = false;

			if ((nui != null) && (nui.IsRunning))
				{
				SharedData.kinect_operational = true;
				rtn = true;
				}
			return(rtn);
		}


		}
	}
