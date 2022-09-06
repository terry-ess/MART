using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AutoRobotControl;
using Microsoft.Kinect;


namespace Work_Assist
	{
	class SSWorkSpaceInfo:WorkSpaceInfoInterface
		{

		// primary assumptions: 
		//		1. the skill in invokded immediately after a "come here" (assumption is likely but not absolutely ok, neccessary because of speaker context)
		//		2. the speaker has not moved since the "come here" (ok assumption)
		//		3. the speaker is at the "work space" (ok assumption)
		//		4. the work space is a flat surface (table,bench,desk) top (ok assumption)
		//		5. the robot is ~ 6.5 ft from the speaker (not a near "come here" of ~ 2.5 ft) (ok assumption with "near" reposition step)
		//		6. the far "reachable" edge of the workspace is within 24 in. of the speaker (ok assumption)
		//		7. the top is at least 8 in deep (ok assumption)
		//		8. nothing that could be confused as the work space top (shelves etc.) exists in this space (ok assumption???)
		//		9. the work space is lower then the speaker's head (ok assumption)
		//		10. the sensor data is somewhat noisey so some filtering is required (ok assumption) [using averaging of delta height and mutli-detect of state transition]
		//		11. three possible arrangements (robot with speaker): same side "in line", other side "across" or edge (ok assumption) [current implementaion only covers the first]
		//		12. the top at the "work location col" is completely clear (dubious assumption!)
		//		The current implementation works at least partially because of the relationship of the robot's location and that of the speaker at the begining and the fact that only a "left" position is possible.
		//		Check this by sitting @ left side of work bench and giving "right" response
		//		The current implementaion assumes that the robot is behind the speaker, check this by sitting near wall in front of make shift bench


		private const double NESS_THRESHOLD = .015;
//		private const int MIN_TOP_LEN = 12;
		private const int MIN_TOP_LEN = 8;
		private const int START_TRANS_REQ = 2;
		private const int EDGE_SEPERATION = 9;
		private const int DIST_FROM_SPEAKER_CENTER = 18;



		private WorkAssist.CommandOccur co;
		private DepthImagePoint[] dips;



		private bool DetermineEdgeOrient(int row,ref double pa)

		{
			bool rtn = false;
			int start,end,i,count = 0,dist,col;
			double sx2 = 0, sxy = 0, sx = 0, sy = 0, b, see = 0, ye;
			double m, ra, x, y;
			double angle, rdist,nsee;

			col = (Kinect.nui.ColorStream.FrameWidth / 2) - 20;
			start = row * Kinect.nui.ColorStream.FrameWidth + col;
			end = start + 40;
			for (i = start;i < end;i++)
				{
				if (dips[i].Depth > 0)
					{
					count += 1;
					dist = dips[i].Depth;
					ra = Kinect.VideoHorDegrees(col - (Kinect.nui.ColorStream.FrameWidth / 2));
					rdist = dist / Math.Cos(ra * SharedData.DEG_TO_RAD);
					angle = 360 - ra;
					angle %= 360;
					x = (rdist * Math.Sin(angle * SharedData.DEG_TO_RAD));
					y = dist;
					sx += x;
					sx2 += x * x;
					sy += y;
					sxy += x * y;
					}
				col += 1;
				}
			if (count >= (end - start) * .75)
				{
				m = ((count * sxy) - (sx * sy)) / ((count * sx2) - Math.Pow(sx, 2));
				b = (sy / count) - ((m * sx) / count);
				col = (Kinect.nui.ColorStream.FrameWidth / 2) - 20;
				for (i = start; i < end; i++)
					{
					if (dips[i].Depth > 0)
						{
						dist = dips[i].Depth;
						ra = Kinect.VideoHorDegrees(col - (Kinect.nui.ColorStream.FrameWidth / 2));
						rdist = dist / Math.Cos(ra * SharedData.DEG_TO_RAD);
						angle = 360 - ra;
						angle %= 360;
						x = (rdist * Math.Sin(angle * SharedData.DEG_TO_RAD));
						y = dist;
						ye = (x * m) + b;
						see += Math.Pow(y - ye, 2);
						}
					col += 1;
					}
				see = Math.Sqrt(see / count);
				nsee = see / (sy /count);
				if (nsee < NESS_THRESHOLD)
					{
					rtn = true;
					pa = Math.Round(Math.Atan(m) * SharedData.RAD_TO_DEG);
					}
				else
					Log.LogEntry("DetermineEdgeOrient: error exceeded limit.");
				}
			else
				Log.LogEntry("DetermineEdgeOrient: insufficent data");
			return(rtn);
		}



		private bool AnalyzeData()

		{
			bool rtn = false;
			double angle, dist, ph, dh, dd, last_dist = -1, last_ph = 0, avg_last = 0, total = 0, dhthres,thres_factor,top_start = 0,direct;
			bool top_found = false, edge_found = false,edge_perp_found = false;
			int pan, i = 0, start, end,index = 0,count = 0, esrow = 0, efrow = 0,state_trans = 0,no_samples = 0;
			Arm.Loc3D l3d;
			NavData.location cl;
			Room.rm_location rl;
			string fname;
			DateTime now = DateTime.Now;
			TextWriter tw = null;
			PersonDetect.scan_data pd;

			try
			{
			thres_factor = (1 / 531.54) / 2;
			dips = SkillShared.pd.LastDepthImage();
			pd = SpeakerData.Person;
			start = (pd.vdo.y * Kinect.nui.ColorStream.FrameWidth) + Kinect.nui.ColorStream.FrameWidth / 2;
			index = pd.vdo.y;
			end = Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth;
			if (SharedData.log_operations)
				{
				fname = Log.LogDir() + "work location distance vs height " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".csv";
				tw = File.CreateText(fname);
				if (tw != null)
					{
					tw.WriteLine("Work location cartisian distance vs height");
					tw.WriteLine(now.ToShortDateString() + "  " + now.ToShortTimeString());
					tw.WriteLine();
					tw.WriteLine("Height (in),Distance (in)");
					}
				}
			SkillShared.al.Clear();
			for (i = start; i < end; i += Kinect.nui.ColorStream.FrameWidth)
				{
				if (dips[i].Depth > 0)
					{
					dist = Kinect.CorrectedDistance(dips[i].Depth * SharedData.MM_TO_IN);
					dhthres = dist * thres_factor;
					angle = Kinect.VideoVerDegrees((Kinect.nui.ColorStream.FrameHeight / 2) - index);
					l3d = Arm.MapKCToRC(0, dist,0,angle);
					ph = l3d.y;
					if (ph < pd.vdo.y)
						{
						no_samples += 1;
						if (tw != null)
							tw.WriteLine(ph.ToString("F2") + "," + dist.ToString("F2"));
						dd = dist - last_dist;
						dh = ph - last_ph;
						if (!top_found)
							{
							if ((no_samples > 1) && SkillShared.AvgLast(dh, ref avg_last))
								{
								if (Math.Abs(avg_last) < dhthres)
									{
									state_trans += 1;
									if (state_trans == START_TRANS_REQ)
										{
										top_found = true;
										total = ph;
										count = 1;
										state_trans = 0;
										top_start = dist;
										}
									}
								else
									state_trans = 0;
								}
							}
						else if (top_found && !edge_found)
							{
							if (SkillShared.AvgLast(dh, ref avg_last))
								{
								if (Math.Abs(avg_last) > dhthres)
									{
									state_trans += 1;
									if (state_trans == START_TRANS_REQ)
										{
										if (top_start - dist >= MIN_TOP_LEN)
											{
											edge_found = true;
											if (SkillShared.wsd.existing_area)
												{
												if (Math.Abs((total/count) - SkillShared.wsd.top_height) > AAShare.TOP_HI_LO_DIFF)
													Log.LogEntry("Possible top height descrepency found " + (total/count).ToString("F2") + " vs " + SkillShared.wsd.top_height.ToString("F2"));
												}
											else
												SkillShared.wsd.top_height = total/count;
											total = dist;
											count = 1;
											esrow = index;
											}
										else
											{
											top_found = false;
											state_trans = 0;
											}
										}
									}
								else if (state_trans == 0)
									{
									total += ph;
									count += 1;
									}
								else
									state_trans = 0;
								}
							}
						else if (top_found && edge_found)
							{
							if (Math.Abs(dh) > (3 * dhthres))
								{
								SkillShared.wsd.front_edge_dist = total/count;
								efrow = index;
								break;
								}
							else
								{
								total += dist;
								count += 1;
								}
							}
						}
					last_dist = dist;
					last_ph = ph;
					}
				index += 1;
				}
			if ((SkillShared.wsd.front_edge_dist == 0) && edge_found)
				{
				SkillShared.wsd.front_edge_dist = total/count;
				efrow = esrow + count;
				}
			if (edge_found)
				{
				double pa = 0;
				PersonDetect.scan_data fd;

				cl = NavData.GetCurrentLocation();
				pan = HeadAssembly.PanAngle();
				fd = SpeakerData.Face;
				if (DetermineEdgeOrient((esrow + efrow) / 2, ref pa))
					{
					SkillShared.wsd.edge_perp_direct = (int) Math.Round((cl.orientation + pan - pa));
					if (SkillShared.wsd.edge_perp_direct < 0)
						SkillShared.wsd.edge_perp_direct += 360;
					else if (SkillShared.wsd.edge_perp_direct > 360)
						SkillShared.wsd.edge_perp_direct -= 360;
					SkillShared.wsd.work_loc.orient = SkillShared.wsd.edge_perp_direct;
					if (fd.detected)
						{
						Point pt1,pt2,pt3,pt4,pt;
						int rangle;
						bool done = false;

						rl = NavCompute.PtDistDirectApprox(cl.coord,cl.orientation + pan, (int)Math.Round(SkillShared.wsd.front_edge_dist));
						pt3 = rl.coord;
						angle = (cl.orientation + pan - fd.angle) % 360;
						if (angle < 0)
							angle += 360;
						rl = NavCompute.PtDistDirectApprox(cl.coord, (int)Math.Round(angle), (int)Math.Round(fd.dist));
						Log.LogEntry("Face location: " + rl.coord + " (room coord)");
						if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
							direct = SkillShared.wsd.edge_perp_direct - 90;
						else 
							direct = (SkillShared.wsd.edge_perp_direct + 90) % 360;
						if (direct < 0)
							direct += 360;
						angle = direct;
						do
							{
							if ((angle >= 315) || (angle <= 45))
								done = true;
							else
								angle = (angle + 90) % 360;
							}
						while(!done);
						rangle = NavCompute.AngularDistance((int) Math.Round(angle),0);
						if (!NavCompute.ToRightDirect(rangle,0))
							rangle *= -1;
						Log.LogEntry("Rotate angle: " + rangle);
						pt1 = SkillShared.RotatePoint(rl.coord,rangle);
						Log.LogEntry("pt1: " + rl.coord + " R " + pt1);
						rl = NavCompute.PtDistDirectApprox(rl.coord,(int) Math.Round(direct), DIST_FROM_SPEAKER_CENTER);
						pt2 = SkillShared.RotatePoint(rl.coord,rangle);
						Log.LogEntry("pt2: " + rl.coord + " R " + pt2);
						pt = pt3;
						pt3 = SkillShared.RotatePoint(pt3,rangle);
						Log.LogEntry("pt3: " + pt + " R " + pt3);
						pt4 = new Point(pt3.X,pt2.Y);
						SkillShared.wsd.center_workspace_edge = SkillShared.RotatePoint(pt4,-rangle);
						Log.LogEntry("pt4: " + pt4 + " R " + SkillShared.wsd.center_workspace_edge);
						rl = NavCompute.PtDistDirectApprox(SkillShared.wsd.center_workspace_edge,(SkillShared.wsd.edge_perp_direct + 180) % 360,EDGE_SEPERATION);
						SkillShared.wsd.work_loc = new MotionMeasureProb.Pose(rl.coord, SkillShared.wsd.edge_perp_direct);
						}
					else
						{
						rl = NavCompute.PtDistDirectApprox(cl.coord,cl.orientation + pan, (int)Math.Round(SkillShared.wsd.front_edge_dist));
						SkillShared.wsd.center_workspace_edge = rl.coord;
						rl = NavCompute.PtDistDirectApprox(rl.coord,(SkillShared.wsd.edge_perp_direct + 180) % 360,EDGE_SEPERATION);
						SkillShared.wsd.work_loc = new MotionMeasureProb.Pose(rl.coord, SkillShared.wsd.edge_perp_direct);
						}
					if (tw != null)
						{
						tw.WriteLine();
						tw.WriteLine("Work space data: " + SkillShared.wsd.top_height.ToString("F2") + "(in)  " + SkillShared.wsd.front_edge_dist.ToString("F2") + "(in)  " + SkillShared.wsd.edge_perp_direct + "°  " + SkillShared.wsd.center_workspace_edge + "  " + SkillShared.wsd.work_loc);
						}
					edge_perp_found = true;
					rtn = true;
					}
				}
			}

			catch(Exception ex)
			{
			Log.LogEntry("AnalyzeData exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			Log.LogEntry("Index: " + i);
			}

			if (!top_found)
				Log.LogEntry("AnalyzeData: top not found.");
			else if (!edge_found)
				Log.LogEntry("AnalyzeData: edge not found.");
			else if (!edge_perp_found)
				Log.LogEntry("AnalyzeData: edge perp direction not found");
			if (tw != null)
				{
				tw.WriteLine();
				if (!top_found)
					tw.WriteLine("Top not found");
				else if (!edge_found)
					tw.WriteLine("Edge not found");
				else if (!edge_perp_found)
					tw.WriteLine("Edge perp direction not found");
				tw.Close();
				}
			return (rtn);
		}



		public bool CollectWorkspaceInfo()

		{
			bool rtn = false;
			NavData.location cl;
			NavCompute.pt_to_pt_data ppd;
			double angle = 0;
			int pan,x;
			PersonDetect.scan_data pdd = PersonDetect.Empty(),fd = PersonDetect.Empty();
			ArrayList al = new ArrayList();
			Point pt;
			Graphics g;

			cl = NavData.GetCurrentLocation();
			SkillShared.wsd.initial_robot_loc = cl;
			SkillShared.OutputSpeech("Collecting initial work space information for " + SkillShared.wsd.room + SkillShared.wsd.name);
			co = new WorkAssist.CommandOccur(false);
			ppd = NavCompute.DetermineRaDirectDistPtToPt(SpeakerData.Person.rm_location, cl.coord);
			pan = NavCompute.AngularDistance(cl.orientation, ppd.direc);
			if (!NavCompute.ToRightDirect(cl.orientation, ppd.direc))
				pan *= -1;
			HeadAssembly.Pan(pan, true);
			if (SkillShared.FindSpeaker(pan,ref pdd))
				{
				SpeakerData.Person = pdd;
				if (Rplidar.CaptureScan(ref al,true))
					{
					Rplidar.SaveLidarScan(ref al, "Work assist initial LIDAR scan");
					if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
						{
						x = pdd.vdo.x - (pdd.vdo.width / 2);
						angle = Kinect.VideoHorDegrees(x - Kinect.nui.ColorStream.FrameWidth / 2);
						angle = pan + angle;
						SkillShared.wsd.side = SharedData.RobotLocation.LEFT;
						}
					else
						{
						}
					HeadAssembly.Pan((int) Math.Round(angle),true);
					if (SkillShared.FindSpeakerFace((int)Math.Round(angle),ref pdd,ref fd))
						{
						SpeakerData.Person = pdd;
						SpeakerData.Face = fd;
						if (AnalyzeData())
							{
							Log.LogEntry("Work space data: " + SkillShared.wsd.top_height.ToString("F2") + "(in)," + SkillShared.wsd.front_edge_dist.ToString("F2") + "(in)," + SkillShared.wsd.edge_perp_direct + "°," + SkillShared.wsd.center_workspace_edge + "," + SkillShared.wsd.work_loc);
							SkillShared.ws_map = SkillShared.ScanMap(al,cl);
							if (SharedData.log_operations)
								{
								WorkAssist.bmap = SkillShared.MapToBitmap(SkillShared.ws_map);

								try 
								{
								g = Graphics.FromImage(WorkAssist.bmap);
								pt = new Point(SpeakerData.Face.rm_location.X,SpeakerData.Face.rm_location.Y);
								g.FillEllipse(Brushes.HotPink, pt.X - 2, pt.Y - 2, 4, 4);	//speaker
								SkillShared.DisplayWorkSpace(SkillShared.wsd.center_workspace_edge,Pens.Blue,g, SkillShared.wsd.edge_perp_direct);	//work space
								pt = SkillShared.wsd.center_workspace_edge;
								g.FillEllipse(Brushes.Red, pt.X - 1, pt.Y - 1,2,2);	//front edge center
								pt = SkillShared.wsd.work_loc.coord;
								g.FillEllipse(Brushes.Yellow, pt.X - 2, pt.Y - 2,4,4);	//work location
								g.FillEllipse(Brushes.Brown,cl.coord.X - 2,cl.coord.Y - 2,4,4);	//robot
								}

								catch(Exception ex)
								{
								Log.LogEntry("Mapping exception: " + ex.Message);
								Log.LogEntry("Stack trace: " + ex.StackTrace);
								}

								}
							SkillShared.wsd.tight_quarters = SpeakerData.TightQuaters;
							rtn = true;
							Speech.SpeakAsync("Initial information collection complete.");
							}
						else
							Speech.SpeakAsync("Could not determine work space parameters.");
						}
					else
						Speech.SpeakAsync("Could not locate speaker.");
					}
				else
					SkillShared.OutputSpeech("Could not capature a LIDAR scan");
				}
			else
				{
				SkillShared.OutputSpeech("Could not find the speaker.");
				SpeakerData.ClearPersonFace();
				}
			HeadAssembly.Pan(0,true);
			return(rtn);
		}

		
		}
	}
