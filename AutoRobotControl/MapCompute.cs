using System;
using System.Collections;
using System.Drawing;

namespace AutoRobotControl
	{
	public static class MapCompute
		{

		public struct obs_correct
		{
			public bool correctable;
			public int direct;
			public int dist;
		};


		public static int FindMapObstacle(byte[,] map,Point spt,int direct,int dist,int front_clear,int side_clear)

		{
			int mdist = -1,i,j,hwdist,pdirect;
			bool obs_found = false;
			Point npt,fpt;

			Log.LogEntry("FindMapObstacle: (" + spt.X + " " + spt.Y + ") " + direct + "  " + dist + "  " + front_clear + "  " + side_clear);
			hwdist = (int) (Math.Ceiling((double)SharedData.ROBOT_WIDTH / 2) + side_clear);
			pdirect = (direct + 90) %360;
			for (i = 0;;i++)
				{
				npt = new Point((int) Math.Round(spt.X + (i * Math.Sin(direct * SharedData.DEG_TO_RAD))),(int) Math.Round(spt.Y - (i * Math.Cos(direct * SharedData.DEG_TO_RAD))));
				for (j = -hwdist; j < hwdist;j++ )
					{
					fpt = new Point((int) Math.Round(npt.X + (j * Math.Sin(pdirect * SharedData.DEG_TO_RAD))), (int)Math.Round(npt.Y - (j * Math.Cos(pdirect * SharedData.DEG_TO_RAD))));

					try
					{
					if (map[fpt.X, fpt.Y] != (byte) Room.MapCode.CLEAR)
						{
						obs_found = true;
						Log.LogEntry("Obstacle @ " + fpt + " , forward distance " + i);
						mdist = i;
						break;
						}
					}

					catch (Exception)
					{
					obs_found = true;
					Log.LogEntry("Edge @ " + fpt);
					mdist = i;
					break;
					}

					}
				if (obs_found)
					break;
				if ((dist != -1) && (i >= dist + front_clear))
					break;
				}
			return (mdist);
		}



		public static int MapMapObstacles(byte[,] map,Point spt,int direct,int dist,int side_clear,ref ArrayList al)

		{
			int mdist = -1,i,j,hwdist,pdirect;
			Point npt,fpt;

			hwdist = (int) (Math.Ceiling((double)SharedData.ROBOT_WIDTH / 2) + side_clear);
			pdirect = (direct + 90) %360;
			for (i = 0;;i++)
				{
				npt = new Point((int) Math.Round(spt.X + (i * Math.Sin(direct * SharedData.DEG_TO_RAD))),(int) Math.Round(spt.Y - (i * Math.Cos(direct * SharedData.DEG_TO_RAD))));
				for (j = -hwdist; j < hwdist;j++ )
					{
					fpt = new Point((int) Math.Round(npt.X + (j * Math.Sin(pdirect * SharedData.DEG_TO_RAD))), (int)Math.Round(npt.Y - (j * Math.Cos(pdirect * SharedData.DEG_TO_RAD))));

					try
					{
					if (map[fpt.X, fpt.Y] != (byte) Room.MapCode.CLEAR)
						{
						if (mdist == -1)
							mdist = i;
						else if (i < mdist)
							mdist = i;
						al.Add(new Point (fpt.X, fpt.Y));
						break;
						}
					}

					catch (Exception)
					{
					if (mdist == -1)
						mdist = i;
					else if (i < mdist)
						mdist = i;
					al.Add(new Point (fpt.X, fpt.Y));
					break;
					}

					}
				if ((dist != -1) && (i >= dist))
					break;
				}
			return (mdist);
		}



		public static obs_correct AnalyizeMapObstacles(Point loc,int direct,ArrayList obs,int side_clear)

		{
			obs_correct obc = new obs_correct();
			bool center = false,right = false,left = false;
			int i;
			Point pt;
			NavCompute.pt_to_pt_data ppd;
			int max_dist = 0,dist,angle,side_dist;

			side_dist = (int) Math.Ceiling(((double ) SharedData.ROBOT_WIDTH/2) + side_clear);
			for (i = 0;i < obs.Count;i++)
				{
				pt = (Point) obs[i];
				ppd = NavCompute.DetermineRaDirectDistPtToPt(pt,loc);
				if (direct == ppd.direc)
					{
					center = true;
					break;
					}
				else if (NavCompute.ToRightDirect(direct,ppd.direc))
					{
					right = true;
					if (left == true)
						break;
					else
						{
						angle = NavCompute.AngularDistance(direct,ppd.direc);
						dist = (int) Math.Floor(NavCompute.DistancePtToPt(loc, pt) * Math.Sin(angle * SharedData.DEG_TO_RAD));
						dist = side_dist - dist;
						if (dist > max_dist)
							max_dist = dist;
						}
					}
				else
					{
					left = true;
					if (right == true)
						break;
					else
						{
						angle = NavCompute.AngularDistance(direct, ppd.direc);
						dist = (int)Math.Ceiling(NavCompute.DistancePtToPt(loc, pt) * Math.Sin(angle * SharedData.DEG_TO_RAD));
						dist = side_dist - dist;
						if (dist > max_dist)
							max_dist = dist;
						}
					}
				}
			if (center || (right && left))
				{
				obc.correctable = false;
				obc.direct = -1;
				obc.dist = -1;
				}
			else
				{
				obc.correctable = true;
				obc.dist = max_dist;
				if (obc.dist == 0)
					obc.dist = 1;
				obc.dist += 1;
				if (left)
					obc.direct = (direct + 90) % 360;
				else
					{
					obc.direct = direct - 90;
					if (obc.direct < 0)
						obc.direct += 360;
					}
				}
			Log.LogEntry("Map obstacle correction : " + obc.correctable + "  " + obc.direct + "  " + obc.dist);
			return(obc);
		}



		// based on current location @cpt and unclear destination @ mpt, determine nearest clear destination point
		public static bool FindNearestClearArea(byte[,] map,Point cpt,Point mpt,ref Point cp)

		{
			bool rtn = false;
			double dx, dy, dist, x, y,x2,y2;
			int i;

			if (map != null)
				{
				dist = Math.Sqrt(Math.Pow(cpt.X - mpt.X, 2) + Math.Pow(cpt.Y - mpt.Y, 2));
				dx = (cpt.X - mpt.X) / dist;
				dy = (cpt.Y - mpt.Y) / dist;
				x = mpt.X;
				y = mpt.Y;
				try
					{
					for (i = 0; i < dist; i++)
						{
						x += dx;
						y += dy;
						if (map[(int)Math.Round(x), (int)Math.Round(y)] == (byte)Room.MapCode.CLEAR)
							{
							Log.LogEntry("Clear point: (" + x + "," + y + ")");
							if (Math.Abs(dx) > Math.Abs(dy))
								{
								x2 = x + SharedData.ROBOT_WIDTH;
								y2 = y;
								}
							else
								{
								y2 = y + SharedData.ROBOT_WIDTH;
								x2 = x;
								}
							if (map[(int)Math.Round(x2), (int)Math.Round(y2)] == (byte)Room.MapCode.CLEAR)
								{
								cp.X = (int)Math.Round((x + x2)/2);
								cp.Y = (int)Math.Round((y + y2)/2);
								rtn = true;
								}
							break;
							}
						}
					}

				catch (IndexOutOfRangeException)

					{
					}

				}
			else
				Log.LogEntry("No map available.");
			if (rtn)
				Log.LogEntry("FindNearestClearArea for " + cpt + "," + mpt + " : " + cp);
			else
				Log.LogEntry("Could not determine nearest clear area.");
			return (rtn);
		}



		public static bool PtInClear(Point cp,int clr_dist)

		{
			bool rtn = true;
			int x,y;

			for (x = cp.X - clr_dist;x <= cp.X + clr_dist;x++)
				{
				for (y = cp.Y - clr_dist;y < cp.Y + clr_dist;y++)
					{
					if ((y < 0) || (y >= NavData.rd.rect.Height))
						{
						rtn = false;
						break;
						}
					if ((x < 0) || (x >= NavData.rd.rect.Width))
						{
						rtn = false;
						break;
						}
					if (NavData.detail_map[x,y] != (byte)Room.MapCode.CLEAR)
						{
						rtn = false;
						break;
						}
					}
				if (!rtn)
					break;
				}
			return(rtn);
		}

		}
	}
