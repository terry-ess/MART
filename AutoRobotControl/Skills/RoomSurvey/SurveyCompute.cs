using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using AutoRobotControl;

namespace Room_Survey
	{
	static class SurveyCompute
		{

		const int STHRESHOLD = 10;
		const int DTHRESHOLD = 12;
		const int FTHRESHOLD = 2;

		public enum adjustment {NONE,ANGLE,MOVE};


		public struct pt_to_pt_data
		{
			public int direc;
			public int dist;
		};
		


		public static int FindObstacles(int shift_angle,int dist,ArrayList sdata,double side_clear,ref ArrayList obs)

		{
			int i,angle,min_dist = (int) Math.Round(6000 * SharedData.MM_TO_IN);
			double x,y,dx,dy;
			Rplidar.scan_data sd;
			double width_band;

			Log.LogEntry("FindObstacles: " + shift_angle + "  " + dist + "  " + side_clear);
			width_band = ((double) SharedData.ROBOT_WIDTH/2) + side_clear;
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
						obs.Add(sd);
						if (y < min_dist)
							min_dist = (int) Math.Ceiling(y);
						}
					}
				else
					{
					if ((y > 0) && (y <= dist) && (Math.Abs(x) <= width_band))
						{
						obs.Add(sd);
						if (y < min_dist)
							min_dist = (int) Math.Ceiling(y);
						}
					}
				}
			Log.LogEntry("Min obstacle distance: " + min_dist);
			Rplidar.SaveLidarScan(ref sdata, "FindObstacles: " + shift_angle + "  " + dist + "\r\nObstacles: " + obs.Count + "\r\n" + SkillShared.ArrayListToString(obs));
			return (min_dist);
		}



		public static int FindObstacles(int shift_angle,int dist,ArrayList sdata,ref ArrayList obs)

		{
			return(FindObstacles(shift_angle,dist,sdata,SkillShared.STD_SIDE_CLEAR,ref obs));
		}




		public static bool ObstacleAdjustAngle(ArrayList sdata,ArrayList obs,int odist,int shift_angle,double side_clear,ref int angle)

		{
			bool rtn = false,right_obs = false,left_obs = false,center_obs = false;
			int i,sangle, tangle, maxtangle = 0;
			Rplidar.scan_data sd;
			double x,y,mtax = 0,dx,dy;

			dx = (SharedData.FRONT_PIVOT_PT_OFFSET + SharedData.FLIDAR_OFFSET) * Math.Sin(shift_angle * SharedData.DEG_TO_RAD);
			dy = ((SharedData.FRONT_PIVOT_PT_OFFSET + SharedData.FLIDAR_OFFSET) * Math.Cos(shift_angle * SharedData.DEG_TO_RAD)) - SharedData.FRONT_PIVOT_PT_OFFSET;
			for (i = 0;i < obs.Count;i++)
				{
				sd = (Rplidar.scan_data) obs[i];
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
					tangle = (int) Math.Abs(Math.Ceiling(Math.Atan((((double)SharedData.ROBOT_WIDTH / 2) + side_clear - Math.Abs(x)) / y) * SharedData.RAD_TO_DEG));
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
				if (FindObstacles(shift_angle, odist, sdata, side_clear, ref obs) == 0)
					rtn = true;
				else
					rtn = false;
				}
			else
				{
				if (FindObstacles(shift_angle, odist, sdata, side_clear, ref obs) == 0)
					{
					angle = maxtangle;
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
			Log.LogEntry("Obstacle adjust angle: " + rtn + "  " + angle);
			return (rtn);
		}



		public static bool ObstacleAdjustAngle(ArrayList sdata,ArrayList obs, int odist, int shift_angle, ref int angle)

		{
			return(ObstacleAdjustAngle(sdata,obs,odist,shift_angle,SkillShared.STD_SIDE_CLEAR,ref angle));
		}



		public static SharedData.RobotLocation ObstacleSide(ArrayList obs,int odist,int shift_angle,double side_clear,ref int max_y_dist)

		{
			bool right_obs = false,left_obs = false,center_obs = false;
			int sangle,i;
			Rplidar.scan_data sd;
			double x,dx,y,dy,max_y = 0;
			SharedData.RobotLocation side = SharedData.RobotLocation.REAR;

			dx = (SharedData.FRONT_PIVOT_PT_OFFSET + SharedData.FLIDAR_OFFSET) * Math.Sin(shift_angle * SharedData.DEG_TO_RAD);
			dy = ((SharedData.FRONT_PIVOT_PT_OFFSET + SharedData.FLIDAR_OFFSET) * Math.Cos(shift_angle * SharedData.DEG_TO_RAD)) - SharedData.FRONT_PIVOT_PT_OFFSET;
			for (i = 0;i < obs.Count;i++)
				{
				sd = (Rplidar.scan_data) obs[i];
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
					if (y > max_y)
						max_y = y;
					}
				}
			if (left_obs && !right_obs && !center_obs)
				{
				side = SharedData.RobotLocation.LEFT;
				max_y_dist = (int) Math.Round(max_y);
				}
			else if (!left_obs && right_obs && !center_obs)
				{
				side = SharedData.RobotLocation.RIGHT;
				max_y_dist = (int)Math.Round(max_y);
				}
			Log.LogEntry("Obstacle side: " + side);
			return (side);
		}



		private static void SetPoint(ref Bitmap bmp,int x, int y,Brush br)

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



		public static int FindMapObstacle(ref byte[,] map,Point spt,int direct,ref Bitmap bm)

		{
			int mdist = -1,i,j,hwdist,pdirect;
			bool obs_found = false;
			Point npt,fpt;

			Log.LogEntry("FindMapObstacle: (" + spt.X + " " + spt.Y + ") " + direct);
			SetPoint(ref bm, spt.X, spt.Y, Brushes.Blue);
			hwdist = (int) Math.Ceiling((double)SharedData.ROBOT_WIDTH / 2);
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
						SetPoint(ref bm,fpt.X,fpt.Y,Brushes.Red);
						break;
						}
					}

					catch (Exception)
					{
					obs_found = true;
					mdist = i;
					Log.LogEntry("Edge @ (" + fpt.X + " " + fpt.Y + ")  " + mdist);
					SetPoint(ref bm,fpt.X,fpt.Y,Brushes.Red);
					break;
					}

					}
				if (obs_found)
					break;
				}
			return (mdist);
		}



		public static bool TurnAreaClear(Point pt,int dist, ref ArrayList sdata)

		{
			bool rtn = true;
			int i;
			double angle_band;
			double x, y;
			Rplidar.scan_data sd;
			Point cpt;

			Log.LogEntry("TurnAreaClear: " + pt + "  " + dist);
			angle_band = Math.Atan(SharedData.REAR_TURN_RADIUS/dist) * SharedData.RAD_TO_DEG;
			for (i = 0;i < sdata.Count;i++)
				{
				sd = (Rplidar.scan_data)sdata[i];
				if ((sd.angle >= -angle_band) && (sd.angle <= angle_band))
					{
					y = sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD);
					x = sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD);
					cpt = new Point((int)Math.Round(x), (int)Math.Round(y));
					if (NavCompute.DistancePtToPt(pt,cpt) <= SharedData.REAR_TURN_RADIUS)
						{
						rtn = false;
						Log.LogEntry("obstacle @ " + cpt);
						break;
						}
					}
				}
			return (rtn);
		}



		public static pt_to_pt_data DetermineDirectDistPtToPt(Point to_pt,Point from_pt)

		{
			int dy,dx,ra;
			pt_to_pt_data rtn;

			dy = to_pt.Y - from_pt.Y;
			dx = to_pt.X - from_pt.X;
			if (dy == 0)
				if (dx > 0)
					rtn.direc = 90;
				else
					rtn .direc = 270;
			else
				{
				ra = (int) Math.Round(Math.Atan((double) dx/dy) * SharedData.RAD_TO_DEG);
				if (dy > 0)
					rtn.direc = (360 + ra) % 360;
				else
					rtn.direc = (180 + ra) % 360;
				}
			rtn.dist = (int) Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));
			Log.LogEntry("Determine direct and dist: " + from_pt + " to " + to_pt + " = " + rtn.direc + ", " + rtn.dist);
			return(rtn);
		}



		public static byte[,] ScanMap(ArrayList scan,MotionMeasureProb.Pose pose, ref Point map_shift)

		{
			int i,height, width,angle;
			Rplidar.scan_data sd;
			double x,y,min_x = 1000, max_x = 0, min_y = 1000, max_y = 0;
			byte[,] map;

			for (i = 0;i < scan.Count;i++)
				{
				sd = (Rplidar.scan_data) scan[i];
				angle = (sd.angle + pose.orient) % 360;
				y = sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD) + SharedData.FLIDAR_OFFSET + pose.coord.Y;
				x = sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD) + pose.coord.X;
				if (x > max_x)
					max_x = x;
				if (x < min_x)
					min_x = x;
				if (y > max_y)
					max_y = y;
				if (y < min_y)
					min_y = y;
				}
			height = (int) Math.Ceiling(max_y - min_y);
			width = (int) Math.Ceiling(max_x - min_x);
			map = new byte[width + 1,height + 1];
			map_shift.X = (int) -Math.Floor(min_x);
			map_shift.Y = (int) Math.Ceiling(max_y);
			for (i = 0; i < scan.Count; i++)
				{
				sd = (Rplidar.scan_data)scan[i];
				angle = (sd.angle + pose.orient) % 360;
				y = map_shift.Y - (sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD) + SharedData.FLIDAR_OFFSET + pose.coord.Y);
				x = sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD) + pose.coord.X + map_shift.X;

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




		public static void SurveySize(ArrayList scans, ref int height, ref int width, ref int emax_x)

		{
			MotionMeasureProb.Pose cpose;
			int i, j;
			ArrayList scan;
			RoomSurvey.ExtScanData esd;
			double min_x = 1000, max_x = 0, max_y = 0;

			for (i = 0; i < scans.Count; i++)
				{
				scan = (ArrayList) scans[i];
				cpose = (MotionMeasureProb.Pose) scan[0];
				for (j = 1; j < scan.Count; j++)
					{
					esd = (RoomSurvey.ExtScanData)scan[j];
					if (esd.coord.X + cpose.coord.X > max_x)
						max_x = esd.coord.X + cpose.coord.X;
					else if (esd.coord.X + cpose.coord.X < min_x)
						min_x = esd.coord.X + cpose.coord.X;
					if (esd.coord.Y + cpose.coord.Y > max_y)
						max_y = esd.coord.Y + cpose.coord.Y;
					}
				}
			width = (int) Math.Ceiling(max_x - min_x);
			height = (int) Math.Ceiling(max_y);
			emax_x = (int) Math.Ceiling(max_x);
		}



		public static Point MapToCc(Point mpt,Point map_shift)

		{
			Point ccpt = new Point();

			ccpt.X = mpt.X - map_shift.X;
			ccpt.Y = map_shift.Y - mpt.Y;
			return(ccpt);
		}

		
		public static Point CcToMap(Point ccpt,Point map_shift)

		{
			Point mpt = new Point();

			mpt.X = ccpt.X + map_shift.X;
			mpt.Y = map_shift.Y - ccpt.Y;
			return (mpt);
		}



		public static Point CcToMap(Point ccpt)

		{
			return(CcToMap(ccpt,SkillShared.map_shift));
		}



		private static bool FindOpening(ArrayList sdata,int zindx,int open_dist,ref int oindx)

		{
			bool right,opening_found = false;
			int i,j,indx;
			double y,x;
			Rplidar.scan_data sd = new Rplidar.scan_data();
			const int DIST_LIMIT = 18;

			for (i = 0;i < 2;i++)
				{
				if (i == 0)
					right = true;
				else
					right = false;
				for (j = 1;j < sdata.Count;j++)
					{
					if (right)
						indx = (zindx + j) % sdata.Count;
					else
						{
						indx = zindx - j;
						if (indx < 0)
							indx += sdata.Count;
						}
					sd = (Rplidar.scan_data) sdata[indx];
					y = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD));
					if (y > open_dist)
						{
						opening_found = true;
						oindx = indx;
						break;
						}
					x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
					if (Math.Abs(x) > DIST_LIMIT)
						break;
					}
				if (opening_found)
					break;
				}
			return(opening_found);
		}




		public static bool ExitPosition(NavData.connection connect,int epdist,int eorient,ref int turn_angle,ref int adist)

		{
			bool rtn = false,opening_found = false;
			bool edge_right = false;
			ArrayList sdata = new ArrayList();
			Rplidar.scan_data sd = new Rplidar.scan_data();
			int search_angle,i,indx = -1,ad,min_ad = 180,start_angle,cangle,top,bottom,shift,oindx = -1;
			double dx,angle,min_dist = epdist * 2,dy,last_distance = -1,theta,td,y,sx,sy;
			string lines = "";
			const int DIST_MARGIN = 15;

			Log.LogEntry("ExitPosition: " + connect.name + ", " + epdist + " in, " + eorient + "°");
			if ((connect.lc_edge.ef.type != NavData.FeatureType.NONE) && !connect.lc_edge.door_side)
				edge_right = true;
			shift = eorient - connect.direction;
			if (shift > 180)
				shift -= 360;
			else if (shift < -180)
				shift += 360;
			if (Rplidar.CaptureScan(ref sdata,true))
				{
				lines += "ExitPosition: " + connect.name + "  " + epdist + " in  " + eorient + "°\r\n";
				if (edge_right)
					lines += "Edge to right\r\n";
				else
					lines += "Edge to left\r\n";
				for (i = 0;i < sdata.Count;i++)
					{
					sd = (Rplidar.scan_data) sdata[i];
					if (sd.angle == 0)
						{
						indx = i;
						break;
						}
					else
						{
						if ((ad = NavCompute.AngularDistance(0,sd.angle)) < min_ad)
							{
							min_ad = ad;
							indx = i;
							}
						}
					}
				lines += "Seach start at index " + indx + ".\r\n";
				top = epdist + DIST_MARGIN;
				sd = (Rplidar.scan_data)sdata[indx];
				cangle = (sd.angle + shift) % 360;
				if (cangle < 0)
					cangle += 360;
				y = (sd.dist * Math.Cos(cangle * SharedData.DEG_TO_RAD));
				if (y > top)
					{
					opening_found = true;
					lines += "Opening found at start index.";
					}
				else
					{
					opening_found = FindOpening(sdata,indx,top,ref oindx);
					if (opening_found)
						{
						indx = oindx;
						lines += "Start index changed to opening index " + indx + "\r\n";
						}
					else
						lines += "No opening found.\r\n";
					}
				if (opening_found)
					{
					bottom = epdist - DIST_MARGIN;
					start_angle = ((Rplidar.scan_data)sdata[indx]).angle;
					search_angle = (int) Math.Ceiling(Math.Atan(((double)connect.exit_width) / bottom) * SharedData.RAD_TO_DEG);
					lines += "Search angle " + search_angle + "°\r\n";
					i = indx;
					indx = -1;
					do
						{
						sd = (Rplidar.scan_data) sdata[i];
						cangle = (sd.angle + shift) % 360;
						if (cangle < 0)
							cangle += 360;
						y = sd.dist * Math.Cos(cangle * SharedData.DEG_TO_RAD);
						if (y < top)
							{
							if ((last_distance - sd.dist) > 24)
								{
								lines += "Sudden distance change detected at index " + i + ".\r\n";
								indx = i;
								break;
								}
							else if (sd.dist < min_dist)
								{
								min_dist = sd.dist;
								indx = i;
								}
							else if ((indx != -1) && (sd.dist > min_dist))
								{
								lines += "Minimum distance detected at index " + indx + ".\r\n";						
								break;
								}
							}
						last_distance = sd.dist;
						if (edge_right)
							i = (i + 1) % sdata.Count;
						else
							{
							i -= 1;
							if (i < 0)
								i = sdata.Count - 1;
							}
						}
					while(NavCompute.AngularDistance(start_angle,sd.angle) < search_angle );
					if (indx != -1)
						{
						sd = (Rplidar.scan_data) sdata[indx];
						sx = (SharedData.FRONT_PIVOT_PT_OFFSET + SharedData.FLIDAR_OFFSET) * Math.Sin(-shift * SharedData.DEG_TO_RAD);
						sy = ((SharedData.FRONT_PIVOT_PT_OFFSET + SharedData.FLIDAR_OFFSET) * Math.Cos(-shift * SharedData.DEG_TO_RAD));
						angle = (sd.angle + shift) % 360;
						if (angle < 0)
							angle += 360;
						dx = sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD) - sx;
						dy = (sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD)) + sy;
						if (edge_right )
							dx = (dx - ((double) connect.exit_width / 2));
						else
							dx = (dx + ((double)connect.exit_width / 2));
						theta = Math.Atan(dx/dy) * SharedData.RAD_TO_DEG;
						dx += SharedData.FRONT_PIVOT_PT_OFFSET * Math.Sin(theta * SharedData.DEG_TO_RAD);
						theta = Math.Atan(dx / dy) * SharedData.RAD_TO_DEG;
						td = (connect.direction + theta) % 360;
						turn_angle = NavCompute.AngularDistance(eorient,(int) Math.Round(td));
						if (NavCompute.ToRightDirect(eorient, (int)Math.Round(td)))
							turn_angle *= -1;
						dy -= SharedData.FRONT_PIVOT_PT_OFFSET;
						adist = (int) Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));
						lines += "Edge data: shifted angle " + angle.ToString("F1") + "°  dx " + dx.ToString("F2") + "  dy " + dy.ToString("F2") + "  theta " + theta.ToString("F1") + "°  turn direction " + td.ToString("F1") + "°  turn angle " + turn_angle + "°   distance" + adist;
						rtn = true;
						}
					else
						lines += "No edge found.";
					Rplidar.SaveLidarScan(ref sdata,lines);
					}
				else
					{
					Log.LogEntry("Opening not found.");
					lines += "Opening not found.";
					Rplidar.SaveLidarScan(ref sdata, lines);
					}
				}
			else
				Log.LogEntry("ExitPosition: could not capture LIDAR scan.");
			return(rtn);
		}


		}
	}
