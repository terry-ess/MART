using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AutoRobotControl;

namespace Room_Survey
	{
	class KinectSurvey
		{

		private const int MAX_ABS_DIST_DIF = 3;   //3 sigma for complete localization
		private const int MAG_COMPASS_SAMPLES = 5;
		private const string BASE_MAG_HEADING_FILE_NAME = "heading.cal";
		private const int MIN_FORWARD_MOVE = 6;

		private Room.feature_match nwfm = new Room.feature_match();
		private Room.feature_match wwfm = new Room.feature_match();
		private Room.feature_match c00fm = new Room.feature_match();
		private Room.feature_match ewfm = new Room.feature_match();
		private Room.feature_match cx0fm = new Room.feature_match();
		private Room.feature_match swfm = new Room.feature_match();
		private Room.feature_match c0yfm = new Room.feature_match();
		private Room.feature_match cxyfm = new Room.feature_match();
		private TextWriter sqltw = null;
		private TextWriter nmtw = null;
		private int width,height;
		private int[] mag_table = {-1,-1,-1,-1};
		private double ness_threshold;
		private string northpic = "",eastpic = "",southpic = "",westpic = "";
		private bool width_data_collected = false,height_data_collected = false;
		private bool width_data_recorded = false,height_data_recorded = false,c00_data_recored = false;


		private static bool KinectFindPerpToWall(int offset, int min_angle, int wall_direct, double ness_threshold,ref Room.feature_match fm)

		{
			bool rtn = false;
			int pa = 0, wdist = 0;
			double nsee = 0;

			if (Kinect.FindDistDirectPerpToWall(ref pa, ref nsee, ref wdist, offset, min_angle) && (nsee <= ness_threshold))
				{
				fm.orient = (wall_direct + pa - (HeadAssembly.CurrentHeadAngle() - HeadAssembly.HA_CENTER_ANGLE)) % 360;
				fm.ra = pa;
				if (fm.orient < 0)
					fm.orient += 360;
				fm.matched = true;
				fm.distance = wdist;
				Log.LogEntry("Wall perp. found with Kinect: robot calculated orientation " + fm.orient + "°, cartisian distance " + fm.distance + " in.");
				rtn = true;
				}
			else
				{
				fm.matched = false;
				Log.LogEntry("Wall perp. found with Kinect does not appear to be correct: NESE " + nsee.ToString("F4"));
				}
			return (rtn);
		}



		private void BaseDoc()

		{
			NavData.edge edge;
			int edge_offset;
			string ecoord;

			sqltw.WriteLine("/* Using CC oriented coordinates as place holders in connector entries.");
			sqltw.WriteLine("Using nominal room height and width as place holders in corner entries. */");
			edge_offset = (int) Math.Round((double)SkillShared.connect.exit_width/2);
			sqltw.WriteLine("INSERT INTO Rooms VALUES ('" + SkillShared.connect.name + "',null,null,'" + SkillShared.connect.name + ".bin','" + SkillShared.connect.name + BASE_MAG_HEADING_FILE_NAME + "',null);");
			sqltw.WriteLine("INSERT INTO Connections VALUES('" + SkillShared.exit_connect.name + "','0,0'," + SkillShared.exit_connect.exit_width + "," + ((SkillShared.connect.direction +180) % 360) + ",1);");
			edge = SkillShared.exit_connect.hc_edge;
			sqltw.WriteLine("INSERT INTO Edges VALUES(" + Convert.ToInt32(edge.type) + "," + Convert.ToInt32(edge.door_side) + "," + Convert.ToInt32(edge.ds) + "," + Convert.ToInt32(true) + ",1);");
			ecoord = ",'" + -edge_offset + ",0'";
			sqltw.WriteLine("INSERT INTO Features VALUES(" + Convert.ToInt32(NavData.FeatureType.OPENING_EDGE) + ecoord + ",1,1,null);");
			edge = SkillShared.exit_connect.lc_edge;
			sqltw.WriteLine("INSERT INTO Edges VALUES(" + Convert.ToInt32(edge.type) + "," + Convert.ToInt32(edge.door_side) + "," + Convert.ToInt32(edge.ds) + "," + Convert.ToInt32(false) + ",1);");
			ecoord = ",'" + edge_offset + ",0'";
			sqltw.WriteLine("INSERT INTO Features VALUES(" + Convert.ToInt32(NavData.FeatureType.OPENING_EDGE) + ecoord + ",1,2,null);");
		}



		private void NorthDoc()

		{
			string fname,aline;

			nmtw.WriteLine();
			if (wwfm.matched && nwfm.matched)
				{
				SkillShared.sdata.Clear();
				Rplidar.CaptureScan(ref SkillShared.sdata, true);
				aline = "\r\nNorth LIDAR scan measured room pose (" + wwfm.distance + " " + nwfm.distance + ")  " + nwfm.orient + "\r\n";
				fname = SkillShared.CaptureRoomScan(SkillShared.ccpose, aline);
				if (fname.Length > 0)
					nmtw.WriteLine("North LIDAR scan: " + fname + " with measured room pose(" + wwfm.distance + " " + nwfm.distance + ")  " + nwfm.orient );
				}
			if (northpic.Length > 0)
				nmtw.WriteLine("North facing picture :" + northpic);
			if (nwfm.matched)
				nmtw.WriteLine("North wall: " + nwfm.orient + "°, " + nwfm.distance);
			if (wwfm.matched)
				nmtw.WriteLine("West wall: " + wwfm.orient + "°, " + wwfm.distance);
			if (ewfm.matched)
				nmtw.WriteLine("East wall: " + ewfm.orient + "°, " + ewfm.distance);
			if (ewfm.matched & wwfm.matched)
				{
				nmtw.WriteLine("Nominal room width: " + width);
				sqltw.WriteLine("UPDATE Rooms SET width=" + width + " WHERE name='" + SkillShared.connect.name + "';");
				width_data_recorded = true;
				}
			if (wwfm.matched && nwfm.matched)
				sqltw.WriteLine("INSERT INTO RoomPts VALUES ('center','" + wwfm.distance + "," + nwfm.distance + "',1);");
			if (c00fm.matched)
				{
				c00_data_recored = true;
				sqltw.WriteLine("INSERT INTO Features VALUES (" + Convert.ToInt32(NavData.FeatureType.CORNER) + ",'0,0',1,null,null);");
				nmtw.WriteLine("Corner @ (0,0): " + c00fm.ra.ToString("F2") + "°, " + c00fm.distance);
				nmtw.Write("Location (" + wwfm.distance + "," + nwfm.distance + ") to ");
				nmtw.WriteLine("Corner @ (0,0):  computed distance " + (int)Math.Round(Math.Sqrt((nwfm.distance * nwfm.distance) + (wwfm.distance * wwfm.distance))) + "  measured distance " + c00fm.distance);
				}
			if (cx0fm.matched)
				{
				sqltw.WriteLine("INSERT INTO Features VALUES (" + Convert.ToInt32(NavData.FeatureType.CORNER) + ",'" + width + ",0',1,null,null);");
				nmtw.WriteLine("Corner @ (" + width + ",0): " + cx0fm.ra.ToString("F2") + "°, " + cx0fm.distance);
				nmtw.Write("Location (" + wwfm.distance + "," + nwfm.distance + ") to ");
				nmtw.WriteLine("Corner @ (" + width + ",0):  computed distance " + (int) Math.Round(Math.Sqrt((nwfm.distance * nwfm.distance) + (ewfm.distance * ewfm.distance))) + "  measured distance " + cx0fm.distance);
				}
		}



		private void EastDoc()

		{
			nmtw.WriteLine();

			if (eastpic.Length > 0)
				nmtw.WriteLine("East facing picture :" + eastpic);
			if (ewfm.matched)
				nmtw.WriteLine("East wall: " + ewfm.orient + "°, " + ewfm.distance);
			if (nwfm.matched)
				nmtw.WriteLine("North wall: " + nwfm.orient + "°, " + nwfm.distance);
			if (swfm.matched)
				nmtw.WriteLine("South wall: " + swfm.orient + "°, " + swfm.distance);
			if (nwfm.matched && swfm.matched)
				{
				nmtw.WriteLine("Nominal room height: " + height);
				sqltw.WriteLine("UPDATE Rooms SET height=" + height + " WHERE name='" + SkillShared.connect.name + "';");
				height_data_recorded = true;
				}
		}



		private void SouthDoc()

		{
			nmtw.WriteLine();
			if (southpic.Length > 0)
				nmtw.WriteLine("South facing picture :" + southpic);
			if (swfm.matched)
				nmtw.WriteLine("South wall: " + swfm.orient + "°, " + swfm.distance);
			if (wwfm.matched)
				nmtw.WriteLine("West wall: " + wwfm.orient + "°, " + wwfm.distance);
			if (ewfm.matched)
				nmtw.WriteLine("East wall: " + ewfm.orient + "°, " + ewfm.distance);
			if (!width_data_recorded && wwfm.matched && ewfm.matched)
				{
				nmtw.WriteLine("Nominal room width: " + width);
				sqltw.WriteLine("UPDATE Rooms SET width=" + width + " WHERE name='" + SkillShared.connect.name + "';");
				width_data_recorded = true;
				}
			if (c0yfm.matched && (height > 0))
				{
				sqltw.WriteLine("INSERT INTO Features VALUES (" + Convert.ToInt32(NavData.FeatureType.CORNER) + ",'0," + height + "',1,null,null);");
				nmtw.WriteLine("Corner @ (0," + height + "): " + c0yfm.ra.ToString("F2") + "°, " + c0yfm.distance);
				nmtw.Write("Location (" + wwfm.distance + "," + (height - swfm.distance) + ") to ");
				nmtw.WriteLine("Corner @ (0," + height + "):  computed distance " + (int)Math.Round(Math.Sqrt((swfm.distance * swfm.distance) + (wwfm.distance * wwfm.distance))) + "  measured distance " + c0yfm.distance);
				}
			if (cxyfm.matched && (height > 0) && (width > 0))
				{
				sqltw.WriteLine("INSERT INTO Features VALUES (" + Convert.ToInt32(NavData.FeatureType.CORNER) + ",'" + width + "," + height + "',1,null,null);");
				nmtw.WriteLine("Corner @ (" + width + "," + height + "): " + cxyfm.ra.ToString("F2") + "°, " + cxyfm.distance);
				nmtw.Write("Location (" + wwfm.distance + "," + (height - swfm.distance) + ") to ");
				nmtw.WriteLine("Corner @ (" + width + "," + height + "):  computed distance " + (int)Math.Round(Math.Sqrt((swfm.distance * swfm.distance) + (ewfm.distance * ewfm.distance))) + "  measured distance " + cxyfm.distance);
				}
		}



		private void WestDoc()

		{
			string aline,fname;

			if (!height_data_recorded && nwfm.matched && swfm.matched)
				{
				nmtw.WriteLine("Nominal room height: " + height);
				sqltw.WriteLine("UPDATE Rooms SET height=" + height + " WHERE name='" + SkillShared.connect.name + "';");
				height_data_recorded = true;
				if (wwfm.matched)
					nmtw.WriteLine("West wall: " + wwfm.orient + "°, " + wwfm.distance);
				if (nwfm.matched)
					nmtw.WriteLine("North wall: " + nwfm.orient + "°, " + nwfm.distance);
				if (swfm.matched)
					nmtw.WriteLine("South wall: " + swfm.orient + "°, " + swfm.distance);
				if (!c00_data_recored && c00fm.matched)
					{
					sqltw.WriteLine("INSERT INTO Features VALUES (" + Convert.ToInt32(NavData.FeatureType.CORNER) + ",'0,0',1,null,null);");
					nmtw.WriteLine("Corner @ (0,0): " + c00fm.ra.ToString("F2") + "°, " + c00fm.distance);
					nmtw.Write("Location (" + wwfm.distance + "," + nwfm.distance + ") to ");
					nmtw.WriteLine("Corner @ (0,0):  computed distance " + (int)Math.Round(Math.Sqrt((nwfm.distance * nwfm.distance) + (wwfm.distance * wwfm.distance))) + "  measured distance " + c00fm.distance);
					}
				if (c0yfm.matched && (height > 0))
					{
					sqltw.WriteLine("INSERT INTO Features VALUES (" + Convert.ToInt32(NavData.FeatureType.CORNER) + ",'0," + height + "',1,null,null);");
					nmtw.WriteLine("Corner @ (0," + height + "): " + c0yfm.ra.ToString("F2") + "°, " + c0yfm.distance);
					nmtw.Write("Location (" + wwfm.distance + "," + (height - swfm.distance) + ") to ");
					nmtw.WriteLine("Corner @ (0," + height + "):  computed distance " + (int)Math.Round(Math.Sqrt((swfm.distance * swfm.distance) + (wwfm.distance * wwfm.distance))) + "  measured distance " + c0yfm.distance);
					}
				if (cxyfm.matched && (height > 0) && (width > 0))
					{
					sqltw.WriteLine("INSERT INTO Features VALUES (" + Convert.ToInt32(NavData.FeatureType.CORNER) + ",'" + width + "," + height + "',1,null,null);");
					nmtw.WriteLine("Corner @ (" + width + "," + height + "): " + cxyfm.ra.ToString("F2") + "°, " + cxyfm.distance);
					nmtw.Write("Location (" + wwfm.distance + "," + (height - swfm.distance) + ") to ");
					nmtw.WriteLine("Corner @ (" + width + "," + height + "):  computed distance " + (int)Math.Round(Math.Sqrt((swfm.distance * swfm.distance) + (ewfm.distance * ewfm.distance))) + "  measured distance " + cxyfm.distance);
					}
				if (wwfm.matched && nwfm.matched)
					{
					SkillShared.sdata.Clear();
					Rplidar.CaptureScan(ref SkillShared.sdata, true);
					aline = "\r\nWest LIDAR scan measured room pose (" + wwfm.distance + " " + nwfm.distance + ")  " + nwfm.orient + "\r\n";
					fname = SkillShared.CaptureRoomScan(SkillShared.ccpose, aline);
					if (fname.Length > 0)
						nmtw.WriteLine("West LIDAR scan: " + fname + " with measured room pose(" + wwfm.distance + " " + nwfm.distance + ")  " + nwfm.orient);
					}
				}
			else if (!c00_data_recored && wwfm.matched && nwfm.matched)
				{
				if (wwfm.matched)
					nmtw.WriteLine("West wall: " + wwfm.orient + "°, " + wwfm.distance);
				if (nwfm.matched)
					nmtw.WriteLine("North wall: " + nwfm.orient + "°, " + nwfm.distance);
				if (c00fm.matched)
					{
					sqltw.WriteLine("INSERT INTO Features VALUES (" + Convert.ToInt32(NavData.FeatureType.CORNER) + ",'0,0',1,null,null);");
					nmtw.WriteLine("Corner @ (0,0): " + c00fm.ra.ToString("F2") + "°, " + c00fm.distance);
					nmtw.Write("Location (" + wwfm.distance + "," + nwfm.distance + ") to ");
					nmtw.WriteLine("Corner @ (0,0):  computed distance " + (int)Math.Round(Math.Sqrt((nwfm.distance * nwfm.distance) + (wwfm.distance * wwfm.distance))) + "  measured distance " + c00fm.distance);
					}
				if (wwfm.matched && nwfm.matched)
					{
					SkillShared.sdata.Clear();
					Rplidar.CaptureScan(ref SkillShared.sdata, true);
					aline = "\r\nWest LIDAR scan measured room pose (" + wwfm.distance + " " + nwfm.distance + ")  " + nwfm.orient + "\r\n";
					fname = SkillShared.CaptureRoomScan(SkillShared.ccpose, aline);
					if (fname.Length > 0)
						nmtw.WriteLine("West LIDAR scan: " + fname + " with measured room pose(" + wwfm.distance + " " + nwfm.distance + ")  " + nwfm.orient);
					}
				}
			else if (wwfm.matched)
				nmtw.WriteLine("West wall: " + wwfm.orient + "°, " + wwfm.distance);
		}



		private bool TurnToDirection(int direct,ref Room.feature_match fm,ref bool turned)

		{
			bool rtn = false;
			int tdirect,angle;

			tdirect = direct - SkillShared.connect.direction;
			if (tdirect < 0)
				tdirect += 360;
			angle = NavCompute.AngularDistance(tdirect,SkillShared.ccpose.orient);
			if (NavCompute.ToRightDirect(SkillShared.ccpose.orient,tdirect))
				angle *= -1;
			turned = false;
			if (Move.TurnToAngle(angle))
				{
				turned = true;
				do
					{
					if (KinectFindPerpToWall(0, 10, direct, ness_threshold, ref fm))
						{
						SkillShared.ccpose.orient = fm.orient - SkillShared.connect.direction;
						if (SkillShared.ccpose.orient < 0)
							SkillShared.ccpose.orient += 360;
						if ((angle = NavCompute.AngularDistance(direct, fm.orient)) > SharedData.MIN_TURN_ANGLE)
							{
							if (NavCompute.ToRightDirect(SkillShared.ccpose.orient, tdirect))
								angle *= -1;
							if (!Move.TurnToAngle(angle))
								break;
							}
						}
					else
						{
						fm.matched = false;
						break;
						}
					}
				while (NavCompute.AngularDistance(direct, fm.orient) > SharedData.MIN_TURN_ANGLE);
				}
			if (fm.matched && NavCompute.AngularDistance(direct, fm.orient) <= SharedData.MIN_TURN_ANGLE)
				rtn = true;
			return (rtn);
		}


		
/*		private bool TurnToDirection(int direct, ref Room.feature_match fm, ref bool turned)

			{
			bool rtn = false;

			do
				{
				if (Move.TurnToFaceDirect(direct))
					turned = true;
				else
					{
					turned = false;
					break;
					}
				if (KinectFindPerpToWall(0, 10, direct, ness_threshold, ref fm))
					SkillShared.ccpose.orient = fm.orient;
				else
					{
					fm.matched = false;
					break;
					}
				}
			while (NavCompute.AngularDistance(direct,fm.orient) > SharedData.MIN_TURN_ANGLE);
			if (fm.matched && NavCompute.AngularDistance(direct,fm.orient ) <= SharedData.MIN_TURN_ANGLE)
				rtn = true;
			return (rtn);
		} */



		private void SaveMagHeadingTable()

		{
			string fname;
			TextWriter tw;
			int i;

			fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + SkillShared.connect.name + BASE_MAG_HEADING_FILE_NAME;
			tw = File.CreateText(fname);
			if (tw != null)
				{
				for (i = 0; i < mag_table.Length; i++)
					tw.WriteLine(mag_table[i]);
				tw.Close();
				}
		}



		private void SaveMagHeading(int direct)

		{
			int i;
			int sum = 0;

			for (i = 0;i < MAG_COMPASS_SAMPLES;i++)
				sum += HeadAssembly.GetMagneticHeading();
			mag_table[direct/90] = (int) Math.Round((double) sum/MAG_COMPASS_SAMPLES);
		}



		private bool MoveToCenter(Point ctpt,int tdirect,int movdist)

		{
			bool rtn = false;
			SurveyCompute.pt_to_pt_data ppd;
			int modist,sdist,mdist;
			ArrayList obs = new ArrayList();
			Move.obs_data odata = new Move.obs_data(true);

			ppd = SurveyCompute.DetermineDirectDistPtToPt(ctpt, SkillShared.ccpose.coord);
			if (Move.ReturnToNextPt(ppd.direc, ppd.dist, 0))
				{
				if (Move.TurnToFaceDirect(tdirect))
					{
					if (movdist > Math.Ceiling(SharedData.REAR_TURN_RADIUS + 1))
						{
						sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.FRONT);
						modist = SurveyCompute.FindObstacles(0, -1, SkillShared.sdata, ref obs);
						mdist = Math.Min(sdist, modist) - (int) Math.Ceiling(SharedData.REAR_TURN_RADIUS + 1);
						mdist = Math.Min(mdist,movdist - (int)Math.Ceiling(SharedData.REAR_TURN_RADIUS + 1));
						if (mdist > MIN_FORWARD_MOVE)
							{
							if (Move.MoveForward(mdist,(int) Math.Ceiling(SharedData.REAR_TURN_RADIUS + 1),ref odata))
								rtn = true;
							else if (odata.found)
								{
								mdist = odata.mod - (int) Math.Ceiling(SharedData.REAR_TURN_RADIUS + 1) - 1;
								if (mdist > MIN_FORWARD_MOVE)
									if (Move.MoveForward(mdist,(int) Math.Ceiling(SharedData.REAR_TURN_RADIUS + 1),ref odata))
										rtn = true;
									else
										Log.LogEntry("Attempt to move to approximate center failed.");
								else
									rtn = true;
								}
							else
								Log.LogEntry("Attempt to move to approximate center failed.");
							}
						else
							{
							if ((obs.Count > 0) && (modist < Math.Ceiling(SharedData.REAR_TURN_RADIUS + 2)))
								{
								modist = (int)(Math.Ceiling(SharedData.REAR_TURN_RADIUS + 2) - modist);
								if (Move.MoveBack(modist))
									{
									SkillShared.searchd.ct_pt = new Point(0,0);
									rtn = true;
									}
								else
									Log.LogEntry("Attempt to provide turn space failed.");
								}
							else
								{
								SkillShared.searchd.ct_pt = new Point(0, 0);
								rtn = true;
								}
							}
						}
					else
						{
						modist = SurveyCompute.FindObstacles(0, -1, SkillShared.sdata, ref obs);
						if ((obs.Count > 0) && (modist < Math.Ceiling(SharedData.REAR_TURN_RADIUS + 2)))
							{
							modist = (int)(Math.Ceiling(SharedData.REAR_TURN_RADIUS + 2) - modist);
							if (Move.MoveBack(modist))
								{
								SkillShared.searchd.ct_pt = new Point(0,0);
								rtn = true;
								}
							else
								Log.LogEntry("Attempt to provide turn space failed.");
							}
						else
							{
							SkillShared.searchd.ct_pt = new Point(0, 0);
							rtn = true;
							}
						}
					}
				else
					Log.LogEntry("Attempt to face approximate center failed.");
				}
			else
				Log.LogEntry("Attempt to move to approximate center turn point failed.");
			return (rtn);
		}



		private void CollectNorthData()

		{
			int direct, cdist;
			FeatureMatch fm;
			NavData.feature f;
			bool turned = false;

			if (TurnToDirection(0, ref nwfm, ref turned))
				{
				northpic = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room Kinect survey North facing picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				if (!SkillShared.CapturePicture(northpic))
					northpic = "";
				SaveMagHeading(0);
				HeadAssembly.Pan(-90, true);
				if (KinectFindPerpToWall(0, 10, 270, ness_threshold, ref wwfm))
					{
					direct = (int)Math.Round((Math.Atan(((double)wwfm.distance / nwfm.distance)) * SharedData.RAD_TO_DEG) + nwfm.ra);
					HeadAssembly.Pan(-direct, true);
					fm = new Corner();
					f = new NavData.feature();
					f.type = NavData.FeatureType.CORNER;
					f.coord = new Point(0, 0);
					c00fm = fm.MatchKinect(f, false, false);
					if (c00fm.matched)
						{
						c00fm.distance = (int)Math.Round(Kinect.CorrectedDistance(c00fm.distance) / Math.Cos(c00fm.ra * SharedData.DEG_TO_RAD));
						cdist = (int)Math.Round(Math.Sqrt((nwfm.distance * nwfm.distance) + (wwfm.distance * wwfm.distance)));
						if (Math.Abs(cdist - c00fm.distance) > MAX_ABS_DIST_DIF)
							{
							c00fm.matched = false;
							Log.LogEntry("Distance mismatch finding the 0,0 corner");
							}
						}
					else
						Log.LogEntry("Attempt to find the 0,0 corner failed.");
					}
				else
					Log.LogEntry("Attempt to find the west wall failed.");
				HeadAssembly.Pan(90, true);
				if (KinectFindPerpToWall(0, 10, 90, ness_threshold, ref ewfm))
					{
					if (wwfm.matched)
						{
						width = ewfm.distance + wwfm.distance + SharedData.KINECT_180_DEPTH;
						width_data_collected = true;
						}
					direct = (int)Math.Round((Math.Atan(((double)ewfm.distance / nwfm.distance)) * SharedData.RAD_TO_DEG) + nwfm.ra);
					HeadAssembly.Pan(direct, true);
					fm = new Corner();
					f = new NavData.feature();
					f.type = NavData.FeatureType.CORNER;
					f.coord = new Point(width, 0);
					cx0fm = fm.MatchKinect(f, false, false);
					if (cx0fm.matched)
						{
						cx0fm.distance = (int)Math.Round(Kinect.CorrectedDistance(cx0fm.distance) / Math.Cos(cx0fm.ra * SharedData.DEG_TO_RAD));
						cdist = (int)Math.Round(Math.Sqrt((nwfm.distance * nwfm.distance) + (ewfm.distance * ewfm.distance)));
						if (Math.Abs(cdist - cx0fm.distance) > MAX_ABS_DIST_DIF)
							{
							cx0fm.matched = false;
							Log.LogEntry("Distance mismatch finding the X,0 corner");
							}
						}
					else
						Log.LogEntry("Attempt to find the x,0 corner failed.");
					}
				else
					Log.LogEntry("Attempt to find east wall failed.");
				HeadAssembly.Pan(0, true);
				}
			else
				Log.LogEntry("Attempt to face north wall failed.");
		}



		private void CollectEastData()

		{
			bool turned = false;

			ewfm.matched = false;
			nwfm.matched = false;
			if (TurnToDirection(90,ref ewfm,ref turned))
				{
				SaveMagHeading(90);
				}
			if (turned)
				{
				eastpic = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room Kinect survey East facing picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				if (!SkillShared.CapturePicture(eastpic))
					eastpic = "";
				HeadAssembly.Pan(90,true);
				if (KinectFindPerpToWall(0,10,180,ness_threshold,ref swfm))
					{
					HeadAssembly.Pan(-90,true);
					if (KinectFindPerpToWall(0,10,0,ness_threshold,ref nwfm))
						{
						if (swfm.matched)
							{
							height = nwfm.distance + swfm.distance + SharedData.KINECT_180_DEPTH;
							height_data_collected = true;
							}
						}
					else
						Log.LogEntry("Attempt to find north wall facing east failed.");
					}
				else
					Log.LogEntry("Attempt to find south wall facing east failed.");
				HeadAssembly.Pan(0,true);
				}
			else
				Log.LogEntry("Attempt to face east wall failed.");
		}



		private void CollectSouthData()

		{
			int direct, cdist;
			FeatureMatch fm;
			NavData.feature f;
			bool turned = false;

			swfm.matched = false;
			ewfm.matched = false;
			wwfm.matched = false;
			if (TurnToDirection(180,ref swfm,ref turned))
				{
				southpic = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room Kinect survey South facing picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				if (!SkillShared.CapturePicture(southpic))
					southpic = "";
				SaveMagHeading(180);
				HeadAssembly.Pan(-90,true);
				if (KinectFindPerpToWall(0,10,90,ness_threshold,ref ewfm))
					{
					direct = (int) Math.Round((Math.Atan(((double) ewfm.distance/swfm.distance)) * SharedData.RAD_TO_DEG) + swfm.ra);
					HeadAssembly.Pan(-direct,true);
					fm = new Corner();
					f = new NavData.feature();
					f.type = NavData.FeatureType.CORNER;
					f.coord = new Point(width,height);
					cxyfm = fm.MatchKinect(f,false,false);
					if (cxyfm.matched)
						{
						cxyfm.distance = (int) Math.Round(Kinect.CorrectedDistance(cxyfm.distance) / Math.Cos(c00fm.ra * SharedData.DEG_TO_RAD));
						cdist = (int) Math.Round(Math.Sqrt((swfm.distance * swfm.distance) + (ewfm.distance * ewfm.distance)));
						if (Math.Abs(cdist - cxyfm.distance) > MAX_ABS_DIST_DIF)
							{
							cxyfm.matched = false;
							Log.LogEntry("Distance mismatch finding the X,Y corner");
							}
						}
					else
						Log.LogEntry("Attempt to find the x,y corner failed.");
					}
				else
					Log.LogEntry("Attempt to find east wall failed.");
				HeadAssembly.Pan(90,true);
				if (KinectFindPerpToWall(0,10,270,ness_threshold,ref wwfm))
					{
					if (!width_data_collected && ewfm.matched)
						{
						width = ewfm.distance + wwfm.distance + SharedData.KINECT_180_DEPTH;
						width_data_collected = true;
						}
					direct = (int) Math.Round((Math.Atan(((double) wwfm.distance/swfm.distance)) * SharedData.RAD_TO_DEG) + swfm.ra);
					HeadAssembly.Pan(direct,true);
					fm = new Corner();
					f = new NavData.feature();
					f.type = NavData.FeatureType.CORNER;
					f.coord = new Point(0,height);
					c0yfm = fm.MatchKinect(f,false,false);
					if (c0yfm.matched)
						{
						c0yfm.distance = (int) Math.Round(Kinect.CorrectedDistance(c0yfm.distance) / Math.Cos(c00fm.ra * SharedData.DEG_TO_RAD));
						cdist = (int) Math.Round(Math.Sqrt((swfm.distance * swfm.distance) + (wwfm.distance * wwfm.distance)));
						if (Math.Abs(cdist - c0yfm.distance) > MAX_ABS_DIST_DIF)
							{
							c0yfm.matched = false;
							Log.LogEntry("Distance mismatch finding the 0,Y corner");
							}
						}
					else
						Log.LogEntry("Attempt to find the 0,y corner failed.");
					}
				else
					Log.LogEntry("Attempt to find west wall failed.");
				HeadAssembly.Pan(0,true);
				}
			else
				Log.LogEntry("Attempt to face south wall failed.");
		}



		private void CollectWestData()

		{
			int direct, cdist;
			FeatureMatch fm;
			NavData.feature f;
			bool turned = false;


			swfm.matched = false;
			nwfm.matched = false;
			wwfm.matched = false;
			if (TurnToDirection(270, ref wwfm, ref turned))
				{
				westpic = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room Kinect survey West facing picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				if (SkillShared.CapturePicture(westpic))
					{
					nmtw.WriteLine();
					nmtw.WriteLine("West facing picture: " + westpic);
					}
				SaveMagHeading(270);
				if (!height_data_collected || !c00fm.matched)
					{
					HeadAssembly.Pan(90, true);
					if (KinectFindPerpToWall(0, 10,0, ness_threshold, ref nwfm))
						{
						if (!c00fm.matched && wwfm.matched && nwfm.matched)
							{
							direct = (int)Math.Round((Math.Atan(((double)wwfm.distance / nwfm.distance)) * SharedData.RAD_TO_DEG) + nwfm.ra);
							HeadAssembly.Pan(direct, true);
							fm = new Corner();
							f = new NavData.feature();
							f.type = NavData.FeatureType.CORNER;
							f.coord = new Point(0, 0);
							c00fm = fm.MatchKinect(f, false, false);
							if (c00fm.matched)
								{
								c00fm.distance = (int)Math.Round(Kinect.CorrectedDistance(c00fm.distance) / Math.Cos(c00fm.ra * SharedData.DEG_TO_RAD));
								cdist = (int)Math.Round(Math.Sqrt((nwfm.distance * nwfm.distance) + (wwfm.distance * wwfm.distance)));
								if (Math.Abs(cdist - c00fm.distance) > MAX_ABS_DIST_DIF)
									{
									c00fm.matched = false;
									Log.LogEntry("Distance mismatch finding the 0,0 corner");
									}
								}
							else
								Log.LogEntry("Attempt to find the 0,0 corner failed.");
							}
						if (!height_data_collected)
							{
							HeadAssembly.Pan(-90, true);
							if (KinectFindPerpToWall(0, 10, 0, ness_threshold, ref swfm))
								{
								height = nwfm.distance + swfm.distance + SharedData.KINECT_180_DEPTH;
								height_data_collected = true;
								}
							else
								Log.LogEntry("Attempt to find south wall facing west failed.");
							}
						}
					else
						Log.LogEntry("Attempt to find north wall facing west failed.");
					HeadAssembly.Pan(0, true);
					}
				}

		}



		public bool ConductSurvey(Point ctpt,int tdirect,int mdist)

		{
			bool rtn = false;
			SensorFusion sf = new SensorFusion();
			string fname;

			try
			{
			if (MoveToCenter(ctpt,tdirect,mdist))
				{
				ness_threshold = sf.GetNeseThreshold();
				fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + SkillShared.connect.name + ".sql";
				sqltw = File.CreateText(fname);
				fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + SkillShared.connect.name + " nominal measurements " + ".txt";
				nmtw = File.CreateText(fname);
				nmtw.WriteLine(SkillShared.connect.name + " nominal measurements");
				nmtw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				BaseDoc();
				CollectNorthData();
				NorthDoc();
				CollectEastData();
				EastDoc();
				CollectSouthData();
				SouthDoc();
				CollectWestData();
				WestDoc();
				HeadAssembly.Pan(0,true);
				SaveMagHeadingTable();
				sqltw.Close();
				nmtw.Close();
				rtn = true;
				}
			else
				Log.LogEntry("Move to approximate center point failed.");
			Rplidar.CaptureScan(ref SkillShared.sdata, true);
			}

			catch(Exception ex)
			{
			Log.LogEntry("Survey completion exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			HeadAssembly.Pan(0,true);
			if (sqltw != null)
				sqltw.Close();
			if (nmtw != null)
				nmtw.Close();
			}

			return(rtn);
		}

		}
	}
