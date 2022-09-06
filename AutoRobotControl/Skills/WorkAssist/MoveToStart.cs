using System;
using System.Collections;
using System.Drawing;
using AutoRobotControl;


namespace Work_Assist
	{
	class MoveToStart
		{

		// primary assumptions: 
		//		1. See MoveToWorkSpace
		//		2. At the work location we are sometimes too close to the work space to safely turn to return. LIDAR or SONAR may not detect that. (ok assumption)


		private Move mov = new AutoRobotControl.Move();


		private bool MoveBackwardToPoint(Point mp)

		{
			int sdist,ldist,dist;
			double cdist = 0;
			NavData.location cl;
			bool rtn = false;

			cl = NavData.GetCurrentLocation();
			dist = NavCompute.DistancePtToPt(cl.coord,mp) + SharedData.ROBOT_LENGTH;
			sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.REAR);
			if (LS02CLidar.RearClearence(ref cdist,SharedData.ROBOT_WIDTH +  (2 * SkillShared.MIN_SIDE_CLEAR)))
				{
				ldist = (int) Math.Round(cdist);
				sdist = Math.Min(sdist,ldist);
				}
			if (sdist > dist)
				rtn = SkillShared.MoveBackward(dist,cl);
			else
				Log.LogEntry("MoveBackwardToPt has insufficent rear clearence of " + sdist + " in.");
			return(rtn);
		}



		public bool TurnToFaceBackwardMP(Point mp)

		{
			bool rtn = false;
			NavCompute.pt_to_pt_data ppd;
			NavData.location cl;
			int angle;

			cl = NavData.GetCurrentLocation();
			ppd = NavCompute.DetermineRaDirectDistPtToPt(mp,cl.coord);
			angle = NavCompute.AngularDistance((ppd.direc + 180) % 360,cl.orientation);
			if (NavCompute.ToRightDirect(cl.orientation,(ppd.direc + 180) % 360))
				angle *= -1;
			if (Math.Abs(angle) >= SharedData.MIN_TURN_ANGLE)
				{
				rtn = Turn.TurnAngle(angle);
				if (rtn)
					{
					cl.orientation = (ppd.direc + 180) % 360;
					cl.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(angle), (ppd.direc + 180) % 360, cl.coord);
					cl.ls = NavData.LocationStatus.DR;
					NavData.SetCurrentLocation(cl);
					MotionMeasureProb.Move(new MotionMeasureProb.Pose(cl.coord, cl.orientation));
					}
				else
					{
					Log.LogEntry("Attempt to turn failed.");
					cl.ls = NavData.LocationStatus.UNKNOWN;
					NavData.SetCurrentLocation(cl);
					}
				}
			else
				rtn = true;
			return (rtn);
		}



		private bool MoveBackward(int dist)

		{
			bool rtn = false;
			string rsp;

			rsp = SkillShared.SendCommand(SharedData.BACKWARD_SLOW_NCC + " " + dist, 8000);
			if (rsp.StartsWith("ok"))
				rtn = true;
			return (rtn);
		}



		public static bool MoveToPt(Point mp,bool sfc,ref int moved_dist)

		{
			bool rtn =  false,suff_dist = false;
			NavData.location cl,eloc;
			ArrayList sdata = new ArrayList(),obs = new ArrayList();
			int dist,ldist = -1,kdist = 0,rdist,mdist,cangle = 0,max_angle_correct,no_obs = 0;
			string rsp;
			Point pt;
			string msg, reply;
			const double MAX_DIST_CHG = .1;
			const double MAX_ADJ_DIST = 12;

			Log.LogEntry("MoveTo " + mp);
			cl = NavData.GetCurrentLocation();
			dist = NavCompute.DistancePtToPt(cl.coord,mp);
			if (Rplidar.CaptureScan(ref sdata, true))
				ldist = SkillShared.FindObstacles(0, dist + SharedData.ARM_PERCH_OFFSET + 1,sdata,SkillShared.MIN_SIDE_CLEAR,ref obs);
			if (ldist > 0)
				{
				Log.LogEntry("LIDAR front clearence: " + ldist);
				SkillShared.KinectFrontClear(dist + SharedData.ARM_PERCH_OFFSET + 1, ref kdist,ref no_obs,true);
				Log.LogEntry("Front clearence Kinect: " + kdist);
				if ((ldist < kdist) && (obs.Count > 0))
					{
					ldist = SkillShared.RemoveObstacles(ref sdata,ref obs, kdist, SkillShared.MIN_KINECT_LIDAR_FLOOR_DIST);
					if (obs.Count == 0)
						ldist = kdist;
					}
				mdist = Math.Min(ldist, kdist);
				if ((mdist < SkillShared.MIN_KINECT_LIDAR_FLOOR_DIST) && (obs.Count > 0))
					{
					msg = obs.Count + " possible LIDAR anomalies have been detected " + mdist + " inches ahead of me.  Is my path clear?";
					reply = AutoRobotControl.Speech.Conversation(msg, "responseyn", 5000, true);
					if (reply == "yes")
						{
						obs.Clear();
						mdist = dist + SharedData.ARM_PERCH_OFFSET + 1;
						}
					}
				if (mdist < dist + SharedData.ARM_PERCH_OFFSET + 1)
					{
					rdist = mdist - (SharedData.ARM_PERCH_OFFSET + 1);
					if ((rdist > 0) && (Math.Abs(dist - rdist) < (MAX_DIST_CHG *  dist)))
						{
						Log.LogEntry("Distance changed from " + dist + " to " + rdist);
						dist = rdist;
						suff_dist = true;
						}
					else if (SkillShared.ObstacleAdjustAngle(sdata,obs, dist + SharedData.ARM_PERCH_OFFSET + 1,0,1,ref cangle))
						{
						max_angle_correct = (int) Math.Round(Math.Atan(MAX_ADJ_DIST/dist) * SharedData.RAD_TO_DEG);
						Log.LogEntry("Angles: " + cangle + ", " + max_angle_correct);
						if ((Math.Abs(cangle) <= max_angle_correct) && Turn.TurnAngle(cangle))
							suff_dist = true;
						}
					else
						Log.LogEntry("Clear path not found.");
					}
				else
					suff_dist = true;
				if (suff_dist)
					{
					rsp = SkillShared.SendCommand("TFSFC " + (SharedData.ARM_PERCH_OFFSET + 1),200);
					if (rsp.StartsWith("ok"))
						rsp = SkillShared.SendCommand(SharedData.FORWARD_SLOW + " " + dist, 8000);
					else
						rsp = SkillShared.SendCommand(SharedData.FORWARD_SLOW_NCC + " " + dist, 8000);
					if (rsp.StartsWith("ok") || ((rsp.StartsWith("fail") && (rsp.Contains(SharedData.INSUFFICENT_FRONT_CLEARANCE)))))
						{
						rsp = SkillShared.SendCommand(SharedData.DIST_MOVED,200);
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
						rtn = true;
						}
					else
						{
						cl.ls = NavData.LocationStatus.UNKNOWN;
						rsp = SkillShared.SendCommand(SharedData.DIST_MOVED, 200);
						if (rsp.StartsWith("ok"))
							{
							dist = int.Parse(rsp.Substring(3));
							pt = new Point(0, dist);
							eloc = cl;
							eloc.coord = NavCompute.MapPoint(pt, cl.orientation, cl.coord);
							eloc.ls = NavData.LocationStatus.DR;
							eloc.loc_name = "";
							MotionMeasureProb.Move(new MotionMeasureProb.Pose(eloc.coord, eloc.orientation));
							Log.LogEntry("Expected location: " + eloc.ToString());
							cl = eloc;
							rtn = true;
							}
						NavData.SetCurrentLocation(cl);
						moved_dist = dist;
						}
					}
				else
					Log.LogEntry("Insufficent clearence for move.");
				}
			else
				Log.LogEntry("Could not obtain LIDAR scan.");
			return (rtn);
		}


		public bool ReturnToStart(Stack path,Stack final_adjust)

		{
			bool rtn = false;
			int i,count,moved_dist = 0,dist,angle;
			const int MOVE_BACK = 15;
			Point pt;

			if ((count = path.Count) > 0)
				{
				SkillShared.OutputSpeech("Starting return to start location.");
				Log.LogStack("Final adjust: ",final_adjust);
				if (final_adjust.Count > 0)
					{
					if (final_adjust.Count == 1)
						{
						MoveBackward(1);
						angle = (int) final_adjust.Pop();
						SkillShared.TurnAngle(angle);
						}
					else
						{
						dist = (int) final_adjust.Pop();
						MoveBackward(dist + 1);
						angle = (int) final_adjust.Pop();
						SkillShared.TurnAngle(angle);
						}
					}
				Log.LogStack("Return path: ", path);
				if (SkillShared.at_work_loc)
					{
					SkillShared.MoveBackward(MOVE_BACK, NavData.GetCurrentLocation());
					}
				pt = (Point)path.Pop();
				if (SkillShared.TurnToFaceMP(pt))
					{
					if (MoveToPt(pt,false,ref moved_dist))
						{
						for (i = 1;i < count;i++)
							{
							pt = (Point) path.Pop();
							if (SkillShared.TurnToFaceMP(pt))
								{
								if (!MoveToPt(pt,false,ref moved_dist))
									{
									SkillShared.OutputSpeech("Attempt to move failed. Return to start location aborted.");
									break;
									}
								}
							else
								{
								SkillShared.OutputSpeech("Attempt to turn failed.  Return to start location aborted.");
								break;
								}
							}
						}
					else
						SkillShared.OutputSpeech("Attempt to move failed.  Return to start location aborted.");
					}
				else if (TurnToFaceBackwardMP(pt))
					{
					if (MoveBackwardToPoint(pt))
						{
						count = path.Count;
						for (i = 1;i < count;i++)
							{
							pt = (Point) path.Pop();
							if (TurnToFaceBackwardMP(pt))
								{
								if (!MoveBackwardToPoint(pt))
									{
									SkillShared.OutputSpeech("Attempt to move failed. Return to start location aborted.");
									break;
									}
								}
							else
								{
								SkillShared.OutputSpeech("Attemtp to turn failed.  Return to start location aborted.");
								break;
								}
							}
						}
					else
						SkillShared.OutputSpeech("Attempt to move failed. Return to start location aborted.");
					}
				}
			else if (final_adjust.Count > 0)
				{
				SkillShared.OutputSpeech("Starting return to approach location.");
				if (final_adjust.Count == 1)
					{
					MoveBackward(1);
					angle = (int)final_adjust.Pop();
					if (angle > 0)
						SkillShared.TurnAngle(angle);
					}
				else
					{
					dist = (int)final_adjust.Pop();
					MoveBackward(dist + 1);
					angle = (int)final_adjust.Pop();
					if (angle > 0)
						SkillShared.TurnAngle(angle);
					}
				}
			else
				rtn = true;
			return (rtn);
		}

		}
	}
