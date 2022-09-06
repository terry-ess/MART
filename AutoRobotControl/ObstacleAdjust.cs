using System;
using System.Collections;
using System.Threading;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;


namespace AutoRobotControl
	{
	public class ObstacleAdjust
		{

		public enum obstacle_action {NO_OBSTACLE,NONE,DISTANCE,ADJUST,REMAPPED};

		public const int STD_SIDE_CLEARANCE = 2;

		private const int MAX_ADJUST_ANGLE = 6;
		private const int ADJUST_CLEARENCE = 2;
		private const int MIN_ADJUST_CLEARENCE = 1;
		private const int CLEAR_SCAN_ANGLE_LINIT = 60;
		private const int MAX_SEARCH_DIST = 36;
		
		private bool pos_obstacle, neg_obstacle;
		private int aa;
		Rplidar.scan_data min_x_sd = new Rplidar.scan_data();
		private ArrayList lscan = new ArrayList();
		private ArrayList obstacles = new ArrayList();
		private ArrayList mptal = new ArrayList();
		private Point ep,mp;
		private int obs_center_angle;
		private int obs_dist;
		private int prior_adjust;
		private int initial_orientation;


		private obstacle_action ObstacleFound(ArrayList lscan,int fl,int nc_dist,ref int min_obs_dist)

		{
			obstacle_action rtn = obstacle_action.NO_OBSTACLE;
			int i,tangle,maxtangle = 0;
			Rplidar.scan_data rdata;
			int min_dist = fl;
			double width_band,dtan;
			double x, y,min_x = fl;
			string lines;

			dtan = Math.Tan(MotionMeasureProb.MoveDriftLimit() * SharedData.DEG_TO_RAD);
			lines = "Forward obstacle check: " + fl + "  " + nc_dist + "   " + dtan + "\r\n";
			pos_obstacle = false;
			neg_obstacle = false;
			obstacles.Clear();
			for (i = 0; i < lscan.Count; i++)
				{
				rdata = (Rplidar.scan_data) lscan[i];
				y = rdata.dist * Math.Cos(rdata.angle * SharedData.DEG_TO_RAD) + SharedData.FLIDAR_OFFSET;
				if ((y > SharedData.FLIDAR_OFFSET) && (y <= fl))
					{
					x = rdata.dist * Math.Sin(rdata.angle * SharedData.DEG_TO_RAD);
					width_band = ((double)SharedData.ROBOT_WIDTH / 2) + STD_SIDE_CLEARANCE + (y * dtan);
					if (Math.Abs(x) <= width_band)
						{
						obstacles.Add(rdata);
						lines += "obstacle @ " + rdata.angle + " °   " + rdata.dist + " in. \r\n";
						if (x >= 0)
							neg_obstacle = true;
						else
							pos_obstacle = true;
						if (y < min_dist)
							min_dist = (int) Math.Ceiling(y);
						}
					}
				}
			if (pos_obstacle && neg_obstacle)
				{
				Log.LogEntry("Obstacles on both sides of path.");
				rtn = obstacle_action.ADJUST;
				}
			else if ((pos_obstacle || neg_obstacle) && (min_dist >= nc_dist) && (mp == ep))
				{
				rtn = obstacle_action.DISTANCE;
				min_obs_dist = min_dist;
				}
			else if (pos_obstacle || neg_obstacle)
				{
				for (i = 0;i < obstacles.Count;i++)			//THIS DOES NOT FACTOR IN IMPACT OF OFFSET FROM "FRONT" TO PIVIOT PT OF 2.5 IN
					{													//DOES FACTOR IN POSSIBLE DRIFT IMPACT AND OFFSET FROM LIDAR TO "FRONT"
					rdata = (Rplidar.scan_data) obstacles[i];
					x = rdata.dist * Math.Sin(rdata.angle * SharedData.DEG_TO_RAD);
					y = (Math.Cos(rdata.angle * SharedData.DEG_TO_RAD) * rdata.dist) + SharedData.FLIDAR_OFFSET;
					tangle = (int) Math.Abs(Math.Round(Math.Atan((((double)SharedData.ROBOT_WIDTH / 2) + (STD_SIDE_CLEARANCE + (y * dtan)) - Math.Abs(x)) / y) * SharedData.RAD_TO_DEG));
					if (tangle > maxtangle)
						maxtangle = tangle;
					}
				aa = maxtangle;
				if (aa < SharedData.MIN_TURN_ANGLE)	
					aa = SharedData.MIN_TURN_ANGLE;	
				if (pos_obstacle)
					{
					Log.LogEntry("Obstacle adjust right " + aa.ToString() + "°");
					lines += "Obstacle adjust right " + aa.ToString() + "°\r\n";
					}
				else
					{
					Log.LogEntry("Obstacle adjust left " + aa.ToString() + "°");
					lines += "Obstacle adjust left " + aa.ToString() + "°\r\n";
					}
				rtn = obstacle_action.ADJUST;
				}
			else
				{
				Log.LogEntry("No forward obstacle found");
				lines += "No forward obstacle found\r\n";
				rtn = obstacle_action.NO_OBSTACLE;
				}
			Rplidar.SaveLidarScan(ref lscan,lines);
			return(rtn);
		}



		private obstacle_action PersonInteraction(int movdist,int clear)

		{
			obstacle_action oa = obstacle_action.NONE;
			string reply;
			int tries = 0,mdist = 0,i;
			bool obstacle = true;

			do
				{
				reply = Speech.Conversation("Excuse me.  May I come through?","responseafirm",10000,true);
				tries += 1;
				if (reply.Length > 0)
					{
					Speech.SpeakAsync("Thank you.");
					for (i = 0; i < 4;i++)
						{
						Thread.Sleep(2000);
						lscan.Clear();
						if (Rplidar.CaptureScan(ref lscan, true) && ObstacleFound(lscan,movdist,movdist - clear, ref mdist) == obstacle_action.NO_OBSTACLE)
							{
							obstacle = false;
							break;
							}
						}
					}
				}
			while((reply.Length > 0) && (tries < 3) && obstacle);
			if (!obstacle)
				oa = obstacle_action.NO_OBSTACLE;
			return(oa);
		}



		private bool RightScan(int sangle,int pdist,ref Point pt1)

		{
			bool rtn = false,done = false;
			int i,indx = -1,pt1x = 0;
			Rplidar.scan_data sd = new Rplidar.scan_data();
			double y,x,max_search_angle,clr_angle,clr_x_dist,crn_angle,last_x;

			for (i = 0;i < lscan.Count;i++)
				{
				sd = (Rplidar.scan_data) lscan[i];
				if (sd.angle == sangle)
					{
					indx = i;
					break;
					}
				}
			if (indx > -1)
				{
				last_x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
				max_search_angle = Math.Atan(((double) MAX_SEARCH_DIST/obs_dist) * SharedData.DEG_TO_RAD);
				do
					{
					indx = (indx + 1) % lscan.Count;
					sd = (Rplidar.scan_data) lscan[indx];
					y = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD));
					if (y >= pdist + ((double) SharedData.ROBOT_WIDTH/2 + 1))
						{
						pt1x = (int) Math.Round(last_x);
						clr_x_dist = SharedData.ROBOT_WIDTH + ADJUST_CLEARENCE + (Math.Tan(sd.angle * SharedData.DEG_TO_RAD) * obs_dist);
						clr_angle = Math.Atan(clr_x_dist/obs_dist) * SharedData.RAD_TO_DEG;
						crn_angle = Math.Atan((pdist + (SharedData.ROBOT_WIDTH /2) + MIN_ADJUST_CLEARENCE)/clr_x_dist) * SharedData.RAD_TO_DEG;
						while (sd.angle < clr_angle)
							{
							indx = (indx + 1) % lscan.Count;
							sd = (Rplidar.scan_data)lscan[indx];
							y = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD));
							x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
							if (sd.angle <= crn_angle)
								{
								if (y < pdist + ((double) SharedData.ROBOT_WIDTH/2 + 1))
									{
									obstacles.Add(sd);
									last_x = x;
									break;
									}
								}
							else if (sd.angle <= clr_angle)
								{
								if (x < clr_x_dist)
									{
									obstacles.Add(sd);
									done = true;
									break;
									}
								}
							else
								rtn = true;
							}
						if (rtn)
							{
							while (sd.angle < 90)
								{
								indx = (indx + 1) % lscan.Count;
								sd = (Rplidar.scan_data)lscan[indx];
								x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
								if (x < clr_x_dist)
									{
									obstacles.Add(sd);
									done = true;
									rtn = false;
									break;
									}
								}
							}
						}
					else if (sd.angle > max_search_angle)
						done = true;
					else
						last_x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
					}
				while (!done);
				if (!rtn)
					Log.LogEntry("RightScan: could not find open space.");
				else
					{
					pt1.X = (int) Math.Ceiling(pt1x + ((double) SharedData.ROBOT_WIDTH/2 + ADJUST_CLEARENCE));
					pt1.Y = 0;
					Log.LogEntry("RightScan: walk around pt1 at LIDAR offset " + pt1.ToString());
					}
				}
			else
				Log.LogEntry("RightScan: could not find index for " + sangle);
			return(rtn);
		}



		private bool LeftScan(int sangle,int pdist,ref Point pt1)

		{
			bool rtn = false,done = false;
			int i,indx = -1,pt1x = 0;
			Rplidar.scan_data sd = new Rplidar.scan_data();
			double y,x,max_search_angle,clr_angle,clr_x_dist,crn_angle,last_x;

			if (sangle < 0)
				sangle += 360;
			for (i = 0;i < lscan.Count;i++)
				{
				sd = (Rplidar.scan_data) lscan[i];
				if (sd.angle == sangle)
					{
					indx = i;
					break;
					}
				}
			if (indx > -1)
				{
				max_search_angle = Math.Atan(((double) MAX_SEARCH_DIST/obs_dist) * SharedData.DEG_TO_RAD);
				last_x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
				do
					{
					indx -= 1;
					if (indx < 0)
						indx = lscan.Count - 1;
					sd = (Rplidar.scan_data) lscan[indx];
					y = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD));
					if (y >= pdist + ((double) SharedData.ROBOT_WIDTH/2 + 1))
						{
						pt1x = (int) Math.Round(last_x);
						clr_x_dist = SharedData.ROBOT_WIDTH + ADJUST_CLEARENCE - (Math.Tan(sd.angle * SharedData.DEG_TO_RAD) * obs_dist);
						clr_angle = 360 - Math.Atan(clr_x_dist/obs_dist) * SharedData.RAD_TO_DEG;
						crn_angle = 270 + Math.Atan((pdist + (SharedData.ROBOT_WIDTH /2) + MIN_ADJUST_CLEARENCE)/clr_x_dist) * SharedData.RAD_TO_DEG;
						while (sd.angle > clr_angle)
							{
							indx -= 1;
							if (indx > 0)
								indx = lscan.Count - 1;
							sd = (Rplidar.scan_data)lscan[indx];
							y = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD));
							x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
							if (sd.angle >= crn_angle)
								{
								if (y < pdist + ((double) SharedData.ROBOT_WIDTH/2 + 1))
									{
									obstacles.Add(sd);
									last_x = x;
									break;
									}
								}
							else if (sd.angle >= clr_angle)
								{
								if (Math.Abs(x) < clr_x_dist)
									{
									obstacles.Add(sd);
									done = true;
									break;
									}
								}
							else
								rtn = true;
							}
						if (rtn)
							{
							while (sd.angle > 270)
								{
								indx -= 1;
								if (indx < 0)
									indx = lscan.Count - 1;
								sd = (Rplidar.scan_data)lscan[indx];
								x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
								if (Math.Abs(x) < clr_x_dist)
									{
									obstacles.Add(sd);
									done = true;
									rtn = false;
									break;
									}
								}
							}
						}
					else if (sd.angle < (360 - max_search_angle))
						done = true;
					else
						last_x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
					}
				while (!done);
				if (!rtn)
					Log.LogEntry("LeftScan: could not find open space.");
				else
					{
					pt1.X = (int) Math.Floor(pt1x - ((double) SharedData.ROBOT_WIDTH/2 + ADJUST_CLEARENCE));
					pt1.Y = 0;
					Log.LogEntry("LeftScan: walk around pt1 at LIDAR offset " + pt1.ToString());
					}
				}
			else
				Log.LogEntry("LeftScan: could not find index for " + sangle);
			return(rtn);
		}



		private bool SqdWalkAround()

		{
			bool rtn = false;
			Point pt1 = new Point(), pt2 = new Point();
			NavData.location clocation;
			int i, mina = 0, maxa = 0,angle,tdist;
			Rplidar.scan_data sd;

			mptal.Clear();
			clocation = NavData.GetCurrentLocation();
			for (i = 0;i < obstacles.Count;i++)
				{
				sd = (Rplidar.scan_data) obstacles[i];
				angle = sd.angle;
				if (angle > 180)
					angle -= 360;
				if (angle < mina)
					mina = angle;
				else if (angle > maxa)
					maxa = angle;
				}
			tdist = NavCompute.DistancePtToPt(clocation.coord, mp);
			if (RightScan(maxa,tdist,ref pt1))
				{
				pt2.X = pt1.X;
				pt2.Y = tdist;
				pt1 = NavCompute.MapPoint(pt1,clocation.orientation,clocation.coord);
				pt2 = NavCompute.MapPoint(pt2,clocation.orientation,clocation.coord);
				if (Navigate.PathClear(clocation.coord, pt1) && (Navigate.PathClear(pt1, pt2)) && (Navigate.PathClear(pt2, mp)))
					{
					mptal.Add(pt1);
					mptal.Add(pt2);
					rtn = true;
					}
				else
					Log.LogEntry("SqdWalkAround: right path not clear.");
				}
			if (!rtn && LeftScan(mina,tdist,ref pt1))
				{
				pt2.X = pt1.X;
				pt2.Y = tdist;
				pt1 = NavCompute.MapPoint(pt1,clocation.orientation,clocation.coord);
				pt2 = NavCompute.MapPoint(pt2,clocation.orientation,clocation.coord);
				if (Navigate.PathClear(clocation.coord, pt1) && (Navigate.PathClear(pt1, pt2)) && (Navigate.PathClear(pt2, mp)))
					{
					mptal.Add(pt1);
					mptal.Add(pt2);
					rtn = true;
					}
				else
					Log.LogEntry("SqdWalkAround: left path not clear.");
				}
			return(rtn);
		}



		private bool AngledWalkAround()

		{
			bool rtn = false;
			Point pt1,pt2;
			NavData.location clocation;
			double x,y,xdist;
			int tdist;

			mptal.Clear();
			clocation = NavData.GetCurrentLocation();
			x = Math.Sin(min_x_sd.angle * SharedData.DEG_TO_RAD) * min_x_sd.dist;
			y = Math.Cos(min_x_sd.angle * SharedData.DEG_TO_RAD) * min_x_sd.dist;
			if (pos_obstacle)
				{
				xdist = (((double) SharedData.ROBOT_WIDTH/2) + MIN_ADJUST_CLEARENCE + x)/Math.Cos(aa * SharedData.DEG_TO_RAD);
				x += ((double)SharedData.ROBOT_WIDTH / 2) + ADJUST_CLEARENCE;
				x = Math.Max(x, xdist);
				pt1 = NavCompute.MapPoint(new Point((int) Math.Ceiling(x), (int) Math.Round(y)), clocation.orientation, clocation.coord);
				} 
			else
				{
				xdist = (((double)SharedData.ROBOT_WIDTH / 2) + MIN_ADJUST_CLEARENCE - x) / Math.Cos(aa * SharedData.DEG_TO_RAD);
				x -= ((double)SharedData.ROBOT_WIDTH / 2) + ADJUST_CLEARENCE;
				x = Math.Max(Math.Abs(x), xdist);
				pt1 = NavCompute.MapPoint(new Point((int)Math.Floor(-x), (int)Math.Round(y)), clocation.orientation, clocation.coord);
				}

			if ((Math.Abs(x) - SharedData.FRONT_PIVOT_PT_OFFSET) > SharedData.MAX_DIST_DIF/2)
				{
				tdist = NavCompute.DistancePtToPt(clocation.coord, mp);
				pt2 = NavCompute.MapPoint(new Point(0,(int) Math.Round(tdist - y)),initial_orientation,pt1);
				if (Navigate.PathClear(clocation.coord,pt1) && (Navigate.PathClear(pt1,pt2)) && (Navigate.PathClear(pt2,mp)))
					{
					mptal.Add(pt1);
					mptal.Add(pt2);
					rtn = true;
					}
				}
			else
				{
				if (Navigate.PathClear(clocation.coord, pt1) && (Navigate.PathClear(pt1, mp)))
					{
					mptal.Add(pt1);
					rtn = true;
					}
				}
			return(rtn);
		}



		private bool AnalyzeObstacle(int mdist)

		{
			bool rtn = false;
			int i,no = 0,ca = 0;
			Rplidar.scan_data sd;
			double y,sum = 0;

			for (i = 0;i < obstacles.Count;i++)
				{
				sd = (Rplidar.scan_data) obstacles[i];
				y = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD));
				no += 1;
				sum += y;
				if (sd.angle > 180)
					ca += sd.angle - 360;
				else
					ca += sd.angle;
				}
			if (no > 0)
				{
				rtn = true;
				obs_dist = (int) Math.Round(sum/no);
				ca /= no;
				obs_center_angle = ca;
				Log.LogEntry("AnalyzeObstacle: avg distance (in) - " + obs_dist + "   center angle () - " + obs_center_angle);
				}
			else
				Log.LogEntry("AnalyzeObstacle: no obstacle found.");
			return(rtn);
		}



		private bool CheckLocalization(int emdist,int clear,ref bool orient_changed)

		{
			bool rtn = false;
			NavData.location iloc,eloc = new NavData.location();
			Location loc = new Location();
			int angle,modist = 0;

			iloc = NavData.GetCurrentLocation();
			eloc = NavData.GetCurrentLocation();
			if (loc.DetermineDRLocation(ref eloc, false,new Point(0,0), true))
				{
				NavData.SetCurrentLocation(eloc);
				if (eloc.orientation != iloc.orientation)
					{
					angle = NavCompute.AngularDistance(eloc.orientation,iloc.orientation);
					if (NavCompute.ToRightDirect(eloc.orientation,iloc.orientation))
						angle *= -1;
					if (Turn.TurnAngle(angle))
						{
						eloc.orientation = iloc.orientation;
						eloc.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(angle),eloc.orientation, eloc.coord);
						eloc.ls = NavData.LocationStatus.DR;
						NavData.SetCurrentLocation(eloc);
						orient_changed = true;
						pos_obstacle = false;
						neg_obstacle = false;
						lscan.Clear();
						Rplidar.CaptureScan(ref lscan, true);
						if (ObstacleFound(lscan, emdist + clear, emdist,ref modist) == obstacle_action.NO_OBSTACLE)
							rtn = true;
						else
							Log.LogEntry("CheckLocalization: obstacle found after making localization adjustment turn.");
						}
					}
				}
			return (rtn);
		}



		private bool Adjust(int emdist,int clear, int max_adjust_angle,ref bool orient_changed,ref bool walk_around_possible)

		{
			bool turned,rtn = false;
			NavData.location loc;
			int modist = 0,mdist = -1;
			int direc;
			Point mpt;
			byte[,] map = new byte[Kinect.depth_width, (int)Math.Ceiling(Kinect.nui.DepthStream.TooFarDepth * SharedData.MM_TO_IN)];

			walk_around_possible = false;
			loc = NavData.GetCurrentLocation();
			if ((aa > 0) && (Math.Abs(aa + prior_adjust) <= max_adjust_angle))
				{
				if (pos_obstacle)
					direc = (loc.orientation + aa) % 360;
				else
					{
					direc = loc.orientation - aa;
					 if (direc < 0)
						direc += 360;
					}
				mpt = NavCompute.MapPoint(new Point(0, emdist), direc, loc.coord);
				if (Navigate.PathClear(loc.coord, mpt))
					mdist = emdist + 1;
				if (mdist >= emdist)
					{
					if (pos_obstacle)
						turned = Turn.TurnAngle(-aa);
					else
						turned = Turn.TurnAngle(aa);
					if (turned)
						{
						loc.orientation = direc;
						if (pos_obstacle)
							loc.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(-aa), loc.orientation, loc.coord);
						else
							loc.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(aa), loc.orientation, loc.coord);
						NavData.SetCurrentLocation(loc);
						orient_changed = true;
						pos_obstacle = false;
						neg_obstacle = false;
						lscan.Clear();
						Rplidar.CaptureScan(ref lscan,true);
						if (ObstacleFound(lscan,emdist + clear,emdist,ref modist) == obstacle_action.NO_OBSTACLE)
							rtn = true;
						else
							{
							if (pos_obstacle)
								prior_adjust = -aa;
							else
								prior_adjust = aa;
							Log.LogEntry("Adjust: obstacle found after making adjustment turn.");
							}
						}
					else
						Log.LogEntry("Adjust: attempt to turn failed.");
					}
				else
					Log.LogEntry("Adjust: could not determine that adjustment path clear.");
				}
			else
				{
				if (CheckLocalization(emdist,clear, ref orient_changed))
					rtn = true;
				else
					{
					walk_around_possible = true;
					Log.LogEntry("Adjust: could not make adjustment turn of " + aa);
					}
				}
			return(rtn);
		}



		private void LogObstacles()

		{
			int i;
			Rplidar.scan_data sd;
			int direc;
			NavData.location clocation;
			Point pt;
			ArrayList op = new ArrayList();

			if (obstacles.Count > 0)
				{
				clocation = NavData.GetCurrentLocation();
				for (i = 0;i < obstacles.Count;i++)
					{
					sd = (Rplidar.scan_data) obstacles[i];
					direc = (clocation.orientation + sd.angle) % 360;
					pt = NavCompute.MapPoint(new Point(0, (int) Math.Round(sd.dist)), direc, clocation.coord);
					op.Add(pt);
					}
				Log.LogArrayList("Detected obstacle",op);
				}
		}



		private obstacle_action ObstacleAvoid(int emdist,int clear,ref int min_obs_dist,ref bool orient_changed)

		{
			obstacle_action rtn = obstacle_action.NO_OBSTACLE;
			int modist = 0;
			PersonDetect pd = new PersonDetect();
			bool walk_around_possible = false;

			if (Rplidar.Connected() && Rplidar.CaptureScan(ref lscan,true))
				{
				rtn = ObstacleFound(lscan,emdist + clear,emdist,ref modist);
				if (obstacles.Count == 1)													//LIDAR ANOMALY? COULD CHECK WITH KINECT RATHER THEN RESCAN
					{
					if (Rplidar.CaptureScan(ref lscan,true))
						rtn = ObstacleFound(lscan,emdist + clear,emdist,ref modist);
					}
				switch (rtn)
					{
					case obstacle_action.DISTANCE:
						min_obs_dist = modist;
						break;

					case obstacle_action.ADJUST:
						AnalyzeObstacle(emdist);
						if (pos_obstacle && neg_obstacle)
							{
							if (SqdWalkAround())
								rtn = obstacle_action.REMAPPED;
							else
								rtn = obstacle_action.NONE;
							}
						else if (Adjust(emdist,clear,MAX_ADJUST_ANGLE, ref orient_changed,ref walk_around_possible))
							rtn = obstacle_action.NO_OBSTACLE;
						else if (walk_around_possible)
							{
							if (AngledWalkAround())
								rtn = obstacle_action.REMAPPED;
							else
								rtn = obstacle_action.NONE;
							}
						if ((rtn == obstacle_action.NONE) && pd.ObstacleIsPerson(obs_center_angle))
							rtn = PersonInteraction(emdist,clear);
						else if ((rtn == obstacle_action.NONE) && (aa > MAX_ADJUST_ANGLE))
							{
							if (Adjust(emdist, clear,aa + 2, ref orient_changed, ref walk_around_possible))
								rtn = obstacle_action.NO_OBSTACLE;
							else
								rtn = obstacle_action.NONE;
							}
						break;
					}
				if ((obstacles.Count > 0) && SharedData.log_operations)
					LogObstacles();
				}
			else
				{
				Log.LogEntry("Could not capture LIDAR scan.");
				rtn = obstacle_action.NONE;
				}
			return(rtn);
		}



		public obstacle_action ObstacleAvoidAdjust(int emdist,int clear,ref int min_obs_dist,ref bool orient_changed,Point move_pt,Point end_pt)

		{
			obstacle_action rtn = obstacle_action.NO_OBSTACLE;

			obstacles.Clear();
			mptal.Clear();
			lscan.Clear();
			mp = move_pt;
			ep = end_pt;
			prior_adjust = 0;
			initial_orientation = NavData.GetCurrentLocation().orientation;
			rtn = ObstacleAvoid(emdist, clear,ref min_obs_dist, ref orient_changed);
			if (rtn == obstacle_action.ADJUST)
				{
				rtn = ObstacleAvoid(emdist,clear,ref min_obs_dist,ref orient_changed);
				if (rtn == obstacle_action.ADJUST)
					rtn = obstacle_action.NONE;
				}
			return(rtn);
		}

	

		private static bool ObstacleAdjustAngle(ArrayList obs,int odist,int shift_angle,double side_clear,ref int angle)

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
				rtn = true;
				}
			else
				{
				angle = maxtangle;
				if (left_obs)
					angle *= -1;
				rtn = true;
				}
			Log.LogEntry("Obstacle adjust angle: " + rtn + "  " + angle);
			return (rtn);
		}



		public bool DepartObstacleAvoid(int dist,int front_clear,int side_clear,int adjust_limit)

		{
			bool ready_to_move = false,turn_ok = true;
			ArrayList sdata = new ArrayList();
			int angle = 0,direct;
			NavData.location cloc;
			PersonDetect pd = new PersonDetect();

			if (Rplidar.CaptureScan(ref sdata, true))
				{
				obstacles.Clear();
				Rplidar.FindObstacles(0,dist + front_clear,sdata,side_clear,true, ref obstacles);
				if (obstacles.Count > 0)
					{
					if (ObstacleAdjustAngle(obstacles,dist + front_clear,0,side_clear,ref angle))
						{
						if (Math.Abs(angle) < adjust_limit)
							{
							if (Math.Abs(angle) < SharedData.MIN_TURN_ANGLE)
								{
								if (angle < 0)
									angle = -SharedData.MIN_TURN_ANGLE;
								else
									angle = SharedData.MIN_TURN_ANGLE;
								}
							if ((turn_ok = Turn.TurnAngle(angle)))
								{
								cloc = NavData.GetCurrentLocation();
								direct = (cloc.orientation - angle) % 360;
								if (direct < 0)
									direct += 360;
								cloc.orientation = direct;
								cloc.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(angle),direct, cloc.coord,true);
								cloc.ls = NavData.LocationStatus.DR;
								NavData.SetCurrentLocation(cloc);
								obstacles.Clear();
								sdata.Clear();
								Rplidar.CaptureScan(ref sdata,true);
								Rplidar.FindObstacles(0, dist + front_clear,sdata,side_clear,true, ref obstacles);
								if (obstacles.Count == 0)
									ready_to_move = true;
								else
									Log.LogEntry("DepartObstacleAvoid: obstacle detected after adjustment.");
								}
							else
								Log.LogEntry("DepartObstacleAvoid: attempt to turn failed.");
							}
						else
							Log.LogEntry("DepartObstacleAvoid: determined adjustment angle, " + angle + " exceeds limit " + adjust_limit);
						}
					else
						Log.LogEntry("DepartObstacleAvoid: could not determine obstacle avoid angle.");
					}
				else
					ready_to_move = true;
				if (!ready_to_move && turn_ok)
					{
					AnalyzeObstacle(dist);
					if (pd.ObstacleIsPerson(obs_center_angle))
						ready_to_move = (PersonInteraction(dist, front_clear) == obstacle_action.NO_OBSTACLE);
					}
				}
			else
				Log.LogEntry("DepartObstacleAvoid: could not obtain LIDAR scan.");
			return (ready_to_move);
		}



		public ArrayList DetectedObstacles()

		{
			return(obstacles);
		}



		public ArrayList NewPath()

		{
			return(mptal);
		}

		}
	}
