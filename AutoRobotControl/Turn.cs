using System;
using System.Collections;
using System.Threading;
using System.Drawing;
using System.Windows.Forms;


namespace AutoRobotControl
	{

	public static class Turn
		{

		private const int HEADING_TURN_LIMIT = 5;
		private const int HEADING_TURN_TRYS = 4;
		private const double MIN_TURN_CLEARENCE = .5;


		public struct TurnBlock
		{
			public Point min;
			public Point max;
		};

		public struct TurnLimit
		{
			public int greaterthen;
			public int lessthen;
		};

		private static SharedData.MotionErrorType last_turn_error = SharedData.MotionErrorType.NONE;
		


		private static string SendCommand(string command,int timeout_count)

		{
			string rtn = "";

			if (timeout_count < 20)
				timeout_count = 20;
			Log.LogEntry(command);
			rtn = MotionControl.SendCommand(command,timeout_count);
			Log.LogEntry(rtn);
			return(rtn);
		}



		public static bool TurnToHeading(int dheading,ref int turn_angle)

		{
			bool rtn = false;
			int tangle;
			int cheading;
			bool right;
			int rcheading = -1, rdheading = -1;

			Log.LogEntry("Starting turn to heading : " + dheading.ToString());
			if (HeadAssembly.Connected() && MotionControl.Connected())
				{
				try
				{
				cheading = (int) HeadAssembly.GetMagneticHeading();
				if (cheading > 359)
					cheading = 0;
				rcheading = NavCompute.DetermineDirection(cheading);
				rdheading = NavCompute.DetermineDirection(dheading);
				Log.LogEntry("TurnToHeading look up: cheading - " + cheading.ToString("F1") + "  rcheading - " + rcheading.ToString() + "  dheading - " + dheading.ToString("F1") + "  rdheading - " + rdheading.ToString());
				if ((rcheading > -1) && (rdheading > -1) && (Math.Abs(rcheading - rdheading) > HEADING_TURN_LIMIT))
					{
					if (rdheading < rcheading)
						{
						right = false;
						tangle = (int) (rcheading - rdheading);
						}
					else
						{
						right = true;
						tangle = (int) (rdheading - rcheading);
						}
					if (tangle > 180)
						{
						tangle = 360 - tangle;
						if (right)
							right = false;
						else
							right = true;
						}
					if (right)
						tangle *= -1;
					rtn = TurnAngle(tangle);
					if (rtn)
						turn_angle = tangle;
					}
				}

				catch(Exception ex)
				{
				Log.LogEntry("Turn to heading exception: " + ex.Message);
				Log.LogEntry("                stack trace: " + ex.StackTrace);
				rtn = false;
				}

				}
			else
				Log.LogEntry("HeadAssembly or MotionControl not connected.");
			return(rtn);
		}




		public static bool TurnToDirection(int current_dir,int desired_direc)

		{
			bool rtn = false;
			int tangle = 0;

			tangle = current_dir - desired_direc;
			if (tangle > 180)
				tangle -= 360;
			else if (tangle < -180)
				tangle += 360;
			rtn = TurnAngle(tangle);
			return(rtn);
		}


		
		public static bool TurnAngle(int angle)

		{
			bool rtn = false;
			string rsp,command;
			int timeout;
			int modd = 0,mod = 0;

			Log.LogEntry("TurnAngle: " + angle.ToString());
			if (Math.Abs(angle) >= SharedData.MIN_TURN_ANGLE)
				{
				if (TurnSafe(angle,ref mod,ref modd))
					{
					timeout = (int) (((double) Math.Abs(angle)/180) * 5000) + 1000;
					if (angle < 0)
						command = SharedData.RIGHT_TURN + " " + Math.Abs(angle).ToString();
					else
						command = SharedData.LEFT_TURN + " " + Math.Abs(angle).ToString();
					rsp = SendCommand(command,timeout);
					if (rsp.StartsWith("ok"))
						{
						rtn = true;
						last_turn_error = SharedData.MotionErrorType.NONE;
						}
					else
						{
						if (rsp.Contains(SharedData.MPU_FAIL))
							last_turn_error = SharedData.MotionErrorType.MPU;
						else if (rsp.Contains(SharedData.START_TIMEOUT))
							last_turn_error = SharedData.MotionErrorType.START_TIMEOUT;
						else if (rsp.Contains(SharedData.STOP_TIMEOUT))
							last_turn_error = SharedData.MotionErrorType.STOP_TIMEOUT;
						else
							last_turn_error = SharedData.MotionErrorType.INIT_FAIL;
						}
					}
				else
					last_turn_error = SharedData.MotionErrorType.TURN_NOT_SAFE;
				}
			else
				rtn = true;
			return(rtn);
		}



		public static SharedData.MotionErrorType LastError()

		{
			return(last_turn_error);
		}



		private static Rplidar.scan_data Map(Lidar.rcscan_data sd,double offset)

		{
			Rplidar.scan_data msd = new Rplidar.scan_data();
			double from_x,from_y;
			int cangle = -1;
			int dangle = 0;

			from_x = 0;
			from_y = offset;
			if (Math.Abs(sd.y - from_y) < Math.Abs(sd.x - from_x))
				{
				cangle = (int) Math.Round(Math.Atan(((double) Math.Abs(sd.y - from_y)) / Math.Abs(sd.x - from_x)) * SharedData.RAD_TO_DEG);
				if (from_x > sd.x)
					{
					if (from_y < sd.y)
						dangle = 270 + cangle;
					else
						dangle = 270 - cangle;
					}
				else
					{
					if (from_y < sd.y)
						dangle = 90 - cangle;
					else
						dangle = 90 + cangle;
					}
				}
			else
				{
				cangle = (int) (Math.Atan(((double) Math.Abs(sd.x - from_x)) / Math.Abs(sd.y - from_y)) * SharedData.RAD_TO_DEG);
				if (from_y < sd.y)
					{
					if (from_x < sd.x)
						dangle = cangle;
					else
						dangle = 360 - cangle;
					}
				else
					{
					if (from_x < sd.x)
						{
						dangle = 180 - cangle;
						}
					else
						dangle = 180 + cangle;
					}
				}
			msd.dist = Math.Sqrt(Math.Pow(sd.x - from_x, 2) + Math.Pow(sd.y - from_y, 2));
			msd.angle = (ushort) dangle;
			return(msd);
		}



		public static bool TurnSafe(ref ArrayList lscan, int tangle, ref int min_obs_dist, ref int mod_direc)

			{
			Lidar.rcscan_data sd;
			Rplidar.scan_data rd = new Rplidar.scan_data();;
			int atangle,a,angle = 0,i = 0;
			string[] data = new string[2];
			double x = 0, y = 0, rad, dx, dy,width1,width2,width3;
			bool obs_found = false;

			tangle *= -1;  //TANGLE: RIGHT IS < 0 NOT > 0
			atangle = Math.Abs(tangle);												
			width1 = ((double) SharedData.ROBOT_WIDTH/2);
			width2 = ((double) SharedData.ROBOT_CORE_WIDTH/2);
			width3 = ((double) SharedData.PROBE_WIDTH/2);
			for (a = 1;a < atangle;a++)
				{
				if (tangle < 0)
					angle = -a;
				else
					angle = a;
				rad = angle * SharedData.DEG_TO_RAD;
				dx = SharedData.FRONT_PIVOT_PT_OFFSET * Math.Sin(rad);
				dy = SharedData.FRONT_PIVOT_PT_OFFSET * Math.Cos(rad) - SharedData.FRONT_PIVOT_PT_OFFSET;
				for (i = 0; i < lscan.Count; i++)
					{
					sd = (Lidar.rcscan_data) lscan[i];
					x = (sd.x * Math.Cos(rad)) - (sd.y * Math.Sin(rad)) - dx;
					y = (sd.x * Math.Sin(rad)) + (sd.y * Math.Cos(rad)) + dy;
					if (((Math.Abs(x) < width1) && (y <= 0) && (y >= -SharedData.ROBOT_WHEEL_LENGTH)) ||
						 ((Math.Abs(x) < width2) && (y < -SharedData.ROBOT_WHEEL_LENGTH) && (y >= -SharedData.ROBOT_LENGTH)) ||
						 ((Math.Abs(x) < width3) && (y > 0) && (y < SharedData.PROBE_LENGTH)))
						{
						obs_found = true;
						rd = Map(sd,0);
						break;
						}
					}
				if (obs_found)
					break;
				}
			if (obs_found)
				{
				min_obs_dist = (int) Math.Ceiling(rd.dist);
				mod_direc = rd.angle;
				Log.LogEntry("TurnSafe: obstacle found");
				data[1] = "TurnSafe turn angle: " + tangle + "°\r\nObstacle found at angle " + angle + "°  index " + i + "  distance " + rd.dist.ToString("F2") + "  direction " + rd.angle;
				Lidar.SaveLidarRCScan(ref lscan,data);
				}
			else
				{
				Log.LogEntry("TurnSafe: No obstacle found");
				data[1] = "TurnSafe turn angle: " + tangle + "°\r\nNo obstacle found";
				Lidar.SaveLidarRCScan(ref lscan,data);
				}
			return (!obs_found);
		}



		public static bool TurnSafe(int tangle, ref int min_obs_dist, ref int mod_direc)

		{
			bool rtn = false;
			ArrayList lscan = new ArrayList();

			if (Lidar.CaptureRCScan(ref lscan))
				{
				rtn =TurnSafe(ref lscan,tangle,ref min_obs_dist,ref mod_direc);
				}
			else
				Log.LogEntry("TurnSafe: Could not capture LIDAR scan.");
			return(rtn);
		}



		public static bool TurnSafeMulti(int tangle)

		{
			ArrayList lscan = new ArrayList();
			int tries = 0,scan_fail = 0,mod = 0,modd = 0;
			bool ob_found,rtn = false;
			const int TRY_LIMIT = 2;
			string data = "";

			do
				{
				ob_found = false;
				if (Lidar.CaptureRCScan(ref lscan))
					{
					ob_found = !TurnSafe(tangle,ref mod,ref modd);
					}
				else
					{
					Log.LogEntry("Attempt to capture Lidar RC scan failed.");
					scan_fail += 1;
					}
				tries += 1;
				}
			while (ob_found && (tries < TRY_LIMIT));
			if (ob_found)
				{
				Log.LogEntry("TurnSafeMulti: obstacles found in " + tries + " scans");
				Lidar.SaveLidarRCScan(ref lscan,data);
				}
			else if (scan_fail == tries)
				Log.LogEntry("TurnSafeMulti: could not capture a Lidar RC scan.");
			else
				{
				Log.LogEntry("TurnSafeMulti: No obstacle found");
				Lidar.SaveLidarRCScan(ref lscan,data);
				rtn = true;
				}
			return (rtn);
		}



		public static Point TurnImpactPosition(int angle)

		{
			Point pt = new Point();

			angle = -angle;
			pt.X = (int) Math.Round(SharedData.FRONT_PIVOT_PT_OFFSET * Math.Sin(angle * SharedData.DEG_TO_RAD));
			pt.Y = (int) Math.Round(SharedData.FRONT_PIVOT_PT_OFFSET - (SharedData.FRONT_PIVOT_PT_OFFSET * Math.Cos(angle * SharedData.DEG_TO_RAD)));
			return(pt);
		}



		public static void Stop()

		{
//			run = false;
		}


		private static string SendCommand(string command)

		{
			string rsp = "";

			Log.LogEntry(command);
			rsp = MotionControl.SendCommand(command,200);
			Log.LogEntry(rsp);
			return(rsp);
		}


		
		}
	}
