using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using AutoRobotControl;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Blob;
using OpenCvSharp.CPlusPlus;
using Constants;


namespace Work_Assist
	{
	class AAPosition
		{
		// primary assumptions: 
		//		1. The object for "give me that" is held in a manner so it protruds to the top or side, NOT the bottom. (Not a great assumption but probably necessary)
		//		2. Given that there is only one robot and one person and the spatial relationship is known, the direction from which a hand is coming is also known

		private const int MAX_X_MATCH_DIST = 15;
		private const int POS_TARGET_OFFSET = 2;
		private const int DIST_DELTA = 102;
		private const double FT_TO_PALM = 4.5;       //these vary dependent on hand size, current based on NASA study using ~ average for finger tip to palm based on my hand
		private const double PALM_WIDTH_MIN = 2.7;   //and 5th percentale female for min palm width
		private const double PALM_OVERSHOOT = 1.5;
		private const int MIN_BLOB_AREA = 2000;

		public struct HandDetectData
			{
			public bool hand_detected;
			public Arm.Loc3D target_loc;
			public Point pic_loc;
			public SharedData.RobotLocation from;
			};

		private byte[] bdata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
		private short[] bsdata = new short[Kinect.nui.DepthStream.FramePixelDataLength];
		private AutoRobotControl.HandDetect hd = new AutoRobotControl.HandDetect();
		private Bitmap bbm;
		private HandDetectData hdd = new HandDetectData();
		private double bangle,maxhy,py;


		private void PositionThread(Object obj)

		{
			string err = "";
			Arm.Loc3D target_loc;
			bool pos = false;
			double mdist = 0;

			try
			{
			if (AAShare.arm_pos == AAShare.position.PARK)
				{
				pos = Arm.StartPos(AAShare.start_pos[0],AAShare.start_pos[1]);
				if (pos)
					{
					AAShare.arm_pos = AAShare.position.START;
					Thread.Sleep(1000);
					}
				}
			if (AAShare.arm_pos == AAShare.position.START)
				pos = AAShare.RawMoveEntryExitPt();
			else if ((AAShare.arm_pos == AAShare.position.ENTRY_EXIT) || (AAShare.arm_pos == AAShare.position.IN_WS))
				pos = true;
			if (pos)
				{
				target_loc = (Arm.Loc3D) obj;
				if (Arm.PositionArm(target_loc.x,target_loc.y,target_loc.z,POS_TARGET_OFFSET,AAShare.wrist_inline,ref mdist,ref err))
					{
					AAShare.arm_pos = AAShare.position.IN_WS;
					Thread.Sleep((int) Math.Round((mdist/Arm.ARM_MOVE_RATE) * 1000));
					}
				else
					Speech.SpeakAsync("arm move failed, " + err);
				}
			else
				{
				Speech.SpeakAsync("arm move failed, could not move to workspace entry position.");
				}
			}

			catch(Exception ex)
			{
			SkillShared.OutputSpeech("Position failed with exception " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			AAShare.pos = null;
		}



		private bool RectContainsPoint(int y,int x,CvRect rect)

		{
			bool rtn = false;

			if (x >= rect.X)
				if (x <= rect.X + rect.Width)
					if (y >= rect.Y)
						if (y <= rect.Y + rect.Height)
							rtn = true;
			return(rtn);
		}



		private bool RectOverlap(Rectangle hand,Rectangle robot_arm,ref Rectangle search)

		{
			bool rtn = true;
			int x_left,x_right,y_top,y_bottom;

			x_left = Math.Max(hand.X,robot_arm.X);
			x_right = Math.Min(hand.X + hand.Width,robot_arm.X + robot_arm.Width);
			y_top = Math.Max(hand.Y,robot_arm.Y);
			y_bottom = Math.Min(hand.Y + hand.Height,robot_arm.Y + robot_arm.Height);
			if ((x_right < x_left) || (y_bottom < y_top))
				rtn = false;
			else
				{
				if (robot_arm.X > hand.X)
					{
					search.X = hand.X;
					search.Y = hand.Y;
					search.Width = hand.Width - (x_right - x_left);
					search.Height = hand.Height;
					}
				else if (robot_arm.Y < hand.Y)
					{
					search.X = hand.X;
					search.Y = hand.Y;
					search.Width = hand.Width;
					search.Height = hand.Height - (y_bottom - y_top);
					}
				}
			return(rtn);
		}



		private bool BuildHandBlob(HandDetect.hand_data hdata,Rectangle search,ref CvRect rec)

		{
			bool rtn = false;
			int i,j,mdist = 4000,odist,screen_size,pixel,mdx = 0,mdy = 0;
			string fname;
			DateTime now = DateTime.Now;
			CvBlob b;
			Graphics g;
			double cdist, ray, rax, fx;
			Arm.Loc3D floc;

			Log.LogEntry("Hand blob distance search rectangle: " + search);
			for (i = search.X;i < search.X + search.Width;i++)
				for (j = search.Y; j < search.Y + search.Height;j++)
					{
					pixel = (j * Kinect.nui.ColorStream.FrameWidth) + i;
					odist = SkillShared.dips[pixel].Depth;
					if ((odist > 0) && (odist < mdist))
						{
						mdist = odist;
						mdx = i;
						mdy = j;
						}
					}
			screen_size = Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth;
			for (i = 0;i < screen_size;i++)
				{
				bdata[4 * i] = 0;
				bdata[(4 * i) + 1] = 0;
				bdata[(4 * i) + 2] = 0;
				bsdata[i] = -1;
				}
			maxhy = 0;
			for (i = hdata.vdo.x;i < hdata.vdo.x + hdata.vdo.width;i++)
				for (j = hdata.vdo.y; j < hdata.vdo.y + hdata.vdo.height;j++)
					{
					pixel = (j * Kinect.nui.ColorStream.FrameWidth) + i;
					odist = SkillShared.dips[pixel].Depth;
					if ((odist > 0) && (odist <= mdist + DIST_DELTA))
						{
						cdist = Kinect.CorrectedDistance(odist * SharedData.MM_TO_IN);
						ray = Kinect.VideoVerDegrees((int)Math.Round(((double) Kinect.nui.ColorStream.FrameHeight / 2)) - j);
						rax = Kinect.VideoHorDegrees(i - (Kinect.nui.ColorStream.FrameWidth / 2));
						fx = cdist * Math.Tan(rax * SharedData.DEG_TO_RAD);
						floc = Arm.MapKCToRC(fx, cdist,AAShare.ARM_KINECT_TILT_CORRECT ,ray);
						if (floc.y > maxhy)
							maxhy = floc.y;
						if (floc.y > SkillShared.wsd.top_height + AAShare.TOP_HEIGHT_CLEAR)
							{
							bdata[4 * pixel] = 255;
							bdata[(4 * pixel) + 1] = 255;
							bdata[(4 * pixel) + 2] = 255;
							bsdata[pixel] = (short) odist;
							}
						}
					}
			bbm = bdata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
			bbm.RotateFlip(RotateFlipType.Rotate180FlipY);
			SkillShared.pic = bbm.ToIplImage();
			Cv.CvtColor(SkillShared.pic, SkillShared.gs, ColorConversion.BgrToGray);
			SkillShared.blobs.Label(SkillShared.gs, SkillShared.img);
			b = SkillShared.blobs[SkillShared.blobs.GreaterBlob()];
			if (b.Area >= MIN_BLOB_AREA)
				{
				rtn = true;
				b.CalcCentralMoments(SkillShared.img);
				bangle = 90 + (b.CalcAngle() * SharedData.RAD_TO_DEG);
				rec = b.Rect;
				Log.LogEntry("Hand orientation: " + bangle.ToString("F2") + " °");
				Log.LogEntry("Hand max height: " + maxhy.ToString("F2"));
				if (SharedData.log_operations)
					{
					fname = Log.LogDir() + "Hand blob pic " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + " - " + SharedData.GetUFileNo() + ".jpg";
					g = Graphics.FromImage(bbm);
					g.DrawRectangle(Pens.Red, b.Rect.X, b.Rect.Y, b.Rect.Width, b.Rect.Height);
					bbm.Save(fname,ImageFormat.Jpeg);
					Log.LogEntry("Saved: " + fname);
					}
				}
			else
				Log.LogEntry("Hand blob area " + b.Area + " indicates blob failure.");
			return (rtn);
		}



		private Arm.Loc3D FindFingerLocation(CvRect rect,SharedData.RobotLocation from,ref Point pt)

		{	//handles left or right from any angle but not from above which requires a row search rather then a col based search
			Arm.Loc3D floc = new Arm.Loc3D(0,0,0);
			int col,start,i,j,pixel,inc,basey;
			double cdist,ray,rax,fx;
			Graphics g;
			DateTime now = DateTime.Now;
			string fname;
			bool ftipfound = false;

			if (from == SharedData.RobotLocation.RIGHT)
				col = rect.X + 1;
			else
				col = rect.X + rect.Width - 2;
			start = (rect.Y * Kinect.nui.ColorStream.FrameWidth) + col;
			if (Math.Abs(bangle) > 65)
				{
				start = (rect.Y * Kinect.nui.ColorStream.FrameWidth) + col;
				basey = rect.Y;
				inc = 1;
				}
			else
				{
				start = ((rect.Y + rect.Height) * Kinect.nui.ColorStream.FrameWidth) + col;
				basey = rect.Y + rect.Height;
				inc = -1;
				}
			for (j = start;j < start + 2;j++)
				{
				for (i = 0;Math.Abs(i) < rect.Height;i+=inc)
					{
					pixel = ((basey + i) * Kinect.nui.ColorStream.FrameWidth) + (Kinect.nui.ColorStream.FrameWidth - (col + (j - start)) - 1);
					if (bsdata[pixel] > 0)
						{
						cdist = Kinect.CorrectedDistance(bsdata[pixel] * SharedData.MM_TO_IN);
						ray = Kinect.VideoVerDegrees((int)Math.Round(((double)Kinect.nui.ColorStream.FrameHeight / 2)) - (basey + i));
						rax = Kinect.VideoHorDegrees((col + (j - start)) - (Kinect.nui.ColorStream.FrameWidth / 2));
						fx = cdist * Math.Tan(rax * SharedData.DEG_TO_RAD);
						floc = Arm.MapKCToRC(fx,cdist,AAShare.ARM_KINECT_TILT_CORRECT, ray);
						Log.LogEntry("Finger tip @ RC (" + floc.x.ToString("F1") + "," + floc.y.ToString("F1") + "," + floc.z.ToString("F2") + ")");
						pt = new Point((col + (j - start)), (basey + i));
						Log.LogEntry("Finger tip @ x " + pt.X + "  y " + pt.Y);
						if (SharedData.log_operations)
							{
							g = Graphics.FromImage(bbm);
							g.FillRectangle(Brushes.LightGreen,pt.X -1 ,pt.Y - 1, 2, 2);
							fname = Log.LogDir() + "Finger tip blob pic " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + " - " + SharedData.GetUFileNo() + ".bmp";
							bbm.Save(fname, ImageFormat.Bmp);
							Log.LogEntry("Saved: " + fname);
							}
						ftipfound = true;
						break;
						}
					}
				if (ftipfound)
					break;
				}
			return(floc);
		}



		private bool HandDetect(ref HandDetectData hdd)

		{
			Stopwatch sw = new Stopwatch();
			bool pos_deter = false,blob_built = false;
			int i;
			CvRect rect = new CvRect();
			Arm.Loc3D floc;
			Point ftpt = new Point();
			ArrayList hands = new ArrayList();
			ArrayList robot_arms = new ArrayList();
			Bitmap bm;
			Rectangle search = new Rectangle(),robarm = new Rectangle();

			Log.LogEntry("HandDetect");
			hdd.hand_detected = false;
			hdd.target_loc = new Arm.Loc3D(0, 0, 0);
			sw.Start();
			if (AAShare.arm_pos == AAShare.position.IN_WS)
				{
				hd.DetectHandRobotArm(ref SkillShared.videodata, ref SkillShared.dips,ref hands,ref robot_arms);
				}
			else
				{
				hands = hd.DetectHands(ref SkillShared.videodata, ref SkillShared.dips);
				}
			if ((hands.Count > 0) && (robot_arms.Count <= 1))
				{
				bm = SkillShared.videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth,Kinect.nui.ColorStream.FrameHeight);
				if (robot_arms.Count > 0)
					{
					robarm.X = ((HandDetect.hand_data) robot_arms[0]).vdo.x;
					robarm.Y = ((HandDetect.hand_data)robot_arms[0]).vdo.y;
					robarm.Width = ((HandDetect.hand_data)robot_arms[0]).vdo.width;
					robarm.Height = ((HandDetect.hand_data)robot_arms[0]).vdo.height;
					}
				for (i = 0;i < hands.Count;i++)
					{
					search.X = ((HandDetect.hand_data) hands[i]).vdo.x;
					search.Y = ((HandDetect.hand_data)hands[i]).vdo.y;
					search.Width = ((HandDetect.hand_data)hands[i]).vdo.width;
					search.Height = ((HandDetect.hand_data)hands[i]).vdo.height;
					if (robot_arms.Count > 0)
						{
						if (RectOverlap(search,robarm,ref search))
							Log.LogEntry("Hand " + i + " and robot arm bounding boxes overlap.");
						}
					blob_built = BuildHandBlob((HandDetect.hand_data) hands[i],search, ref rect);
					if (blob_built)
						{
						if (SkillShared.wsd.side == SharedData.RobotLocation.LEFT)
							hdd.from = SharedData.RobotLocation.RIGHT;
						else if (SkillShared.wsd.side == SharedData.RobotLocation.RIGHT)
							hdd.from = SharedData.RobotLocation.LEFT;
						else if (SkillShared.wsd.side == SharedData.RobotLocation.FRONT)
							hdd.from = SkillShared.wsd.side;
						floc = FindFingerLocation(rect,hdd.from,ref ftpt);
						if ((floc.x != 0) || (floc.y != 0) || (floc.z != 0))
							{
							if (AAMap.WithinWorkSpace(floc))
								{
								Log.LogEntry("Finger tip is within work space.");
								hdd.target_loc = floc;
								hdd.pic_loc = ftpt;
								pos_deter = true;
								break;
								}
							else
								Log.LogEntry("Finger tip is not within work space.");
							}
						}
					}
				if (!pos_deter)
					{
					Log.LogEntry("Could not determine position.");
					Speech.SpeakAsync("Could not determine position.");
					}
				}
			else if (hands.Count == 0)
				Log.LogEntry("No hands detected.");
			else
				Log.LogEntry("Error, " + robot_arms.Count +  " robot arms detected.");
			sw.Stop();
			Log.LogEntry("Execution time (msec): " + sw.ElapsedMilliseconds);
			return (pos_deter);
		}



		public void Position()

		{
			Speech.SpeakAsync("okay");
			if (AAShare.Shoot(true))
				{
				if (HandDetect(ref hdd))
					{
					object obj;

					AAMap.SaveMap("detected hand move map ");
					AAShare.pos = new Thread(PositionThread);
					obj = hdd.target_loc;
					AAShare.pos.Start(obj);
					}
				else
					Speech.SpeakAsync("I could not detect a hand within my work space.");
				}
			else
				Speech.SpeakAsync("Could not obtain visual.");
		}



		private bool ArmDown(double dist)

		{
			bool rtn = false;
			string error = "";
			Arm.Loc3D loc;

			loc = Arm.CurrentPositionCorrected();
			loc.y -= dist;
			if ((loc.y > SkillShared.wsd.top_height + AAShare.TOP_HEIGHT_CLEAR) && (Arm.IncrementalPostionArmOk(loc.x,loc.y,loc.z,true)))
				{
				if (Arm.IncrementalPositionArm(loc.x,loc.y,loc.z,AutoArm.INCREMENTAL_MOVE_TIME,true, ref error))
					rtn = true;
				}
			return (rtn);
		}



		private void GiveMeThatThread(Object obj)

		{
			string err = "";
			Arm.Loc3D target_loc;
			bool pos = false;
			double mdist = 0,ddist;
			string reply;
			int pwm;

			try
				{
				if (AAShare.arm_pos == AAShare.position.PARK)
					{
					pos = Arm.StartPos(AAShare.start_pos[0], AAShare.start_pos[1]);
					if (pos)
						{
						AAShare.arm_pos = AAShare.position.START;
						Thread.Sleep(1000);
						}
					}
				if (AAShare.arm_pos == AAShare.position.START)
					pos = AAShare.RawMoveEntryExitPt();
				else if ((AAShare.arm_pos == AAShare.position.ENTRY_EXIT) || (AAShare.arm_pos == AAShare.position.IN_WS))
					pos = true;
				if (pos)
					{
					target_loc = (Arm.Loc3D) obj;
					pwm = Arm.ServoPwm(Arm.GROTATE_CHANNEL);
					if (pwm != Arm.GROTATE_PERP)
						{
						Arm.Position(Arm.GROTATE_CHANNEL, Arm.GROTATE_PERP, Arm.GROTATE_SPEED);
						Thread.Sleep(1000);
						}
					if (Arm.PositionArm(target_loc.x, target_loc.y, target_loc.z,0,false, ref mdist, ref err))
						{
						double wrist_dip;
						const int WRIST_TURN = 45;

						AAShare.arm_pos = AAShare.position.IN_WS;
						Thread.Sleep((int)Math.Round((mdist / Arm.ARM_MOVE_RATE) * 1000));
						wrist_dip = Math.Cos(WRIST_TURN * SharedData.DEG_TO_RAD) * Arm.L4;
						ddist = target_loc.y - py - wrist_dip ;
						if (ddist > 0)
							ArmDown(ddist);													//correct distance above palm
						pwm = Arm.ServoPwm(Arm.WRIST_CHANNEL);							//wrist turn down to bring part closer to hand
						pwm = pwm - (int) Math.Round(WRIST_TURN * Arm.WRIST_PULSE_PD);
						Arm.Position(Arm.WRIST_CHANNEL,pwm,Arm.WRIST_SPEED);
						Thread.Sleep(500);
						do
							reply = Speech.Conversation("Are you ready to take the object?", "responseyn", 10000, false);
						while (reply != "yes");
						Arm.Position(Arm.GRIP_CHANNEL, Arm.GRIP_OPEN, Arm.GRIP_SPEED);
						Thread.Sleep(1000);
						Arm.CloseGrip();
						}
					else
						Speech.SpeakAsync("arm move failed, " + err);
					}
				else
					{
					Speech.SpeakAsync("arm move failed, could not move to workspace entry position.");
					}
				}

			catch (Exception ex)
				{
				SkillShared.OutputSpeech("Position failed with exception " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

			AAShare.pos = null;
		}



		private bool OpenHandDetect(ref HandDetectData hdd)

		{
			bool rtn = false,palm_loced = false;
			int row,col,i,frow,nrow,x,z;
			Arm.Loc3D loc,palm_loc,last;
			double dy,dz,dx,near,far;
			const double EXCESS_CHG = 1;
			SkillShared.Dpt start,curr;
			const int MOVE_CLEAR = 2;

			Log.LogEntry("OpenHandDetect");
			if (HandDetect(ref hdd))
				{
				row = hdd.pic_loc.Y;
				col = hdd.pic_loc.X;
				loc = SkillShared.DPtLocation(row,col,AAShare.ARM_KINECT_TILT_CORRECT);
				start = new SkillShared.Dpt(loc.x,loc.z);
				dx = Math.Sin(bangle * SharedData.DEG_TO_RAD);
				dz = -Math.Cos(bangle * SharedData.DEG_TO_RAD);
				if (hdd.from == SharedData.RobotLocation.RIGHT)
					{
					for (i = 1;;i++)
						{
						x = (int)Math.Round(col + (i * dx));
						z = (int)Math.Round(row + (i * dz));
						loc = SkillShared.DPtLocation(z,x,AAShare.ARM_KINECT_TILT_CORRECT);
						if (loc.y > 0)
							{
							curr = new SkillShared.Dpt(loc.x,loc.z);
							if (SkillShared.DistanceDPtToDPt(start,curr) >= FT_TO_PALM)
								{
								col = x;
								row = z;
								palm_loc = loc;
								palm_loced = true;
								break;
								}
							}
						}
					}
				else if (hdd.from == SharedData.RobotLocation.LEFT)
					{
					for (i = -1; ; i--)
						{
						x = (int)Math.Round(col + (i * dx));
						z = (int)Math.Round(row + (i * dz));
						loc = SkillShared.DPtLocation(z,x, AAShare.ARM_KINECT_TILT_CORRECT);
						if (loc.y > 0)
							{
							curr = new SkillShared.Dpt(loc.x, loc.z);
							if (SkillShared.DistanceDPtToDPt(start,curr) > FT_TO_PALM)
								{
								col = x;
								row = z;
								palm_loc = loc;
								palm_loced = true;
								break;
								}
							}
						}
					}
				else
					palm_loc = new Arm.Loc3D();
				if (palm_loced)
					{
					last = palm_loc;
					for (i = 1;;i++)
						{
						loc = SkillShared.DPtLocation(row + i,col,AAShare.ARM_KINECT_TILT_CORRECT);
						dy = loc.y - last.y;
						dz = loc.z - last.z;
						if ((Math.Abs(dy) > EXCESS_CHG) && (Math.Abs(dz) > EXCESS_CHG))
							{
							near = last.z;
							nrow = row + i -1;
							break;
							}
						last = loc;
						}
					last = palm_loc;
					for (i = -1; ; i--)
						{
						loc = SkillShared.DPtLocation(row + i, col,AAShare.ARM_KINECT_TILT_CORRECT);
						dy = loc.y - last.y;
						dz = loc.z - last.z;
						if ((Math.Abs(dy) > EXCESS_CHG) && (Math.Abs(dz) > EXCESS_CHG))
							{
							far = last.z;
							frow = row + i + 1;
							break;
							}
						last = loc;
						}
					dz = far - near;
					if (dz > PALM_WIDTH_MIN)
						{
						double angle = 0;

						Log.LogEntry("Palm @ " + col + "," + (nrow + ((frow - nrow)/2)));
						palm_loc = SkillShared.DPtLocation((nrow + ((frow - nrow) / 2)), col, AAShare.ARM_KINECT_TILT_CORRECT);
						py = palm_loc.y;
						Log.LogEntry("Palm location (RC) is " + palm_loc);
						//delivery is to above the drop point and beyond so can clear hand and rotate wrist ~ 45 degrees prior to release
						//what if object sticks out the bottom signficantly?
						palm_loc.y = maxhy + Arm.OBS_HEIGHT_CLEAR + MOVE_CLEAR;
						if (AAMap.ArmAngle(palm_loc,ref angle))
							{
							palm_loc.x += PALM_OVERSHOOT * Math.Sin(angle * SharedData.DEG_TO_RAD);
							palm_loc.z += PALM_OVERSHOOT * Math.Cos(angle * SharedData.DEG_TO_RAD);
							}
						else
							palm_loc.z += PALM_OVERSHOOT;
						Log.LogEntry("Delivery location (RC) is " + palm_loc);
						if (AAMap.WithinWorkSpace(palm_loc))
							{
							Log.LogEntry("Palm is within work space");
							hdd.target_loc = palm_loc;
							rtn = true;
							}
						else
							{
							Log.LogEntry("Delivery location is not within work space.");
							Speech.SpeakAsync("I can not delivery the object without going outside my work space.");
							}
						}
					else
						{
						Log.LogEntry("Palm width of " + (far - near).ToString("F1") + " in. indicates hand orientation is wrong.");
						Speech.SpeakAsync("Your palm width indicates your hand orientation is wrong.");
						}
					}
				else
					{
					Log.LogEntry("Could not locate palm.");
					Speech.SpeakAsync("Could not locate palm.");
					}
				}
			else
				Speech.SpeakAsync("I could not detect a hand within my work space.");
			return (rtn);
		}



		public void GiveMeThat(AutoArm ata)

		{
			Speech.SpeakAsync("okay");
			if (AAShare.Shoot(true))
				{
				if (OpenHandDetect(ref hdd))
					{
					object obj;

					AAMap.SaveMap("detected open hand move map ");
					AAShare.pos = new Thread(GiveMeThatThread);
					obj = hdd.target_loc;
					AAShare.pos.Start(obj);
					}
				}
			else
				Speech.SpeakAsync("Could not obtain visual.");
		}

		}
	}
