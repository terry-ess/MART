using System;
using System.Collections;
using System.Drawing;
using AutoRobotControl;

namespace Room_Survey
	{
	static class Move
		{
		
		private const double MIN_RETURN_FORWARD_DIST_RATIO = .75;
		private const int MIN_SIDE_CLEAR = 1;

		public struct obs_data
		{
			public bool found;
			public int mod;
			public ArrayList obs;

			public obs_data(bool fill)

			{
				found = false;
				mod = -1;
				obs = new ArrayList();
			}
		};


		public static bool MoveForward(int dist,int clear,ref obs_data odata)

		{
			bool rtn = false;
			int tc,mov_dist,sdist,modist,kodist,mmodist;
			string rply;
			MotionMeasureProb.Pose  ecloc;
			double min_detect_dist = -1;

			Log.LogEntry("MoveForward " + dist + "  " + clear);
			odata.found = false;
			odata.obs.Clear();
			sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.FRONT);
			modist = SurveyCompute.FindObstacles(0, dist + clear, SkillShared.sdata, ref odata.obs);
			if ((sdist < modist) && (sdist < dist + clear))
				{
				kodist = (int)Math.Round(KinectExt.FindObstacles(modist, SkillShared.STD_SIDE_CLEAR, ref min_detect_dist));
				Log.LogEntry("Kinect min obstacle distance - " + kodist + "   min detect distance - " + min_detect_dist);
				if (kodist > sdist)
					mmodist = Math.Min(kodist, modist);
				else
					mmodist = Math.Min(sdist, modist);
				}
			else
				mmodist = Math.Min(sdist, modist);
			if (SkillShared.run && (mmodist >= dist + clear))
				{
				if (odata.obs.Count == 0)
					{
					tc = (int)(((dist / 7.2) + 2) * 100);
					rply = SkillShared.SSendCommand(SharedData.FORWARD + " " + dist, tc);
					if (rply.StartsWith("ok") || rply.Contains(SharedData.INSUFFICENT_FRONT_CLEARANCE))
						{
						rply = SkillShared.SendCommand(SharedData.DIST_MOVED);
						if (rply.StartsWith("ok"))
							mov_dist = int.Parse(rply.Substring(3));
						else
							mov_dist = -1;
						ecloc = SkillShared.ccpose;
						if (mov_dist != -1)
							{
							ecloc.coord = NavCompute.MapPoint(new Point(0, mov_dist), ecloc.orient, SkillShared.ccpose.coord,false);
							}
						else
							{
							ecloc.coord = NavCompute.MapPoint(new Point(0, dist), ecloc.orient, SkillShared.ccpose.coord,false);
							}
						Log.LogEntry("Expected pose: " + ecloc.ToString());
						SkillShared.ccpose = ecloc;
						SkillShared.sdata.Clear();
						Rplidar.CaptureScan(ref SkillShared.sdata, true);
						rtn = true;
						}
					else
						Log.LogEntry("Forward move failed.");
					}
				else
					{
					odata.found = true;
					odata.mod = mmodist;
					Log.LogEntry("Forward obstacle found. Move cancelled.");
					}
				}
			else if (SkillShared.run)
				{
				odata.found = true;
				odata.mod = mmodist;
				Log.LogEntry("Forward obstacle found. Move cancelled.");
				}
			return (rtn);
		}



		private static bool RecoverUnsafeTurn(int direct,int mod,int angle,int modd)

		{
			bool rtn = false,turned = false;
			int ta1,ta2,nmod = 0,nmodd = 0,tangle = 0,direc,bdist,sdist;
			double cdist;
			Room rm = SharedData.current_rm;

			Log.LogEntry("RecoverUnsafeTurn " + mod + "  " + angle + "  " + modd);
			ta1 = angle + 180;
			if (ta1 > 180)
				ta1 -= 360;
			ta2 = -modd;
			if (ta2 < -180)
				ta2 += 360;
			if ((ta1 == 0) || Turn.TurnSafe(ta1,ref nmod,ref nmodd))
				{
				if (ta1 == 0)
					{
					turned = true;
					tangle = 0;
					}
				else if ((turned = Turn.TurnAngle(ta1)))
					{
					direc = (SkillShared.ccpose.orient - ta1) % 360;
					if (direc < 0)
						direc += 360;
					SkillShared.ccpose.orient = direc;
					SkillShared.ccpose.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(ta1), SkillShared.ccpose.orient, SkillShared.ccpose.coord,false);
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
					direc = (SkillShared.ccpose.orient - ta2) % 360;
					if (direc < 0)
						direc += 360;
					SkillShared.ccpose.orient = direc;
					SkillShared.ccpose.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(ta2), SkillShared.ccpose.orient, SkillShared.ccpose.coord,false);
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
				sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.REAR);
				if (sdist > bdist)
					{
					if (MoveBack(bdist))
						{
						rtn = TurnToFaceDirect(direct);
						}
					else
						{
						Log.LogEntry("Backward move failed.");
						}
					}
				else
					Log.LogEntry("Can not make required backward move.");
				}
			else
				{
				Log.LogEntry("RecoverUnsafeTurn failed.");
				}				
			return(rtn);
		}



		public static bool TurnToFaceDirect(int direct)

		{
			bool rtn = false,turn_safe;
			int angle;
			int mod = 0, modd = 0;

			Log.LogEntry("TurnToFaceDirect " + direct);
			angle = NavCompute.AngularDistance(SkillShared.ccpose.orient,direct);
			if (NavCompute.ToRightDirect(SkillShared.ccpose.orient,direct))
				angle *= -1;
			if (Math.Abs(angle) >= SharedData.MIN_TURN_ANGLE)
				{
				if (!(turn_safe = Turn.TurnSafe(angle, ref mod, ref modd)))
					{
					int org_angle, org_modd, org_mod;

					if (Math.Abs(angle) > 135)
						{
						org_angle = angle;
						org_modd = modd;
						org_mod = mod;
						if (angle < 0)
							angle += 360;
						else
							angle -= 360;
						if (!(turn_safe = Turn.TurnSafe(angle, ref mod, ref modd)))
							if (SharedData.front_lidar_operational)
								rtn = RecoverUnsafeTurn(direct, org_mod, org_angle, org_modd);
						}
					else if (SharedData.front_lidar_operational)
						rtn = RecoverUnsafeTurn(direct, mod, angle, modd);
					}
				if (turn_safe)
					{
					rtn = Turn.TurnAngle(angle);
					if (rtn)
						{
						SkillShared.ccpose.orient = direct;
						SkillShared.ccpose.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(angle), direct, SkillShared.ccpose.coord,false);
						SkillShared.sdata.Clear();
						Rplidar.CaptureScan(ref SkillShared.sdata, true);
						}
					else if ((Turn.LastError() == SharedData.MotionErrorType.TURN_NOT_SAFE) && (Math.Abs(angle) > 135))
						{
						if (angle < 0)
							angle += 360;
						else
							angle -= 360;
						if (Turn.TurnAngle(angle))
							{
							SkillShared.ccpose.orient = direct;
							SkillShared.ccpose.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(angle), direct, SkillShared.ccpose.coord, false);
							SkillShared.sdata.Clear();
							Rplidar.CaptureScan(ref SkillShared.sdata, true);
							}
						else
							Log.LogEntry("Attempt to TurnToFaceDirect failed.");
						}
					else
						Log.LogEntry("Attempt to TurnToFaceDirect failed.");
					}
				}
			else
				rtn = true;
			return (rtn);
		}



		public static bool TurnToFaceDirect(int direct,ref int dist)

		{
			bool rtn = false;
			MotionMeasureProb.Pose start,end;
			int ddist;

			start = SkillShared.ccpose;
			if (TurnToFaceDirect(direct))
				{
				rtn = true;
				end = SkillShared.ccpose;
				ddist = NavCompute.DistancePtToPt(start.coord,end.coord);
				dist -= ddist;
				Log.LogEntry("Revised distance: " + dist);
				}
			return (rtn);
		}


		public static bool TurnToAngle(int angle)

		{
			bool rtn = false;
			int direct;

			rtn = Turn.TurnAngle(angle);
			if (rtn)
				{
				direct = (SkillShared.ccpose.orient - angle) % 360;
				if (direct < 0)
					direct += 360;
				SkillShared.ccpose.orient = direct;
				SkillShared.ccpose.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(angle), direct, SkillShared.ccpose.coord, false);
				Log.LogEntry("Expected pose: " + SkillShared.ccpose.ToString());
				}
			else
				Log.LogEntry("Attempt to TurnToAngle failed.");
			return (rtn);
		}


		public static bool MoveBack(int dist)

		{
			bool rtn = false;
			int tc,mov_dist,sdist;
			double mclear = 0;
			string rply;
			MotionMeasureProb.Pose ecloc;

			Log.LogEntry("MoveBack " + dist);
			sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.REAR);
			if (SharedData.rear_lidar_operational)
				{
				if (LS02CLidar.RearClearence(ref mclear, SharedData.ROBOT_WIDTH + (2 * MIN_SIDE_CLEAR)))
					{
					dist = (int)Math.Round(mclear);
					sdist = Math.Min(sdist, dist);
					}
				}
			if (sdist >= dist + SharedData.REAR_SONAR_MIN_CLEARANCE)
				{
				tc = (int)(((dist / 7.2) + 2) * 100);
				rply = SkillShared.SSendCommand(SharedData.BACKWARD_SLOW + " " + dist, tc);
				if (rply.StartsWith("ok") || rply.Contains(SharedData.INSUFFICENT_REAR_CLEARANCE))
					{
					rply = SkillShared.SendCommand(SharedData.DIST_MOVED);
					if (rply.StartsWith("ok"))
						mov_dist = int.Parse(rply.Substring(3));
					else
						mov_dist = -1;
					ecloc = SkillShared.ccpose;
					if (mov_dist != -1)
						ecloc.coord = NavCompute.MapPoint(new Point(0, -mov_dist), ecloc.orient, SkillShared.ccpose.coord, false);
					else
						ecloc.coord = NavCompute.MapPoint(new Point(0, -dist), ecloc.orient, SkillShared.ccpose.coord, false);
					Log.LogEntry("Expected pose: " + ecloc.ToString());
					SkillShared.ccpose = ecloc;
					SkillShared.sdata.Clear();
					Rplidar.CaptureScan(ref SkillShared.sdata, true);
					rtn = true;
					}
				else
					SkillShared.OutputSpeech("Backward move failed.", true);
				}
			else
				Log.LogEntry("Insufficient rear clearence to move " + dist + " in. backware.");
			return (rtn);
		}



		public static bool MoveToNextPt(int direct,int dist,int clear)

		{
			bool rtn = false;
			obs_data odata = new obs_data(true);

			Log.LogEntry("MoveToNextPt " + direct + "  " + dist + "  " + clear);
			if (TurnToFaceDirect(direct))
				{
				if (!(rtn = MoveForward(dist, clear,ref odata)))
					Log.LogEntry("Attempt to move to movement point failed.");
				}
			else
				Log.LogEntry("Attempt to turn to face movement point failed.");
			return(rtn);
		}



		public static bool ReturnToNextPt(int direct, int dist,int clear)

		{
			bool rtn = false;

			obs_data odata = new obs_data(true);

			Log.LogEntry("ReturnToNextPt " + direct + "  " + dist + "  " + clear);
			if (TurnToFaceDirect(direct))
				{
				if (!(rtn = MoveForward(dist, clear, ref odata)))
					Log.LogEntry("Attempt to move to movement point failed.");
				}
			else if (Turn.LastError() == SharedData.MotionErrorType.TURN_NOT_SAFE)
				{
				if (MoveBack(dist))
					rtn = true;
				else
					Log.LogEntry("Attempt to move backward failed.");
				}
			else
				Log.LogEntry("Attempt to turn to face movement point failed.");
			return (rtn);
		}




		private static bool PathAdjust(obs_data odata,int dist,int clear)

		{
			bool rtn = false;
			int angle = -1,modist;
			ArrayList obs = new ArrayList();
			const int PATH_ADJUST_LIMIT = 10;

			if (SurveyCompute.ObstacleAdjustAngle(SkillShared.sdata,odata.obs,dist + clear,0,ref angle))
				{
				modist = SurveyCompute.FindObstacles(-angle,-1,SkillShared.sdata,ref obs);
				if (modist - clear >= MIN_RETURN_FORWARD_DIST_RATIO * dist)
					{
					if (Math.Abs(angle) < SharedData.MIN_TURN_ANGLE)
						if (angle > 0)
							angle = SharedData.MIN_TURN_ANGLE;
						else
							angle = -SharedData.MIN_TURN_ANGLE;
					if (Math.Abs(angle) <= PATH_ADJUST_LIMIT)
						{
						if (Turn.TurnAngle(angle))
							{
							angle = (SkillShared.ccpose.orient - angle) % 360;
							if (angle < 0)
								angle += 360;
							SkillShared.ccpose.orient = angle;
							SkillShared.sdata.Clear();
							Rplidar.CaptureScan(ref SkillShared.sdata, true);
							obs.Clear();
							modist = SurveyCompute.FindObstacles(0, -1, SkillShared.sdata, ref obs);
							if (modist - clear  >= MIN_RETURN_FORWARD_DIST_RATIO * dist)
								rtn = true;
							else
								Log.LogEntry("Obstacle distance of " + modist + " is less then desired obstacle clearence.");
							}
						else
							Log.LogEntry("Attempt to make adjustment turn failed.");
						}
					else
						Log.LogEntry("Adjust turn angle of " + angle + " exceeds limit of " + PATH_ADJUST_LIMIT + ".");

					}
				}
			return(rtn);
		}



		public static bool MoveToFinalRpathPt(int direct,int dist,int clear)

		{
			bool rtn = false,ready_to_move = false;
			ArrayList obs = new ArrayList();
			int modist,angle = -1,adjusts = 0;
			obs_data odata = new obs_data(true);
			int dist_adjust;
			const int ADJUST_TRIES_LIMIT = 5;

			if (TurnToFaceDirect(direct, ref dist))
				{
				dist_adjust = (int) Math.Ceiling(dist * .1);
				modist = SurveyCompute.FindObstacles(0,dist + clear, SkillShared.sdata, 7, ref obs);
				if (obs.Count > 0)
					{
					do
						{
						if (dist <= dist_adjust)
							break;
						if (SurveyCompute.ObstacleAdjustAngle(SkillShared.sdata,obs, dist + clear,0, 7, ref angle))
							{
							if ((angle != 0) && (Math.Abs(angle) < SharedData.MIN_TURN_ANGLE))
								if (angle < 0)
									angle = -SharedData.MIN_TURN_ANGLE;
								else
									angle = SharedData.MIN_TURN_ANGLE;
							if (TurnToAngle(angle))
								{
								obs.Clear();
								SkillShared.sdata.Clear();
								Rplidar.CaptureScan(ref SkillShared.sdata,true);
								SurveyCompute.FindObstacles(0, dist + clear, SkillShared.sdata, 7, ref obs);
								if (obs.Count == 0)
									ready_to_move = true;
								else
									dist -= dist_adjust;
								}
							else
								{
								Log.LogEntry("Could not make adjustment turn for final reverse path point.");
								break;
								}
							}
						else
							{
							dist -= dist_adjust;
							obs.Clear();
							SurveyCompute.FindObstacles(0, dist + clear, SkillShared.sdata, 7, ref obs);
							if (obs.Count == 0)
								ready_to_move = true;
							}
						adjusts += 1;
						}
					while (!ready_to_move && (adjusts < ADJUST_TRIES_LIMIT));
					}
				else
					ready_to_move = true;
				if (ready_to_move)
					{
					if (MoveForward(dist,clear,ref odata))
						rtn = true;
					else
						Log.LogEntry("Attempt to move to final reverse path point failed.");
					}
				else
					Log.LogEntry("Could not clear obstacles.");
				}
			else
				Log.LogEntry("Could not turn to face final reverse path point.");
			return (rtn);
		}



		public static bool MoveToNextRPathPt(Point npt, int clear,ref bool obstacle)

		{
			bool rtn = false,done = false;
			obs_data odata = new obs_data(true);
			int modist,dist,angle = 0,i,tangle,fmoves = 0,bmoves = 0;
			ArrayList obs = new ArrayList();
			SurveyCompute.pt_to_pt_data ppd;
			const int MIN_MOVE_DIST = 6;
			const int PATH_ADJUST_LIMIT = 5;
			const int SIDE_CLEARANCE = 7;

			Log.LogEntry("Move reverse path " + npt + "  " + clear);
			obstacle = false;
			ppd = SurveyCompute.DetermineDirectDistPtToPt(npt, SkillShared.ccpose.coord);
			tangle = NavCompute.AngularDistance(ppd.direc,SkillShared.ccpose.orient);
			if (tangle > 140)
				done = true;
			else
				{
				if (!NavCompute.ToRightDirect(SkillShared.ccpose.orient,ppd.direc))
					tangle *= -1;
				obs.Clear();
				modist = SurveyCompute.FindObstacles(tangle,ppd.dist + clear, SkillShared.sdata,SIDE_CLEARANCE, ref obs);
				if (obs.Count > 0)
					{
					if (modist - clear >= MIN_RETURN_FORWARD_DIST_RATIO * ppd.dist)
						done = true;
					else if (SurveyCompute.ObstacleAdjustAngle(SkillShared.sdata,obs, ppd.dist + clear,tangle,SIDE_CLEARANCE,ref angle))
						{
						if (Math.Abs(angle) < PATH_ADJUST_LIMIT)
							{
							ppd.direc = (ppd.direc - angle) % 360;
							if (ppd.direc < 0)
								ppd.direc += 360;
							done = true;
							}
						else
							{
							obs.Clear();
							modist = SurveyCompute.FindObstacles(tangle, (int)Math.Round(MIN_RETURN_FORWARD_DIST_RATIO * ppd.dist + clear), SkillShared.sdata, 7, ref obs);
							if (obs.Count > 0)
								{
								if (SurveyCompute.ObstacleAdjustAngle(SkillShared.sdata,obs, ppd.dist + clear, tangle,SIDE_CLEARANCE, ref angle))
									if (Math.Abs(angle) < PATH_ADJUST_LIMIT)
										{
										ppd.direc = (ppd.direc - angle) % 360;
										if (ppd.direc < 0)
											ppd.direc += 360;
										done = true;
										}
								}
							else
								done = true;
							}
						}
					else
						{
						obs.Clear();
						modist = SurveyCompute.FindObstacles(tangle, (int)Math.Round(MIN_RETURN_FORWARD_DIST_RATIO * ppd.dist + clear), SkillShared.sdata, 7, ref obs);
						if (obs.Count > 0)
							{
							if (SurveyCompute.ObstacleAdjustAngle(SkillShared.sdata,obs, ppd.dist + clear, tangle,SIDE_CLEARANCE, ref angle))
								if (Math.Abs(angle) < PATH_ADJUST_LIMIT)
									{
									ppd.direc = (ppd.direc - angle) % 360;
									if (ppd.direc < 0)
										ppd.direc += 360;
									done = true;
									}
							}
						else
							done = true;
						}
					if (!done && ((Math.Abs(tangle) >= 80) && (Math.Abs(tangle) <= 100)))
						{
						dist = MIN_MOVE_DIST;
						for (i = 0;i < 6;i++)
							{
							switch (i)
								{
								case 0:
									if (Move.MoveForward(dist, SharedData.FRONT_SONAR_CLEARANCE, ref odata))
										fmoves += 1;
									break;

								case 1:
									if ((fmoves == 1) && Move.MoveForward(dist, SharedData.FRONT_SONAR_CLEARANCE, ref odata))
										fmoves += 1;
									break;

								case 2:
									if ((fmoves == 2) && Move.MoveForward(dist, SharedData.FRONT_SONAR_CLEARANCE, ref odata))
										fmoves += 1;
									break;

								case 3:
									if (Move.MoveBack(dist * (fmoves + 1)))
										bmoves += 1;
									break;

								case 4:
									if ((bmoves == 1) && Move.MoveBack(dist))
										bmoves += 1;
									break;

								case 5:
									if ((bmoves == 2) && Move.MoveBack(dist))
										bmoves += 1;
									break;
								}
							obs.Clear();
							modist = SurveyCompute.FindObstacles(tangle, -1, SkillShared.sdata,SIDE_CLEARANCE, ref obs);
							if (modist - clear >= MIN_RETURN_FORWARD_DIST_RATIO * ppd.dist)
								{
								done = true;
								break;
								}
							else if ((i == 2) || (i == 5))
								{
								double dtan,sclear;

								dtan = Math.Tan(MotionMeasureProb.MoveDriftLimit() * SharedData.DEG_TO_RAD);
								sclear = Math.Ceiling(SkillShared.STD_SIDE_CLEAR + (ppd.dist * dtan));
								modist = SurveyCompute.FindObstacles(tangle, -1, SkillShared.sdata,sclear, ref obs);
								if (modist - clear >= MIN_RETURN_FORWARD_DIST_RATIO * ppd.dist)
									{
									done = true;
									break;
									}
								}
							}
						}
					}
				else
					done = true;
				}
			if (done)
				{
				if (TurnToFaceDirect(ppd.direc))
					{
					dist = NavCompute.DistancePtToPt(npt,SkillShared.ccpose.coord);
					if (dist > MIN_MOVE_DIST)
						{
						if (!(rtn = MoveForward(dist, clear, ref odata)))
							{
							if (odata.found)
								{
								obs.Clear();
								modist = odata.mod;
								if (modist - clear >= MIN_RETURN_FORWARD_DIST_RATIO * dist)
									{
									dist = modist - clear - 2;
									if (!(rtn = MoveForward(dist,clear,ref odata)))
										Log.LogEntry("Attempt to move to reverse path point failed.");
									else
										rtn = true;
									}
								else if (PathAdjust(odata, dist,clear))
									{
									if (!(rtn = MoveForward(dist, clear, ref odata)))
										Log.LogEntry("Attempt to move to reverse path point failed.");
									else
										rtn = true;
									}
								else
									{
									obstacle = true;
									Log.LogEntry("Attempt to move to reverse path point failed due to obstacle.");
									}
								}
							else
								Log.LogEntry("Attempt to move to reverse path point failed.");
							}
						else
							rtn = true;
						}
					else
						rtn = true;
					}
				else
					Log.LogEntry("Attempt to turn to face reverse path point failed.");
				}
			else
				Log.LogEntry("Could not make move to reverse path point.");
			return (rtn);
		}




		public static bool MoveBackFromEntryPt()

		{
			bool rtn = false;
			NavData.location cloc;
			int sdist,tc,dist;
			int mb_dist = (int) Math.Ceiling(SharedData.REAR_TURN_RADIUS + 1);
			string rply;
			double mclear = 0;

			sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.REAR);
			if (SharedData.rear_lidar_operational)
				{
				if (LS02CLidar.RearClearence(ref mclear, SharedData.ROBOT_WIDTH + (2 * MIN_SIDE_CLEAR)))
					{
					dist = (int)Math.Round(mclear);
					sdist = Math.Min(sdist, dist);
					}
				}
			if (sdist >= mb_dist + SharedData.ROBOT_LENGTH)
				{
				tc = (int)(((mb_dist / 7.2) + 2) * 100);
				rply = SkillShared.SSendCommand(SharedData.BACKWARD_SLOW + " " + mb_dist, tc);
				if (rply.StartsWith("ok") || rply.Contains(SharedData.INSUFFICENT_REAR_CLEARANCE))
					{
					cloc = NavData.GetCurrentLocation();
					cloc.coord = NavCompute.MapPoint(new Point(0,-mb_dist),cloc.orientation,cloc.coord);
					cloc.ls = NavData.LocationStatus.DR;
					NavData.SetCurrentLocation(cloc);
					rtn = true;
					}
				else
					Log.LogEntry("MoveBackFromEntryPt motion command failed.");
				}
			else
				Log.LogEntry("MoveBackFromEntryPt insufficient distance for move " + sdist);
			return (rtn);
		}



		public static bool MoveForwardSlow(int dist,int clear)

		{
			bool rtn = false;
			string rply;
			int mov_dist,sdist;
			MotionMeasureProb.Pose ecloc;

			sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.FRONT);
			if (sdist >= dist + clear)
				{
				rply = SkillShared.SSendCommand(SharedData.FORWARD_SLOW + " " + dist, 10000);
				if (rply.StartsWith("ok") || rply.Contains(SharedData.INSUFFICENT_FRONT_CLEARANCE))
					{
					rply = SkillShared.SendCommand(SharedData.DIST_MOVED);
					if (rply.StartsWith("ok"))
						mov_dist = int.Parse(rply.Substring(3));
					else
						mov_dist = -1;
					ecloc = SkillShared.ccpose;
					if (mov_dist != -1)
						ecloc.coord = NavCompute.MapPoint(new Point(0, mov_dist), ecloc.orient, SkillShared.ccpose.coord, false);
					else
						ecloc.coord = NavCompute.MapPoint(new Point(0, dist), ecloc.orient, SkillShared.ccpose.coord, false);
					Log.LogEntry("Expected pose: " + ecloc.ToString());
					SkillShared.ccpose = ecloc;
					SkillShared.sdata.Clear();
					Rplidar.CaptureScan(ref SkillShared.sdata, true);
					rtn = true;
					}
				else
					Log.LogEntry("Forward move failed.");
				}
			else
				Log.LogEntry("Insufficant distance for move");
			return (rtn);
		}

		}
	}
