using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Text;


namespace AutoRobotControl
	{
	public class LocDecisionEngine
		{

		private const int LIDAR_EDGE_GR_LIMIT = 60;

		public enum Type { NONE, OBSTRUCT_OPEN_WALL, OPEN_WALL, OBSTRUCT_WALL, WALL, CORNER, CONNECTION };
		public enum Actions { EDGE, CORNER, TARGET, PERP_DIR };
		public enum Sensor { NONE, FLIDAR,RLIDAR, KINECT };

		public struct loc_info
			{
			public Point start;
			public Point end;
			public Type loc_type;
			public int direct;
			public int distance;
			public int length;
			public int index;
			public int ra;
			public bool target;
			};


		public struct action
			{
			public int info_index;
			public Sensor sensor;
			public Actions act;
			public Point coord;
			public int offset;
			public int dspan;
			public bool head_motion_required;
			public NavData.EdgeType et;
			};


		private ArrayList context_info = new ArrayList(), ppactions = new ArrayList(), spactions = new ArrayList(), dactions = new ArrayList();
		private ArrayList feature = new ArrayList();
		private SortedList wallperp = new SortedList();
		private bool wallperponly = false;



		private void DecideOnActionDetailWPO()

		{
			dactions.Clear();
			if (wallperp.Count > 0)
				dactions.Add((action) wallperp[wallperp.Count - 1]);
		}


		private ArrayList DecideOnActionDetail()

		{
			action act,act2;
			loc_info li1,li2,li3,li4;
			bool done = false;
			int i;

			dactions.Clear();
			if (wallperp.Count >= 2)
				{
				if (wallperp.Count == 2)
					{
					act = (action) wallperp.GetByIndex(1);
					li1 = (loc_info) context_info[act.info_index];
					act = (action) wallperp.GetByIndex(0);
					li2 = (loc_info) context_info[act.info_index];
					if ((Math.Abs(li1.direct - li2.direct) == 90) || (Math.Abs(li1.direct - li2.direct) == 270))
						{
						dactions.Add((action) wallperp.GetByIndex(1));
						dactions.Add((action) wallperp.GetByIndex(0));
						done = true;
						}
					}
				if (wallperp.Count == 3)
					{
					act = (action) wallperp.GetByIndex(2);
					li1 = (loc_info) context_info[act.info_index];
					act = (action) wallperp.GetByIndex(1);
					li2 = (loc_info) context_info[act.info_index];
					act = (action) wallperp.GetByIndex(0);
					li3 = (loc_info) context_info[act.info_index];
					if ((Math.Abs(li1.direct - li2.direct) == 90) || (Math.Abs(li1.direct - li2.direct) == 270))
						{
						dactions.Add((action) wallperp.GetByIndex(2));
						dactions.Add((action) wallperp.GetByIndex(1));
						done = true;
						}
					else if ((Math.Abs(li1.direct - li3.direct) == 90) || (Math.Abs(li1.direct - li3.direct) == 270))
						{
						dactions.Add((action) wallperp.GetByIndex(2));
						dactions.Add((action) wallperp.GetByIndex(0));
						done = true;
						}
					else if ((Math.Abs(li2.direct - li3.direct) == 90) || (Math.Abs(li2.direct - li3.direct) == 270))
						{
						dactions.Add((action)wallperp.GetByIndex(1));
						dactions.Add((action)wallperp.GetByIndex(0));
						done = true;
						}
					}
				if (wallperp.Count == 4)
					{
					act = (action) wallperp.GetByIndex(3);
					li1 = (loc_info) context_info[act.info_index];
					act = (action) wallperp.GetByIndex(2);
					li2 = (loc_info) context_info[act.info_index];
					act = (action) wallperp.GetByIndex(1);
					li3 = (loc_info) context_info[act.info_index];
					act = (action) wallperp.GetByIndex(0);
					li4 = (loc_info) context_info[act.info_index];
					if ((Math.Abs(li1.direct - li2.direct) == 90) || (Math.Abs(li1.direct - li2.direct) == 270))
						{
						dactions.Add((action) wallperp.GetByIndex(3));
						dactions.Add((action) wallperp.GetByIndex(2));
						done = true;
						}
					else if ((Math.Abs(li1.direct - li3.direct) == 90) || (Math.Abs(li1.direct - li3.direct) == 270))
						{
						dactions.Add((action) wallperp.GetByIndex(3));
						dactions.Add((action) wallperp.GetByIndex(1));
						done = true;
						}
					else if ((Math.Abs(li1.direct - li4.direct) == 90) || (Math.Abs(li1.direct - li4.direct) == 270))
						{
						dactions.Add((action)wallperp.GetByIndex(3));
						dactions.Add((action)wallperp.GetByIndex(0));
						done = true;
						}
					else if ((Math.Abs(li2.direct - li4.direct) == 90) || (Math.Abs(li2.direct - li4.direct) == 270))
						{
						dactions.Add((action)wallperp.GetByIndex(2));
						dactions.Add((action)wallperp.GetByIndex(0));
						done = true;
						}
					else if ((Math.Abs(li2.direct - li3.direct) == 90) || (Math.Abs(li2.direct - li3.direct) == 270))
						{
						dactions.Add((action)wallperp.GetByIndex(2));
						dactions.Add((action)wallperp.GetByIndex(1));
						done = true;
						}
					else if ((Math.Abs(li3.direct - li4.direct) == 90) || (Math.Abs(li3.direct - li4.direct) == 270))
						{
						dactions.Add((action)wallperp.GetByIndex(1));
						dactions.Add((action)wallperp.GetByIndex(0));
						done = true;
						}
					}
				}
			if ((!done) && (wallperp.Count > 0))
				{
				if (feature.Count > 0)
					{
					act = (action)feature[0];
					if (act.act == Actions.EDGE)
						{
						li1 = (loc_info)context_info[act.info_index];
						if (act.coord == li1.start)
							{
							if ((act.info_index > 0) && (((loc_info)context_info[act.info_index - 1]).loc_type == Type.WALL))
								{
								for (i = 0; i < wallperp.Count; i++)
									{
									act2 = (action)wallperp.GetByIndex(i);
									if (act2.info_index == act.info_index - 1)
										{
										dactions.Add(act2);
										dactions.Add(act);
										done = true;
										break;
										}
									}
								}
							}
						else if ((act.info_index < context_info.Count - 1) && (((loc_info)context_info[act.info_index + 1]).loc_type == Type.WALL))
							{
							for (i = 0; i < wallperp.Count; i++)
								{
								act2 = (action)wallperp.GetByIndex(i);
								if (act2.info_index == act.info_index - 1)
									{
									dactions.Add(act2);
									dactions.Add(act);
									done = true;
									break;
									}
								}
							}
						}
					}
				if (!done)
					{
					dactions.Add((action) wallperp.GetByIndex(wallperp.Count - 1));
					if (feature.Count >= 1)
						{
						dactions.Add((action) feature[0]);
						done = true;
						}
					}
				}
			if (!done && (wallperp.Count == 0) && (feature.Count >= 2))
				 {
				 act = (action) feature[0];
				 act2 = (action) feature[1];
				 if (act.info_index != act2.info_index)
					{
					dactions.Add(act);
					dactions.Add(act2);
					done = true;
					}
				else if (! done && (feature.Count > 2))
					{
					act2 = (action) feature[2];
					 if (act.info_index != act2.info_index)
						{
						dactions.Add(act);
						dactions.Add(act2);
						done = true;
						}
					else if (! done && (feature.Count > 3))
						{
						act2 = (action) feature[3];
						 if (act.info_index != act2.info_index)
							{
							dactions.Add(act);
							dactions.Add(act2);
							done = true;
							}
						 else if (!done && (feature.Count > 3))
							 {
							 act2 = (action)feature[4];
							 if (act.info_index != act2.info_index)
								{
								dactions.Add(act);
								dactions.Add(act2);
								done = true;
								}
							 }
						}
					}
				 }
			return(dactions);
		}



		private void WallPerpAdd(action act)

		{
			while (wallperp.Contains(act.dspan))
				act.dspan -= 1;
			wallperp.Add(act.dspan,act);
		}


		private void DecideOnAction(bool kinect_head_motion)

		{
			action act;
			int i;
			ArrayList wallperpwhm = new ArrayList(), featurewhm = new ArrayList();

			wallperp.Clear();
			feature.Clear();
			if (!kinect_head_motion)
				{
				for (i = 0;i < ppactions.Count;i++)
					{
					act = (action) ppactions[i];
					if ((act.sensor == Sensor.FLIDAR) || ((act.sensor == Sensor.KINECT) && (!act.head_motion_required)))
						{
						if (act.act == Actions.PERP_DIR)
							WallPerpAdd(act);
						else
							feature.Add(act);
						}
					}
				}
			else
				{
				for (i = 0;i < ppactions.Count;i++)
					{
					act = (action) ppactions[i];
					if (act.act == Actions.PERP_DIR)
						{
						if ((act.sensor == Sensor.KINECT) && act.head_motion_required)
							wallperpwhm.Add(act);
						else
							WallPerpAdd(act);
						}
					else
						{
						if ((act.sensor == Sensor.KINECT) && act.head_motion_required)
							featurewhm.Add(act);
						else
							feature.Add(act);
						}
					}
				}
			if (wallperponly)
				DecideOnActionDetailWPO();
			else
				DecideOnActionDetail();
			if ((dactions.Count < 2) && kinect_head_motion)
				{
				for (i = 0;i < wallperpwhm.Count;i++)
					WallPerpAdd((action) wallperpwhm[i]);
				feature.AddRange(featurewhm);
				DecideOnActionDetail();
				}
		}



		private bool ConnectionCoordBetween(Point c1,Point c2,Point cc)

		{
			bool rtn = false;

			if (c1.X == c2.X)
				{
				if ((c1.Y > c2.Y) && ((cc.Y <= c1.Y) && (cc.Y >= c2.Y)))
					rtn = true;
				else if ((c1.Y < c2.Y) && ((cc.Y >= c1.Y) && (cc.Y <= c2.Y)))
					rtn = true;
				}
			else if (c1.Y == c2.Y)
				{
				if ((c1.X > c2.X) && ((cc.X <= c1.X) && (cc.X >= c2.X)))
					rtn = true;
				else if ((c1.X < c2.X) && ((cc.X >= c1.X) && (cc.X <= c2.X)))
					rtn = true;
				}
			return(rtn);
		}



		private action LidarAnalysis(NavData.location cloc,loc_info li)

		{
			NavData.connection connect;
			NavCompute.pt_to_pt_data sptp,eptp;
			action act = new action();
			int span;

			if ((li.loc_type == Type.OBSTRUCT_OPEN_WALL) || (li.loc_type == Type.OBSTRUCT_WALL) || (li.loc_type == Type.CORNER))
				act.sensor = Sensor.NONE;
			else if (li.loc_type == Type.WALL)
				{
				if (li.length > SharedData.MIN_WALL_DIST)
					{
					sptp = NavCompute.DetermineRaDirectDistPtToPt(li.start,cloc.coord,false);
					eptp = NavCompute.DetermineRaDirectDistPtToPt(li.end,cloc.coord,false);
					if ((sptp.dist < SharedData.LIDAR_MAX_DIST) && (eptp.dist < SharedData.LIDAR_MAX_DIST))
						{
						act.sensor = Sensor.FLIDAR;
						act.act = Actions.PERP_DIR;
						span = Math.Abs(sptp.direc - eptp.direc);
						act.dspan = span;
						}
					}
				}
			else if (li.loc_type == Type.OPEN_WALL)
				{
				if (li.length > SharedData.MIN_WALL_DIST)
					{
					sptp = NavCompute.DetermineRaDirectDistPtToPt(li.start,cloc.coord,false);
					eptp = NavCompute.DetermineRaDirectDistPtToPt(li.end,cloc.coord,false);
					if ((sptp.dist < SharedData.LIDAR_MAX_DIST) && (eptp.dist < SharedData.LIDAR_MAX_DIST))
						{
						act.sensor = Sensor.FLIDAR;
						act.act = Actions.PERP_DIR;
						span = Math.Abs(sptp.direc - eptp.direc);
						act.dspan = span;
						}
					}
				}
			else if (li.loc_type == Type.CONNECTION)
				{
				Point op;

				if (li.distance < SharedData.LIDAR_MAX_DIST)
					{
					connect = (NavData.connection) NavData.rd.connections[li.index];
					if ((connect.hc_edge.ef.type == NavData.FeatureType.OPENING_EDGE) || (connect.lc_edge.ef.type == NavData.FeatureType.OPENING_EDGE))
						{
						if ((connect.hc_edge.ef.type == NavData.FeatureType.OPENING_EDGE) && (!connect.hc_edge.door_side))
							{
							op = NavCompute.DetermineVisualObstacleProjectPt(cloc.coord,connect.hc_edge.ef.coord,NavData.detail_map,false);
							if ((op.IsEmpty || (op == connect.hc_edge.ef.coord))  && ConnectionCoordBetween(li.start,li.end,connect.hc_edge.ef.coord))
								if (NavCompute.DistancePtToPt(cloc.coord,connect.hc_edge.ef.coord) < SharedData.LIDAR_MAX_DIST)
									{
									act.sensor = Sensor.FLIDAR;
									act.act = Actions.EDGE;
									act.coord = connect.hc_edge.ef.coord;
									act.et = connect.hc_edge.type;
									}
							}
						else if ((connect.lc_edge.ef.type == NavData.FeatureType.OPENING_EDGE) && (!connect.lc_edge.door_side))
							{
							op = NavCompute.DetermineVisualObstacleProjectPt(cloc.coord, connect.lc_edge.ef.coord, NavData.detail_map,false);
							if ((op.IsEmpty || (op == connect.lc_edge.ef.coord)) && ConnectionCoordBetween(li.start, li.end, connect.lc_edge.ef.coord))
								if (NavCompute.DistancePtToPt(cloc.coord,connect.lc_edge.ef.coord) < SharedData.LIDAR_MAX_DIST)
									{
									act.sensor = Sensor.FLIDAR;
									act.act = Actions.EDGE;
									act.coord = connect.lc_edge.ef.coord;
									act.et = connect.lc_edge.type;
									}
							}
						}
					}
				}
			return(act);
		}



		private bool KinectGoodWallProject(NavData.location loc,int direct,int wall_perp_direct)

		{
			bool rtn = false;
			Point wp;
			int dir,dist;

			dir = direct - 10;
			if (dir < 0)
				dir += 360;
			wp = NavCompute.DetermineWallProjectPt(loc.coord,dir,false);
			dist = NavCompute.DistancePtToPt(loc.coord,wp);
			if ((NavCompute.DirectPerpToWall(wp) == wall_perp_direct) && (dist < SharedData.KINECT_MAX_DIST) && (dist > SharedData.KINECT_MIN_DIST))
				{
				dir = (direct + 10) % 360;
				wp = NavCompute.DetermineWallProjectPt(loc.coord,dir,false);
				dist = NavCompute.DistancePtToPt(loc.coord,wp);
				if ((NavCompute.DirectPerpToWall(wp) == wall_perp_direct) && (dist < SharedData.KINECT_MAX_DIST) && (dist > SharedData.KINECT_MIN_DIST))
					rtn = true;
				}
			return(rtn);
		}


		private ArrayList KinectAnalysis(NavData.location cloc, loc_info li, bool head_motion)

		{
			action act = new action();
			ArrayList actions = new ArrayList();

			if ((li.loc_type == Type.OBSTRUCT_OPEN_WALL) || (li.loc_type == Type.OPEN_WALL))
				act.sensor = Sensor.NONE;
			else if (li.loc_type == Type.CORNER)
				{
				if ((li.distance < SharedData.KINECT_MAX_DIST) && (li.distance > SharedData.KINECT_MIN_DIST) && (head_motion || (Math.Abs(li.ra) < (SharedData.KINECT_HOR_VIEW/2))))
					{
					act.sensor = Sensor.KINECT;
					act.act = Actions.CORNER;
					if (Math.Abs(li.ra) < (SharedData.KINECT_HOR_VIEW/2) - 5)
						act.head_motion_required = false;
					else
						act.head_motion_required = true;
					}
				}
			else if ((li.loc_type == Type.WALL) || (li.loc_type == Type.OBSTRUCT_WALL))
				{
				int direct;
				ArrayList features = new ArrayList();

				if (li.target)
					{
					if (head_motion || (Math.Abs(li.ra) < (SharedData.KINECT_HOR_VIEW/2)))
						{
						act.sensor = Sensor.KINECT;
						act.act = Actions.TARGET;
						features = NavData.GetFeatures(cloc.rm_name);

						try
						{
						act.coord = ((NavData.feature) features[li.index]).coord;
						if (Math.Abs(li.ra) < (SharedData.KINECT_HOR_VIEW / 2))
							act.head_motion_required = false;
						else
							act.head_motion_required = true;
						actions.Add(act);
						act = new action();
						}

						catch(Exception ex)
						{
						Log.LogEntry("KinectAnalysis exception: " + ex.Message);
						Log.LogEntry("Feature index: " + li.index);
						Log.LogEntry("Stack trace: " + ex.StackTrace);
						}

						}
					}
				if (li.length > SharedData.MIN_WALL_DIST)
					{
					if (KinectGoodWallProject(cloc,cloc.orientation,li.direct))
						{
						act.sensor = Sensor.KINECT;
						act.act = Actions.PERP_DIR;
						act.offset = 0;
						act.head_motion_required = false;
						}
					else
						{
						direct = cloc.orientation - 15;
						if (direct < 0)
							direct += 360;
						if (KinectGoodWallProject(cloc,cloc.orientation,li.direct))
							{
							act.sensor = Sensor.KINECT;
							act.act = Actions.PERP_DIR;
							act.offset = -15;
							act.head_motion_required = false;
							}
						else
							{
							direct = (cloc.orientation + 15) % 360;
							if (KinectGoodWallProject(cloc,cloc.orientation,li.direct))
								{
								act.sensor = Sensor.KINECT;
								act.act = Actions.PERP_DIR;
								act.offset = 15;
								act.head_motion_required = false;
								}
							else if (head_motion)
								{
								act.sensor = Sensor.KINECT;
								act.act = Actions.PERP_DIR;
								act.offset = 0;
								act.head_motion_required = true;
								}
							}
						}
					}
				}
			else if (li.loc_type == Type.CONNECTION)
				{
				NavData.connection connect;
				NavCompute.pt_to_pt_data ptp;
				int ra;

				if (li.distance < SharedData.KINECT_MAX_DIST)
					{
					connect = (NavData.connection) NavData.rd.connections[li.index];
					if ((connect.hc_edge.ef.type == NavData.FeatureType.OPENING_EDGE) || (connect.lc_edge.ef.type == NavData.FeatureType.OPENING_EDGE))
						{
						if ((connect.hc_edge.ef.type == NavData.FeatureType.OPENING_EDGE) && (!connect.hc_edge.door_side))
							{
							ptp = NavCompute.DetermineRaDirectDistPtToPt(connect.hc_edge.ef.coord,cloc.coord,false);
							if ((ptp.dist < SharedData.KINECT_MAX_DIST) && (ptp.dist > SharedData.KINECT_MIN_DIST) && (NavCompute.AngularDistance(cloc.orientation,connect.direction) < 45) && (head_motion || (ptp.ra < (SharedData.KINECT_HOR_VIEW/2))))
								{
								act.sensor = Sensor.KINECT;
								act.act = Actions.EDGE;
								act.coord = connect.hc_edge.ef.coord;
								act.et = connect.hc_edge.type;
								ra = Math.Abs(cloc.orientation - ptp.direc);
								if (ra < (SharedData.KINECT_HOR_VIEW / 2))
									act.head_motion_required = false;
								else
									act.head_motion_required = true;
								}
							}
						else if ((connect.lc_edge.ef.type == NavData.FeatureType.OPENING_EDGE) && (!connect.lc_edge.door_side))
							{
							ptp = NavCompute.DetermineRaDirectDistPtToPt(connect.lc_edge.ef.coord, cloc.coord,false);
							if ((ptp.dist < SharedData.KINECT_MAX_DIST) && (ptp.dist > SharedData.KINECT_MIN_DIST) && (NavCompute.AngularDistance(cloc.orientation, connect.direction) < 45) && (head_motion || (ptp.ra < (SharedData.KINECT_HOR_VIEW / 2))))
								{
								act.sensor = Sensor.KINECT;
								act.act = Actions.EDGE;
								act.coord = connect.lc_edge.ef.coord;
								act.et = connect.lc_edge.type;
								ra = Math.Abs(cloc.orientation - ptp.direc);
								if (ra < (SharedData.KINECT_HOR_VIEW / 2))
									act.head_motion_required = false;
								else
									act.head_motion_required = true;
								}
							}
						}
					}
				}
			actions.Add(act);
			return(actions);
		}



		private bool AdjacentToClearWallSeg(int curr_index)

		{
			bool adjacent = false;
			loc_info cli,ali;

			cli = (loc_info) context_info[curr_index];
			if (curr_index > 0)
				{
				ali = (loc_info)context_info[curr_index - 1];
				if ((cli.direct == ali.direct) && (ali.loc_type == Type.WALL))
					adjacent = true;
				}
			if (!adjacent && (curr_index < context_info.Count - 1))
				{
				ali = (loc_info)context_info[curr_index + 1];
				if ((cli.direct == ali.direct) && (ali.loc_type == Type.WALL))
					adjacent = true;
				}
			return(adjacent);
		}



		private void AnalyzeInfo(NavData.location cloc,bool head_motion,bool kinect,bool lidar)

		{
			int i,j;
			action act;
			loc_info li;
			bool lidar_action_found = false;
			ArrayList actions;

			ppactions.Clear();
			spactions.Clear();
			for (i = 0;i < context_info.Count;i++)
				{
				li = (loc_info) context_info[i];
				if (li.loc_type != Type.NONE)
					{
					if (lidar)
						{
						act = LidarAnalysis(cloc,li);
						if (act.sensor == Sensor.FLIDAR)
							{
							act.info_index = i;
							if (li.loc_type == Type.OPEN_WALL)
								spactions.Add(act);
							else if ((li.loc_type == Type.CONNECTION) && (li.distance > LIDAR_EDGE_GR_LIMIT))
								spactions.Add(act);
							else
								ppactions.Add(act);
							lidar_action_found = true;
							}
						else
							lidar_action_found = false;
						}
					if (kinect)
						{
						actions = KinectAnalysis(cloc,li,head_motion);
						for (j = 0;j < actions.Count;j++)
							{
							act = (action) actions[j];
							if (act.sensor == Sensor.KINECT)
								{
								act.info_index = i;
								if (lidar_action_found && (act.act != Actions.TARGET))
									spactions.Add(act);
								else if ((act.act == Actions.PERP_DIR) && (li.loc_type == Type.OBSTRUCT_WALL))
									{
									if (!AdjacentToClearWallSeg(i))
										spactions.Add(act);
									}
								else
									ppactions.Add(act);
								}
							}
						}
					}
				}
		}



		private bool NonFeatureCorner(ref loc_info li, int len)

		{
			bool rtn = false;

			if (len == 1)
				if ((li.loc_type == Type.OBSTRUCT_OPEN_WALL) || (li.loc_type == Type.OBSTRUCT_WALL) || (li.loc_type == Type.OPEN_WALL) || (li.loc_type == Type.WALL))
					if ((li.start.X == 0) && ((li.start.Y == 0) || (li.start.Y == NavData.rd.rect.Height - 1)) || ((li.start.X == NavData.rd.rect.Width - 1) && ((li.start.Y == 0) || (li.start.Y == NavData.rd.rect.Height - 1))))
						rtn = true;
			return(rtn);
		}



		private bool Between(int sdirect,int edirect,int cdirect)

		{
			bool rtn = false;

			if (sdirect > edirect)
				{
				if ((cdirect > edirect) && (cdirect < sdirect))
					rtn = true;
				}
			else
				if ((cdirect < sdirect) || (cdirect > edirect))
					rtn = true;
			return(rtn);
		}



		// gathers context info from left to right
		private void GatherContextInfo(NavData.location cloc,bool perp_wall_only,ref int no_pts)

		{
			Point wp,op,lwp = new Point();
			int sdirect,cdirect,edirect,wdirect,cdist,len = 0,max_no_pts;
			NavCompute.pt_to_pt_data ppd;
			loc_info current = new loc_info(),last = new loc_info();
			bool done = false;

			context_info.Clear();
			sdirect = cloc.orientation;
			sdirect -= 160;
			if (sdirect < 0)
				sdirect += 360;
			edirect = (sdirect + 320) % 360;
			cdirect = sdirect; 
			wp = NavCompute.DetermineWallProjectPt(cloc.coord,sdirect,false);
			max_no_pts = (NavData.rd.rect.Height * 2) + (NavData.rd.rect.Width * 2);
			no_pts = 0;
			Log.LogEntry("GatherContectInfo:  start wp @ " + wp + ", max no pts " + max_no_pts);
			while (!done)
				{

				try
				{
				no_pts += 1;
				if (NavData.detail_map[wp.X,wp.Y] == (byte) Room.MapCode.CLEAR)
					{
					op = NavCompute.DetermineVisualObstacleProjectPt(cloc.coord,wp,NavData.detail_map,false);
					if (op.IsEmpty || (op == wp))
						current.loc_type = Type.WALL;
					else
						current.loc_type = Type.OBSTRUCT_WALL;
					wdirect = NavCompute.DirectPerpToWall(wp);
					if ((wdirect == 0) || (wdirect == 180))
						cdist = Math.Abs(wp.Y - cloc.coord.Y);
					else
						cdist = Math.Abs(wp.X - cloc.coord.X);
					current.direct = wdirect;
					current.distance = cdist;
					}
				else if (NavData.detail_map[wp.X, wp.Y] == (byte)Room.MapCode.BLOCKED)
					{
					current.loc_type = Type.OBSTRUCT_WALL;
					wdirect = NavCompute.DirectPerpToWall(wp);
					if ((wdirect == 0) || (wdirect == 180))
						cdist = Math.Abs(wp.Y - cloc.coord.Y);
					else
						cdist = Math.Abs(wp.X - cloc.coord.X);
					current.direct = wdirect;
					current.distance = cdist;
					}
				else if (NavData.detail_map[wp.X, wp.Y] == (byte)Room.MapCode.OPEN_WALL)
					{
					op = NavCompute.DetermineVisualObstacleProjectPt(cloc.coord,wp,NavData.detail_map,false);
					if (op.IsEmpty || (op == wp))
						current.loc_type = Type.OPEN_WALL;
					else
						current.loc_type = Type.OBSTRUCT_OPEN_WALL;
					wdirect = NavCompute.DirectPerpToWall(wp);
					if ((wdirect == 0) || (wdirect == 180))
						cdist = Math.Abs(wp.Y - cloc.coord.Y);
					else
						cdist = Math.Abs(wp.X - cloc.coord.X);
					current.direct = wdirect;
					current.distance = cdist;
					}
				else if (perp_wall_only)
					{
					current.loc_type = Type.NONE;
					current.direct = -1;
					}
				else if ((NavData.detail_map[wp.X, wp.Y] - (NavData.detail_map[wp.X, wp.Y] % 10)) == (byte)Room.MapCode.CORNER)
					{
					current.loc_type = Type.CORNER;
					current.index = NavData.detail_map[wp.X, wp.Y] % 10;
					ppd = NavCompute.DetermineRaDirectDistPtToPt(wp, cloc.coord,false);
					current.distance = ppd.dist;
					cdirect = cloc.orientation - ppd.direc;
					if (cdirect > 180)
						cdirect = 360 - cdirect;
					else if (cdirect < -180)
						cdirect = 360 + cdirect;
					current.ra = cdirect;
					}
				else if ((NavData.detail_map[wp.X, wp.Y] - (NavData.detail_map[wp.X, wp.Y] % 10)) == (byte)Room.MapCode.EXIT)
					{
					NavData.connection connect;

					current.loc_type = Type.CONNECTION;
					current.index = NavData.detail_map[wp.X, wp.Y] % 10;
					connect = (NavData.connection) NavData.rd.connections[current.index];
					current.distance = NavCompute.DistancePtToPt(connect.exit_center_coord,cloc.coord);
					}
				else if ((NavData.detail_map[wp.X, wp.Y] - (NavData.detail_map[wp.X, wp.Y] % 10)) == (byte)Room.MapCode.TARGET)
					{
					current.loc_type = last.loc_type;
					last.target = true;
					last.index = NavData.detail_map[wp.X, wp.Y] % 10;
					ppd = NavCompute.DetermineRaDirectDistPtToPt(wp, cloc.coord,false);
					current.distance = ppd.dist;
					cdirect = cloc.orientation - ppd.direc;
					if (cdirect > 180)
						cdirect = 360 - cdirect;
					else if (cdirect < -180)
						cdirect = 360 + cdirect;
					last.ra = cdirect;
					}
				if ((current.loc_type == last.loc_type) && (current.direct == last.direct))
					len += 1;
				else
					{
					if ((last.loc_type != Type.NONE) && !NonFeatureCorner(ref last,len))
						{
						last.length = len;
						if ((last.loc_type == Type.CONNECTION) || (last.loc_type == Type.CORNER) || (last.target) || (len > 1))
							{
							last.end = lwp;
							context_info.Add(last);
							}
						}
					current.start = wp;
					len = 1;
					last = current;
					}
				lwp = wp;
				if ((wp.X == 0) && (wp.Y == 0))
					wp.X += 1;
				else if ((wp.X == NavData.rd.rect.Width - 1) && (wp.Y == 0))
					wp.Y += 1;
				else if ((wp.X == NavData.rd.rect.Width - 1) && (wp.Y == NavData.rd.rect.Height - 1))
					wp.X -= 1;
				else if ((wp.X == 0) && (wp.Y == NavData.rd.rect.Height - 1))
					wp.Y -= 1;
				else if (wp.Y == 0)
					wp.X += 1;
				else if (wp.X == 0)
					wp.Y -= 1;
				else if (wp.Y == NavData.rd.rect.Height - 1)
					wp.X -= 1;
				else
					wp.Y += 1;
				ppd = NavCompute.DetermineRaDirectDistPtToPt(wp,cloc.coord,false);
				cdirect = ppd.direc;
				if (no_pts < 10)
					done = false;
				else if ((cdirect == edirect) || Between(sdirect,edirect,cdirect) || (no_pts >= max_no_pts))
					{
					if (cdirect == edirect)
						Log.LogEntry("At end direction.");
					else if (Between(sdirect,edirect,cdirect))
						Log.LogEntry("In dead zone.");
					else
						Log.LogEntry("At max. number points of " + max_no_pts);
					done = true;
					}
				}

				catch (Exception ex)
				{
				Log.LogEntry("GatherContextInfo exception: " + ex.Message);
				Log.LogEntry("Data: no points - " + no_pts + "   wp - " + wp.ToString());
				break;
				}

				}
			if (last.loc_type != Type.NONE)
				{
				last.length = len;
				last.end = lwp;
				context_info.Add(last);
				}
		}



		public ArrayList Run(NavData.location loc,bool head_motion,bool kinect,bool lidar,bool perp_wall_only)

		{	
			int no_pts = 0;
			int i;
			StringBuilder line;
			Stopwatch sw = new Stopwatch();
			loc_info li;
			action act;

			wallperponly = perp_wall_only;
			if (loc.coord.X == 0)
				loc.coord.X = 1;
			else if (loc.coord.X == NavData.rd.rect.Width - 1)
				loc.coord.X -= 1;
			if (loc.coord.Y == 0)
				loc.coord.Y = 1;
			else if (loc.coord.Y == NavData.rd.rect.Height - 1)
				loc.coord.Y -= 1;
			Log.LogEntry("LocDecisionEngine.Run  location " + loc.coord + ":" + loc.orientation + ", head motion " + head_motion + ", Kinect " + kinect + ", LIDAR " + lidar);
			sw.Start();
			GatherContextInfo(loc,perp_wall_only,ref no_pts);
			sw.Stop();

			Log.LogEntry("Number map points checked: " + no_pts);
			Log.LogEntry("Context info:");
			for (i = 0; i < context_info.Count; i++)
				{
				li = (loc_info) context_info[i];
				line = new StringBuilder((i + 1).ToString() + " " + li.start.ToString() + " to " + li.end.ToString() + " " + li.loc_type.ToString());
				if (li.loc_type == Type.CONNECTION)
					{
					line.Append(" # " + li.index + ", distance to center " + li.distance + " in, length " + li.length + " in");
					}
				else if (li.loc_type == Type.CORNER)
					{
					line.Append(", feature # " + li.index + ", distance " + li.distance + " in, ra " + li.ra + "°");
					}
				else
					{
					line.Append(", perp. direct " + li.direct + ", cartisian distance " + li.distance + " in" + ", length " + li.length + " in");
					if (li.target)
						{
						line.Append(", with TARGET   feature # " + li.index + ", ra " + li.ra + "°");
						}
					}
				Log.LogEntry(line.ToString());
				}

			sw.Start();
			AnalyzeInfo(loc,head_motion,kinect,lidar);
			sw.Stop();

			Log.LogEntry("Possible primary actions:");
			if (ppactions.Count > 0)
				{
				for (i = 0; i < ppactions.Count; i++)
					{
					act = (action) ppactions[i];
					line = new StringBuilder((act.info_index  + 1).ToString() + ". " + act.sensor.ToString() + ", " + act.act.ToString() + ", ");
					if ((act.act == Actions.PERP_DIR) && (act.sensor == Sensor.KINECT))
						{
						line.Append("offset " + act.offset + ", head motion " + act.head_motion_required);
						}
					else if (act.act == Actions.CORNER)
						{
						li = (loc_info) context_info[act.info_index];
						line.Append("@ " + li.start);
						if (act.sensor == Sensor.KINECT)
							line.Append( ", head motion " + act.head_motion_required);
						}
					else if ((act.act == Actions.EDGE) || (act.act == Actions.TARGET))
						{
						line.Append( "@ " + act.coord);
						if (act.sensor == Sensor.KINECT)
							line.Append( ", head motion " + act.head_motion_required);
						}
					Log.LogEntry(line.ToString());
					}
				}
			else
				Log.LogEntry("None");
			Log.LogEntry("Possible secondary actions:");
			if (spactions.Count > 0)
				{
				for (i = 0; i < spactions.Count; i++)
					{
					act = (action) spactions[i];
					line = new StringBuilder((act.info_index  + 1).ToString() + ". " + act.sensor.ToString() + ", " + act.act.ToString() + ", ");
					if ((act.act == Actions.PERP_DIR) && (act.sensor == Sensor.KINECT))
						{
						line.Append( "offset " + act.offset + ", head motion " + act.head_motion_required);
						}
					else if (act.act == Actions.CORNER)
						{
						li = (loc_info) context_info[act.info_index];
						line.Append( "@ " + li.start);
						if (act.sensor == Sensor.KINECT)
							line.Append( ", head motion " + act.head_motion_required);
						}
					else if ((act.act == Actions.EDGE) || (act.act == Actions.TARGET))
						{
						line.Append( "@ " + act.coord);
						if (act.sensor == Sensor.KINECT)
							line.Append(", head motion " + act.head_motion_required);
						}
					Log.LogEntry(line.ToString());
					}
				}
			else
				Log.LogEntry("None");

			sw.Start();
			DecideOnAction(head_motion);
			if ((dactions.Count < 2) && (spactions.Count > 0))
				{
				ppactions.AddRange(spactions);
				spactions.Clear();
				DecideOnAction(head_motion);
				}
			sw.Stop();

			Log.LogEntry("Decided actions:");
			if (dactions.Count == 0)
				Log.LogEntry("None");
			else
				{
				for (i = 0; i < dactions.Count; i++)
					{
					act = (action) dactions[i];
					line = new StringBuilder((act.info_index  + 1).ToString() + ". " + act.sensor.ToString() + ", " + act.act.ToString() + ", ");
					if ((act.act == Actions.PERP_DIR) && (act.sensor == Sensor.KINECT))
						{
						line.Append("offset " + act.offset + "°, ");
						}
					else if ((act.act == Actions.CORNER) || (act.act == Actions.TARGET))
						{
						li = (loc_info) context_info[act.info_index];
						line.Append("@ " + li.start);
						}
					else if (act.act == Actions.EDGE)
						{
						line.Append("@ " + act.coord);
						}
					Log.LogEntry(line.ToString());
					}
				}
			Log.LogEntry("Run time (ms): " + sw.ElapsedMilliseconds);

			return(dactions);
		}



		public ArrayList Run(NavData.location loc,bool head_motion,bool kinect,bool lidar)

		{
			return(Run(loc,head_motion,kinect,lidar,false));
		}



		public int LocationActionCheck(NavData.location loc,bool head_motion)

		{	
			int no_pts = 0;

			if (loc.coord.X == 0)
				loc.coord.X = 1;
			else if (loc.coord.X == NavData.rd.rect.Width - 1)
				loc.coord.X -= 1;
			if (loc.coord.Y == 0)
				loc.coord.Y = 1;
			else if (loc.coord.Y == NavData.rd.rect.Height - 1)
				loc.coord.Y -= 1;
			GatherContextInfo(loc, false, ref no_pts);
			AnalyzeInfo(loc,head_motion,true,true);
			return(ppactions.Count + spactions.Count);
		}



		public loc_info GetContext(int index)

		{
			if (index < context_info.Count)
				return((loc_info) context_info[index]);
			else
				return(new loc_info());
		}



		public int GetContectCount()

		{
			return(context_info.Count);
		}



		public bool CloseOpenWall(NavData.location loc,int cdist)

		{
			int i;
			bool rtn = false;
			loc_info li;
			int wdist;

			for (i = 0;i < context_info.Count;i++)
				{
				li = (loc_info) context_info[i];
				if (li.loc_type == Type.OPEN_WALL)
					{
					if ((li.direct == 0) || (li.direct == 180))
						wdist = Math.Abs(li.start.Y - loc.coord.Y);
					else
						wdist = Math.Abs(li.start.X - loc.coord.X);
					if (cdist >= wdist)
						{
						rtn = true;
						break;
						}
					}
				}
			return(rtn);
		}



		public ArrayList ReRun(ArrayList failed_action,bool head_motion)

		{
			int i,j;
			action act,pact;
			bool used_secondary = false;
			StringBuilder line;
			Stopwatch sw = new Stopwatch();
			loc_info li;

			sw.Start();
			for (i = 0;i < failed_action.Count;i++)
				{
				act = (action) dactions[(int) failed_action[i]];
				for (j = 0;j < ppactions.Count;j++)
					{
					pact = (action) ppactions[j];
					if (act.info_index == pact.info_index)
						{
						ppactions.RemoveAt(j);				//SINCE A TARGET IS PART OF A WALL 
						Log.LogEntry("LocDecisionEngne.ReRun  failed action index " + pact.info_index + " removed.");
						break;								//REMOVING A TARGET OR ITS WALL REMOVES BOTH
						}
					}
				}
			sw.Stop();

			Log.LogEntry ("LocDecisionEngne.ReRun  failed actions " + failed_action.Count + ", head motion " + head_motion);
			Log.LogEntry("Revised possible primary actions (after failed action removal): ");
			if (ppactions.Count > 0)
				{
				for (i = 0; i < ppactions.Count; i++)
					{
					act = (action) ppactions[i];
					line = new StringBuilder((act.info_index  + 1).ToString() + ". " + act.sensor.ToString() + ", " + act.act.ToString() + ", ");
					if ((act.act == Actions.PERP_DIR) && (act.sensor == Sensor.KINECT))
						{
						line.Append("offset " + act.offset + ", head motion " + act.head_motion_required);
						}
					else if ((act.act == Actions.CORNER) || (act.act == Actions.TARGET))
						{
						li = (loc_info) context_info[act.info_index];
						line.Append("@ " + li.start);
						if (act.sensor == Sensor.KINECT)
							line.Append( ", head motion " + act.head_motion_required);
						}
					else if (act.act == Actions.EDGE)
						{
						line.Append( "@ " + act.coord);
						if (act.sensor == Sensor.KINECT)
							line.Append( ", head motion " + act.head_motion_required);
						}
					Log.LogEntry(line.ToString());
					}
				}
			else
				Log.LogEntry("None");

			sw.Start();
			DecideOnAction(head_motion);
			if ((dactions.Count < 2) && (spactions.Count > 0))
				{
				ppactions.AddRange(spactions);
				spactions.Clear();
				DecideOnAction(head_motion);
				used_secondary = true;
				}
			sw.Stop();

			if (used_secondary)
				{
				Log.LogEntry("Revised possible primary actions (after rerun):");
				if (ppactions.Count > 0)
					{
					for (i = 0; i < ppactions.Count; i++)
						{
						act = (action) ppactions[i];
						line = new StringBuilder((act.info_index  + 1).ToString() + ". " + act.sensor.ToString() + ", " + act.act.ToString() + ", ");
						if ((act.act == Actions.PERP_DIR) && (act.sensor == Sensor.KINECT))
							{
							line.Append("offset " + act.offset + ", head motion " + act.head_motion_required);
							}
						else if ((act.act == Actions.CORNER) || (act.act == Actions.TARGET))
							{
							li = (loc_info) context_info[act.info_index];
							line.Append("@ " + li.start);
							if (act.sensor == Sensor.KINECT)
								line.Append( ", head motion " + act.head_motion_required);
							}
						else if (act.act == Actions.EDGE)
							{
							line.Append( "@ " + act.coord);
							if (act.sensor == Sensor.KINECT)
								line.Append( ", head motion " + act.head_motion_required);
							}
						Log.LogEntry(line.ToString());
						}
					}
				else
					Log.LogEntry("None");
				}
			Log.LogEntry("Decided actions:");
			if (dactions.Count == 0)
				Log.LogEntry("None");
			else
				{
				for (i = 0; i < dactions.Count; i++)
					{
					act = (action) dactions[i];
					line = new StringBuilder((act.info_index  + 1).ToString() + ". " + act.sensor.ToString() + ", " + act.act.ToString() + ", ");
					if ((act.act == Actions.PERP_DIR) && (act.sensor == Sensor.KINECT))
						{
						line.Append("offset " + act.offset + "°, ");
						}
					else if ((act.act == Actions.CORNER) || (act.act == Actions.TARGET))
						{
						li = (loc_info) context_info[act.info_index];
						line.Append("@ " + li.start);
						}
					else if (act.act == Actions.EDGE)
						{
						line.Append("@ " + act.coord);
						}
					Log.LogEntry(line.ToString());
					}
				}
			Log.LogEntry("Run time (ms): " + sw.ElapsedMilliseconds);

			return(dactions);
		}


		}
	}
