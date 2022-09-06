using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using AutoRobotControl;
using Microsoft.Kinect;


namespace Work_Assist
	{

	class WorkAssist : SkillsInterface
		{

		// context
		//		1. workspace info (work assist shared)
		//		2. move to work space path (thread local array list)
		//		3. return to initial position path (thread local stacks)
		//		4. at work space (work assist shared bool)
		//		5. speaker data (AutoRobotControl class)
		// assumptions:
		//		1. This is a single robot - single person
		//		2. Operation is at a "fixed" location

		public const int SPEAKER_TS_DIF = 60000;

		public struct CommandOccur
		{
			public AutoResetEvent evnt;
			public string msg;

			public CommandOccur(bool set)
			{
				evnt = new AutoResetEvent(set);
				msg = "";
			}
		};

		public static Bitmap bmap = null;

		private Thread worker;
		private PersonDetect pd = new PersonDetect();
		private WorkAreaData wad = new WorkAreaData();
		private WorkSpaceInfoInterface wsi = null;
		private MoveToWorkSpaceInterface mtws = null;
		private MoveToStart mts = new MoveToStart();
		private AutoArm aa = new AutoArm();


		public bool Open(params object[] obj)

		{
			bool rtn = false;

			Log.LogEntry("Work assist Open " + obj.Length);
			if (SharedData.head_assembly_operational && SharedData.kinect_operational && SharedData.speech_recognition_active && SharedData.front_lidar_operational
				&& SharedData.rear_lidar_operational && SharedData.motion_controller_operational && SharedData.navdata_operational && SharedData.visual_obj_detect_operational)
				{
				if ((SpeakerData.Person.detected) && (SpeakerData.Person.ts > 0) && ((SharedData.app_time.ElapsedMilliseconds - SpeakerData.Person.ts) < SPEAKER_TS_DIF))
					{
					worker = new Thread(WorkerThread);
					worker.Start();
					rtn = true;
					}
				else
					SkillShared.OutputSpeech("Can not run the work assist skill.  No current speaker data.");
				}
			else
				SkillShared.OutputSpeech("Can not run the work assist skill.  The necessary resources are not available.");
			return (rtn);
		}



		public void Close()

		{
		}



		private void WorkerThread()
		
		{
			ArrayList move_to_ws = new ArrayList();
			Stack final_adjust = new Stack();
			Stack move_to_start = new Stack();
			NavData.location cl;
			string reply;
			NavData.location loc;
			Rectangle rect;
			string msg;

			try
			{
			Speech.DisableAllCommands();
			bmap = null;
			wsi = null;
			mtws = null;
			if (wad.GatherWorkAreaData())
				{
				if (SkillShared.wsd.arrange == SkillShared.work_space_arrange.SAME_SIDE)
					{
					wsi = new SSWorkSpaceInfo();
					mtws = new SSMoveToWorkSpace();
					}
				else if (SkillShared.wsd.arrange == SkillShared.work_space_arrange.EDGE)
					{
					wsi = new EDWorkSpaceInfo();
					mtws = new EDMoveToWorkSpace();
					}
				else if (SkillShared.wsd.arrange == SkillShared.work_space_arrange.OPPOSITE_SIDE)
					{
					SkillShared.OutputSpeech("Opposite side topology functions not implementted.");
					}
				else
					SkillShared.OutputSpeech("No work area topology determined.");
				if ((wsi != null) && (mtws != null) && (wsi.CollectWorkspaceInfo()))
					{
					if (mtws.Move(ref move_to_ws,ref move_to_start,ref final_adjust))
						{
						aa.Work();
						do
							{
							reply = Speech.Conversation("Is it okay for me to return to my start location?","responseyn",60000,false);
							}
						while (reply != "yes");
						if (!SkillShared.wsd.existing_area)
							{
							reply = Speech.Conversation("Should I remember this work space?","responseyn",60000,false);
							if (reply == "yes")
								{
								string pcoord = SpeakerData.Face.rm_location.X + "," + SpeakerData.Face.rm_location.Y;
								WorkSpaceDAO.AddWorkSpace(SkillShared.wsd.name,SkillShared.wsd.room,(short) SkillShared.wsd.arrange,(short) SkillShared.wsd.side,pcoord,(short) SkillShared.wsd.edge_perp_direct,SkillShared.wsd.top_height);
								}
							}
						mts.ReturnToStart(move_to_start,final_adjust);
						}
					else
						{
						SkillShared.OutputSpeech("Attempt to move to work space failed.");
						cl = NavData.GetCurrentLocation();
						if ((cl.ls != NavData.LocationStatus.UNKNOWN) && ((move_to_start.Count > 0) || (final_adjust.Count > 0)))
							mts.ReturnToStart(move_to_start,final_adjust);
						else if (cl.ls == NavData.LocationStatus.UNKNOWN)
							SkillShared.OutputSpeech("Can not recover from failed move.");
						}
					}
				}
			SkillShared.OutputSpeech("Closing skill now.");
			}

			catch(ThreadAbortException)
			{
			}

			catch(Exception ex)
			{
			SkillShared.OutputSpeech("Worker assist exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			Skills.ReturnFailed();
			}

			HeadAssembly.Pan(0, true);
			HeadAssembly.Tilt(0,true);
			worker = null;
			if (SharedData.log_operations && (bmap != null))
				SkillShared.SaveMap("initial information collection", bmap);
			Skills.CloseSkill();
			SkillShared.OutputSpeech("The worker assist skill has been closed.");
			rect = MotionMeasureProb.PdfRectangle();
			loc = NavData.GetCurrentLocation();
			if (loc.ls == NavData.LocationStatus.UNKNOWN)
				msg = loc.rm_name + "," + loc.coord.X + "," + loc.coord.Y + "," + loc.orientation + "," + loc.loc_name + "," + loc.ls + ",,,,";
			else
				msg = loc.rm_name + "," + loc.coord.X + "," + loc.coord.Y + "," + loc.orientation + "," + loc.loc_name + "," + loc.ls + "," + rect.Height + "," + rect.Width + "," + rect.X + "," + rect.Y;
			UiCom.SendLocMessage(msg);
			Speech.EnableAllCommands();
		}


		}
	}
