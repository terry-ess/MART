using System;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;



namespace AutoRobotControl
	{
	public static class Navigate
		{

		public static RoomInterface rmi = new Room();
		

		private static void MultiOutput(string msg)

		{
			Speech.SpeakAsync(msg);
			Log.LogEntry(msg);
		}



		private static bool GoToRoom(string name)

		{
			bool rtn = false;
			NavData.location clocation;
			int i,j,dist = 0;
			string[] rms;
			ArrayList connections;
			string crm = "";
			Location loc = new Location();
			Point dif = new Point();
			NavData.connection connect;

			Log.KeyLogEntry("Starting go to room " + name);
			clocation = NavData.GetCurrentLocation();
			rms = NavData.BuildingPath(clocation.rm_name,name);
			if (rms != null)
				{
				for (i = 0;i < rms.Length;i++)
					{
					if (rmi.Leave(rms[i],ref dif,ref dist))
						{
						rmi.Close();
						Log.LogEntry("Connection dif: " + dif.ToString());
						connections = NavData.GetConnections(rms[i]);
						for (j = 0;j < connections.Count;j++)
							{
							crm = (string) ((NavData.connection) connections[j]).name;
							if (crm == clocation.rm_name)
								break;
							else
								crm = "";
							}
						if (crm.Length == 0)
							{
							rtn = false;
							MultiOutput("Could not find connection data for " + clocation.rm_name + ".  Move cancelled.");
							break;
							}
						clocation = NavData.GetCurrentLocation();
						connect = (NavData.connection) connections[j];
						if ((connect.direction == 90) || (connect.direction == 270))
							{
							clocation.coord.X = connect.exit_center_coord.X;
							clocation.coord.Y = connect.exit_center_coord.Y + dif.Y;
							}
						else
							{
							clocation.coord.X = connect.exit_center_coord.X + dif.X;
							clocation.coord.Y = connect.exit_center_coord.Y;
							}
						clocation.rm_name = rms[i];
						clocation.entrance = true;
						NavData.SetCurrentLocation(clocation);
						MotionMeasureProb.ConnectionLocalize(new MotionMeasureProb.Pose(clocation.coord,clocation.orientation),connect.direction,connect.exit_width,dist);
						if ((rtn = rmi.Open((NavData.connection) connections[j])))
							{

							if (rmi.GoToEntryPoint((NavData.connection) connections[j]))
								rtn = true;
							else
								{
								MultiOutput("Could not move to entrance point.  Move cancelled.");
								rtn = false;
								break;
								}
							}
						else
							{
							MultiOutput("Could not determine entrance location.  Move cancelled.");
							rtn = false;
							break;
							}
						}
					else
						{
						rtn = false;
						break;
						}
					}
				}
			else
				{
				MultiOutput("Could not determine path to " + name + ".  Move cancelled.");
				}
			return(rtn);
		}



		private static bool GoToRmPoint(string rm_name,string pt_name)

		{
			bool rtn = false;

			if (GoToRoom(rm_name))
				rtn = rmi.GoToPoint(pt_name);
			return(rtn);
		}



		private static int DetermineExit(string rm_name)

		{
			ArrayList connections;
			int i,rtn = -1;
			string reply;

			connections = NavData.GetConnections(rm_name);
			for (i = 0;i < connections.Count;i++)
				{
				reply = Speech.Conversation("The exit for " + ((NavData.connection) connections[i]).name + "?","responseyn",5000,true);
				if (reply == "yes")
					{
					rtn = i;
					break;
					}
				}
			return(rtn);
		}



		private static int FindExit(string rm_name,string exit_rm_name)

		{
			ArrayList connections;
			int i,rtn = -1;

			connections = NavData.GetConnections(rm_name);
			for (i = 0;i < connections.Count;i++)
				{
				if (((NavData.connection) connections[i]).name == exit_rm_name)
					{
					rtn = i;
					break;
					}
				}
			return(rtn);
		}



		private static bool GoToNearestRecharge(NavData.location cloc)

		{
			string name,reply;
			bool rtn = false;

			name = NavData.ClosestRechargeStation(cloc.rm_name);
			if (name.Length > 0)
				{
				reply = Speech.Conversation("Do you mean the recharge station in " + name + "?", "responseyn", 5000,false);
				if (reply == "yes")
					{
					Speech.Speak("OK");
					rtn = GoToRmPoint(name,SharedData.RECHARGE_LOC_NAME);
					}
				else
					Speech.Speak("I do not know what recharge station location you want.");
				}
			else
				Speech.Speak("I do not know what recharge station location you want.");
			return(rtn);
		}



		private static bool GoTo(string name1,string name2,string name3,NavData.location cloc)

		{
			bool rtn = false;
			ArrayList pts,rms,connections;
			NavData.room_pt rp;
			int i,j;
			int connect_index;

			if ((name2.Length == 0) || (name1 == cloc.rm_name))
				{
				if (name1 == cloc.rm_name)
					{
					name1 = name2;
					name2 = name3;
					name3 = "";
					}
				if (name1.Equals("exit"))
					{
					if (NavData.rd.connections.Count > 0)
						{
						if (NavData.rd.connections.Count == 1)
							rtn = rmi.GoToPoint(name1);
						else if (name2.Length > 0)
							{
							connect_index = FindExit(name1,name2);
							if (connect_index != -1)
								rtn = rmi.GoToPoint(name1 + "-" + connect_index);
							}
						else
							{
							connect_index = DetermineExit(NavData.rd.name);
							if (connect_index != -1)
								rtn = rmi.GoToPoint(name1 + "-" + connect_index); 
							}
						}
					}
				else if (name1.Equals("recharge"))
					{
					if (!NavData.rd.recharge_station.coord.IsEmpty)
						rtn = rmi.GoToPoint(name1);
					else
						rtn = GoToNearestRecharge(cloc);
					}
				else if (name1.Length > 0)
					{
					pts = NavData.GetPoints(NavData.rd.name);
					for (i = 0;i < pts.Count;i++)
						{
						rp = (NavData.room_pt) pts[i];
						if (rp.name.Equals(name1))
							{
							rtn = rmi.GoToPoint(name1);
							break;
							}
						}
					if ((pts.Count == 0) || (i == pts.Count))
						{
						rtn = GoToRoom(name1);
						}
					}
				}
			else if (name3.Length == 0)
				{
				rms = NavData.GetRooms();
				for (i = 0;i < rms.Count;i++)
					{
					if (name1 == (string) rms[i])
						{
						if (name2.Equals("exit"))
							{
							connections = NavData.GetConnections(name1);
							if (connections.Count > 0)
								{
								if (connections.Count == 1)
									rtn = GoToRmPoint(name1,name2);
								else
									{
									connect_index = DetermineExit(name1);
									if (connect_index != -1)
										rtn = GoToRmPoint(name1,name2 + "-" + connect_index); 
									}
								}
							}
						else if (name2.Equals("recharge"))
							{
							if (!NavData.GetRechargeStation(name1).coord.IsEmpty)
								rtn = GoToRmPoint(name1,name2);
							else
								rtn = GoToNearestRecharge(cloc);
							}
						else
							{
							pts = NavData.GetPoints(name1);
							for (j = 0;j < pts.Count;j++)
								{
								rp = (NavData.room_pt) pts[j];
								if (rp.name.Equals(name2))
									{
									if (name1 == NavData.rd.name)
										rtn = rmi.GoToPoint(name2);
									else
										rtn = GoToRmPoint(name1,name2);
									break;
									}
								}
							}
						}
					}
				}
			else
				{
				if (name2.Equals("exit"))
					{
					connections = NavData.GetConnections(name1);
					if (connections.Count > 0)
						{
						if (connections.Count == 1)
							rtn = GoToRmPoint(name1,name2);
						else
							{
							connect_index = FindExit(name1,name3);
							if (connect_index != -1)
								rtn = GoToRmPoint(name1,name2 + "-" + connect_index); 
							}
						}
					}
				}
			return(rtn);
		}



		public static bool GoTo(string name1,string name2,string name3)

		{
			bool rtn = false,rtn_to_start = true;
			NavData.location cloc;
			ArrayList connections;

			cloc = NavData.GetCurrentLocation();
			SharedData.med.et = SharedData.MotionErrorType.NONE;
			rtn = GoTo(name1,name2,name3,cloc);
			if ((!rtn) && (SharedData.med.et == SharedData.MotionErrorType.OBSTACLE) && (SharedData.start.rm_name.Length > 0) && (SharedData.start.ls != NavData.LocationStatus.UNKNOWN))
				{
				Log.KeyLogEntry("Attempting return to start point or the closest recharge station.");
				cloc = NavData.GetCurrentLocation();
				if ((cloc.ls != NavData.LocationStatus.UNKNOWN) && (SharedData.start.loc_name.Length > 0))
					{
					name1 = SharedData.start.rm_name;
					if (SharedData.start.loc_name == "exit")
						{
						connections = NavData.GetConnections(name1);
						if (connections.Count == 1)
							name2 = SharedData.start.loc_name;
						else
							rtn_to_start = false;
						}
					else
						name2 = SharedData.start.loc_name;
					if (rtn_to_start)
						GoTo(name1,name2,"",cloc);
					else
						{
						name1 = NavData.ClosestRechargeStation(cloc.rm_name);
						if (name1.Length > 0)
							GoTo(name1, SharedData.RECHARGE_LOC_NAME, "", cloc);
						else
							Log.LogEntry("Could not determine nearest recharge station.");
						}
					}
				else if (cloc.ls != NavData.LocationStatus.UNKNOWN)
					{
					name1 = NavData.ClosestRechargeStation(cloc.rm_name);
					if (name1.Length > 0)
						GoTo(name1, SharedData.RECHARGE_LOC_NAME, "", cloc);
					else
						Log.LogEntry("Could not determine nearest recharge station.");
					}
				else
					Log.LogEntry("Can not execute return.");
				}
			return(rtn);
		}



		public static bool PathClear(Point ccoord,Point tcoord)

		{
			bool rtn = false;
			NavCompute.pt_to_pt_data rsp;

			Log.LogEntry("Checking path clear from " + ccoord + " to " + tcoord);
			rsp = NavCompute.DetermineRaDirectDistPtToPt(tcoord,ccoord);
			if (MapCompute.FindMapObstacle(NavData.detail_map,ccoord,rsp.direc,rsp.dist,0,1) == -1)
				rtn = true;
			return(rtn);
		}



		public static void DisplayRmMap(Bitmap bm)

		{
		}



		public static void Stop()

		{
			if (NavData.rd.name != "")
				rmi.Stop();
		}



		public static bool Open()

		{
			bool rtn = false;

			if (SharedData.kinect_operational && SharedData.front_lidar_operational && SharedData.head_assembly_operational)
				rtn = rmi.Open();
			return(rtn);
		}



		public static void Close()

		{
			NavData.location clocation;

			rmi.Close();
			clocation = NavData.GetCurrentLocation();
			clocation.ls = NavData.LocationStatus.UNKNOWN;
			NavData.SetCurrentLocation(clocation);
			NavData.SaveLastLocation(clocation);
		}

		}
	}
