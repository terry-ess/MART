using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AutoRobotControl;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;

namespace Room_Survey
	{
	static class KinectExt
		{
		private const double KINECT_HEIGHT_0 = 58.5;
		private const double KINECT_HEIGHT_D3O = 57.75;
		private const double TILT_ERROR = 2;
		private const int TILT_ANGLE = -30;

		private static short[] depthdata = new short[Kinect.nui.DepthStream.FramePixelDataLength];

		public static int kinect_max_depth = (int) Math.Floor(Kinect.nui.DepthStream.MaxDepth * SharedData.MM_TO_IN);


		private static double DepthVerDegrees(int no_pixel)

		{
			double val = 0,adj;

			adj = ((double) Kinect.nui.DepthStream.FrameHeight/2) / Math.Tan(Kinect.nui.DepthStream.NominalVerticalFieldOfView / 2 * SharedData.DEG_TO_RAD);
			val = Math.Atan(no_pixel / adj) * SharedData.RAD_TO_DEG;
			return (val);
		}



		private static double DepthHorDegrees(int no_pixel)

		{
			double val = 0;
			double adj;

			adj = ((double) Kinect.nui.DepthStream.FrameWidth/2)/Math.Tan(Kinect.nui.DepthStream.NominalHorizontalFieldOfView/2 * SharedData.DEG_TO_RAD);
			val = Math.Atan(no_pixel/adj) * SharedData.RAD_TO_DEG;
			return (val);
		}



		public static double FindObstacles(int dist_limit,double side_clear, ref double min_floor_detect_dist)

		{
			int i;
			System.Drawing.Bitmap bm = null;
			Graphics g;
			string fname;
			DateTime now = DateTime.Now;
			int row = 0, col = 0, pixel, no_obstacle_hits = 0, no_acd_hits = 0, no_fdist_hits = 0;
			double pdist, ray, rax, acd = 0, acd_dist_limit = 0, x, depth, width_limit, fdist, max_dif = 0, frow_fdist = -1, max_fdist_dif = 0;
			double tilt_angle, tilt_err, min_obs_dist = Kinect.nui.DepthStream.MaxDepth * SharedData.MM_TO_IN, kinect_height;

			Log.LogEntry("FindObstacles: " + dist_limit + "  " + side_clear);
			if (AutoRobotControl.HeadAssembly.Tilt(TILT_ANGLE,true))
				{
				if (Kinect.GetDepthFrame(ref depthdata,40))
					{
					for (i = 0; i < Kinect.nui.DepthStream.FramePixelDataLength; i++)
						{
						depthdata[i] = (short)(depthdata[i] >> 3);
						}
					if (SharedData.log_operations)
						{
						bm = depthdata.ToBitmap(Kinect.nui.DepthStream.FrameWidth, Kinect.nui.DepthStream.FrameHeight, 0, Color.White);
						g = System.Drawing.Graphics.FromImage(bm);
						g.DrawLine(Pens.Red, 0, Kinect.nui.DepthStream.FrameHeight / 2, bm.Width, Kinect.nui.DepthStream.FrameHeight / 2);
						g.DrawLine(Pens.Red, Kinect.nui.DepthStream.FrameWidth / 2, 0, Kinect.nui.DepthStream.FrameWidth / 2, bm.Height);
						}
					tilt_angle = Math.Abs(HeadAssembly.TiltAngle());
					tilt_err = TILT_ERROR;
					tilt_angle += tilt_err;
					kinect_height = KINECT_HEIGHT_0 - ((KINECT_HEIGHT_0 - KINECT_HEIGHT_D3O) * (tilt_angle / 30));
					width_limit = ((double) SharedData.ROBOT_WIDTH / 2) + side_clear;
					for (row = Kinect.depth_height - 1; row >= 0; row--)
						{
						ray = DepthVerDegrees(row - (Kinect.depth_height / 2));
						acd = kinect_height / Math.Cos((90 - (tilt_angle + ray)) * SharedData.DEG_TO_RAD);
						acd_dist_limit = dist_limit / Math.Sin((90 - (tilt_angle + ray)) * SharedData.DEG_TO_RAD);
						for (col = 0; col < Kinect.depth_width; col++)
							{
							pixel = (row * Kinect.depth_width) + col;
							if (depthdata[pixel] > 0)
								{
								depth = Math.Abs(Kinect.CorrectedDistance(depthdata[pixel] * SharedData.MM_TO_IN));
								rax = DepthHorDegrees(col - (Kinect.depth_width / 2));
								x = depth * Math.Tan(rax * SharedData.DEG_TO_RAD);
								if (Math.Abs(x) <= width_limit)
									{
									pdist = depth / Math.Cos(ray * SharedData.DEG_TO_RAD);
									fdist = pdist * Math.Sin((90 - (tilt_angle + ray)) * SharedData.DEG_TO_RAD);
									if (row == Kinect.depth_height - 1)
										frow_fdist = fdist;
									if (acd <= acd_dist_limit)
										{
										if (pdist < acd)
											{
											no_obstacle_hits += 1;
											no_acd_hits += 1;
											if (acd - pdist > max_dif)
												max_dif = acd - pdist;
											if (fdist < min_obs_dist)
												min_obs_dist = fdist;
											if (bm != null)
												bm.SetPixel(col,row,Color.HotPink);
											}
										}
									else if (fdist < dist_limit)
										{
										no_obstacle_hits += 1;
										no_fdist_hits += 1;
										if (dist_limit - fdist > max_fdist_dif)
											max_fdist_dif = dist_limit - fdist;
										if (fdist < min_obs_dist)
											min_obs_dist = fdist;
										if (bm != null)
											bm.SetPixel(col, row, Color.DarkOrange);
										}
									}
								}
							}
						}
					if (bm != null)
						{
						fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Depth " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
						bm.RotateFlip(RotateFlipType.Rotate180FlipY);
						bm.Save(fname, ImageFormat.Jpeg);
						Log.LogEntry("Saved: " + fname);
						}
					}
				else
					Log.LogEntry("Could not get Kinect depth frame");
				AutoRobotControl.HeadAssembly.Tilt(0, true);
				}
			else
				Log.LogEntry("Could not tilt Kinect.");
			min_floor_detect_dist = frow_fdist;
			Log.LogEntry("Obstacles detected: " + no_obstacle_hits + " (ACD detects - " + no_acd_hits + "  dist limit detects - " + no_fdist_hits + ")");
			return (min_obs_dist);
		}



		static public bool MapDepthCore(ref byte[,] map,ref int min_floor_detect_dist)

		{
			bool rtn = false;
			int row = 0, col = 0,pixel = 0;
			double pdist, ray = 0, rax = 0, acd = 0, x, depth, fdist, frow_fdist = 0;
			double tilt_angle, tilt_err, kinect_height, acd_dist_limit;
			int xr = 0, dr = 0;
			System.Drawing.Bitmap bm = null;
			Graphics g;
			DateTime now = DateTime.Now;
			string fname = "";
			TextWriter tw = null;

			if (SharedData.log_operations)
				{
				bm = depthdata.ToBitmap(Kinect.nui.DepthStream.FrameWidth, Kinect.nui.DepthStream.FrameHeight, 0, Color.White);
				g = System.Drawing.Graphics.FromImage(bm);
				g.DrawLine(Pens.Red, 0, Kinect.nui.DepthStream.FrameHeight / 2, bm.Width, Kinect.nui.DepthStream.FrameHeight / 2);
				g.DrawLine(Pens.Red, Kinect.nui.DepthStream.FrameWidth / 2, 0, Kinect.nui.DepthStream.FrameWidth / 2, bm.Height);
				fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "MapDepth depth picture " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				bm.RotateFlip(RotateFlipType.Rotate180FlipY);
				bm.Save(fname, ImageFormat.Jpeg);
				Log.LogEntry("Saved: " + fname);
				fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "MapDepth depth data set " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".csv";
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
			kinect_height = KINECT_HEIGHT_0 - ((KINECT_HEIGHT_0 - KINECT_HEIGHT_D3O) * (tilt_angle / 30));

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
					if ((depthdata[pixel] > 0) && (depthdata[pixel] != Kinect.nui.DepthStream.TooFarDepth)
						&& (depthdata[pixel] != Kinect.nui.DepthStream.TooNearDepth) && (depthdata[pixel] != Kinect.nui.DepthStream.UnknownDepth))
						{
						depth = Math.Abs(Kinect.CorrectedDistance(depthdata[pixel] * SharedData.MM_TO_IN));
						rax = -Kinect.DepthHorDegrees(col - (Kinect.depth_width / 2));
						x = depth * Math.Tan(rax * SharedData.DEG_TO_RAD);
						pdist = depth / Math.Cos(ray * SharedData.DEG_TO_RAD);
						fdist = pdist * Math.Sin((90 - (tilt_angle + ray)) * SharedData.DEG_TO_RAD);
						if ((row == Kinect.depth_height - 1) && (col == Kinect.depth_width / 2))
							frow_fdist = fdist;
						if (acd < acd_dist_limit)
							{
							if (pdist < acd)
								{
								xr = (int)Math.Round(x);
								dr = (int)Math.Round(fdist);
								if (map[Kinect.depth_width / 2 + xr,map.GetLength(1) - dr] == (byte)AutoRobotControl.Room.MapCode.CLEAR)
									{
									map[Kinect.depth_width / 2 + xr, map.GetLength(1) - dr] = (byte)AutoRobotControl.Room.MapCode.BLOCKED;
									if (tw != null)
										tw.WriteLine(xr + "," + dr);
									}
								}
							}
						else
							{
							xr = (int)Math.Round(x);
							dr = (int)Math.Round(fdist);
							if (map[Kinect.depth_width / 2 + xr, map.GetLength(1) - dr] == (byte)AutoRobotControl.Room.MapCode.CLEAR)
								{
								map[Kinect.depth_width / 2 + xr, map.GetLength(1) - dr] = (byte)AutoRobotControl.Room.MapCode.BLOCKED;
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
				Log.LogEntry("Saved: " + fname);
				tw.Close();
				}
			if (bm != null)
				{
				bm = SkillShared.MapToBitmap(map);
				fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "MapDepth depth map " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".bmp";
				bm.Save(fname,ImageFormat.Bmp);
				Log.LogEntry("Saved: " + fname);
				}
			Log.LogEntry("MapDepthCore " + rtn);
			return (rtn);
		}



		static public bool MapDepth(ref byte[,] map,ref int min_floor_detect_dist)

		{
			bool rtn = false;
			int i,j;

			if (AutoRobotControl.HeadAssembly.Tilt(TILT_ANGLE, true))
				{
				if (Kinect.GetDepthFrame(ref depthdata, 40))
					{
					for (i = 0; i < Kinect.nui.DepthStream.FramePixelDataLength; i++)
						{
						depthdata[i] = (short)(depthdata[i] >> 3);
						}
					for (i = 0;i < map.GetUpperBound(0);i++)
						for (j = 0;j < map.GetUpperBound(1);j++)
							map[i,j] = (byte)AutoRobotControl.Room.MapCode.CLEAR;
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
						if (map[fpt.X, fpt.Y] == (byte)AutoRobotControl.Room.MapCode.BLOCKED)
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
		}

		}
	}
