using System;
using System.Collections;
using System.Drawing;
using System.IO;
using AutoRobotControl;


namespace Work_Assist
	{
	static class AAMap
		{
		//	primary assumptions:
		//		1. Same side topology
		//			a. The final positioning of the robot is at 45° to the work space forward edge
		//			b. The work space used by the robot is limited to the work top edge to 11 in deep, from the edge point perpendicular to the robot for 17 in. toward the speaker and from the work top surface up.
		//		2. Edge topology
		//			a. The final positioning of the robot is perpendicular to the work space forward edge at approximately 4 in. from the side edge
		//			b. The work space used by the robot is limited to the work top edges (front and side) to 12 in. deep and from the work top surface up.

		private const double COS_SIN_45 = .7071;

		public static short[] base_wp_map = null;
		public static short[] wp_map = null;
		public static string[] ob_check_map;
		public static int map_size;
		public static int wsmap_width;
		public static int wsmap_height;
		public static double rlimit,llimit,edist;

		private static int rlmt, llmt;


		public static bool InitWsMap()

		{
			bool rtn = false;

			if (SkillShared.wsd.arrange == SkillShared.work_space_arrange.SAME_SIDE)
				{
				wsmap_height = 11;
				wsmap_width = 18;
				edist = SkillShared.wsd.front_edge_dist * COS_SIN_45;
				if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
					{
					rlimit = wsmap_width;
					llimit = 0;
					}
				else
					{
					rlimit = 0;
					llimit = wsmap_width;
					}

				rtn = true;
				}
			else  if (SkillShared.wsd.arrange == SkillShared.work_space_arrange.EDGE)
				{
				wsmap_height = 12;
				wsmap_width = 12;
				edist = SkillShared.wsd.front_edge_dist;
				if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
					{
					rlimit = SkillShared.wsd.side_edge_dist;
					llimit = -(wsmap_width - SkillShared.wsd.side_edge_dist);
					}
				else
					{
					rlimit = wsmap_width - SkillShared.wsd.side_edge_dist;
					llimit = -SkillShared.wsd.side_edge_dist;
					}
				rtn = true;
				}
			else
				{
				Log.LogEntry("InitWsMap does not support the topology: " + SkillShared.wsd.arrange);
				}
			if (rtn)
				{
				wp_map = new short[wsmap_height * wsmap_width];
				ob_check_map = new string[wsmap_height * wsmap_width];
				map_size = wsmap_height * wsmap_width;
				rlmt = (int) Math.Floor(rlimit);
				llmt = (int) Math.Floor(llimit);
				}
			return (rtn);
		}



		private static bool WithinWorkSpace(Arm.Loc3D loc,ref SkillShared.Dpt dpt,bool log = false)

		{
			bool rtn = false;

			dpt = new SkillShared.Dpt(loc.x, loc.z);
			if (SkillShared.wsd.arrange == SkillShared.work_space_arrange.SAME_SIDE)
				{
				if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
					dpt = SkillShared.RotateDPoint(dpt, -45);
				else
					dpt = SkillShared.RotateDPoint(dpt,45);
				if ((loc.y > SkillShared.wsd.top_height + AAShare.TOP_HEIGHT_CLEAR) && (dpt.Y >= edist) && (dpt.Y < edist + wsmap_height) && (dpt.X >= llimit) && (dpt.X < rlimit))
					{
					if (log)
						Log.LogEntry("@ work space location: (" + ((int) Math.Floor(dpt.X)) + "," + ((int) Math.Floor(dpt.Y - edist)) + ")" );
					rtn = true;
					}
				}
			else if (SkillShared.wsd.arrange == SkillShared.work_space_arrange.EDGE)
				{
				if ((loc.y > SkillShared.wsd.top_height + AAShare.TOP_HEIGHT_CLEAR) && (dpt.Y >= edist) && (dpt.Y < edist + wsmap_height) && (dpt.X >= llimit) && (dpt.X <= rlimit))
					{
					if (log)
						Log.LogEntry("@ work space location: (" + ((int)Math.Floor(dpt.X)) + "," + ((int)Math.Floor(dpt.Y - edist)) + ")");
					rtn = true;
					}
				}
			else
				Log.LogEntry("InitWsMap does not support the topology: " + SkillShared.wsd.arrange);
			return(rtn);
		}


		public static bool WithinWorkSpace(Arm.Loc3D loc,bool speak = false)

		{
			bool rtn = false;
			SkillShared.Dpt dpt = new Work_Assist.SkillShared.Dpt();

			rtn = WithinWorkSpace(loc,ref dpt);
			if (!rtn && speak)
				Speech.SpeakAsync("Position would be outside of my work space.");
			return (rtn);
		}



		private static bool WithinWorkSpaceXZ(Arm.Loc3D loc,ref SkillShared.Dpt dpt,bool log = false)

		{
			bool rtn = false;

			dpt = new SkillShared.Dpt(loc.x, loc.z);
			if (SkillShared.wsd.arrange == SkillShared.work_space_arrange.SAME_SIDE)
				{
				if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
					dpt = SkillShared.RotateDPoint(dpt, -45);
				else
					dpt = SkillShared.RotateDPoint(dpt, 45);
				if ((dpt.Y >= edist) && (dpt.Y < edist + wsmap_height) && (dpt.X >= llimit) && (dpt.X < rlimit))
					{
					if (log)
						Log.LogEntry("@ work space location: (" + ((int)Math.Floor(dpt.X)) + "," + ((int)Math.Floor(dpt.Y - edist)) + ")");
					rtn = true;
					}
				}
			else if (SkillShared.wsd.arrange == SkillShared.work_space_arrange.EDGE)
				{
				if ((dpt.Y >= edist) && (dpt.Y < edist + wsmap_height) && (dpt.X >= llimit) && (dpt.X <= rlimit))
					{
					if (log)
						Log.LogEntry("@ work space location: (" + ((int)Math.Floor(dpt.X)) + "," + ((int)Math.Floor(dpt.Y - edist)) + ")");
					rtn = true;
					}
				}
			else
				Log.LogEntry("InitWsMap does not support the topology: " + SkillShared.wsd.arrange);
			return (rtn);
		}



		public static bool WithinWorkSpaceXZ(Arm.Loc3D loc,bool log = false)

		{
			SkillShared.Dpt dpt = new Work_Assist.SkillShared.Dpt();

			return(WithinWorkSpaceXZ(loc,ref dpt,log));
		}



		public static bool MapWorkPlace()

		{
			double h;
			SkillShared.Dpt pt = new SkillShared.Dpt();
			int row = 0, col = 0,hi,lo = 0,i,pixels = 0,updates = 0,maxh = 0;
			Arm.Loc3D loc;
			Point ipt = new Point();
			bool rtn = true;

			for (i = 0; i < wp_map.Length; i++)
				wp_map[i] = -99;

			try
			{
			for (row = 0; row < Kinect.nui.ColorStream.FrameHeight; row++)
				for (col = 0; col < Kinect.nui.ColorStream.FrameWidth; col++)
					{
					loc = SkillShared.DPtLocation(row, col,AAShare.ARM_KINECT_TILT_CORRECT);
					if (loc.y > SkillShared.wsd.top_height - SkillShared.TOP_MAGRIN)
						{
						if (WithinWorkSpaceXZ(loc,ref pt))
							{
							pixels += 1;
							ipt.Y = (int) Math.Floor(pt.Y - edist);
							ipt.X = (int) Math.Floor(pt.X);
							h = loc.y - SkillShared.wsd.top_height;
							if (h > 1)
								hi = (int) Math.Ceiling(h);
							else
								hi = (int) Math.Round(h);
							if ((ipt.X >= llmt) && (ipt.X < rlmt) && MapArrayLocation(ipt,ref lo) && (lo >= 0) && (lo < wp_map.Length) && (hi > wp_map[lo]))
								{
								wp_map[lo] = (short) hi;
								if (hi > maxh)
									maxh = hi;
								updates += 1;
								}
							}
						}
					}
			}

			catch(Exception ex)
			{
			Log.LogEntry("MapWorkPlace execption: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			Log.LogEntry("Row " + row + "  Col " + col + "  Point " + ipt);
			rtn = false;
			}

			Log.LogEntry("MapWorkPlace:  pixels - " + pixels + "  updates - " + updates + "  max height - " + maxh);
			return(rtn);
		}



		public static string SaveMap(string name)

		{
			string fname;
			DateTime now = DateTime.Now;
			TextWriter tw;
			int row,col,loc,i;

			fname = Log.LogDir() + name + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + "-" + SharedData.GetUFileNo() + ".csv";
			tw = File.CreateText(fname);
			if (tw != null)
				{
				tw.WriteLine(name);
				tw.WriteLine(now.ToShortDateString() + "  "  + now.ToShortTimeString());
				tw.WriteLine();
				for (i = 0;i < wsmap_width;i++)
					tw.Write("," + i);
				tw.WriteLine();
				loc = 0;
				for (row = 0;row < wsmap_height;row++)
					{
					for (col = 0;col < wsmap_width;col++)
						{
						if (col == 0)
							tw.Write((wsmap_height - row -1) + "," + wp_map[loc]);
						else
							tw.Write("," + wp_map[loc]);
						loc += 1;
						}
					tw.WriteLine();
					}
				tw.Close();
				Log.LogEntry("Saved: " + fname);
				}
			else
				fname = "";
			return (fname);
		}



		public static bool SaveCheckMap(string name,String lines)

		{
			bool rtn = false;
			TextWriter tw;
			int row, col, loc, i;
			string fname;
			DateTime now = DateTime.Now;

			fname = Log.LogDir() + name + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".csv";
			tw = File.CreateText(fname);
			if (tw != null)
				{
				tw.WriteLine(fname);
				tw.WriteLine(now.ToShortDateString() + "  " + now.ToShortTimeString());
				tw.WriteLine();
				tw.WriteLine(lines);
				tw.WriteLine();
				for (i = 0; i < wsmap_width; i++)
					tw.Write("," + i);
				tw.WriteLine();
				loc = 0;
				for (row = 0; row < wsmap_height; row++)
					{
					for (col = 0; col < wsmap_width; col++)
						{
						if (col == 0)
							tw.Write((wsmap_height - row - 1) + "," + ob_check_map[loc]);
						else
							tw.Write("," + ob_check_map[loc]);
						loc += 1;
						}
					tw.WriteLine();
					}
				tw.Close();
				Log.LogEntry("Saved: " + fname);
				rtn = true;
				}
			return (rtn);
		}



		private static ArrayList DetermineSteps(Point sp,Point ep)

		{
			ArrayList steps = new ArrayList();
			int dx,dy,minc,i,count,loc = 0;
			bool xmain;
			Point step;
			double oinc;

			dx = ep.X - sp.X;
			dy = ep.Y - sp.Y;
			if (Math.Abs(dx) >= Math.Abs(dy))
				{
				xmain = true;
				count = Math.Abs(dx);
				oinc = Math.Abs((double) dy/dx);
				if (dy < 0)
					oinc *= -1;
				if (dx > 0)
					minc = 1;
				else
					minc = -1;
				}
			else
				{
				xmain = false;
				count = Math.Abs(dy);
				oinc = Math.Abs((double) dx/dy);
				if (dx < 0)
					oinc *= -1;
				if (dy > 0)
					minc = 1;
				else
					minc = -1;
				}
			step = sp;
			for (i = 0;i < count;i++)
				{
				if (xmain)
					{
					step.X += minc;
					step.Y = sp.Y + (int) Math.Round((i + 1) * oinc);
					}
				else
					{
					step.Y += minc;
					step.X = sp.X + (int) Math.Round((i + 1) * oinc);
					}
				if (MapArrayLoc(step,ref loc))
					{
					steps.Add(step);
					ob_check_map[loc] += "e";
					}
				}
			return (steps);
		}



		private static bool MapCheck(int loc,double sh,double eh)

		{
			bool rtn = true;
			double hi;

			if ((loc >= 0) && (loc < ob_check_map.Length))
				{
				ob_check_map[loc] += " c";
				hi = wp_map[loc] + SkillShared.wsd.top_height + Arm.OBS_HEIGHT_CLEAR;
				if ((hi > sh) || (hi > eh))
					{
					rtn = false;
					Log.LogEntry("Obstacle found with height of " + hi + "  min arm height of " + Math.Min(sh, eh).ToString("F3"));
					ob_check_map[loc] += "O";
					}
				}
			else
				Log.LogEntry("Index outside of map: " + loc);
			return(rtn);
		}



		private static bool MapArrayLoc(Point pt,ref int loc)	//pt is map index set, loc is map array index

		{
			bool rtn = false;

			loc = ((wsmap_height - pt.Y - 1) * wsmap_width) + pt.X;
			if ((loc >= 0) && (loc < map_size))
				rtn = true;
			return (rtn);
		}



		private static bool MapArrayLocation(Point pt,ref int loc)	//pt is a work space coord, loc is a map array index

		{
			bool rtn = false;

			if ((SkillShared.wsd.arrange != SkillShared.work_space_arrange.OPPOSITE_SIDE) && (SkillShared.wsd.arrange != SkillShared.work_space_arrange.NONE))
				{
				if (SkillShared.wsd.arrange == SkillShared.work_space_arrange.SAME_SIDE)
					{
					if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
						loc = ((wsmap_height - pt.Y - 1) * wsmap_width) + pt.X;
					else
						loc = ((wsmap_height - pt.Y - 1) * wsmap_width) + pt.X + wsmap_width;
					if ((loc > 0) && (loc < map_size))
						rtn = true;
					}
				else
					{
					if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
						loc = ((wsmap_height - pt.Y - 1) * wsmap_width) + (int) Math.Round(wsmap_width - SkillShared.wsd.side_edge_dist + pt.X);
					else
						loc = ((wsmap_height - pt.Y - 1) * wsmap_width) + (int) Math.Round(SkillShared.wsd.side_edge_dist + pt.X);
					if ((loc > 0) && (loc < map_size))
						rtn = true;
					}
				}
			else
				Log.LogEntry("MapArrayLocation does not support topology " + SkillShared.wsd.arrange);
			return (rtn);
		}



		private static bool DetermineCheckPts(Point ap,Point stp,double sh,double eh)

		{
			int dx,dy,minc,count,dist,loc = 0,i,th;
			bool xmain;
			Point cp,cp2 = new Point();
			double oinc;
			bool rtn = true;

			dx = stp.X - ap.X;
			dy = stp.Y - ap.Y;
			dist = Math.Abs(ap.Y);
			th = (int)(Math.Round(SkillShared.wsd.top_height));
			if (Math.Abs(dx) >= Math.Abs(dy))
				{
				xmain = true;
				count = Math.Abs(dx);
				oinc = (double) dy/dx;
				if (dx > 0)
					minc = 1;
				else
					minc = -1;
				}
			else
				{
				xmain = false;
				count = Math.Abs(dy);
				oinc = (double) dx/dy;
				if (dy > 0)
					minc = 1;
				else
					minc = -1;
				}
			cp = ap;
			for (i = dist;i < count + 1;i++)
				{
				if (xmain)
					{
					cp.X = ap.X + (i * minc);
					cp.Y = ap.Y + (int) Math.Round(i * oinc);
					}
				else
					{
					cp.Y = ap.Y + (i * minc);
					cp.X = ap.X + (int) Math.Round(i * oinc);
					}
				if (!MapArrayLoc(cp,ref loc) || !MapCheck(loc,sh,eh))
					{
					rtn = false;
					break;
					}
				if (Math.Abs(dx) < Math.Abs(dy))
					{
					cp2.Y = cp.Y;
					cp2.X = cp.X + 1;
					if (!(rtn = MapArrayLoc(cp2, ref loc)) || !(rtn = MapCheck(loc, sh, eh)))
						break;
					cp2.X = cp.X - 1;
					if (!(rtn = MapArrayLoc(cp2, ref loc)) || !(rtn = MapCheck(loc, sh, eh)))
						break;
					}
				else
					{
					cp2.X = cp.X;
					cp2.Y = cp.Y + 1;
					if (!(rtn = MapArrayLoc(cp2, ref loc)) || !(rtn = MapCheck(loc, sh, eh)))
						break;
					cp2.Y = cp.Y - 1;
					if (!(rtn = MapArrayLoc(cp2, ref loc)) || !(rtn = MapCheck(loc, sh, eh)))
						break;
					}
				}
			return (rtn);
		}



		public static bool ObstacleCheck(Arm.Loc3D loc1, Arm.Loc3D loc2) //loc1 and loc2 are robot coordinates

		{	//ASSUMES THAT THE GRIPPER IS AT THE LOWEST HEIGHT OF THE ARM
			bool rtn = true;
			SkillShared.Dpt spt,ept;
			Point sp,ep,ap;
			double ih,sh,dh,eh,mdist;
			ArrayList steps = new ArrayList();
			int i,loc = -1;
			string lines;

			if ((SkillShared.wsd.arrange != SkillShared.work_space_arrange.OPPOSITE_SIDE) && (SkillShared.wsd.arrange != SkillShared.work_space_arrange.NONE))
				{
				try
				{
				Log.LogEntry("Arm obstacle check");
				for (i = 0; i < AAMap.ob_check_map.Length; i++)
					ob_check_map[i] = "";
				spt = new SkillShared.Dpt(loc1.x, loc1.z);
				ept = new SkillShared.Dpt(loc2.x, loc2.z);
				if (SkillShared.wsd.arrange == SkillShared.work_space_arrange.SAME_SIDE)
					{
					if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
						{
						spt = SkillShared.RotateDPoint(spt, -45);
						ept = SkillShared.RotateDPoint(ept, -45);
						ap = new Point(0, (int)Math.Floor(-edist));
						sp = new Point((int) Math.Floor(spt.X),(int) Math.Floor(spt.Y - edist));
						ep = new Point((int)Math.Floor(ept.X), (int)Math.Floor(ept.Y - edist));
						MapArrayLoc(sp,ref loc);
						}
					else
						{
						spt = SkillShared.RotateDPoint(spt, 45);
						ept = SkillShared.RotateDPoint(ept, 45);
						ap = new Point(wsmap_width, (int)Math.Floor(-edist));
						sp = new Point((int) Math.Floor(wsmap_width + spt.X),(int) Math.Floor(spt.Y - edist));
						ep = new Point((int)Math.Floor(wsmap_width + ept.X), (int)Math.Floor(ept.Y - AAMap.edist));
						MapArrayLoc(sp,ref loc);
						}
					}
				else
					{
					if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
						{
						ap = new Point((int) Math.Floor(wsmap_width - SkillShared.wsd.side_edge_dist), (int)Math.Floor(-edist));
						sp = new Point((int) Math.Floor(wsmap_width - SkillShared.wsd.side_edge_dist + spt.X),(int) Math.Floor(spt.Y - edist));
						ep = new Point((int)Math.Floor(wsmap_width - SkillShared.wsd.side_edge_dist + ept.X), (int)Math.Floor(ept.Y - edist));
						MapArrayLoc(sp,ref loc);
						}
					else
						{
						ap = new Point((int) Math.Floor(SkillShared.wsd.side_edge_dist), (int)Math.Floor(-edist));
						sp = new Point((int) Math.Floor(SkillShared.wsd.side_edge_dist + spt.X),(int) Math.Floor(spt.Y - edist));
						ep = new Point((int)Math.Floor(SkillShared.wsd.side_edge_dist + ept.X), (int)Math.Floor(ept.Y - edist));
						MapArrayLoc(sp,ref loc);
						}
					}
				Log.LogEntry("Start map point " + sp + ", end map point " + ep);
				if ((loc >= 0) && (loc < ob_check_map.Length))
					ob_check_map[loc] = "start";
				ih = sh = loc1.y;
				mdist = NavCompute.DistancePtToPt(sp,ep);
				dh = (loc2.y - loc1.y) /mdist;
				if ((wp_map != null) && WithinWorkSpaceXZ(loc2))
					{
					steps = DetermineSteps(sp, ep);
					for (i = 0; i < steps.Count; i++)
						{
						eh = ih + ((i + 1) * dh);
						rtn = DetermineCheckPts(ap,(Point) steps[i],sh,eh);
						if (!rtn)
							break;
						sh = eh;
						}
					lines = "Start point: " + sp.X + " " + sp.Y + "\r\nEnd point: " + ep.X + " " + ep.Y + "\r\nARM point: " + ap.X + " " + ap.Y;
					if (SharedData.log_operations)
						SaveCheckMap("Arm check map ",lines);
					}
				}

				catch(Exception ex)
				{
				rtn = false;
				Log.LogEntry("ObstacleCheck excetion: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				}
			else
				Log.LogEntry("ObstacleCheck does not support topology " + SkillShared.wsd.arrange);
			return (rtn);
		}



		public static bool ArmAngle(Arm.Loc3D ploc,ref double aa)

		{
			bool rtn = false;
			Point ap,ep;
			SkillShared.Dpt ept;

			ept = new SkillShared.Dpt(ploc.x, ploc.z);
			if (SkillShared.wsd.arrange == SkillShared.work_space_arrange.SAME_SIDE)
				{
				if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
					{
					ept = SkillShared.RotateDPoint(ept, -45);
					ap = new Point(0, (int)Math.Floor(-edist));
					ep = new Point((int)Math.Floor(ept.X), (int)Math.Floor(ept.Y - edist));
					}
				else
					{
					ept = SkillShared.RotateDPoint(ept, 45);
					ap = new Point(wsmap_width, (int)Math.Floor(-edist));
					ep = new Point((int)Math.Floor(wsmap_width + ept.X), (int)Math.Floor(ept.Y - AAMap.edist));
					}
				rtn = true;
				aa = Math.Tan((ep.X - ap.X)/(ep.Y - ap.Y)) * SharedData.RAD_TO_DEG;
				}
			else
				{
				if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
					{
					ap = new Point((int)Math.Floor(wsmap_width - SkillShared.wsd.side_edge_dist), (int)Math.Floor(-edist));
					ep = new Point((int)Math.Floor(wsmap_width - SkillShared.wsd.side_edge_dist + ept.X), (int)Math.Floor(ept.Y - edist));
					}
				else
					{
					ap = new Point((int)Math.Floor(SkillShared.wsd.side_edge_dist), (int)Math.Floor(-edist));
					ep = new Point((int)Math.Floor(SkillShared.wsd.side_edge_dist + ept.X), (int)Math.Floor(ept.Y - edist));
					}
				rtn = true;
				aa = Math.Tan((ep.X - ap.X) / (ep.Y - ap.Y)) * SharedData.RAD_TO_DEG;
				}
			return (rtn);
		}



		private static Point MapArrayPt(SkillShared.Dpt pt)

		{
			Point mapt = new Point();

			if ((SkillShared.wsd.arrange == SkillShared.work_space_arrange.SAME_SIDE) && (SkillShared.wsd.side == SharedData.RobotLocation.LEFT))
				mapt.X = (int) Math.Floor(pt.X);
			else if ((SkillShared.wsd.arrange == SkillShared.work_space_arrange.EDGE) && (SkillShared.wsd.side == SharedData.RobotLocation.LEFT))
				mapt.X = (int) Math.Floor(pt.X + wsmap_width - rlimit);
			mapt.Y = (int) Math.Floor(pt.Y - edist);
			return(mapt);
		}



		public static void CorrectMoveMap(Arm.Loc3D loc)	//loc is robot coordinates
																			//assumes that arm has ~ 5 in. wide impact on map worst case
		{
			SkillShared.Dpt dpt;
			Point cpt;
			int i,j,mloc = 0;	
			double r,dy,dx,dy2,dx2;
			bool search_complete = false;
			const int SEARCH_WIDTH = 7;
			const double SEARCH_UNIT = .35;

			Log.LogEntry("CorrectMoveMap");
			dpt = new SkillShared.Dpt(loc.x, loc.z);
			if (SkillShared.wsd.arrange == SkillShared.work_space_arrange.SAME_SIDE)
				{
				if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
					dpt = SkillShared.RotateDPoint(dpt, -45);
				else
					dpt = SkillShared.RotateDPoint(dpt, 45);
				}
			if ((base_wp_map != null) && (AAShare.arm_pos == AAShare.position.IN_WS))
				{
				Log.LogEntry("Initial map pt: " + dpt);
				r = Math.Atan(dpt.X / dpt.Y);
				dy = dx2 = SEARCH_UNIT * Math.Cos(r);
				dx = dy2 = -SEARCH_UNIT * Math.Sin(r);
				Log.LogEntry("Increments:  dy = " + dy.ToString("F2") + "   dx = " + dx.ToString("F2"));
				cpt = MapArrayPt(dpt);
				Log.LogEntry("Initial map indexes: " + cpt.X + "," + cpt.Y );
				for (i = 0;!search_complete;i++)	//remove arm
					{
					if (i > 100)
						{
						Log.LogEntry("Run away map correct. i = " + i + "  cpt = " + cpt);
						break;
						}
					for (j = -SEARCH_WIDTH;j <= SEARCH_WIDTH;j++)
						{
						cpt.X = (int) Math.Floor(dpt.X + (i * dx) + (j * dx2));
						cpt.Y = (int) Math.Floor(dpt.Y - (i * dy) + (j * dy2) - edist);
						if ((cpt.X >= llmt) && (cpt.X < rlmt) && MapArrayLocation(cpt,ref mloc) && (mloc < map_size) && (mloc >= 0))
							{
//							Log.LogEntry("Correct @ " + cpt);
							wp_map[mloc] = base_wp_map[mloc];
							}
						if ((r >= 0) && (j == -SEARCH_WIDTH) && (cpt.Y == 0))
							search_complete = true;
						else if ((r < 0) && (j == SEARCH_WIDTH) && (cpt.Y == 0))
							search_complete = true;
						}
					}
				for (i = 0; i < wp_map.Length; i++) //remove "shadows"
					{
					if (wp_map[i] == -99)
						wp_map[i] = base_wp_map[i];
					}
				if (SharedData.log_operations)
					SaveMap("Corrected map ");
				}
			else
				{
				if (base_wp_map == null)
					Log.LogEntry("No base map exists.");
				else
					Log.LogEntry("Arm is not in workspace.");
				}
		}


		}
	}
