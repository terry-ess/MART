using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AutoRobotControl;


namespace Room_Survey
	{
	class LidarSearch
		{

		public const int MOVE_DIST = 24;
		private const double ACCEPT_MARGIN = .75;
		private const int PATH_ADJUST_LIMIT = 10;
		private const int SIDE_CLEARENCE = 5;


		private ArrayList yscans = new ArrayList();
		private ArrayList xscans = new ArrayList();
		private ArrayList yscans_raw_files = new ArrayList();
		private ArrayList xscans_raw_files = new ArrayList();
		private byte[,] ymap, xmap;
		private Bitmap fbmp;
		private RoomSurvey.RoomSize yrm_size, xrm_size;
		private int height, width;
		private Point center;
		private string ysearchpic = "",xsearchpic = "";
		private byte[,] map;


		private void SaveYSearchResults()

		{
			string fname;
			TextWriter dstw = null;
			int i,j,x,y;
			ArrayList scan;
			MotionMeasureProb.Pose cpose;
			RoomSurvey.ExtScanData esd;

			fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room survey Y search scan data set " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
			dstw = File.CreateText(fname);
			if (dstw != null)
				{

				try
				{
				dstw.WriteLine("Room survey Y search scan data set (with PDF margins)");
				dstw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				dstw.WriteLine();
				dstw.WriteLine("");
				dstw.WriteLine(",Location");
				dstw.WriteLine("Scan,Estimated");
				for (i = 0;i < yscans.Count;i++)
					{
					scan = (ArrayList) yscans[i];
					cpose = (MotionMeasureProb.Pose) scan[0];
					dstw.WriteLine(i + ",(" + cpose.coord.X + " " + cpose.coord.Y + ")");
					}
				dstw.WriteLine();
				dstw.WriteLine("X,Y");
				for (i = 0; i < yscans.Count; i++)
					{
					scan = (ArrayList) yscans[i];
					cpose = (MotionMeasureProb.Pose) scan[0];
					for (j = 1;j < scan.Count;j++)
						{
						esd = (RoomSurvey.ExtScanData) scan[j];
						x = esd.coord.X + cpose.coord.X;
						y = esd.coord.Y + cpose.coord.Y;
						dstw.WriteLine(x + "," + y);
						}
					dstw.WriteLine();
					dstw.WriteLine();
					}
				}

				catch(Exception ex)
				{
				Log.LogEntry("Save search results exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}
				
				dstw.Close();
				Log.LogEntry("Saved " + fname);
				}
			}



		private void SaveXSearchResults()

		{
			string fname;
			TextWriter dstw = null;
			int i,j,x,y;
			ArrayList scan;
			MotionMeasureProb.Pose cpose;
			RoomSurvey.ExtScanData esd;

			fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room survey X search scan data set " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
			dstw = File.CreateText(fname);
			if (dstw != null)
				{

				try
				{
				dstw.WriteLine("Room survey X search scan data set (with PDF margins)");
				dstw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				dstw.WriteLine();
				dstw.WriteLine(",Location");
				dstw.WriteLine("Scan,Estimated");
				for (i = 0;i < xscans.Count;i++)
					{
					scan = (ArrayList) xscans[i];
					cpose = (MotionMeasureProb.Pose) scan[0];
					dstw.WriteLine(i + ",(" + cpose.coord.X + " " + cpose.coord.Y + ")");
					}
				dstw.WriteLine();
				dstw.WriteLine();
				dstw.WriteLine("X,Y");
				for (i = 0; i < xscans.Count; i++)
					{
					scan = (ArrayList) xscans[i];
					cpose = (MotionMeasureProb.Pose) scan[0];
					for (j = 1;j < scan.Count;j++)
						{
						esd = (RoomSurvey.ExtScanData) scan[j];
						x = esd.coord.X + cpose.coord.X;
						y = esd.coord.Y + cpose.coord.Y;
						dstw.WriteLine(x + "," + y);
						}
					dstw.WriteLine();
					dstw.WriteLine();
					}
				}

				catch(Exception ex)
				{
				Log.LogEntry("Save search results exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}
				
				dstw.Close();
				Log.LogEntry("Saved " + fname);
				}
		}



		private string SaveMap(string title,byte[,] map)

		{
			Bitmap bm;
			string fname;

			fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room survey " + title + " composite map " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".bmp";
			bm = SkillShared.MapToBitmap(map);
			bm.Save(fname);
			Log.LogEntry("Saved " + fname);
			return(fname);
		}



		private bool YSearchSurvey()

		{
			int i,j,max_x = 0;
			MotionMeasureProb.Pose cpose;
			bool rtn = true;
			double px, py;
			string fname = "",sfname;
			ArrayList scan;
			RoomSurvey.ExtScanData esd;
			Point mpt = new Point();
			TextWriter tw;

			SkillShared.OutputSpeech("Mapping the Y search survey data.",false);
			sfname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room survey Y search results " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".txt";
			tw = File.CreateText(sfname);
			tw.WriteLine("Room survey Y search results (CC orientation)");
			tw.WriteLine(DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToShortTimeString());
			tw.WriteLine();
			if (ysearchpic.Length > 0)
				tw.WriteLine("Start picture: " + ysearchpic);
			SurveyCompute.SurveySize(yscans,ref height,ref width,ref max_x);
			tw.WriteLine("Survey determined room size: height " + height + "  width " + width);
			yrm_size = new RoomSurvey.RoomSize(height,width,max_x);
			py = SkillShared.YMS * (yscans.Count - 1);			//PDF margins for map
			px = SkillShared.XMS * (yscans.Count - 1);
			tw.WriteLine("Survey max Y deviation " + py + "  X deviation " + px);
			SkillShared.map_shift.X = width - max_x + (int)Math.Round(px);
			SkillShared.map_shift.Y = height + (int)Math.Round(py);
			tw.WriteLine("Map shift X " + SkillShared.map_shift.X + "  Y " + SkillShared.map_shift.Y);
			height += (int) Math.Ceiling(2 * py);
			width += (int) Math.Ceiling(2 * px);
			center.X = max_x - (int)Math.Round((double) width/2);
			center.Y = (int) Math.Round((double) height/2);
			tw.WriteLine("Map dimensions: X " + width + "  Y " + height);
			tw.WriteLine();
			tw.WriteLine("Raw scan data files: ");
			for (i = 0;i < yscans_raw_files.Count;i++)
				tw.WriteLine("  " + (string) yscans_raw_files[i]);
			tw.Close();
			Log.LogEntry("Saved " + sfname);
			ymap = new byte[width + 2, height + 2];
			for (i = 0;i < width + 2;i++)
				for (j = 0;j < height + 2;j++)
					ymap[i,j] = (byte) AutoRobotControl.Room.MapCode.CLEAR;
			for (i = 0;i < yscans.Count;i++)
				{
				scan = (ArrayList)yscans[i];
				cpose = (MotionMeasureProb.Pose) scan[0];

				try
				{
				for (j = 1;j < scan.Count;j++) 
					{
					esd = (RoomSurvey.ExtScanData) scan[j];
					mpt = SurveyCompute.CcToMap(esd.coord);
					mpt.X += cpose.coord.X;
					mpt.Y -= cpose.coord.Y;
					ymap[mpt.X,mpt.Y] = (byte)AutoRobotControl.Room.MapCode.BLOCKED;
					}
				}

				catch(Exception ex)
				{
				Log.LogEntry("YSearchSurvey exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				Log.LogEntry("x " + mpt.X + "  y " + mpt.Y);
				rtn = false;
				}

				fname = SaveMap("Y search step " + i,ymap);
				}
			SaveYSearchResults();
			fbmp = new Bitmap(fname);
			return(rtn);
		}



		private bool DetermineXSearch(double mcount,ref double tpmoves,ref int mdirec,ref int odist)

		{
			bool rtn = false;
			int i,dist,max_dist = 0,max_mov = -1,direct,mdist;
			string fname;
			Point mcoord = new Point();

			if ((yrm_size.width/2) > yrm_size.max_x)
				direct = 270;
			else
				direct = 90;
			Log.LogEntry("DetermineXSearch search direction " + direct);
			mdist = (int) Math.Floor((double) MOVE_DIST/2);
			mcount = (mcount * 2) - 1;
			Log.LogEntry("Number of X searches: " + mcount);
			mcoord = SurveyCompute.CcToMap(SkillShared.ccpose.coord);
			for (i = 0;i < mcount;i++) 
				{
				dist = SurveyCompute.FindMapObstacle(ref ymap,mcoord,direct,ref fbmp);
				mcoord.Y += mdist;
				if (dist > max_dist)
					{
					max_dist = dist;
					max_mov = i;
					}
				}
			fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room survey X search map " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".bmp";
			fbmp.Save(fname);
			Log.LogEntry("Saved " + fname);
			if (max_dist >= SkillShared.MIN_DIST_FOR_SEARCH)
				{
				rtn = true;
				mdirec = direct;
				tpmoves = ((double) max_mov/2);
				odist = max_dist;
				Log.LogEntry("X search parameters:  # moves to turn pt " + tpmoves + "  X search direction " + direct + "  expected X search distance " + odist);
				}
			else
				Log.LogEntry("Could not determine X search parameters.");
			return(rtn);
		}



		private bool XSearchSurvey()

		{
			int i,j,max_x = 0;
			MotionMeasureProb.Pose cpose;
			bool rtn = true;
			double px, py;
			string fname = "",sfname;
			ArrayList scan;
			RoomSurvey.ExtScanData esd;
			Point mpt = new Point();
			TextWriter tw;

			SkillShared.OutputSpeech("Mapping the X search survey data.",false);
			sfname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room survey X search results " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".txt";
			tw = File.CreateText(sfname);
			tw.WriteLine("Room survey X search results (CC orientation)");
			tw.WriteLine(DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToShortTimeString());
			tw.WriteLine();
			if (xsearchpic.Length > 0)
				tw.WriteLine("Start picture: " + xsearchpic);
			SurveyCompute.SurveySize(xscans,ref height,ref width ,ref max_x);
			tw.WriteLine("Survey determined room size: height " + height + "  width " + width);
			xrm_size = new RoomSurvey.RoomSize(height,width,max_x);
			py = SkillShared.XMS * (xscans.Count - 1);			//PDF margins for map
			px = SkillShared.YMS * (xscans.Count - 1);
			tw.WriteLine("Survey max Y deviation " + py + "  X deviation " + px);
			SkillShared.map_shift.X = width - max_x + (int) Math.Round(px);
			SkillShared.map_shift.Y = height + (int)Math.Round(py);
			tw.WriteLine("Map shift X " + SkillShared.map_shift.X + "  Y " + SkillShared.map_shift.Y);
			height += (int) Math.Ceiling(2 * py);
			width += (int) Math.Ceiling(2 * px);
			center.X = max_x - (int)Math.Round((double) width/2);
			center.Y = (int) Math.Round((double) height/2);
			tw.WriteLine("Map dimensions: X " + width + "  Y " + height);
			tw.WriteLine();
			tw.WriteLine("Raw scan data files: ");
			for (i = 0;i < xscans_raw_files.Count;i++)
				tw.WriteLine("  " + (string) xscans_raw_files[i]);
			tw.Close();
			Log.LogEntry("Saved " + sfname);
			xmap = new byte[width + 2, height + 2];
			for (i = 0; i < width + 2; i++)
				for (j = 0; j < height + 2; j++)
					xmap[i, j] = (byte)AutoRobotControl.Room.MapCode.CLEAR;
			for (i = 0;i < xscans.Count;i++)
				{
				scan = (ArrayList) xscans[i];
				cpose = (MotionMeasureProb.Pose) scan[0];

				try
				{
				for (j = 1;j < scan.Count;j++) 
					{
					esd = (RoomSurvey.ExtScanData) scan[j];
					mpt = SurveyCompute.CcToMap(esd.coord);
					mpt.X += cpose.coord.X;
					mpt.Y -= cpose.coord.Y;
					xmap[mpt.X,mpt.Y] = (byte)AutoRobotControl.Room.MapCode.BLOCKED;
					}
				}

				catch(Exception ex)
				{
				Log.LogEntry("YSearchSurvey exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				Log.LogEntry("x " + mpt.X + "  y " + mpt.Y);
				rtn = false;
				}

				fname = SaveMap("X search step " + i,xmap);
				}
			SaveXSearchResults();
			fbmp = new Bitmap(fname);
			return(rtn);
		}



		private bool DetermineYSearch(double mcount,ref double tpmoves,ref int mdirec,ref int odist)

		{
			bool rtn = false;
			int i,dist,max_dist = 0,max_mov = -1,direct,mdist;
			string fname;
			Point mcoord = new Point();

			if (SkillShared.ccpose.coord.Y < xrm_size.height/2)
				direct = 0;
			else
				direct = 180;
			Log.LogEntry("DetermineYSearch search direction " + direct);
			mdist = (int) Math.Floor((double) MOVE_DIST/2);
			mcount = (mcount * 2) - 1;
			Log.LogEntry("Number of Y searches: " + mcount);
			mcoord = SurveyCompute.CcToMap(SkillShared.ccpose.coord);
			for (i = 0;i < mcount;i++) 
				{
				dist = SurveyCompute.FindMapObstacle(ref xmap,mcoord,direct,ref fbmp);
				if (SkillShared.ccpose.coord.X < 0)
					mcoord.X += mdist;
				else
					mcoord.X -= mdist;
				if (dist > max_dist)
					{
					max_dist = dist;
					max_mov = i;
					}
				}
			fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room survey Y search map " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".bmp";
			fbmp.Save(fname);
			Log.LogEntry("Saved " + fname);
			if (max_dist >= SkillShared.MIN_DIST_FOR_SEARCH)
				{
				rtn = true;
				mdirec = direct;
				tpmoves = ((double) max_mov/2);
				odist = max_dist;
				Log.LogEntry("Y search parameters:  # moves to turn pt " + tpmoves + "  Y search direction " + direct + "  expected Y search distance " + odist);
				}
			else
				Log.LogEntry("Could not determine Y search parameters.");
			return(rtn);
		}



		private bool PathAdjust(ArrayList sdata,ref ArrayList obs,ref int modist,int odist,bool adjust)

		{
			bool rtn = false;
			int angle = 0;

			Log.LogEntry("LIDAR based PathAdjust");
			if (SurveyCompute.ObstacleAdjustAngle(SkillShared.sdata,obs, odist,0,ref angle))
				{
				if (angle != 0)
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
							if (adjust)
								{
								angle = (SkillShared.ccpose.orient - angle) % 360;
								if (angle < 0)
									angle += 360;
								SkillShared.ccpose.orient = angle;
								}
							SkillShared.sdata.Clear();
							Rplidar.CaptureScan(ref SkillShared.sdata, true);
							obs.Clear();
							modist = SurveyCompute.FindObstacles(0, -1, SkillShared.sdata, ref obs);
							if (modist >= odist)
								rtn = true;
							else
								Log.LogEntry("Obstacle distance of " + modist + " is less then desired obstacle clearence of " + odist);
							}
						else
							Log.LogEntry("Attempt to make adjustment turn failed.");
						}
					else
						Log.LogEntry("Adjust turn angle of " + angle + " exceeds limit of " + PATH_ADJUST_LIMIT + ".");
					}
				}
			else
				Log.LogEntry("Could not determine adjust angle");
			return(rtn);
		}



		private bool PathAdjust(ref int modist,int odist,bool right_side)

		{
			bool rtn = false,right,edge = false;
			int mfdist = -1, mdist = -1, i, direct, max_mdist = 0, tangle = 0;
			Point spt;

			Log.LogEntry("Kinect based PathAdjust");
			right = right_side;
			spt = new Point((int)Math.Round((double)map.GetLength(0) / 2), map.GetLength(1) - 1);
			for (i = SharedData.MIN_TURN_ANGLE; i <= PATH_ADJUST_LIMIT; i++)
				{
				if (!right)
					direct = i;
				else
					direct = 360 - i;
				mdist = KinectExt.CheckMapObstacles(spt, direct,KinectExt.kinect_max_depth,SkillShared.STD_SIDE_CLEAR, map, ref right, ref edge);
				if (mdist == -1)
					{
					max_mdist = mdist = KinectExt.kinect_max_depth + 1;
					tangle = i;
					break;
					}
				else if (mdist > max_mdist)
					{
					max_mdist = mdist;
					tangle = i;
					}
				}
			if ((max_mdist >= odist + 1) && (tangle > 0))
				{
				if (!right)
					tangle *= -1;
				if (Move.TurnToAngle(tangle))
					{
					Log.LogEntry("Turned " + tangle + " to adjust entry.");
					map = new byte[Kinect.depth_width, (int)Math.Ceiling(Kinect.nui.DepthStream.TooFarDepth * SharedData.MM_TO_IN)];
					if (KinectExt.MapDepth(ref map, ref mfdist))
						{
						mdist = KinectExt.CheckMapObstacles(spt, 0, KinectExt.kinect_max_depth,odist, map, ref right, ref edge);
						if (mdist == -1)
							mdist = KinectExt.kinect_max_depth + 1;
						if (mdist > odist)
							rtn = true;
						else
							Log.LogEntry("Obstacle distance of " + mdist + " is less then desired obstacle clearencd of " + odist);
						modist = mdist;
						}
					}
				else
					Log.LogEntry("Attempt to make adjustment turn failed.");
				}
			else
				Log.LogEntry("Could not determine adjust angle.");
			return (rtn);
		}



		private bool YSearch(ref double acount,bool first_search)

		{
			bool rtn = false, kinect_measure = false, right = false, edge = false;
			int modist = 0, mcount = 0, i, mov_dist = MOVE_DIST;
			ArrayList obs = new ArrayList();
			Move.obs_data odata = new Move.obs_data(true);
			int mfdist = 0;
			Point spt;

			odata.obs = new ArrayList();
			Log.LogEntry("Y axis LIDAR search");
			for (i = 0; i < 2; i++)
				{
				map = new byte[Kinect.depth_width, (int)Math.Ceiling(Kinect.nui.DepthStream.TooFarDepth * SharedData.MM_TO_IN)];
				if (KinectExt.MapDepth(ref map, ref mfdist))
					{
					spt = new Point((int)Math.Round((double)map.GetLength(0) / 2), map.GetLength(1) - 1);
					modist = KinectExt.CheckMapObstacles(spt, 0, KinectExt.kinect_max_depth, SkillShared.STD_SIDE_CLEAR, map, ref right, ref edge);
					if (modist == -1)
						modist = KinectExt.kinect_max_depth + 1;
					kinect_measure = true;
					}
				else
					{
					obs.Clear();
					modist = SurveyCompute.FindObstacles(0, -1, SkillShared.sdata, ref obs);
					kinect_measure = false;
					}
				modist -= SharedData.FRONT_SONAR_CLEARANCE;
				mcount = (int)Math.Floor((double)modist / MOVE_DIST);
				if (mcount == 0)
					{
					if (i == 0)
						{
						if (kinect_measure)
							PathAdjust(ref modist, SharedData.FRONT_SONAR_CLEARANCE + MOVE_DIST, right);
						else
							PathAdjust(SkillShared.sdata,ref obs, ref modist, SharedData.FRONT_SONAR_CLEARANCE + MOVE_DIST, false);
						}
					}
				else
					break;
				}
			SkillShared.CaptureRoomScan(SkillShared.ccpose, ref yscans, ref yscans_raw_files);
			mcount = (int)Math.Floor((double)modist / MOVE_DIST);
			Log.LogEntry("Expected number moves in Y search: " + mcount);
			ysearchpic = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room survey Y LIDAR search start picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
			SkillShared.CapturePicture(ysearchpic,true);
			// capture tilted Kinect depth scan to augment LIDAR scans??
			for (i = 0; i < mcount; i++)
				{
				if (!Move.MoveForward(mov_dist, SharedData.FRONT_SONAR_CLEARANCE, ref odata))
					{
					if (odata.found)
						if (Move.MoveForward(mov_dist / 2,SharedData.FRONT_SONAR_CLEARANCE, ref odata))
							{
							SkillShared.CaptureRoomScan(SkillShared.ccpose, ref yscans, ref yscans_raw_files);
							acount += .5;
							}
						else if (odata.found)
							{
							if (Move.MoveForward(mov_dist / 4, SharedData.FRONT_SONAR_CLEARANCE, ref odata))
								{
								SkillShared.CaptureRoomScan(SkillShared.ccpose, ref yscans, ref yscans_raw_files);
								acount += .25;
								}
							}
					break;
					}
				else
					{
					SkillShared.CaptureRoomScan(SkillShared.ccpose, ref yscans, ref yscans_raw_files);
					acount += 1;
					}
				}
			if (!odata.found)
				{
				if (Move.MoveForward(mov_dist / 2,SharedData.FRONT_SONAR_CLEARANCE, ref odata))
					{
					SkillShared.CaptureRoomScan(SkillShared.ccpose, ref yscans, ref yscans_raw_files);
					acount += .5;
					}
				else if (odata.found)
					{
					if (Move.MoveForward(mov_dist / 4, SharedData.FRONT_SONAR_CLEARANCE, ref odata))
						{
						SkillShared.CaptureRoomScan(SkillShared.ccpose, ref yscans, ref yscans_raw_files);
						acount += .25;
						}
					}
				}
			Log.LogEntry("Actual number moves in Y search: " + acount);
			if (i > 0)
				{
				YSearchSurvey();
				if (first_search)
					{
					SkillShared.fs_map_shift = SkillShared.map_shift;
					SkillShared.fs_map = (byte[,]) ymap.Clone();
					}
				rtn = true;
				}
			return(rtn);
		}



		private bool XSearch(ref double acount,bool first_search)

		{
			bool rtn = false,kinect_measure = false,right = false,edge = false;
			int modist = 0, mcount = 0, i, mov_dist = MOVE_DIST;
			ArrayList obs = new ArrayList();
			Move.obs_data odata = new Room_Survey.Move.obs_data(true);
			int mfdist = 0;
			Point spt;

			odata.obs = new ArrayList();
			Log.LogEntry("X axis LIDAR search");
			for (i = 0; i < 2; i++)
				{
				map = new byte[Kinect.depth_width, (int)Math.Ceiling(Kinect.nui.DepthStream.TooFarDepth * SharedData.MM_TO_IN)];
				if (KinectExt.MapDepth(ref map, ref mfdist))
					{
					spt = new Point((int)Math.Round((double)map.GetLength(0) / 2), map.GetLength(1) - 1);
					modist = KinectExt.CheckMapObstacles(spt, 0, KinectExt.kinect_max_depth,SkillShared.STD_SIDE_CLEAR, map, ref right, ref edge);
					if (modist == -1)
						modist = KinectExt.kinect_max_depth + 1;
					kinect_measure = true;
					}
				else
					{
					obs.Clear();
					modist = SurveyCompute.FindObstacles(0, -1, SkillShared.sdata, ref obs);
					kinect_measure = false;
					}
				modist -= SharedData.FRONT_SONAR_CLEARANCE;
				mcount = (int) Math.Floor((double)modist / MOVE_DIST);
				if (mcount == 0)
					{
					if (i == 0)
						{
						if (kinect_measure)
							PathAdjust(ref modist, SharedData.FRONT_SONAR_CLEARANCE + MOVE_DIST,right);
						else
							PathAdjust(SkillShared.sdata,ref obs, ref modist, SharedData.FRONT_SONAR_CLEARANCE + MOVE_DIST, false);
						}
					}
				else
					break;
				}
			SkillShared.CaptureRoomScan(SkillShared.ccpose, ref xscans, ref xscans_raw_files);
			mcount = (int)Math.Floor((double)modist / MOVE_DIST);
			Log.LogEntry("Expected number moves in X search: " + mcount);
			acount = 0;
			xsearchpic = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room survey X LIDAR search start picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
			SkillShared.CapturePicture(xsearchpic,true);
			// capture tilted Kinect depth to augment LIDAR scans??
			for (i = 0; i < mcount; i++)
				{
				if (!Move.MoveForward(mov_dist,SharedData.FRONT_SONAR_CLEARANCE, ref odata))
					{
					if (odata.found)
						if (Move.MoveForward(mov_dist / 2,SharedData.FRONT_SONAR_CLEARANCE, ref odata))
							{
							SkillShared.CaptureRoomScan(SkillShared.ccpose, ref xscans, ref xscans_raw_files);
							acount += .5;
							}
						else if (odata.found)
							{
							if (Move.MoveForward(mov_dist / 4, SharedData.FRONT_SONAR_CLEARANCE, ref odata))
								{
								SkillShared.CaptureRoomScan(SkillShared.ccpose, ref yscans, ref yscans_raw_files);
								acount += .25;
								}
							}
					break;
					}
				else
					{
					SkillShared.CaptureRoomScan(SkillShared.ccpose, ref xscans, ref xscans_raw_files);
					acount += 1;
					}
				}
			if (!odata.found)
				{
				if (Move.MoveForward(mov_dist / 2,SharedData.FRONT_SONAR_CLEARANCE, ref odata))
					{
					SkillShared.CaptureRoomScan(SkillShared.ccpose, ref xscans, ref xscans_raw_files);
					acount += .5;
					}
				else if (odata.found)
					{
					if (Move.MoveForward(mov_dist / 4, SharedData.FRONT_SONAR_CLEARANCE, ref odata))
						{
						SkillShared.CaptureRoomScan(SkillShared.ccpose, ref yscans, ref yscans_raw_files);
						acount += .25;
						}
					}
				}
			AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.FRONT);
			Log.LogEntry("Actual number moves in X search: " + acount);
			if (i > 0)
				{
				XSearchSurvey();
				if (first_search)
					{
					SkillShared.fs_map_shift = SkillShared.map_shift;
					SkillShared.fs_map = (byte[,]) xmap.Clone();
					}
				rtn = true;
				}
			return (rtn);
		}



		private void YXDataCenter(ref Point cpt, ref int tdirect, ref int cdist)

		{
			RoomSurvey.RoomSize cntr;
			int cd;

			cntr.height = Math.Max(yrm_size.height,xrm_size.height);
			cntr.width = Math.Max(xrm_size.width,yrm_size.width);
			cntr.max_x = Math.Max(yrm_size.max_x,xrm_size.max_x);
			cpt.Y = SkillShared.ccpose.coord.Y;
			cpt.X = (int) Math.Round((double) cntr.max_x - ((double) cntr.width / 2));
			cd = (int) Math.Round((double) cntr.height / 2);
			if (SkillShared.ccpose.coord.Y < cd)
				tdirect = 0;
			else
				tdirect = 180;
			cdist = Math.Abs(SkillShared.ccpose.coord.Y - cd);
			Log.LogEntry("Approximate center turn point, direction and distance (X & Y data): " + cpt + ",  " + tdirect + ",  " + cdist);
		}



		private void XYDataCenter(ref Point cpt, ref int tdirect, ref int cdist)

		{
			RoomSurvey.RoomSize cntr;
			int cd;

			cntr.height = Math.Max(yrm_size.height,xrm_size.height);
			cntr.width = Math.Max(xrm_size.width,yrm_size.width);
			cntr.max_x = Math.Max(yrm_size.max_x,xrm_size.max_x);
			cpt.Y = (int)Math.Round((double)yrm_size.height / 2);
			cpt.X = SkillShared.ccpose.coord.X;
			cd = (int) Math.Round((double) cntr.height / 2);
			if (SkillShared.ccpose.coord.Y < cd)
				tdirect = 0;
			else
				tdirect = 180;
			cdist = Math.Abs(SkillShared.ccpose.coord.Y - cd);
			Log.LogEntry("Approximate center turn point, direction and distance (X & Y data): " + cpt + ",  " + tdirect + ",  " + cdist);
		}



		private void YDataCenter(ref Point cpt, ref int tdirect, ref int cdist)

		{
			int cd;

			cpt.Y = (int)Math.Round((double)yrm_size.height / 2);
			cpt.X = SkillShared.ccpose.coord.X;
			cd = (int)Math.Round((double)yrm_size.max_x - ((double)yrm_size.width / 2));
			if (SkillShared.ccpose.coord.X > cd)
				tdirect = 270;
			else
				tdirect = 90;
			cdist = Math.Abs(SkillShared.ccpose.coord.X - cd);
			Log.LogEntry("Approximate center turn point, direction and distance (Y data): " + cpt + ",  " + tdirect + ",  " + cdist);
		}



		private void XDataCenter(ref Point cpt, ref int tdirect, ref int cdist)

		{
			int cd;

			cpt.Y = SkillShared.ccpose.coord.Y;
			cpt.X = (int)Math.Round((double)xrm_size.max_x - ((double)xrm_size.width / 2));
			cd = (int)Math.Round((double)xrm_size.height / 2);
			if (SkillShared.ccpose.coord.Y < cd)
				tdirect = 0;
			else
				tdirect = 180;
			cdist = Math.Abs(SkillShared.ccpose.coord.Y - cd);
			Log.LogEntry("Approximate center turn point, direction and distance (X data): " + cpt + ",  " + tdirect + ",  " + cdist);
		}



		public bool SearchYX(ref Point cpt,ref int tdirect,ref int cdist)

		{
			bool rtn = false;
			int modist = 0,i,sa;
			double acount = 0;
			ArrayList obs = new ArrayList();
			int mdirec = -1,odist = -1,mov_dist = MOVE_DIST,tpdist,tpdirect,mov_margin;
			double tp_moves = -1;
			ArrayList xscan = new ArrayList();
			MotionMeasureProb.Pose mpose;
			Move.obs_data odata = new Room_Survey.Move.obs_data(true);

			Log.KeyLogEntry("LIDAR search");
			try
			{
			cpt = new Point(0,0);
			if (YSearch(ref acount,true))
				{
				if (DetermineXSearch(acount,ref tp_moves,ref mdirec,ref odist))
					{
					mpose = SkillShared.ccpose;
					tpdirect = (mpose.orient + 180) % 360;
					tpdist = (int) Math.Floor(tp_moves * mov_dist);	
					modist = -1;
					if (tpdist == 0)				
						{
						obs.Clear();
						sa = NavCompute.AngularDistance(SkillShared.ccpose.orient,mdirec);
						if (!NavCompute.ToRightDirect(SkillShared.ccpose.orient,mdirec))
							sa *= -1;
						modist = SurveyCompute.FindObstacles(sa,-1,SkillShared.sdata,SIDE_CLEARENCE,ref obs);
						if (modist <= odist * ACCEPT_MARGIN)
							{
							Move.MoveBack(SIDE_CLEARENCE);
							obs.Clear();
							sa = NavCompute.AngularDistance(SkillShared.ccpose.orient,mdirec);
							if (!NavCompute.ToRightDirect(SkillShared.ccpose.orient,mdirec))
								sa *= -1;
							modist = SurveyCompute.FindObstacles(sa,-1,SkillShared.sdata,SIDE_CLEARENCE,ref obs);
							}
						}
					else if (Move.ReturnToNextPt(tpdirect,tpdist,SharedData.FRONT_SONAR_CLEARANCE))
						{
						mov_margin = (int) Math.Round(tp_moves * SkillShared.YMS);
						for (i = 0;i < 7;i ++)
							{
							obs.Clear();
							sa = NavCompute.AngularDistance(SkillShared.ccpose.orient,mdirec);
							if (!NavCompute.ToRightDirect(SkillShared.ccpose.orient,mdirec))
								sa *= -1;
							modist = SurveyCompute.FindObstacles(sa,-1,SkillShared.sdata,SIDE_CLEARENCE,ref obs);
							if (modist > odist * ACCEPT_MARGIN)
								break;
							else
								switch(i)
									{
									case 0:
										Move.MoveBack((int) Math.Floor((double) mov_margin/3));
										break;

									case 1:
										Move.MoveBack((int) Math.Floor((double) mov_margin/3));
										break;

									case 2:
										Move.MoveBack((int)Math.Floor((double)mov_margin / 3));
										break;

									case 3:
										Move.MoveForward((int) Math.Floor(mov_margin * 1.333), SharedData.FRONT_SONAR_CLEARANCE,ref odata);
										break;

									case 4:
										Move.MoveForward((int) Math.Floor((double) mov_margin/3), SharedData.FRONT_SONAR_CLEARANCE,ref odata);
										break;

									case 5:
										Move.MoveForward((int) Math.Floor((double) mov_margin/3), SharedData.FRONT_SONAR_CLEARANCE,ref odata);
										break;
									}
							}
						}
					if (modist > odist * ACCEPT_MARGIN)
						{
						if (Move.TurnToFaceDirect(mdirec))
							{
							SkillShared.searchd.axis2t_pt = SkillShared.ccpose.coord;
							if (XSearch(ref acount,false))
								{
								YXDataCenter(ref cpt, ref tdirect, ref cdist);
								rtn = true;
								}
							else
								{
								Log.LogEntry("X search failed.");
								YDataCenter(ref cpt, ref tdirect, ref cdist);
								rtn = true;
								}
							}
						}
					else
						{
						Log.LogEntry("Could not locate X search corridor.");
						YDataCenter(ref cpt, ref tdirect, ref cdist);
						rtn = true;
						}
					}
				else
					{
					Log.LogEntry("Could not determine X search corridor.");
					YDataCenter(ref cpt, ref tdirect, ref cdist);
					rtn = true;
					}
				}
			else
				Log.LogEntry("Y search failed.");
			}

			catch(Exception ex)
			{
			SkillShared.OutputSpeech("SearchYX exception: " + ex.Message,true);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			fbmp = null;
			SkillShared.searchd.ct_pt = cpt;
			return (rtn);
		}



		public bool SearchXY(ref Point ctpt,ref int tdirect,ref int cdist)

		{
			bool rtn = false;
			int modist = 0, i,sa;
			double acount = 0;
			ArrayList obs = new ArrayList();
			int mdirec = -1, odist = -1, mov_dist = MOVE_DIST, tpdist, tpdirect, mov_margin;
			double tp_moves = -1;
			ArrayList xscan = new ArrayList();
			MotionMeasureProb.Pose mpose;
			Move.obs_data odata = new Room_Survey.Move.obs_data(true);

			Log.KeyLogEntry("LIDAR search");
			try
			{
			ctpt = new Point(0,0);
			if (XSearch(ref acount,true))
				{
				if (DetermineYSearch(acount,ref tp_moves,ref mdirec,ref odist))
					{
					mpose = SkillShared.ccpose;
					tpdirect = (mpose.orient + 180) % 360;
					tpdist = (int) Math.Floor(tp_moves * mov_dist);	
					modist = -1;
					if (tpdist == 0)				
						{
						obs.Clear();
						sa = NavCompute.AngularDistance(SkillShared.ccpose.orient,mdirec);
						if (!NavCompute.ToRightDirect(SkillShared.ccpose.orient,mdirec))
							sa *= -1;
						modist = SurveyCompute.FindObstacles(sa,-1,SkillShared.sdata,SIDE_CLEARENCE,ref obs);
						if (modist <= odist * ACCEPT_MARGIN)
							{
							Move.MoveBack(SIDE_CLEARENCE);
							obs.Clear();
							sa = NavCompute.AngularDistance(SkillShared.ccpose.orient,mdirec);
							if (!NavCompute.ToRightDirect(SkillShared.ccpose.orient,mdirec))
								sa *= -1;
							modist = SurveyCompute.FindObstacles(sa,-1,SkillShared.sdata,SIDE_CLEARENCE,ref obs);
							}
						}
					else if (Move.ReturnToNextPt(tpdirect,tpdist,SharedData.FRONT_SONAR_CLEARANCE))
						{
						mov_margin = (int) Math.Round(tp_moves * SkillShared.YMS);
						for (i = 0;i < 7;i ++)
							{
							obs.Clear();
							sa = NavCompute.AngularDistance(SkillShared.ccpose.orient,mdirec);
							if (!NavCompute.ToRightDirect(SkillShared.ccpose.orient,mdirec))
								sa *= -1;
							modist = SurveyCompute.FindObstacles(sa,-1,SkillShared.sdata,SIDE_CLEARENCE,ref obs);
							if (modist > odist * ACCEPT_MARGIN)
								break;
							else
								switch(i)
									{
									case 0:
										Move.MoveBack((int) Math.Floor((double) mov_margin/3));
										break;

									case 1:
										Move.MoveBack((int) Math.Floor((double) mov_margin/3));
										break;

									case 2:
										Move.MoveBack((int) Math.Floor((double) mov_margin/3));
										break;

									case 3:
										Move.MoveForward((int) Math.Floor(mov_margin * 1.333), SharedData.FRONT_SONAR_CLEARANCE,ref odata);
										break;

									case 4:
										Move.MoveForward((int) Math.Floor((double) mov_margin/3), SharedData.FRONT_SONAR_CLEARANCE,ref odata);
										break;

									case 5:
										Move.MoveForward((int) Math.Floor((double) mov_margin/3), SharedData.FRONT_SONAR_CLEARANCE,ref odata);
										break;
									}
							}
						}
					if (modist > odist * ACCEPT_MARGIN)
						{
						if (Move.TurnToFaceDirect(mdirec))
							{
							SkillShared.searchd.axis2t_pt = SkillShared.ccpose.coord;
							if (YSearch(ref acount,false))
								{
								XYDataCenter(ref ctpt,ref tdirect,ref cdist);
								rtn = true;
								}
							else
								{
								Log.LogEntry("Y search failed.");
								XDataCenter(ref ctpt, ref tdirect, ref cdist);
								rtn = true;
								}
							}
						}
					else
						{
						Log.LogEntry("Could not locate Y search corridor.");
						XDataCenter(ref ctpt, ref tdirect, ref cdist);
						rtn = true;
						}
					}
				else
					{
					Log.LogEntry("Could not determine Y search cooridor.");
					XDataCenter(ref ctpt, ref tdirect, ref cdist);
					rtn = true;
					}
				}
			else
				Log.LogEntry("X search failed.");
			}

			catch (Exception ex)
			{
			SkillShared.OutputSpeech("SearchXY exception: " + ex.Message, true);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			fbmp = null;
			SkillShared.searchd.ct_pt = ctpt;
			return (rtn);
		}

		}
	}
