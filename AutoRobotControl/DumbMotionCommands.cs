using System;
using System.Threading;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using Microsoft.Speech.Recognition;

namespace AutoRobotControl
	{
	public class DumbMotionCommands:CommandHandlerInterface
		{

		public const string GRAMMAR = "dumbmotioncommands";

		private const string RIGHT_TURN = "R SLOW";
		private const string LEFT_TURN = "L SLOW";
		private const string FORWARD = "TF 0";
		private const string BACKWARD = "TB 0";
		private const string STOP_COMMAND = "SL";
		private const string STOP_SPIN_COMMAND = "SM";
		
		private string command;
		private Thread cmd_exec;
		private RechargeDock rcd = null;
		private bool motion_started = false;


		public DumbMotionCommands()

		{
			RegisterCommandSpeech();
		}



		private string SendCommand(string command,bool quiet)

		{
			string rtn = "";

			Log.LogEntry(command);
			rtn = MotionControl.SendCommand(command);
			Log.LogEntry(rtn);
			if (rtn.Contains("fail"))
				Speech.SpeakAsync("Command " + rtn);
			else if (!quiet)
				Speech.SpeakAsync("Command successful.");
			return(rtn);
		}



		private void OutputSpeech(string output)

		{
			Speech.Speak(output);
			Log.LogEntry(output);
		}



		internal void LocationMessage()

		{
			NavData.location loc;
			string msg;
			
			loc = NavData.GetCurrentLocation();
			msg = loc.rm_name + "," + loc.coord.X + "," + loc.coord.Y + "," + loc.orientation + ","  + loc.loc_name + "," + loc.ls + ",,,,";
			UiCom.SendLocMessage(msg);
		}



		public void StopSpeechHandler(string msg)

		{
			if (command == SharedData.RECHARGE_LOC_NAME)
				{
				Speech.SpeakAsync("OK");
				if (rcd != null)
					rcd.StopDocking();
				Speech.UnRegisterHandler(Speech.STOP_GRAMMAR);
				Speech.EnableAllCommands();
				}
			else if (motion_started)
				{
				Speech.SpeakAsync("OK");
				if ((command == FORWARD) || (command == BACKWARD))
					MotionControl.SendStopCommand(STOP_COMMAND);
				else
					SendCommand(STOP_SPIN_COMMAND,true);
				Speech.UnRegisterHandler(Speech.STOP_GRAMMAR);
				Speech.EnableAllCommands();
				motion_started = false;
				}
		}



		private void CommandThread()

		{
			string rtn;
			NavData.location clocation;

			clocation = NavData.GetCurrentLocation();
			Speech.DisableAllCommands();
			Speech.RegisterHandler(Speech.STOP_GRAMMAR, StopSpeechHandler,null);

			try
			{
			if (command == SharedData.RECHARGE_LOC_NAME)
				{
				if ((clocation.ls == NavData.LocationStatus.UNKNOWN) || (clocation.rm_name == null) || (clocation.rm_name.Length == 0))
					{
					Target target = new AutoRobotControl.Target();
					string rname,reply;
					
					if (target.LocateTarget())
						{
						OutputSpeech("I have found a recharge station target.");
						rname = NavData.ClosestRechargeStation(clocation.rm_name);
						reply = Speech.Conversation("Am I in " + rname + "?","responseyn",5000,false);
						if (reply == "yes")
							{
							NavData.LoadRoomdata(rname);
							clocation.rm_name = rname;
							}
						else
							clocation.rm_name = "" ;
						}
					else
						OutputSpeech("I did not find a recharge station target.");
					}
				if (clocation.rm_name.Length > 0)
					{
					NavData.recharge rst;

					rst = NavData.GetRechargeStation(clocation.rm_name);
					if (!rst.coord.IsEmpty)
						{
						rcd = new RechargeDock();
						if (rcd.StartDocking(NavData.GetRechargeStation(clocation.rm_name),ref clocation,false))
							{
							clocation.loc_name = SharedData.RECHARGE_LOC_NAME;
							NavData.SetCurrentLocation(clocation);
							if (NavData.rd.name != clocation.rm_name)
								NavData.LoadRoomdata(clocation.rm_name);
							OutputSpeech("I have docked at the recharge station.");
							}
						else
							OutputSpeech("Docking attempt failed." + rcd.LastError());
						}
					else
						OutputSpeech("There is no recharge station in " + clocation.rm_name);
					}
				Speech.UnRegisterHandler(Speech.STOP_GRAMMAR);
				Speech.EnableAllCommands();
				}
			else
				{
				motion_started = true;
				rtn = SendCommand(command,true);
				if (rtn.Contains("fail"))
					{
					motion_started = false;
					Speech.UnRegisterHandler(Speech.STOP_GRAMMAR);
					Speech.EnableAllCommands();
					if ((command == FORWARD) || (command == BACKWARD))
						{
						string lmf = "";
						MotionControl.DownloadLastMoveFile(ref lmf);
						}
					else if ((command == LEFT_TURN) || (command == RIGHT_TURN))
						{
						string ltf = "";
						MotionControl.DownloadLastTurnFile(ref ltf);
						}
					clocation.ls = NavData.LocationStatus.UNKNOWN;
					NavData.SetCurrentLocation(clocation);
					LocationMessage();
					}
				else
					{
					clocation.ls = NavData.LocationStatus.UNKNOWN;
					NavData.SetCurrentLocation(clocation);
					LocationMessage();
					}
				}
			}

			catch(Exception ex)
			{
			if (command == SharedData.RECHARGE_LOC_NAME)
				{
				if (rcd != null)
					rcd.StopDocking();
				}
			else if (motion_started)
				{
				if ((command == FORWARD) || (command == BACKWARD))
					MotionControl.SendStopCommand(STOP_COMMAND);
				else
					SendCommand(STOP_SPIN_COMMAND, true);
				}
			OutputSpeech("Dumb motion command " + command + " exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			motion_started = false;
			Speech.UnRegisterHandler(Speech.STOP_GRAMMAR);
			Speech.EnableAllCommands();
			clocation.ls = NavData.LocationStatus.UNKNOWN;
			NavData.SetCurrentLocation(clocation);
			LocationMessage();
			}

		}



		public void SpeechHandler(string msg)

		{
			bool command_ok = true;

			Log.KeyLogEntry(msg);
			if (SharedData.motion_controller_operational)
				{
				if (msg == "right turn")
					command = RIGHT_TURN;
				else if (msg == "left turn")
					command = LEFT_TURN;
				else if (msg == "forward")
					command = FORWARD;
				else if (msg == "backward")
					command = BACKWARD;
				else if (msg == SharedData.RECHARGE_LOC_NAME)
					{
					string rsp = "";

					if ((Kinect.nui != null) && Kinect.nui.IsRunning && MotionControl.Operational(ref rsp) && Rplidar.Operational())
						command = SharedData.RECHARGE_LOC_NAME;
					else
						{
						command_ok = false;
						OutputSpeech("Can not execute recharge.");
						}
					}
				else
					{
					command_ok = false;
					OutputSpeech("Did not recognize command: " + msg);
					}
				if (command_ok)
					{
					cmd_exec = new Thread(CommandThread);
					cmd_exec.Start();
					Speech.SpeakAsync("OK");
					}
				}
			else
				OutputSpeech("Motion controller is not operational.  Can not execute command.");
		}



		public void RegisterCommandSpeech()

		{
			Speech.RegisterHandler(GRAMMAR,SpeechHandler,null);
		}

		}
	}
