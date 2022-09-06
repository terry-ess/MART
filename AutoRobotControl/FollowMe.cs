using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using AutoRobotControl;
using Microsoft.Kinect;
using MathNet.Numerics.LinearAlgebra.Double;


namespace AutoRobotControl
	{
	class FollowMe
		{

		private const string PID_PARAM_FILE = "fmpid";
		private const string MOTION_PARAM_FILE = "NORMALtlm.param";
		private const int CORRECT_DELAY = 200;
		private const int MAINTAINED_DISTANCE = (int)(3.5 * 12);
		private const double BASE_STOP_TIME = .117;
		private const int SPEAKER_TS_DIF = 60000;
		private const int START_SPEED = 30;
		private const double START_TOP_SPEED = 6.7;
		private const int MAX_LINEAR_SPEED = 100;
		private const double SPEED_CONVERT = .02257;
		private const string PERSON_LOST = "person lost";
		private const string NO_CLEAR_PATH = "No clear path found.";
		private const int LOC_TIME = 225;
		private const double SCAN_DELAY = .4;
		private const int MIN_CLEAR = 20;
		private const int SCAN_TIME = 500;


		private enum linear_directions { NONE, FORWARD, BACKWARD };
		private enum turn_directions { NONE, RIGHT, LEFT };

		private struct motion_cntl
		{
		public linear_directions ldirec;
		public int lspeed;
		public turn_directions tdirec;
		public int tspeed;
		};

		private struct motor_cntl
		{
		public int rnow;
		public int lnow;
		};



		private bool initialized;
		private PersonDetect pd = new PersonDetect();
		private Move mov = new Move();
		private string error = "";
		private bool stop_follow_me = false;
		private motion_cntl mc;
		private int lgain, tgain;
		private motor_cntl mp;
		private ArrayList ts = new ArrayList();
		private ArrayList pdist = new ArrayList();
		private ArrayList pangle = new ArrayList();
		private ArrayList dist = new ArrayList();
		private ArrayList rangle = new ArrayList();
		private ArrayList lspeed = new ArrayList();
		private ArrayList tspeed = new ArrayList();
		private ArrayList rms = new ArrayList();
		private ArrayList lms = new ArrayList();
		private ArrayList locatn = new ArrayList();
		private int mtr_setting;
		private double vs_intercept,vs_slope,rel_angle;
		private DepthImagePixel[] depthdata = new DepthImagePixel[Kinect.nui.DepthStream.FramePixelDataLength];
		private SkeletonPoint[] sips = new SkeletonPoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];
		private NavData.location current = new NavData.location();
		private string target_new_room = "";
		private int last_heading;
		private Point last_target_loc;
		private NavData.connection connect;
		private Location floc = new Location();
		private double volt_factor;


		private static Stopwatch sw = new Stopwatch();
		private static bool waiting_localization = false;
		private static bool localization_avail = true;
		private static Thread lthread;
		private static MotionMeasureProb.Pose pose;
		private static double correction = 0;
		private static int localize_time;


		public bool Open()

		{
			initialized = false;
			if (SharedData.head_assembly_operational && SharedData.kinect_operational && SharedData.front_lidar_operational
				&& SharedData.motion_controller_operational && ReadRefParameters())
				{
				if ((SpeakerData.Person.detected) && (SpeakerData.Person.ts > 0) && ((SharedData.app_time.ElapsedMilliseconds - SpeakerData.Person.ts) < SPEAKER_TS_DIF))
					{
					if (InTheClear())
						initialized = true;
					else
						OutputSpeech("Can not run follow me.  I am not in a clear area.");
					}
				else
					OutputSpeech("Can not run follow me.  No current speaker data.");
				}
			else
				OutputSpeech("Can not run follow me.  The necessary resources are not available.");
			return (initialized);
		}



		private bool InTheClear()

		{
			bool rtn = true;
			ArrayList lscan = new ArrayList();
			int i;
			Lidar.rcscan_data sd;
			double dist;

			if (Lidar.CaptureRCScan(ref lscan))
				{
				for (i = 0; i < lscan.Count; i++)
					{
					sd = (Lidar.rcscan_data) lscan[i];
					dist = Math.Sqrt((sd.x * sd.x) + (sd.y * sd.y));
					if (dist < SharedData.REAR_TURN_RADIUS)
						{
						rtn = false;
						break;
						}
					}
				}
			else
				rtn = false;
			return (rtn);
		}



		private void OutputSpeech(string output)

		{
			Speech.Speak(output);
			Log.LogEntry(output);
		}


		private string SendCommand(string command,int timeout)

		{
			string rtn = "";

			rtn = AutoRobotControl.MotionControl.SendCommand(command,timeout/10);
			Log.LogEntry(command + "," + rtn);
			return(rtn);
		}



		private bool Turn(int angle,ref string error)

		{
			string rsp;
			bool rtn = false;

			if (angle < 0)
				rsp = SendCommand(SharedData.RIGHT_TURN +  " " + Math.Abs(angle),2000);
			else
				rsp = SendCommand(SharedData.LEFT_TURN + " " + angle,2000);
			if (rsp.StartsWith("ok"))
				rtn = true;
			else
				error = rsp.Substring(4);
			return(rtn);
		}



		private bool TurnToFacePerson(int initial_turn,ref int turned)

		{
			bool rtn = false,turn_failed = false;
			PersonDetect.scan_data pdd = new PersonDetect.scan_data();
			string rsp = "";
			int turn = 0;

			if (Turn(initial_turn,ref rsp))
				{
				turn = -initial_turn;
				if (pd.NearestHCLPerson(false, ref pdd))
					{
					while (Math.Abs(pdd.angle) > 2)
						{
						rsp = "";
						if (Turn((int)pdd.angle, ref rsp))
							{
							turn -= (int) pdd.angle;
							if (!pd.NearestHCLPerson(false, ref pdd))
								{
								error = "Turn to person failed, person lost";
								turn_failed = true;
								Log.LogEntry(error);
								break;
								}
							}
						else
							{
							if (rsp.Length > 0)
								{
								error = "Turn to person failed with: " + rsp;
								Log.LogEntry(error);
								}
							else
								{
								error = "Turn to person failed with command error";
								Log.LogEntry(error);
								}
							turn_failed = true;
							break;
							}
						}
					if (!turn_failed)
						{
						rtn = true;
						turned = turn;
						}
					}
				else
					{
					error = "Turn to person failed, person not found";
					Log.LogEntry(error);
					}
				}
			else
				{
				error = "Turn to person failed with: " + rsp;
				Log.LogEntry(error);
				}
			return (rtn);
		}



		private void StopHandler(string msg)

		{
			stop_follow_me = true;
			SendCommand(SharedData.REF_MOVE_STOP, 1000);
			Log.LogEntry("User stop issued.");
		}



		private bool TurnLogic(int ra)

		{	//tspeed is the combined speed setting, the motion controller speed setting for each motor is tspeed/2 with one motor positive and the other negative
			bool rtn = true;
			int correction;

			if (Math.Abs(ra) < 1)
				{
				mc.tdirec = turn_directions.NONE;
				mc.tspeed = 0;
				}
			else
				{
				correction = (int) (tgain * ra);
				if (ra > 0)
					mc.tdirec = turn_directions.RIGHT;
				else
					mc.tdirec = turn_directions.LEFT;
				if (Math.Abs(correction) > 240)
					mc.tspeed = 240;
				else
					mc.tspeed = Math.Abs(correction);
				}
			return(rtn);
		}



		private bool LinearLogic(int dist)

		{	//lspeed is the motion controller speed setting for each motor
			bool rtn = true;
			int sdist,correction;
			double stop_dist;

			stop_dist = (((mc.lspeed/START_SPEED) * START_TOP_SPEED)/2)*((mc.lspeed/START_SPEED) * BASE_STOP_TIME);
			if (dist <= ComeHere.PERSON_CLEARANCE + stop_dist)
				{
				mc.ldirec = linear_directions.NONE;
				mc.lspeed = 0;
				}
			else
				{
				sdist = dist - MAINTAINED_DISTANCE;
				correction = (int) (lgain * sdist);
				if (correction > 100)
					correction = 100;
				else  if (correction < 5)
					correction = 0;
				mc.lspeed = correction;
				mc.ldirec = linear_directions.FORWARD;
				}
			return(rtn);
		}



		private bool SetMotors()

		{
			bool rtn = true;
			string msg,rsp;
			motor_cntl last_mp;
			int right_delta,left_delta;

			last_mp = mp;
			if ((mc.lspeed > 120) || (mc.tspeed > 240))
				{
				rtn = false;
				error = "Set motors failed due to a bad parameter.";
				Log.LogEntry(error);
				}
			else if (mc.tspeed > 120)
				{
				mp.rnow = mp.lnow = 128;
				if (mc.tdirec == turn_directions.RIGHT)
					{
					mp.rnow += mc.tspeed/2;
					mp.lnow -= mc.tspeed/2;
					}
				else
					{
					mp.rnow -= mc.tspeed / 2;
					mp.lnow += mc.tspeed / 2;
					}
				}
			else
				{
				if (mc.ldirec == linear_directions.FORWARD)
					mp.rnow = mp.lnow = 128 - mc.lspeed;
				else if ((mc.ldirec == linear_directions.NONE) || (mc.lspeed == 0))
					{
					mp.rnow = mp.lnow = 128;
					mc.ldirec = linear_directions.NONE;
					mc.lspeed = 0;
					}
				if (mc.tdirec == turn_directions.RIGHT)
					{
					if (mc.ldirec == linear_directions.FORWARD)
						{
						mp.lnow -= mc.tspeed;
						if (mp.lnow < 0)
							{
							mp.rnow += Math.Abs(mp.lnow);
							mp.lnow = 0;
							}
						}
					else
						{
						mp.rnow += mc.tspeed/2;
						mp.lnow -= mc.tspeed/2;
						}
					}
				else if (mc.tdirec == turn_directions.LEFT)
					{
					if (mc.ldirec == linear_directions.FORWARD)
						{
						mp.rnow -= mc.tspeed;
						if (mp.rnow < 0)
							{
							mp.lnow += Math.Abs(mp.rnow);
							mp.rnow = 0;
							}
						}
					else
						{
						mp.rnow -= mc.tspeed/2;
						mp.lnow += mc.tspeed/2;
						}
					}
				}
			if (rtn)
				{
				right_delta = last_mp.rnow - mp.rnow;
				left_delta = last_mp.lnow - mp.lnow;
				if ((right_delta != 0) || (left_delta != 0))
					{
					msg = SharedData.REF_CHG_SPEED + " " + right_delta.ToString() + " " + left_delta.ToString();
					rsp = AutoRobotControl.MotionControl.SendCommand(msg, 200);
					if (rsp.StartsWith("fail"))
						{
						SendCommand(SharedData.REF_MOVE_STOP, 1000);
						rtn = false;
						error = "Follow Person failed, CS command failed.";
						Log.LogEntry(error);
						}
					}
				}
			return (rtn);
		}



		private bool RelativeAngle()

		{
			string rsp;
			bool rtn = true;
			string[] val;

			rsp = SendCommand(SharedData.REF_MOVE_REL_ANGLE,100);
			if (rsp.StartsWith("fail"))
				rtn = false;
			else
				{
				val = rsp.Split(' ');
				if (val.Length == 2)
					rel_angle = double.Parse(val[1]);
				}
			return(rtn);
		}




		private Arm.Loc3D PtLocation(int row,int col,int tangle)

		{
			Arm.Loc3D loc = new Arm.Loc3D();
			int pixel;
			double dist, cangle;

			pixel = (row * Kinect.nui.ColorStream.FrameWidth) + (Kinect.nui.ColorStream.FrameWidth - col - 1);
			loc.y = (sips[pixel].Y * SharedData.M_TO_IN) + SharedData.BASE_KINECT_HEIGHT;
			dist = Math.Sqrt(Math.Pow(sips[pixel].X * SharedData.M_TO_IN, 2) + Math.Pow(sips[pixel].Z * SharedData.M_TO_IN, 2));
			cangle = Math.Asin((double) -sips[pixel].X/ sips[pixel].Z) * SharedData.RAD_TO_DEG;
			loc.x = dist * Math.Sin((cangle - tangle) * SharedData.DEG_TO_RAD);
			loc.z = dist * Math.Cos((cangle - tangle) * SharedData.DEG_TO_RAD);
			return (loc);
		}



		private bool FrontClear(int tangle, int dist, int side_clear = 5)

		{
			int row, col;
			bool obstacle;
			Arm.Loc3D target_loc = new Arm.Loc3D();

			obstacle = false;
			for (row = 0; row < Kinect.nui.ColorStream.FrameHeight; row++)
				{
				for (col = 0; col < Kinect.nui.ColorStream.FrameWidth; col++)
					{
					target_loc = PtLocation(row, col, tangle);
					if ((target_loc.z > 0) && (target_loc.z <= dist) && (target_loc.y < SharedData.BASE_KINECT_HEIGHT + 2) && (target_loc.y > 2))
						{
						if ((target_loc.x == 0) && (col != 319))
							{
							//ignore since this is an unknown location
							}
						else if (Math.Abs(target_loc.x) <= ((double) SharedData.ROBOT_WIDTH / 2) + side_clear)
							{
							obstacle = true;
							Log.KeyLogEntry("Front obstacle @  row: " + row + "   col: " + col + "   modified robot coord (in): " + target_loc.ToString());
							break;
							}
						}
					}
				if (obstacle)
					break;
				}
			return (obstacle);
		}



		private bool FrontClearAngle(int dist,int sangle,ref int fangle,bool log = true)

		{
			bool rtn = false,obstacle;
			int tangle,factor,i;

			if (Kinect.GetDepthFrame(ref depthdata, 60))
				{
				Kinect.nui.CoordinateMapper.MapDepthFrameToSkeletonFrame(Kinect.nui.DepthStream.Format, depthdata, sips);
				tangle = sangle;
				obstacle = FrontClear(tangle,dist);
				if (obstacle)
					{
					if (sangle >= 0)
						factor = -1;
					else
						factor = 1;
					for (i = 1; i < 4; i++)
						{
						tangle = sangle + (i * 2 * factor);
						obstacle = FrontClear(tangle, dist);
						if (!obstacle)
							break;
						}
					if (obstacle)
						{
						factor = -factor;
						for (i = 1; i < 4; i++)
							{
							tangle = sangle + (i * 2 * factor);
							obstacle = FrontClear(tangle, dist);
							if (!obstacle)
								break;
							}
						}
					if (log)
						SaveSip(sips);
					}
				rtn = !obstacle;
				if (!obstacle)
					{
					fangle = tangle;
					if (sangle != tangle)
						Log.LogEntry("Angle modified from " + sangle + "° to " + tangle + "°");
					else
						Log.LogEntry("Clear path");
					}
				else
					Log.LogEntry("No clear path found.");
				}
			else
				Log.LogEntry("Could not obtain a depth frame.");
			return (rtn);
		}



		private void LocalizeThread(object loc)

		{
			Location rloc = new Location();
			Stopwatch sw = new Stopwatch();

			Log.LogEntry("LocalizeThread started");
			NavData.location mloc = (NavData.location) loc;
			sw.Start();
			if (rloc.DetermineDRLocation(ref mloc, false, new Point(0, 0)))
				{
				sw.Stop();
				pose.coord = mloc.coord;
				pose.orient = mloc.orientation;
				localization_avail = true;
				localize_time = (int) sw.ElapsedMilliseconds;
				}
			else
				{
				waiting_localization = false;
				Log.LogEntry("Could not localize.");
				}
		}



		private void TrackLocation(int dur,int adirect,int fdirect,bool first,ref NavData.location nloc)

		{
			string new_rm;
			NavData.location loc;
			double ldist;
			MotionMeasureProb.Pose epose;
			ArrayList sdata = new ArrayList();
			NavCompute.out_of_bounds ob;

			Log.LogEntry("TrackLocation: " + dur +  "," + adirect + "," + fdirect);
			loc = NavData.GetCurrentLocation();
			ldist = mc.lspeed * SPEED_CONVERT * ((double) dur/1000) * 12 * volt_factor;
			if (first)
				ldist /= 2;
			loc.coord.X += (int)Math.Round(ldist * Math.Sin(adirect * SharedData.DEG_TO_RAD));
			loc.coord.Y -= (int)Math.Round(ldist * Math.Cos(adirect * SharedData.DEG_TO_RAD));
			ob = NavCompute.LocationOutOfBounds(loc.coord);
			if (ob != NavCompute.out_of_bounds.NOT)
				{
				Point new_loc = new Point(), x_loc = new Point(),ox_loc = new Point();
				int cdist = 0;
				NavData.connection oconnector = new NavData.connection(),nconnector = new NavData.connection();

				new_rm = NavCompute.DetermineNewRoom(loc.coord,ob,fdirect,ref new_loc, ref x_loc,ref ox_loc, ref oconnector,ref nconnector, ref cdist);
				if (new_rm != loc.rm_name)
					{
					Log.LogEntry("I just entered " + new_rm);
					epose.coord = ox_loc;
					epose.orient = fdirect;
					loc.rm_name = new_rm;
					loc.coord = x_loc;
					MotionMeasureProb.ConnectorMove(epose,new MotionMeasureProb.Pose(x_loc.X, x_loc.Y,fdirect),oconnector,nconnector);
					NavData.SetCurrentLocation(loc);
					Navigate.rmi.OpenLimited(new_rm);
					waiting_localization = true;
					localization_avail = false;
					ldist = mc.lspeed * SPEED_CONVERT * SCAN_DELAY * 12 * volt_factor;
					loc.coord = new_loc;
					if (oconnector.direction == 90)
						loc.coord.X += (int) Math.Round(ldist);
					else if (oconnector.direction == 270)
						loc.coord.X -= (int) Math.Round(ldist);
					else if (oconnector.direction == 180)
						loc.coord.Y += (int)Math.Round(ldist);
					else
						loc.coord.Y -= (int)Math.Round(ldist);
					lthread = new Thread(LocalizeThread);
					lthread.Start(loc);
					loc.coord = new_loc;
					}
				else
					{
					loc.coord = new_loc;
					}
				}
			else if (waiting_localization)
				{
				if (localization_avail)
					{
					waiting_localization = false;
					loc.coord = pose.coord;
					loc.orientation = pose.orient;
					NavData.SetCurrentLocation(loc);
					correction = pose.orient - fdirect;
					fdirect = pose.orient;
					ldist = mc.lspeed * SPEED_CONVERT * ((double) localize_time/ 2000) * 12 * volt_factor;
					loc.coord.X += (int) Math.Round(ldist * Math.Sin(fdirect * SharedData.DEG_TO_RAD));
					loc.coord.Y -= (int) Math.Round(ldist * Math.Cos(fdirect * SharedData.DEG_TO_RAD));
					Log.LogEntry("Localization correction issued (" + localize_time + " ms)");
					}
				}
			epose.coord = loc.coord;
			epose.orient = fdirect;
			MotionMeasureProb.Move(epose);
			nloc.coord = loc.coord;
			nloc.rm_name = loc.rm_name;
			nloc.loc_name = "";
			nloc.ls = NavData.LocationStatus.DR;
			nloc.orientation = fdirect;
			NavData.SetCurrentLocation(nloc);
			locatn.Add(nloc);
		}



		private void TrackLocation(int dist,int turn,ref NavData.location nloc)

		{
			string new_rm;
			NavData.location loc;
			MotionMeasureProb.Pose epose;
			ArrayList sdata = new ArrayList();
			NavCompute.out_of_bounds ob;

			Log.LogEntry("TrackLocation: " + dist + ", " + turn);
			loc = NavData.GetCurrentLocation();
			loc.orientation += turn;
			if (loc.orientation > 360)
				loc.orientation %= 360;
			else if (loc.orientation < 0)
				loc.orientation += 360;
			loc.coord.X += (int)Math.Round(dist * Math.Sin(loc.orientation * SharedData.DEG_TO_RAD));
			loc.coord.Y -= (int)Math.Round(dist * Math.Cos(loc.orientation * SharedData.DEG_TO_RAD));
			ob = NavCompute.LocationOutOfBounds(loc.coord);
			if (ob != NavCompute.out_of_bounds.NOT)
				{
				Point new_loc = new Point(), x_loc = new Point(),ox_loc = new Point();
				int cdist = 0;
				NavData.connection oconnector = new NavData.connection(), nconnector = new NavData.connection();

				new_rm = NavCompute.DetermineNewRoom(loc.coord,ob,loc.orientation,ref new_loc, ref x_loc, ref ox_loc,ref oconnector,ref nconnector, ref cdist);
				if (new_rm.Length > 0)
					{
					Log.LogEntry("I just entered " + new_rm);
					epose.coord = ox_loc;
					epose.orient = loc.orientation;
					loc.rm_name = new_rm;
					loc.coord = x_loc;
					loc.ls = NavData.LocationStatus.DR;
					MotionMeasureProb.ConnectorMove(epose,new MotionMeasureProb.Pose(x_loc.X,x_loc.Y,loc.orientation), oconnector,nconnector);
					NavData.SetCurrentLocation(loc);
					Navigate.rmi.OpenLimited(new_rm);
					loc.coord = new_loc;
					}
				else
					{
					error = "Could not determine new room";
					Log.LogEntry(error);
					}
				}
			epose.coord = loc.coord;
			epose.orient = loc.orientation;
			MotionMeasureProb.Move(epose);
			nloc.coord = loc.coord;
			nloc.rm_name = loc.rm_name;
			nloc.loc_name = "";
			nloc.ls = NavData.LocationStatus.DR;
			nloc.orientation = loc.orientation;
			NavData.SetCurrentLocation(nloc);
			locatn.Add(nloc);
		}



		private void FollowPerson()

		{
			bool move_started = false,person_detect;
			PersonDetect.scan_data pdd = new PersonDetect.scan_data();
			string rsp;
			NavData.location nloc = new NavData.location();
			int missed_person = 0;
			int mdist,mangle,dur,samples,ad;
			double sd,cd,ld;
			long last_time = 0;
			double cdist;

			locatn.Clear();
			ts.Clear();
			pdist.Clear();
			pangle.Clear();
			dist.Clear();
			rangle.Clear();
			lspeed.Clear();
			tspeed.Clear();
			rms.Clear();
			lms.Clear();
			correction = 0;
			error = "";
			mc.ldirec = linear_directions.NONE;
			mc.lspeed = 0;
			mc.tdirec = turn_directions.NONE;
			mc.tspeed = 0;
			current = NavData.GetCurrentLocation();
			waiting_localization = false;
			Stopwatch stime = new Stopwatch();

			try
				{
			if (initialized)
				{
				do
					{
					if (pd.NearestHCLPerson(false, ref pdd, false))
						{
						if (pdd.dist > MAINTAINED_DISTANCE)
							{
							UiCom.SetVideoSuspend(true);
							Supervisor.SetPollRead(false);
							rsp = SendCommand(SharedData.REF_MOVE_START + " " + START_SPEED, 1000);
							last_time = sw.ElapsedMilliseconds;
							if (rsp.StartsWith("fail"))
								{
								if (rsp.Contains("receive timed out"))
									{
									SendCommand(SharedData.REF_MOVE_STOP, 1000);
									}
								if (rsp.Length > 5)
									error = "MoveToPerson failed with: " + rsp;
								else
									error = "MoveToPerson failed with command error";
								Log.LogEntry(error);
								stop_follow_me = true;
								UiCom.SetVideoSuspend(false);
								Supervisor.SetPollRead(true);
								break;
								}
							else
								{
								mc.lspeed = START_SPEED;
								mc.tspeed = 0;
								mp.rnow = mp.lnow = 128 - START_SPEED;
								ts.Add(0);
								pdist.Add(pdd.dist);
								pangle.Add(pdd.angle);
								dist.Add(0);
								rangle.Add(0);
								lspeed.Add(mc.lspeed);
								tspeed.Add(mc.tspeed);
								rms.Add(mp.rnow);
								lms.Add(mp.lnow);
								locatn.Add(current);
								}
							move_started = true;
							}
						}
					}
				while (!move_started);
				rel_angle = 0;
				cd = sd = current.orientation;
				samples = 0;
				while (!stop_follow_me)
					{
					long et;

					person_detect = pd.NearestHCLPerson(false, ref pdd, false, false);
					samples += 1;
					RelativeAngle();
					ld = cd;
					cd = sd - rel_angle + correction;
					if (cd < 0)
						cd += 360;
					else if (cd > 360)
						cd %= 360;
					et = sw.ElapsedMilliseconds;
					dur = (int) (et - last_time);
					ts.Add(dur);
					last_time = et;
					if (Math.Abs(cd - ld) > 180)
						ad = ((int) Math.Round((cd + ld + 360)/2)) % 360;
					else
						ad = (int)Math.Round((cd + ld) / 2);
					TrackLocation(dur,ad,(int) Math.Round(cd),samples == 1,ref nloc);
					if (nloc.rm_name == target_new_room)
						target_new_room = "";
					if (person_detect && !stop_follow_me)
						{
						missed_person = 0;
						if (pdd.dist == Kinect.nui.DepthStream.TooFarDepth * SharedData.MM_TO_IN)
							mdist = 120;
						else
							mdist = (int) Math.Round(pdd.dist);
						mangle = (int) -Math.Round(pdd.angle);
						if (!stop_follow_me && sw.ElapsedMilliseconds > CORRECT_DELAY)
							{
							LinearLogic(mdist);
							if ((mc.lspeed > 0) && (mc.lspeed < START_SPEED))
								mc.lspeed = START_SPEED;
							if (mc.lspeed == 0)
								mc.tspeed = 0;
							else if (Math.Abs(mangle) > 0)
								{
								if ((mdist - 12 > Kinect.NEAR_MIN) && !FrontClearAngle(mdist - 12,mangle,ref mangle))
									{
									error = "No clear path found.";
									Log.LogEntry(error);
									stop_follow_me = true;
									}
								else
									TurnLogic(mangle);
								}
							else
								mc.tspeed = 0;
							if (!stop_follow_me)
								{
								if (!SetMotors())
									{
									error = "Set motor command failed.";
									Log.LogEntry(error);
									stop_follow_me = true;
									}
								else
									{
									Point curr = nloc.coord;
									curr.X += (int) Math.Round(pdd.dist * Math.Sin((cd - pdd.angle) * SharedData.DEG_TO_RAD));
									curr.Y -= (int) Math.Round(pdd.dist * Math.Cos((cd - pdd.angle) * SharedData.DEG_TO_RAD));
									if (target_new_room.Length  == 0)
										{
										if (NavCompute.LocationOutOfBounds(curr) != NavCompute.out_of_bounds.NOT)	
											{
											connect = new NavData.connection();
											target_new_room = NavCompute.DetermineNewRoom(curr,ref connect);
											if (target_new_room.Length > 0)
												Log.LogEntry("follow me target just entered " + target_new_room);
											}
										}
									else
										{
										last_heading = NavCompute.DetermineHeadingPtToPt(curr,nloc.coord);
										last_target_loc = curr;
										}
									}
								}
							}
						pdist.Add(pdd.dist);
						pangle.Add(pdd.angle);
						dist.Add(mdist);
						rangle.Add(mangle);
						lspeed.Add(mc.lspeed);
						tspeed.Add(mc.tspeed);
						rms.Add(mp.rnow);
						lms.Add(mp.lnow);
						}
					else if (!stop_follow_me)
						{
						TurnLogic(0);
						if (!SetMotors())
							{
							error = "Set motor command failed.";
							Log.LogEntry(error);
							stop_follow_me = true;
							}
						pdist.Add((double) -1);
						pangle.Add((double) -1);
						dist.Add(-1);
						rangle.Add(0);
						lspeed.Add(mc.lspeed);
						tspeed.Add(mc.tspeed);
						rms.Add(mp.rnow);
						lms.Add(mp.lnow);
						missed_person += 1;
						if (missed_person == 2)
							{
							error = PERSON_LOST;
							Log.LogEntry(error);
							stop_follow_me = true;
							}
						}
					}
				if (move_started)
					{
					stime.Start();
					rsp = SendCommand(SharedData.REF_MOVE_STOP, 2000);
					stime.Stop();
					cdist = mc.lspeed * SPEED_CONVERT * ((double) stime.ElapsedMilliseconds/1000) * 12 * .5 * volt_factor;
					RelativeAngle();
					double ora = sd + correction - cd;
					TrackLocation((int) Math.Round(cdist),(int) -Math.Round(rel_angle - ora),ref nloc);
					ts.Add(stime.ElapsedMilliseconds);
					pdist.Add((double) -1);
					pangle.Add((double) -1);
					dist.Add(-1);
					rangle.Add(0);
					lspeed.Add(0);
					tspeed.Add(0);
					rms.Add(128);
					lms.Add(128);
					locatn.Add(nloc);
					if (rsp.StartsWith(Constants.UiConstants.FAIL))
						{
						if (!MotionControl.Operational(ref rsp))
							SharedData.motion_controller_operational = false;
						}
					UiCom.SetVideoSuspend(false);
					Supervisor.SetPollRead(true);
					SaveMoveProfile(error);
					if (floc.DetermineDRLocation(ref nloc, false, new Point(0, 0)))
						NavData.SetCurrentLocation(nloc);
					LocationMessage();
					}
				}
			else
				{
				error = "Follow Person failed, PID parameters not initialized";
				Log.LogEntry(error);
				}
			}

			catch(Exception ex)
			{
			SendCommand(SharedData.REF_MOVE_STOP, 1000);
			error = "follow person had an exception";
			Log.LogEntry("FollowPerson exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			SaveMoveProfile(error);
			}

		}



		private string SaveMoveProfile(string error)

		{
			int i;
			TextWriter sw;
			string fname;

			fname = Log.LogDir() + "Follow person " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
			sw = File.CreateText(fname);
			if (sw != null)
				{
				sw.WriteLine("Follow person: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				sw.WriteLine("Motion correction gains:");
				sw.WriteLine("   turn gain - " + tgain);
				sw.WriteLine("   linear motion gain - " + lgain);
				sw.WriteLine("Start location: " + current.ToCSVString());
				sw.WriteLine();
				if (error.Length > 0)
					{
					sw.WriteLine("Error: " + error.Replace(',',' '));
					sw.WriteLine();
					}
				sw.WriteLine("Duration (ms),Person relative angle (°),Person distance (in),Control relative angle (°),Control distance (in),Linear speed,Turn speed,Left motor setting,Right motor setting,Location");
				for (i = 0;i < ts.Count;i++)
					{
					try
					{
					sw.Write(ts[i].ToString() + ",");
					if ((int) dist[i] == -1)
						{
						sw.Write("PND,");
						sw.Write("PND,");
						sw.Write("PND,");
						sw.Write("PND,");
						}
					else
						{
						sw.Write(((double) pangle[i]).ToString("F1") + ",");
						sw.Write(((double) pdist[i]).ToString("F1") + ",");
						sw.Write(rangle[i].ToString() + ",");
						sw.Write(dist[i].ToString() + ",");
						}
					sw.Write(lspeed[i].ToString() + ",");
					sw.Write(tspeed[i].ToString() + ",");
					sw.Write(lms[i].ToString() + ",");
					sw.Write(rms[i].ToString() + ",");
					sw.Write(((NavData.location) locatn[i]).ToCSVString());
					}

					catch(Exception ex)
					{
					sw.Write("exception: " + ex.Message);
					}

					sw.WriteLine();
					sw.Flush();
					}
				sw.Close();
				Log.LogEntry("Saved " + fname);
				}
			else
				fname = "";
			return(fname);
		}



		private void SaveSip(SkeletonPoint[] sips)

		{
			BinaryWriter bw;
			string fname;
			DateTime now = DateTime.Now;
			double value;
			int i;

			fname = Log.LogDir() + "\\Point cloud binary data " + +now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + "-" + SharedData.GetUFileNo() + ".pc";
			bw = new BinaryWriter(File.Create(fname));
			if (bw != null)
				{
				for (i = 0; i < sips.Length; i++)
					{
					value = (short)(sips[i].X * 1000);
					bw.Write((short)value);
					value = (short)(sips[i].Y * 1000);
					bw.Write((short)value);
					value = (short)(sips[i].Z * 1000);
					bw.Write((short)value);
					}
				bw.Close();
				Log.LogEntry("Saved: " + fname);
				}
		}



		private bool LidarFrontClear(int rangle,int clear_dist, int width,ref ArrayList sdata)

		{
			bool rtn = true;
			Rplidar.scan_data sd;
			int i,ang;
			double x,y,xmax,ymin = clear_dist + 1;
			Point pt;

			Log.LogEntry("LidarFrontClear " + rangle + "  " + clear_dist + "  " + width);
			xmax = ((double) width / 2);
			for (i = 0; i < sdata.Count; i++)
				{
				sd = (Rplidar.scan_data)sdata[i];
				ang = (sd.angle + rangle) % 360;
				if (ang < 0)
					ang += 360;
				if ((ang < 90) || (ang > 270))
					{
					x = sd.dist * Math.Sin(ang * SharedData.DEG_TO_RAD);
					if (Math.Abs(x) <= xmax)
						{
						y = sd.dist * Math.Cos(ang * SharedData.DEG_TO_RAD);
						if (y < clear_dist)
							{
							pt = new Point((int)Math.Round(x), (int)Math.Round(y));
							Log.LogEntry("Obstacle @ " + pt);
							rtn = false;
							break;
							}
						}
					}
				}
			Log.LogEntry("LidarFrontClearn " + rtn);
			return(rtn);
		}



		private bool LidarFrontClearAngle(int rangle,int clear_dist,int width,ref ArrayList sdata,ref int direct)

		{
			int i,cangle;
			bool rtn = false;


			if (LidarFrontClear(rangle, clear_dist, width, ref sdata))
				{
				rtn = true;
				direct = rangle;
				}
			else
				{
				for (i = 1;i < 6;i++)
					{
					cangle = 2 * i;
					if (LidarFrontClear(rangle + cangle, clear_dist, width, ref sdata))
						{
						rtn = true;
						direct = rangle + cangle;
						break;
						}
					else if (LidarFrontClear(rangle - cangle, clear_dist, width, ref sdata))
						{
						rtn = true;
						direct = rangle - cangle;
						break;
						}
					}
				}
			return(rtn);
		}
		


		private bool SearchInLine(ref int pan)

		{
			bool rtn = false, turned = false;
			NavData.location ncloc = new NavData.location();
			int ra = 0,width,dist;
			int tc;
			string rsp = "";

			Log.LogEntry("SearhInLine");
			ncloc = NavData.GetCurrentLocation();
			ra = NavCompute.AngularDistance(connect.direction,ncloc.orientation);
			if (!NavCompute.ToRightDirect(ncloc.orientation,connect.direction))
				ra *= -1;
			if ((connect.direction == 90) || (connect.direction == 270))
				dist = Math.Abs(connect.exit_center_coord.X - ncloc.coord.X);
			else
				dist = Math.Abs(connect.exit_center_coord.Y - ncloc.coord.Y);
			dist += SharedData.ROBOT_LENGTH;
			width = (int) Math.Round(((double) SharedData.ROBOT_WIDTH/2) + 1);
			if (LidarFrontClearAngle(ra,dist,width,ref floc.sdata,ref ra))
				{
				if (ra != 0)
					turned = Turn(-ra, ref rsp);
				else
					turned = true;
				if (turned)
					{
					tc = (int)(((dist / 7.2) + 2) * 1000);
					rsp = SendCommand(SharedData.FORWARD + " " + dist, tc);
					if (rsp.StartsWith("fail"))
						Log.LogEntry("forward move failed.");
					else
						{
						TrackLocation(dist,ra, ref ncloc);
						pan = 67;
						if (!NavCompute.ToRightDirect(ncloc.orientation, last_heading))
							pan *= -1;
						rtn = true;
						}
					}
				else
					Log.LogEntry("turn to face connector failed.");
				}
			else
				Log.LogEntry("No clear path");
			return (rtn);
		}



		private bool SearchNotInLine(ref int pan)

		{
			bool rtn = false;

			Log.LogEntry("SearhNotInLine");
			// determine approx direct and distance to "exit point"
			// check for obstacles
			// move to "exit point"
			// TrackLocation
			// determine approx direct to connector center
			// determine approach angle and dist for connector (see Door.ExitPosition)
			// move just beyond connector
			// TrackLocation
			// pan based on room characteristics and target last heading
			return (rtn);
		}



		private bool InLine(string current_rm)

		{
			bool rtn = false;
			Rectangle rm;

			rm = NavData.GetRectangle(current_rm);
			if ((connect.direction == 90) || (connect.direction == 270))
				{
				if (connect.exit_width == rm.Height)
					rtn = true;
				}
			else
				{
				if (connect.exit_width == rm.Width)
					rtn = true;
				}
			return (rtn);
		}



		private bool SearchTask()

		{
			bool rtn = false;
			int pan = 0,turned = 0;
			NavData.location ncloc = new NavData.location();
			PersonDetect.scan_data pdd = new PersonDetect.scan_data();
			int direct = 0;

			ncloc = NavData.GetCurrentLocation();
			Log.LogEntry("SearchTask: current room " + ncloc.rm_name + "   target room " + target_new_room + "   last heading " + last_heading + "   last location " + last_target_loc + "   connection orientation " + connect.direction);
			if (InLine(ncloc.rm_name))
				rtn = SearchInLine(ref pan);
			else
				rtn = SearchNotInLine(ref pan);
			if (rtn)
				{
				rtn = false;
				HeadAssembly.Pan(pan, true);
				if (pd.NearestHCLPerson(false, ref pdd))
					{
					direct = (int) -Math.Round(pan - pdd.angle);
					if (direct > 180)
						direct -= 360;
					else if (direct < -180)
						direct += 360;
					HeadAssembly.Pan(0,true);
					if (TurnToFacePerson(direct,ref turned))
						{
						ncloc = NavData.GetCurrentLocation();
						ncloc.orientation += turned;
						if (ncloc.orientation > 360)
							ncloc.orientation %= 360;
						else if (ncloc.orientation < 0)
							ncloc.orientation += 360;
						NavData.SetCurrentLocation(ncloc);
						rtn = true;
						Speech.SpeakAsync("Oh there you are.");
						}
					else
						error = "Turn to face person failed.";
					}
				else
					{
					HeadAssembly.Pan(0, true);
					error = PERSON_LOST;
					}
				}
			else
				error = "Search failed.";
			return (rtn);
		}



		private void LocationMessage()

		{
			NavData.location loc;
			Rectangle rect;
			string msg;
			
			rect = MotionMeasureProb.PdfRectangle();
			loc = NavData.GetCurrentLocation();
			msg = loc.rm_name + "," + loc.coord.X + "," + loc.coord.Y + "," + loc.orientation + ","  + loc.loc_name + "," + loc.ls + "," + rect.Height + "," + rect.Width + "," + rect.X + "," + rect.Y;
			UiCom.SendLocMessage(msg);
		}



		public void FollowMeThread()

		{
			int pan = 0;
			PersonDetect.scan_data pdd = new PersonDetect.scan_data();
			Room.rm_location rl;
			NavData.location cl;
			Room rm = SharedData.current_rm;
			ArrayList sdata = new ArrayList();
			Location rloc = new Location();
			bool done = false;
			double volts;

			Speech.DisableAllCommands();
			volts = MotionControl.GetVoltage();
			if (volts > 0)
				volt_factor = ((volts * 4.7882 - 112.45 )/100) + 1;
			else
				volt_factor = 1;

			try
			{
			Log.KeyLogEntry("Follow me");
			cl = NavData.GetCurrentLocation();
			if (pd.NearestHCLPerson(false, ref pdd,false,false))
				{
				Kinect.SetNearRange();
				Log.LogEntry("Speaker detected at angle of " + (pdd.angle - pan).ToString("F1") + " and distance of " + pdd.dist.ToString("F1") + " in");
				rl = NavCompute.PtDistDirectApprox(cl.coord, (int)Math.Round((cl.orientation + pan - pdd.angle) % 360), (int)Math.Round(pdd.dist));
				Log.LogEntry("Speaker located at " + rl.coord);
				pdd.rm_location = rl.coord;
				pdd.ts = SharedData.app_time.ElapsedMilliseconds;
				SpeakerData.Person = pdd;
				Speech.RegisterHandler(Speech.STOP_GRAMMAR, StopHandler, null);
				Speech.Speak("OK, lead on.");
				sw.Restart();
				do
					{
					stop_follow_me = false;
					error = "";
					FollowPerson();
					if (error.Length == 0)
						done = true;
					else if (((error == PERSON_LOST) || ((error == NO_CLEAR_PATH)) && (target_new_room.Length > 0)))
						{
						if (SearchTask())
							{
							correction = 0;
							error = "";
							}
						else
							done = true;
						}
					else
						done = true;
					}
				while (!done);
				sw.Stop();
				Speech.UnRegisterHandler(Speech.STOP_GRAMMAR);
				if (error.Length > 0)
					Speech.SpeakAsync("Following you failed, " + error);
				else
					Speech.SpeakAsync("Following you is completed.");
				Kinect.SetFarRange();
				}
			else
				Speech.SpeakAsync("Could not find the speaker.");
			}

			catch (Exception ex)
			{
			Speech.SpeakAsync("Follow me command thread had an exception");
			Log.LogEntry("Follow me command thread exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			Kinect.SetFarRange();
			}

			Speech.EnableAllCommands();
		}



		private bool ReadRefParameters()

		{
			string fname;
			TextReader tr;
			bool rtn = false;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PID_PARAM_FILE + SharedData.CAL_FILE_EXT;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);

				try
				{
				lgain = int.Parse(tr.ReadLine());
				tgain = int.Parse(tr.ReadLine());
				rtn = true;
				}

				catch(Exception ex)
				{
				Log.LogEntry("ReadRefParameters exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				tr.Close();
				}
			else
				Log.LogEntry("Could not read " + PID_PARAM_FILE);
			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + MOTION_PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);

				try
				{
				string line;
				string[] val;

				mtr_setting = int.Parse(tr.ReadLine());
				tr.ReadLine();
				tr.ReadLine();
				tr.ReadLine();
				tr.ReadLine();
				tr.ReadLine();
				tr.ReadLine();
				tr.ReadLine();
				line = tr.ReadLine();
				val = line.Split(',');
				if (val.Length == 2)
					{
					vs_slope = double.Parse(val[0]);
					vs_intercept = double.Parse(val[1]);
					}
				}

				catch (Exception ex)
				{
				Log.LogEntry("ReadRefParameters exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				tr.Close();
				}
			else
				Log.LogEntry("Could not read " + MOTION_PARAM_FILE);
			return (rtn);
		}

		}
	}
