using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using AutoRobotControl;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;


namespace Work_Assist
	{
	class SSMoveToWorkSpace: MoveToWorkSpaceInterface
		{
		// primary assumptions: 
		//		1. If the speaker is in "tight quarters" then the move within "tight quarters" so the room map is of no use (ok assumption)
		//		2. Expect some obstacle annomilies in front LIDAR data (proven)
		//		3. LIDAR annomilies are few, single and isolated (ok assumption)
		//		4. SONAR and visual depth images will not have the same annomilies (ok assumption)
		//		5. Visual depth images can not detect annomilies closer then 13 in (ok assumption with current Kinect and front LIDAR parameters)
		//		6. SONAR can not be used to rule out annomilies (proven)
		//		7. A second LIDAR scan can remove or add annomilies (proven)
		//		8. SONAR and visual can detect obstacles that LIDAR would miss (ok assumptions)
		//		9. Obstacles can be dymanic (ok assumption)
		//		10. SONAR can have anomilies but they are not characterized (ok assumption)
		//		11. Visual takes significantly longer (time to tilt etc.) and the "floor data" is noisey but can eliminate LIDAR anomalies beyond 13 ft (proven)
		//		12. Obstacles (e.g. stools) can have LIDAR signatures similar to anomalites especially at distance and on the track edge (proven)
		//		13. "Thin" obstacles (e.g. stools) can be missed by SONAR at distance beyond 4 ft or on the edge of the robot's track (proven)
		//		14. The alignment of the front LIDAR and the Kinect will be off some, especially if the Kinect is panned, this is reflected in the side margins used with the Kinect (? assumption)
		//		15. Same side "in line" arrangement (robot with speaker) (limiting assumption but correct for current proof of principal implementation)
		//		16. A Kinect tilt angle of -55 degrees will give the robot a good view of the work area
		//		17. A person can be inserted as a 12" radius circle on a LIDAR scan or map (ok assumption)
		//		18. The front edge of the work space is the "furthest out" part of the work space (ok assumption)
		//		19. The speaker is the only person at the work space. (Limiting assumption but ok for current proof of principal implmentation)
		//		20. The speaker is facing the robot during the "move process" (ok assumption)
		//		21. Given 6, 10 & 13, SONAR should be used only for low level, dynamic obstacle avoidance during motion NOT motion planning (ok assumption)
		//		22. An accurate representation of the speaker's location requires a face detection to determine the center point (ok assumption)
		//		23. Visual can have anamolies, they appear as mutli-point blobs (rare but proven see Kinect floor scan 11.4.2019 15.56-48.pc)


		private enum MoveStatus {SUCCESS,FAIL,PATH_CHG};
		private enum MoveAdjustStatus {FAIL,NONE,NEW_POINT,NEW_DIST,NEW_PATH};

		private const int MIN_EDGE_DIST = 12;  //THIS IS SOMEWHAT DEPENDENT ON THE HEIGHT OF THE WORK SPACE COMPARED TO THE HEIGHT OF THE ARM

		private bool run_monitor = false;
		private double min_monitor_dist;
		private bool non_straight_entry = false;
		private Room rm = SharedData.current_rm;


		private double DetermineTopEdgeDistance(double tilt_correct,bool log = true)

		{
			int i, start, end, index = 0;
			double dist, angle, min_dist = Kinect.nui.DepthStream.MaxDepth * SharedData.MM_TO_IN;
			Arm.Loc3D loc = new Arm.Loc3D();

			start = 320;
			end = Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth;
			for (i = start; i < end; i += Kinect.nui.ColorStream.FrameWidth)
				{
				if (SkillShared.dips[i].Depth > 0)
					{
					dist = Kinect.CorrectedDistance(SkillShared.dips[i].Depth * SharedData.MM_TO_IN);
					angle = Kinect.VideoVerDegrees((Kinect.nui.ColorStream.FrameHeight / 2) - index);
					loc = Arm.MapKCToRC(0, dist,tilt_correct, angle);
					if (loc.y < SkillShared.wsd.top_height - SkillShared.TOP_MAGRIN)
						break;
					else if (loc.y < SkillShared.wsd.top_height + SkillShared.TOP_MAGRIN)
						{
						if ((loc.z > 0) && (loc.z < min_dist))
							min_dist = loc.z;
						}
					}
				index += 1;
				}
			if (SharedData.log_operations && log)
				SkillShared.SaveDipsData(" Determine top edge distance ",SkillShared.dips);
			if ((min_dist == Kinect.nui.DepthStream.MaxDepth * SharedData.MM_TO_IN) || (min_dist < 0))
				min_dist = 0;
			return (min_dist);
		}



		private int DetermineAdjustMoveDist(ArrayList al)

		{
			int mdist = 0,mapd,kd;
			double min_dist;
			ArrayList obs = new ArrayList();
			const double ADJUST_MIN_CLEAR = .5;

			mapd = SkillShared.FindObstacles(0,-1,al, (double)SkillShared.ARM_PERCH_WIDTH / 2,ADJUST_MIN_CLEAR, ref obs);
			mapd -= SharedData.ARM_PERCH_OFFSET + 2;
			min_dist = DetermineTopEdgeDistance(SkillShared.MOVE_KINECT_TILT_CORRECT);
			kd = (int) Math.Floor(min_dist - MIN_EDGE_DIST);
			Log.LogEntry("Clear dist:  LIDAR map " + mapd + "   Kinect " + kd);
			mdist = Math.Min(kd,mapd);
			Log.LogEntry("Adjust move dist: " + mdist);
			return (mdist);
		}



		private bool AdjustMoveForward(int dist)

		{
			bool rtn = false;
			string rsp;

			rsp = SkillShared.SendCommand(SharedData.FORWARD_SLOW_NCC + " " + dist, 8000);
			if (rsp.StartsWith("ok"))
				rtn = true;
			return (rtn);
		}



		private bool FinalAdjustment(ref Stack final_move)

		{
			int pan,turn;
			PersonDetect.scan_data fd = new PersonDetect.scan_data();
			bool rtn = false;

			if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
				{
				pan = 45;
				turn = -45;
				}
			else
				{
				pan = -45;
				turn = 45;
				}
			HeadAssembly.Pan(0, true);
			HeadAssembly.Tilt(SkillShared.MOVE_KINECT_TILT, true);
			Kinect.SetNearRange();
			SkillShared.RecordWorkSpaceData("Work space data mtws 1 ");
			if (SkillShared.TurnAngle(turn))
				{
				ArrayList al = new ArrayList();
				int mdist;
				SkillShared.RecordWorkSpaceData("Work space data mtws 2 ");
				final_move.Push(-turn);
				if (Rplidar.CaptureScan(ref al, true))
					{
					HeadAssembly.Tilt(0, true);
					HeadAssembly.Pan(pan, true);
					if (SkillShared.FindFace(pan - turn, ref fd, true))
						{
						SpeakerData.Face = fd;
						HeadAssembly.Pan(0, true);
						HeadAssembly.Tilt(SkillShared.MOVE_KINECT_TILT, true);
						AddCircleToScan(ref al,pan - fd.angle,fd.dist - SharedData.FLIDAR_OFFSET + SkillShared.HEAD_CENTER_DIST,SkillShared.PERSON_RADIUS);
						mdist = DetermineAdjustMoveDist(al);
						if (mdist > 0)
							{
							if (AdjustMoveForward(mdist))
								final_move.Push(mdist);
							else
								Log.LogEntry("Adjust move forward failed.");
							SkillShared.RecordWorkSpaceData("Work space data mtws 3 ");
							}
						SkillShared.wsd.front_edge_dist = DetermineTopEdgeDistance(SkillShared.MOVE_KINECT_TILT_CORRECT);
						Log.LogEntry("Edge distance: " + SkillShared.wsd.front_edge_dist);
						rtn = true;
						}
					else
						{
						SkillShared.OutputSpeech("Could not find speaker's face");
						SpeakerData.FaceClear();
						HeadAssembly.Pan(0, true);
						}
					}
				else
					SkillShared.OutputSpeech("Could not capture LIDAR scan for final adjustment move.");
				}
			else
				{
				SkillShared.OutputSpeech("turn failed.");
				}
			HeadAssembly.Tilt(0,true);
			Kinect.SetFarRange();
			return(rtn);
		}



		private void AddCircleToScan(ref ArrayList sdata,double cpangle,double cpdist, int rad)

		{
			PointF cpt = new PointF(),cirpt = new PointF();
			int i,angle;
			Rplidar.scan_data sd = new Rplidar.scan_data();
			
			Log.LogEntry("AddCircleToScan: " + cpangle.ToString("F2") + ", " + cpdist.ToString("F2") + "," + rad);
			cpt.X = (float) (cpdist * Math.Sin(cpangle * SharedData.DEG_TO_RAD));
			cpt.Y = (float) (cpdist * Math.Cos(cpangle * SharedData.DEG_TO_RAD));
			for (i = 0;i < 360;i += 5)
				{
				cirpt.X = cpt.X + (float) (rad * Math.Sin(i * SharedData.DEG_TO_RAD));
				cirpt.Y = cpt.Y + (float) (rad * Math.Cos(i * SharedData.DEG_TO_RAD));
				angle = (int) Math.Round(Math.Atan(cirpt.X / cirpt.Y) * SharedData.RAD_TO_DEG);
				if (angle < 0)
					angle += 360;
				if (angle > 360)
					angle -= 360;
				sd.angle = (ushort) angle;
				sd.dist = Math.Sqrt((cirpt.X * cirpt.X) + (cirpt.Y * cirpt.Y));
				sdata.Add(sd);
				}
		}



		private MoveAdjustStatus LastMoveAdjust(NavData.location cl,int pan,ref int dist,ref Point pt)
		//assumes final move is ~ 24 in.
		{
			MoveAdjustStatus rtn = MoveAdjustStatus.FAIL;
			ArrayList sdata = new ArrayList();
			int mdist,edist,min_dist,tilt,ldist,mapd,mapdw,no_kobs = 0;
			ArrayList obs = new ArrayList();
			Point dpt;

			Log.LogEntry("LastMoveAdjust: " + dist);
			if ((Rplidar.CaptureScan(ref sdata, true)))
				{
				Kinect.SetNearRange();
				HeadAssembly.Tilt(SkillShared.MOVE_KINECT_TILT, true);
				ldist = SkillShared.FindObstacles(0,-1, sdata, SkillShared.MIN_SIDE_CLEAR, ref obs);
				SkillShared.RemoveLidarAnomalies(dist,ldist,ref obs,ref sdata,ref no_kobs,false);
				AddCircleToScan(ref sdata,pan - SpeakerData.Face.angle, SpeakerData.Face.dist - SharedData.FLIDAR_OFFSET + SkillShared.HEAD_CENTER_DIST,SkillShared.PERSON_RADIUS);
				Rplidar.SaveLidarScan(ref sdata,"Last move adjust scan with Person");
				mapd = SkillShared.FindObstacles(0,-1,sdata,(double) SkillShared.ARM_PERCH_WIDTH/2, SkillShared.MIN_SIDE_CLEAR,ref obs);
				obs.Clear();
				mapdw = SkillShared.FindObstacles(0,-1, sdata,(double) SharedData.ROBOT_WIDTH / 2, SkillShared.MIN_SIDE_CLEAR, ref obs);
				if (mapd - mapdw > SharedData.ARM_PERCH_OFFSET)
					mdist = mapdw;
				else
					mdist = mapd - SharedData.ARM_PERCH_OFFSET;
				if (Kinect.GetDepthFrame(ref SkillShared.depthdata, 60))
					{
					Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, SkillShared.depthdata, SkillShared.dips);
					edist = (int) Math.Floor(DetermineTopEdgeDistance(SkillShared.MOVE_KINECT_TILT_CORRECT));
					tilt = NavCompute.AngularDistance(cl.orientation, SkillShared.wsd.work_loc.orient);
					edist -= (int) (Math.Ceiling(((double) SkillShared.ARM_PERCH_WIDTH/2) * Math.Tan(tilt * SharedData.DEG_TO_RAD)) + (SharedData.ARM_PERCH_OFFSET + 1));
					Log.LogEntry("LIDAR map dist - " + mdist + "  Kinect edge dist - " + edist);
					min_dist = Math.Min(mdist,edist);
					if (min_dist < dist)
						{
						if (min_dist == edist)
							{
							dist = min_dist;
							rtn = MoveAdjustStatus.NEW_DIST;
							}
						else
							{
							int angle = -1;

							if (SkillShared.ObstacleAdjustAngle(sdata,obs,edist,0,SkillShared.MIN_SIDE_CLEAR,ref angle))
								{
								if (Math.Abs(angle) < SharedData.MIN_TURN_ANGLE)
									{
									if (angle > 0)
										angle = SharedData.MIN_TURN_ANGLE;
									else
										angle = -SharedData.MIN_TURN_ANGLE;
									}
								dpt = new Point(0,edist);
								pt = NavCompute.MapPoint(dpt,cl.orientation - angle, cl.coord);
								rtn = MoveAdjustStatus.NEW_POINT;
								}
							else
								Log.LogEntry("No adjustment found.");
							}
						}
					else if (min_dist > dist)
						{
						dist = min_dist;
						rtn = MoveAdjustStatus.NEW_DIST;
						}
					else
						rtn = MoveAdjustStatus.NONE;
					}
				else
					Log.LogEntry("Could not capture a Kinect depth frame.");
				HeadAssembly.Tilt(0,true);
				Kinect.SetFarRange();
				}
			else
				{
				Log.LogEntry("Could not capture a LIDAR scan.");
				}
			return (rtn);
		}



		private MoveAdjustStatus MoveAdjust(ArrayList path, int mindx, NavData.location cl,ref int dist,ref Point npt,ref ArrayList npath)

		{																							//FAILURE IN DEBUG/2.5.2020 12.1.6 DUE TO SINGLE ANOMILY
			MoveAdjustStatus rtn = MoveAdjustStatus.FAIL;							//HOW TO MAKE ANOMILY REMOVAL BETTER????
			ArrayList sdata = new ArrayList(), obs = new ArrayList();
			int ldist = -1, rdist, mdist, cangle = 0,no_kobs = 0;
			Point pt;
			const double MAX_DIST_CHG = .1;

			Log.LogEntry("MoveAdjust: " + mindx + "  " + dist);
			if (Rplidar.CaptureScan(ref sdata, true))
				ldist = SkillShared.FindObstacles(0, dist + SharedData.ARM_PERCH_OFFSET + 1, sdata, SkillShared.MIN_SIDE_CLEAR, ref obs);
			if (ldist > 0)
				{
				Log.LogEntry("LIDAR front clearence: " + ldist);
				mdist = SkillShared.RemoveLidarAnomalies(dist,ldist,ref obs,ref sdata,ref no_kobs,true);
				if (mdist < dist + SharedData.ARM_PERCH_OFFSET + 1)
					{
					rdist = mdist - (SharedData.ARM_PERCH_OFFSET + 1);
					if ((rdist > 0) && (Math.Abs(dist - rdist) < (MAX_DIST_CHG * dist)))
						{
						dist = rdist;
						rtn = MoveAdjustStatus.NEW_DIST;
						}
					else if ((obs.Count > 0) && SkillShared.ObstacleAdjustAngle(sdata, obs, dist + SharedData.ARM_PERCH_OFFSET + 1, 0, 1, ref cangle))
						{
						if (path.Count == 1)
							{
							if (SkillShared.TwoStepPlan(cl.orientation, cangle, ref npath))
								{
								rtn = MoveAdjustStatus.NEW_PATH;
								}
							else
								rtn = MoveAdjustStatus.FAIL;
							}
						else
							{
							pt = new Point(0, dist);
							npt = NavCompute.MapPoint(pt, cl.orientation - cangle, cl.coord);
							rtn = MoveAdjustStatus.NEW_POINT;
							}
						}
					}
				else
					rtn = MoveAdjustStatus.NONE;
				}
			return (rtn);
		}



		private void MoveMonitor()

		{
			Stopwatch sw = new Stopwatch();
			double edist;
			long em;

			try
			{
			Thread.Sleep(100);
			while(run_monitor)
				{
				sw.Restart();
				if (Kinect.GetDepthFrame(ref SkillShared.depthdata, 40))
					{
					Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, SkillShared.depthdata, SkillShared.dips);
					edist = DetermineTopEdgeDistance(SkillShared.MOVE_KINECT_TILT_CORRECT,false);
					if ((edist > 0) && (edist <= min_monitor_dist))
						{
						AutoRobotControl.MotionControl.SendStopCommand("SL");
						Log.LogEntry("Move monitor stop issued.");
						if (SharedData.log_operations)
							SkillShared.SaveDipsData("Move monitor stop ",SkillShared.dips);
						break;
						}
					}
				em = sw.ElapsedMilliseconds;
				if ((em > 100) && run_monitor)
					Thread.Sleep(100 - (int) em);
				}
			}

			catch(Exception)
			{
			run_monitor = false;
			}
		}



		private MoveStatus MoveToPt(ref ArrayList path,int mindx,ref int target_dist,ref int moved_dist,int pan)

		{																//DOES NOT CHECK FOR SUFFICENT SIDE CLEARENCE TO MAKE TURN AT MOVE POINT
			MoveStatus rtn = MoveStatus.FAIL;
			bool suff_dist = false;
			NavData.location cl,eloc;
			ArrayList sdata = new ArrayList(),obs = new ArrayList(),npath = new ArrayList();
			int dist;
			string rsp;
			Point pt;
			Point mp;
			MoveAdjustStatus mas;
			Thread monitor = new Thread(MoveMonitor);

			mp = (Point) path[mindx];
			Log.LogEntry("MoveTo " + mp);
			cl = NavData.GetCurrentLocation();
			dist = NavCompute.DistancePtToPt(cl.coord,mp);
			if ((mindx == path.Count - 1) && SpeakerData.Face.detected)
				mas = LastMoveAdjust(cl,pan,ref dist,ref mp);
			else
				mas = MoveAdjust(path,mindx,cl,ref dist,ref mp,ref npath);
			switch(mas)
				{
				case MoveAdjustStatus.NONE:
					suff_dist = true;
					break;

				case MoveAdjustStatus.NEW_POINT:
					path[mindx] = mp;
					rtn = MoveStatus.PATH_CHG;
					Log.LogArrayList("Modified move path: ", path);
					break;

				case MoveAdjustStatus.NEW_DIST:
					suff_dist = true;
					break;

				case MoveAdjustStatus.NEW_PATH:
					path = npath;
					rtn = MoveStatus.PATH_CHG;
					Log.LogArrayList("Modified move path: ", path);
					break;
				}
			if (suff_dist)
				{
				target_dist = dist;
				rsp = SkillShared.SendCommand("TFSFC " + (SharedData.ARM_PERCH_OFFSET + 1), 200);
				if (mindx == path.Count - 1)
					{
					double inc_angle;

					HeadAssembly.Tilt(SkillShared.MOVE_KINECT_TILT,true);
					run_monitor = true;
					inc_angle = NavCompute.AngularDistance(cl.orientation, SkillShared.wsd.edge_perp_direct);
					min_monitor_dist = MIN_EDGE_DIST/Math.Cos(inc_angle * SharedData.DEG_TO_RAD);
					monitor.Start();
					}
				if (rsp.StartsWith("ok"))
					rsp = SkillShared.SendCommand(SharedData.FORWARD_SLOW + " " + dist, 8000);
				else
					rsp = SkillShared.SendCommand(SharedData.FORWARD_SLOW_NCC + " " + dist, 8000);
				if (mindx == path.Count - 1)
					{
					run_monitor = false;
					monitor.Join();
					HeadAssembly.Tilt(0, true);
					}
				if (rsp.StartsWith("ok") || ((rsp.StartsWith("fail") && (rsp.Contains(SharedData.INSUFFICENT_FRONT_CLEARANCE)))))
					{
					rsp = SkillShared.SendCommand(SharedData.DIST_MOVED, 200);
					if (rsp.StartsWith("ok"))
						dist = int.Parse(rsp.Substring(3));
					moved_dist = dist;
					pt = new Point(0, dist);
					eloc = cl;
					eloc.coord = NavCompute.MapPoint(pt, cl.orientation, cl.coord);
					eloc.ls = NavData.LocationStatus.DR;
					eloc.loc_name = "";
					Log.LogEntry("Expected location: " + eloc.ToString());
					NavData.SetCurrentLocation(eloc);
					MotionMeasureProb.Move(new MotionMeasureProb.Pose(eloc.coord, eloc.orientation));
					rtn = MoveStatus.SUCCESS;
					}
				else
					{
					string rsp2;

					cl.ls = NavData.LocationStatus.UNKNOWN;
					rsp2 = SkillShared.SendCommand(SharedData.DIST_MOVED, 200);
					if (rsp2.StartsWith("ok"))
						{
						dist = int.Parse(rsp.Substring(3));
						pt = new Point(0, dist);
						eloc = cl;
						eloc.coord = NavCompute.MapPoint(pt, cl.orientation, cl.coord);
						eloc.ls = NavData.LocationStatus.DR;
						eloc.loc_name = "";
						Log.LogEntry("Expected location: " + eloc.ToString());
						MotionMeasureProb.Move(new MotionMeasureProb.Pose(eloc.coord, eloc.orientation));
						cl = eloc;
						if (rsp.Contains(SharedData.MPU_FAIL))
							{
							SkillShared.OutputSpeech("Attempting to recover from " + SharedData.MPU_FAIL + ". This may take a couple of minutes.");
							if (AutoRobotControl.MotionControl.RestartMC())
								rtn = MoveStatus.SUCCESS;
							}
						}
					NavData.SetCurrentLocation(cl);
					moved_dist = dist;
					}
				}
			else if (rtn != MoveStatus.PATH_CHG)
				SkillShared.OutputSpeech("Insufficient clearence for move.");
			return (rtn);
		}



		private bool MoveToWSLoc(ref ArrayList path,ref Stack rtn_path)

		{
			bool rtn = false;
			int i,pan = 0,moved_dist = 0;
			NavCompute.pt_to_pt_data ppd;
			NavData.location cl;
			ArrayList new_path = new ArrayList();
			MoveStatus ms;
			PersonDetect.scan_data pd = PersonDetect.Empty(),fd = PersonDetect.Empty();

			for (i = 0;i < path.Count;i++)
				{
				if (SkillShared.TurnToFaceMP((Point) path[i]))
					{
					int deter_dist = 0;

					if (i == path.Count - 1)
						{
						cl = NavData.GetCurrentLocation();
						ppd = NavCompute.DetermineRaDirectDistPtToPt(SpeakerData.Person.rm_location, cl.coord);
						pan = NavCompute.AngularDistance(cl.orientation, ppd.direc);
						if (!NavCompute.ToRightDirect(cl.orientation, ppd.direc))
							pan *= -1;
						HeadAssembly.Pan(pan, true);
						if (SkillShared.FindSpeakerFace(pan, ref pd,ref fd))
							{
							SpeakerData.Person = pd;
							if (!fd.detected)
								SpeakerData.FaceClear();
							else
								SpeakerData.Face = fd;
							}
						else
							{
							SpeakerData.ClearPersonFace();
							SkillShared.OutputSpeech("Could not find the speaker.");
							}
						HeadAssembly.Pan(0, true);
						}
					if ((ms = MoveToPt(ref path,i,ref deter_dist,ref moved_dist,pan)) == MoveStatus.SUCCESS)
						{
						if ((i == 0) && (!non_straight_entry))
							rtn_path.Push(SkillShared.wsd.initial_robot_loc.coord);
						else if (i > 0)
							rtn_path.Push((Point) path[i - 1]);
						if (i == path.Count - 1)
							{
							int angle;

							cl = NavData.GetCurrentLocation();
							SkillShared.at_work_loc = true;
							angle = NavCompute.AngularDistance(cl.orientation, SkillShared.wsd.work_loc.orient);
							if (NavCompute.ToRightDirect(cl.orientation, SkillShared.wsd.work_loc.orient))
								angle *= -1;
							if (SkillShared.TurnAngle(angle))
								{
								cl.orientation = SkillShared.wsd.work_loc.orient;
								NavData.SetCurrentLocation(cl);
								}
							SkillShared.OutputSpeech("At the work location.");
							rtn = true;
							}
						}
					else if (ms == MoveStatus.FAIL)
						{
						if (moved_dist > 6)
							{
							if (i == 0)
								rtn_path.Push(SkillShared.wsd.initial_robot_loc.coord);
							else
								rtn_path.Push((Point)path[i - 1]);
							}
						break;
						}
					else if (ms == MoveStatus.PATH_CHG)
						{
						i -= 1;
						}
					}
				else
					break;
				}
			return (rtn);
		}



		private bool NonStraightEntry(ArrayList sdata,int wldirect,int wldist, ref ArrayList path)

		{
			bool rtn = false;
			int mdist,cdist = 0,i;
			Point fmloc = new Point();
			Bitmap bm;
			string fname;
			DateTime now = DateTime.Now;
			Graphics g;

			SkillShared.ScanMap(sdata, SkillShared.wsd.initial_robot_loc);
			bm = SkillShared.MapToBitmap(SkillShared.ws_map);
			g = Graphics.FromImage(bm);
			if ((SkillShared.wsd.initial_robot_loc.orientation > 315) || (SkillShared.wsd.initial_robot_loc.orientation < 45))
				{
				g.FillRectangle(Brushes.Yellow, SkillShared.wsd.initial_robot_loc.coord.X, SkillShared.wsd.initial_robot_loc.coord.Y,1,1);
				if ((wldirect > 180) && (wldirect < 360))
					{
					mdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.FRONT);
					mdist -= (int) Math.Ceiling(SharedData.FRONT_TURN_RADIUS + 1);
					mdist = Math.Min(mdist,24);
					for (i = 1;i < mdist;i++)
						{
						fmloc = new Point(SkillShared.wsd.initial_robot_loc.coord.X, SkillShared.wsd.initial_robot_loc.coord.Y - i);
						g.FillRectangle(Brushes.Yellow, fmloc.X, fmloc.Y, 1, 1);
						cdist = SkillShared.FindMapObstacle(ref SkillShared.ws_map, fmloc, 270, ref bm);
						if (cdist >= wldist)
							break;
						}
					if (cdist < wldist)
						{
						mdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.REAR);
						mdist -= (int)Math.Ceiling(SharedData.REAR_TURN_RADIUS + 1);
						mdist = Math.Min(mdist,24);
						for (i = 1; i < mdist; i++)
							{
							fmloc = new Point(SkillShared.wsd.initial_robot_loc.coord.X, SkillShared.wsd.initial_robot_loc.coord.Y + i);
							g.FillRectangle(Brushes.Blue, fmloc.X, fmloc.Y, 1, 1);
							cdist = SkillShared.FindMapObstacle(ref SkillShared.ws_map, fmloc, 270, ref bm,(int) (2 * Math.Ceiling(SharedData.ROBOT_LENGTH - SharedData.FRONT_PIVOT_PT_OFFSET)));
							if (cdist >= wldist)
								break;
							}
						}
					if (cdist > wldist)
						{
						Point np;
						int dist,direct;
						Room.rm_location rl;

						rtn = true;
						path.Add(fmloc);
						np = new Point((fmloc.X + SkillShared.wsd.work_loc.coord.X) / 2, fmloc.Y);
						path.Add(np);
						dist = NavCompute.DistancePtToPt(SkillShared.wsd.work_loc.coord, np);
						if (dist > SkillShared.MAX_FINAL_MOVE_DIST)
							{
							direct = NavCompute.DetermineHeadingPtToPt(SkillShared.wsd.work_loc.coord, np);
							rl = NavCompute.PtDistDirectApprox(np, direct, dist - SkillShared.MAX_FINAL_MOVE_DIST);
							path.Add(rl.coord);
							}
						path.Add(SkillShared.wsd.work_loc.coord);
						non_straight_entry = true;
						}
					fname = Log.LogDir() + "non-straight entry map " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".bmp";
					bm.Save(fname);
					}
				}
			else
				Log.LogEntry("Unimplenented non-straight move sector");
			return(rtn);
		}



		public bool PossibleAnomalies(ArrayList obs,ArrayList sdata)

		{
			const int MAX_ANOMALY_COUNT = 3;
			const int MIN_ANOMALY_DIST = 12;
			int i,j,span,indx,idx;
			SkillShared.lidar_obstacle[] ld;
			bool rtn = true;
			double xi,yi,xo,yo;
			Rplidar.scan_data sd;

			if (obs.Count <= MAX_ANOMALY_COUNT)
				{
				ld = new SkillShared.lidar_obstacle[obs.Count];
				for (i = 0;i < obs.Count;i++)
					{
					ld[i] = (SkillShared.lidar_obstacle) obs[i];
					yi = ld[i].sd.dist * Math.Cos(ld[i].sd.angle * SharedData.DEG_TO_RAD);
					xi = ld[i].sd.dist * Math.Sin(ld[i].sd.angle * SharedData.DEG_TO_RAD);
					span = (int) Math.Ceiling(Math.Atan(MIN_ANOMALY_DIST/ ld[i].sd.dist) * SharedData.RAD_TO_DEG);
					indx = ld[i].indx;
					for (j = -span;j <= span;j++)
						{
						if (j != 0)
							{
							idx = (indx + j) % sdata.Count;
							if (idx < 0)
								idx += sdata.Count;
							sd = (Rplidar.scan_data) sdata[idx];
							yo = sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD);
							xo = sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD);
							if (Math.Sqrt(Math.Pow(yi-yo,2) + Math.Pow(xi-xo,2)) < MIN_ANOMALY_DIST)
								{
								rtn = false;
								break;
								}
							}
						}
					}
				}
			else
				rtn = false;
			return(rtn);
		}



		private bool ConventionalMove(ref Stack rtn_path,ref Stack final_adjust)

		{
			bool rtn = false;	
			NavData.location cl;														
																							
			SkillShared.OutputSpeech("Starting move to work location.");
			cl = NavData.GetCurrentLocation();
			if (SkillShared.DirectMove(SkillShared.wsd.work_loc.coord,false))
				{
				rtn_path.Push(cl.coord);
				// "final adjustment" to get the robot as close as possible
				}
			return(rtn);
		}



		public bool Move(ref ArrayList path,ref Stack rtn_path,ref Stack final_adjust)

		{
			bool rtn = false,move_ok = false;
			NavCompute.pt_to_pt_data ppd;
			int angle,cangle = 0,direct,i,mdist = 0,kmdist = 0,sangle,oremove = 0,dist,no_obs = 0;
			ArrayList sdata= new ArrayList(),obs = new ArrayList();
			Room.rm_location rl;
			Graphics g;
			Point pt;
			double y,dy;
			SkillShared.lidar_obstacle lo;
			string reply,msg;

			if (SkillShared.wsd.tight_quarters)
				{
				SkillShared.OutputSpeech("Determining path to work space.");

				if (Rplidar.CaptureScan(ref sdata, true))
					{
					Rplidar.SaveLidarScan(ref sdata, "Move to work space path LIDAR scan");
					ppd = NavCompute.DetermineRaDirectDistPtToPt(SkillShared.wsd.work_loc.coord, SkillShared.wsd.initial_robot_loc.coord);
					angle = NavCompute.AngularDistance(SkillShared.wsd.initial_robot_loc.orientation, ppd.direc);
					if (!NavCompute.ToRightDirect(SkillShared.wsd.initial_robot_loc.orientation, ppd.direc))
						angle *= -1;
					mdist = SkillShared.FindObstacles(angle, ppd.dist, sdata, 1, ref obs);
					if (obs.Count > 0)
						{
						if ((mdist >= SkillShared.MIN_KINECT_LIDAR_FLOOR_DIST) && PossibleAnomalies(obs,sdata))
							{
							HeadAssembly.Pan(angle,true);
							SkillShared.KinectFrontClear(ppd.dist, ref kmdist,ref no_obs,true);
							HeadAssembly.Pan(0,true);
							dy = ((SharedData.FRONT_PIVOT_PT_OFFSET + SharedData.FLIDAR_OFFSET) * Math.Cos(angle * SharedData.DEG_TO_RAD)) - SharedData.FRONT_PIVOT_PT_OFFSET;
							for (i = obs.Count - 1;i >= 0;i--)
								{
								lo = (SkillShared.lidar_obstacle) obs[i];
								sangle = (lo.sd.angle - angle) % 360;
								if (sangle < 0)
									sangle += 360;
								y = lo.sd.dist * Math.Cos(sangle * SharedData.DEG_TO_RAD) + dy;
								if ((y > SkillShared.MIN_KINECT_LIDAR_FLOOR_DIST) && (y < kmdist))
									{
									sdata.Remove(lo.sd);
									obs.RemoveAt(i);
									oremove += 1;
									}
								}
							if (oremove > 0)
								{
								mdist = ppd.dist + 1;
								foreach (Rplidar.scan_data sdt in obs)
									{
									sangle = (sdt.angle - angle) % 360;
									if (sangle < 0)
										sangle += 360;
									y = sdt.dist * Math.Cos(sangle * SharedData.DEG_TO_RAD) + SharedData.FLIDAR_OFFSET;
									if (y < mdist)
										mdist = (int) Math.Round(y);
									}
								}
							Log.LogEntry(oremove + " obs removed, revised LIDAR min obs dist of " + mdist);
							mdist = Math.Min(mdist,kmdist);
							}
						else if (obs.Count < 5)	//what is a good number???
							{
							msg = obs.Count + " possible LIDAR anomalies have been detected " + mdist + " inches ahead on the path to the work space.  Is my path clear?";
							reply = AutoRobotControl.Speech.Conversation(msg, "responseyn", 5000, true);
							if (reply == "yes")
								{
								mdist = ppd.dist + 1;
								}
							}
						if (mdist >= ppd.dist)
							{
							move_ok = true;
							if (mdist > SkillShared.MAX_FINAL_MOVE_DIST)
								{
								rl = NavCompute.PtDistDirectApprox(SkillShared.wsd.initial_robot_loc.coord,ppd.direc, mdist - SkillShared.MAX_FINAL_MOVE_DIST);
								path.Add(rl.coord);
								path.Add(SkillShared.wsd.work_loc.coord);
								}
							else
								path.Add(SkillShared.wsd.work_loc.coord);
							}
						if (!move_ok)
							{
							if (SkillShared.ObstacleAdjustAngle(sdata,obs,ppd.dist,angle,1,ref cangle))
								{
								if ((SkillShared.wsd.side == SharedData.RobotLocation.LEFT) && (cangle > 0))
									{	//when should this be changed to two move path??
									direct = ppd.direc - cangle;
									if (direct < 0)
										direct += 360;
									dist = (int)Math.Round(SkillShared.wsd.front_edge_dist - SharedData.ARM_PERCH_OFFSET - 1);
									if (dist > SkillShared.MAX_FINAL_MOVE_DIST)
										{
										rl = NavCompute.PtDistDirectApprox(SkillShared.wsd.initial_robot_loc.coord, direct, dist - SkillShared.MAX_FINAL_MOVE_DIST);
										path.Add(rl.coord);
										rl = NavCompute.PtDistDirectApprox(rl.coord, direct, SkillShared.MAX_FINAL_MOVE_DIST);
										path.Add(rl.coord);
										}
									else
										{
										rl = NavCompute.PtDistDirectApprox(SkillShared.wsd.initial_robot_loc.coord, direct,dist);
										path.Add(rl.coord);
										}
									move_ok = true;
									}
								else if ((SkillShared.wsd.side == SharedData.RobotLocation.RIGHT) && (cangle < 0))
									{	//when should this be changed to two move path??
									direct = (ppd.direc - cangle) % 360;
									dist = (int)Math.Round(SkillShared.wsd.front_edge_dist - SharedData.ARM_PERCH_OFFSET - 1);
									if (dist > SkillShared.MAX_FINAL_MOVE_DIST)
										{
										rl = NavCompute.PtDistDirectApprox(SkillShared.wsd.initial_robot_loc.coord, direct, dist - SkillShared.MAX_FINAL_MOVE_DIST);
										path.Add(rl.coord);
										rl = NavCompute.PtDistDirectApprox(rl.coord, direct, SkillShared.MAX_FINAL_MOVE_DIST);
										path.Add(rl.coord);
										}
									else
										{
										rl = NavCompute.PtDistDirectApprox(SkillShared.wsd.initial_robot_loc.coord, direct, dist);
										path.Add(rl.coord);
										}
									move_ok = true;
									}
								else
									{
									move_ok = SkillShared.TwoStepPlan(ppd.direc,cangle,ref path);
									}
								}
							else if (NonStraightEntry(sdata,ppd.direc,ppd.dist,ref path))
								{
								move_ok = true;
								}
							}
						}
					else
						{
						if (mdist > SkillShared.MAX_FINAL_MOVE_DIST)
							{
							rl = NavCompute.PtDistDirectApprox(SkillShared.wsd.initial_robot_loc.coord, ppd.direc, mdist - SkillShared.MAX_FINAL_MOVE_DIST);
							path.Add(rl.coord);
							rl = NavCompute.PtDistDirectApprox(rl.coord,ppd.direc, SkillShared.MAX_FINAL_MOVE_DIST);
							path.Add(rl.coord);
							}
						else
							path.Add(SkillShared.wsd.work_loc.coord);
						move_ok = true;
						}
					if (move_ok)
						{
						Log.LogArrayList("Move path: ", path);
						if (SharedData.log_operations && (WorkAssist.bmap != null))
							{
							g = Graphics.FromImage(WorkAssist.bmap);
							for (i = 0;i < path.Count;i++)
								{
								pt = (Point) path[i];
								g.FillEllipse(Brushes.Green, pt.X - 1, pt.Y - 1, 2, 2);
								}
							}
						SkillShared.OutputSpeech("Path determined. Starting move to work location.");
						if (MoveToWSLoc(ref path,ref rtn_path))
							{
							rtn = FinalAdjustment(ref final_adjust);
							}
						}
					else
						SkillShared.OutputSpeech("Could not find path to work location.");
					}
				else
					SkillShared.OutputSpeech("Could not capture LIDAR scan.  Attempt to move to the work location failed.");
				}
			else
				rtn = ConventionalMove(ref rtn_path, ref final_adjust);
			return (rtn);
		}

		}
	}
