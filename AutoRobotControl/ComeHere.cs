using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Collections;
using System.IO;
using System.Windows.Forms;
using Microsoft.Speech.Recognition;
using Coding4Fun.Kinect.WinForm;


namespace AutoRobotControl
	{
	public class ComeHere
		{

		public const int PERSON_CLEARANCE = 36;
		private const int MIN_MOVE = 10;
		private const int SAMPLE_MOVE_DIST = 1;
		private const int CORRECT_DELAY = 200;
		private const string PID_PARAM_FILE = "chpid";
		private const int START_SPEED = 30;
		private const int SPEAKER_TS_DIF = 60000;
		private const int BASE_TILT = -55;
		private const double BASE_TILT_CORRECT = -4.0;
		private const double BASE_TILT_MAX_DIST = 77.6;
		private const double MIN_HEIGTH_DETECT = 2.0;
		
		private byte[] videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
		private string error;
		private PersonDetect pd = new PersonDetect();
		private double pgain, igain, dgain;
		private bool initialized;
		private Move mov = new Move();
		private Room rm = SharedData.current_rm;
		private ArrayList ts = new ArrayList();
		private ArrayList dist = new ArrayList();
		private ArrayList rangle = new ArrayList();
		public bool succeed = false;


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
				pgain = double.Parse(tr.ReadLine());
				igain = double.Parse(tr.ReadLine());
				dgain = double.Parse(tr.ReadLine());
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
			return(rtn);
		}



		public void LoadPIDParam(double pgain,double igain,double dgain)

		{
			this.pgain = pgain;
			this.igain = igain;
			this.dgain = dgain;
		}



		public void SavePersonPic()

		{
			string fname;
			Bitmap bm;
			Graphics g;
			int col,row;

			if (Kinect.GetColorFrame(ref videodata, 40))
				{
				fname = Log.LogDir() + "person pic " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				g = System.Drawing.Graphics.FromImage(bm);
				col = (int)Math.Round((double)Kinect.nui.ColorStream.FrameWidth / 2);
				g.DrawLine(Pens.Red, col, 0, col, 479);
				row = (int)Math.Round((double)Kinect.nui.ColorStream.FrameHeight / 2);
				g.DrawLine(Pens.Red, 0, row, 639, row);
				bm.RotateFlip(RotateFlipType.Rotate180FlipY);
				bm.Save(fname,ImageFormat.Jpeg);
				Log.LogEntry("Saved " + fname);
				}
		}



		private string SendCommand(string command,int timeout)

		{
			string rtn = "";

			rtn = MotionControl.SendCommand(command,timeout/10);
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



		public bool TurnToPerson(ref int pdist,ref int direct)

		{
			bool rtn = false;
			PersonDetect.scan_data pdd = new PersonDetect.scan_data();
			string rsp;
			NavData.location cl;
			int angle_correct = 0;

			error = "";
			cl = NavData.GetCurrentLocation();
			if (pd.NearestHCLPerson(false, ref pdd))
				{
				while (Math.Abs(pdd.angle) > 2)
					{
					rsp = "";
					if (Turn((int) pdd.angle, ref rsp))
						{
						angle_correct -= (int) pdd.angle;
						if (!pd.NearestHCLPerson(false, ref pdd))
							{
							error = "TurnToPerson failed, person lost";
							Log.LogEntry(error);
							break;
							}
						}
					else
						{
						if (rsp.Length > 0)
							{
							error = "TurnToPerson failed with: " + rsp;
							Log.LogEntry(error);
							}
						else
							{
							error = "TurToPerson failed with command error";
							Log.LogEntry(error);
							}
						break;
						}
					}
				if (error.Length == 0)
					{				
					rtn = true;
					if (pdd.dist == Kinect.nui.DepthStream.TooFarDepth)
						pdist = int.MaxValue;
					else
						pdist = (int) Math.Round(pdd.dist);
					direct = (int) Math.Round((cl.orientation + angle_correct - pdd.angle) % 360);
					if (direct < 0)
						direct += 360;
					}
				}
			else
				{
				rtn = false;
				error = "TurnToPerson failed, person not found";
				Log.LogEntry(error);
				}
			return(rtn);
		}



		public string SaveMoveProfile(string error)

		{
			int i;
			TextWriter sw;
			string fname;

			fname = Log.LogDir() + "Move to person " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
			sw = File.CreateText(fname);
			if (sw != null)
				{
				sw.WriteLine("Move to person: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				sw.WriteLine("Turn correction gains:");
				sw.WriteLine("   pgain - " + pgain);
				sw.WriteLine("   dgain - " + dgain);
				sw.WriteLine("   igain - " + igain);
				sw.WriteLine();
				if (error.Length > 0)
					{
					sw.WriteLine("Error: " + error);
					sw.WriteLine();
					}
				sw.WriteLine("Elapsed time (ms),Relative Angle (°),Distance (in)");
				for (i = 0;i < ts.Count;i++)
					{
					try
					{
					sw.Write(ts[i].ToString() + ",");
					if ((double)dist[i] == -1)
						sw.Write("person not detected");
					else
						{
						sw.Write(((double) rangle[i]).ToString("F1") + ",");
						sw.Write(((double) dist[i]).ToString("F1"));
						}
					}

					catch(Exception)
					{
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



		public bool MoveToPerson(ref int pdist,ref string fname)

		{
			bool rtn = false,not_done = true;
			string rsp;
			long start_time;
			PersonDetect.scan_data pdd = new PersonDetect.scan_data();
			double sum_angle = 0, last_angle = 400;
			int applied_correction = 0,missed_person = 0;
			Stopwatch sw = new Stopwatch();

			error = "";
			ts.Clear();
			rangle.Clear();
			dist.Clear();
			if (initialized)
				{
				UiCom.SetVideoSuspend(true);
				Supervisor.SetPollRead(false);
				rsp = SendCommand(SharedData.REF_MOVE_START + " " + START_SPEED, 500);
				if (rsp.StartsWith("fail"))
					{
					if (rsp.Contains("receive timedout"))
						{
						SendCommand(SharedData.REF_MOVE_STOP, 1000);
						}
					if (rsp.Length > 5)
						error = "MoveToPerson failed with: " + rsp;
					else
						error = "MoveToPerson failed with command error";
					Log.LogEntry(error);
					}
				else
					{
					sw.Start();
					start_time = sw.ElapsedMilliseconds;
					while (not_done)
						{
						if (pd.NearestHCLPerson(false, ref pdd,false,false))
							{
							if (pdd.dist < PERSON_CLEARANCE + SAMPLE_MOVE_DIST)
								{
								SendCommand(SharedData.REF_MOVE_STOP, 1000);
								not_done = false;
								pdist = (int) pdd.dist;
								rtn = true;
								}
							else
								{
								int correction, delta_correct;
								string msg = "";

								if ((sw.ElapsedMilliseconds - start_time) > CORRECT_DELAY)
									{
									sum_angle += pdd.angle;
									correction = (int)((pgain * pdd.angle) + (igain * sum_angle));
									if (last_angle != 400)
										correction += (int)(dgain * (pdd.angle - last_angle));
									delta_correct = correction - applied_correction;
									applied_correction += delta_correct;
									if (delta_correct != 0)
										{
										msg = SharedData.REF_CHG_SPEED + " " + delta_correct.ToString() + " " + (-delta_correct).ToString();
										rsp = MotionControl.SendCommand(msg, 200);
										if (rsp.StartsWith("fail"))
											{
											SendCommand(SharedData.REF_MOVE_STOP, 1000);
											rtn = false;
											error = "MoveToPerson failed, CS command failed.";
											Log.LogEntry(error);
											break;
											}
										}
									}
								last_angle = pdd.angle;
								}
							ts.Add(sw.ElapsedMilliseconds - start_time);
							rangle.Add(pdd.angle);
							dist.Add(pdd.dist);
							}
						else
							{
							ts.Add(sw.ElapsedMilliseconds - start_time);
							rangle.Add(-1);
							dist.Add(-1);
							missed_person += 1;
							if (missed_person == 2)
								{
								SendCommand(SharedData.REF_MOVE_STOP, 1000);
								error = "MoveToPerson failed, person lost.";
								Log.LogEntry(error);
								not_done = false;
								}
							}
						}
					sw.Stop();
					if (SharedData.log_operations)
						fname = SaveMoveProfile(error);
					}
				UiCom.SetVideoSuspend(false);
				Supervisor.SetPollRead(true);
				}
			else
				{
				error = "MoveToPerson failed, PID parameters not initialized";
				Log.LogEntry(error);
				}
			return(rtn);
		}



		private void InitialFaceSpeaker(Room.rm_location rl, PersonDetect.scan_data pdd)

		{
			NavCompute.pt_to_pt_data ppd;
			int pan,tilt;
			NavData.location cl;

			Log.LogEntry("Initial attempt to 'face' speaker");
			cl = NavData.GetCurrentLocation();
			ppd = NavCompute.DetermineRaDirectDistPtToPt(rl.coord, cl.coord);
			pan = NavCompute.AngularDistance(cl.orientation, ppd.direc);
			if (!NavCompute.ToRightDirect(cl.orientation, ppd.direc))
				pan *= -1;
			HeadAssembly.Pan(pan, true);
			Log.LogEntry("Current location: " + cl);
			Log.LogEntry("Speaker location: " + rl.coord);
			Log.LogEntry("Pan angle: " + pan);
			Log.LogEntry("Speaker box top right: (" + pdd.vdo.x + "," + pdd.vdo.y + ")");
			if (pdd.vdo.y > Kinect.nui.ColorStream.FrameHeight / 2)
				{
				double angle, down;

				angle = Kinect.VideoVerDegrees(pdd.vdo.y - Kinect.nui.ColorStream.FrameHeight / 2);
				down = Math.Abs(pdd.dist * Math.Tan(angle * SharedData.DEG_TO_RAD));
				tilt = (int)Math.Round(Math.Abs(Math.Atan(down / NavCompute.DistancePtToPt(cl.coord, rl.coord))) * SharedData.RAD_TO_DEG);
				Log.LogEntry("Tilt angle: " + -tilt);
				if (angle > 1)
					HeadAssembly.Tilt(-tilt, true);
				}
		}



		private bool MoveToObstructedSpeaker(NavData.location cl,Room.rm_location rl, PersonDetect.scan_data pdd,Point mpt)

		{
			bool rtn = false;
			NavData.room_pt rp;

			rp = new NavData.room_pt();
			rp.name = cl.rm_name;
			rp.coord = mpt;
			Log.LogEntry("Determined move point: " + mpt);
			Speech.SpeakAsync("Coming");
			rm.Run = true;
			if (mov.GoToRoomPoint(rp, new Point(0, 0)))
				{
				InitialFaceSpeaker(rl,pdd);
				rtn = true;
				}
			else
				Speech.SpeakAsync("Attempt to move to speaker failed.");
			rm.Run = false;
			return (rtn);
		}



		private bool DirectMoveToSpeaker(NavData.location cl, Room.rm_location rl, PersonDetect.scan_data pdd,ref int direct)

		{
			bool rtn = false;
			int pdist = 0;
			ArrayList lscan = new ArrayList();
			ArrayList obs = new ArrayList();
			string fname = "";

			if (TurnToPerson(ref pdist,ref direct))
				{
				if (pdist <= PERSON_CLEARANCE + MIN_MOVE)
					rtn = true;
				else if (Rplidar.CaptureScan(ref lscan, true))
					{
					Rplidar.FindObstacles(0,pdist - PERSON_CLEARANCE,lscan,1,false,ref obs);
					if (obs.Count == 0)
						{
						if (MoveToPerson(ref pdist,ref fname))
							rtn = true;
						}
					else
						{
						error = "Obstacle detected in path to speaker.";
						Log.LogEntry(error);
						}
					}
				else
					{
					error = "Could not capture front LIDAR scan.";
					Log.LogEntry(error);
					}
				}
			return (rtn);
		}



		private bool PathMoveToSpeaker(NavData.location cl, Room.rm_location rl, PersonDetect.scan_data pdd,Point mpt,ref int direct)

		{
			bool rtn = false;
			NavData.room_pt rp;

			rp = new NavData.room_pt();
			rp.name = cl.rm_name;
			rp.coord = mpt;
			Speech.SpeakAsync("Coming");
			rm.Run = true;
			if (mov.GoToRoomPoint(rp, new Point(0, 0)))
				{
				if (mov.TurnToFaceMP(rl.coord))
					rtn = DirectMoveToSpeaker(cl,rl,pdd,ref direct);
				else
					{
					error = "Attempt to turn to speaker failed.";
					Log.LogEntry(error);
					}
				}
			else
				{
				error = "Attempt to move to speaker failed.";
				Log.LogEntry(error);
				}
			rm.Run = false;
			return (rtn);
		}
		


		private bool PathClear(PersonDetect.scan_data pdd)

		{
			bool rtn = false,lrtn;
			ArrayList sdata = new ArrayList(),obs = new ArrayList();
			double mdist;
			int ldist;
			string reply;

			if (SpeakerData.Person.detected && ((SharedData.app_time.ElapsedMilliseconds - SpeakerData.Person.ts) < SPEAKER_TS_DIF))
				{
				mdist = pdd.dist - PERSON_CLEARANCE;
				if ((lrtn = Rplidar.CaptureScan(ref sdata, true)))
					{
					Rplidar.SaveLidarScan(ref sdata, "Move to person LIDAR scan");
					ldist = Rplidar.FindObstacles((int) Math.Round(-pdd.angle),(int) Math.Round(mdist),sdata,1,false,ref obs);
					}
				if (lrtn && (obs.Count == 0))
					rtn = true;
				else
					{
					if (!lrtn)
						Speech.Speak("Could not obtain obstruction data.");
					else
						Speech.Speak("Possible obstacles have been detected.");
					reply = AutoRobotControl.Speech.Conversation("Is my path to you clear.", "responseyn", 5000, true);
					if (reply == "yes")
						rtn = true;
					}
				}
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




		public void ComeHereCommandThread(object data)

		{
			int pan = 0,diff,adj,tilt = 0;
			PersonDetect.scan_data pdd = new PersonDetect.scan_data();
			bool seek_person = false,moved_to_person = false;
			Room.rm_location rl;
			NavData.location cl;
			Point mpt = new Point();
			ArrayList mptal = new ArrayList();
			Room rm = SharedData.current_rm;
			ArrayList al = new ArrayList();
			bool near = false,direct_move = false;
			int speaker_direction,direct = -1;
			SmartMotionCommands.come_data cd;
			ObstacleAdjust oa = new ObstacleAdjust();
			Location floc = new Location();

			Speech.DisableAllCommands();
			succeed = false;

			try
			{
			Log.KeyLogEntry("Come here");
			cd = (SmartMotionCommands.come_data) data;
			speaker_direction = cd.direction % 360;
			if (speaker_direction > -1)
				{
				Log.LogEntry("Speaker direction: " + speaker_direction);
				cl = NavData.GetCurrentLocation();
				if (HeadAssembly.DirectInPanLimits(Speech.speaker_direction))
					{
					if (speaker_direction < 180)
						{
						pan = speaker_direction;
						diff = Math.Abs(speaker_direction - 90);					// a crude linear approximation of the angle difference from the Kinect or the speech direct board
						adj = (int) Math.Round(3 - ((double) 3 * diff/90));	// assumes a distance of ~ 10 ft to the speaker
						pan = speaker_direction + adj;
						if (!HeadAssembly.DirectInPanLimits(pan))
							pan = speaker_direction;
						seek_person = true;
						}
					else
						{
						pan = speaker_direction - 360;
						diff = Math.Abs(pan + 90);
						adj = (int) Math.Round(3 - ((double) 3 * diff / 90));
						pan = speaker_direction - adj;
						if (!HeadAssembly.DirectInPanLimits(pan))
							pan = speaker_direction;
						seek_person = true;
						}
					}
				else
					{
					bool turned = false;

					Speech.SpeakAsync("okay, turning");
					if (cl.ls == NavData.LocationStatus.UNKNOWN)
						turned = AutoRobotControl.Turn.TurnAngle(180);
					else
						turned = mov.TurnToFaceDirect((cl.orientation + 180) % 360);
					if (turned)
						{
						Location loc;

						pan = 0;
						loc = new Location();
						if (cl.ls != NavData.LocationStatus.UNKNOWN)
							{
							cl = NavData.GetCurrentLocation();
							if (loc.DetermineDRLocation(ref cl,false,new Point(0,0)))
								NavData.SetCurrentLocation(cl);
							}
						seek_person = true;
						}
					else
						{
						error = "Attempt to face speaker to rear failed.";
						Log.LogEntry(error);
						}
					}
				if (seek_person)
					{
					HeadAssembly.Pan(pan,true);
					if (pd.NearestHCLPerson(false,ref pdd))
						{
						pdd.angle -= pan;
						if (pdd.dist <= PERSON_CLEARANCE + MIN_MOVE )
							{
							moved_to_person = true;
							direct = cl.orientation;
							direct_move = true;
							near = true;
							succeed = true;
							}
						else
							{
							Speech.SpeakAsync("okay.");
							HeadAssembly.Pan(0, true);
							Log.LogEntry("Speaker detected at angle of " + (pdd.angle).ToString("F1") + " and distance of " + pdd.dist.ToString("F1") + " in");
							if (pdd.dist < Kinect.nui.DepthStream.MaxDepth * SharedData.MM_TO_IN)
								{
								rl = NavCompute.PtDistDirectApprox(cl.coord,(int) Math.Round((cl.orientation - pdd.angle) % 360),(int) Math.Round(pdd.dist));
								Log.LogEntry("Speaker located at " + rl.coord);
								pdd.rm_location = rl.coord;
								}
							else
								{
								rl = new Room.rm_location();
								pdd.rm_location = rl.coord = new Point(0,0);
								}
							pdd.ts = SharedData.app_time.ElapsedMilliseconds;
							SpeakerData.Person = pdd;
							if (pdd.dist < PERSON_CLEARANCE + MIN_MOVE)
								{
								moved_to_person = true;
								direct = cl.orientation;
								direct_move  = true;
								near = true;
								succeed = true;
								}
							else if ((pdd.dist < Kinect.nui.DepthStream.MaxDepth * SharedData.MM_TO_IN) && PathClear(pdd))
								{
								int pdist = 0;
								string fname = "";
								bool turned = false;

								if (Math.Abs(pdd.angle) >= 2)
									{
									turned = Turn((int)Math.Round(pdd.angle), ref fname);
									direct = (int) Math.Round(cl.orientation - pdd.angle);
									if (direct < 0)
										direct += 360;
									}
								else
									{
									turned = true;
									direct = cl.orientation;
									}
								if (turned)
									{
									Speech.SpeakAsync("Coming");
									moved_to_person = MoveToPerson(ref pdist, ref fname);
									direct_move = true;
									near = true;
									}
								else
									{
									error = "Could not turn to face person.";
									Log.LogEntry(error);
									}
								}
							else if (cl.ls == NavData.LocationStatus.UNKNOWN)
								{
								error = "My location is unknown.";
								Log.LogEntry(error);
								}
							else if (pdd.rm_location.IsEmpty)
								{
								error = "Can not determine person's location.";
								Log.LogEntry(error);
								}
							else if (Navigate.PathClear(cl.coord,rl.coord))
								{
								Speech.SpeakAsync("Coming");
								moved_to_person = DirectMoveToSpeaker(cl,rl,pdd,ref direct);
								direct_move = true;
								near = true;
								}
							else if (mov.DetermineIntermediatePts(rl.coord, cl.coord, ref mptal))
								{
								if (mptal.Count > 1)
									{
									mpt = (Point)mptal[mptal.Count - 2];
									moved_to_person = PathMoveToSpeaker(cl, rl, pdd, mpt, ref direct);
									direct_move = true;
									near = true;
									}
								else
									{
									if (mov.TurnToFaceMP(rl.coord))
										{
										Speech.SpeakAsync("Coming");
										moved_to_person = DirectMoveToSpeaker(cl, rl, pdd, ref direct);
										direct_move = true;
										near = true;
										}
									else
										{
										error = "Could not turn to face speaker.";
										Log.LogEntry(error);
										}
									}
								}
							else if ((mptal.Count > 0) && mov.ReturnClosestClearPtinPath(rl.coord, cl.coord, ref mpt))
								{
								if (NavCompute.DistancePtToPt(rl.coord,mpt) < pdd.dist)
									{
									moved_to_person = MoveToObstructedSpeaker(cl, rl, pdd,mpt);
									SpeakerData.TightQuaters = true;
									}
								else
									{
									error = "Could not determine path to speaker closer then the current position.";
									Log.LogEntry(error);
									}
								}
							else if (MapCompute.FindNearestClearArea(NavData.detail_map, cl.coord, rl.coord, ref mpt))
								{
								if (mov.ReturnClosestClearPtinPath(mpt, cl.coord, ref mpt))
									{
									moved_to_person = MoveToObstructedSpeaker(cl, rl, pdd,mpt);
									SpeakerData.TightQuaters = true;
									}
								else
									{
									error = "Could not determine move path to clear point near speaker.";
									Log.LogEntry(error);
									}
								}
							else
								{
								error = "Could not determine path to speaker.";
								Log.LogEntry(error);
								}
							if (moved_to_person)
								{
								if (pd.NearestHCLPerson(near, ref pdd))
									{
									int x;
									double angle;

									Log.LogEntry("Final correction to 'face' speaker");

									x = pdd.vdo.x + pdd.vdo.width / 2;
									x = Kinect.nui.ColorStream.FrameWidth - x;
									pan = HeadAssembly.PanAngle();
									angle = Kinect.VideoHorDegrees(x - Kinect.nui.ColorStream.FrameWidth / 2);
									if (!near)
										{
										angle = pan + angle;
										}
									if (Math.Abs(angle) > 1)
										{
										Log.LogEntry("Pan: " + ((int)Math.Round(angle)));
										HeadAssembly.Pan((int)Math.Round(angle), true);
										}
									if (pdd.vdo.y > Kinect.nui.ColorStream.FrameHeight / 2)
										{
										tilt = HeadAssembly.TiltAngle();
										angle = Math.Abs(Kinect.VideoVerDegrees((Kinect.nui.ColorStream.FrameHeight / 2) - (pdd.vdo.y + pdd.vdo.height/4)));
										if (!near)
											angle = tilt + angle;
										if (angle > 1)
											{
											Log.LogEntry("Tilt: " + ((int)-Math.Round(angle)));
											HeadAssembly.Tilt((int)-Math.Round(angle), true);
											}
										}
									cl = NavData.GetCurrentLocation();
									if (cl.ls != NavData.LocationStatus.UNKNOWN)
										{
										if (direct_move)
											{
											rl = NavCompute.PtDistDirectApprox(rl.coord,(direct + 180) % 360 ,(int) Math.Round(pdd.dist));
											cl.coord = rl.coord;
											cl.orientation = direct;
											cl.ls = NavData.LocationStatus.USR;
											NavData.SetCurrentLocation(cl);
											MotionMeasureProb.UserLocalize(new MotionMeasureProb.Pose(cl.coord,cl.orientation));
											}
										else
											{
											angle = (cl.orientation + pan - pdd.angle) % 360;
											if (angle < 0)
												angle += 360;
											rl = NavCompute.PtDistDirectApprox(cl.coord,(int) Math.Round(angle),(int) Math.Round(pdd.dist));
											pdd.rm_location = rl.coord;
											}
										pdd.ts = SharedData.app_time.ElapsedMilliseconds;
										SpeakerData.Person = pdd;

										if (floc.DetermineDRLocation(ref cl, false, new Point(0, 0)))
											NavData.SetCurrentLocation(cl);
										LocationMessage();
										}
									if (cd.come_here)
										Speech.Speak("I am here.");
									succeed = true;
									}
								else
									{
									error = "Could not find the speaker after move.";
									Log.LogEntry(error);
									Speech.SpeakAsync(error);
									if (direct_move)
										{
										cl = NavData.GetCurrentLocation();
										cl.ls = NavData.LocationStatus.UNKNOWN;
										}
									}
								if (SharedData.log_operations)
									SavePersonPic();
								HeadAssembly.Tilt(0, true);
								HeadAssembly.Pan(0, true);
								}
							else
								Speech.SpeakAsync("Can not move to speaker, " + error);
							}
						}
					else
						{
						Speech.SpeakAsync("Could not find the speaker.");	//this is no person detected or the detected person is out of Kinect range
						HeadAssembly.Pan(0,true);
						}
					}
				else
					Speech.SpeakAsync(error);
				}
			else
				Speech.SpeakAsync("Could not determine speaker's direction.");
			}

			catch(Exception ex)
			{
			Speech.SpeakAsync("Come here command thread had an exception");
			Log.LogEntry("Come here command thread exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			Speech.EnableAllCommands();
		}
		


		public ComeHere()

		{
			initialized = ReadRefParameters();
		}

		}
	}
