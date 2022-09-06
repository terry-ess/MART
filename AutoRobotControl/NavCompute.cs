using System;
using System.Collections;
using System.Drawing;
using MathNet.Numerics.LinearAlgebra.Double;

namespace AutoRobotControl
	{
	public static class NavCompute
		{

		public enum out_of_bounds {NOT = 0,X_LOW = 1,X_HIGH = 2,Y_LOW = 4,Y_HIGH = 8};
		private static int MAX_ANGLE_DIFF = 10;

		public struct pt_to_pt_data
			{
			public int ra;
			public int direc;
			public int dist;
			};


		private static DenseMatrix mat90 = DenseMatrix.OfArray(new[,] { { 0.0, -1.0 }, { 1.0, 0.0 } });
		private static DenseMatrix matxreflect = DenseMatrix.OfArray(new[,] { { 1.0, 0.0 }, { 0.0, -1.0 } });  //required because room's postive Y is down (GUI style coordinate system)


		public static int AngularDistance(int a1,int a2)

		{
			int ad;

			ad = Math.Abs(a1 - a2);
			if (ad > 180)
				ad = 360 - ad;
			return(ad);
		}



		public static bool ToRightDirect(int current_dir,int desired_direc)

		{
			bool rtn = false;
			int tangle = 0;

			tangle = current_dir - desired_direc;
			if (tangle > 180)
				tangle -= 360;
			else if (tangle < -180)
				tangle += 360;
			if (tangle < 0)
				rtn = true;
			return(rtn);
		}



		public static int DetermineHeadingPtToPt(Point to_pt,Point from_pt)

		{
			int direct = -1,dy,dx,ra;

			dy = to_pt.Y - from_pt.Y;
			dx = to_pt.X - from_pt.X;
			if (dy == 0)
				if (dx > 0)
					direct = 90;
				else
					direct = 270;
			else
				{
				ra = (int)Math.Round(Math.Atan((double)dx / dy) * SharedData.RAD_TO_DEG);
				if (dy < 0)
					direct = (360 - ra) % 360;
				else
					direct = 180 - ra;
				}
			direct = NavData.heading_table[direct];
			Log.LogEntry("DetermineHeadingPtToPt from " + from_pt + " to " + to_pt + " : " + direct);
			return(direct);
		}



		public static pt_to_pt_data DetermineRaDirectDistPtToPt(Point to_pt,Point from_pt,bool log)

		{

			int dy, dx;
			pt_to_pt_data rtn;

			dy = to_pt.Y - from_pt.Y;
			dx = to_pt.X - from_pt.X;
			if (dy == 0)
				{
				if (dx > 0)
					{
					rtn.direc = 90;
					rtn.ra = -90;
					}
				else
					{
					rtn.direc = 270;
					rtn.ra = 90;
					}
				}
			else
				{
				rtn.ra = (int) Math.Round(Math.Atan((double)dx / dy) * SharedData.RAD_TO_DEG);
				if (dy > 0)
					rtn.direc = (180 - rtn.ra) % 360;
				else
					rtn.direc = (360 - rtn.ra) % 360;
				}
			rtn.dist = (int)Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));

			if (log)
				{
				Log.LogEntry("DetermineRaDirectDistPtToPt from " + from_pt + " to " + to_pt);
				Log.LogEntry("DetermineRaDirectDistPtToPt data: ra - " + rtn.ra + "  direc - " + rtn.direc + "  dist - " + rtn.dist);
				}
			return(rtn);
		}



		public static pt_to_pt_data DetermineRaDirectDistPtToPt(Point to_pt,Point from_pt)

		{
			return(DetermineRaDirectDistPtToPt(to_pt,from_pt,true));
		}


		public static int DistancePtToPt(Point p1,Point p2)

		{
			return ((int) Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)));
		}



		public static Point MapPoint(Point pt,int direction,Point off_set,bool y_reflect = true)

		{
			DenseMatrix mat;
			DenseVector vec, osvec;
			DenseVector result;
			int rangle;
			Point mpt;

			rangle = direction % 90;
			mat = DenseMatrix.OfArray(new[,] { { Math.Cos(rangle * SharedData.DEG_TO_RAD), -Math.Sin(rangle * SharedData.DEG_TO_RAD) }, { Math.Sin(rangle * SharedData.DEG_TO_RAD), Math.Cos(rangle * SharedData.DEG_TO_RAD) } });
			vec = new DenseVector(new[] {(double) pt.X, (double) pt.Y});
			osvec = new DenseVector(new[] { (double)off_set.X, (double)off_set.Y });
			direction %= 360;
			if ((direction >= 90) && (direction < 180))
				vec = vec * mat90;
			else if ((direction >= 180) && (direction < 270))
				vec = vec * mat90 * mat90;
			else if (direction >= 270)
				vec = vec * mat90 * mat90 * mat90;
			if (y_reflect)
				result = (vec * mat * matxreflect) + osvec;
			else
				result = (vec * mat) + osvec;
			mpt = new Point((int) Math.Round(result.Values[0]), (int) Math.Round(result.Values[1]));
			Log.LogEntry("MapPoint for " + pt + "," + direction + "," + off_set + " : " + mpt);
			return(mpt);
		}



		public static int DetermineDirection(int mheading)

		{
			int i,direc = -1;

			for (i = 0;i < NavData.heading_table.Length;i++)
				{
				if (NavData.heading_table[i] == mheading)
					direc = i;
				else if ((direc == -1) && (NavData.heading_table[i] < mheading) && (NavData.heading_table[(i + 1) % NavData.heading_table.Length] > mheading))
					direc = i;
				else if ((direc == -1) && (NavData.heading_table[i] > NavData.heading_table[(i + 1) % NavData.heading_table.Length]) && (mheading > NavData.heading_table[i]) && (mheading > NavData.heading_table[(i + 1) % NavData.heading_table.Length]))
					direc = i;
				else if ((direc == -1) && (NavData.heading_table[i] > NavData.heading_table[(i + 1) % NavData.heading_table.Length]) && (mheading < NavData.heading_table[i]) && (mheading < NavData.heading_table[(i + 1) % NavData.heading_table.Length]))
					direc = i;
				}
			Log.LogEntry("DetermineDirection for " + mheading + " : " + direc);
			return (direc);
		}



		private static Point DNCalc(Point current,Point dest)

		{
			Point pt;
			int a, b, c, d;

			d = current.Y;
			b = dest.X - current.X;
			a = current.Y - dest.Y;
			c = (int) (((double) b/a) * d);
			pt = new Point(current.X + c,0);
			return(pt);
		}



		private static Point DECalc(Point current,Point dest)

		{
			Point pt;
			int a, b, c, d;

			d = NavData.rd.rect.Width - current.X;
			b = dest.Y - current.Y;
			a = dest.X - current.X;
			c = (int)(((double)b / a) * d);
			pt = new Point(NavData.rd.rect.Width - 1, current.Y + c);
			return(pt);
		}



		private static Point DSCalc(Point current,Point dest)

		{
			Point pt;
			int a, b, c, d;

			d = NavData.rd.rect.Height - current.Y;
			b = dest.X - current.X;
			a = dest.Y - current.Y;
			c = (int)(((double)b / a) * d);
			pt = new Point(current.X + c, NavData.rd.rect.Height - 1);
			return(pt);
		}



		private static Point DWCalc(Point current,Point dest)

		{
			Point pt;
			int a, b, c, d;

			d = current.X;
			b = current.Y - dest.Y;
			a = dest.X - current.X;
			c = (int)(((double)b / a) * d);
			pt = new Point(0,current.Y + c);
			return(pt);
		}



		public static Point DetermineWallProjectPt(Point current,Point dest,int dir,bool log)

		{
			Point pt = new Point(-1,-1);

			if ((dir > 45) && (dir <= 135))
				{
				pt = DECalc(current, dest);
				if (pt.Y < 0)
					pt = DNCalc(current,dest);
				else if (pt.Y > NavData.rd.rect.Height)
					pt = DSCalc(current,dest);
				}
			else if ((dir > 135) && (dir <= 225))
				{
				pt = DSCalc(current,dest);
				if (pt.X < 0)
					pt = DWCalc(current,dest);
				else if (pt.X > NavData.rd.rect.Width)
					pt = DECalc(current,dest);
				}
			else if ((dir > 225) && (dir <= 315))
				{
				pt = DWCalc(current,dest);
				if (pt.Y < 0)
					pt = DNCalc(current,dest);
				else if (pt.Y > NavData.rd.rect.Height)
					pt = DSCalc(current,dest);
				}
			else
				{
				pt = DNCalc(current,dest);
				if (pt.X < 0)
					pt = DWCalc(current,dest);
				else if (pt.X > NavData.rd.rect.Width)
					pt = DECalc(current,dest);
				}
			if (log)
				Log.LogEntry("DetermineWallProjectPt for " + current + "," + dest + "," + dir + " : " + pt.ToString());
			return(pt);
		}



		public static Point DetermineWallProjectPt(Point current,Point dest,int dir)

		{
			return(DetermineWallProjectPt(current,dest,dir,true));
		}


		private static Point DNCalc(Point current,int dir)

		{
			Point pt;
			int dx,angle;

			if (dir > 270)
				angle = 360 -dir;
			else
				angle = dir;
			dx = (int) Math.Round(current.Y * Math.Tan(angle * SharedData.DEG_TO_RAD));
			if (dir > 270)
				dx *= -1;
			pt = new Point(current.X + dx,0);
			return(pt);
		}



		private static Point DECalc(Point current,int dir)

		{
			Point pt;
			int dy,angle;

			if (dir > 90)
				angle = dir - 90;
			else
				angle = 90 - dir;
			dy = (int)Math.Round((NavData.rd.rect.Width - current.X) * Math.Tan(angle * SharedData.DEG_TO_RAD));
			if (dir < 90)
				dy *= -1;
			pt = new Point(NavData.rd.rect.Width - 1, current.Y + dy);
			return(pt);
		}



		private static Point DSCalc(Point current,int dir)

		{
			Point pt;
			int dx,angle;

			if (dir > 180)
				angle = dir - 180;
			else
				angle = 180 -dir;
			dx = (int)Math.Round((NavData.rd.rect.Height - current.Y) * Math.Tan(angle * SharedData.DEG_TO_RAD));
			if (dir > 180)
				dx *= -1;
			pt = new Point(current.X + dx, NavData.rd.rect.Height - 1);
			return(pt);
		}



		private static Point DWCalc(Point current,int dir)

		{
			Point pt;
			int dy,angle;

			if (dir > 270)
				angle = dir - 270;
			else
				angle = 270 -dir;
			dy = (int) Math.Round(current.X * Math.Tan(angle * SharedData.DEG_TO_RAD));
			if (dir > 270)
				dy *= -1;
			pt = new Point(0,current.Y + dy);
			return(pt);
		}



		public static Point DetermineWallProjectPt(Point current,int dir,bool log)

		{
			Point pt = new Point(-1,-1);

			if ((dir > 45) && (dir <= 135))
				{
				pt = DECalc(current,dir);
				if (pt.Y < 0)
					pt = DNCalc(current,dir);
				else if (pt.Y > NavData.rd.rect.Height)
					pt = DSCalc(current,dir);
				else if (pt.Y == NavData.rd.rect.Height)
					pt.Y -= 1;
				}
			else if ((dir > 135) && (dir <= 225))
				{
				pt = DSCalc(current,dir);
				if (pt.X < 0)
					pt = DWCalc(current,dir);
				else if (pt.X > NavData.rd.rect.Width)
					pt = DECalc(current,dir);
				else if (pt.X == NavData.rd.rect.Width)
					pt.X -= 1;
				}
			else if ((dir > 225) && (dir <= 315))
				{
				pt = DWCalc(current,dir);
				if (pt.Y < 0)
					pt = DNCalc(current,dir);
				else if (pt.Y > NavData.rd.rect.Height)
					pt = DSCalc(current,dir);
				else if (pt.Y == NavData.rd.rect.Height)
					pt.Y -= 1;
				}
			else
				{
				pt = DNCalc(current,dir);
				if (pt.X < 0)
					pt = DWCalc(current,dir);
				else if (pt.X > NavData.rd.rect.Width)
					pt = DECalc(current,dir);
				else if (pt.X == NavData.rd.rect.Width)
					pt.X -= 1;
				}
			if (log)
				Log.LogEntry("DetermineWallProjectPt for " + current + "," + dir + " : " + pt.ToString());
			return(pt);
		}



		public static Point DetermineWallProjectPt(Point current,int dir)

		{
			return(DetermineWallProjectPt(current,dir,true));
		}


		public static Point DetermineObstacleProjectPt(Point current,Point wpp,bool log)

		{
			Point rtn = new Point();
			double dx,dy,dist,hw,x,y;
			int i;

			if (NavData.detail_map != null)
				{
				dist = Math.Sqrt(Math.Pow(current.X - wpp.X, 2) + Math.Pow(current.Y - wpp.Y, 2));
				dx = (wpp.X - current.X)/dist;
				dy = (wpp.Y - current.Y)/dist;
				x = current.X;
				y = current.Y;
				hw = SharedData.ROBOT_WIDTH/2;
				try
				{
				for (i = 0;i < dist;i++)
					{
					x += dx;
					y += dy;
					if (NavData.detail_map[(int)x, (int)y] == (byte)Room.MapCode.BLOCKED)
						{
						rtn.X = (int) x;
						rtn.Y = (int) y;
						break;
						}
					else if ((Math.Abs(dy) >= Math.Abs(dx)) && ((NavData.detail_map[(int)(x + hw), (int)y] == (byte)Room.MapCode.BLOCKED) || (NavData.detail_map[(int)(x - hw), (int)y] == (byte)Room.MapCode.BLOCKED)))
						{
						rtn.X = (int) x;
						rtn.Y = (int) y;
						break;
						}
					else if ((Math.Abs(dx) > Math.Abs(dy)) && ((NavData.detail_map[(int)x, (int)(y + hw)] == (byte)Room.MapCode.BLOCKED) || (NavData.detail_map[(int)x, (int)(y - hw)] == (byte)Room.MapCode.BLOCKED)))
						{
						rtn.X = (int) x;
						rtn.Y = (int) y;
						break;
						}
					}
				}

				catch(IndexOutOfRangeException)

				{
				}

				}
			if (log)
				Log.LogEntry("DetermineObstacleProjectPt for " + current + "," + wpp + " : " + rtn);
			return(rtn);
		}



		public static Point DetermineObstacleProjectPt(Point current,Point wpp)

		{
			return(DetermineObstacleProjectPt(current,wpp,true));
		}


		public static Point DetermineVisualObstacleProjectPt(Point current,Point wpp,byte[,] map,bool log)

		{
			Point rtn = new Point();
			double dx,dy,dist,x,y;
			int i;

			if (map != null)
				{
				dist = Math.Sqrt(Math.Pow(current.X - wpp.X, 2) + Math.Pow(current.Y - wpp.Y, 2));
				dx = (wpp.X - current.X)/dist;
				dy = (wpp.Y - current.Y)/dist;
				x = current.X;
				y = current.Y;
				try
				{
				for (i = 0;i < dist;i++)
					{
					x += dx;
					y += dy;
					if (map[(int) Math.Round(x),(int) Math.Round(y)] == (byte) Room.MapCode.BLOCKED)
						{
						rtn.X = (int) Math.Round(x);
						rtn.Y = (int) Math.Round(y);
						break;
						}
					}
				}

				catch(IndexOutOfRangeException)

				{
				}

				}
			if (log)
				Log.LogEntry("DetermineVisualObstacleProjectPt for " + current + "," + wpp + " : " + rtn);
			return(rtn);
		}



		/* PERPENDICULAR HEADING AND POINT BASED LOCATION APPROXIMATION
			Given the distance to a known point on a wall and the angle to this point from the perpendicular heading to the wall
			we can calculate the approximate location.
		*/

		public static Point PerpHeadingPtApprox(Room.feature_match rfm,int a2,int direct,int pangle)

		{
			Point pt;
			int x;

			x = (int)(rfm.distance * Math.Sin(a2 * SharedData.DEG_TO_RAD));
			if (rfm.head_angle > pangle)
				x = -x;
			pt = new Point(x, (int)-(rfm.distance * Math.Cos(a2 * SharedData.DEG_TO_RAD)));
			pt = NavCompute.MapPoint(pt, direct, ((NavData.feature) NavData.rd.features[rfm.index]).coord);
			Log.LogEntry("PerpHeadingPtApprox param: {" + rfm.index + "," + rfm.distance + "," + rfm.head_angle + "," + rfm.ra + "}, " + a2 + ", " + direct);
			Log.LogEntry("PerpHeadingPtApprox est: " + pt);
			return(pt);
		}



		public static Point PerpDistPtApprox(Room.feature_match rfm,int wdist,int direct,int pangle)

		{
			Point pt;
			int x,y;

			y = -wdist;
			x = (int) Math.Sqrt(Math.Pow(rfm.distance,2) - Math.Pow(y,2));
			if (rfm.head_angle > pangle)
				x = -x;
			pt = new Point(x,y);
			pt = NavCompute.MapPoint(pt, direct, ((NavData.feature) NavData.rd.features[rfm.index]).coord);
			Log.LogEntry("PerpDistPtApprox param: {" + rfm.index + "," + rfm.distance + "," + rfm.head_angle + "," + rfm.ra + "}, " + wdist + ", " + direct);
			Log.LogEntry("PerpDistPtApprox est: " + pt);
			return(pt);
		}



		/* TRILATERATION
			Given the distance to two know points and the distance between these points trilateration calculates the location 
			of the robot. see: http://en.wikipedia.org/wiki/Trilateration and http://mathworld.wolfram.com/Circle-CircleIntersection.html
		*/

		public static Point Trilaterate(Point pt1,Point pt2,int ha1,int ha2,int dist1,int dist2)

		{
			Point pt;
			double d1,d2,d3;
			double x,y;
			int direct;
			pt_to_pt_data ppd;

			d1 = (int) Math.Sqrt(Math.Pow(pt1.X - pt2.X,2) + Math.Pow(pt1.Y - pt2.Y,2));
			if (ha1 < ha2)
				{
				d2 = dist2;
				d3 = dist1;
				ppd = DetermineRaDirectDistPtToPt(pt2,pt1);
				direct = ppd.direc - 90;
				if (direct < 0)
					direct += 360;
				x = (Math.Pow(d1,2) - Math.Pow(d2,2) + Math.Pow(d3,2))/(2 * d1);
				y = -Math.Sqrt(Math.Pow(d3, 2) - Math.Pow(x, 2));
				if ((d3 >= d2) || (d2 <= Math.Sqrt(Math.Pow(y,2) + Math.Pow(d1,2))))
					{
					}
				else
					x = -x;
				pt = NavCompute.MapPoint(new Point((int) x, (int) y),direct,pt1);
				}
			else
				{
				d2 = dist1;
				d3 = dist2;
				ppd = DetermineRaDirectDistPtToPt(pt1,pt2);
				direct = ppd.direc - 90;
				if (direct < 0)
					direct += 360;
				x = (Math.Pow(d1, 2) - Math.Pow(d2, 2) + Math.Pow(d3, 2)) / (2 * d1);
				y = -Math.Sqrt(Math.Pow(d3, 2) - Math.Pow(x, 2));
				if ((d2 <= d3) || (d2 <= Math.Sqrt(Math.Pow(y,2) + Math.Pow(d1,2))))
					{
					}
				else
					x = -x;
				pt = NavCompute.MapPoint(new Point((int) x, (int) y),direct,pt2);
				}
			Log.LogEntry("Trilateration distances: d1 - " + d1 + "  d2 - " + d2 + "  d3 - " + d3);
			Log.LogEntry("Trilateration est: " + pt);
			return (pt);
		}


		public static Point Trilaterate(Room.feature_match f1,Room.feature_match f2)

		{
			Point pt1,pt2;
			int ha1,ha2,dist1,dist2;

			pt1 = ((NavData.feature) NavData.rd.features[f1.index]).coord;
			pt2 = ((NavData.feature) NavData.rd.features[f2.index]).coord;
			ha1 = f1.head_angle;
			ha2 = f2.head_angle;
			dist1 = f1.distance;
			dist2 = f2.distance;
			return(Trilaterate(pt1,pt2,ha1,ha2,dist1,dist2));
		}



		/* CIRCLE - POINT LOCATION BASED APPOXIMATION
			Given an approximate current location, the distance to a known point and the relative angle to the known point
			the best estimate of the actual location is based on the best way to find a point on a circle closest to a given point.
			see: @ http://stackoverflow.com/questions/300871/best-way-to-find-a-point-on-a-circle-closest-to-a-given-point
		*/

		public static Room.rm_location CirclePtApprox(Point center, int radius, Point cpt, int ra)

		{
			Room.rm_location rml = new Room.rm_location();
			pt_to_pt_data ppd;
			Point pt;

			ppd = DetermineRaDirectDistPtToPt(center,cpt);
			pt = new Point(0, ppd.dist - radius);
			rml.coord = NavCompute.MapPoint(pt, ppd.direc,cpt);
			rml.orientation = (int) (ppd.direc + ra);
			if (rml.orientation < 0)
				rml.orientation += 360;
			else if (rml.orientation > 360)
				rml.orientation %= 360;
			Log.LogEntry("CiclePtAppros param: " + center + ", " + radius + ", " + cpt.ToString() + ", " + ra);
			Log.LogEntry("CirclePtApprox est:  coord - " + rml.coord + "  orientation - " + rml.orientation);
			return (rml);
		}



		public static int DirectPerpToWall(Point wp)

		{
			int direct = -1;

			if (wp.Y == 0)
				direct = 0;
			else if (wp.X == 0)
				direct = 270;
			else if (wp.Y == NavData.rd.rect.Height - 1)
				direct = 180;
			else
				direct = 90;
			return(direct);
		}



		// wall 1: wdist1 - distance (in), wdirec1 - wall perp. direction (°)
		// wall 2: wdist2 - distance (in) , wdirec2 - wall perp. direction (°)
		public static Room.rm_location TwoWallApprox(int wdist1,int wdist2,int wdirec1,int wdirec2)

		{
			Room.rm_location rml = new Room.rm_location();

			if ((wdirec1 == 0) && (wdirec2 == 270))
				{
				rml.coord.X = wdist2;
				rml.coord.Y = wdist1;
				}
			else if ((wdirec1 == 270) && (wdirec2 == 0))
				{
				rml.coord.X = wdist1;
				rml.coord.Y = wdist2;
				}
			if ((wdirec1 == 0) && (wdirec2 == 90))
				{
				rml.coord.X = NavData.rd.rect.Width - wdist2;
				rml.coord.Y = wdist1;
				}
			else if ((wdirec1 == 90) && (wdirec2 == 0))
				{
				rml.coord.X = NavData.rd.rect.Width - wdist1;
				rml.coord.Y = wdist2;
				}
			if ((wdirec1 == 180) && (wdirec2 == 90))
				{
				rml.coord.X = NavData.rd.rect.Width - wdist2;
				rml.coord.Y = NavData.rd.rect.Height - wdist1;
				}
			else if ((wdirec1 == 90) && (wdirec2 == 180))
				{
				rml.coord.X = NavData.rd.rect.Width - wdist1;
				rml.coord.Y = NavData.rd.rect.Height - wdist2;
				}
			if ((wdirec1 == 180) && (wdirec2 == 270))
				{
				rml.coord.X = wdist2;
				rml.coord.Y = NavData.rd.rect.Height - wdist1;
				}
			else if ((wdirec1 == 270) && (wdirec2 == 180))
				{
				rml.coord.X = wdist1;
				rml.coord.Y = NavData.rd.rect.Height - wdist2;
				}
			Log.LogEntry("TwoWallApprox param: wdist1 - " + wdist1 + "  wdist2 - " + wdist2 + "  wdirec1 - " + wdirec1 + "  wdirec2 - " + wdirec2);
			Log.LogEntry("TwoWallApprox location est: " + rml.coord);
			return (rml);
		}


		// Feature: fm - feature match
		// Orientation (°): orient - robot orientation
		public static Room.rm_location FeatureOrientationApprox(Room.feature_match fm,int orient)

		{
			int direct;
			Point opt,pt;
			Room.rm_location rl = new Room.rm_location();

			pt = new Point(0, -fm.distance);
			opt = ((NavData.feature) NavData.rd.features[fm.index]).coord;
			direct = (int) Math.Round((double) orient - (HeadAssembly.HA_CENTER_ANGLE - fm.head_angle));
			if (direct < 0)
				direct += 360;
			else if (direct > 360)
				direct -= 360;
			rl.coord = NavCompute.MapPoint(pt,direct,opt);
			rl.orientation = orient;
			Log.LogEntry("Feature orientation approximation");
			Log.LogEntry("  Feature location: " + ((NavData.feature) NavData.rd.features[fm.index]).coord);
			Log.LogEntry("  Feature distance: " + fm.distance + " in.");
			Log.LogEntry("  Feature direction: " + direct + "°");
			Log.LogEntry("  Orientation:  " + orient + "°");
			Log.LogEntry("  Corrected location: " + rl.coord + ", " + rl.orientation + "°");
			return(rl);
		}



		public static Room.rm_location FeatureOrientationApprox(Room.feature_match fm,Location.feature_match_plus fmp)

		{
			Room.rm_location rl;

			rl = FeatureOrientationApprox(fm,fmp.fm.orient);
			if (!rl.coord.IsEmpty)
				{
				if (fmp.coord.X == -1)
					rl.coord.Y = fmp.coord.Y;
				else
					rl.coord.X = fmp.coord.X;
				Log.LogEntry("  Revised location: " + rl.coord + ", " + rl.orientation + "°");
				}
			return(rl);
		}



		// Move start point - sp
		// Distance moved (in) - dist
		// Direction moved (°) = direct
		public static Room.rm_location PtDistDirectApprox(Point sp,int direct,int dist)

		{
			Point pt;
			Room.rm_location rl = new Room.rm_location();

			pt = new Point(0,dist);
			rl.coord = MapPoint(pt,direct,sp);
			rl.orientation = direct;
			Log.LogEntry("PtDistDirectApproc param: sp - " + sp + "  direct - " + direct + "  dist - " + dist);
			Log.LogEntry("PtDistDirect location approx: " + rl.coord + " " + rl.orientation + "°");
			return(rl);
		}



		public static out_of_bounds LocationOutOfBounds(Point coord)

		{
			out_of_bounds rtn = out_of_bounds.NOT;

			if (coord.X < 0)
				rtn = out_of_bounds.X_LOW;
			else if (coord.X > NavData.rd.rect.Width)
				rtn = out_of_bounds.X_HIGH;
			if (coord.Y < 0)
				rtn |= out_of_bounds.Y_LOW;
			else if (coord.Y > NavData.rd.rect.Height)
				rtn |= out_of_bounds.Y_HIGH;
			Log.LogEntry("Location out of bounds " + rtn);
			return(rtn);
		}



		public static string DetermineNewRoom(Point coord,ref NavData.connection connect)

		{
			string rm_name = "";
			ArrayList connectors;
			int i, dist = 1000;

			connectors = NavData.GetCurrentRoomConnections();
			if (connectors.Count == 1)
				{
				connect = (NavData.connection)connectors[0];
				rm_name = connect.name;
				}
			else
				{
				for (i = 0; i < connectors.Count; i++)
					{
					int conn_dist;

					conn_dist = NavCompute.DistancePtToPt(((NavData.connection)connectors[i]).exit_center_coord, coord);
					if (conn_dist < dist)
						{
						dist = conn_dist;
						connect = (NavData.connection)connectors[i];
						rm_name = connect.name;
						}
					}
				}
			return (rm_name);
		}



		public static string DetermineNewRoom(Point coord,out_of_bounds ob,int direct,ref Point nr_coord,ref Point x_coord,ref Point ox_coord,ref NavData.connection oconnector, ref NavData.connection nconnector, ref int mdist)

		{
			string rm_name = "";
			ArrayList connectors;
			int i,dist = 1000;
			NavData.connection connect1 = new NavData.connection(),connect2 = new NavData.connection();
			Point org_coord = new Point();

			connectors = NavData.GetCurrentRoomConnections();
			if (connectors.Count == 1)
				{
				connect1 = (NavData.connection)connectors[0];
				rm_name = connect1.name;
				}
			else
				{
				for (i = 0;i < connectors.Count;i++)
					{
					int conn_dist;

					conn_dist = NavCompute.DistancePtToPt(((NavData.connection) connectors[i]).exit_center_coord,coord);
					if (conn_dist < dist)
						{
						dist = conn_dist;
						connect1 = (NavData.connection)connectors[i];
						rm_name = connect1.name;
						}
					}
				}
			if (rm_name.Length > 0)
				{
				bool process = true;
				int adjust;

				if (((connect1.direction == 90) || (connect1.direction == 270)) && ((ob.HasFlag(out_of_bounds.Y_HIGH)) || (ob.HasFlag(out_of_bounds.Y_LOW))))
					{
					rm_name = NavData.GetCurrentLocation().rm_name;
					adjust = (int) Math.Round((double) SharedData.ROBOT_WIDTH/2);
					nr_coord.X = coord.X;
					if (ob.HasFlag(out_of_bounds.Y_LOW))
						nr_coord.Y = adjust;
					else
						nr_coord.Y = NavData.rd.rect.Height - adjust;
					process = false;
					Log.LogEntry("Mistaken Y out of bounds corrected");
					}
				else if (((connect1.direction == 0) || (connect1.direction == 180)) && ((ob.HasFlag(out_of_bounds.X_HIGH)) || (ob.HasFlag(out_of_bounds.X_LOW))))
					{
					rm_name = NavData.GetCurrentLocation().rm_name;
					adjust = (int) Math.Round((double)SharedData.ROBOT_WIDTH / 2);
					nr_coord.Y = coord.Y;
					if (ob.HasFlag(out_of_bounds.X_LOW))
						nr_coord.X = adjust;
					else
						nr_coord.X = NavData.rd.rect.Width - adjust;
					process = false;
					Log.LogEntry("Mistaken X out of bounds corrected");
					}
				else if (NavCompute.AngularDistance(connect1.direction,direct) > MAX_ANGLE_DIFF)
					{
					rm_name = NavData.GetCurrentLocation().rm_name;
					if (ob.HasFlag(out_of_bounds.X_LOW))
						{
						nr_coord.X = 0;
						nr_coord.Y = coord.Y;
						}
					else if (ob.HasFlag(out_of_bounds.X_HIGH))
						{
						nr_coord.X = NavData.rd.rect.Width - 1;
						nr_coord.Y = coord.Y;
						}
					else if (ob.HasFlag(out_of_bounds.Y_LOW))
						{
						nr_coord.X = coord.X;
						nr_coord.Y = 0;
						}
					else if (ob.HasFlag(out_of_bounds.Y_HIGH))
						{
						nr_coord.X = coord.X;
						nr_coord.Y = NavData.rd.rect.Height - 1;
						}
					process = false;
					Log.LogEntry("Bad direction match in out of bounds corrected");
					}
				if (process)
					{
					string cr_name;

					org_coord = NavData.GetCurrentLocation().coord;
					mdist = NavCompute.DistancePtToPt(connect1.exit_center_coord,org_coord);
					cr_name = NavData.rd.name;
					NavData.LoadRoomdata(rm_name);
					connectors = NavData.GetCurrentRoomConnections();
					for (i = 0;i < connectors.Count;i++)
						{
						connect2 = (NavData.connection) connectors[i];
						if (connect2.name == cr_name)
							break;
						}
					oconnector = connect1;
					nconnector = connect2;
					if (connect1.direction == 90)
						{
						nr_coord.X = coord.X - connect1.exit_center_coord.X;
						nr_coord.Y = coord.Y + connect2.exit_center_coord.Y - connect1.exit_center_coord.Y;
						ox_coord.X = connect1.exit_center_coord.X;
						ox_coord.Y = org_coord.Y + (((coord.X - connect1.exit_center_coord.X) / (coord.X - org_coord.X)) * (coord.Y - org_coord.Y));
						x_coord.X = 0;
						x_coord.Y = ox_coord.Y + connect2.exit_center_coord.Y - connect1.exit_center_coord.Y;
						}
					else if (connect1.direction == 270)
						{
						nr_coord.X = connect2.exit_center_coord.X + coord.X;
						nr_coord.Y = connect2.exit_center_coord.Y + coord.Y - connect1.exit_center_coord.Y;
						ox_coord.X = 0;
						ox_coord.Y = org_coord.Y + (((coord.X - connect1.exit_center_coord.X) / (coord.X - org_coord.X)) * (coord.Y - org_coord.Y));
						x_coord.X = connect2.exit_center_coord.X;
						x_coord.Y = ox_coord.Y + connect2.exit_center_coord.Y - connect1.exit_center_coord.Y;
						}
					else if (connect1.direction == 0)
						{

						}
					else if (connect1.direction == 180)
						{

						}
					}
				}
			Log.LogEntry("org coord: " + org_coord);
			Log.LogEntry("new room: " + rm_name + "," + nr_coord + "  [old room coord: " + coord + "]");
			Log.LogEntry("new room cross coord: " + x_coord);
			return (rm_name);
		}

		}
	}
