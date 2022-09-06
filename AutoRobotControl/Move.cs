using System;
using System.Drawing;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using MapAStar;
using Microsoft.Kinect;
using MotionControl;
using Coding4Fun.Kinect.WinForm;


namespace AutoRobotControl
	{
	public class Move
		{

		public const int MAX_MOVE_SEG_DIST = 120;
		public enum MoveToPointResult { TRUE, FALSE, MAP_INSERTION };

		private enum MotionType { NONE, X, Y };
		private enum MoveDirect {NONE,FORWARD,BACKWARD};
		private const int MIN_MOVE_DIST = 6;
		private const int MAX_FINAL_DISTANCE = 12;
		private const int COLLISION_CHECK_DIST = 22;
		private const int MIN_PT_DISTANCE = 20;
		private const int START_PT_DIST_LIMIT = 5;
		private const int MIN_SIDE_CLEAR = 1;



		private ObstacleAdjust oa = new ObstacleAdjust();

		private NavData.location clocation;
		private bool insufficent_clearence = false;
		private AutoResetEvent frame_complete = new AutoResetEvent(false);
		private SensorFusion sf = new SensorFusion();
		private string last_error = "";
		private bool run_monitor = false;


		private bool SendCommand(string command,int timeout_count)

		{
			string rsp = "";
			bool rtn;

			Log.LogEntry(command);
			last_error = "";
			insufficent_clearence = false;
			if (timeout_count < 20)
				timeout_count = 20;
			rsp = MotionControl.SendCommand(command,timeout_count);
			Log.LogEntry(rsp);
			if (rsp.Contains("fail"))
				{
				if (rsp.Contains(SharedData.INSUFFICENT_FRONT_CLEARANCE))
					insufficent_clearence = true;
				if (rsp.Contains(SharedData.INSUFFICENT_REAR_CLEARANCE))
					insufficent_clearence = true;
				last_error = rsp.Substring(5);
				rtn = false;
				}
			else
				rtn = true;
			return(rtn);
		}



		private string SendCommand(string command)

		{
			string rsp = "";

			Log.LogEntry(command);
			rsp = MotionControl.SendCommand(command,200);
			Log.LogEntry(rsp);
			return(rsp);
		}



		private void MultiOutput(string msg)

		{
			Speech.SpeakAsync(msg);
			Log.KeyLogEntry(msg);
		}


		
		private Point ConvertMoveToDetail(Point mpt)

		{
			Point dpt = new Point();

			dpt.X = mpt.X * Room.MAP_SHRINK_FACTOR + (Room.MAP_SHRINK_FACTOR / 2);
			dpt.Y = mpt.Y * Room.MAP_SHRINK_FACTOR + (Room.MAP_SHRINK_FACTOR / 2);
			return(dpt);
		}







		private bool DetermineIntermediatePts(Point ept,Point spt,ref ArrayList al,byte[,] move_map,bool return_closest_clear_pt = false)

		{
			bool rtn = false;
			PathFinderFast pff;
			MotionType last_mt = MotionType.NONE;
			Point last,end,start;
			Point mp, pt1, pt2, pt3,rtn_pt = new Point();
			List<PathFinderFast.PathFinderNode> path = null;
			Room rm = SharedData.current_rm;
			int mdist, direct, i;
			NavCompute.pt_to_pt_data ppd,ppd2;
			ArrayList ptp = new ArrayList();
			bool ur_error = false;
			ArrayList obs = new ArrayList();
			MapCompute.obs_correct obc;
			Bitmap rmap;
			Graphics g;
			string fname;

			try
			{
			Log.LogEntry("DetermineIntermediatePts: (" + ept.X + "," + ept.Y + ")  (" + spt.X + "," + spt.Y + ")  " + al.Count + "  " + move_map.Length );
			rmap = rm.RmMapClone();
			g = Graphics.FromImage(rmap);
			end = new Point();
			end.X = ept.X / Room.MAP_SHRINK_FACTOR;
			end.Y = ept.Y / Room.MAP_SHRINK_FACTOR;
			start = new Point();
			start.X = spt.X / Room.MAP_SHRINK_FACTOR;
			start.Y = spt.Y / Room.MAP_SHRINK_FACTOR;
			pff = new PathFinderFast(move_map);
			if (pff != null)
				{
				pff.Diagonals = false;
				pff.Formula = PathFinderFast.HeuristicFormula.Euclidean;
				pff.PunishChangeDirection = true;
				path = pff.FindPath(start, end);
				if ((path != null) && (path.Count > 2))
					{
					path.Reverse();
					last = new Point();
					ptp.Add(spt);
					foreach (PathFinderFast.PathFinderNode node in path)
						{
						Point pt = new Point(node.X, node.Y);
						mp = ConvertMoveToDetail(pt);
						g.FillRectangle(Brushes.DeepPink, mp.X, mp.Y, 1, 1);
						if (!pt.Equals(start))
							{
							if (last.X == pt.X)
								{
								if (last_mt == MotionType.NONE)
									{
									last_mt = MotionType.X;
									}
								else if (last_mt == MotionType.Y)
									{
									mp = ConvertMoveToDetail(last);
									last_mt = MotionType.X;
									ptp.Add(mp);
									}
								}
							else if (last.Y == pt.Y)
								{
								if (last_mt == MotionType.NONE)
									{
									last_mt = MotionType.Y;
									}
								else if (last_mt == MotionType.X)
									{
									mp = ConvertMoveToDetail(last);
									last_mt = MotionType.Y;
									ptp.Add(mp);
									}
								}
							}
						last = pt;
						}
					if (ptp.Count > 1)
						{
						mp = ConvertMoveToDetail(end);
						if (NavCompute.DistancePtToPt(mp, ept) >= START_PT_DIST_LIMIT)
							{
							pt1 = (Point)ptp[ptp.Count - 1];
							ppd = NavCompute.DetermineRaDirectDistPtToPt(ept, pt1);
							mdist = MapCompute.FindMapObstacle(NavData.detail_map, pt1, ppd.direc, ppd.dist, SharedData.FRONT_SONAR_CLEARANCE, 2);
							if ((mdist != -1) && (mdist < ppd.dist))
								{
								ptp.Add(mp);
								}
							}
						}
					ptp.Add(ept);
					if (ptp.Count > 2)
						{
						if (ptp.Count > 3)
							{
							pt1 = (Point)ptp[0];
							pt2 = (Point)ptp[1];
							pt3 = (Point)ptp[2];

							if (NavCompute.DistancePtToPt(pt1, pt2) < MIN_PT_DISTANCE)
								{
								ppd = NavCompute.DetermineRaDirectDistPtToPt(pt3, pt1);
								mdist = MapCompute.FindMapObstacle(NavData.detail_map, pt1, ppd.direc, ppd.dist, SharedData.FRONT_SONAR_CLEARANCE, 2);
								if (mdist == -1)
									ptp.RemoveAt(1);
								else if (mdist >= ppd.dist)
									{
									direct = (ppd.direc + 180) % 360;
									pt3 = NavCompute.MapPoint(new Point(0, SharedData.FRONT_SONAR_CLEARANCE - (mdist - ppd.dist)), direct, pt3, true);
									ptp[2] = pt3;
									ptp.RemoveAt(1);
									}
								}
							}
						if (ptp.Count > 3)
							{
							pt1 = (Point) ptp[ptp.Count - 3];
							pt2 = (Point) ptp[ptp.Count - 2];
							pt3 = (Point) ptp[ptp.Count - 1];

							ppd = NavCompute.DetermineRaDirectDistPtToPt(pt2,pt1);
							ppd2 = NavCompute.DetermineRaDirectDistPtToPt(pt3,pt2);
							if (NavCompute.AngularDistance(ppd.direc,ppd2.direc) > 45)	// is 45 degree a good threshold???
								{
								int dy,dx;
								Point mpt1;
								
								dy = Math.Abs(pt3.Y - pt1.Y);
								dx = Math.Abs(pt3.X - pt1.X);
								if (dy > dx)
									mpt1 = new Point(pt3.X,pt1.Y);
								else
									mpt1 = new Point(pt1.X,pt3.Y);
								if (Navigate.PathClear(pt3,mpt1))
									{
									ptp[ptp.Count - 3] = mpt1;
									ptp.RemoveAt(ptp.Count - 2);
									}
								}
							}
						for (i = 0; i < ptp.Count - 1; i++)
							{
							pt1 = (Point)ptp[i];
							pt2 = (Point)ptp[i + 1];
							ppd = NavCompute.DetermineRaDirectDistPtToPt(pt2, pt1);
							mdist = MapCompute.FindMapObstacle(NavData.detail_map, pt1, ppd.direc, ppd.dist, SharedData.FRONT_SONAR_CLEARANCE, 2);
							if (mdist != -1)
								{
								if (mdist >= ppd.dist)
									{
									direct = (ppd.direc + 180) % 360;
									pt2 = NavCompute.MapPoint(new Point(0, SharedData.FRONT_SONAR_CLEARANCE - (mdist - ppd.dist)), direct, pt2, true);
									ptp[i + 1] = pt2;
									}
								else
									{
									obs.Clear();
									MapCompute.MapMapObstacles(NavData.detail_map, pt1, ppd.direc, ppd.dist, 2, ref obs);
									obc = MapCompute.AnalyizeMapObstacles(pt1, ppd.direc, obs, 2);
									if (obc.correctable)
										{
										pt1 = new Point((int)Math.Round(pt1.X + (obc.dist * Math.Sin(obc.direct * SharedData.DEG_TO_RAD))), (int)Math.Round(pt1.Y - (obc.dist * Math.Cos(obc.direct * SharedData.DEG_TO_RAD))));
										pt2 = new Point((int)Math.Round(pt2.X + (obc.dist * Math.Sin(obc.direct * SharedData.DEG_TO_RAD))), (int)Math.Round(pt2.Y - (obc.dist * Math.Cos(obc.direct * SharedData.DEG_TO_RAD))));
										ppd = NavCompute.DetermineRaDirectDistPtToPt(pt2, pt1);
										mdist = MapCompute.FindMapObstacle(NavData.detail_map, pt1, ppd.direc, ppd.dist, SharedData.FRONT_SONAR_CLEARANCE, 2);
										if (mdist == -1)
											{
											ptp[i + 1] = pt2;
											ptp[i] = pt1;
											}
										else if (mdist >= ppd.dist)
											{
											direct = (ppd.direc + 180) % 360;
											pt2 = NavCompute.MapPoint(new Point(0, SharedData.FRONT_SONAR_CLEARANCE - (mdist - ppd.dist)), direct, pt2, true);
											ptp[i + 1] = pt2;
											ptp[i] = pt1;
											}
										else
											{
											ur_error = true;
											Log.LogEntry("Could not correct for 'side obstacle' for " + pt1 + " to " + pt2);
											if (return_closest_clear_pt)
												rtn_pt = pt1;
											break;
											}
										}
									else
										{
										ur_error = true;
										Log.LogEntry("Could not correct for 'side obstacle' for " + pt1 + " to" + pt2);
										if (return_closest_clear_pt)
											rtn_pt = pt1;
										break;
										}
									}
								}
							}
						if (!ur_error)
							{
							ptp.RemoveAt(0);
							rtn = true;
							foreach (Point pt in ptp)
								g.FillEllipse(Brushes.Blue, pt.X - 2, pt.Y - 2, 4, 4);
							if (return_closest_clear_pt)
								{
								ptp.Clear();
								ptp.Add(ept);
								}
							al = (ArrayList) ptp.Clone();
							}
						else if (return_closest_clear_pt)
							{
							ptp.Clear();
							ptp.Add(rtn_pt);
							al = (ArrayList) ptp.Clone();
							}
						else
							{
							if (i > 1)
								ptp.RemoveRange(i + 1,ptp.Count - (i + 1));
							ptp.RemoveAt(0);
							al = (ArrayList) ptp.Clone();
							}
						}
					else
						{
						ptp.RemoveAt(0);
						rtn = true;
						foreach (Point pt in ptp)
							g.FillEllipse(Brushes.Blue, pt.X - 2, pt.Y - 2, 4, 4);
						if (return_closest_clear_pt)
							{
							ptp.Clear();
							ptp.Add(ept);
							}
						al = (ArrayList) ptp.Clone();
						}
					}
				else
					Log.LogEntry("No path found.");
				}
			else
				Log.LogEntry("Could not initialize fast path finder.");
			if (SharedData.log_operations)
				{
				fname = Log.LogDir() + NavData.rd.name + " room map " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				rmap.Save(fname);
				Log.LogEntry("Saved " + fname );
				}
			}

			catch (Exception ex)
			{
			Log.KeyLogEntry("DetermineIntermediatePts exception: " + ex.Message);
			Log.LogEntry("stack trace: " + ex.StackTrace);
			Log.LogEntry("mapped points array list size: " + al.Count);
			if (path == null)
				Log.LogEntry("path list size: null");
			else
				Log.LogEntry("path list size: " + path.Count);
			rtn = false;
			al.Clear();
			}

			return(rtn);
		}



		public bool DetermineIntermediatePts(Point ept,Point spt,ref ArrayList al)

		{
			return(DetermineIntermediatePts(ept,spt,ref al,NavData.move_map));
		}



		public bool ReturnClosestClearPtinPath(Point ept,Point spt,ref Point rpt)

		{
			ArrayList al = new ArrayList();
			bool rtn = false;

			DetermineIntermediatePts(ept, spt, ref al, NavData.move_map,true);
			if(al.Count > 0)
				{
				rtn = true;
				rpt = (Point) al[0];
				}
			return(rtn);
		}



		public SharedData.MotionErrorType MotionErrorType(string error)

		{
			SharedData.MotionErrorType et;

			if (error.Contains(SharedData.MPU_FAIL))
				et = SharedData.MotionErrorType.MPU;
			else if (error.Contains(SharedData.START_TIMEOUT))
				et = SharedData.MotionErrorType.START_TIMEOUT;
			else if (error.Contains(SharedData.STOP_TIMEOUT))
				et = SharedData.MotionErrorType.STOP_TIMEOUT;
			else if (error.Contains(SharedData.UDP_TIMEOUT))
				et = SharedData.MotionErrorType.UDP_TIMEOUT;
			else
				et = SharedData.MotionErrorType.INIT_FAIL;
			return(et);
		}



		private bool RecoverUnsafeTurn(Point mp,int mod,int angle,int modd)

		{
			bool rtn = false,turned = false;
			int ta1,ta2,nmod = 0,nmodd = 0,tangle = 0,direc,bdist,sdist,value,ldist;
			string cmd,rsp;
			double cdist;
			Room rm = SharedData.current_rm;

			ta1 = angle + 180;
			if (ta1 > 180)
				ta1 -= 360;
			ta2 = -modd;
			if (ta2 < -180)
				ta2 += 360;
			if (clocation.loc_name == SharedData.RECHARGE_LOC_NAME)
				{
				turned = true;
				tangle = 0;
				}
			else if ((ta1 == 0) || Turn.TurnSafe(ta1,ref nmod,ref nmodd))
				{
				if (ta1 == 0)
					{
					turned = true;
					tangle = 0;
					}
				else if ((turned = Turn.TurnAngle(ta1)))
					{
					direc = (clocation.orientation - ta1) % 360;
					if (direc < 0)
						direc += 360;
					clocation.orientation = direc;
					clocation.ls = NavData.LocationStatus.DR;
					clocation.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(ta1),clocation.orientation, clocation.coord);
					NavData.SetCurrentLocation(clocation);
					tangle = ta1;
					}
				}
			else if ((ta2 == 0) || Turn.TurnSafe(ta2,ref nmod,ref nmodd))
				{
				if (ta2 == 0)
					{
					turned = true;
					tangle = 0;
					}
				else if ((turned = Turn.TurnAngle(ta2)))
					{
					direc = (clocation.orientation - ta2) % 360;
					if (direc < 0)
						direc += 360;
					clocation.orientation = direc;
					clocation.ls = NavData.LocationStatus.DR;
					clocation.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(ta2),clocation.orientation, clocation.coord);
					NavData.SetCurrentLocation(clocation);
					tangle = ta2;
					}
				}
			else
				{
				turned = true;
				tangle = 0;
				}
			if (turned)
				{
				cdist = mod * Math.Abs((Math.Cos((modd + tangle) * SharedData.DEG_TO_RAD)));
				bdist = (int) Math.Ceiling(SharedData.REAR_TURN_RADIUS - cdist + SharedData.FRONT_PIVOT_PT_OFFSET + 1);
				sdist = MotionControl.GetSonarReading(SharedData.RobotLocation.REAR);
				if (LS02CLidar.RearClearence(ref cdist,SharedData.ROBOT_WIDTH +  (2 * MIN_SIDE_CLEAR)))
					{
					ldist = (int) Math.Round(cdist);
					sdist = Math.Min(sdist,ldist);
					}
				if (sdist > bdist)
					cmd = SharedData.BACKWARD_SLOW + " " + bdist;
				else
					cmd = "";
				if (cmd.Length > 0)
					{
					if (SendCommand(cmd,20000))
						{
						rsp = SendCommand(SharedData.DIST_MOVED);
						if (rsp.StartsWith("ok"))
							value = int.Parse(rsp.Substring(3));
						else
							value = bdist;
						clocation.coord = NavCompute.MapPoint(new Point(0, -value), clocation.orientation, clocation.coord);
						clocation.ls = NavData.LocationStatus.DR;
						clocation.loc_name = "";
						NavData.SetCurrentLocation(clocation);
						MotionMeasureProb.Move(new MotionMeasureProb.Pose(clocation.coord, clocation.orientation));
						rm.ClearLastRobotLocation();
						rm.ClearLastPDFEllipse();
						rm.DisplayRobotLoc(clocation.coord, Brushes.Purple);
						rm.DisplayPDFEllipse();
						rm.DisplayRmMap();
						rm.LocationMessage();
						rtn = TurnToFaceMP(mp);
						}
					else if (insufficent_clearence)
						{
						string lmf = "";

						MotionControl.DownloadLastMoveFile(ref lmf);
						rsp = SendCommand(SharedData.DIST_MOVED);
						if (rsp.StartsWith("ok"))
							value = int.Parse(rsp.Substring(3));
						else
							value = bdist/2;
						clocation.coord = NavCompute.MapPoint(new Point(0, -value), clocation.orientation, clocation.coord);
						clocation.ls = NavData.LocationStatus.DR;
						clocation.loc_name = "";
						NavData.SetCurrentLocation(clocation);
						MotionMeasureProb.Move(new MotionMeasureProb.Pose(clocation.coord, clocation.orientation));
						rm.LocationMessage();
						rm.ClearLastRobotLocation();
						rm.ClearLastPDFEllipse();
						rm.DisplayRobotLoc(clocation.coord, Brushes.Purple);
						rm.DisplayPDFEllipse();
						rm.DisplayRmMap();
						rm.LocationMessage();
						rtn = TurnToFaceMP(mp);
						}
					else
						{
						rsp = SendCommand(SharedData.DIST_MOVED);
						if (rsp.StartsWith("ok"))
							value = int.Parse(rsp.Substring(3));
						else
							value = bdist / 2;
						clocation.coord = NavCompute.MapPoint(new Point(0, -value), clocation.orientation, clocation.coord);
						clocation.ls = NavData.LocationStatus.DR;
						clocation.loc_name = "";
						NavData.SetCurrentLocation(clocation);
						MotionMeasureProb.Move(new MotionMeasureProb.Pose(clocation.coord, clocation.orientation));
						rm.LocationMessage();
						SharedData.med.mt = SharedData.MoveType.LINEAR;
						SharedData.med.et = MotionErrorType(last_error);
						SharedData.med.ob_descript = null;
						Log.LogEntry("Backward move failed.");
						}
					}
				else
					Log.LogEntry("Can not make required backward move.");
				}
			else
				{
				Log.LogEntry("Turn failed.");
				SharedData.med.mt = SharedData.MoveType.SPIN;
				SharedData.med.et = Turn.LastError();
				SharedData.med.ob_descript = null;
				}				
			return(rtn);
		}



		public bool TurnToFaceMP(Point mp)

		{
			bool rtn = false;
			NavCompute.pt_to_pt_data ppd;
			bool turn_safe;
			int angle;
			int mod = 0,modd = 0;

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
				if (!(turn_safe = Turn.TurnSafe(angle, ref mod,ref modd)))
					{
					int org_angle,org_modd,org_mod;

					if (Math.Abs(angle) > 135)
						{
						org_angle = angle;
						org_modd = modd;
						org_mod = mod;
						if (angle < 0)
							angle += 360;
						else
							angle -= 360;
						if (!(turn_safe = Turn.TurnSafe(angle, ref mod,ref modd)))
							if (SharedData.front_lidar_operational)
								rtn = RecoverUnsafeTurn(mp,org_mod,org_angle,org_modd);
						}
					else if (SharedData.front_lidar_operational)
						rtn = RecoverUnsafeTurn(mp,mod,angle,modd);
					}
				if (turn_safe)
					{
					rtn = Turn.TurnAngle(angle);
					if (rtn)
						{
						clocation.orientation = ppd.direc;
						clocation.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(angle),ppd.direc,clocation.coord);
						clocation.ls = NavData.LocationStatus.DR;
						NavData.SetCurrentLocation(clocation);
						MotionMeasureProb.Move(new MotionMeasureProb.Pose(clocation.coord,clocation.orientation));	//added 6/11/2019
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



		public bool TurnToFaceDirect(int direct)

		{
			Room.rm_location rl;

			clocation = NavData.GetCurrentLocation();
			rl = NavCompute.PtDistDirectApprox(clocation.coord,direct,200);
			return(TurnToFaceMP(rl.coord));
		}



		private bool AdjustLocation(ref NavData.location ecloc,Point sp,Point wp)

		{
			bool rtn = false;
			string err = "";
			Location loc = new Location();

			if (loc.DetermineDRLocation(ref ecloc,false,sp,true))
				rtn = true;
			else
				err = "could not find location or determine wall distance";
			if (rtn)
				Log.LogEntry("Adjusted location: " + ecloc.coord + "," + ecloc.orientation);
			else
				Log.LogEntry("Could not adjust location, " + err);
			return(rtn);
		}



		private void CollisionMonitorThread()

		{
			ArrayList lscan = new ArrayList();
			Rplidar.scan_data rdata;
			int central_angle,i;
			double mdist;
			bool obstacle_found = false;
			const int SIDE_CLEARANCE = 2;

			Thread.Sleep(1000);
			while (run_monitor)
				{
				Thread.Sleep(1000);
				if (!run_monitor)
					break;
				lscan.Clear();
				if (Rplidar.CaptureScan(ref lscan,true) && run_monitor)
					{
					mdist = ((double) SharedData.ROBOT_WIDTH/ 2) + SIDE_CLEARANCE;
					central_angle = (int) Math.Round(Math.Atan(mdist/COLLISION_CHECK_DIST) * SharedData.RAD_TO_DEG);
					obstacle_found = false;
					for (i = 0; i < lscan.Count; i++)
						{
						rdata = (Rplidar.scan_data) lscan[i];
						if ((rdata.angle <= 90) || (rdata.angle >= 270))
							{
							if ((rdata.angle <= central_angle) || (rdata.angle >= (360 - central_angle)))
								{
								if ((rdata.dist * Math.Cos(rdata.angle * SharedData.DEG_TO_RAD)) <= COLLISION_CHECK_DIST)
									{
									Log.KeyLogEntry("Y obstacle @ " + rdata.angle + " °, " + rdata.dist + " in.");
									obstacle_found = true;
									break;
									}
								}
							else
								{
								if (Math.Abs(rdata.dist * Math.Sin(rdata.angle * SharedData.DEG_TO_RAD)) < mdist)
									{
									Log.KeyLogEntry("X obstacle @ " + rdata.angle + " °, " + rdata.dist + " in.");
									obstacle_found = true;
									break;
									}
								}
							}
						}
					if (obstacle_found)
						{
						MotionControl.SendStopCommand("SL");
						run_monitor = false;
						Rplidar.SaveLidarScan(ref lscan);
						}
					}
				}
		}



		public MoveToPointResult MoveToPoint(Point mp,Point ep,bool entry)

		{
			bool rsp;
			MoveToPointResult rtn = MoveToPointResult.FALSE;
			Point wp;
			NavCompute.pt_to_pt_data ppd;
			int tc,mov_dist,mh,mad;
			string rply;
			bool orient_changed = false,wi_mag_limit = true;
			Thread mt = null;
			const int MIN_MONITORED_DIST = 36;
			int modist = 0;
			ObstacleAdjust.obstacle_action oact;
			Room rm = SharedData.current_rm;
			NavData.location ecloc = new NavData.location();

			Log.KeyLogEntry("Move to " + mp.ToString());
			mh = HeadAssembly.GetMagneticHeading();
			mad = NavCompute.DetermineDirection(mh);
			clocation = NavData.GetCurrentLocation();
			if (rm.Run && (mh >= 0) && (wi_mag_limit = sf.WithInMagLimit(clocation.orientation, mad)) && TurnToFaceMP(mp))
				{
				clocation = NavData.GetCurrentLocation();
				ppd = NavCompute.DetermineRaDirectDistPtToPt(mp, clocation.coord);
				rsp = false;
				insufficent_clearence = false;
				if (!entry)
					oact = oa.ObstacleAvoidAdjust(ppd.dist,SharedData.MIN_FRONT_CLEARANCE, ref modist, ref orient_changed,mp,ep);
				else
					{
					if (oa.DepartObstacleAvoid(ppd.dist,SharedData.MIN_FRONT_CLEARANCE,2,5))
						oact = ObstacleAdjust.obstacle_action.NO_OBSTACLE;
					else
						oact = ObstacleAdjust.obstacle_action.NONE;
					}
				if (oact == ObstacleAdjust.obstacle_action.DISTANCE)
					{
					ppd.dist = modist - SharedData.MIN_FRONT_CLEARANCE;
					oact = ObstacleAdjust.obstacle_action.NO_OBSTACLE;
					}
				if (rm.Run && (oact == ObstacleAdjust.obstacle_action.NO_OBSTACLE))
					{
					if (orient_changed)
						{
						Point pt;

						clocation = NavData.GetCurrentLocation();
						pt = new Point(0,ppd.dist);
						mp = NavCompute.MapPoint(pt,clocation.orientation,clocation.coord);
						}
					wp = NavCompute.DetermineWallProjectPt(clocation.coord, mp, clocation.orientation);
					mov_dist = ppd.dist;
					if (ppd.dist > MIN_MONITORED_DIST)
						{
						run_monitor = true;
						mt = new Thread(CollisionMonitorThread);
						mt.Start();
						}
					if (ppd.dist < SharedData.MAX_DIST_DIF / 3)
						{
						rsp = true;
						mov_dist = 0;
						}
					else if (ppd.dist < 24)
						rsp = SendCommand(SharedData.FORWARD_SLOW + " " + ppd.dist,8000);
					else
						{
						tc = (int) (((ppd.dist/7.2) + 2) * 100);
						rsp = SendCommand(SharedData.FORWARD + " " + ppd.dist,tc);
						}
					if (run_monitor)
						run_monitor = false;
					if (mt != null)
						mt.Join();
					if (rsp || insufficent_clearence)
						{
						bool corrected_location;

						if (mov_dist == 0)
							ecloc = clocation;
						else
							{
							rply = SendCommand(SharedData.DIST_MOVED);
							if (rply.StartsWith("ok"))
								mov_dist = int.Parse(rply.Substring(3));
							else
								mov_dist = -1;
							ecloc = clocation;
							if (mov_dist != -1)
								{
								Point pt;

								pt = new Point(0,mov_dist);
								ecloc.coord = NavCompute.MapPoint(pt,clocation.orientation,clocation.coord);
								}
							else
								ecloc.coord = mp;
							}
						ecloc.ls = NavData.LocationStatus.DR;
						ecloc.loc_name = "";
						Log.LogEntry("Expected location: " + ecloc.ToString());
						MotionMeasureProb.Move(new MotionMeasureProb.Pose(ecloc.coord,ecloc.orientation));
						rm.ClearLastRobotLocation();
						rm.ClearLastPDFEllipse();
						rm.DisplayRobotLoc(ecloc.coord, Brushes.PaleTurquoise);
						rm.DisplayPDFEllipse();
						rm.DisplayRmMap();
						corrected_location = AdjustLocation(ref ecloc, clocation.coord, wp);
						if (!corrected_location)
							{
							if ((mov_dist != -1) || (!insufficent_clearence) || entry)
								{
								clocation = ecloc;
								rm.VerifyLoc = true;
								}
							else
								{
								string lmf = "";

								MultiOutput("Could not estimate location after insufficent front clearance.  Move cancelled.");
								clocation.ls = NavData.LocationStatus.UNKNOWN;
								NavData.SetCurrentLocation(clocation);
								MotionControl.DownloadLastMoveFile(ref lmf);
								rtn = MoveToPointResult.FALSE;
								}
							}
						else
							{
							rm.VerifyLoc = false;
							clocation = ecloc;
							}
						clocation.loc_name = "";
						clocation.entrance = false;
						rm.ClearLastRobotLocation();
						rm.ClearLastPDFEllipse();
						rm.DisplayRobotLoc(clocation.coord, Brushes.Purple);
						rm.DisplayPDFEllipse();
						rm.DisplayRmMap();
						NavData.SetCurrentLocation(clocation);
						rm.LocationMessage();
						if (rm.VerifyLoc)
							{
							rm.VerifyLoc = false;
							MultiOutput("Could not verify location.");
							clocation.ls = NavData.LocationStatus.UNKNOWN;
							NavData.SetCurrentLocation(clocation);
							SharedData.med.et = SharedData.MotionErrorType.LOC_NOT_VERIFIED;
							rm.LocationMessage();
							rtn = MoveToPointResult.FALSE;
							}
						else
							rtn = MoveToPointResult.TRUE;
						}
					else if (rm.Run)
						{
						string lmf = "";

						Log.LogEntry("Could not move to " + mp.ToString());
						MotionControl.DownloadLastMoveFile(ref lmf);
						SharedData.med.mt = SharedData.MoveType.LINEAR;
						SharedData.med.et = MotionErrorType(last_error);
						SharedData.med.ob_descript = null;
						clocation.loc_name = "";
						rply = SendCommand(SharedData.DIST_MOVED);
						if (rply.StartsWith("ok"))
							mov_dist = int.Parse(rply.Substring(3));
						else
							mov_dist = -1;
						ecloc = clocation;
						if (mov_dist != -1)
							{
							ecloc.coord = NavCompute.MapPoint(new Point(0,mov_dist),clocation.orientation,clocation.coord);
							ecloc.ls = NavData.LocationStatus.DR;
							Log.LogEntry("Expected location: " + ecloc.ToString());
							MotionMeasureProb.Move(new MotionMeasureProb.Pose(ecloc.coord, ecloc.orientation));
							}
						else
							ecloc.ls = NavData.LocationStatus.UNKNOWN;
						NavData.SetCurrentLocation(ecloc);
						rtn = MoveToPointResult.FALSE;
						}
					}
				else if (oact == ObstacleAdjust.obstacle_action.REMAPPED)
					rtn = MoveToPointResult.MAP_INSERTION;
				else
					{
					Log.LogEntry("Could not adjust for forward obstacles.");
					SharedData.med.mt = SharedData.MoveType.LINEAR;
					SharedData.med.et = SharedData.MotionErrorType.OBSTACLE;
					SharedData.med.ob_descript = oa.DetectedObstacles();
					rtn = MoveToPointResult.FALSE;
					}
				}
			else if (rm.Run)
				{
				if (mh < 0)
					{
					MultiOutput("Could not obtain magnectic heading.");
					}
				else if (wi_mag_limit)
					Log.LogEntry("Could not turn to face point " + mp.ToString());
				else
					{
					MultiOutput("Orientation did not agree with magnectic compass: orientation - " + clocation.orientation  + "  mag compass - " + mad);
					clocation.ls = NavData.LocationStatus.UNKNOWN;
					NavData.SetCurrentLocation(clocation);
					}
				rtn = MoveToPointResult.FALSE;
				}
			return(rtn);
		}



		public MoveToPointResult MoveToPoint(Point mp, Point ep)

		{
			return(MoveToPoint(mp,ep,false));
		}



		private bool ErrorRecovery()

		{
			bool rtn = false;
			Location loc = new Location();

			if ((SharedData.med.mt == SharedData.MoveType.SPIN) && (SharedData.med.et == SharedData.MotionErrorType.TURN_NOT_SAFE))
				MultiOutput("Can not recover from motion error " + SharedData.med.et.ToString());
			else
				{
				if ((clocation.ls == NavData.LocationStatus.UNKNOWN))
					{
					if (!loc.DetermineDRLocation(ref clocation,true,NavData.GetCurrentLocation().coord))
						{
						MultiOutput("Can not recover from unverified location.");
						return(false);
						}
					else
						NavData.SetCurrentLocation(clocation);
					}
				switch(SharedData.med.et)
					{
					case SharedData.MotionErrorType.MPU:
					case SharedData.MotionErrorType.START_TIMEOUT:
					case SharedData.MotionErrorType.UDP_TIMEOUT:
					case SharedData.MotionErrorType.INIT_FAIL:
						MultiOutput("Attempting to recover from the motion error " + SharedData.med.et.ToString() + ". This may take a couple of minutes.");
						rtn = MotionControl.RestartMC();
						break;

					case SharedData.MotionErrorType.LOC_NOT_VERIFIED:
						rtn = true;
						break;
					}
				}
			return(rtn);
		}



		private bool PtInRoom(Point pt)

		{
			bool rtn = false;
			int h,w;

			h = NavData.rd.rect.Height;
			w = NavData.rd.rect.Width;
			if ((pt.X > 0) && (pt.Y > 0) && (pt.X < w)  && (pt.Y < h))
				rtn = true;
			return(rtn);
		}


		private bool ReplanMove(ref ArrayList mptal,Point ep)

		{
			bool rtn = false;
			int i,direc;
			Rplidar.scan_data sd;
			byte[,] dmap, mmap;
			Move mov = new Move();
			NavData.location clocation;
			Bitmap rmbm = null;
			string fname;
			Point pt;
			ArrayList obstacles;

			dmap = (byte[,]) NavData.detail_map.Clone();
			clocation = NavData.GetCurrentLocation();
			if (SharedData.log_operations)
				rmbm = (Bitmap) SharedData.current_rm_map.Clone();
			obstacles = oa.DetectedObstacles();
			for (i = 0;i < obstacles.Count;i++)
				{
				sd = (Rplidar.scan_data) obstacles[i];
				direc = (clocation.orientation + sd.angle) % 360;
				pt = NavCompute.MapPoint(new Point(0, (int) Math.Round(sd.dist)), direc, clocation.coord);
				if (PtInRoom(pt))
					{
					dmap[pt.X,pt.Y] = (int) Room.MapCode.BLOCKED;
					if (rmbm != null)
						rmbm.SetPixel(pt.X,pt.Y, Color.Black);
					}
				}
			if (rmbm != null)
				{
				fname = Log.LogDir() + NavData.rd.name + " obstacle map " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				Log.LogEntry("Saved: " + fname);
				rmbm.Save(fname,ImageFormat.Jpeg);
				}
			mmap = Room.CreateMoveMap(dmap);
			if (mmap.Length > 0)
				{
				mptal.Clear();
				if (!mov.DetermineIntermediatePts(ep, clocation.coord,ref mptal,mmap))
					{
					mptal.Clear();
					Log.LogEntry("ReplanMove: could not determine new path.");
					}
				else
					rtn = true;
				}
			else
				Log.LogEntry("ReplanMove: could not create move map.");
			return(rtn);
		}



		public bool GoToRoomPoint(NavData.room_pt rpt,Point fp)

		{
			bool rtn = false;
			MoveToPointResult mtpr;
			ArrayList mptal = new ArrayList();
			NavCompute.pt_to_pt_data ppd;
			int i;
			Point mp = new Point();
			Room rm = SharedData.current_rm;

			try
			{
			clocation = NavData.GetCurrentLocation();
			if (NavCompute.DistancePtToPt(rpt.coord,clocation.coord) > SharedData.MAX_DIST_DIF/2)
				{
				rm.DisplayPoint(rpt.coord, Brushes.Red);
				if (!Navigate.PathClear(clocation.coord, rpt.coord))
					{
					if (!DetermineIntermediatePts(rpt.coord, clocation.coord,ref mptal))
						{
						mptal.Clear();
						MultiOutput("Could not determine a path.");
						rm.DisplayPoint(clocation.coord, Brushes.Red);
						rm.DisplayRmMap();
						}
					}
				else
					mptal.Add(rpt.coord);
				if (rm.VerifyLoc && mptal.Count > 0)
					{
					NavData.location loc = new NavData.location();
					LocDecisionEngine lde = new LocDecisionEngine();

					loc.coord = (Point) mptal[0];
					ppd = NavCompute.DetermineRaDirectDistPtToPt(loc.coord, clocation.coord);
					loc.orientation = ppd.direc;
					if (lde.LocationActionCheck(loc,false) < 2)
						{
						MultiOutput("Location verification required but not within range of sufficent localization actions.  Move cancelled.");
						mptal.Clear();
						}
					}
				Log.LogArrayList("Initial move path",mptal);
				for (i = 0;(i < mptal.Count) && rm.Run;i++)
					{
					int dist;
					double seg;

					mp = (Point) mptal[i];
					rtn = false;
					if ((i > 0) && !Navigate.PathClear(clocation.coord,mp))
						{
						Log.LogEntry("Next move does not have a clear path.  Attempting to determine revised path.");
						mptal.Clear();
						if (!DetermineIntermediatePts(rpt.coord, clocation.coord,ref mptal))
							{
							mptal.Clear();
							MultiOutput("Could not determine a revised path.");
							rm.DisplayPoint(clocation.coord, Brushes.Red);
							rm.DisplayRmMap();
							break;
							}
						else
							{
							Log.LogArrayList("Revised move path",mptal);
							i = 0;
							mp = (Point) mptal[i]; 
							}
						}
					if ((dist = NavCompute.DistancePtToPt(clocation.coord,mp)) > MAX_MOVE_SEG_DIST)
						{
						i -= 1;
						seg = ((double) dist/MAX_MOVE_SEG_DIST);
						mp.X = (int) Math.Round(((double) mp.X + clocation.coord.X)/ Math.Ceiling(seg));
						mp.Y = (int) Math.Round(((double) mp.Y + clocation.coord.Y) / Math.Ceiling(seg));
						Log.LogEntry("Move exceeds max. length limit, intermediate point set to : (" + mp.X + "," + mp.Y + ").");
						}
					if ((mtpr = MoveToPoint(mp,rpt.coord)) == MoveToPointResult.TRUE)
						{
						rtn = true;
						if (i == mptal.Count -1)
							{
							if ((dist = NavCompute.DistancePtToPt(mp,clocation.coord)) > MAX_FINAL_DISTANCE)
								{
								Log.KeyLogEntry("FINAL  MOVE POINT ACTUAL VS PLANNED WAS " + dist + " IN. DIFFERENT");
								if (fp != Room.ignor)
									{
									if (fp.IsEmpty || (NavCompute.DistancePtToPt(fp,clocation.coord) > MAX_MOVE_SEG_DIST) 
										|| (NavCompute.DistancePtToPt(fp,clocation.coord) > NavCompute.DistancePtToPt(mp,fp)) 
										|| !Navigate.PathClear(clocation.coord,fp))
										{
										if (Navigate.PathClear(clocation.coord,mp))
											i -= 1;
										else
											{
											mptal.Clear();
											if (DetermineIntermediatePts(rpt.coord, clocation.coord,ref mptal))
												i = -1;
											else
												{
												rtn = false;
												MultiOutput("Can not complete the move.");
												break;
												}
											}
										}
									}
								}
							}
						else
							{
							Point np = new Point();

							if ((dist = NavCompute.DistancePtToPt(mp,clocation.coord)) > MAX_FINAL_DISTANCE)
								Log.KeyLogEntry("MOVE POINT ACTUAL VS PLANNED WAS " + dist + " IN. DIFFERENT");
							np = (Point) mptal[i + 1];
							if (!Navigate.PathClear(clocation.coord,np))
								{
								if (Navigate.PathClear(clocation.coord,mp))
									i -= 1;
								else
									{
									mptal.Clear();
									if (DetermineIntermediatePts(rpt.coord, clocation.coord,ref mptal))
										i = -1;
									else
										{
										rtn = false;
										MultiOutput("Can not complete the move.");
										break;
										}

									}
								}
							}
						}
					else if (mtpr == MoveToPointResult.MAP_INSERTION)
						{
						mptal.InsertRange(i,oa.NewPath());
						Log.LogArrayList("Move path after oa insertion",mptal);
						i -= 1;
						}
					else
						{
						if (SharedData.med.et == SharedData.MotionErrorType.OBSTACLE)
							{
							if (ReplanMove(ref mptal,rpt.coord))
								{
								i = -1;
								Log.LogArrayList("Move path after obstacle replan",mptal);
								}
							else
								{
								MultiOutput("Can not find way around obstacle.");
								break;
								}
							}
						else if (ErrorRecovery())
							{
							if ((i + 1) < mptal.Count)
								{
								Point np = new Point();

								np = (Point) mptal[i + 1];
								if (!Navigate.PathClear(clocation.coord,np))
									{
									if (Navigate.PathClear(clocation.coord,mp))
										i -= 1;
									else
										{
										mptal.Clear();
										if (DetermineIntermediatePts(rpt.coord, clocation.coord,ref mptal))
											i = -1;
										else
											{
											MultiOutput("Can not recover from motion error " + SharedData.med.et.ToString());
											break;
											}
										}
									}
								}
							else if (NavCompute.DistancePtToPt(mp, clocation.coord) <= MAX_FINAL_DISTANCE)
								{
								rtn = true;
								break;
								}
							else
								i = -1;
							}
						else
							break;
						}
					}
				}
			else
				rtn = true;
			}

			catch(Exception ex)
			{
			MultiOutput("GoToRoomPoint exception: " + ex.Message);
			Log.LogEntry("Source: " + ex.Source);
			Log.LogEntry("  stack trace: " + ex.StackTrace);
			rtn = false;
			}

			return(rtn);
		}



		}
	}
