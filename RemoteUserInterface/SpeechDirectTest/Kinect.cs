using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;

namespace AutoRobotControl
	{

	public static class Kinect
		{
		public enum EdgeDetectTrigger { NONE, DIST_CHANGE, MIN_DIST,EITHER };

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

		private static double[,] depth_cal;
		private static short[] depthdata = null;
		private static DepthImagePixel[] depthdata2;
		private static DepthImagePoint[] dips;
		private static AutoResetEvent frame_complete = new AutoResetEvent(false);
		
		private static object get_video_frame = new object();
		private static object get_depth_frame = new object();


		public static bool Open()

		{
			bool rtn = false;

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
					depth_hor_pixals_per_degree = nui.DepthStream.FrameWidth / nui.DepthStream.NominalHorizontalFieldOfView;
					depth_vert_pixals_per_degree = nui.DepthStream.FrameHeight/nui.DepthStream.NominalVerticalFieldOfView;
					avg_hor_pixals_per_degree = nui.ColorStream.FrameWidth / nui.ColorStream.NominalHorizontalFieldOfView;
					vert_pixals_per_degree = nui.ColorStream.FrameHeight / nui.ColorStream.NominalVerticalFieldOfView;
					SharedData.kinect_operational = true;
					depthdata = new short[Kinect.nui.DepthStream.FramePixelDataLength];
					depthdata2 = new DepthImagePixel[Kinect.nui.DepthStream.FramePixelDataLength];
					dips = new DepthImagePoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];
					SetFarRange();
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
			double cdist = 0,d1,d2,p;
			int i;

			if ((depth_cal == null) || (depth_cal.Length == 0) || (dist <= 0))
				cdist = dist;
			else
				{
				for (i = 0;i < depth_cal.Length/2;i++)
					{
					if (dist < depth_cal[1,i])
						{
						d1 = depth_cal[0,i - 1]/depth_cal[1,i - 1];
						if (d1.Equals(double.NaN))
							d1 = 1;
						d2 = depth_cal[0, i] / depth_cal[1, i];
						p = ((dist - depth_cal[1,i-1])/(depth_cal[1,i] - depth_cal[1,i-1]));
						cdist = ((p * (d2 - d1)) + d1) * dist;
						break;
						}
					}
				if (i == depth_cal.Length/2)
					{
					d1 = depth_cal[0, i - 1] / depth_cal[1, i - 1];
					cdist = d1 * dist;
					}
				}
			return(cdist);
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
			double val = 0,adj;

			adj = ((double)Kinect.nui.ColorStream.FrameWidth / 2) / Math.Tan((nui.ColorStream.NominalHorizontalFieldOfView / 2) * SharedData.DEG_TO_RAD);
			val = Math.Atan(no_pixel / adj) * SharedData.RAD_TO_DEG;
			return (val);
		}



		public static double DepthVerDegrees(int no_pixel)

		{
			double val = 0,adj;

			adj = ((double) Kinect.nui.DepthStream.FrameHeight/2) / Math.Tan(Kinect.nui.DepthStream.NominalVerticalFieldOfView / 2 * SharedData.DEG_TO_RAD);
			val = Math.Atan(no_pixel / adj) * SharedData.RAD_TO_DEG;
			return (val);
		}



		public static double DepthHorDegrees(int no_pixel)

		{
			double val = 0;
			double adj;

			adj = ((double)Kinect.nui.DepthStream.FrameWidth / 2) / Math.Tan(nui.DepthStream.NominalHorizontalFieldOfView/2 * SharedData.DEG_TO_RAD);
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



		private static double KinectForwardDistDelta(double tilt)

		{
			double fdd,cfa,cfd,x2;

			cfa = Math.Atan(SharedData.KINECT_FRONT_OFFSET / SharedData.KINECT_FRONT_OFFSET) * SharedData.RAD_TO_DEG;
			cfd = Math.Sqrt((SharedData.KINECT_CENTER_OFFSET * SharedData.KINECT_CENTER_OFFSET) + (SharedData.KINECT_FRONT_OFFSET * SharedData.KINECT_FRONT_OFFSET));
			x2 = cfd * Math.Sin((cfa + tilt) * SharedData.DEG_TO_RAD);
			fdd = SharedData.KINECT_FRONT_OFFSET - x2;
			return (fdd);
		}


		private static double KinectHeight(double tilt)

		{
			double kh,y2,cfa,cfd;

			cfa = Math.Atan(SharedData.KINECT_FRONT_OFFSET/ SharedData.KINECT_FRONT_OFFSET) * SharedData.RAD_TO_DEG;
			cfd = Math.Sqrt((SharedData.KINECT_CENTER_OFFSET * SharedData.KINECT_CENTER_OFFSET) + (SharedData.KINECT_FRONT_OFFSET * SharedData.KINECT_FRONT_OFFSET));
			y2 = cfd * Math.Cos((cfa + tilt) * SharedData.DEG_TO_RAD);
			kh = SharedData.BASE_KINECT_HEIGHT + (SharedData.KINECT_CENTER_OFFSET - y2);
			return(kh);
		}



/*		static public bool MapDepthCore(ref byte[,] map,ref int min_floor_detect_dist)

		{
			bool rtn = false;
			int row = 0, col = 0,pixel = 0;
			double pdist, ray = 0, rax = 0, acd = 0, x, depth, fdist, frow_fdist = 0;
			double tilt_angle, tilt_err, kinect_height, acd_dist_limit,fdd;
			int xr = 0, dr = 0;
			System.Drawing.Bitmap bm = null;
			DateTime now = DateTime.Now;
			string fname = "";
			TextWriter tw = null;

			if (SharedData.log_operations)
				{
				bm = depthdata.ToBitmap(Kinect.nui.DepthStream.FrameWidth, Kinect.nui.DepthStream.FrameHeight, 0, Color.White);
				fname = Log.LogDir() + "MapDepth depth data set " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".csv";
				tw = File.CreateText(fname);
				if (tw != null)
					{
					tw.WriteLine(fname);
					tw.WriteLine();
					tw.WriteLine("X,Z");
					}
				}
			tilt_angle = Math.Abs(HeadAssembly.TiltAngle());
			tilt_err = TILT_ERROR;
			tilt_angle += tilt_err;
			fdd = KinectForwardDistDelta(tilt_angle);
			kinect_height = KinectHeight(tilt_angle);

			try
			{
			rtn = true;
			for (row = Kinect.depth_height - 1; row >= 0; row--)
				{
				ray = DepthVerDegrees(row - (Kinect.depth_height / 2));
				acd = kinect_height / Math.Cos((90 - (tilt_angle + ray)) * SharedData.DEG_TO_RAD);
				acd_dist_limit = (Kinect.nui.DepthStream.TooFarDepth * SharedData.MM_TO_IN) / Math.Sin((90 - (tilt_angle + ray)) * SharedData.DEG_TO_RAD);
				for (col = 0; col < Kinect.depth_width; col++)
					{
					pixel = (row * Kinect.depth_width) + col;
					if (dips[pixel].Depth > 0)
						{
						depth = Math.Abs(Kinect.CorrectedDistance(dips[pixel].Depth * SharedData.MM_TO_IN));
						rax = -Kinect.DepthHorDegrees(col - (Kinect.depth_width / 2));
						x = depth * Math.Tan(rax * SharedData.DEG_TO_RAD);
						pdist = depth / Math.Cos(ray * SharedData.DEG_TO_RAD);
						fdist = pdist * Math.Sin((90 - (tilt_angle + ray)) * SharedData.DEG_TO_RAD) - fdd;
						if ((row == Kinect.depth_height - 1) && (col == Kinect.depth_width / 2))
							frow_fdist = fdist;
						if (acd < acd_dist_limit)
							{
							if (pdist < acd)
								{
								xr = (int)Math.Round(x);
								dr = (int)Math.Round(fdist);
								if (map[Kinect.depth_width / 2 + xr,map.GetLength(1) - dr] == 0)
									{
									map[Kinect.depth_width / 2 + xr, map.GetLength(1) - dr] = 1;
									if (tw != null)
										tw.WriteLine(xr + "," + dr);
									}
								}
							}
						else
							{
							xr = (int)Math.Round(x);
							dr = (int)Math.Round(fdist);
							if (map[Kinect.depth_width / 2 + xr, map.GetLength(1) - dr] == 0)
								{
								map[Kinect.depth_width / 2 + xr, map.GetLength(1) - dr] = 1;
								if (tw != null)
									tw.WriteLine(xr + "," + dr);
								}
							}
						}
					}
				}
			}

			catch (Exception ex)
			{
			rtn = false;
			Log.LogEntry("MapDepthCore exception : " + ex.Message );
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			Log.LogEntry("row " + row + "   col " + col + "   pixel " + pixel);
			if (pixel < Kinect.nui.ColorStream.FramePixelDataLength)
				Log.LogEntry("depth (mm) " + depthdata[pixel]);
			Log.LogEntry("ray " + ray + "   rax " + rax);
			Log.LogEntry("x " + xr + "   floor dist " + dr);
			Log.LogEntry("Map dimensions " + map.GetLength(0) + " X " + map.GetLength(1));
			}

			min_floor_detect_dist = (int) Math.Floor(frow_fdist);
			if (tw != null)
				{
				tw.Close();
				Log.LogEntry("Saved: " + fname);
				}
			if (bm != null)
				{
				bm = NavData.MapToBitmap(map);
				fname = Log.LogDir() + "MapDepth depth map " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".bmp";
				bm.Save(fname,ImageFormat.Bmp);
				Log.LogEntry("Saved: " + fname);
				}
			Log.LogEntry("MapDepthCore " + rtn);
			return (rtn);
		}



		static public bool MapDepth(ref byte[,] map,ref int min_floor_detect_dist)

		{
			bool rtn = false;

			if (AutoRobotControl.HeadAssembly.Tilt(TILT_ANGLE, true))
				{
				if (Kinect.GetDepthFrame(ref depthdata2, 40))
					{
					Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, depthdata2, dips);
					rtn = MapDepthCore(ref map,ref min_floor_detect_dist);
					}
				}
			AutoRobotControl.HeadAssembly.Tilt(0, true);
			Log.LogEntry("MapDepth " + rtn);
			return (rtn);
		}



		static public int CheckMapObstacles(Point spt,int direct,int dist,int sc,byte[,] map,ref bool right,ref bool edge)

		{
			int mfdist = -1;
			int pdirect, i, j, hwdist;
			Point npt, fpt;
			bool obs_found = false;

			Log.LogEntry("CheckMapObstacles: " + spt + "  " + direct + "  " + dist + "  " + sc);
			pdirect = (direct + 90) % 360;
			hwdist = (int)(Math.Ceiling((double) SharedData.ROBOT_WIDTH / 2) + sc);
			spt = new Point((int) Math.Round((double) map.GetLength(0) / 2), map.GetLength(1) - 1);
			for (i = 5; i < dist; i++)
				{
				npt = new Point((int)Math.Round(spt.X + (i * Math.Sin(direct * SharedData.DEG_TO_RAD))), (int)Math.Round(spt.Y - (i * Math.Cos(direct * SharedData.DEG_TO_RAD))));
				for (j = -hwdist; j < hwdist; j++)
					{
					fpt = new Point((int)Math.Round(npt.X + (j * Math.Sin(pdirect * SharedData.DEG_TO_RAD))), (int)Math.Round(npt.Y - (j * Math.Cos(pdirect * SharedData.DEG_TO_RAD))));

					try
						{
						if (map[fpt.X, fpt.Y] == 1)
							{
							obs_found = true;
							edge = false;
							mfdist = i;
							if (fpt.X > spt.X)
								{
								right = true;
								Log.LogEntry("Obstacle @ (" + fpt.X + " " + fpt.Y + ")  " + mfdist + "  right");
								}
							else
								{
								right = false;
								Log.LogEntry("Obstacle @ (" + fpt.X + " " + fpt.Y + ")  " + mfdist + "  left");
								}
							break;
							}
						}

					catch (Exception)
						{
						obs_found = true;
						edge = true;
						mfdist = i;
						Log.LogEntry("Edge @ (" + fpt.X + " " + fpt.Y + ")  " + mfdist);
						break;
						}

					}
				if (obs_found)
					break;
				}
			if (!obs_found)
				Log.LogEntry("No obstacle found.\r\n");
			return (mfdist);
		} */



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
