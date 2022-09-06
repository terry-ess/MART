using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using AutoRobotControl;
using MapAStar;

namespace Room_Survey
	{

	class Exit

	{

		private const int MAP_SHRINK_FACTOR = 10;
		private enum MotionType { NONE, X, Y };
		private const int MIN_PT_DISTANCE = 20;
		private const int START_PT_DIST_LIMIT = 5;

		private Graphics g = null;



		static public byte[,] CreateMoveMap(byte[,] detail_map,int width,int height)

		{
			int i, x, y, w, h, j, w2, h2;
			bool open = false;
			byte[,] move_map;
			Bitmap mbm;
			string fname;

			w = width / MAP_SHRINK_FACTOR;
			if ((double) width / MAP_SHRINK_FACTOR > w)
				w += 1;
			w = 1 << (int)(Math.Log(w, 2) + 1);
			h = height / MAP_SHRINK_FACTOR;
			if ((double) height / MAP_SHRINK_FACTOR > h)
				h += 1;
			h = 1 << (int)(Math.Log(h, 2) + 1);
			move_map = new byte[w, h];
			mbm = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			for (x = 0; x < w; x++)
				for (y = 0; y < h; y++)
					{
					move_map[x, y] = (int) Room.MapCode.BLOCKED;
					mbm.SetPixel(x, y, Color.Black);
					}
			for (x = 0; x < w; x++)
				for (y = 0; y < h; y++)
					{
					w2 = x * MAP_SHRINK_FACTOR;
					h2 = y * MAP_SHRINK_FACTOR;
					open = true;
					for (i = w2;(i < w2 + MAP_SHRINK_FACTOR) && open;i++)
						for (j = h2;(j < h2 + MAP_SHRINK_FACTOR) && open;j++)
							if ((i >= width) || (j >= height) || (detail_map[i, j] == (int)AutoRobotControl.Room.MapCode.BLOCKED))
								{
								open = false;
								}
					if (open)
						{
						move_map[x, y] = (byte) Room.MapCode.CLEAR;
						mbm.SetPixel(x, y, Color.White);
						}
					}
			fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "move map " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second + "-" + SharedData.GetUFileNo() + ".jpg";
			mbm.Save(fname);
			Log.LogEntry("Saved: " + fname);
			return (move_map);
		}


		private Point ConvertMoveToDetail(Point mpt)

		{
			Point dpt = new Point();

			dpt.X = mpt.X * MAP_SHRINK_FACTOR + (MAP_SHRINK_FACTOR / 2);
			dpt.Y = mpt.Y * MAP_SHRINK_FACTOR + (MAP_SHRINK_FACTOR / 2);
			return(dpt);
		}



		private void DisplayPoint(Point pt,Brush br)

		{
			g.FillRectangle(br, pt.X - 2, pt.Y - 2, 4, 4);
		}



		private void DisplaySmallPoint(Point pt,Brush br)

		{
			g.FillRectangle(br, pt.X, pt.Y,1,1);
		}



		private bool DetermineIntermediatePts(Point ept,Point spt,ref ArrayList al,byte[,] move_map,byte[,] detail_map)

		{
			bool rtn = false;
			PathFinderFast pff;
			MotionType last_mt = MotionType.NONE;
			Point last,end,start;
			Point mp, pt1, pt2, pt3;
			List<PathFinderFast.PathFinderNode> path = null;
			int mdist, direct, i;
			NavCompute.pt_to_pt_data ppd;
			ArrayList ptp = new ArrayList();
			bool ur_error = false;
			ArrayList obs = new ArrayList();
			MapCompute.obs_correct obc;

			try
			{
			Log.LogEntry("DetermineIntermediatePts: (" + ept.X + "," + ept.Y + ")  (" + spt.X + "," + spt.Y + ")  " + al.Count + "  " + move_map.Length ); 
			end = new Point();
			end.X = ept.X / MAP_SHRINK_FACTOR;
			end.Y = ept.Y / MAP_SHRINK_FACTOR;
			start = new Point();
			start.X = spt.X / MAP_SHRINK_FACTOR;
			start.Y = spt.Y / MAP_SHRINK_FACTOR;
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
						DisplaySmallPoint(mp,Brushes.DeepPink);
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
							mdist = MapCompute.FindMapObstacle(detail_map, pt1, ppd.direc, ppd.dist, SharedData.FRONT_SONAR_CLEARANCE, 2);
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
								mdist = MapCompute.FindMapObstacle(detail_map, pt1, ppd.direc, ppd.dist, SharedData.FRONT_SONAR_CLEARANCE, 2);
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
						for (i = 0; i < ptp.Count - 1; i++)
							{
							pt1 = (Point)ptp[i];
							pt2 = (Point)ptp[i + 1];
							ppd = NavCompute.DetermineRaDirectDistPtToPt(pt2, pt1);
							mdist = MapCompute.FindMapObstacle(detail_map, pt1, ppd.direc, ppd.dist, SharedData.FRONT_SONAR_CLEARANCE, 2);
							if (mdist != -1)
								{
								if (mdist >= ppd.dist)
									{
									direct = (ppd.direc + 180) % 360;
									pt2 = NavCompute.MapPoint(new Point(0, SharedData.FRONT_SONAR_CLEARANCE - (mdist - ppd.dist)), direct, pt2, true);
									if (pt1 != pt2)
										ptp[i + 1] = pt2;
									else
										ptp.RemoveAt(i + 1);
									}
								else
									{
									obs.Clear();
									MapCompute.MapMapObstacles(detail_map, pt1, ppd.direc, ppd.dist, 2, ref obs);
									obc = MapCompute.AnalyizeMapObstacles(pt1, ppd.direc, obs, 2);
									if (obc.correctable)
										{
										pt1 = new Point((int)Math.Round(pt1.X + (obc.dist * Math.Sin(obc.direct * SharedData.DEG_TO_RAD))), (int)Math.Round(pt1.Y - (obc.dist * Math.Cos(obc.direct * SharedData.DEG_TO_RAD))));
										pt2 = new Point((int)Math.Round(pt2.X + (obc.dist * Math.Sin(obc.direct * SharedData.DEG_TO_RAD))), (int)Math.Round(pt2.Y - (obc.dist * Math.Cos(obc.direct * SharedData.DEG_TO_RAD))));
										ppd = NavCompute.DetermineRaDirectDistPtToPt(pt2, pt1);
										mdist = MapCompute.FindMapObstacle(detail_map, pt1, ppd.direc, ppd.dist, SharedData.FRONT_SONAR_CLEARANCE, 2);
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
											break;
											}
										}
									else
										{
										ur_error = true;
										Log.LogEntry("Could not correct for 'side obstacle' for " + pt1 + " to" + pt2);
										break;
										}
									}
								}
							}
						if (!ur_error)
							{
							ptp.RemoveAt(0);
							al = (ArrayList) ptp.Clone();
							rtn = true;
							foreach (Point pt in ptp)
								DisplayPoint(pt,Brushes.Blue);
							}
						}
					else
						{
						ptp.RemoveAt(0);
						al = (ArrayList) ptp.Clone();
						rtn = true;
						foreach (Point pt in ptp)
							DisplayPoint(pt,Brushes.Blue);
						}
					}
				else
					Log.LogEntry("No path found.");
				}
			else
				Log.LogEntry("Could not initialize fast path finder.");
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



		public bool MoveToReadyMoveExit(RoomSurvey rs)

		{
			bool rtn = false;
			Point mpt = new Point(),start,end;
			byte[,] move_map;
			ArrayList al = new ArrayList();
			int i,direct = 0,dist = 0;
			bool obstacle = false;
			Bitmap bm;
			string fname;

			bm = SkillShared.MapToBitmap(SkillShared.fs_map);
			g = Graphics.FromImage(bm);
			start = SurveyCompute.CcToMap(SkillShared.ccpose.coord,SkillShared.fs_map_shift);
			end = SurveyCompute.CcToMap(SkillShared.fs_start, SkillShared.fs_map_shift);
			DisplayPoint(start,Brushes.Red);
			DisplayPoint(end,Brushes.Green);
			move_map = CreateMoveMap(SkillShared.fs_map,bm.Width,bm.Height);
			if (DetermineIntermediatePts(end,start, ref al,move_map,SkillShared.fs_map))
				{
				for (i = 0;i < al.Count;i++)
					{
					mpt = (Point) al[i];
					mpt = SurveyCompute.MapToCc(mpt,SkillShared.fs_map_shift);
					rtn = Move.MoveToNextRPathPt(mpt, SharedData.FRONT_SONAR_CLEARANCE, ref obstacle);
					if (!rtn && (i == al.Count - 1) && obstacle && (NavCompute.DistancePtToPt(new Point(0, 0), SkillShared.ccpose.coord) <= Room.STD_EXIT_DISTANCE))
						{
						if (rs.DetermineFinalMove(mpt, ref direct, ref dist) && Move.MoveToFinalRpathPt(direct, dist, SharedData.FRONT_SONAR_CLEARANCE))
							rtn = true;
						else
							break;
						}
					else if (!rtn)
						break;
					}
				}
			else
				Log.LogEntry("Could not determine path to exit.");
			fname = Application.StartupPath + SharedData.DATA_SUB_DIR + SkillShared.SURVEY_SUB_DIR + "Room survey return to exit  path map " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".bmp";
			bm.Save(fname);
			Log.LogEntry("Saved: " + fname);
			return(rtn);
		}

	}


	}
