using System;
using System.Drawing;
using System.Threading;
using System.Collections;
using System.IO;
using System.Windows.Forms;
using Microsoft.Speech.Recognition;
using Coding4Fun.Kinect.WinForm;

namespace AutoRobotControl
	{
	public class SmartMotionCommands:CommandHandlerInterface
		{
		public const string GRAMMAR = "smartmotioncommands";

		public struct come_data
		{
		public int direction;
		public bool come_here;
		};

		private string name1,name2,name3;
		private Thread cmd_exec;


		public SmartMotionCommands()

		{
			RegisterCommandSpeech();
		}



		private void OutputSpeech(string output)

		{
			Speech.Speak(output);
			Log.LogEntry(output);
		}



		public void StopSpeechHandler(string msg)

		{
			Navigate.Stop();
			OutputSpeech("stop sent");
		}



		private void GoToCommandThread()

		{
			Speech.DisableAllCommands();
			Speech.RegisterHandler(Speech.STOP_GRAMMAR, StopSpeechHandler,null);

			try
			{
			if (Navigate.GoTo(name1,name2,name3))
				OutputSpeech("Command go to " + name1 + " " + name2 + " " + name3 + " successful.");
			else
				OutputSpeech("Command go to " + name1 + " " + name2 + " " + name3 + " failed.");
			}

			catch(Exception ex)
			{
			OutputSpeech("Go to command exception " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			Speech.EnableAllCommands();
			Speech.UnRegisterHandler(Speech.STOP_GRAMMAR);
		}



		private void FaceCommandThread()

		{
			int i;
			ArrayList pts;
			NavData.room_pt rp = new NavData.room_pt();
			NavData.location cloc;
			bool good_pt = false;

			Speech.DisableAllCommands();

			try
			{
			cloc = NavData.GetCurrentLocation();
			if ( (cloc.rm_name.Length > 0) && ((cloc.ls == NavData.LocationStatus.DR) || (cloc.ls == NavData.LocationStatus.VERIFIED)))
				{
				if (name1.Equals("exit"))
					{
					if (NavData.rd.connections.Count > 0)
						{
						if (NavData.rd.connections.Count == 1)
							{
							good_pt = true;
							rp.coord = ((NavData.connection) NavData.rd.connections[0]).exit_center_coord;
							}
						else
							OutputSpeech("There are multiple exits in " + NavData.rd.name);
						}
					}
				else if (name1.Equals("recharge"))
					{
					if (!NavData.rd.recharge_station.coord.IsEmpty)
						{
						good_pt = true;
						rp.coord = NavData.rd.recharge_station.coord;
						}
					}
				else
					{
					pts = NavData.GetCurrentRoomPoints();
					for (i = 0;i < pts.Count;i++)
						{
						rp = (NavData.room_pt) pts[i];
						if (rp.name == name1)
							{
							good_pt = true;
							break;
							}
						}
					}
				if (good_pt)
					{
					Move mov = new Move();
					
					if (mov.TurnToFaceMP(rp.coord))
						OutputSpeech("Command face " + name1 + " successful.");
					else
						OutputSpeech("Command face " + name1 + " failed.");
					}
				else
					OutputSpeech("Command can not be executed. The point " + name1 + " does not exist in the current room " + cloc.rm_name + ".");
				}
			else
				OutputSpeech("Command can not be executed. I do not know my current location.");
			}

			catch(Exception ex)
			{
			OutputSpeech("Face command  exception " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			Speech.EnableAllCommands();
		}



		private void ComeWithMeThread(Object direction)

		{
			ComeHere ch = new ComeHere();
			come_data cd;
			Thread sub;

			sub = new Thread(ch.ComeHereCommandThread);
			cd.direction = (int) direction;
			cd.come_here = false;
			sub.Start(cd);
			sub.Join();
			if (ch.succeed)
				{
				FollowMe fm = new FollowMe();

				if (fm.Open())
					{
					sub = new Thread(fm.FollowMeThread);
					sub.Start();
					}
				}
		}



		public void SpeechHandler(string msg)

		{
			string[] words;
			int direction;

			direction = Speech.speaker_direction;
			Log.LogEntry(msg);
			if (SharedData.head_assembly_operational && SharedData.kinect_operational && SharedData.front_lidar_operational 
				&& SharedData.motion_controller_operational && SharedData.navdata_operational)
				{
				words = msg.Split(' ');
				if ((msg.StartsWith("go to")) && (words.Length >= 3))
					{
					Speech.SpeakAsync("okay");
					name1 = words[2];
					name2 = "";
					name3 = "";
					if (words.Length > 3)
						{
						name2 = words[3];
						if (words.Length > 4)
							name3 = words[4];
						}
					SharedData.start = NavData.GetCurrentLocation();
					cmd_exec = new Thread(GoToCommandThread);
					cmd_exec.Start();
					}
				else if ((msg.StartsWith("face")) && (words.Length == 2))
					{
					Log.KeyLogEntry(msg);
					Speech.SpeakAsync("okay");
					name1 = words[1];
					cmd_exec = new Thread(FaceCommandThread);
					cmd_exec.Start();
					}
				else if (msg == "come here")
					{
					ComeHere ch = new ComeHere();
					come_data cd;

					cmd_exec = new Thread(ch.ComeHereCommandThread);
					cd.direction = direction;
					cd.come_here = true;
					cmd_exec.Start(cd);
					}
				else if (msg == "come with me")
					{
					cmd_exec = new Thread(ComeWithMeThread);
					cmd_exec.Start(direction);
					}
				else
					OutputSpeech("Command " + msg + " is incorrect.");
				}
			else
				OutputSpeech("I am not fully operational.  Can not execute command " + msg);
		}



		public bool STSHandler(string msg)

		{
			bool rtn = false;

			string[] words;

			if (SharedData.head_assembly_operational && SharedData.kinect_operational && SharedData.front_lidar_operational 
				&& SharedData.motion_controller_operational && SharedData.navdata_operational)
				{
				words = msg.Split(' ');
				if ((msg.StartsWith("go to")) && (words.Length >= 3))
					{
					name1 = words[2];
					name2 = "";
					name3 = "";
					if (words.Length > 3)
						{
						name2 = words[3];
						if (words.Length > 4)
							name3 = words[4];
						}
					if (Navigate.GoTo(name1, name2,name3))
						rtn = true;
					}
				else
					OutputSpeech("Command " + msg + " is incorrect.");
				}
			else
				OutputSpeech("I am not fully operational.  Can not execute command " + msg);

			return(rtn);
		}


		public void RegisterCommandSpeech()

		{
			Speech.RegisterHandler(GRAMMAR,SpeechHandler,STSHandler);
		}



		public static void AddSmartMotionGrammar()

		{
			TextReader tr;
			TextWriter tw1;
			string rfname,wfname1,line;
			int i,j;
			NavData.room_data rd;
			NavData.room_pt rpt;
			NavData.connection connect;

			rfname = Application.StartupPath + SharedData.CAL_SUB_DIR + "basecommands.txt";
			wfname1 = Application.StartupPath + SharedData.CAL_SUB_DIR + GRAMMAR + ".xml";
			if (File.Exists(rfname) && (NavData.rooms.Count > 0))
				{
				tr = File.OpenText(rfname);
				tw1 = File.CreateText(wfname1);
				while ((line = tr.ReadLine()) != null)
					{
					if (line.Equals("</grammar>"))
						{
						tw1.WriteLine("  <rule id=\"rootRule\">");
						tw1.WriteLine("    <one-of>");
						tw1.WriteLine("       <item> <ruleref uri=\"#goto\" /> </item>");
						tw1.WriteLine("       <item> <ruleref uri=\"#face\" /> </item>");
						tw1.WriteLine("       <item> come here </item>");
						tw1.WriteLine("       <item> come with me </item>");
						tw1.WriteLine("    </one-of>");
						tw1.WriteLine("  </rule>");
						tw1.WriteLine();
						tw1.WriteLine("  <rule id=\"goto\">");
						tw1.WriteLine("    <item> go to </item>");
						tw1.WriteLine("    <item>");
						tw1.WriteLine("       <item repeat=\"0-1\">");
						tw1.WriteLine("         <one-of>");
						for (i = 0;i < NavData.rooms.Count;i++)
							{
							rd = (NavData.room_data) NavData.rooms[i];
							tw1.WriteLine("         <item>" + rd.name + "</item>");
							}
						tw1.WriteLine("         </one-of>");
						tw1.WriteLine("       </item>");
						tw1.WriteLine("       <item repeat=\"0-1\">");
						tw1.WriteLine("         <one-of>");
						tw1.WriteLine("           <item>exit</item>");
						tw1.WriteLine("           <item>recharge</item>");
						for (i = 0; i < NavData.rooms.Count; i++)
							{
							rd = (NavData.room_data)NavData.rooms[i];
							for (j = 0;j < rd.room_pts.Count;j++)
								{
								rpt = (NavData.room_pt) rd.room_pts[j];
								tw1.WriteLine("           <item>" + rpt.name + "</item>");
								} 
							}
						tw1.WriteLine("         </one-of>");
						tw1.WriteLine("       </item>");
						tw1.WriteLine("       <item repeat=\"0-1\">");
						tw1.WriteLine("         <one-of>");
						for (i = 0; i < NavData.rooms.Count; i++)
							{
							rd = (NavData.room_data)NavData.rooms[i];
							for (j = 0;j < rd.connections.Count;j++)
								{
								connect = (NavData.connection) rd.connections[j];
								tw1.WriteLine("         <item>" + connect.name + "</item>");
								}
							}
						tw1.WriteLine("         </one-of>");
						tw1.WriteLine("       </item>");
						tw1.WriteLine("    </item>");
						tw1.WriteLine("  </rule>");
						tw1.WriteLine();
						tw1.WriteLine("  <rule id=\"face\">");
						tw1.WriteLine("    <item> face </item>");
						tw1.WriteLine("    <item>");
						tw1.WriteLine("      <one-of>");
						tw1.WriteLine("         <item>exit</item>");
						tw1.WriteLine("         <item>recharge</item>");
						for (i = 0; i < NavData.rooms.Count; i++)
							{
							rd = (NavData.room_data) NavData.rooms[i];
							for (j = 0;j < rd.room_pts.Count;j++)
								{
								rpt = (NavData.room_pt) rd.room_pts[j];
								tw1.WriteLine("         <item>" + rpt.name + "</item>");
								} 
							}
						tw1.WriteLine("      </one-of>");
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
			else
				Log.LogEntry("AddSmartMotionGrammar: could not add grammar.");
		}


		}
	}
