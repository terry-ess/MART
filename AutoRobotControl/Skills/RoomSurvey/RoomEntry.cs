using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using AutoRobotControl;

namespace Room_Survey
	{
	class RoomEntry
		{

		public enum search_strategy { NONE,Y_X,X_Y};



		private bool SearchForward(int offset)

		{
			bool rtn = false;
			int sdist, modist, mwdist = -1,kodist,mmodist;
			ArrayList obs = new ArrayList();
			bool back_wall_found;
			double min_detect_dist = -1;

			sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.FRONT);
			SkillShared.CaptureRoomScan(SkillShared.ccpose, "");
			modist = SurveyCompute.FindObstacles(0, -1, SkillShared.sdata, ref obs);
			back_wall_found = Kinect.MinWallDistance(ref mwdist);
			Log.LogEntry("Search forward distances: " + sdist + ", " + modist + ", " + mwdist);
			if ((sdist < modist) && (sdist < SkillShared.MIN_DIST_FOR_SEARCH))
				{
				kodist = (int) Math.Round(KinectExt.FindObstacles(modist, SkillShared.STD_SIDE_CLEAR, ref min_detect_dist));
				Log.LogEntry("Kinect min obstacle distance - " + kodist + "   min detect distance - " + min_detect_dist);
				if ((kodist > sdist) && (min_detect_dist <= sdist))
					mmodist = Math.Min(kodist, modist);
				else
					mmodist = Math.Min(sdist, modist);
				}
			else
				mmodist = Math.Min(sdist, modist);
			if (mmodist >= (SkillShared.MIN_DIST_FOR_SEARCH - offset))
				rtn = true;
			else if (back_wall_found && (mwdist >= (SkillShared.MIN_DIST_FOR_SEARCH - offset)) && (modist >= (int)Math.Round(.75 * mwdist)))
				rtn = true;
			Log.LogEntry("SearchForward: " + rtn);
			return (rtn);
		}



		public search_strategy MinEntry()

		{
			search_strategy ss = search_strategy.NONE;
			int modist,sdist,modist2,mmodist,kodist;
			ArrayList obs = new ArrayList();
			double min_detect_dist = -1;

			sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.FRONT);
			modist = SurveyCompute.FindObstacles(0, -1, SkillShared.sdata,ref obs);
			Log.LogEntry("MinEntry distances: " + sdist + ", " + modist);
			if ((sdist < modist) && (sdist < SkillShared.MIN_DIST_FOR_SEARCH))
				{
				kodist = (int)Math.Round(KinectExt.FindObstacles(modist, SkillShared.STD_SIDE_CLEAR, ref min_detect_dist));
				Log.LogEntry("Kinect min obstacle distance - " + kodist + "   min detect distance - " + min_detect_dist);
				if ((kodist > sdist) && (min_detect_dist >= sdist))
					mmodist = Math.Min(kodist,modist);
				else
					mmodist = Math.Min(sdist,modist);
				}
			else
				mmodist = Math.Min(sdist, modist);
			if (mmodist > SharedData.ROBOT_LENGTH + SharedData.REAR_TURN_RADIUS + 1)
				{
				sdist = (int) Math.Ceiling(SharedData.REAR_TURN_RADIUS);
				if (Move.MoveForwardSlow(mmodist - (sdist + 1),sdist))
					{
					SkillShared.searchd.entry_pt = SkillShared.ccpose.coord;
					SkillShared.CaptureRoomScan(SkillShared.ccpose,"");
					obs.Clear();
					modist = SurveyCompute.FindObstacles(90, -1, SkillShared.sdata, ref obs);
					obs.Clear();
					modist2 = SurveyCompute.FindObstacles(-90, -1, SkillShared.sdata, ref obs);
					if (modist2 > modist)
						Move.TurnToFaceDirect(270);
					else
						Move.TurnToFaceDirect(90);
					if (SearchForward(SkillShared.ccpose.coord.X))
						ss = search_strategy.X_Y;
					else
						{
						sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.FRONT);
						modist = SurveyCompute.FindObstacles(0, -1, SkillShared.sdata, ref obs);
						Log.LogEntry("After turn distances: " + sdist + ", " + modist);
						if ((sdist < modist) && (sdist < SkillShared.MIN_DIST_FOR_SEARCH))
							{
							kodist = (int)Math.Round(KinectExt.FindObstacles(modist, SkillShared.STD_SIDE_CLEAR, ref min_detect_dist));
							Log.LogEntry("Kinect min obstacle distance - " + kodist + "   min detect distance - " + min_detect_dist);
							if ((kodist > sdist) && (min_detect_dist >= sdist))
								mmodist = Math.Min(kodist, modist);
							else
								mmodist = Math.Min(sdist, modist);
							}
						else
							mmodist = Math.Min(sdist, modist);
						if (mmodist > SharedData.ROBOT_LENGTH + SharedData.REAR_TURN_RADIUS + 1)
							{
							sdist = (int)Math.Ceiling(SharedData.REAR_TURN_RADIUS);
							if (Move.MoveForwardSlow(mmodist - (sdist + 1), sdist))
								{
								SkillShared.searchd.entry_pt2 = SkillShared.ccpose.coord;
								if (SkillShared.ccpose.orient == 90)
									modist = SurveyCompute.FindObstacles(-90, -1, SkillShared.sdata, ref obs);
								else
									modist = SurveyCompute.FindObstacles(90, -1, SkillShared.sdata, ref obs);
								if (modist - (SharedData.FRONT_TURN_RADIUS + 1) >= (SkillShared.MIN_DIST_FOR_SEARCH - SkillShared.ccpose.coord.Y))
									{
									Move.TurnToFaceDirect (0);
									if (SearchForward(SkillShared.ccpose.coord.Y))
										ss = search_strategy.Y_X;
									else
										SkillShared.OutputSpeech("Insufficient space to proceed.",true);
									}
								else
									SkillShared.OutputSpeech("Insufficient space to proceed",true);
								}
							else
								SkillShared.OutputSpeech("Attempt to explore entry failed.",true);
							}
						else
							SkillShared.OutputSpeech("Insufficient space to explore entry.",true);	//SHOULD ATTEMPT TO FIND SEARCH PATH
						}																									//USING LIDAR BASED MAP??
					if (ss == search_strategy.NONE)		//IF CENTERED DOOR CONFIG ETC. NEITHER STRATEGY COULD BE SELECTED THEN WHAT??
						SkillShared.OutputSpeech("No search strategy could be selected.",true);
					}
				else
					SkillShared.OutputSpeech("Attempt to enter the room " + SkillShared.connect.name + " failed.",true);
				}
			else
				SkillShared.OutputSpeech("Insufficient space to enter the room " + SkillShared.connect.name,true);
			SkillShared.searchd.ss = ss;
			Log.LogEntry("Search strategy: " + ss);
			return (ss);
		}

		}
	}
