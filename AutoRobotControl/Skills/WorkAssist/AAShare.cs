using System;
using System.Drawing;
using System.Threading;
using AutoRobotControl;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;

namespace Work_Assist
	{
	static class AAShare
		{

		public const int ARM_KINECT_TILT = -55;
		public const double ARM_KINECT_TILT_CORRECT = -4.0;
		public const double TOP_HI_LO_DIFF = 4.5;
		public const double TOP_HEIGHT_CLEAR = 2.5;
		public const int HEIGHT_ABOVE = 8;
		public enum position { PARK, START, ENTRY_EXIT, IN_WS };

		public static position arm_pos = position.PARK;
		public static Thread pos = null;
		public static int[] start_pos = { 118, -155 };
		public static bool wrist_inline = true;
		public static bool handle_speech = true;

		
		public static bool Shoot(bool video)

		{
			bool rtn = false;
			bool images = false;
			Bitmap bm;
			string fname;
			DateTime now = DateTime.Now;

			if (video)
				images = Kinect.GetColorFrame(ref SkillShared.videodata,40) && Kinect.GetDepthFrame(ref SkillShared.depthdata, 40);
			else
				images = Kinect.GetDepthFrame(ref SkillShared.depthdata, 40);
			if (images)
				{
				if (video)
					{
					bm = SkillShared.videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
					fname = Log.LogDir() + "Workspace pic " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
					bm.RotateFlip(RotateFlipType.Rotate180FlipY);
					bm.Save(fname, ImageFormat.Jpeg);
					Log.LogEntry("Saved " + fname);
					}
				Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, SkillShared.depthdata, SkillShared.dips);
				SkillShared.SaveDipsData("Workspace depth data ", SkillShared.dips);
				if (AAMap.MapWorkPlace())
					{
					SkillShared.wsd.LogWSD();
					AAMap.SaveMap("Work area map ");
					rtn = true;
					}
				else
					SkillShared.OutputSpeech("Could not create the work space map");
				}
			else
				SkillShared.OutputSpeech("Could not obtain a depth frame.");
			return(rtn);
		}



		public static bool RawMoveEntryExitPt()

		{
			bool rtn = false;
			string err = "";
			
			if (arm_pos == position.ENTRY_EXIT)
				rtn = true;
			else
				{
				if ((rtn = Arm.RawPositionArm(0, SkillShared.wsd.top_height + HEIGHT_ABOVE, SkillShared.wsd.front_edge_dist, true, true, false, ref err)))
					{
					Thread.Sleep(2000);
					arm_pos = position.ENTRY_EXIT;
					}
				}
			return (rtn);
		}



		public static bool MoveExitPt()

		{
			bool rtn = false;
			string err = "";
			double dist = 0;

			if (arm_pos == position.IN_WS)
				{
				if ((rtn = Arm.PositionArm(0, SkillShared.wsd.top_height + HEIGHT_ABOVE, SkillShared.wsd.front_edge_dist,0, true,ref dist,ref err)))
					{
					Thread.Sleep((int) Math.Round((dist/Arm.ARM_MOVE_RATE) * 1000));
					arm_pos= position.ENTRY_EXIT;
					}
				}
			else if (arm_pos == position.ENTRY_EXIT)
				rtn = true;
			return (rtn);
		}

		}
	}
