using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using AutoRobotControl;

namespace Room_Survey
	{
	class RoomSurvey: SkillsInterface
		{

		private const string GRAMMAR = "roomsurvey";
		private const int MIN_DIM = 120;
		private const int MAX_X_LIMIT = 10;
		private const int MIN_Y_DIST_FOR_YX_SEARCH = 120;
		private const int MAX_ADJUST_ANGLE = 5;

		public enum entry_strategy  {NONE,Y_X,MIN_ENTRY};


		public struct ExtScanData
		{
			public Rplidar.scan_data sd;
			public Point coord;
		};



		public struct RoomSize
		{
			public int height,width,max_x;

			public RoomSize(int h,int w,int mx)
			{
			height = h;
			width = w;
			max_x = mx;
			}
		};

		private string grammar_file = Application.StartupPath + SharedData.CAL_SUB_DIR + GRAMMAR + ".xml";
		private Thread survey = null;
		private Thread survey_prep = null;
		private string rm_name,connect_name;
		private NavData.location clocation;


		public bool Open(params object[] obj)

		{
			bool rtn = false;
			string dname;

			Log.LogEntry("RoomSurvey Open " + obj.Length );
			if (SharedData.head_assembly_operational && SharedData.kinect_operational && SharedData.front_lidar_operational
				&& SharedData.motion_controller_operational && SharedData.navdata_operational)
				{
				if (obj.Length == 0)
					{
					rm_name = "";
					connect_name = "";
					survey_prep = new Thread(SurveyPrepThread);
					survey_prep.Start();
					rtn = true;
					}
				else if (obj.Length == 2)
					{
					try
					{
					rm_name = (string) obj[0];
					connect_name = (string) obj[1];
					Log.LogEntry("Parameters: " + rm_name + "  " + connect_name);
					if (ValidRoomNames())
						{
						dname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR;
						if (!Directory.Exists(dname))
							Directory.CreateDirectory(dname);
						SkillShared.OutputSpeech("The room survey skill is starting the survey of " + connect_name, true);
						survey = new Thread(SurveyExecutionThread);
						survey.Start();
						rtn = true;
						}
					else
						SkillShared.OutputSpeech("Can not run the room survey skill.  The past parameters are not valid.", true);
					}

					catch(Exception ex)
					{
					SkillShared.OutputSpeech("Can not run the room survey skill.  The past parameters are not valid.", true);
					Log.LogEntry("Exception: " + ex.Message);
					Log.LogEntry("Stack trace: " + ex.StackTrace);
					}

					}
				else
					SkillShared.OutputSpeech("Can not run the room survey skill.  The number of pasted parameters is not valid.", true);
				}
			else
				SkillShared.OutputSpeech("Can not run the room survey skill.  The necessary resources are not available.",true);
			return (rtn);
		}



		public void Close()

		{
			if ((survey != null) && (survey.IsAlive))
				{
				survey.Abort();
				survey.Join();
				survey = null;
				}
		}



		private bool ValidRoomNames()

		{
			bool rtn = false;
			ArrayList al;
			int i;

			al = NavData.GetRooms();
			for (i = 0;i < al.Count;i++)
				{
				if (rm_name == (string) al[i])
					break;
				}
			if (i < al.Count)
				{
				al = NavData.GetConnections(rm_name);
				for (i = 0;i < al.Count;i++)
					{
					if (connect_name == ((NavData.connection)al[i]).name)
						break;
					}
				if (i < al.Count)
					rtn = true;
				}
			return(rtn);
		}


		
		private void SurveyPrepThread()

		{
			string dname;

			try
			{
			rm_name = DetermineEntryRoom(false);
			if ((rm_name != null) && (rm_name.Length > 0))
				{
				connect_name = DetermineConnectionName(false);
				if ((connect_name != null) && (connect_name.Length > 0))
					{
					AddGrammar();
					Speech.AddGrammerHandler(grammar_file);
					Speech.RegisterHandler(GRAMMAR, SpeechHandler, null);
					Speech.EnableCommand(GRAMMAR);
					SkillShared.OutputSpeech("The room survey skill is ready to survey " + connect_name, true);
					dname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR;
					if (!Directory.Exists(dname))
						Directory.CreateDirectory(dname);
					}
				else
					{
					SkillShared.OutputSpeech("Can not conduct a room survey without the connection name.  Closing the room survey skill.",true);
					Skills.CloseSkill();
					}
				}
			else
				{
				SkillShared.OutputSpeech("Can not conduct a room survey without the entry room name.  Closing the room survey skill.", true);
				Skills.CloseSkill();
				}
			}

			catch (Exception ex)
			{
			SkillShared.OutputSpeech("Survey exception: " + ex.Message,true);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			SkillShared.OutputSpeech("Closing the room survey skill.",true);
			Skills.CloseSkill();
			}

			survey_prep = null;
		}



		private string DetermineEntryRoom(bool disable_cmds)

		{
			string room = "";
			ArrayList rooms;
			int i;
			string reply;

			rooms = NavData.GetRooms();
			for (i = 0;i < rooms.Count;i++)
				{
				reply = Speech.Conversation("The entry room is " + ((string) rooms[i]) + "?","responseyn",5000,disable_cmds);
				if (reply == "yes")
					{
					room = ((string) rooms[i]);
					Speech.Speak("Your response was yes");
					break;
					}
				else if (reply == "no")
					Speech.Speak("Your response was no");
				else
					{
					Speech.Speak("No response was received.");
					break;
					}
				}
			return(room);
		}



		private string DetermineConnectionName(bool disable_cmds)

		{
			string connection = "";
			ArrayList connections;
			int i;
			string reply;

			connections = NavData.GetConnections(rm_name);
			for (i = 0;i < connections.Count;i++)
				{
				reply = Speech.Conversation("The connection is " + (((NavData.connection) connections[i]).name) + "?","responseyn",5000,disable_cmds);
				if (reply == "yes")
					{
					connection = ((NavData.connection)connections[i]).name;
					Speech.Speak("Your response was yes");
					break;
					}
				else if (reply == "no")
					Speech.Speak("Your response was no");
				else
					{
					Speech.Speak("No response was received.");
					break;
					}
				}
			return(connection);
		}



		private void AddGrammar()

		{
			TextReader tr;
			TextWriter tw1;
			string rfname,line;

			rfname = Application.StartupPath + SharedData.CAL_SUB_DIR + "basecommands.txt";
			if (File.Exists(rfname))
				{
				tr = File.OpenText(rfname);
				tw1 = File.CreateText(grammar_file);
				while ((line = tr.ReadLine()) != null)
					{
					if (line.Equals("</grammar>"))
						{
						tw1.WriteLine("  <rule id=\"rootRule\">");
						tw1.WriteLine("    <one-of>");
						tw1.WriteLine("       <item> quiet </item>");
						tw1.WriteLine("       <item> verbose </item>");
						tw1.WriteLine("       <item> start </item>");
						tw1.WriteLine("    </one-of>");
						tw1.WriteLine("  </rule>");
						tw1.WriteLine();
						tw1.WriteLine(line);
						break;
						}
					else
						tw1.WriteLine(line);
					}
				tr.Close();
				tw1.Close();
				}
		}



		public void SpeechHandler(string msg)

		{

			if (SharedData.head_assembly_operational && SharedData.kinect_operational && SharedData.front_lidar_operational
				&& SharedData.motion_controller_operational && SharedData.navdata_operational)
				{
				if (msg == "quiet")
					{
					SkillShared.silent = true;
					Speech.SpeakAsync("okay");
					}
				else if (msg == "verbose")
					{
					SkillShared.silent = false;
					Speech.SpeakAsync("okay");
					}
				else if (msg == "start")
					{
					if (survey == null)
						{
						Speech.SpeakAsync("okay");
						survey = new Thread(SurveyExecutionThread);
						survey.Start();
						}
					else
						SkillShared.OutputSpeech("A room survey is alread underway.",true);
					}
				}
			else
				SkillShared.OutputSpeech("I am not fully operational.  Can not execute a room survey." + msg,true);
		}



		public void StopSpeechHandler(string msg)

		{
			SkillShared.run = false;
			SkillShared.OutputSpeech("stop received",true);
		}






		private bool MoveToEntryPoint(Speech.STSHandler stshi)

		{
			int dist = 0, i;
			ArrayList cnts = new ArrayList();
			bool rtn = false;

			clocation = NavData.GetCurrentLocation();
			if ((rm_name != clocation.rm_name) && SkillShared.run)
				{
				if (!stshi.Invoke("go to " + rm_name))
					{
					SkillShared.OutputSpeech("The command go to " + rm_name + " failed.",true);
					SkillShared.run = false;
					}
				}
			if (SkillShared.run)
				{
				cnts = NavData.GetCurrentRoomConnections();
				for (i = 0; i < cnts.Count; i++)
					if (((NavData.connection)cnts[i]).name == connect_name)
						{
						SkillShared.connect = (NavData.connection)cnts[i];
						break;
						}
				if ((i < cnts.Count) && Navigate.rmi.GoToExitPoint(SkillShared.connect, ref dist))
					{
					SkillShared.cc_orient = SkillShared.connect.direction;
					if (SkillShared.SendCommand(SharedData.FORWARD + " " + dist, (int)(((dist / 7.2) + 2) * 100)))
						{
						clocation = NavData.GetCurrentLocation();
						clocation.coord = NavCompute.MapPoint(new Point(0, dist), clocation.orientation, clocation.coord);
						clocation.ls = NavData.LocationStatus.DR;
						NavData.SetCurrentLocation(clocation);
						if (Turn.TurnToDirection(clocation.orientation, SkillShared.connect.direction))
							{
							SkillShared.ccpose.coord = new Point(0,0);
							SkillShared.ccpose.orient = SkillShared.connect.direction;
							clocation.orientation = SkillShared.connect.direction;
							NavData.SetCurrentLocation(clocation);
							SkillShared.exit_connect.name = rm_name;
							SkillShared.exit_connect.exit_center_coord = new Point(0,0);
							SkillShared.exit_connect.exit_width = SkillShared.connect.exit_width;
							SkillShared.exit_connect.direction = 180;
							SkillShared.exit_connect.hc_edge = SkillShared.connect.lc_edge;
							if (SkillShared.exit_connect.hc_edge.ds == NavData.DoorSwing.OUT)
								SkillShared.exit_connect.hc_edge.ds = NavData.DoorSwing.IN;
							else if (SkillShared.exit_connect.hc_edge.ds == NavData.DoorSwing.IN)
								SkillShared.exit_connect.hc_edge.ds = NavData.DoorSwing.OUT;
							SkillShared.exit_connect.lc_edge = SkillShared.connect.hc_edge;
							if (SkillShared.exit_connect.lc_edge.ds == NavData.DoorSwing.OUT)
								SkillShared.exit_connect.lc_edge.ds = NavData.DoorSwing.IN;
							else if (SkillShared.exit_connect.lc_edge.ds == NavData.DoorSwing.IN)
								SkillShared.exit_connect.lc_edge.ds = NavData.DoorSwing.OUT;
							SkillShared.sdata.Clear();
							Rplidar.CaptureScan(ref SkillShared.sdata, true);
							SkillShared.ccpose = new MotionMeasureProb.Pose(new Point(0, 0),0);
							rtn = true;
							}
						else
							SkillShared.OutputSpeech("Attemtp to turn in the entry way failed",true);
						}
					else
						SkillShared.OutputSpeech("Attempt to move to survey entry point failed",true);
					}
				}
			return(rtn);
		}



		public bool DetermineFinalMove(Point fmpt,ref int direct,ref int dist)

		{
			bool rtn = false;
			byte[,] map;
			Point map_shift = new Point(),spt,mpt = new Point();
			SurveyCompute.pt_to_pt_data ppd,fppd;
			int sdist,nsearch,i,odist,max_odist = 0,dcount = 0;
			Bitmap bm;
			string fname;
			const int SEARCH_DIST = 4;

			fppd = SurveyCompute.DetermineDirectDistPtToPt(new Point(0,0),fmpt);
			ppd = SurveyCompute.DetermineDirectDistPtToPt(fmpt,SkillShared.ccpose.coord);
			map = SurveyCompute.ScanMap(SkillShared.sdata,SkillShared.ccpose,ref map_shift);
			spt = SkillShared.ccpose.coord;
			spt = SurveyCompute.CcToMap(spt,map_shift);
			bm = SkillShared.MapToBitmap(map);
			sdist = SEARCH_DIST;
			nsearch = (ppd.dist + sdist)/sdist;
			for (i = 0;i < nsearch;i++)
				{
				if (spt.X < map_shift.X)		//sufficent direction based mod???
					spt.X += sdist;
				else
					spt.X -= sdist;
				odist = SurveyCompute.FindMapObstacle(ref map,spt,fppd.direc,ref bm);
				if (odist > max_odist)
					{
					max_odist = odist;
					dcount = i + 1;
					mpt = spt;
					}
				}
			fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room survey final move search map " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".bmp";
			bm.Save(fname);
			Log.LogEntry("Saved " + fname);
			if (max_odist > fppd.dist)
				{
				rtn = true;
				direct = ppd.direc;
				odist = SurveyCompute.FindMapObstacle(ref map,mpt,ppd.direc,ref bm);
				dist = dcount * sdist;
				if (odist < SharedData.FRONT_SONAR_CLEARANCE)
					dist -= ((SharedData.FRONT_SONAR_CLEARANCE + 1) - odist);
				Log.LogEntry("DetermineFinalMovePt: " + direct + ", " + dist);
				}
			else
				Log.LogEntry("Could not determine final move.");
			return(rtn);
		}



		private bool MoveToReadyMoveExit()

		{
			bool rtn = false;
			int i,dist,direct = -1;
			Point npt = new Point();
			const int MIN_MOVE = 2;

			for (i = 0;i < 4;i++)
				{
				switch(i)
					{
					case 0:
						npt = SkillShared.searchd.ct_pt;
						break;

					case 1:
						npt = SkillShared.searchd.axis2t_pt;
						break;

					case 2:
						npt = SkillShared.searchd.entry_pt2;
						break;

					case 3:
						npt = SkillShared.searchd.entry_pt;
						break;
					}
				if (!npt.IsEmpty)
					{
					dist = NavCompute.DistancePtToPt(npt,SkillShared.ccpose.coord);
					if (dist > MIN_MOVE)
						{
						if ((i == 3) && (!SkillShared.searchd.entry_pt2.IsEmpty))
							{
							if (DetermineFinalMove(npt,ref direct,ref dist) && Move.MoveToFinalRpathPt(direct,dist,SharedData.FRONT_SONAR_CLEARANCE))
								rtn = true;
							else
								break;
							}
						else
							{
							bool obstacle = false;

							if (Move.MoveToNextRPathPt(npt,SharedData.FRONT_SONAR_CLEARANCE,ref obstacle))
								SkillShared.ccpose.coord = npt;
							else if ((i == 3) && obstacle && (NavCompute.DistancePtToPt(new Point(0,0), SkillShared.ccpose.coord) <= Room.STD_EXIT_DISTANCE))
								{
								if (DetermineFinalMove(npt, ref direct, ref dist) && Move.MoveToFinalRpathPt(direct, dist, SharedData.FRONT_SONAR_CLEARANCE))
									rtn = true;
								else
									break;
								}
							else
								break;
							}
						}
					}
				}
			if (i == 4)
				rtn = true;
			return (rtn);
		}




		private bool MoveToEntryPoint(ref int dist)

		{
			int turn_angle = 0,modist,sangle,orient;
			ArrayList obs = new ArrayList();
			bool rtn = false,ready_move_exit = false,moved_to_ready_move_exit = false;
			SurveyCompute.pt_to_pt_data ppd;
			Exit exit = new Exit();

			if (SkillShared.fs_map != null)
				{
				if (SkillShared.searchd.entry_pt == new Point(0,0))
					SkillShared.fs_start = SkillShared.searchd.axis2t_pt;
				else
					SkillShared.fs_start = SkillShared.searchd.entry_pt;
				moved_to_ready_move_exit = exit.MoveToReadyMoveExit(this);
				}
			else
				moved_to_ready_move_exit = MoveToReadyMoveExit();
			if (moved_to_ready_move_exit)
				{
				ppd = SurveyCompute.DetermineDirectDistPtToPt(new Point(0,0),SkillShared.ccpose.coord);
				if (ppd.dist <= Room.STD_EXIT_DISTANCE)
					ready_move_exit = Move.TurnToFaceDirect(SkillShared.exit_connect.direction);
				else
					{
					sangle = NavCompute.AngularDistance(ppd.direc, SkillShared.ccpose.orient);
					if (!NavCompute.ToRightDirect(SkillShared.ccpose.orient, ppd.direc))
						sangle *= -1;
					modist = SurveyCompute.FindObstacles(sangle,-1, SkillShared.sdata, ref obs);
					if (modist > ppd.dist - Room.STD_EXIT_DISTANCE + SharedData.FRONT_SONAR_CLEARANCE)
						ready_move_exit = Move.MoveToNextPt(ppd.direc, ppd.dist - Room.STD_EXIT_DISTANCE, SharedData.FRONT_SONAR_CLEARANCE);
					else
						ready_move_exit = Move.TurnToFaceDirect(SkillShared.exit_connect.direction);
					}
				if (ready_move_exit)
					{
					if (SurveyCompute.ExitPosition(SkillShared.exit_connect,Room.STD_EXIT_DISTANCE, SkillShared.ccpose.orient,ref turn_angle,ref dist))
						{
						if ((Math.Abs(turn_angle) < SharedData.MIN_TURN_ANGLE) || Move.TurnToAngle(turn_angle))
							{
							SkillShared.sdata.Clear();
							if (Rplidar.CaptureScan(ref SkillShared.sdata, true))
								{
								obs.Clear();
								SurveyCompute.FindObstacles(0,dist, SkillShared.sdata,ref obs);
								if (obs.Count == 0)
									{
									if (SkillShared.SendCommand(SharedData.FORWARD + " " + dist, (int)(((dist / 7.2) + 2) * 100)))
										{
										clocation.coord = SkillShared.connect.exit_center_coord;
										orient = (SkillShared.connect.direction + SkillShared.ccpose.orient) % 360;
										if (orient < 0)
											orient += 360;
										clocation.orientation = orient;
										clocation.ls = NavData.LocationStatus.DR;
										NavData.SetCurrentLocation(clocation);
										rtn = true;
										}
									else
										SkillShared.OutputSpeech("Move to connecter failed",true);
									}
								else
									SkillShared.OutputSpeech("Path to connecter not clear",true);
								}
							else
								SkillShared.OutputSpeech("Could not capture LIDAR scan",true);
							}
						else
							SkillShared.OutputSpeech("Turn for exit failed.",true);
						}
					else
						SkillShared.OutputSpeech("Could not determine exit position",true);
					}
				else
					SkillShared.OutputSpeech("Attempt to move to exit point failed.",true);
				}
			else
				SkillShared.OutputSpeech("Attempt to move to exit point failed.",true);
			return(rtn);
		}



		private void ReturnStart(NavData.location start,Speech.STSHandler stshi)

		{
			NavData.location cloc;
			string name1,name2 = "";
			ArrayList connections;
			bool rtn_to_start = true;

			SkillShared.OutputSpeech("Returning to start point or the closest recharge station.", true);
			cloc = NavData.GetCurrentLocation();
			if ((cloc.ls != NavData.LocationStatus.UNKNOWN) && (start.loc_name.Length > 0))
				{
				name1 = start.rm_name;
				if (start.loc_name == "exit")
					{
					connections = NavData.GetConnections(name1);
					if (connections.Count == 1)
						name2 = start.loc_name;
					else
						rtn_to_start = false;
					}
				else
					name2 = start.loc_name;
				if (rtn_to_start)
					if (stshi.Invoke("go to " + name1 + " " + name2))
						SkillShared.OutputSpeech("Returned to " + name1 + " " + name2,true);
					else
						{
						SkillShared.OutputSpeech ("Return to start point failed.",true);
						Skills.ReturnFailed();
						}
				else
					{
					name1 = NavData.ClosestRechargeStation(cloc.rm_name);
					if (name1.Length > 0)
						if (stshi.Invoke("go to " + name1 + " " + SharedData.RECHARGE_LOC_NAME))
							SkillShared.OutputSpeech("Returned to " + name1 + " " + SharedData.RECHARGE_LOC_NAME, true);
						else
							{
							SkillShared.OutputSpeech("Return to " + name1 + " " + SharedData.RECHARGE_LOC_NAME + " failed", true);
							Skills.ReturnFailed();
							}
					else
						{
						SkillShared.OutputSpeech("Could not determine nearest recharge station.",true);
						Skills.ReturnFailed();
						}
					}
				}
			else if (cloc.ls != NavData.LocationStatus.UNKNOWN)
				{
				name1 = NavData.ClosestRechargeStation(cloc.rm_name);
				if (name1.Length > 0)
					if (stshi.Invoke("go to " + name1 + " " + SharedData.RECHARGE_LOC_NAME))
						SkillShared.OutputSpeech("Returned to " + name1 + " " + SharedData.RECHARGE_LOC_NAME, true);
					else
						{
						SkillShared.OutputSpeech("Return to " + name1 + " " + SharedData.RECHARGE_LOC_NAME + " failed", true);
						Skills.ReturnFailed();
						}
				else
					{
					SkillShared.OutputSpeech("Could not determine nearest recharge station.",true);
					Skills.ReturnFailed();
					}
				}
			else
				{
				SkillShared.OutputSpeech("Can not execute return.",true);
				Skills.ReturnFailed();
				}
		}



		private bool ObstructionDueToDoor(ArrayList obs,int odist)

		{
			bool rtn = false;
			int max_y_dist = -1;


			if ((SurveyCompute.ObstacleSide(obs,odist, 0,1,ref max_y_dist) == SharedData.RobotLocation.RIGHT) &&  SkillShared.connect.lc_edge.door_side)
				{
				if ((max_y_dist != -1) && (max_y_dist <= SkillShared.connect.exit_width + 6))
					rtn = true;
				}
			else if ((SurveyCompute.ObstacleSide(obs,odist, 0, 1,ref max_y_dist) == SharedData.RobotLocation.LEFT) && SkillShared.connect.hc_edge.door_side)
				{
				if ((max_y_dist != -1) && (max_y_dist <= SkillShared.connect.exit_width + 6))
					rtn = true;
				}
			return (rtn);
		}



		private entry_strategy DetermineEntryStrategyWOMap()

		{
			entry_strategy es = entry_strategy.NONE;
			int sdist,modist,mwdist = -1,mmodist,angle = 0,kodist;
			ArrayList obs = new ArrayList();
			bool back_wall_found;
			double min_detect_dist = 0;

			sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.FRONT);
			SkillShared.sdata.Clear();
			Rplidar.CaptureScan(ref SkillShared.sdata, true);
			modist = SurveyCompute.FindObstacles(0, -1, SkillShared.sdata, ref obs);
			back_wall_found = Kinect.MinWallDistance(ref mwdist);
			Log.LogEntry("DetermineSurveyStrategyWOMap distances: " + sdist + ", " + modist + ", " + mwdist);
			if ((sdist < modist) && (sdist < MIN_Y_DIST_FOR_YX_SEARCH))
				{
				kodist = (int) Math.Round(KinectExt.FindObstacles(modist,SkillShared.STD_SIDE_CLEAR,ref min_detect_dist));
				Log.LogEntry("Kinect min obstacle distance - " + kodist + "   min floor detect distance - " + min_detect_dist);
				if ((kodist > sdist) && (min_detect_dist < sdist))
					mmodist = Math.Min(kodist,modist);
				else
					mmodist = Math.Min(sdist,modist);
				}
			else
				mmodist = Math.Min(sdist,modist);
			if (mmodist >= MIN_Y_DIST_FOR_YX_SEARCH)
				es = entry_strategy.Y_X;
			else if (back_wall_found && ((mwdist >= MIN_Y_DIST_FOR_YX_SEARCH) && (mmodist >= (int)Math.Round(.75 * mwdist))))
				es = entry_strategy.Y_X;
			else if (ObstructionDueToDoor(obs,MIN_Y_DIST_FOR_YX_SEARCH))
				{
				if (SurveyCompute.ObstacleAdjustAngle(SkillShared.sdata,obs, SkillShared.connect.exit_width + 6, 0, ref angle))
					{
					if (Math.Abs(angle) < SharedData.MIN_TURN_ANGLE)
						if (angle > 0)
							angle = SharedData.MIN_TURN_ANGLE;
						else
							angle = -SharedData.MIN_TURN_ANGLE;
					if (Math.Abs(angle) <= MAX_ADJUST_ANGLE)
						{
						if (Move.TurnToAngle(angle))
							{
							sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.FRONT);
							SkillShared.sdata.Clear();
							Rplidar.CaptureScan(ref SkillShared.sdata, true);
							modist = SurveyCompute.FindObstacles(0, -1, SkillShared.sdata, ref obs);
							Log.LogEntry("After turn distances: " + sdist + ", " + modist);
							if ((sdist < modist) && (sdist < MIN_Y_DIST_FOR_YX_SEARCH))
								{
								kodist = (int)Math.Round(KinectExt.FindObstacles(modist, SkillShared.STD_SIDE_CLEAR, ref min_detect_dist));
								Log.LogEntry("Kinect min obstacle distance - " + kodist + "   min detect distance - " + min_detect_dist);
								if ((kodist > sdist) && (min_detect_dist < sdist))
									mmodist = Math.Min(kodist, modist);
								else
									mmodist = Math.Min(sdist, modist);
								}
							else
								mmodist = Math.Min(sdist, modist);
							if (mmodist >= MIN_Y_DIST_FOR_YX_SEARCH)
								es = entry_strategy.Y_X;
							else if (back_wall_found && (mwdist >= MIN_Y_DIST_FOR_YX_SEARCH) && (mmodist >= (int)Math.Round(.75 * mwdist)))
								es = entry_strategy.Y_X;
							else if (mmodist > SharedData.ROBOT_LENGTH + SharedData.REAR_TURN_RADIUS + 1)
								es = entry_strategy.MIN_ENTRY;
							}
						else
							Log.LogEntry("Attempt to turn to adjustment angle failed.");
						}
					else if (mmodist > SharedData.ROBOT_LENGTH + SharedData.REAR_TURN_RADIUS + 1)
						es = entry_strategy.MIN_ENTRY;
					}
				else if (mmodist > SharedData.ROBOT_LENGTH + SharedData.REAR_TURN_RADIUS + 1)
					es = entry_strategy.MIN_ENTRY;
				}
			else if (mmodist > SharedData.ROBOT_LENGTH + SharedData.REAR_TURN_RADIUS + 1)
				es = entry_strategy.MIN_ENTRY;
			Log.LogEntry("Entry strategy: " + es);
			SkillShared.searchd.es = es;
			return (es);	
		}



		private entry_strategy DetermineEntryStrategy()

		{
			entry_strategy es = entry_strategy.NONE;
			byte[,] map = new byte[Kinect.depth_width, (int) Math.Ceiling(Kinect.nui.DepthStream.TooFarDepth * SharedData.MM_TO_IN)];
			int mfdist = -1,mdist = -1,sdist,i,direct,max_mdist = 0,tangle,mwdist = 0;
			bool right = false,edge = false,back_wall_found;
			Point spt;
			const int ENTRY_SIDE_CLEAR = 2;
			const int MAX_ENTRY_ADJUST_ANGLE = 9;

			if (KinectExt.MapDepth(ref map,ref mfdist))
				{
				spt = new Point((int) Math.Round((double) map.GetLength(0)/ 2), map.GetLength(1) - 1);
				mdist = KinectExt.CheckMapObstacles(spt,0,KinectExt.kinect_max_depth,ENTRY_SIDE_CLEAR,map,ref right,ref edge);
				if (mdist == -1)
					mdist = KinectExt.kinect_max_depth + 1;
				if (mdist < MIN_Y_DIST_FOR_YX_SEARCH)
					{
					max_mdist = mdist;
					tangle = 0;
					Log.LogEntry("DetermineEntryStrategy search for best entry angle required.");
					for (i = SharedData.MIN_TURN_ANGLE;i <= MAX_ENTRY_ADJUST_ANGLE;i++)
						{
						if (!right)
							direct = i;
						else
							direct = 360 - i;
						mdist = KinectExt.CheckMapObstacles(spt,direct,MIN_Y_DIST_FOR_YX_SEARCH,ENTRY_SIDE_CLEAR, map, ref right, ref edge);
						if (mdist == -1)
							{
							max_mdist = mdist = MIN_Y_DIST_FOR_YX_SEARCH + 1;
							tangle = direct;
							break;
							}
						else if (mdist > max_mdist)
							{
							max_mdist = mdist;
							tangle = direct;
							}
						}
					if ((max_mdist >= SharedData.ROBOT_LENGTH + SharedData.REAR_TURN_RADIUS + 1) && (tangle > 0))
						{
						if (tangle > 180)
							tangle = 360 - tangle;
						else
							tangle = -tangle;
						if (Move.TurnToAngle(tangle))
							{
							Log.LogEntry("Turned " + tangle + " to adjust entry.");
							SkillShared.sdata.Clear();
							Rplidar.CaptureScan(ref SkillShared.sdata, true);
							map = new byte[Kinect.depth_width, (int)Math.Ceiling(Kinect.nui.DepthStream.TooFarDepth * SharedData.MM_TO_IN)];
							if (KinectExt.MapDepth(ref map, ref mfdist))
								{
								mdist = KinectExt.CheckMapObstacles(spt, 0, KinectExt.kinect_max_depth,ENTRY_SIDE_CLEAR, map, ref right, ref edge);
								if (mdist == -1)
									mdist = KinectExt.kinect_max_depth + 1;
								if (mdist >= MIN_Y_DIST_FOR_YX_SEARCH)
									es = entry_strategy.Y_X;
								else if (mdist >= SharedData.ROBOT_LENGTH + SharedData.REAR_TURN_RADIUS + 1)
									{
									back_wall_found = Kinect.MinWallDistance(ref mwdist);
									if (back_wall_found && (mwdist >= MIN_Y_DIST_FOR_YX_SEARCH) && (mdist >= (int)Math.Round(.75 * mwdist)))
										es = entry_strategy.Y_X;
									else
										es = entry_strategy.MIN_ENTRY;
									}
								}
							else
								es = DetermineEntryStrategyWOMap();
							}
						else
							Log.LogEntry("Attempt to adjust entry angle failed.");
						}
					else if (max_mdist >= SharedData.ROBOT_LENGTH + SharedData.REAR_TURN_RADIUS + 1)
						{
						mdist = max_mdist;
						if (mdist >= MIN_Y_DIST_FOR_YX_SEARCH)
							es = entry_strategy.Y_X;
						else if (mdist >= SharedData.ROBOT_LENGTH + SharedData.REAR_TURN_RADIUS + 1)
							{
							back_wall_found = Kinect.MinWallDistance(ref mwdist);
							if (back_wall_found && (mwdist >= MIN_Y_DIST_FOR_YX_SEARCH) && (mdist >= (int)Math.Round(.75 * mwdist)))
								es = entry_strategy.Y_X;
							else
								es = entry_strategy.MIN_ENTRY;
							}
						}
					}
				Log.LogEntry("Kinect map determined minimum obstacle distance: " + mdist);
				if (mdist >= MIN_Y_DIST_FOR_YX_SEARCH)
					{
					sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.FRONT);
					Log.LogEntry("Sonar detected minimum obstacle distance: " + sdist);
					if (sdist < mfdist)
						{
						if (sdist >= SharedData.ROBOT_LENGTH + SharedData.REAR_TURN_RADIUS + 1)
							es = entry_strategy.MIN_ENTRY;
						}
					else
						es = entry_strategy.Y_X;
					}
				Log.LogEntry("Entry strategy: " + es);
				}
			else
				es = DetermineEntryStrategyWOMap();
			return (es);
		}



		private void SurveyExecutionThread()

		{
			Speech.STSHandler stshi = null;
			Speech.pair_handlers ph;
			int dist = 0,tdirect = 0,cdist = -1;
			Point center = new Point();
			NavData.location start;
			LidarSearch ls = new Room_Survey.LidarSearch();
			KinectSurvey ks = new KinectSurvey();
			entry_strategy es;
			string entrypic;
			RoomEntry re = new RoomEntry();
			RoomEntry.search_strategy ss = RoomEntry.search_strategy.NONE;

			Speech.RegisterHandler(Speech.STOP_GRAMMAR, StopSpeechHandler, null);

			try
			{
			ph = (Speech.pair_handlers) Speech.handlers[SmartMotionCommands.GRAMMAR];
			stshi = (Speech.STSHandler) ph.stsh;
			if (stshi == null)
				SkillShared.OutputSpeech("No handler is available for smart commands",true);
			else
				{
				SkillShared.run = true;
				SkillShared.OutputSpeech("Moving to survey's entry point",false);
				start = NavData.GetCurrentLocation();
				if (MoveToEntryPoint(stshi))
					{
					es = DetermineEntryStrategy();
					SkillShared.CaptureRoomScan(SkillShared.ccpose,"");
					entrypic = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room Kinect survey connection center picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
					SkillShared.CapturePicture(entrypic ,true);
					if (es == entry_strategy.Y_X)
						{
						SkillShared.OutputSpeech("Starting LIDAR search",false);
						if (ls.SearchYX(ref center,ref tdirect,ref cdist))
							{
							SkillShared.OutputSpeech("Starting Kinect survey",false);
							if (ks.ConductSurvey(center,tdirect,cdist))
								SkillShared.OutputSpeech("Current survey implementation completed. Returning to " + clocation.rm_name,false);
							else
								SkillShared.OutputSpeech("Survey failed.Returning to " + clocation.rm_name, true);
							}
						else
							SkillShared.OutputSpeech("Search failed. Returning to " + clocation.rm_name, true);
						}
					else if (es == entry_strategy.MIN_ENTRY)
						{
						SkillShared.OutputSpeech("Starting room entry", false);
						if ((ss = re.MinEntry()) != RoomEntry.search_strategy.NONE)
							{
							if (ss == RoomEntry.search_strategy.X_Y)
								{
								SkillShared.OutputSpeech("Starting LIDAR search",false);
								if (ls.SearchXY(ref center, ref tdirect,ref cdist))
									{
									SkillShared.OutputSpeech("Starting Kinect survey",false);
									if (ks.ConductSurvey(center,tdirect,cdist))
										SkillShared.OutputSpeech("Current survey implementation completed. Returning to " + clocation.rm_name,false);
									else
										SkillShared.OutputSpeech("Survey failed.Returning to " + clocation.rm_name, true);
									}
								else
									SkillShared.OutputSpeech("Search failed. Returning to " + clocation.rm_name,true);
								}
							else if (ss == RoomEntry.search_strategy.Y_X)
								{
								SkillShared.OutputSpeech("Starting LIDAR search",false);
								if (ls.SearchYX(ref center,ref tdirect,ref cdist))
									{
									SkillShared.OutputSpeech("Starting Kinect survey",false);
									if (ks.ConductSurvey(center,tdirect,cdist))
										SkillShared.OutputSpeech("Current survey implementation completed. Returning to " + clocation.rm_name,false);
									else
										SkillShared.OutputSpeech("Survey failed.Returning to " + clocation.rm_name, true);
									}
								else
									SkillShared.OutputSpeech("Search failed. Returning to " + clocation.rm_name,true);
								}
							else
								SkillShared.OutputSpeech("Could not determine search strategy.",true);
							}
						else
							SkillShared.OutputSpeech("Room entry failed. Returning to " + clocation.rm_name,true);
						}
					else
						SkillShared.OutputSpeech("Could not determine an room entry strategy",true);
					if (SkillShared.ccpose.coord == new Point(0,0))
						{
						if (Move.MoveBackFromEntryPt())
							ReturnStart(start,stshi);
						else
							{
							SkillShared.OutputSpeech("Attempt to back into " + clocation.rm_name + " failed.",true);
							Skills.ReturnFailed();
							}
						}
					else if (MoveToEntryPoint(ref dist))
						{
						MotionMeasureProb.ConnectionLocalize(new MotionMeasureProb.Pose(clocation.coord,clocation.orientation), SkillShared.connect.direction, SkillShared.connect.exit_width,dist);
						if (Navigate.rmi.GoToEntryPoint(SkillShared.connect))
							{
							SkillShared.OutputSpeech("Survey completed.",true);
							ReturnStart(start,stshi);
							}
						else
							{
							SkillShared.OutputSpeech("Move to " + clocation.rm_name + " failed.",true);
							Skills.ReturnFailed();
							}
						}
					else
						{
						SkillShared.OutputSpeech("Move to exit point failed.",true);
						Skills.ReturnFailed();
						}
					}
				else
					ReturnStart(start,stshi);
				}
			}

			catch(ThreadAbortException)
			{
			}

			catch(Exception ex)
			{
			SkillShared.OutputSpeech("Survey exception: " + ex.Message,true);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			Skills.ReturnFailed();
			}

			Speech.UnRegisterHandler(Speech.STOP_GRAMMAR);
			survey = null;
			Skills.CloseSkill();
			SkillShared.OutputSpeech("The room survey skill has been closed.",true);
		}


		}
	}
