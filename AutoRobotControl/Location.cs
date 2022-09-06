using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Windows.Forms;


namespace AutoRobotControl
	{

	public class Location
		{

		public enum LocationDeterminationStatus {NO_MATCH,GOOD_MATCH,FAILED};

		private const int USR_MAX_DIST_DIF = 18;
		private const int MAX_OPEN_WALL_DIST_DIF = 14;
		private const int USR_MAX_OPEN_WALL_DIST_DIF = 21;
		private const int VERIFIED_COORD_DIF = 2;
		private const int VERIFIED_ORIENT_DIF = 2;
		private const int DR_COORD_DIF = 7;
		private const int DR_ORIENT_DIF = 2;

		public struct feature_match_plus
			{
			public Room.feature_match fm;
			public LocDecisionEngine.action actn;
			public Point coord;
			};


		private LocationDeterminationStatus stat = LocationDeterminationStatus.FAILED;
		private SensorFusion sf = new SensorFusion();
		public ArrayList sdata = new ArrayList();
		private LocDecisionEngine lde = new LocDecisionEngine();
		private ArrayList successful_actions = new ArrayList();
		private ArrayList actions;


		private Room.feature_match FindFeature(NavData.feature f,Point est_coord,bool perp_backwall,bool corner_right,int edist,int era)

		{
			Room.feature_match rfm = new Room.feature_match();
			FeatureMatch fm = null;

			rfm.matched = false;
			if (f.type == NavData.FeatureType.CORNER)
				{
				fm = new Corner();
				Log.LogEntry("FindFeature: checking corner @ " + f.coord + " with params - " + perp_backwall + "," + corner_right);
				}
			else if (f.type == NavData.FeatureType.OPENING_EDGE)
				{
				fm = new Door();
				Log.LogEntry("FindFeature: checking opening edge @ " + f.coord + " with param - " + est_coord);
				}
			else if (f.type == NavData.FeatureType.TARGET)
				{
				fm = new Target();
				Log.LogEntry("FindFeature: checking target @ " + f.coord);
				}
			else
				Log.LogEntry("FindFeature: unknown feature type " + f.type.ToString());
			if (fm != null)
				{
				if (f.type == NavData.FeatureType.CORNER)
					rfm = fm.MatchKinect(f,perp_backwall,corner_right);
				else if (f.type == NavData.FeatureType.OPENING_EDGE)
					rfm = fm.MatchKinect(f,NavData.rd,est_coord);
				else if (f.type == NavData.FeatureType.TARGET)
					if ((edist != 0) && (era != 0))
						rfm = fm.MatchKinect(f,edist,era);
					else
						rfm = fm.MatchKinect(f);
				else
					rfm = fm.MatchKinect(f);
				if (rfm.matched)
					{
					if (f.type == NavData.FeatureType.CORNER)
						rfm.distance = (int) (Kinect.CorrectedDistance(rfm.distance)/Math.Cos(rfm.ra * SharedData.DEG_TO_RAD));
					rfm.head_angle = (int) Math.Round(HeadAssembly.CurrentHeadAngle() - rfm.ra);
					Log.LogEntry("FindFeature data: distance - " + rfm.distance + "  ra - " + rfm.ra + "  head angle - " + rfm.head_angle);
					}
				}
			return(rfm);
		}



		private Room.feature_match FindFeature(NavData.feature f,Point est_coord,bool perp_backwall,bool corner_right)

		{
			return(FindFeature(f,est_coord,perp_backwall,corner_right,0,0));
		}



		private int DeterminePreferedDirection()

		{
			int mheading,direc;

			mheading = (int) HeadAssembly.GetMagneticHeading();
			direc = NavCompute.DetermineDirection(mheading);
			if ((direc >= 45) && (direc < 135))
				direc = 90;
			else if ((direc >= 135) && (direc < 225))
				direc = 180;
			else if ((direc >= 225) && (direc < 315))
				direc = 270;
			else
				direc = 0;
			return(direc);
		}



		private int[] DetermineFeaturePerpDirections(NavData.feature f)

		{
			int[] directions = {-1,-1};
			int i;
			NavData.connection connect;

			if (f.type == NavData.FeatureType.CORNER)	// the corner logic only works for pure rectangle layout
				{
				if ((f.coord.X == 0) && (f.coord.Y == 0))
					{
					directions[0] = 0;
					directions[1] = 270;
					}
				else if ((f.coord.X == 0) && (f.coord.Y > 0))
					{
					directions[0] = 270;
					directions[1] = 180;
					}
				else if ((f.coord.X > 0) && (f.coord.Y == 0))
					{
					directions[0] = 0;
					directions[1] = 90;
					}
				else
					{
					directions[0] = 90;
					directions[1] = 180;
					}
				}
			else if (f.type == NavData.FeatureType.OPENING_EDGE)
				{
				for (i = 0; i < NavData.rd.connections.Count; i++)
					{
					connect = (NavData.connection)NavData.rd.connections[i];
					if ((connect.hc_edge.ef.coord == f.coord)  || (connect.lc_edge.ef.coord == f.coord))
						{
						directions[0] = NavCompute.DetermineDirection(NavData.heading_table[connect.direction]);
						}
					}
				}
			else if (f.type == NavData.FeatureType.TARGET)
				{
				if (f.coord.Y == 0)
					{
					directions[0] = 0;
					}
				else if (f.coord.X == 0)
					{
					directions[0] = 270;
					}
				else if (f.coord.Y == NavData.rd.rect.Height)
					{
					directions[0] = 180;
					}
				else
					{
					directions[0] = 90;
					}
				}
			return(directions);
		}



		private Room.TurnLimits FeatureTurnLimits(NavData.feature f,int[] fdirec,int direc)

		{
			Room.TurnLimits tl = Room.TurnLimits.NONE;

			if (f.type == NavData.FeatureType.CORNER)
				{
				if (((fdirec[0] == 0) && (fdirec[1] == 90)) || ((fdirec[0] == 90) && (fdirec[1] == 0)))
					{
					if (direc == 90)
						tl = Room.TurnLimits.MINUS_ONLY;
					else
						tl = Room.TurnLimits.PLUS_ONLY;
					}
				else if (((fdirec[0] == 0) && (fdirec[1] == 270)) || ((fdirec[0] == 270) && (fdirec[1] == 0)))
					{
					if (direc == 0)
						tl = Room.TurnLimits.MINUS_ONLY;
					else
						tl = Room.TurnLimits.PLUS_ONLY;
					}
				else if (((fdirec[0] == 180) && (fdirec[1] == 90)) || ((fdirec[0] == 90) && (fdirec[1] == 180)))
					{
					if (direc == 180)
						tl = Room.TurnLimits.MINUS_ONLY;
					else
						tl = Room.TurnLimits.PLUS_ONLY;
					}
				else if (((fdirec[0] == 180) && (fdirec[1] == 270)) || ((fdirec[0] == 270) && (fdirec[1] == 180)))
					{
					if (direc == 270)
						tl = Room.TurnLimits.MINUS_ONLY;
					else
						tl = Room.TurnLimits.PLUS_ONLY;
					}
				}
			return(tl);
		}



		private bool ReasonableMatch(int bha, int ra,Room.target t)

		{
			bool rtn = true;
			int la,aa;

			if (t.plus && t.minus)
				rtn = true;
			else if (t.minus)
				{
				la = bha - 90;
				aa = HeadAssembly.CurrentHeadAngle() - ra;
				if ((aa > bha) || (aa < la))
					rtn = false;
				}
			else if (t.plus)
				{
				la = bha + 90;
				aa = HeadAssembly.CurrentHeadAngle() - ra;
				if ((aa > la) || (aa < bha))
					rtn = false;
				}
			Log.LogEntry("ReasonableMatch: " + rtn);
			return(rtn);
		}


		private Room.feature_match FindFeatureWSearch(ref ArrayList ft,int search_angle,Point est_coord)

		{
			NavData.feature f;
			Room.feature_match fm = new Room.feature_match();
			Room.target t;
			int base_angle;

			fm.matched = false;
			base_angle = HeadAssembly.CurrentHeadAngle();
			while ((ft.Count > 0) && !fm.matched)
				{
				t = ((Room.target)ft[0]);
				f = (NavData.feature) NavData.rd.features[t.findex];
				if (t.forward)
					{
					if (t.plus)
						fm = FindFeature(f,est_coord,true,true);
					else
						fm = FindFeature(f,est_coord,true,false);
					t.forward = false;
					}
				if (fm.matched)
					{
					fm.matched = ReasonableMatch(base_angle,(int) fm.ra,t);
					if (fm.matched)
						fm.index = t.findex;
					}
				else
					{
					int sangle;

					if (t.plus)
						{
						sangle = base_angle + search_angle;
						if (sangle > HeadAssembly.MAX_HA_ANGLE)
							sangle = HeadAssembly.MAX_HA_ANGLE;
						HeadAssembly.SendHeadAngle(sangle, true);
						fm = FindFeature(f,est_coord,false,false);
						t.plus = false;
						if (fm.matched)
							{
							fm.matched = ReasonableMatch(base_angle,(int)fm.ra, t);
							if (fm.matched)
								fm.index = t.findex;
							}
						HeadAssembly.SendHeadAngle(base_angle, true);
						}
					if (!fm.matched && t.minus)
						{
						sangle = base_angle - search_angle;
						if (sangle < 0)
							sangle = 0;
						HeadAssembly.SendHeadAngle(sangle, true);
						fm = FindFeature(f,est_coord,false,false);
						t.minus = false;
						if (fm.matched)
							{
							fm.matched = ReasonableMatch(base_angle,(int)fm.ra, t);
							if (fm.matched)
								fm.index = t.findex;
							}
						HeadAssembly.SendHeadAngle(base_angle, true);
						}
					}
				if (!t.forward && !t.plus && !t.minus)
					ft.RemoveAt(0);
				else
					ft[0] = t;
				}
			return (fm);
		}



		private Room.feature_match FindSecondFeature(int findex, Point est_coord, int direct)

		{
			NavData.feature f;
			int i, direc = 0,ba;
			int[] fdirec;
			NavCompute.pt_to_pt_data ppd;
			Room.feature_match fm = new Room.feature_match();

			fm.matched = false;
			ba = HeadAssembly.CurrentHeadAngle();
			for (i = 0; i < NavData.rd.features.Count; i++)
				{
				if (i != findex)
					{
					f = (NavData.feature) NavData.rd.features[i];
					fdirec = DetermineFeaturePerpDirections(f);
					if ((direct == fdirec[0]) || (direct == fdirec[1]))
						{
						ppd = NavCompute.DetermineRaDirectDistPtToPt(f.coord,est_coord);
						direc = (ba + ppd.direc - direct) % 360;
						if (HeadAssembly.SendHeadAngle(direc, true).StartsWith("ok"))
							{
							fm = FindFeature(f,est_coord,false,false);
							if (fm.matched)
								{
								fm.index = i;
								break;
								}
							}
						}
					}
				}
			HeadAssembly.SendHeadAngle(ba, true);
			return (fm);
		}



		private bool CorrectLocationEstimate(ref NavData.location rl, ref ArrayList bps)

		{
			bool rtn = false;
			ArrayList al = null;

#if DEBUG
			int i;
			Room.bad_pt bp;
			string stg;

			stg = "CorrectLocationEstimate bps data:";
			for (i = 0; i <bps.Count;i++)
				{
				bp = (Room.bad_pt)bps[i];
				stg += " {" + bp.index + "," + bp.dist + "}";
				}
			Log.LogEntry(stg);
#endif
			if (bps.Count > 3)
				Log.LogEntry("CorrectLocationEstimate: Can not determine location.  Estimates in blocked area.");
			else if (bps.Count  == 1)
				{
				Room.bad_pt bp1;

				bp1 = (Room.bad_pt)bps[0];
				bp1.dist += 1;
				if ((bp1.index == 2) || bp1.index == 4)
					{
					rl.coord = NavCompute.MapPoint(new Point(-bp1.dist, 0), rl.orientation, rl.coord);
					Log.LogEntry("CorrectionLocationEstimate: corrected location " + rl.coord);
					}
				else if ((bp1.index == 1) || (bp1.index == 3))
					{
					rl.coord = NavCompute.MapPoint(new Point(bp1.dist, 0), rl.orientation, rl.coord);
					Log.LogEntry("CorrectionLocationEstimate: corrected location " + rl.coord);
					}
				}
			else if (bps.Count == 2)
				{
				Room.bad_pt bp1, bp2;
				int dist;

				bp1 = (Room.bad_pt)bps[0];
				bp2 = (Room.bad_pt)bps[1];
				if ((bp1.dist != -1) && (bp2.dist != -1))
					{
					bp1.dist += 1;
					bp2.dist += 1;
					dist = Math.Max(bp1.dist,bp2.dist);
					if (((bp1.index == 2) && (bp2.index == 4)) || ((bp1.index == 4) && (bp2.index == 2)))
						{
						rl.coord = NavCompute.MapPoint(new Point(-dist,0), rl.orientation,rl.coord);
						Log.LogEntry("CorrectionLocationEstimate: corrected location " + rl.coord);
						}
					else if (((bp1.index == 1) && (bp2.index == 3)) || ((bp1.index == 3) && (bp2.index == 1)))
						{
						rl.coord = NavCompute.MapPoint(new Point(dist, 0), rl.orientation, rl.coord);
						Log.LogEntry("CorrectionLocationEstimate: corrected location " + rl.coord);
						}
					else if (((bp1.index == 3) && (bp2.index == 4)) || ((bp1.index == 4) && (bp2.index == 3)))
						{
						rl.coord = NavCompute.MapPoint(new Point(0,dist), rl.orientation, rl.coord);
						Log.LogEntry("CorrectionLocationEstimate: corrected location " + rl.coord);
						}
					else
						Log.LogEntry("CorrectLocationEstimate: Could not determine location. Unsupported two blocked point configuration");
					}
				else
					Log.LogEntry("CorrectLocationEstimate: Could not determine location.  No distance to unblock available.");
				}
			else if (bps.Count == 3)
				{
				Room.bad_pt bp1, bp2, bp3;
				int dist;

				bp1 = (Room.bad_pt)bps[0];
				bp2 = (Room.bad_pt)bps[1];
				bp3 = (Room.bad_pt)bps[2];
				if ((bp1.dist != -1) && (bp2.dist != -1) && (bp3.dist != 1))
					{
					bp1.dist += 1;
					bp2.dist += 1;
					bp3.dist += 1;
					dist = Math.Max(bp1.dist,bp2.dist);
					dist = Math.Max(dist,bp3.dist);
					if (((bp1.index == 0) && (bp2.index == 1) && (bp3.index == 2)) || ((bp1.index == 1) && (bp2.index == 2) && (bp3.index == 0)) || ((bp1.index == 2) && (bp2.index == 0) && (bp3.index == 1)))
						{
						rl.coord = NavCompute.MapPoint(new Point(0,-dist), rl.orientation,rl.coord);
						Log.LogEntry("CorrectionLocationEstimate: corrected location " + rl.coord);
						}
					else
						Log.LogEntry("CorrectLocationEstimate: Could not determine location. Unsupported three blocked point configuration");
					}
				else
					Log.LogEntry("CorrectLocationEstimate: Could not determine location.  No distance to unblock available.");
				}
			rtn = (Navigate.rmi.InOpenArea(rl.coord, rl.orientation, ref al));
			return(rtn);
		}



		private void CreateFeatureTargetList(NavData.FeatureType ft,int direct,ref ArrayList targets)

		{
			int i;
			int[] fdirec;
			Room.TurnLimits tl;
			Room.target t;
			NavData.feature f;

			for (i = 0; i < NavData.rd.features.Count; i++)
				{
				f = (NavData.feature)NavData.rd.features[i];
				fdirec = DetermineFeaturePerpDirections(f);
				if (((ft == NavData.FeatureType.NONE) || (f.type == ft)) && ((direct == fdirec[0]) || (direct == fdirec[1])))
					{
					t = new Room.target();
					t.findex = i;
					t.forward = true;
					tl = FeatureTurnLimits(f, fdirec, direct);
					if (tl == Room.TurnLimits.NO_TURN)
						{
						t.plus = false;
						t.minus = false;
						}
					else if (tl == Room.TurnLimits.NONE)
						{
						t.plus = true;
						t.minus = true;
						}
					else if (tl == Room.TurnLimits.MINUS_ONLY)
						{
						t.plus = false;
						t.minus = true;
						}
					else
						{
						t.plus = true;
						t.minus = false;
						}
					targets.Add(t);
					}
				}
		}



		private Room.rm_location Trilateration(Room.feature_match fm1, Room.feature_match fm2,int orient)

		{
			Room.rm_location rl = new Room.rm_location();
			ArrayList bad_pts = new ArrayList();
			NavCompute.pt_to_pt_data ppd;
			int mh,mad;

			if (((NavData.feature)NavData.rd.features[fm1.index]).coord == ((NavData.feature) NavData.rd.features[fm2.index]).coord)
				{
				Log.LogEntry("Trying to make trilateration with same points.");
				rl.coord = new Point(0, 0);
				stat = LocationDeterminationStatus.FAILED;
				}
			else
				{
				rl.coord = NavCompute.Trilaterate(fm1,fm2);
				if ((rl.coord.X > NavData.rd.rect.Width) || (rl.coord.X < 0) || (rl.coord.Y > NavData.rd.rect.Height) || (rl.coord.Y < 0))
					{
					Log.LogEntry("Location estimate is impossible.");
					rl.coord = new Point(0,0);
					stat = LocationDeterminationStatus.FAILED;
					}
				else
					{
					if (orient != -1)
						{
						rl.orientation = orient;
						stat = LocationDeterminationStatus.GOOD_MATCH;
						}
					else
						{
						ppd = NavCompute.DetermineRaDirectDistPtToPt(((NavData.feature)NavData.rd.features[fm2.index]).coord, rl.coord);
						rl.orientation = (HeadAssembly.HA_CENTER_ANGLE - fm2.head_angle + ppd.direc) % 360;
						mh = HeadAssembly.GetMagneticHeading();
						mad = NavCompute.DetermineDirection(mh);
						if (!sf.WithInMagLimit(rl.orientation,mad))
							{
							rl.coord.X = 0;
							rl.coord.Y = 0;
							stat = LocationDeterminationStatus.FAILED;
							Log.LogEntry("Bad orientation estimate.");
							}
						else
							stat = LocationDeterminationStatus.GOOD_MATCH;
						}
					}
				}
			Log.LogEntry("Determined pose: " + rl.coord + "  " + rl.orientation);
			Log.LogEntry("Trilaterate location status: " + stat);
			return(rl);
		}



		private bool DetermineUnkownLocation(ref NavData.location cloc)

		{
			ArrayList bad_pts = new ArrayList();
			Room.feature_match fm1, fm2;
			Point est = new Point(0,0);
			int direct,pdirect,pangle,angle,chpangle,wdist,turn = 0;
			ArrayList targets;
			int[] fdirec;
			double nsee = 0;
			int dist = 0,orientation = -1,hangle;
			Room.rm_location erl = new Room.rm_location();
			NavCompute.pt_to_pt_data ppd;
			bool rtn = false;
			NavData.location loc = new NavData.location();

			Log.LogEntry("DetermineLocation: unknow status");
			pdirect = direct = DeterminePreferedDirection();
			HeadAssembly.SetHeadHeading(NavData.heading_table[direct]);
			hangle = HeadAssembly.HA_CENTER_ANGLE - HeadAssembly.CurrentHeadAngle();
			if ((Kinect.FindDistDirectPerpToWall(ref turn,ref nsee,ref dist,0,20)) && (nsee < sf.GetNeseThreshold()))
				{
				HeadAssembly.SendHeadAngle(HeadAssembly.CurrentHeadAngle() - turn,true);
				orientation = (direct + turn + hangle) % 360;
				if (orientation < 0)
					orientation += 360;
				}
			pangle = HeadAssembly.CurrentHeadAngle();
			chpangle = pangle;
			Log.LogEntry("DetermineLocation initial settings: wall direction - " + direct + "  orientation - " + orientation +   "  perp head angle - " + pangle + "  mag heading - " + HeadAssembly.GetMagneticHeading());
			targets = new ArrayList();
			CreateFeatureTargetList(NavData.FeatureType.CORNER, direct, ref targets);
			Log.LogEntry("Location and orientation estimates:");
			do
				{
				do
					{
					Log.LogEntry("Direction: " + direct);
					wdist = Kinect.WallDistance();
					fm1 = FindFeatureWSearch(ref targets,55, cloc.coord);
					if (fm1.matched)
						{
						if (orientation != -1)
							{
							erl = NavCompute.FeatureOrientationApprox(fm1,orientation);
							est = erl.coord;
							}
						else if (wdist > 0)
							{
							est = NavCompute.PerpDistPtApprox(fm1, wdist, direct, chpangle);
							ppd = NavCompute.DetermineRaDirectDistPtToPt(((NavData.feature)NavData.rd.features[fm1.index]).coord, est);
							orientation = (HeadAssembly.HA_CENTER_ANGLE - fm1.head_angle + ppd.direc) % 360;
							}

						else
							{
							est = NavCompute.PerpHeadingPtApprox(fm1,Math.Abs(fm1.head_angle - HeadAssembly.CurrentHeadAngle()),direct,chpangle);
							ppd = NavCompute.DetermineRaDirectDistPtToPt(((NavData.feature)NavData.rd.features[fm1.index]).coord, est);
							orientation = (HeadAssembly.HA_CENTER_ANGLE - fm1.head_angle + ppd.direc) % 360;
							}
						if ((est.X > NavData.rd.rect.Width) || (est.X < 0) || (est.Y > NavData.rd.rect.Height) || (est.Y < 0))
							{
							fm1.matched = false;
							Log.LogEntry("Impossible location estimate.");
							}
						}
					}
				while (!fm1.matched && (targets.Count > 0));
				if (!fm1.matched)
					{
					if (direct == pdirect)
						{
						direct = (pdirect + 90) % 360;
						CreateFeatureTargetList(NavData.FeatureType.CORNER, direct, ref targets);
						angle = pangle + 90;
						if ((angle > HeadAssembly.MAX_HA_ANGLE) || (targets.Count == 0))
							{
							Log.LogEntry("Can not search direction: " + direct);
							targets.Clear();
							direct = pdirect - 90;
							if (direct < 0)
								direct += 360;
							CreateFeatureTargetList(NavData.FeatureType.CORNER, direct, ref targets);
							angle = pangle - 90;
							if ((angle < HeadAssembly.MIN_HA_ANGLE) || (targets.Count < 0))
								{
								Log.LogEntry("Can not search direction: " + direct);
								targets.Clear();
								}
							}
						if (targets.Count > 0)
							{
							HeadAssembly.SendHeadAngle(angle,true);
							chpangle = angle;
							}
						}
					else if (direct == (pdirect + 90) % 360)
						{
						direct = pdirect - 90;
						if (direct < 0)
							direct += 360;
						CreateFeatureTargetList(NavData.FeatureType.CORNER, direct, ref targets);
						angle = pangle - 90;
						if ((angle < HeadAssembly.MIN_HA_ANGLE) || (targets.Count == 0))
							{
							targets.Clear();
							Log.LogEntry("Can not search direction: " + direct);
							}
						else
							{
							HeadAssembly.SendHeadAngle(angle,true);
							chpangle = angle;
							}
						}
					}
				}
			while(!fm1.matched && targets.Count != 0);
			if (fm1.matched)
				{
				fdirec = DetermineFeaturePerpDirections(((NavData.feature) NavData.rd.features[fm1.index]));
				if ((pdirect == fdirec[0]) || (pdirect == fdirec[1]))
					{
					HeadAssembly.SendHeadAngle(pangle,true);
					direct = pdirect;
					}
				fm2 = FindSecondFeature(fm1.index,est,direct);
				if (fm2.matched)
					{
					erl = Trilateration(fm1,fm2,orientation);
					if (erl.coord.IsEmpty)
						{
						Log.LogEntry("Trilateration failed, trying DR localization");
						HeadAssembly.SendHeadAngle(HeadAssembly.HA_CENTER_ANGLE, true);
						loc.rm_name = cloc.rm_name;
						loc.coord = est;
						loc.orientation = orientation;
						loc.ls = NavData.LocationStatus.USR;
						MotionMeasureProb.UserLocalize(new MotionMeasureProb.Pose(loc.coord,loc.orientation));
						if (DetermineDRLocation(ref loc,true,new Point(0,0)))
							{
							cloc = loc;
							stat = LocationDeterminationStatus.GOOD_MATCH;
							rtn = true;
							}
						else
							{
							Log.LogEntry("DR localization did not help,");
							stat = LocationDeterminationStatus.FAILED;
							}
						}
					else
						{
						loc.rm_name = cloc.rm_name;
						loc.coord = erl.coord;
						loc.orientation = erl.orientation;
						loc.ls = NavData.LocationStatus.VERIFIED;
						if (ReasonableLocationEstimate(ref loc))
							{
							cloc = loc;
							stat = LocationDeterminationStatus.GOOD_MATCH;
							rtn = true;
							MotionMeasureProb.CompleteLocalize(new MotionMeasureProb.Pose(cloc.coord,cloc.orientation));
							}
						}
					}
				else
					{
					HeadAssembly.SendHeadAngle(HeadAssembly.HA_CENTER_ANGLE, true);
					Log.LogEntry("Could not find second feature, trying DR localization");
					loc.rm_name = cloc.rm_name;
					loc.coord = est;
					loc.orientation = orientation;
					loc.ls = NavData.LocationStatus.USR;
					NavData.SetCurrentLocation(loc);
					MotionMeasureProb.UserLocalize(new MotionMeasureProb.Pose(loc.coord, loc.orientation));
					if (DetermineDRLocation(ref loc,true,new Point(0,0)))
						{
						cloc = loc;
						stat = LocationDeterminationStatus.GOOD_MATCH;
						rtn = true;
						}
					else
						{
						Log.LogEntry("DR localization did not help,");
						stat = LocationDeterminationStatus.FAILED;
						}
					}
				}
			else
				{
				Log.LogEntry("Could not find initial feature");
				stat = LocationDeterminationStatus.FAILED;
				}
			HeadAssembly.SendHeadAngle(HeadAssembly.HA_CENTER_ANGLE, true);
			return(rtn);
		}



		private bool StartDistDirectApprox(Point start,int direct,ref Room.rm_location rl)

		{
			bool rtn = false;
			string rply;
			int dist;

			rply = MotionControl.SendCommand(SharedData.DIST_MOVED,200);
			if (rply.StartsWith("ok"))
				{
				dist = int.Parse(rply.Substring(3));
				rl = NavCompute.PtDistDirectApprox(start,direct,dist);
				if (!rl.coord.IsEmpty)
					rtn = true;
				}
			return(rtn);
		}



		private bool ReasonableLocationEstimate(NavData.location oldloc, ref NavData.location rl)

		{
			bool rtn = true;
			ArrayList bad_pts = new ArrayList();

			if ((rl.coord.X >= NavData.rd.rect.Width) || (rl.coord.X < 0))
				{
				if ((rl.coord.Y > NavData.rd.rect.Height) || (rl.coord.Y < 0))
					{
					Log.LogEntry("Location.ReasonableLocationEstimate: Impossible location estimate.");
					rtn = false;
					}
				else 
					{
					if (rl.coord.X < 0)
						rl.coord.X = 0;
					else
						rl.coord.X = NavData.rd.rect.Width - 1;
					}
				}
			else if ((rl.coord.Y >= NavData.rd.rect.Height) || (rl.coord.Y < 0))
				{
				if ((rl.coord.X > NavData.rd.rect.Width) || (rl.coord.X < 0))
					{
					Log.LogEntry("Location.ReasonableLocationEstimate: Impossible location estimate.");
					rtn = false;
					}
				else
					{
					if (rl.coord.Y < 0)
						rl.coord.Y = 0;
					else
						rl.coord.Y = NavData.rd.rect.Height - 1;
					}
				}
			else if (rtn && !oldloc.coord.IsEmpty)
				{
				if (!MotionMeasureProb.InPdfEllipse(rl.coord))
					{
					Log.KeyLogEntry("Accepted location estimate is not in the location PDF.");
					rtn = false;
					}
				else if (!lde.CloseOpenWall(rl,18) && !oldloc.entrance && !Navigate.rmi.InOpenArea(rl.coord, rl.orientation, ref bad_pts))
					{
					if (!CorrectLocationEstimate(ref rl,ref bad_pts))
						{
						rtn = false;
						Log.LogEntry("Location.ReasonableLocationEstimate: attempt to make corrected location estimate failed.");
						}
					}
				}
			return (rtn);
		}



		private bool ReasonableLocationEstimate(ref NavData.location rl)

		{
			bool rtn = true;
			ArrayList bad_pts = new ArrayList();

			if ((rl.coord.X >= NavData.rd.rect.Width) || (rl.coord.X < 0) || (rl.coord.Y >= NavData.rd.rect.Height) || (rl.coord.Y < 0))
				{
				Log.LogEntry("Location.ReasonableLocationEstimate: Impossible location estimate.");
				rtn = false;
				}
			else
				{
				if (!Navigate.rmi.InOpenArea(rl.coord,rl.orientation,ref bad_pts))
					{
					if (!CorrectLocationEstimate(ref rl,ref bad_pts))
						{
						rtn = false;
						Log.LogEntry("Location.ReasonableLocationEstimate: attempt to make corrected location estimate failed.");
						}
					}
				}
			return(rtn);
		}



		private bool PerpWallEdgeMethod(NavData.location cloc, LocDecisionEngine.action act,feature_match_plus wfmp, ref Room.feature_match fm)

		{
			bool rtn = false;
			SharedData.RobotLocation edge_direc;
			NavCompute.pt_to_pt_data ppd;
			LocDecisionEngine.loc_info li;
			int ra = 0,dist = 0,starta,starti,searcha,startd,direc,searchb;
			MotionMeasureProb.Limits limits;

			Log.LogEntry("PerpWallEdgeMethod: " + cloc.coord.ToString() + " " + cloc.orientation + ", " + act.act.ToString() + ", " + wfmp.actn.info_index);
			fm.matched = false;
			li = lde.GetContext(wfmp.actn.info_index);
			if ((act.sensor == LocDecisionEngine.Sensor.FLIDAR) && (wfmp.actn.sensor == LocDecisionEngine.Sensor.FLIDAR))
				{
				int shift_angle;

				if (wfmp.actn.info_index < act.info_index)
					edge_direc = SharedData.RobotLocation.RIGHT;
				else
					edge_direc = SharedData.RobotLocation.LEFT;
				starti = wfmp.fm.pw.start_indx;
				if (edge_direc == SharedData.RobotLocation.LEFT)
					starti = (starti + wfmp.fm.pw.count - 1) % sdata.Count;
				starta = ((Rplidar.scan_data) sdata[starti]).angle;
				startd = (cloc.orientation + starta) % 360;
				limits = MotionMeasureProb.PdfLimits(act.coord);
				direc = (cloc.orientation - limits.min_ra) % 360;
				if (direc < 0)
					direc += 360;
				searcha = NavCompute.AngularDistance(startd,direc);
				direc = (cloc.orientation - limits.max_ra) % 360;
				if (direc < 0)
					direc += 360;
				searchb = NavCompute.AngularDistance(startd,direc);
				if (searchb > searcha)
					searcha = searchb;
				searcha += (int) Math.Round(Math.Sqrt(MotionMeasureProb.OrientationVariance()) * 3);
				shift_angle = li.direct - wfmp.fm.orient;
				if (shift_angle < 0)
					shift_angle += 360;
				if (Rplidar.FindEdge(ref sdata,shift_angle, ref ra, ref dist,wfmp.fm.pw.start_indx,wfmp.fm.pw.count,searcha,edge_direc))
					{
					if ((dist <= limits.max_dist) && (dist >= limits.min_dist) && (ra <= limits.max_ra) && (ra >= limits.min_ra))
						{
						rtn = true;
						fm.matched = true;
						fm.ra = ra;
						fm.distance = dist;
						Log.LogEntry("Edge found with LIDAR: relative angle " + fm.ra + "°, distance " + fm.distance + " in.");
						}
					else
						{
						Log.KeyLogEntry("Matching edge not found. Expected " + limits.min_ra + " to " + limits.max_ra + "°, " + limits.min_dist + " to " + limits.max_dist + " in.");
						Log.KeyLogEntry("                         Found " + ra + "°, " + dist + " in");
						}
					}
				else
					Log.LogEntry("Edge not found");
				}
			else if ((act.sensor == LocDecisionEngine.Sensor.KINECT) && (wfmp.actn.sensor == LocDecisionEngine.Sensor.KINECT))
				{
				int shift_angle;
				int chead_direct = 0;
				int offset;
				bool high_col,rslt;
				short[] ddata;
				int corient;

				ppd = NavCompute.DetermineRaDirectDistPtToPt(act.coord,cloc.coord);
				if (act.head_motion_required)
					{
					chead_direct = HeadAssembly.CurrentHeadAngle();
					shift_angle = ppd.direc - wfmp.fm.orient;
					if (shift_angle > 180)
						shift_angle -= 360;
					else if (shift_angle < -180)
						shift_angle += 360;
					HeadAssembly.SendHeadAngle((chead_direct + shift_angle) % 360, true);
					shift_angle = li.direct - ppd.direc;
					if (shift_angle < 0)
						shift_angle += 360;
					corient = ppd.dist;
					}
				else
					{
					corient = wfmp.fm.orient;
					shift_angle = li.direct - wfmp.fm.orient;
					if (shift_angle < 0)
						shift_angle += 360;
					}
				if (wfmp.actn.info_index < act.info_index)
					{
					high_col = true;
					if (act.head_motion_required)
						offset = 10;
					else
						offset = (ppd.direc - corient) + 10;
					}
				else
					{
					high_col = false;
					if (act.head_motion_required)
						offset = -10;
					else
						offset = (ppd.direc - corient) - 10;
					}
				ddata = new short[Kinect.nui.DepthStream.FramePixelDataLength];
				if (Kinect.GetDepthFrame(ref ddata, 30))
					{
					limits = MotionMeasureProb.PdfLimits(act.coord);      // BASED ON INITIAL MOVE PDF, DOES NOT REFLECT NEW LOC DATA
					if (((rslt = Kinect.FindEdge(ref ddata, shift_angle, ref ra, ref dist, offset, high_col))) && (dist <= limits.max_dist) && (dist >= limits.min_dist) && (ra <= limits.max_ra) && (ra >= limits.min_ra))
						{
						Log.LogEntry("Matching edge found");
						rtn = true;
						fm.matched = true;
						fm.ra = ra;
						fm.distance = dist;
						}
					else if (!rslt)
						Log.LogEntry("Edge not found");
					else
						{
						Log.LogEntry("Matching edge not found. Expected " + limits.min_ra + " to " + limits.max_ra + "°, " + limits.min_dist + " to " + limits.max_dist + " in.");
						Log.LogEntry("                         Found " + ra + "°, " + dist + " in");
						}
					}
				else
					Log.LogEntry("Could not acquired depth frame");
				if (act.head_motion_required)
					HeadAssembly.SendHeadAngle(chead_direct, true);
				}
			else
				Log.LogEntry("PerpWallEdgeMethod: perp wall sensor did not match action sensor.");
			return(rtn);
		}



		private bool TakeEdgeAction(NavData.location cloc, LocDecisionEngine.action act, ref Room.feature_match fm)

		{
			bool rtn = false,wall_found = false,done = false;
			int index = 0;
			LocDecisionEngine.loc_info li = new LocDecisionEngine.loc_info();
			feature_match_plus fmp;

			try
			{
			fm.matched = false;
			Log.LogEntry("TakeEdgeAction: " + cloc.coord.ToString() + " " + cloc.orientation + ", " + act.act.ToString());
			if (act.info_index > 0)
				{
				index = act.info_index -1;
				li = lde.GetContext(index);
				if ((li.loc_type == LocDecisionEngine.Type.WALL) || (li.loc_type == LocDecisionEngine.Type.OBSTRUCT_WALL))
					{
					if (NavCompute.DistancePtToPt(li.end,act.coord) < 2)
						{
						wall_found = true;
						if (successful_actions.Count > 0)
							{
							fmp = (feature_match_plus)successful_actions[0];
							if ((index == fmp.actn.info_index) && (fmp.actn.act == LocDecisionEngine.Actions.PERP_DIR))
								{
								done = true;
								rtn = PerpWallEdgeMethod(cloc,act,fmp,ref fm);
								}
							}
						}
					}
				}
			if (!wall_found)
				{
				if (act.info_index < lde.GetContectCount() - 1)
					{
					index = act.info_index + 1;
					li = lde.GetContext(index);
					if ((li.loc_type == LocDecisionEngine.Type.WALL) || (li.loc_type == LocDecisionEngine.Type.OBSTRUCT_WALL))
						{
						if (NavCompute.DistancePtToPt(li.start,act.coord) < 2)
							{
							wall_found = true;
							if (successful_actions.Count > 0)
								{
								fmp = (feature_match_plus) successful_actions[0];
								if ((index == fmp.actn.info_index) && (fmp.actn.act == LocDecisionEngine.Actions.PERP_DIR))
									{
									rtn = PerpWallEdgeMethod(cloc, act, fmp, ref fm);
									}
								}
							}
						}
					}
				}
			if (wall_found && !done)
				Log.LogEntry("TakeEdgeAction: perp wall not located.");
			else if (!wall_found)
				Log.LogEntry("TakeEdgeAction: wall not found.");
			if (fm.matched)
				{
				int i;
				ArrayList features;
				NavData.feature feature;

				features = NavData.GetCurrentRoomsFeatures();
				for (i = 0;i < features.Count;i++)
					{
					feature = (NavData.feature) features[i];
					if (feature.coord == act.coord)
						{
						fm.index = i;
						break;
						}
					}
				fm.head_angle = (int) Math.Round(HeadAssembly.HA_CENTER_ANGLE - fm.ra);
				}
			}

			catch(Exception ex)
			{
			Log.LogEntry("Exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			Log.LogEntry("Source: " + ex.Source);
			}

			return(rtn);
		}



		private bool TakePerpDirAction(ref NavData.location cloc, LocDecisionEngine.action act, ref Room.feature_match fm)

		{
			bool rtn = false;
			int shift_angle,org_cshift_angle,i,center_shift_angle;
			LocDecisionEngine.loc_info li;
			int chead_direct = 0;
			Point midpt;
			NavCompute.pt_to_pt_data ppd;

			Log.LogEntry("Location.TakePerpDirAction: " + cloc.coord.ToString() + " " + cloc.orientation + ", " + act.act.ToString());
			li = lde.GetContext(act.info_index);
			midpt = new Point((li.start.X + li.end.X) / 2, (li.start.Y + li.end.Y) / 2);
			if ((act.sensor == LocDecisionEngine.Sensor.FLIDAR) && (li.loc_type != LocDecisionEngine.Type.OPEN_WALL)) // open wall handling does not work well
				{
				int sd,ed,md;

				ppd = NavCompute.DetermineRaDirectDistPtToPt(li.start,cloc.coord);
				sd = ppd.direc;
				ppd = NavCompute.DetermineRaDirectDistPtToPt(li.end, cloc.coord);
				ed = ppd.direc;
				md = (sd + (NavCompute.AngularDistance(sd,ed)/2)) % 360;
				center_shift_angle = md - cloc.orientation ;
				if (center_shift_angle > 180)
					center_shift_angle -= 360;
				else if (center_shift_angle < -180)
					center_shift_angle += 360;
				org_cshift_angle = center_shift_angle;
				shift_angle = li.direct - cloc.orientation;
				if (shift_angle > 180)
					shift_angle -= 360;
				else if (shift_angle < -180)
					shift_angle += 360;
				for (i = 0;i < 11;i++)
					{
					if ((rtn = sf.LidarFindPerpToWall(ref sdata, center_shift_angle, shift_angle,li.direct,ref fm)))
						break;
					switch(i)
						{
						case 0: shift_angle = org_cshift_angle + 5;
								break;

						case 1: center_shift_angle = org_cshift_angle - 5;
								break;

						case 2: center_shift_angle = org_cshift_angle + 10;
								break;

						case 3: center_shift_angle = org_cshift_angle - 10;
								break;

						case 4: center_shift_angle = org_cshift_angle + 15;
								break;

						case 5: center_shift_angle = org_cshift_angle - 15;
								break;

						case 6: center_shift_angle = org_cshift_angle + 20;
								break;

						case 7: center_shift_angle = org_cshift_angle - 20;
								break;

						case 8: center_shift_angle = org_cshift_angle + 25;
								break;

						case 9: center_shift_angle = org_cshift_angle - 25;
								break;
						}
					if (center_shift_angle > 180)
						center_shift_angle -= 360;
					else if (center_shift_angle < -180)
						center_shift_angle += 360;
					}
				}
			else if ((act.sensor == LocDecisionEngine.Sensor.KINECT) && (!li.target))	// targets can throw off but not with sufficent error to pick it up
				{
				int offset = 0,org_offset,min_angle;

				ppd = NavCompute.DetermineRaDirectDistPtToPt(midpt,cloc.coord);
				if (act.head_motion_required)
					{
					chead_direct = HeadAssembly.CurrentHeadAngle();
					shift_angle = ppd.direc - cloc.orientation;
					if (shift_angle > 180)
						shift_angle -= 360;
					else if (shift_angle < -180)
						shift_angle += 360;
					HeadAssembly.SendHeadAngle((chead_direct + shift_angle) % 360, true);
					}
				else
					offset = ppd.direc - cloc.orientation;
				org_offset = offset;
				min_angle = (int)Math.Ceiling(Math.Atan(((double)SharedData.MIN_WALL_DIST / 2) / li.distance) * SharedData.RAD_TO_DEG);
				for (i = 0; i < 4; i++)
					{
					if ((rtn = sf.KinectFindPerpToWall(offset,min_angle,li.direct,ref fm)))
						break;
					switch(i)
						{
						case 0: offset = org_offset + 5;
								break;

						case 1: offset = org_offset - 5;
								break;

						case 2: offset = org_offset + 10;
								break;

						case 3: offset = org_offset - 10;
								break;
						}
					if (offset > 20)
						offset = 20;
					else if (offset < -20)
						offset = -20;
					}
				if (act.head_motion_required)
					HeadAssembly.SendHeadAngle(chead_direct, true);
				}
			if (rtn)
				{
				bool xcoord = false;
				MotionMeasureProb.CartisianDistLimits limits;

				cloc.orientation = fm.orient;
				if (li.direct == 0)
					{
					cloc.coord.Y = fm.distance;
					xcoord = false;
					}
				else if (li.direct == 90)
					{
					cloc.coord.X = NavData.rd.rect.Width - fm.distance;
					xcoord = true;
					}
				else if (li.direct == 180)
					{
					cloc.coord.Y = NavData.rd.rect.Height - fm.distance;
					xcoord = false;
					}
				else if (li.direct == 270)
					{
					cloc.coord.X = fm.distance;
					xcoord = true;
					}
				limits = MotionMeasureProb.CartisianAbsDistLimit(midpt, xcoord);
				if ((Math.Abs(fm.distance) > limits.max_dist) || (Math.Abs(fm.distance) < limits.min_dist))
					{
					Log.KeyLogEntry("Accepted wall detection is not within the PDF cartisian distance limits: " + limits.min_dist + " to " + limits.max_dist);
					rtn = false;
					}
				}
			return(rtn);
		}



		private bool TakeAction(ref NavData.location cloc,LocDecisionEngine.action act,ref Room.feature_match fm)

		{
			bool rtn = false;
			LocDecisionEngine.loc_info li;
			int chead_direct = 0,pan = 0;
			MotionMeasureProb.Limits limits;
			string rsp;

			Log.LogEntry("Location.TakeAction: " + cloc.coord.ToString() + " " + cloc.orientation + ", " + act.act.ToString());
			li = lde.GetContext(act.info_index);
			if ((act.act == LocDecisionEngine.Actions.TARGET) || (act.act == LocDecisionEngine.Actions.CORNER))
				{
				if (act.act == LocDecisionEngine.Actions.CORNER)
					act.coord = li.start;
				if (act.head_motion_required)
					{
					chead_direct = HeadAssembly.CurrentHeadAngle();
					pan = chead_direct + li.ra;
					rsp = HeadAssembly.SendHeadAngle(pan,true);
					}
				else
					rsp = "ok";
				if (rsp == "ok")
					{
					if (act.act == LocDecisionEngine.Actions.CORNER)
						{
						int wall1_direct,wall2_direct;
						bool perp_wall = false,corner_right = false;

						wall1_direct = (lde.GetContext(act.info_index - 1)).direct;
						wall2_direct = (lde.GetContext(act.info_index + 1)).direct;
						if (NavCompute.AngularDistance(wall1_direct,cloc.orientation) < 10)
							{
							perp_wall = true;
							if (li.ra < 0)
								corner_right = true;
							}
						else if (NavCompute.AngularDistance(wall2_direct, cloc.orientation) < 10)
							{
							perp_wall = true;
							if (li.ra < 0)
								corner_right = true;
							}
						fm = FindFeature((NavData.feature)NavData.rd.features[li.index], cloc.coord,perp_wall, corner_right);
						}
					else
						fm = FindFeature((NavData.feature)NavData.rd.features[li.index], cloc.coord, false, false,li.distance,li.ra);
					if (fm.matched)
						{
						limits = MotionMeasureProb.PdfLimits(act.coord);      // BASED ON INITIAL MOVE PDF, DOES NOT REFLECT NEW LOC DATA
						if ((fm.distance <= limits.max_dist) && (fm.distance >= limits.min_dist) && (Math.Round(fm.ra) <= limits.max_ra) && (Math.Round(fm.ra) >= limits.min_ra))
							{
							int i;
							ArrayList features;
							NavData.feature feature;

							features = NavData.GetFeatures(cloc.rm_name);
							for (i = 0;i < features.Count;i++)
								{
								feature = (NavData.feature) features[i];
								if (feature.coord == act.coord)
									{
									fm.index = i;
									break;
									}
								}
							Log.LogEntry(act.act + " found");
							rtn = true;
							}
						else
							{
							Log.KeyLogEntry("Feature not found. Expected " + limits.min_ra + " to " + limits.max_ra + "°, " + limits.min_dist + " to " + limits.max_dist + " in.");
							Log.KeyLogEntry("                     Found " + fm.ra + "°, " + fm.distance + " in");
							}
						}
					else
						Log.LogEntry(act.act + " not found");
					if (act.head_motion_required)
						HeadAssembly.SendHeadAngle(chead_direct, true);
					}
				else
					Log.LogEntry("Could not pan to face feature.");
				}
			else if (act.act == LocDecisionEngine.Actions.EDGE)
				rtn = TakeEdgeAction(cloc,act,ref fm);
			else if (act.act == LocDecisionEngine.Actions.PERP_DIR)
				rtn = TakePerpDirAction(ref cloc,act,ref fm);
			return(rtn);
		}



		public bool AlreadySuccessfulAction(LocDecisionEngine.action act)

		{
			bool rtn = false;
			feature_match_plus fmp;
			int i;

			if (successful_actions.Count > 0)
				{
				for (i = 0;i < successful_actions.Count;i++)
					{
					fmp = (feature_match_plus) successful_actions[i];
					if ((act.info_index == fmp.actn.info_index) && (act.sensor == fmp.actn.sensor)  && (act.act == fmp.actn.act))
						{
						rtn = true;
						break;
						}
					}
				}
			return(rtn);
		}



		public bool DetermineLocFromFeatures(feature_match_plus fmp,feature_match_plus fmp2,ref NavData.location cloc,ref bool coord_y_good)
		
		{
			bool rtn = false;
			LocDecisionEngine.loc_info li, li2;
			Room.rm_location rl = new Room.rm_location();

			if ((fmp.actn.act == LocDecisionEngine.Actions.PERP_DIR) && (fmp2.actn.act == LocDecisionEngine.Actions.PERP_DIR))
				{
				if (Math.Abs(fmp.fm.orient - fmp2.fm.orient) > 300)
					cloc.orientation = (fmp.fm.orient + fmp2.fm.orient + 360) / 2;
				else
					cloc.orientation = (fmp.fm.orient + fmp2.fm.orient) / 2;
				li = lde.GetContext(fmp.actn.info_index);
				li2 = lde.GetContext(fmp2.actn.info_index);
				rl = NavCompute.TwoWallApprox(fmp.fm.distance, fmp2.fm.distance, li.direct, li2.direct);
				if (!rl.coord.IsEmpty)
					{
					rtn = true;
					cloc.coord = rl.coord;
					cloc.ls = NavData.LocationStatus.VERIFIED;
					}
				else
					{
					rtn = true;
					cloc.ls = NavData.LocationStatus.DR;
					li = lde.GetContext(fmp.actn.info_index);
					if ((li.direct == 0) || (li.direct == 180))
						coord_y_good = true;
					else
						coord_y_good = false;
					}
				}
			else if (fmp.actn.act == LocDecisionEngine.Actions.PERP_DIR)
				{
				cloc.orientation = fmp.fm.orient;
				rl = NavCompute.FeatureOrientationApprox(fmp2.fm, fmp);
				if (!rl.coord.IsEmpty)
					{
					rtn = true;
					cloc.coord = rl.coord;
					cloc.ls = NavData.LocationStatus.VERIFIED;
					}
				else
					{
					rtn = true;
					cloc.ls = NavData.LocationStatus.DR;
					li = lde.GetContext(fmp.actn.info_index);
					if ((li.direct == 0) || (li.direct == 180))
						coord_y_good = true;
					else
						coord_y_good = false;
					}
				}
			else if (fmp2.actn.act == LocDecisionEngine.Actions.PERP_DIR)
				{
				cloc.orientation = fmp2.fm.orient;
				rl = NavCompute.FeatureOrientationApprox(fmp.fm, fmp2);
				if (!rl.coord.IsEmpty)
					{
					rtn = true;
					cloc.coord = rl.coord;
					cloc.ls = NavData.LocationStatus.VERIFIED;
					}
				else
					{
					rtn = true;
					cloc.ls = NavData.LocationStatus.DR;
					li = lde.GetContext(fmp.actn.info_index);
					if ((li.direct == 0) || (li.direct == 180))
						coord_y_good = true;
					else
						coord_y_good = false;
					}
				}
			else
				{
				rl = Trilateration(fmp.fm, fmp2.fm, -1);
				if (!rl.coord.IsEmpty)
					{
					rtn = true;
					cloc.coord = rl.coord;
					cloc.orientation = rl.orientation;
					cloc.ls = NavData.LocationStatus.VERIFIED;
					}
				}

			return (rtn);
		}


		public bool DetermineDRLocation(ref NavData.location cloc,bool head_turn,Point start,bool do_not_use_secondary)

		{
			bool rtn = false,done = false,kinect = false,lidar = false;
			ArrayList failed_actions = new ArrayList();
			Room.feature_match fm;
			LocDecisionEngine.action act;
			feature_match_plus fmp = new feature_match_plus();
			int i,mh,mad;
			feature_match_plus fmp2 = new feature_match_plus();
			LocDecisionEngine.loc_info li;
			Room.rm_location rl = new Room.rm_location();
			NavData.location oloc;
			bool coord_y_good = false;

			try
			{
			Log.LogEntry("DetermineLocation: dead reckoning status");
			oloc = cloc;
			sdata.Clear();
			successful_actions.Clear();
			if (Rplidar.CaptureScan(ref sdata,true))
				{
				lidar = true;
				Rplidar.SaveLidarScan(ref sdata);
				}
			if (Kinect.Operational())
				kinect = true;
			mh = HeadAssembly.GetMagneticHeading();
			mad = NavCompute.DetermineDirection(mh);
			if ((sf.WithInMagLimit(cloc.orientation, mad)) && (kinect || lidar))
				{
				actions = lde.Run(cloc,head_turn,kinect,lidar);
				do
					{
					failed_actions.Clear();
					for (i = 0;i< actions.Count;i++)
						{
						act = (LocDecisionEngine.action) actions[i];
						li = lde.GetContext(act.info_index);
						if ((li.loc_type != LocDecisionEngine.Type.OPEN_WALL) || (li.loc_type != LocDecisionEngine.Type.OBSTRUCT_OPEN_WALL))
							{
							if (!AlreadySuccessfulAction(act))
								{
								fm = new Room.feature_match();
								if (!TakeAction(ref cloc,act,ref fm))
									failed_actions.Add(i);
								else
									{
									fmp = new feature_match_plus();
									fmp.fm = fm;
									fmp.actn = act;
									if (act.act == LocDecisionEngine.Actions.PERP_DIR)
										{
										li = lde.GetContext(act.info_index);
										if (li.direct == 0)
											{
											fmp.coord.Y = cloc.coord.Y;
											fmp.coord.X = -1;
											}
										else if (li.direct == 90)
											{
											fmp.coord.X = cloc.coord.X;
											fmp.coord.Y = -1;
											}
										else if (li.direct == 180)
											{
											fmp.coord.Y = cloc.coord.Y;
											fmp.coord.X = -1;
											}
										else if (li.direct == 270)
											{
											fmp.coord.X = cloc.coord.X;
											fmp.coord.Y = -1;
											}
										else
											{
											fmp.coord.X = -1;
											fmp.coord.Y = -1;
											}
										}
									successful_actions.Add(fmp);
									}
								}
							else
								Log.LogEntry("Prior run match " + act.act.ToString());
							}
						else
							Log.LogEntry("Open wall action removed " + act.act.ToString());
						}
					if ((failed_actions.Count > 0) && (successful_actions.Count < 2))
						{
						actions = lde.ReRun(failed_actions,head_turn);
						}
					else
						done = true;
					}
				while ((actions.Count > 0) && !done);
				if (successful_actions.Count > 0)
					Log.LogEntry("Successfull matches: " + successful_actions.Count);
				if (successful_actions.Count == 0)
					{
					Log.KeyLogEntry("No successful matches.");
					if (!do_not_use_secondary)
						if (StartDistDirectApprox(start,cloc.orientation,ref rl))
							{
							cloc.coord = rl.coord;
							cloc.orientation = rl.orientation;
							rtn = true;
							}
					}
				else if (successful_actions.Count == 1)
					{
					fmp = (feature_match_plus) successful_actions[0];
					if (fmp.actn.act == LocDecisionEngine.Actions.PERP_DIR)
						{
						cloc.orientation = fmp.fm.orient;
						cloc.ls = NavData.LocationStatus.DR;
						li = lde.GetContext(fmp.actn.info_index);
						if ((li.direct == 0) || (li.direct == 180))
							coord_y_good = true;
						else
							coord_y_good = false;
						rtn = true;
						}
					else
						{
						rl = NavCompute.FeatureOrientationApprox(fmp.fm,cloc.orientation);
						if (!rl.coord.IsEmpty)
							{
							cloc.coord = rl.coord;
							cloc.orientation = rl.orientation;
							rtn = true;
							}
						else if (!do_not_use_secondary)
							{
							if (StartDistDirectApprox(start, cloc.orientation, ref rl))
								{
								cloc.coord = rl.coord;
								cloc.orientation = rl.orientation;
								rtn = true;
								}
							}
						}
					}
				else if (successful_actions.Count == 2)
					{
					fmp = (feature_match_plus) successful_actions[0];
					fmp2 = (feature_match_plus) successful_actions[1];
					rtn = DetermineLocFromFeatures(fmp,fmp2,ref cloc,ref coord_y_good);
					}
				else if (successful_actions.Count > 2)
					{
					int j;
					ArrayList success = new ArrayList();
					int[] pair = new int[2];

					for (i = 0;i < successful_actions.Count;i++)
						{
						fmp = (feature_match_plus)successful_actions[i];
						for (j = i + 1;j < successful_actions.Count;j++)
							{
							fmp2 = (feature_match_plus) successful_actions[j];
							rtn = DetermineLocFromFeatures(fmp,fmp2,ref cloc,ref coord_y_good);
							if (rtn && (cloc.ls == NavData.LocationStatus.VERIFIED))
								break;
							else if (rtn)
								{
								pair[0] = i;
								pair[1] = j;
								success.Add(pair);
								}
							}
						if (rtn && (cloc.ls == NavData.LocationStatus.VERIFIED))
							break;
						}
					if (!rtn && (success.Count > 0))
						{
						pair = (int[]) success[0];
						fmp = (feature_match_plus) successful_actions[pair[0]];
						fmp2 = (feature_match_plus) successful_actions[pair[1]];
						rtn = DetermineLocFromFeatures(fmp,fmp2,ref cloc,ref coord_y_good);
						}
					}
				if (rtn)
					{
					if (ReasonableLocationEstimate(oloc,ref cloc))
						{
						if (cloc.ls == NavData.LocationStatus.VERIFIED)
							MotionMeasureProb.CompleteLocalize(new MotionMeasureProb.Pose(cloc.coord, cloc.orientation));
						else if (successful_actions.Count > 0)
							{
							fmp = (feature_match_plus) successful_actions[0];
							if (fmp.actn.act == LocDecisionEngine.Actions.PERP_DIR)
								{
								if (coord_y_good)
									MotionMeasureProb.WallOnlyLocalize(cloc.coord,new MotionMeasureProb.Pose(-1, cloc.coord.Y, cloc.orientation));
								else
									MotionMeasureProb.WallOnlyLocalize(cloc.coord,new MotionMeasureProb.Pose(cloc.coord.X, -1, cloc.orientation));
								}
							else
								MotionMeasureProb.SinglePointLocalize(new MotionMeasureProb.Pose(cloc.coord,cloc.orientation));
							}
						}
					else
						{
						cloc = oloc;
						rtn = false;
						}

					}
				}
			else
				if (!kinect && ! lidar)
					Log.LogEntry("Neither Kinect or LIDAR available.");
				else
					{
					Log.LogEntry("Orientation did not agree with magnectic compass: orient - " + cloc.orientation + "  mag compass - " + mad);
					cloc.ls = NavData.LocationStatus.UNKNOWN;
					NavData.SetCurrentLocation(cloc);
					}
			}

			catch(Exception ex)
			{
				rtn = false;
				Log.LogEntry("Exception: " + ex.Message);
				Log.LogEntry("Source: " + ex.Source);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			return(rtn);
		}



		public bool DetermineDRLocation(ref NavData.location cloc,bool head_turn,Point start)

		{
			bool rtn = false;

			if (cloc.ls == NavData.LocationStatus.VERIFIED)
				rtn = true;
			else if (start.IsEmpty)
				rtn = DetermineDRLocation(ref cloc,head_turn,start,true);
			else
				rtn = DetermineDRLocation(ref cloc,head_turn,start,false);
			return(rtn);
		}



		public NavData.location DetermineLocation( Point start,bool head_turn)

		{
			NavData.location cloc;
			RechargeDock rdock = new RechargeDock();

			cloc = NavData.GetCurrentLocation();
			stat = LocationDeterminationStatus.NO_MATCH;
			if (cloc.ls == NavData.LocationStatus.VERIFIED)
				stat = LocationDeterminationStatus.GOOD_MATCH;
			else if (cloc.loc_name == SharedData.RECHARGE_LOC_NAME)
				{
				if (rdock.AtRechargeDock(ref cloc))
					{
					NavData.SetCurrentLocation(cloc);
					stat = LocationDeterminationStatus.GOOD_MATCH;
					}
				}
			if (cloc.ls == NavData.LocationStatus.UNKNOWN)
				{
				if (DetermineUnkownLocation(ref cloc))
					{
					if (cloc.ls == NavData.LocationStatus.VERIFIED)
						stat = LocationDeterminationStatus.GOOD_MATCH;
					else
						{
						cloc.coord = new Point(0,0);
						cloc.ls = NavData.LocationStatus.UNKNOWN;
						stat = LocationDeterminationStatus.FAILED;
						}
					}
				else
					cloc.coord = new Point(0,0);
				}
			else if ((cloc.ls == NavData.LocationStatus.DR) || (cloc.ls == NavData.LocationStatus.USR))
				{
				if (DetermineDRLocation(ref cloc,head_turn,start))
					stat = LocationDeterminationStatus.GOOD_MATCH;
				else
					cloc.coord = new Point(0,0);
				}
			return (cloc);
		}



		public LocationDeterminationStatus LocationDetermineStatus()

		{
			return(stat);
		}

		}
	}
