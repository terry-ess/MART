using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AutoRobotControl
	{
	public static class MotionMeasureProb
		{

		public const string PARAM_FILE = "mmp.param";

		private const int NO_SAMPLES = 5000;
		private const double ELLIPLSE_FACTOR = 1;

		public enum LocType {COMPLETE,USER,SINGLE_PT};

		public struct Pose
		{
		public Point coord;
		public int orient;

		public Pose(int x,int y,int o)

		{
			coord = new Point();
			coord.X = x;
			coord.Y = y;
			orient = o;
		}

		public Pose(Point pt,int o)

		{
			coord = new Point();
			coord.X = pt.X;
			coord.Y = pt.Y;
			orient = o;
		}

		public override string ToString()

		{
			string rtn = "";
			rtn = "{" + coord.X + " " + coord.Y + "}  " + orient;
			return (rtn);
		}

		};
		
		public struct Ellipse
		{
			public Point center;
			public double rx;
			public double ry;

		public override string ToString()

		{
			string rtn = "";
			rtn = "{" + center.X + "," + center.Y + "}  " + rx.ToString("F2") + ", " + ry.ToString("F2");
			return (rtn);
		}

		};
		
		public struct PDF
		{
			public Ellipse elpse;
			public Pose[] data_pts;
			public Point[] pts;
			public Pose epose;
			public double orient_var;
		};
				
		public struct MoveErrorParam
		{
			public int rot_slow_limit;
			public double p_slow_rot;
			public double p_rot;
			public double p_dist;
			public double drift_var;
		};
		
		public struct CompLocParam
		{
			public double orient_var;
			public double loc_var;
		};

		public struct WOLocParam
		{
			public double orient_var;
			public double x_loc_var;
			public double y_loc_var;
		};


		public struct ConnectorLocParam
		{
			public double orient_var;
			public double p_dist;
		};



		public struct Limits
		{
			public int max_dist;
			public int min_dist;
			public int max_ra;
			public int min_ra;
		};

		public struct CartisianDistLimits
		{
			public int max_dist;
			public int min_dist;
		};

		private static PDF pdf;
		private static MoveErrorParam mep;
		private static CompLocParam clp;
		private static WOLocParam wolp;
		private static CompLocParam ulp;
		private static ConnectorLocParam cnlp;
		private static CompLocParam splp;
		private static Random rand = new Random();
		private static Stopwatch sw = new Stopwatch();


		static MotionMeasureProb()

		{
			string fname;
			TextReader tr;

			pdf.elpse = new Ellipse();
			pdf.pts = new Point[8];
			pdf.data_pts = new Pose[NO_SAMPLES];
			pdf.epose = new Pose();
			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);

				try
				{
				mep.rot_slow_limit  = int.Parse(tr.ReadLine());
				mep.p_slow_rot = double.Parse(tr.ReadLine());
				mep.p_rot = double.Parse(tr.ReadLine());
				mep.p_dist = double.Parse(tr.ReadLine());
				mep.drift_var = double.Parse(tr.ReadLine());
				clp.orient_var = double.Parse(tr.ReadLine());
				clp.loc_var = double.Parse(tr.ReadLine());
				wolp.orient_var = double.Parse(tr.ReadLine());
				wolp.x_loc_var = double.Parse(tr.ReadLine());
				wolp.y_loc_var = double.Parse(tr.ReadLine());
				ulp.orient_var = double.Parse(tr.ReadLine());
				ulp.loc_var = double.Parse(tr.ReadLine());
				cnlp.orient_var = double.Parse(tr.ReadLine());
				cnlp.p_dist = double.Parse(tr.ReadLine());
				splp.orient_var = double.Parse(tr.ReadLine());
				splp.loc_var = double.Parse(tr.ReadLine());
				}

				catch
				{
				mep.rot_slow_limit = 10;
				mep.p_slow_rot = .014;
				mep.p_rot = .026;
				mep.p_dist = .2;
				mep.drift_var = .2;
				clp.orient_var = 2;
				clp.loc_var = 2;
				wolp.orient_var = 2;
				wolp.x_loc_var = 2;
				wolp.y_loc_var = 2;
				ulp.orient_var = 11;
				ulp.loc_var = 25;
				cnlp.orient_var = 2;
				cnlp.p_dist = .15;
				splp.orient_var = 2;
				splp.loc_var = 10;
				}

				tr.Close();
				}
			else
				{
				mep.rot_slow_limit = 10;
				mep.p_slow_rot = .014;
				mep.p_rot = .026;
				mep.p_dist = .2;
				mep.drift_var = .2;
				clp.orient_var = 2;
				clp.loc_var = 2;
				wolp.orient_var = 2;
				wolp.x_loc_var = 2;
				wolp.y_loc_var = 2;
				ulp.orient_var = 11;
				ulp.loc_var = 25;
				cnlp.orient_var = 2;
				cnlp.p_dist = .15;
				splp.orient_var = 2;
				splp.loc_var = 10;
				}
		}



		private static bool PtInEllipse(Point pt,Ellipse e)

		{
			bool rtn = false;

			if (((Math.Pow(pt.X - e.center.X,2)/(e.rx * e.rx)) + (Math.Pow(pt.Y - e.center.Y,2)/(e.ry * e.ry))) < 1)
				rtn = true;
			return(rtn);
		}



		private static double Sample(double var)

		{
			double x1,x2,y;

			x1 = rand.NextDouble();
			x2 = rand.NextDouble();
			y = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
			return(y * Math.Sqrt(var));

/*			double b = Math.Sqrt(var);
			double result = 0.0;
			const int c_AggregationCount = 12;

			for (int i = 0; i < c_AggregationCount; i++)
				{
				result += rand.NextDouble() * 2 * b - b;
				}
			return 0.5 * result; */
		}



		private static void LocalizedPDF(LocType lt = LocType.COMPLETE)

		{
			int i;
			Pose pt = new Pose();
			double xmax = 0, ymax = 0, xmin, ymin;
			double loc_var,orient_var;

			xmin = pdf.epose.coord.X;
			ymin = pdf.epose.coord.Y;
			if (lt == LocType.COMPLETE)
				{
				loc_var = clp.loc_var;
				orient_var = clp.orient_var;
				}
			else if (lt == LocType.USER)
				{
				loc_var = ulp.loc_var;
				orient_var = ulp.orient_var;
				}
			else
				{
				loc_var = splp.loc_var;
				orient_var = splp.orient_var;
				}
			Log.LogEntry("orient variance: " + orient_var.ToString("F2") + "  location variance: " + loc_var.ToString("F2"));
			pdf.orient_var = orient_var;
			sw.Restart();
			for (i = 0; i < NO_SAMPLES; i++)
				{
				pt.coord.X = (int) Math.Round(pdf.epose.coord.X - Sample(loc_var));
				pt.coord.Y = (int) Math.Round(pdf.epose.coord.Y - Sample(loc_var));
				pt.orient = (int) Math.Round(pdf.epose.orient - Sample(orient_var));
				if (pt.orient > 360)
					pt.orient -= 360;
				else if (pt.orient < 0)
					pt.orient += 360;
				pdf.data_pts[i] = pt;
				if ((pt.coord.X - pdf.epose.coord.X) > xmax)
					xmax = pt.coord.X - pdf.epose.coord.X;
				else if ((pt.coord.X - pdf.epose.coord.X) < xmin)
					xmin = pt.coord.X - pdf.epose.coord.X;
				if ((pt.coord.Y - pdf.epose.coord.Y) > ymax)
					ymax = pt.coord.Y - pdf.epose.coord.Y;
				else if ((pt.coord.Y - pdf.epose.coord.Y) < ymin)
					ymin = pt.coord.Y - pdf.epose.coord.Y;
				}
			xmax = (xmax + Math.Abs(xmin)) / 2;
			ymax = (ymax + Math.Abs(ymin)) / 2;
			double a = xmax;
			double b = ymax;
			pdf.pts[0] = new Point(pdf.epose.coord.X, (int)Math.Round(pdf.epose.coord.Y + b));
			pdf.pts[1] = new Point((int)Math.Round(pdf.epose.coord.X - (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y + (.7071 * b)));
			pdf.pts[2] = new Point((int)Math.Round(pdf.epose.coord.X - a), pdf.epose.coord.Y);
			pdf.pts[3] = new Point((int)Math.Round(pdf.epose.coord.X - (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y - (.7071 * b)));
			pdf.pts[4] = new Point(pdf.epose.coord.X, (int)Math.Round(pdf.epose.coord.Y - b));
			pdf.pts[5] = new Point((int)Math.Round(pdf.epose.coord.X + (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y - (.7071 * b)));
			pdf.pts[6] = new Point((int)Math.Round(pdf.epose.coord.X + a), pdf.epose.coord.Y);
			pdf.pts[7] = new Point((int)Math.Round(pdf.epose.coord.X + (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y + (.7071 * b))); ;
			pdf.elpse.center = pdf.epose.coord;
			pdf.elpse.rx = (int)Math.Round(xmax);
			pdf.elpse.ry = (int)Math.Round(ymax);
			sw.Stop();
			Log.LogEntry("PF time (msec): " + sw.ElapsedMilliseconds);
			Log.LogEntry("ellipse: " + pdf.elpse.ToString());
		}



		private static void ConnectionPDF(int connect_direct,int connect_width,int dist)

		{
			double x_var, y_var, xmax = 0, ymax = 0, xmin, ymin;
			int i;
			Pose pt = new Pose();

			xmin = pdf.epose.coord.X;
			ymin = pdf.epose.coord.Y;
			if ((connect_direct == 90) || (connect_direct == 270))
				{
				x_var = dist * cnlp.p_dist;
				y_var = Math.Pow(((((double) connect_width - SharedData.ROBOT_WIDTH)/2)/4),2);  //var = 4 sigma of possible width space squared
				}
			else
				{
				y_var = dist * cnlp.p_dist;
				x_var = Math.Pow(((((double) connect_width - SharedData.ROBOT_WIDTH)/2)/4),2);
				}
			Log.LogEntry("orient variance: " + cnlp.orient_var.ToString("F2") + "  X variance: " + x_var.ToString("F2") + "  Y variance: " + y_var.ToString("F2"));
			pdf.orient_var = cnlp.orient_var;
			sw.Restart();
			for (i = 0; i < NO_SAMPLES; i++)
				{
				pt.coord.X = (int) Math.Round(pdf.epose.coord.X - Sample(x_var));
				pt.coord.Y = (int) Math.Round(pdf.epose.coord.Y - Sample(y_var));
				pt.orient = (int) Math.Round(pdf.epose.orient - Sample(cnlp.orient_var));
				if (pt.orient > 360)
					pt.orient -= 360;
				else if (pt.orient < 0)
					pt.orient += 360;
				pdf.data_pts[i] = pt;
				if ((pt.coord.X - pdf.epose.coord.X) > xmax)
					xmax = pt.coord.X - pdf.epose.coord.X;
				else if ((pt.coord.X - pdf.epose.coord.X) < xmin)
					xmin = pt.coord.X - pdf.epose.coord.X;
				if ((pt.coord.Y - pdf.epose.coord.Y) > ymax)
					ymax = pt.coord.Y - pdf.epose.coord.Y;
				else if ((pt.coord.Y - pdf.epose.coord.Y) < ymin)
					ymin = pt.coord.Y - pdf.epose.coord.Y;
				}
			xmax = (xmax + Math.Abs(xmin)) / 2;
			ymax = (ymax + Math.Abs(ymin)) / 2;
			double a = xmax;
			double b = ymax;
			pdf.pts[0] = new Point(pdf.epose.coord.X, (int)Math.Round(pdf.epose.coord.Y + b));
			pdf.pts[1] = new Point((int)Math.Round(pdf.epose.coord.X - (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y + (.7071 * b)));
			pdf.pts[2] = new Point((int)Math.Round(pdf.epose.coord.X - a), pdf.epose.coord.Y);
			pdf.pts[3] = new Point((int)Math.Round(pdf.epose.coord.X - (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y - (.7071 * b)));
			pdf.pts[4] = new Point(pdf.epose.coord.X, (int)Math.Round(pdf.epose.coord.Y - b));
			pdf.pts[5] = new Point((int)Math.Round(pdf.epose.coord.X + (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y - (.7071 * b)));
			pdf.pts[6] = new Point((int)Math.Round(pdf.epose.coord.X + a), pdf.epose.coord.Y);
			pdf.pts[7] = new Point((int)Math.Round(pdf.epose.coord.X + (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y + (.7071 * b))); ;
			pdf.elpse.center = pdf.epose.coord;
			pdf.elpse.rx = (int)Math.Round(xmax);
			pdf.elpse.ry = (int)Math.Round(ymax);
			sw.Stop();
			Log.LogEntry("PF time (msec): " + sw.ElapsedMilliseconds);
			Log.LogEntry("ellipse: " + pdf.elpse.ToString());
		}



		private static void WOLocalizedPDF(bool y_limited)
		
		{
			int i;
			Pose pt = new Pose();
			double xmax = 0, ymax = 0,xmin, ymin;
			
			xmin = pdf.epose.coord.X;
			ymin = pdf.epose.coord.Y;
			Log.LogEntry("orient variance: " + wolp.orient_var.ToString("F2") + "  X variance: " + wolp.x_loc_var.ToString("F2") + "  Y variance: " + wolp.y_loc_var.ToString("F2"));
			pdf.orient_var = wolp.orient_var;
			sw.Restart();
			for (i = 0; i < NO_SAMPLES; i++)
				{
				pt.orient = (int) Math.Round(pdf.epose.orient - Sample(wolp.orient_var));
				if (pt.orient > 360)
					pt.orient -= 360;
				else if (pt.orient < 0)
					pt.orient += 360;
				if (y_limited)
					{
					pt.coord.X = (int) Math.Round(pdf.epose.coord.X - Sample(wolp.x_loc_var));
					pt.coord.Y = pdf.data_pts[i].coord.Y;
					}
				else
					{
					pt.coord.X = pdf.data_pts[i].coord.X;
					pt.coord.Y = (int) Math.Round(pdf.epose.coord.Y - Sample(wolp.y_loc_var));
					}
				pdf.data_pts[i] = pt;
				if ((pt.coord.X - pdf.epose.coord.X) > xmax)
					xmax = pt.coord.X - pdf.epose.coord.X;
				else if ((pt.coord.X - pdf.epose.coord.X) < xmin)
					xmin = pt.coord.X - pdf.epose.coord.X;
				if ((pt.coord.Y - pdf.epose.coord.Y) > ymax)
					ymax = pt.coord.Y - pdf.epose.coord.Y;
				else if ((pt.coord.Y - pdf.epose.coord.Y) < ymin)
					ymin = pt.coord.Y - pdf.epose.coord.Y;
				}
			xmax = (xmax + Math.Abs(xmin)) / 2;
			ymax = (ymax + Math.Abs(ymin)) / 2;
			double a = xmax;
			double b = ymax;
			pdf.pts[0] = new Point(pdf.epose.coord.X, (int)Math.Round(pdf.epose.coord.Y + b));
			pdf.pts[1] = new Point((int)Math.Round(pdf.epose.coord.X - (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y + (.7071 * b)));
			pdf.pts[2] = new Point((int)Math.Round(pdf.epose.coord.X - a), pdf.epose.coord.Y);
			pdf.pts[3] = new Point((int)Math.Round(pdf.epose.coord.X - (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y - (.7071 * b)));
			pdf.pts[4] = new Point(pdf.epose.coord.X, (int)Math.Round(pdf.epose.coord.Y - b));
			pdf.pts[5] = new Point((int)Math.Round(pdf.epose.coord.X + (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y - (.7071 * b)));
			pdf.pts[6] = new Point((int)Math.Round(pdf.epose.coord.X + a), pdf.epose.coord.Y);
			pdf.pts[7] = new Point((int)Math.Round(pdf.epose.coord.X + (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y + (.7071 * b))); ;
			pdf.elpse.center = pdf.epose.coord;
			pdf.elpse.rx = a;
			pdf.elpse.ry = b;
			sw.Stop();
			Log.LogEntry("PF time (msec): " + sw.ElapsedMilliseconds);
			Log.LogEntry("ellipse: " + pdf.elpse.ToString());
		}



		private static void ConnectorMovePDF(Pose cross,int rotation,int distance, NavData.connection oconnector, NavData.connection nconnector)	//new side cross pose, origin side connector

		{
			double arot,adist,xmax = 0,ymax = 0,xmin,ymin,a,b;
			Pose pt = new Pose();
			int i,sumx = 0,sumy = 0;
			double rot_var,dist_var,con_var;

			pdf.epose = cross;
			if (Math.Abs(rotation) >= mep.rot_slow_limit)
				rot_var = Math.Abs(rotation * mep.p_rot);
			else
				rot_var = Math.Abs(rotation * mep.p_slow_rot);
			rot_var += mep.drift_var + clp.orient_var;
			dist_var = Math.Abs(distance * mep.p_dist);
			con_var = Math.Pow(((((double) oconnector.exit_width - SharedData.ROBOT_WIDTH) / 2) / 4), 2);  //var = 4 sigma of possible width space squared
			Log.LogEntry("rotation variance: " + rot_var.ToString("F2") + "  distance variance: " + dist_var.ToString("F2") + "  connector variance: " + con_var);
			pdf.orient_var = rot_var;
			xmin = pdf.epose.coord.X;
			ymin = pdf.epose.coord.Y;
			sw.Restart();
			for (i = 0; i < NO_SAMPLES; i++)
				{
				arot = (double) rotation - Sample(rot_var);
				adist = (double) distance - Sample(dist_var);
				pt.orient = (pdf.data_pts[i].orient - (int) Math.Round(arot)) % 360;
				if (pt.orient < 0)
					pt.orient += 360;
				if (oconnector.direction == 90)
					{
					pt.coord.X = (int) Math.Round(pdf.data_pts[i].coord.X + (adist * Math.Sin(pt.orient * SharedData.DEG_TO_RAD)) - oconnector.exit_center_coord.X);
					pt.coord.Y = (int) Math.Round(pdf.epose.coord.Y - Sample(con_var));
					}
				else if (oconnector.direction == 270)
					{
					pt.coord.X = (int) Math.Round(nconnector.exit_center_coord.X + pdf.data_pts[i].coord.X + (adist * Math.Sin(pt.orient * SharedData.DEG_TO_RAD)));
					pt.coord.Y = (int) Math.Round(pdf.epose.coord.Y - Sample(con_var));
					}
				else if (oconnector.direction == 0)
					{
					pt.coord.X = (int)Math.Round(pdf.epose.coord.X - Sample(con_var));

					}
				else if (oconnector.direction == 180)
					{
					pt.coord.X = (int)Math.Round(pdf.epose.coord.X - Sample(con_var));

					}
				pdf.data_pts[i] = pt;
				if ((pt.coord.X - pdf.epose.coord.X) > xmax)
					xmax = pt.coord.X - pdf.epose.coord.X;
				else if ((pt.coord.X - pdf.epose.coord.X ) < xmin)
					xmin = pt.coord.X - pdf.epose.coord.X;
				if ((pt.coord.Y - pdf.epose.coord.Y) > ymax)
					ymax = pt.coord.Y - pdf.epose.coord.Y;
				else if ((pt.coord.Y - pdf.epose.coord.Y) < ymin)
					ymin = pt.coord.Y - pdf.epose.coord.Y;
				sumx += pt.coord.X;
				sumy += pt.coord.Y;
				}
			Log.LogEntry("Computed mean location: {" + ((double) sumx/NO_SAMPLES).ToString("F2") + ", " + ((double) sumy/NO_SAMPLES).ToString("F2") + "}");
			xmax = (xmax + Math.Abs(xmin)) / 2;
			ymax = (ymax + Math.Abs(ymin))/2;
			a = ELLIPLSE_FACTOR * xmax;
			b = ELLIPLSE_FACTOR * ymax;
			pdf.pts[0] = new Point(pdf.epose.coord.X,(int) Math.Round(pdf.epose.coord.Y + b));
			pdf.pts[1] = new Point((int)Math.Round(pdf.epose.coord.X - (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y + (.7071 * b)));
			pdf.pts[2] = new Point((int)Math.Round(pdf.epose.coord.X - a), pdf.epose.coord.Y);
			pdf.pts[3] = new Point((int)Math.Round(pdf.epose.coord.X - (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y - (.7071 * b)));
			pdf.pts[4] = new Point(pdf.epose.coord.X, (int)Math.Round(pdf.epose.coord.Y - b));
			pdf.pts[5] = new Point((int)Math.Round(pdf.epose.coord.X + (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y - (.7071 * b)));
			pdf.pts[6] = new Point((int)Math.Round(pdf.epose.coord.X + a), pdf.epose.coord.Y);
			pdf.pts[7] = new Point((int)Math.Round(pdf.epose.coord.X + (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y + (.7071 * b))); ;
			pdf.elpse.center = pdf.epose.coord;
			pdf.elpse.rx = a;
			pdf.elpse.ry = b;
			sw.Stop();
			Log.LogEntry("PF time (msec): " + sw.ElapsedMilliseconds);
			Log.LogEntry("ellipse: " + pdf.elpse.ToString());
		}



		private static void MotionPDF(int rotation,int distance)

		{
			double arot,adist,xmax = 0,ymax = 0,xmin,ymin,a,b;
			Pose pt = new Pose();
			int i,sumx = 0,sumy = 0;
			double rot_var,dist_var;

			if (Math.Abs(rotation) >= mep.rot_slow_limit)
				rot_var = Math.Abs(rotation * mep.p_rot);
			else
				rot_var = Math.Abs(rotation * mep.p_slow_rot);
			rot_var += mep.drift_var + clp.orient_var;
			dist_var = Math.Abs(distance * mep.p_dist);
			Log.LogEntry("rotation variance: " + rot_var.ToString("F2") + "  distance variance: " + dist_var.ToString("F2"));
			pdf.orient_var = rot_var;
			xmin = pdf.epose.coord.X;
			ymin = pdf.epose.coord.Y;
			sw.Restart();
			for (i = 0; i < NO_SAMPLES; i++)
				{
				arot = (double) rotation - Sample(rot_var);
				adist = (double) distance - Sample(dist_var);
				pt.orient = (pdf.data_pts[i].orient - (int) Math.Round(arot)) % 360;
				if (pt.orient < 0)
					pt.orient += 360;
				pt.coord.X = (int) Math.Round(pdf.data_pts[i].coord.X + (adist * Math.Sin(pt.orient * SharedData.DEG_TO_RAD)));
				pt.coord.Y = (int) Math.Round(pdf.data_pts[i].coord.Y + (adist * -Math.Cos(pt.orient * SharedData.DEG_TO_RAD)));
				pdf.data_pts[i] = pt;
				if ((pt.coord.X - pdf.epose.coord.X) > xmax)
					xmax = pt.coord.X - pdf.epose.coord.X;
				else if ((pt.coord.X - pdf.epose.coord.X ) < xmin)
					xmin = pt.coord.X - pdf.epose.coord.X;
				if ((pt.coord.Y - pdf.epose.coord.Y) > ymax)
					ymax = pt.coord.Y - pdf.epose.coord.Y;
				else if ((pt.coord.Y - pdf.epose.coord.Y) < ymin)
					ymin = pt.coord.Y - pdf.epose.coord.Y;
				sumx += pt.coord.X;
				sumy += pt.coord.Y;
				}
			Log.LogEntry("Computed mean location: {" + ((double) sumx/NO_SAMPLES).ToString("F2") + ", " + ((double) sumy/NO_SAMPLES).ToString("F2") + "}");
			xmax = (xmax + Math.Abs(xmin)) / 2;
			ymax = (ymax + Math.Abs(ymin))/2;
			a = ELLIPLSE_FACTOR * xmax;
			b = ELLIPLSE_FACTOR * ymax;
			pdf.pts[0] = new Point(pdf.epose.coord.X,(int) Math.Round(pdf.epose.coord.Y + b));
			pdf.pts[1] = new Point((int)Math.Round(pdf.epose.coord.X - (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y + (.7071 * b)));
			pdf.pts[2] = new Point((int)Math.Round(pdf.epose.coord.X - a), pdf.epose.coord.Y);
			pdf.pts[3] = new Point((int)Math.Round(pdf.epose.coord.X - (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y - (.7071 * b)));
			pdf.pts[4] = new Point(pdf.epose.coord.X, (int)Math.Round(pdf.epose.coord.Y - b));
			pdf.pts[5] = new Point((int)Math.Round(pdf.epose.coord.X + (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y - (.7071 * b)));
			pdf.pts[6] = new Point((int)Math.Round(pdf.epose.coord.X + a), pdf.epose.coord.Y);
			pdf.pts[7] = new Point((int)Math.Round(pdf.epose.coord.X + (.7071 * a)), (int)Math.Round(pdf.epose.coord.Y + (.7071 * b))); ;
			pdf.elpse.center = pdf.epose.coord;
			pdf.elpse.rx = a;
			pdf.elpse.ry = b;
			sw.Stop();
			Log.LogEntry("PF time (msec): " + sw.ElapsedMilliseconds);
			Log.LogEntry("ellipse: " + pdf.elpse.ToString());
		}



		public static void Move(Pose expected)

		{
			NavCompute.pt_to_pt_data  ppd;
			int rotation,distance;

			Log.LogEntry("Move: " + expected.ToString());
			ppd = NavCompute.DetermineRaDirectDistPtToPt(expected.coord, pdf.epose.coord);
			rotation = pdf.epose.orient - expected.orient;
			if (rotation > 180)
				rotation -= 360;
			else if (rotation < -180)
				rotation += 360;
			pdf.epose = expected;
			if (NavCompute.AngularDistance(expected.orient,ppd.direc) > 90)
				distance = -ppd.dist;
			else
				distance = ppd.dist;
			Log.LogEntry("rotation: " + rotation + "  distance: " + distance);
			MotionPDF(rotation,distance);
		}



		public static void ConnectorMove(Pose ocross,Pose ncross, NavData.connection oconnector,NavData.connection nconnector)	
		//ocross = origin cross coord, ncross = new cross coord, oconnector = orign connector, nconnector = new connector

		{
			NavCompute.pt_to_pt_data  ppd;
			int rotation,distance;

			Log.LogEntry("ConnectorMove: " + ocross.ToString() + "   " + ncross.ToString());
			ppd = NavCompute.DetermineRaDirectDistPtToPt(ocross.coord, pdf.epose.coord);
			rotation = pdf.epose.orient - ocross.orient;
			if (rotation > 180)
				rotation -= 360;
			else if (rotation < -180)
				rotation += 360;
			if (NavCompute.AngularDistance(ncross.orient,ppd.direc) > 90)
				distance = -ppd.dist;
			else
				distance = ppd.dist;
			Log.LogEntry("rotation: " + rotation + "  distance: " + distance);
			ConnectorMovePDF(ncross,rotation,distance,oconnector,nconnector);
		}



		public static void CompleteLocalize(Pose current)

		{
			Log.LogEntry("CompleteLocalize: " + current.ToString());
			pdf.epose = current;
			LocalizedPDF();
		}



		public static void UserLocalize(Pose current)

		{
			Log.LogEntry("UserLocalize: " + current.ToString());
			pdf.epose = current;
			LocalizedPDF(LocType.USER);
		}



		public static void SinglePointLocalize(Pose current)

		{
			Log.LogEntry("SinglePointLocalize: " + current.ToString());
			pdf.epose = current;
			LocalizedPDF(LocType.SINGLE_PT);
		}



		public static void ConnectionLocalize(Pose current,int connect_dir,int connect_width,int dist)

		{
			Log.LogEntry("ConnectionLocalize: " + current.ToString() + ",  " + connect_dir + ",  " + connect_width + ",  " + dist);
			pdf.epose = current;
			ConnectionPDF(connect_dir,connect_width,dist);
		}



		public static void WallOnlyLocalize(Point current,Pose pse)

		{
			bool ylimit;
			if ((pse.coord.X == -1) ^ (pse.coord.Y == -1))
				{
				pdf.epose.orient = pse.orient;
				pdf.epose.coord = current;
				if (pse.coord.X == -1)
					ylimit = false;
				else
					ylimit = true;
				Log.LogEntry("WallOnlyLocalize: " + pse.ToString());
				WOLocalizedPDF(ylimit);
				}
			else
				Log.LogEntry("WallOnlyLocalize: One and only one location must be -1.");
		}



		public static bool InPdfEllipse(Point pt)

		{
			return(PtInEllipse(pt,pdf.elpse));
		}



		public static Limits PdfLimits(Point pt)

		{
			int i, max_dist = 0, min_dist = AutoRobotControl.Move.MAX_MOVE_SEG_DIST * 2;
			int max_ra = -180,min_ra = 180,ra;
			NavCompute.pt_to_pt_data ppd;
			Limits lmts = new Limits();

			for (i = 0; i < pdf.pts.Length; i++)
				{
				ppd = NavCompute.DetermineRaDirectDistPtToPt(pt,pdf.pts[i]);
				if (ppd.dist > max_dist)
					max_dist = ppd.dist;
				if (ppd.dist < min_dist)
					min_dist = ppd.dist;
				ra = NavCompute.AngularDistance(pdf.epose.orient,ppd.direc);
				if (NavCompute.ToRightDirect(pdf.epose.orient,ppd.direc))
					ra = -ra;
				if (ra > max_ra)
					max_ra = ra;
				if (ra < min_ra)
					min_ra = ra;
				}
			lmts.max_dist = max_dist;
			lmts.min_dist = min_dist;
			lmts.max_ra = max_ra;
			lmts.min_ra = min_ra;
			return(lmts);
		}



		public static CartisianDistLimits CartisianAbsDistLimit(Point pt,bool xcoord)

		{
			CartisianDistLimits limits;
			int max_dist = 0,min_dist,i,cdist;

			min_dist = NavCompute.DistancePtToPt(pdf.epose.coord,pt);
			for (i = 0; i < pdf.pts.Length; i++)
				{
				if (xcoord)
					cdist = Math.Abs(pt.X - pdf.pts[i].X);
				else
					cdist = Math.Abs(pt.Y - pdf.pts[i].Y);
				if (cdist > max_dist)
					max_dist = cdist;
				else if (cdist < min_dist)
					min_dist = cdist;
				}
			limits.max_dist = max_dist;
			limits.min_dist = min_dist;
			return(limits);
		}



		public static Rectangle PdfRectangle()

		{
			Rectangle rct = new Rectangle();
			
			rct.Height = (int)Math.Round(2 * pdf.elpse.ry);
			rct.Width = (int)Math.Round(2 * pdf.elpse.rx);
			rct.X = (int)Math.Round(pdf.elpse.center.X - pdf.elpse.rx);
			rct.Y = (int)Math.Round(pdf.elpse.center.Y - pdf.elpse.ry);
			return(rct);
		}



		public static double OrientationVariance()

		{
			return(pdf.orient_var);
		}



		public static double MoveDriftLimit()

		{
			return(Math.Sqrt(mep.drift_var) * 3);
		}


		public static void Init(Pose current,bool user)

		{
			LocType lt;

			pdf.epose = current;
			if (user)
				lt = LocType.USER;
			else
				lt = LocType.COMPLETE;
			LocalizedPDF(lt);
			Log.LogEntry("Init: " + current.ToString() + ", " + user);
		}

		}
	}
