using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using AutoRobotControl;


namespace Work_Assist
	{
	class EDMoveToWorkSpace : MoveToWorkSpaceInterface
		{

		private const int EDGE_STANDOFF_DIST = 11;

		private bool run_monitor = false;
		private int min_monitor_dist,max_monitor_dist;
		private Room rm = SharedData.current_rm;



		private double DetermineTopEdgeDistance(double tilt_correct,int max_dist,bool log = true)

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
					if (loc.z  < max_dist)
						{
						if (loc.y < SkillShared.wsd.top_height - SkillShared.TOP_MAGRIN)
							break;
						else if (loc.y < SkillShared.wsd.top_height + SkillShared.TOP_MAGRIN)
							{
							if ((loc.z > 0) && (loc.z < min_dist))
								min_dist = loc.z;
							}
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



		private void MoveMonitor()

		{
			Stopwatch sw = new Stopwatch();
			double edist;
			long em,total = 0,count = 0;

			try
			{
			Thread.Sleep(100);
			while(run_monitor)
				{
				sw.Restart();
				if (Kinect.GetDepthFrame(ref SkillShared.depthdata, 40))
					{
					Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, SkillShared.depthdata, SkillShared.dips);
					edist = DetermineTopEdgeDistance(SkillShared.MOVE_KINECT_TILT_CORRECT,max_monitor_dist,false);
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
				count += 1;
				total += em;
				if ((em > 100) && run_monitor)
					Thread.Sleep(100 - (int) em);
				}
			}

			catch(Exception)
			{
			run_monitor = false;
			}

			Log.LogEntry(count + " samples averaged procressing time of " + (total/count) + " ms ");
		}



		private bool TurnToDirection(int current_dir,int desired_dir)

		{
			bool rtn = false,turn_safe;
			int tangle = 0;
			NavData.location cl;


			tangle = current_dir - desired_dir;
			if (tangle > 180)
				tangle -= 360;
			else if (tangle < -180)
				tangle += 360;
			if (!(turn_safe = Turn.TurnSafeMulti(tangle)))
				{
				if (Math.Abs(tangle) > 135)
					{
					if (tangle < 0)
						tangle += 360;
					else
						tangle -= 360;
					turn_safe = Turn.TurnSafeMulti(tangle);
					}
				}
			if (turn_safe)
				{
				rtn = SkillShared.TurnAngle(tangle);
				if (rtn)
					{
					cl = NavData.GetCurrentLocation();
					cl.orientation = desired_dir;
					cl.coord = NavCompute.MapPoint(Turn.TurnImpactPosition(tangle), desired_dir, cl.coord);
					cl.ls = NavData.LocationStatus.DR;
					NavData.SetCurrentLocation(cl);
					MotionMeasureProb.Move(new MotionMeasureProb.Pose(cl.coord, cl.orientation));
					}
				else
					{
					SharedData.med.mt = SharedData.MoveType.SPIN;
					SharedData.med.et = Turn.LastError();
					SharedData.med.ob_descript = null;
					}
				}
			return (rtn);
		}



		public bool Move(ref ArrayList path, ref Stack rtn_path, ref Stack final_adjust)

		{
			bool rtn = false;
			NavData.location cl,eloc;
			Thread monitor = new Thread(MoveMonitor);
			string rsp;
			int dist,angle = 0;
			Point pt;
			PersonDetect.scan_data pdd = PersonDetect.Empty(), fd = PersonDetect.Empty();
			NavCompute.pt_to_pt_data ppd;
			int pan,tilt;


			SkillShared.OutputSpeech("Starting move to work location.");
			if (SkillShared.wsd.tight_quarters)
				{
				SkillShared.OutputSpeech("Tight quarters move not implemented.");
				}
			else if (SkillShared.DirectMove(SkillShared.wsd.work_loc.coord))
				{
				cl = NavData.GetCurrentLocation();
				TurnToDirection(cl.orientation,SkillShared.wsd.prime_direct);
				cl = NavData.GetCurrentLocation();
				ppd = NavCompute.DetermineRaDirectDistPtToPt(SpeakerData.Person.rm_location, cl.coord);
				pan = NavCompute.AngularDistance(cl.orientation, ppd.direc);
				if (!NavCompute.ToRightDirect(cl.orientation, ppd.direc))
					pan *= -1;
				HeadAssembly.Pan(pan, true);
				if (SpeakerData.Person.vdo.y > Kinect.nui.ColorStream.FrameHeight / 2)
					{
					tilt = HeadAssembly.TiltAngle();
					angle = (int) Math.Round(Math.Abs(Kinect.VideoVerDegrees((Kinect.nui.ColorStream.FrameHeight / 2) - (SpeakerData.Person.vdo.y + SpeakerData.Person.vdo.height / 4))));
					if (angle > 1)
						{
						Log.LogEntry("Tilt: " + -angle);
						HeadAssembly.Tilt(-angle, true);
						}
					}
				if (SkillShared.FindSpeakerFace(0, ref pdd, ref fd))
					{
					SpeakerData.Person = pdd;
					SpeakerData.Face = fd;
					}
				if (SpeakerData.Face.detected)
					dist = NavCompute.DistancePtToPt(cl.coord,SpeakerData.Face.rm_location);
				else
					dist = NavCompute.DistancePtToPt(cl.coord,SpeakerData.Person.rm_location);
				HeadAssembly.Pan(0, true);
				HeadAssembly.Tilt(SkillShared.MOVE_KINECT_TILT, true);
				SkillShared.RecordWorkSpaceData("Edge work space info at approach point", false);
				if (SkillShared.FindEdges(0,dist + EDWorkSpaceInfo.MIN_TOP_LEN) && (SkillShared.wsd.front_edge_dist > 0) & (SkillShared.wsd.front_edge_dist < EDWorkSpaceInfo.APPROACH_LOC_OFFSET * 1.1) )
					{
					SkillShared.wsd.LogWSD();
					if (Math.Abs(SkillShared.wsd.side_edge_dist - EDWorkSpaceInfo.EDGE_OFFSET) > .75)
						{
						angle = (int) Math.Round(Math.Atan(((EDWorkSpaceInfo.EDGE_OFFSET - SkillShared.wsd.side_edge_dist)/SkillShared.wsd.front_edge_dist)) * SharedData.RAD_TO_DEG);
						Log.LogEntry("Edge final adjust angle: " + angle);
						if (Math.Abs(angle) >  0)
							SkillShared.TurnAngle(angle);
						}
					else
						angle = 0;
					dist = (int) Math.Ceiling(SkillShared.wsd.front_edge_dist - (SharedData.ARM_PERCH_OFFSET + 1));
					min_monitor_dist = EDGE_STANDOFF_DIST;
					max_monitor_dist = (int) Math.Round(SkillShared.wsd.front_edge_dist + 5);
					run_monitor = true;
					monitor.Start();
					rsp = SkillShared.SendCommand(SharedData.FORWARD_SLOW_NCC + " " + dist, 8000);
					run_monitor = false;
					monitor.Join();
					if (Math.Abs(angle) > 0)
						SkillShared.TurnAngle(-angle);
					if (rsp.StartsWith("ok")) 
						{
						MotionMeasureProb.Pose mmpp;

						rsp = SkillShared.SendCommand(SharedData.DIST_MOVED, 200);
						if (rsp.StartsWith("ok"))
							dist = int.Parse(rsp.Substring(3));
						final_adjust.Push(0);
						final_adjust.Push(dist);
						pt = new Point(0, dist);
						eloc = cl;
						eloc.coord = NavCompute.MapPoint(pt, cl.orientation, cl.coord);
						eloc.ls = NavData.LocationStatus.DR;
						eloc.loc_name = "";
						Log.LogEntry("Expected location: " + eloc.ToString());
						mmpp = new MotionMeasureProb.Pose(eloc.coord, eloc.orientation);
						MotionMeasureProb.Move(mmpp);
						SkillShared.wsd.work_loc = mmpp;
						dist = NavCompute.DistancePtToPt(eloc.coord, SpeakerData.Person.rm_location);
						SkillShared.RecordWorkSpaceData("Edge work space info at work location", false);
						if (SkillShared.FindEdges(0, dist + EDWorkSpaceInfo.MIN_TOP_LEN))
							{
							SkillShared.wsd.LogWSD();
							rtn = true;
							}
						else
							SkillShared.OutputSpeech("Could not determine location of edges.");
						}
					else
						SkillShared.OutputSpeech("Final move to work location failed.");
					}
				}
			else
				SkillShared.OutputSpeech("Can not move to work location.");
			HeadAssembly.Tilt(0,true);
			HeadAssembly.Pan(0,true);
			return (rtn);
		}


		}
	}
