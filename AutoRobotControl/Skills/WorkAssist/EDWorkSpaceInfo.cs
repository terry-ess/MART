using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AutoRobotControl;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;


namespace Work_Assist
	{
	class EDWorkSpaceInfo : WorkSpaceInfoInterface
		{

		//	assumptions:
		//		1. Kinect is ~ 31 in. above the top [Kinect is ~ 61 in. above floor and top is ~ 30 in. above floor]
		//		2. The person's center point is ~ 12 in. from the work area top
		//		3. Work area orientation is either parallel or perpendicular to a "prime" room direction [0,90,180,270]


		public const int MIN_TOP_LEN = 8;
		public const int EDGE_OFFSET = 4;
		public const int APPROACH_LOC_OFFSET = 20;


		private bool CalcApproachPt(int direct,double edist,double sdist,ref Point apt)

		{
			bool rtn = false;
			double dx,dy,angle;
			NavData.location cl;
			NavCompute.pt_to_pt_data ppd;

			cl = NavData.GetCurrentLocation();
			dx = edist * Math.Sin(direct * SharedData.DEG_TO_RAD);
			dy = -edist * Math.Cos(direct * SharedData.DEG_TO_RAD); //- because of Y flip
			if (SkillShared.wsd.prime_direct == 270)		//this should be replaced with geometery-matrix algebra logic that uses the prime direction
				{
				dx += APPROACH_LOC_OFFSET;
				angle = NavCompute.AngularDistance(270,direct);
				dy += EDGE_OFFSET - (sdist * Math.Cos(angle * SharedData.DEG_TO_RAD));
				rtn = true;
				}
			else
				Log.LogEntry("Approach point calculation not implmented for prime direction " + SkillShared.wsd.prime_direct);
			if (rtn)
				{
				apt.X = cl.coord.X + (int) Math.Round(dx);
				apt.Y = cl.coord.Y + (int) Math.Round(dy);
				Log.LogEntry("Intial approach location: " + apt);
				ppd = NavCompute.DetermineRaDirectDistPtToPt(apt, cl.coord);
				angle = NavCompute.AngularDistance(ppd.direc, SkillShared.wsd.prime_direct);
				dx = SharedData.FRONT_PIVOT_PT_OFFSET * Math.Sin(angle * SharedData.DEG_TO_RAD);
				if (NavCompute.ToRightDirect(ppd.direc, SkillShared.wsd.prime_direct))
					dx *= -1;
				apt = NavCompute.MapPoint(new Point((int)Math.Round(dx), 0), SkillShared.wsd.prime_direct, apt);
				Log.LogEntry("Corrected approach location: " + apt);
				}
			return (rtn);
		}




		private bool DataAnalysis(int direct,int pdist) //How to determine if move to edge is under tight quarters? Speaker being tight quarters does not determine.
																		//This implementation assumes that it is NOT tight quarters and that the move is a single direct move
		{																//see WorkAreaData assumptions
			bool rtn = false;
			NavData.location cl;
			Point pt = new Point();
			rtn = SkillShared.FindEdges(direct,pdist);
			if (rtn)
				{
				rtn = false;
				cl = NavData.GetCurrentLocation();
				if (CalcApproachPt(direct,SkillShared.wsd.front_edge_dist,SkillShared.wsd.side_edge_dist, ref pt))
					{
					if (Navigate.PathClear(cl.coord, pt))
						{
						SkillShared.wsd.work_loc = new MotionMeasureProb.Pose(pt, SkillShared.wsd.prime_direct);
						SkillShared.wsd.tight_quarters = false;
						rtn = true;
						}
					else
						Log.LogEntry("Path to work location is not clear.");
					}
				}
			return (rtn);
		}



		public bool CollectWorkspaceInfo()	

		{
			bool rtn = false,pan_determine = false;
			NavData.location cl;
			int pangle = 0,tangle,direct;
			Point pt;
			NavCompute.pt_to_pt_data ppd;
			double dist;

			SkillShared.OutputSpeech("Collecting initial work space information for " + SkillShared.wsd.room + SkillShared.wsd.name);
			cl = NavData.GetCurrentLocation();
			if ((SkillShared.wsd.prime_direct == 270) && SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
				{
				pt = NavCompute.MapPoint(new Point(0,24),180,SpeakerData.Face.rm_location);
				ppd = NavCompute.DetermineRaDirectDistPtToPt(pt,cl.coord);
				pangle = NavCompute.AngularDistance(cl.orientation,ppd.direc);
				if (!NavCompute.ToRightDirect(cl.orientation,ppd.direc))
					pangle *= -1;
				pan_determine = true;
				Log.LogEntry("Pan: " + pangle);
				}
			else
				SkillShared.OutputSpeech("Determination of pan angle not implemented for prime direct " + SkillShared.wsd.prime_direct + " and side of " + SkillShared.wsd.side);
			if (pan_determine && (Math.Abs(pangle) < HeadAssembly.MAX_HA_ANGLE/2))
				{
				if (SpeakerData.Face.detected)
					dist = SpeakerData.Face.dist;
				else
					dist = SpeakerData.Person.dist;
				tangle = ((int) Math.Round(Math.Atan((dist - 12) / 31) * SharedData.RAD_TO_DEG)) - 90;	//using constant for height assuming Kinect ~ 61 in and work space top ~ 30 in
				Log.LogEntry("Tilt: " + tangle);																			//probably better if use height at top of face (or person) - approx height above top of 26 in 
				HeadAssembly.Pan(pangle, true);																			//for work space top
				HeadAssembly.Tilt(tangle,true);
				if (SkillShared.RecordWorkSpaceData("Edge work space info ",false))
					{
					direct = cl.orientation + pangle;
					int pdist = (int) Math.Round(dist / Math.Abs(Math.Cos(pangle * SharedData.DEG_TO_RAD))) + MIN_TOP_LEN;
					if (DataAnalysis(direct,pdist))
						SkillShared.wsd.LogWSD();
					}
				HeadAssembly.Tilt(0, true);
				HeadAssembly.Pan(0,true);
				rtn = true;
				}
			else if (pan_determine)
				Log.LogEntry("Work space pan angle of " + pangle + " degrees exceeds limits.");
			return(rtn);
		}

		}
	}
