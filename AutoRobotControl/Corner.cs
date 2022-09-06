using System;
using System.Threading;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;
using System.Diagnostics;
using System.Collections;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AutoRobotControl
	{
	public class Corner: FeatureMatch
		{

		private const int MIN_SIDE_WALL_INCLINE = (int) (5 * SharedData.IN_TO_MM);
		private const int MAX_BACK_WALL_INCLINE = (int) (2 * SharedData.IN_TO_MM);
		private const int PASS_COUNT_LIMIT = 5;

		public struct depthloc
			{
			public Point coord;
			public int dist;

			public override string ToString()

			{
				return("{" + this.coord.X  + "  " + this.coord.Y + "}  " + this.dist);
			}

			};

		private short[] depthdata;
		private bool perp_backwall,corner_right;


		public Corner()

		{
			depthdata = new short[Kinect.nui.DepthStream.FramePixelDataLength];
		}



		private bool GlobalMaxSearch(ref depthloc loc)

		{
			int max_depth = -1, depth, start,i,mdi = -1,retry = 0,bad_pixel = 0;
			bool rtn = false,done;

			Log.LogEntry("Corner.GlobalMaxSearch " + loc.ToString());
			do
				{
				done = true;
				start = loc.coord.Y * Kinect.nui.DepthStream.FrameWidth;
				max_depth = -1;
				for (i = start;i < start + Kinect.nui.DepthStream.FrameWidth;i += 1)
					{
					depth = depthdata[i];
					if (depth >= 0)
						{
						if (depth == Kinect.nui.DepthStream.TooFarDepth)
							{
							if (SharedData.log_operations)
								RecordCornerDepth(loc);
							mdi = -1;
							Log.LogEntry("Corner.GlobalMaxSearch: match failed, too far distance encountered looking for corner.");
							break;
							}
						else if (depth == Kinect.nui.DepthStream.TooNearDepth)
							depth = (int) (31 * SharedData.IN_TO_MM);
						if (depth > max_depth)
							{
							max_depth = depth;
							mdi = i;
							}
						}
					else
						{
						bad_pixel += 1;
						if (bad_pixel > Kinect.nui.DepthStream.FrameWidth/2)
							{
							mdi = -1;
							Log.KeyLogEntry("Corner.GlobalMaxSearch: too many unknown depths in data.");
							break;
							}
						}
					}
				if (mdi > 0)
					{
					int no_pixals,aslope1 = 0,aslope2 = 0,col;

					no_pixals = (int) (1/((max_depth * SharedData.MM_TO_FT)/Kinect.nui.DepthStream.FrameWidth));
					col = mdi % Kinect.nui.DepthStream.FrameWidth;
					if ((col > no_pixals) && (col + no_pixals < Kinect.nui.DepthStream.FrameWidth))
						{
						aslope1 = depthdata[mdi] - depthdata[mdi - no_pixals];
						if (aslope1 > MIN_SIDE_WALL_INCLINE)
							{
							aslope2 = depthdata[mdi + no_pixals] - depthdata[mdi];
							if (aslope2 <= 0)
								{
								loc.coord.X = col;
								loc.dist = max_depth;
								rtn = true;
								}
							}
						}
					if (loc.coord.X == 0)
						{
						if (SharedData.log_operations)
							{
							depthloc dloc = new depthloc();

							Log.LogEntry("Corner.GlobalMaxSearch:  failed corner line check w slope 1 - " + aslope1  + "  slope 2 - " + aslope2);
							dloc.coord.X = col;
							dloc.coord.Y = loc.coord.Y;
							RecordCornerDepth(dloc);
							}
						if ((aslope1 > MIN_SIDE_WALL_INCLINE/2) && (retry < 1))
							{
							loc.coord.Y = Kinect.FindCeiling(col,depthdata);
							if (loc.coord.Y >= 0)
								{
								loc.coord.Y += 20;
								retry += 1;
								done = false;
								Thread.Sleep(10);
								}
							}
						}
					}
				}
			while(!done);
			return(rtn);
		}



		private bool LowHighInflectSearch(ref depthloc loc)

		{
			int max_depth = -1, depth, start,i,mdi = -1,pass_count = 0,bad_pixel = 0;
			bool rtn = false;

			Log.LogEntry("Corner.LowHighInflectSearch " + loc.ToString());
			start = loc.coord.Y * Kinect.nui.DepthStream.FrameWidth;
			max_depth = -1;
			for (i = start;i < start + Kinect.nui.DepthStream.FrameWidth;i += 1)
				{
				depth = depthdata[i];
				if (depth >= 0)
					{
					if (depth == Kinect.nui.DepthStream.TooFarDepth)
						{
						if (SharedData.log_operations)
							RecordCornerDepth(loc);
						mdi = -1;
						Log.LogEntry("Corner.LowHighInflectSearch: match failed, too far distance encountered looking for corner.");
						break;
						}
					else if (depth == Kinect.nui.DepthStream.TooNearDepth)
						depth = (int) (31 * SharedData.IN_TO_MM);
					if (depth > max_depth)
						{
						max_depth = depth;
						mdi = i;
						pass_count = 0;
						}
					else if (mdi > 0)
						{
						pass_count += 1;
						if (pass_count > PASS_COUNT_LIMIT)
							break;
						}
					}
				else
					{
					bad_pixel += 1;
					if (bad_pixel > Kinect.nui.DepthStream.FrameWidth/2)
						{
						mdi = -1;
						Log.KeyLogEntry("Corner.LowHighInflectSearch: too many unknown depths in data.");
						break;
						}
					}
				}
			if (mdi > 0)
				{
				int no_pixals,aslope,col;

				no_pixals = (int) (1/((max_depth * SharedData.MM_TO_FT)/(Kinect.nui.DepthStream.FrameWidth * 2)));
				col = mdi % Kinect.nui.DepthStream.FrameWidth;
				if ((col > no_pixals) && (col + no_pixals < Kinect.nui.DepthStream.FrameWidth))
					{
					aslope = depthdata[mdi] - depthdata[mdi - no_pixals];
					if (aslope > MIN_SIDE_WALL_INCLINE)
						{
						aslope = depthdata[mdi + no_pixals] - depthdata[mdi];
						if (aslope < MAX_BACK_WALL_INCLINE)
							{
							loc.coord.X = col;
							loc.dist = max_depth;
							rtn = true;
							}
						}
					}
				if (SharedData.log_operations)
					{
					if (loc.coord.X == 0)
						{
						depthloc dloc = new depthloc();

						Log.LogEntry("Corner.LowHighInflectSearch:  failed corner line check.");
						dloc.coord.X = col;
						dloc.coord.Y = loc.coord.Y;
						RecordCornerDepth(dloc);
						}
					}
				}
			return(rtn);
		}



		private bool HighLowInflectSearch(ref depthloc loc)

		{
			int max_depth = -1, depth, start,end,i,mdi = -1,pass_count = 0,bad_pixel = 0;
			bool rtn = false;

			Log.LogEntry("Corner.HighLowInflectSearch " + loc.ToString());
			end = loc.coord.Y * Kinect.nui.DepthStream.FrameWidth;
			start = ((loc.coord.Y + 1) * Kinect.nui.DepthStream.FrameWidth) - 1;
			max_depth = -1;
			for (i = start;i > end;i -= 1)
				{
				depth = depthdata[i];
				if (depth >= 0)
					{
					if (depth == Kinect.nui.DepthStream.TooFarDepth)
						{
						if (SharedData.log_operations)
							RecordCornerDepth(loc);
						mdi = -1;
						Log.LogEntry("Corner.HighLowInflectSearch: match failed, too far distance encountered looking for corner.");
						break;
						}
					else if (depth == Kinect.nui.DepthStream.TooNearDepth)
						depth = (int) (31 * SharedData.IN_TO_MM);
					if (depth > max_depth)
						{
						max_depth = depth;
						mdi = i;
						pass_count = 0;
						}
					else if (mdi > 0)
						{
						pass_count += 1;
						if (pass_count > PASS_COUNT_LIMIT)
							break;
						}
					}
				else
					{
					bad_pixel += 1;
					if (bad_pixel > Kinect.nui.DepthStream.FrameWidth/2)
						{
						mdi = -1;
						Log.KeyLogEntry("Corner.HighLowInflectSearch: too many unknown depths in data.");
						break;
						}
					}
				}
			if (mdi > 0)
				{
				int no_pixals,aslope,col;

				no_pixals = (int) (1/((max_depth * SharedData.MM_TO_FT)/(Kinect.nui.DepthStream.FrameWidth * 2)));
				col = mdi % Kinect.nui.DepthStream.FrameWidth;
				if ((col > no_pixals) && (col + no_pixals < Kinect.nui.DepthStream.FrameWidth))
					{
					aslope = depthdata[mdi + no_pixals] - depthdata[mdi];
					if (aslope > MIN_SIDE_WALL_INCLINE)
						{
						aslope = depthdata[mdi] - depthdata[mdi - no_pixals];
						if (aslope < MAX_BACK_WALL_INCLINE)
							{
							loc.coord.X = col;
							loc.dist = max_depth;
							rtn = true;
							}
						}
					}
				if (SharedData.log_operations)
					{
					if (loc.coord.X == 0)
						{
						depthloc dloc = new depthloc();

						Log.LogEntry("Corner.HighLowInflectSearch:  failed corner line check.");
						dloc.coord.X = col;
						dloc.coord.Y = loc.coord.Y;
						RecordCornerDepth(dloc);
						}
					}
				}
			return(rtn);
		}




		private depthloc CornerMatch(int check_col)

		{
			depthloc loc = new depthloc();
			int i;

			loc.coord.Y = Kinect.FindCeiling(check_col,depthdata);
			if ((SharedData.log_operations) && (loc.coord.Y == -1))
				RecordColDepth(check_col);
			if (loc.coord.Y >= 0)
				{
				for (i = 0;i < 2;i++)
					{
					loc.coord.Y += 20;
					if (loc.coord.Y < Kinect.nui.DepthStream.FrameHeight)
						{
						if (!perp_backwall)
							GlobalMaxSearch(ref loc);
						else if (corner_right)
							LowHighInflectSearch(ref loc);
						else
							HighLowInflectSearch(ref loc);
						if (loc.coord.X > 0)
							break;
						}
					else
						break;
					}
				}
			else
				Log.LogEntry("Corner.CornerMatch: match failed, could not find ceiling.");
			return(loc);
		}



		private depthloc CornerMatch()

		{
			depthloc loc = new depthloc();

			loc = CornerMatch(Kinect.nui.DepthStream.FrameWidth/2);
			if (loc.coord.X > 0)
				loc = CornerMatch(loc.coord.X);
			return(loc);
		}




		private void RecordColDepth(int col)

		{
			string chart_dir,file;
			StreamWriter sw;
			int i,no_samples = 0;
			double depth;
			Graphics g;
			Bitmap bmc;

			chart_dir = Log.LogDir();
			bmc = depthdata.ToBitmap(Kinect.depth_width, Kinect.depth_height, 0, Color.White); ;
			g = System.Drawing.Graphics.FromImage(bmc);
			g.DrawLine(Pens.Red, col, 0, col, Kinect.nui.DepthStream.FrameHeight);
			file = chart_dir + "coldepth pic " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second + "-" + SharedData.GetUFileNo() + ".jpg";
			bmc.Save(file, ImageFormat.Jpeg);
			bmc = null;
			Log.LogEntry("Corner.RecordColDepth: saved " + file);
			file = chart_dir + "ColDepthChart " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second + "-" + SharedData.GetUFileNo() + ".csv";
			sw = new StreamWriter(file);
			if (sw != null)
				{
				Log.LogEntry("Corner.RecordColDepth: saved " + file);
				sw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				sw.WriteLine("Col: " + col);
				sw.WriteLine();
				sw.WriteLine("Index,Distance (in)");
				for (i = col;i < Kinect.nui.DepthStream.FramePixelDataLength;i += Kinect.depth_width)
					{
					depth = depthdata[i] * SharedData.MM_TO_IN;
					sw.WriteLine(no_samples.ToString() + "," + depth);
					no_samples += 1;
					}
				sw.Close();
				}
		}


		private void RecordCornerDepth(depthloc dl)
			
		{
			string chart_dir,file;
			StreamWriter sw;
			int i,start_pos,end_pos,no_samples = 0;
			double depth;
			Bitmap bmc;
			Graphics g;

			chart_dir = Log.LogDir();
			bmc = depthdata.ToBitmap(Kinect.depth_width, Kinect.depth_height, 0, Color.White);
			g = System.Drawing.Graphics.FromImage(bmc);
			g.DrawLine(Pens.Red, 0, dl.coord.Y, Kinect.nui.DepthStream.FrameWidth, dl.coord.Y);
			g.DrawLine(Pens.Red, dl.coord.X, 0, dl.coord.X, Kinect.nui.DepthStream.FrameHeight);
			file = chart_dir + "cornerdepth pic " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second + "-" + SharedData.GetUFileNo() + ".jpg";
			bmc.Save(file, ImageFormat.Jpeg);
			bmc = null;
			Log.LogEntry("Corner.RecordCornerDepth: saved " + file);
			file = chart_dir + "CornerDepthChart " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second + "-" + SharedData.GetUFileNo() + ".csv";
			sw = new StreamWriter(file);
			if (sw != null)
				{
				Log.LogEntry("Corner.RecordCornerDepth: saved " + file); 
				sw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				sw.WriteLine("Row: " + dl.coord.Y);
				sw.WriteLine();
				sw.WriteLine("Index,Distance (in)");
				start_pos = (dl.coord.Y * Kinect.depth_width);
				end_pos = start_pos + Kinect.depth_width;
				for (i = start_pos;i < end_pos; i++)
					{
					depth = depthdata[i] * SharedData.MM_TO_IN;
					sw.WriteLine(no_samples.ToString() + "," + depth);
					no_samples += 1;
					}
				sw.Close();
				}
		}



		public Room.feature_match MatchKinect(NavData.feature f,params object[] obj)

		{
			Room.feature_match fm = new Room.feature_match();
			depthloc floc;
			int i;

			fm.matched = false;
			if ((f.type == NavData.FeatureType.CORNER) && (obj.Length == 2))
				{
				perp_backwall = (bool) obj[0];
				corner_right = (bool) obj[1];
				if (Kinect.GetDepthFrame(ref depthdata,30))
					{
					for (i = 0;i < Kinect.nui.DepthStream.FramePixelDataLength;i++)
						depthdata[i] = (short) (depthdata[i] >> 3);
					floc = CornerMatch();
					if (floc.coord.X > 0)
						{
						fm.matched = true;
						fm.distance = (int) (floc.dist * SharedData.MM_TO_IN);
						fm.ra = Kinect.DepthHorDegrees((int)Math.Round(floc.coord.X - ((double) Kinect.nui.DepthStream.FrameWidth / 2)));
						Log.LogEntry("Corner: col - " + floc.coord.X.ToString() + "  ra - " + fm.ra.ToString() + "  dist - " + fm.distance);
						if (SharedData.log_operations)
							{
							RecordColDepth(Kinect.nui.DepthStream.FrameWidth / 2);
							RecordCornerDepth(floc);
							}
						}
					else
						Log.LogEntry("Could not find corner.");
					}
				else
					Log.LogEntry("Could not collect depth data.");
				}
			else
				Log.LogEntry("Corner.MatchKinect: wrong type of feature - " + f.ToString() + " or number passed parameters - " + obj.Length);
			return(fm);
		}


		}
	}
