using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
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

	class AutoArm
		{

		// primary assumptions: 
		//		1. A "yes" reply to "Is it okay for me to map the work area surface?" means that the work area is clear. (Not a great assumption but probably necessary)
		//		2. The space between the robot and the work area is clear at the work top level. (reasonable assumption)
		//		3. The start position is clear of the robot's vision but such that it can safely go to and from a work top higher then the robot perch
		//	How aware of human position (other then in work space) does it need to be?  Is move only with speaker control, only "position" the arm within the work space and never "position" the arm without obstacle check sufficent? Is it too limiting?
		// additional context:
		//		1. gross position of arm (parked,at start location,at entry/exit location, in the workspace)
		//		2. wrist in line or not
		//		3. base and current work space maps
		//	The height above the work space top that can be seen is limited because of the heigth of the bench top and the Kinect tilt angle of - 55 (with correction -59).

		private const string GRAMMAR = "autoarm";
		private const int HEIGHT_ABOVE = 8;
		public const int INCREMENTAL_MOVE_TIME = 500;


		public struct dpt_to_dpt_data
		{
		public double direct;
		public double dist;
		};


		private string grammar_file = Application.StartupPath + SharedData.CAL_SUB_DIR + GRAMMAR + ".xml";
		private ManualResetEvent aae = new ManualResetEvent(false);
		private string last_repeat_cmd = "";
		private int grotate_angle;
		private ArmManualMode amm = new ArmManualMode();
		private AAPosition aap = new AAPosition();


		public void Work()

		{

			try
			{
			if (SetUpArmOperation())
				{
				aae.WaitOne();
				CloseArmOperation();
				}
			}

			catch(Exception ex)
			{
			SkillShared.OutputSpeech("Exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			CloseArmOperation();
			}

		}


		
		private bool SetUpArmOperation()

		{
			bool rtn;
			string reply;

			if ((rtn = Arm.OpenServoController()))
				{

				try
				{
				Arm.RegisterMapDelegates(AAMap.ObstacleCheck, AAMap.SaveMap, AAMap.CorrectMoveMap,MoveExitStartPt);
				AAMap.InitWsMap();
				Kinect.SetNearRange();
				do
					{
					reply = Speech.Conversation("Is it okay for me to map the work area surface?", "responseyn", 10000, false);
					if (reply != "yes")
						Thread.Sleep(10000);
					}
				while (reply != "yes");
				AddGrammar();
				Speech.AddGrammerHandler(grammar_file);
				Speech.RegisterHandler(GRAMMAR, SpeechHandler, null);
				HeadAssembly.Tilt(AAShare.ARM_KINECT_TILT,true);
				if (ShootInitial())
					{
					if (Arm.StartPos(AAShare.start_pos[0], AAShare.start_pos[1]))	
						{
						AAShare.arm_pos = AAShare.position.START;
						Thread.Sleep(1000);
						Speech.Speak("Ready to work.");
						SkillShared.RecordWorkSpaceData("Work space data ready to work");
						rtn = true;
						AAShare.handle_speech = true;
						Speech.EnableCommand(GRAMMAR);
						}
					else
						{
						rtn = false;
						SkillShared.OutputSpeech("Could not position arm to its start position.");
						CloseArmOperation();
						}
					}
				else
					{
					SkillShared.OutputSpeech("Could not create initial work space map");
					CloseArmOperation();
					rtn = false;
					}
				}

				catch(Exception ex)
				{
				SkillShared.OutputSpeech("Exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				CloseArmOperation();
				rtn = false;
				}

				}
			else
				SkillShared.OutputSpeech("Could not open arm servo controller.");
			return (rtn);
		}



		private void CloseArmOperation()

		{
			if (AAShare.arm_pos == AAShare.position.IN_WS)
				AAShare.Shoot(true);
			if ((AAShare.arm_pos == AAShare.position.IN_WS) || (AAShare.arm_pos == AAShare.position.ENTRY_EXIT))
				Arm.StopPos();
			else if (AAShare.arm_pos == AAShare.position.START)
				Arm.EPark();
			AAShare.arm_pos = AAShare.position.PARK;
			Kinect.SetFarRange();
			Arm.CloseServoController();
			Arm.UnRegisterMapDelegates();
			HeadAssembly.Tilt(0, true);
			HeadAssembly.Pan(0, true);
			Speech.UnRegisterHandler(GRAMMAR);
			Speech.UnloadGrammar(GRAMMAR);
			Speech.Speak("Arm operation closed.");
		}



		private bool ShootInitial()
		
		{
			bool rtn = false;

			if (Kinect.GetDepthFrame(ref SkillShared.depthdata, 40))
				{
				Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, SkillShared.depthdata, SkillShared.dips);
				SkillShared.SaveDipsData("Workspace depth data ", SkillShared.dips);
				if (DetermineWorkPlaceHeight())
					{
					if (AAMap.MapWorkPlace())
						{
						SkillShared.wsd.LogWSD();
						AAMap.base_wp_map = (short[]) AAMap.wp_map.Clone();
						AAMap.SaveMap("Work area base map ");
						rtn = true;
						}
					else
						SkillShared.OutputSpeech("Could not create the base work space map");
					}
				else
					SkillShared.OutputSpeech("Could not determine the work space height.");
				}
			else
				SkillShared.OutputSpeech("Could not obtain a depth frame.");
			return(rtn);
		}



		private bool GrippingObject()

		{
			bool rtn = false;
			int pwm,value;

			pwm = Arm.ServoPwm(Arm.GRIP_CHANNEL);
			value = Arm.ReadAnalogInput();
			if ((pwm != Arm.GRIP_OPEN) && (pwm != Arm.GRIP_CLOSE) && (value >= Arm.MAX_GRIP_VALUE))
				rtn = true;
			return (rtn);
		}


		

		private bool DetermineWorkPlaceHeight()

		{
			double height = 0;
			int row, col,samples = 0,region_hits = 0;
			Arm.Loc3D loc;
			bool rtn = false;

			for (row = 0; row < Kinect.nui.ColorStream.FrameHeight; row++)
				{
				for (col = 0; col < Kinect.nui.ColorStream.FrameWidth; col++)
					{
					loc = SkillShared.DPtLocation(row, col, AAShare.ARM_KINECT_TILT_CORRECT);
					if ((col == 320) && (row == 240))
						Log.LogEntry("Center values: " + loc.ToString());
					if (loc.y > SkillShared.wsd.top_height - SkillShared.TOP_MAGRIN)
						{
						region_hits += 1;
						if (AAMap.WithinWorkSpaceXZ(loc))
							{
							samples += 1;
							height += loc.y;
							}
						}
					}
				}
			if (samples > 0)
				height /= samples;
			if ((height != double.NaN) && (Math.Abs(SkillShared.wsd.top_height - height) <= AAShare.TOP_HI_LO_DIFF))
				{
				rtn = true;
				SkillShared.wsd.top_height = height;
				Log.LogEntry("Work space top height of " + SkillShared.wsd.top_height.ToString("F2") + " in. determined with " + samples + " samples");
				}
			else
				Log.LogEntry("Could not determine work space top height.  Samples - " + samples + "  Region hits - " + region_hits + "  Height - " + height);
			return (rtn);
		}



		private  dpt_to_dpt_data DetermineDirectDistPtToPt(SkillShared.Dpt to_pt,SkillShared.Dpt from_pt,bool log)

		{

			double dy, dx;
			dpt_to_dpt_data rtn;
			double ra;

			dy = to_pt.Y - from_pt.Y;
			dx = to_pt.X - from_pt.X;
			if (dy == 0)
				{
				if (dx > 0)
					rtn.direct = 90;
				else
					rtn.direct = 270;
				}
			else
				{
				ra =Math.Atan((dx / dy) * SharedData.RAD_TO_DEG);
				if (dy > 0)
					rtn.direct= (180 - ra) % 360;
				else
					rtn.direct = (360 - ra) % 360;
				}
			rtn.dist = Math.Sqrt((dx * dx) + (dy * dy));

			if (log)
				{
				Log.LogEntry("DetermineDirectDistPtToPt from " + from_pt + " to " + to_pt);
				Log.LogEntry("DetermineDirectDistPtToPt data: direct - " + rtn.direct + "  dist - " + rtn.dist);
				}
			return(rtn);
		}



		private bool MoveExitStartPt()

		{
			bool rtn = false;

			if (AAShare.MoveExitPt())
				{
				if ((rtn = Arm.StartPosOnly(AAShare.start_pos[0], AAShare.start_pos[1])))
					{
					AAShare.arm_pos = AAShare.position.START;
					Thread.Sleep(1000);
					}
				}
			return (rtn);
		}



		private bool ArmXMove(bool right)

		{
			bool rtn = false;
			int dx = 0;
			string error = "";
			Arm.Loc3D loc;

			if (right)
				dx = 1;
			else
				dx = -1;
			loc = Arm.CurrentPositionCorrected();
			loc.x += dx;
			if (AAMap.WithinWorkSpace(loc,true) && Arm.IncrementalPostionArmOk(loc.x,loc.y,loc.z,true))
				{
				Speech.SpeakAsync("okay");
				if (Arm.IncrementalPositionArm(loc.x,loc.y, loc.z,INCREMENTAL_MOVE_TIME ,true, ref error))
					{
					if (right)
						last_repeat_cmd = "right";
					else
						last_repeat_cmd = "left";
					rtn = true;
					}
				else
					{
					Speech.SpeakAsync("Arm position error: " + error);
					last_repeat_cmd = "";
					}
				}
			else
				{
				Speech.SpeakAsync("Can not move arm as requested.");
				last_repeat_cmd = "";
				}
			return (rtn);
		}



		private bool ArmYMove(bool up)

		{
			bool rtn = false;
			int dy = 0;
			string error = "";
			Arm.Loc3D loc;

			if (up)
				dy = 1;
			else
				dy = -1;
			loc = Arm.CurrentPositionCorrected();
			loc.y += dy;
			if ((loc.y > SkillShared.wsd.top_height + AAShare.TOP_HEIGHT_CLEAR) && (Arm.IncrementalPostionArmOk(loc.x,loc.y,loc.z,true)))
				{
				Speech.SpeakAsync("okay");
				if (Arm.IncrementalPositionArm(loc.x,loc.y,loc.z,INCREMENTAL_MOVE_TIME,true, ref error))
					{
					if (up)
						last_repeat_cmd = "up";
					else
						last_repeat_cmd = "down";
					rtn = true;
					}
				else
					{
					Speech.SpeakAsync("Arm position error: " + error);
					last_repeat_cmd = "";
					}
				}
			else
				{
				Speech.SpeakAsync("Can not move arm as requested.");
				last_repeat_cmd = "";
				}
			return (rtn);
		}



		private bool ArmZMove(bool forward)

		{
			bool rtn = false;
			int dz = 0;
			string error = "";
			Arm.Loc3D loc;

			if (forward)
				dz = 1;
			else
				dz = -1;
			loc = Arm.CurrentPositionCorrected();
			loc.z += dz;
			if (AAMap.WithinWorkSpace(loc,true) && Arm.IncrementalPostionArmOk(loc.x,loc.y,loc.z,true))
				{
				Speech.SpeakAsync("okay");
				if (Arm.IncrementalPositionArm(loc.x,loc.y,loc.z,INCREMENTAL_MOVE_TIME,true, ref error))
					{
					if (forward)
						last_repeat_cmd = "forward";
					else
						last_repeat_cmd = "back";
					rtn = true;
					}
				else
					{
					Speech.SpeakAsync("Arm position error: " + error);
					last_repeat_cmd = "";
					}
				}
			else
				{
				Speech.SpeakAsync("Can not move arm as requested.");
				last_repeat_cmd = "";
				}
			return (rtn);
		}



		private bool More()

		{
			bool rtn = false;

			if (last_repeat_cmd.Length > 0)
				{
				switch(last_repeat_cmd)
					{
					case "right":
						ArmXMove(true);
						break;

					case "left":
						ArmXMove(false);
						break;

					case "up":
						ArmYMove(true);
						break;

					case "down":
						ArmYMove(false);
						break;

					case "forward":
						ArmZMove(true);
						break;

					case "back":
						ArmZMove(false);
						break;

					case "rotate right":
						GripRotate("right");
						break;

					case "rotate left":
						GripRotate("left");
						break;

					default:
						Speech.SpeakAsync("The command to repeat, " + last_repeat_cmd + ", can not be executed.");
						last_repeat_cmd = "";
						break;
					}
				}
			else
				Speech.SpeakAsync("There is no command to repeat.");
			return(rtn);
		}



		private bool GripRotate(string dir)

		{
			bool rtn = false;
			int nangle,pwm;
			const int ROTATE_ANGLE = 5;
			
			if (dir == "right")
				{
				if (grotate_angle > 0)
					{
					Speech.SpeakAsync("okay");
					if (grotate_angle <= ROTATE_ANGLE)
						{
						Arm.Position(Arm.GROTATE_CHANNEL, Arm.GROTATE_PAR, Arm.GROTATE_SPEED);
						grotate_angle = 0;
						last_repeat_cmd = "";
						}
					else
						{
						nangle = grotate_angle - ROTATE_ANGLE;
						pwm = Arm.GRotateAngleToPwm(nangle);
						Arm.Position(Arm.GROTATE_CHANNEL,pwm, Arm.GROTATE_SPEED);
						grotate_angle = nangle;
						}
					}
				}
			else if (dir == "left")
				{
				if (grotate_angle < 90)
					{
					Speech.SpeakAsync("okay");
					if (grotate_angle >= 90 -ROTATE_ANGLE)
						{
						Arm.Position(Arm.GROTATE_CHANNEL, Arm.GROTATE_PERP, Arm.GROTATE_SPEED);
						grotate_angle = 90;
						last_repeat_cmd = "";
						}
					else
						{
						nangle = grotate_angle + ROTATE_ANGLE;
						pwm = Arm.GRotateAngleToPwm(nangle);
						Arm.Position(Arm.GROTATE_CHANNEL,pwm, Arm.GROTATE_SPEED);
						grotate_angle = nangle;
						}
					}
				}
			else
				{
				Speech.SpeakAsync("Can not rotate as directed.");
				last_repeat_cmd = "";
				}
			return(rtn);
		}



		public void SpeechHandler(string msg)

		{
			string[] words;
			string reply;

			if ((AAShare.handle_speech) && ((AAShare.pos == null) || (!AAShare.pos.IsAlive)))
				{
				try
				{
				if (msg == "we are finished")
					{
					last_repeat_cmd = "";
					if (GrippingObject())
						{
						Speech.SpeakAsync("I am still holding an object.");
						}
					else
						{
						Speech.SpeakAsync("okay");
						aae.Set();
						}
					}
				else if (msg == "arm off")
					{
					Speech.SpeakAsync("okay");
					Arm.ArmOff();
					AAShare.arm_pos = AAShare.position.PARK;
					}
				else if (msg ==  "shoot")
					{
					last_repeat_cmd = "";
					Speech.SpeakAsync("okay");
					if (AAShare.Shoot(true))
						{
						reply = Speech.Conversation("Should I save the work space map as the base map?", "responseyn", 10000,true);
						if (reply == "yes")
							{
							AAMap.base_wp_map = (short[]) AAMap.wp_map.Clone();
							Speech.SpeakAsync("Map saved.");
							}
						else
							Speech.SpeakAsync("okay");
						}
					}
				else if (msg == "manual mode")
					{
					last_repeat_cmd = "";
					if (amm.StartManualMode(this))
						{ 
						Speech.SpeakAsync("okay");
						AAShare.handle_speech = false;
						}
					else
						SkillShared.OutputSpeech("Could not establish connection.");
					}
				else if (msg.StartsWith("wrist"))
					{
					words = msg.Split(' ');
					if (words.Length == 2)
						{
						if (words[1] == "parallel")
							{
							Speech.SpeakAsync("okay");
							AAShare.wrist_inline = false;
							last_repeat_cmd = "";
							}
						else if (words[1] == "inline")
							{
							Speech.SpeakAsync("okay");
								AAShare.wrist_inline = true;
							last_repeat_cmd = "";
							}
						else
							{
							Speech.SpeakAsync("The command " + msg + " is not supported.");
							last_repeat_cmd = "";
							}
						}
					else
						{
						Speech.SpeakAsync("The command " + msg + " is not supported.");
						last_repeat_cmd = "";
						}
					}
				else if (msg == "position")
					{
					aap.Position();
					last_repeat_cmd = "";
					}
				else if (msg == "give me that")
					{
					if (GrippingObject())
						{
						aap.GiveMeThat(this);
						last_repeat_cmd = "";
						}
					else
						Speech.SpeakAsync("I am not holding any thing.");
					}
				else if (AAShare.arm_pos != AAShare.position.PARK)
					{
					if (msg == "return start")
						{
						last_repeat_cmd = "";
						if ((AAShare.arm_pos == AAShare.position.IN_WS) || (AAShare.arm_pos == AAShare.position.ENTRY_EXIT))
							{
							Speech.SpeakAsync("okay");
							if (AAShare.Shoot(true))
								{
								if (AAShare.arm_pos == AAShare.position.IN_WS)
									{
									if (AAShare.MoveExitPt())
										{
										if (Arm.StartPosOnly(AAShare.start_pos[0], AAShare.start_pos[1]))
											AAShare.arm_pos = AAShare.position.START;
										else
											Speech.SpeakAsync("Return start failed.");
										}
									else
										Speech.SpeakAsync("Return start failed.");
									}
								else
									if (Arm.StartPosOnly(AAShare.start_pos[0], AAShare.start_pos[1]))
										AAShare.arm_pos = AAShare.position.START;
									else
										Speech.SpeakAsync("Return start failed.");
								}
							else
								Speech.SpeakAsync("Could not compose new work space map.");
							}
						else
							Speech.SpeakAsync("Arm not in work space.");
						}
					else if (msg == "open")
						{
						Speech.SpeakAsync("okay");
						Arm.Position(Arm.GRIP_CHANNEL, Arm.GRIP_OPEN,Arm.GRIP_SPEED);
						last_repeat_cmd = "";
						}
					else if (msg == "close")
						{
						Speech.SpeakAsync("okay");
						Arm.CloseGrip();
						last_repeat_cmd = "";
						}
					else if (msg == "right")
						ArmXMove(true);
					else if (msg == "left")
						ArmXMove(false);
					else if (msg == "up")
						ArmYMove(true);
					else if (msg == "down")
						ArmYMove(false);
					else if (msg == "forward")
						ArmZMove(true);
					else if (msg == "back")
						ArmZMove(false);
					else if ((msg == "again") || (msg == "more") || (msg == "repeat"))
						More();
					else if (msg.StartsWith("rotate"))
						{
						words = msg.Split(' ');
						if (words.Length == 2)
							{
							if (words[1] == "parallel")
								{
								Speech.SpeakAsync("okay");
								Arm.Position(Arm.GROTATE_CHANNEL, Arm.GROTATE_PAR, Arm.GROTATE_SPEED);
								grotate_angle = 0;
								last_repeat_cmd = "";
								}
							else if (words[1] == "perpendicular")
								{
								Speech.SpeakAsync("okay");
								Arm.Position(Arm.GROTATE_CHANNEL, Arm.GROTATE_PERP, Arm.GROTATE_SPEED);
								grotate_angle = 90;
								last_repeat_cmd = "";
								}
							else if ((words[1] == "right") || (words[1] == "left"))
								{
								Speech.SpeakAsync("okay");
								GripRotate(words[1]);
								last_repeat_cmd = msg;
								}
							else
								{
								Speech.SpeakAsync("The command " + msg + " is not supported.");
								last_repeat_cmd = "";
								}
							}
						else
							{
							Speech.SpeakAsync("The command " + msg + " is not supported.");
							last_repeat_cmd = "";
							}
						}
					else
						{
						Speech.SpeakAsync("The command " + msg + " is not supported.");
						last_repeat_cmd = "";
						}
					}
				else
					Speech.SpeakAsync("The arm is parked.");
				}

				catch(Exception ex)
				{
				SkillShared.OutputSpeech("Exception " + ex.Message);
				SkillShared.OutputSpeech("While executing " + msg);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

			}
		}



		private void AddGrammar()

		{
			TextReader tr;
			TextWriter tw1;
			string rfname,line;

			rfname = Application.StartupPath + SharedData.CAL_SUB_DIR + "basecommands.txt";
			if (File.Exists(rfname))
				{
				tr = File.OpenText(rfname);
				tw1 = File.CreateText(grammar_file);
				while ((line = tr.ReadLine()) != null)
					{
					if (line.Equals("</grammar>"))
						{
						tw1.WriteLine("  <rule id=\"rootRule\">");
						tw1.WriteLine("    <one-of>");
						tw1.WriteLine("       <item> we are finished </item>");
						tw1.WriteLine("       <item> manual mode </item>");
						tw1.WriteLine("       <item> shoot </item>");
						tw1.WriteLine("       <item> position </item>");
						tw1.WriteLine("       <item> arm off </item>");
						tw1.WriteLine("       <item> return start </item>");
						tw1.WriteLine("       <item> open </item>");
						tw1.WriteLine("       <item> close </item>");
						tw1.WriteLine("       <item> forward </item>");
						tw1.WriteLine("       <item> back </item>");
						tw1.WriteLine("       <item> left </item>");
						tw1.WriteLine("       <item> right </item>");
						tw1.WriteLine("       <item> up </item>");
						tw1.WriteLine("       <item> down </item>");
						tw1.WriteLine("       <item> park </item>");
						tw1.WriteLine("       <item> give me that </item>");
						tw1.WriteLine("       <item> okay </item>");		//to check for speaking "okay" being detected as voice input
						tw1.WriteLine("       <item> <ruleref uri=\"#again\" /> </item>");
						tw1.WriteLine("       <item> <ruleref uri=\"#rotate\" /> </item>");
						tw1.WriteLine("       <item> <ruleref uri=\"#wrist\" /> </item>");
						tw1.WriteLine("    </one-of>");
						tw1.WriteLine("  </rule>");
						tw1.WriteLine();
						tw1.WriteLine("  <rule id=\"again\">");
						tw1.WriteLine("    <one-of>");
						tw1.WriteLine("      <item>again</item>");
						tw1.WriteLine("      <item>repeat</item>");
						tw1.WriteLine("      <item>more</item>");
						tw1.WriteLine("    </one-of>");
						tw1.WriteLine("  </rule>");
						tw1.WriteLine();
						tw1.WriteLine("  <rule id=\"rotate\">");
						tw1.WriteLine("    <item> rotate </item>");
						tw1.WriteLine("    <item>");
						tw1.WriteLine("       <one-of>");
						tw1.WriteLine("         <item>perpendicular</item>");
						tw1.WriteLine("         <item>parallel</item>");
						tw1.WriteLine("         <item>right</item>");
						tw1.WriteLine("         <item>left</item>");
						tw1.WriteLine("       </one-of>");
						tw1.WriteLine("    </item>");
						tw1.WriteLine("  </rule>");
						tw1.WriteLine();
						tw1.WriteLine("  <rule id=\"wrist\">");
						tw1.WriteLine("    <item> wrist </item>");
						tw1.WriteLine("    <item>");
						tw1.WriteLine("       <one-of>");
						tw1.WriteLine("         <item>inline</item>");
						tw1.WriteLine("         <item>parallel</item>");
						tw1.WriteLine("       </one-of>");
						tw1.WriteLine("    </item>");
						tw1.WriteLine("  </rule>");
						tw1.WriteLine();
						tw1.WriteLine(line);
						break;
						}
					else
						tw1.WriteLine(line);
					}
				tr.Close();
				tw1.Close();
				}
		}

		}
	}
