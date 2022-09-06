using System;
using System.IO;
using System.Collections;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace AutoRobotControl
	{

	public class SensorFusion
		{

		public const string PARAM_FILE = "sf.param";

		private double ness_threshold = .015;
		private int allowed_mag_angle_dif = 30;


		public SensorFusion()

		{
			string fname;
			TextReader tr;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				allowed_mag_angle_dif = int.Parse(tr.ReadLine());
				ness_threshold = double.Parse(tr.ReadLine());
				tr.Close();
				}
		}



		public bool WithInMagLimit(int angle,int mad)

		{
			bool rtn = false;

			if (NavCompute.AngularDistance(angle,mad) <= allowed_mag_angle_dif)
				rtn = true;
			return(rtn);
		}



		private bool KCheckMag(ref int torient,ref int twd,int orient,int mad,int wd,int pangle)

		{
			bool rtn = false;
			int worient,wwd;

			if (WithInMagLimit(orient, mad))
				{
				torient = orient;
				twd = wd;
				rtn = true;
				}
			else if (pangle < 0)
				{
				worient = (orient + 90) % 360;
				wwd = (wd + 90) % 360;
				if (WithInMagLimit(worient,mad))
					{
					torient = worient;
					twd = wwd;
					rtn = true;
					}
				else
					{
					torient = -1;
					twd = -1;
					}
				}
			else
				{
				worient = (orient + 270) %360;
				wwd = (wd + 270) % 360;
				if (WithInMagLimit(worient,mad))
					{
					torient = worient;
					twd = wwd;
					rtn = true;
					}
				else
					{
					torient = -1;
					twd = -1;
					}
				}
			return(rtn);
		}



		public bool KinectFindEdge(ref short[] depthdata,ref int ra, ref int dist,ref int orient,int offset,bool high_col)

		{
			bool rtn = false;
			int pa = 0, ldist = 0, mad, wd, lorient, torient = 0, twd = 0;
			double lnsee = 0;

			mad = NavCompute.DetermineDirection(HeadAssembly.GetMagneticHeading());
			if ((mad > 315) || (mad <= 45))
				wd = 0;
			else if ((mad > 45) && (mad <= 135))
				wd = 90;
			else if ((mad > 135) && (mad <= 225))
				wd = 180;
			else
				wd = 270;
			if (Kinect.FindDistDirectPerpToWall(ref depthdata,ref pa,ref lnsee,ref ldist,offset,20))
				{
				Log.LogEntry("Wall found with pa - " + pa + "°   nese - " + lnsee.ToString("F4") + "   distance - " + ldist + " in.");
				if (lnsee < ness_threshold)
					{
					lorient = pa + wd - (HeadAssembly.CurrentHeadAngle() - HeadAssembly.HA_CENTER_ANGLE);
					if (KCheckMag(ref torient, ref twd, lorient, mad, wd,pa))
						{
						if (Kinect.FindEdge(ref depthdata,pa,ref ra,ref dist,offset,high_col))
							{
							Log.LogEntry("Edge located at relative angle " + ra + "°   distance " + dist + " in.");
							orient = torient;
							rtn = true;
							}
						else
							Log.LogEntry("Could not locate edge.");
						}
					else
						Log.LogEntry("Difference from mag. estimated direction exceeds limit, can not find wall.");
					}
				else
					Log.LogEntry("NESE exceeds allowed limit, can not find wall.");
				}
			else
				Log.LogEntry("Could not find wall.");
			return(rtn);
		}



		public bool LidarFindPerpToWall(ref ArrayList sdata,int center_shift_angle,int shift_angle,int wall_direct,ref Room.feature_match fm)
		
		{
			bool rtn = false;
			int pa = 0, wdist = 0,mh,mad;
			double nsee = 0;

			Rplidar.FindDistAnglePerpToWall(ref sdata,ref pa,ref nsee,ref wdist,center_shift_angle,shift_angle,ref fm.pw);
			if (nsee <= ness_threshold)
				{
				fm.orient = (wall_direct - shift_angle + pa) % 360;
				if (fm.orient < 0)
					fm.orient += 360;
				mh = HeadAssembly.GetMagneticHeading();
				mad = NavCompute.DetermineDirection(mh);
				if (WithInMagLimit(fm.orient,mad))
					{
					fm.matched = true;
					fm.distance = wdist;
					Log.LogEntry("Wall perp. found with LIDAR: orientation " + fm.orient + "°, distance " + fm.distance + " in.");
					rtn = true;
					}
				else
					Log.LogEntry("Wall perp. found with LIDAR does not appear to be correct: calc. orientation " + fm.orient + "°, magnetic heading " + mad + "°");
				}
			else
				Log.LogEntry("Wall perp. found with LIDAR does not appear to be correct: NESE " + nsee.ToString("F4"));
			return(rtn);
		}



		public bool KinectFindPerpToWall(int offset,int min_angle,int wall_direct,ref Room.feature_match fm)
		
		{
			bool rtn = false;
			int pa = 0, wdist = 0,mh,mad;
			double nsee = 0;

			if (Kinect.FindDistDirectPerpToWall(ref pa, ref nsee, ref wdist, offset,min_angle) && (nsee <= ness_threshold))
				{
				fm.orient = (wall_direct + pa - (HeadAssembly.CurrentHeadAngle() - HeadAssembly.HA_CENTER_ANGLE)) % 360;
				if (fm.orient < 0)
					fm.orient += 360;
				fm.ra = pa;
				mh = HeadAssembly.GetMagneticHeading();
				mad = NavCompute.DetermineDirection(mh);
				if (WithInMagLimit(fm.orient,mad))
					{
					fm.matched = true;
					fm.distance = wdist;
					Log.LogEntry("Wall perp. found with Kinect: orientation " + fm.orient + "°, distance " + fm.distance + " in.");
					rtn = true;
					}
				else
					Log.LogEntry("Wall perp. found with Kinect does not appear to be correct: calc. orientation " + fm.orient + "°, magnetic heading " + mad + "°");
				}
			else
				Log.LogEntry("Wall perp. found with Kinect does not appear to be correct: NESE " + nsee.ToString("F4"));
			return(rtn);
		}



		public double GetNeseThreshold()

		{
			return(ness_threshold);
		}


		}
	}
