using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using EMK.Cartography;
using System.Data;
using System.Data.SQLite;
using BuildingDataBase;


namespace AutoRobotControl
	{
	public static class NavData
		{

		public enum FeatureType {NONE,CORNER,OPENING_EDGE,TARGET};
		public enum EdgeType { L, I };
		public enum LocationStatus { UNKNOWN, DR, VERIFIED,USR };
		public enum DoorSwing { NONE, IN, OUT };
		public enum TypeSurface { NONE, WINDOW, OPEN_WALL };


		public struct location
			{
			public string rm_name;
			public Point coord;
			public int orientation;
			public LocationStatus ls;
			public string loc_name;
			public bool entrance;

			public override string ToString()

			{
				string rtn = "";

				rtn = this.rm_name + ", " + this.coord + "," + this.orientation + ", " + this.loc_name + ", " + this.ls;
				return(rtn);
			}

			public string ToCSVString()

			{
				string rtn = "";

				rtn = this.rm_name + " (" + this.coord.X  + " " + this.coord.Y + ") " + this.orientation + " "  + this.ls;
				return(rtn);

			}

			};

		public struct connection
			{
			public string name;
			public Point exit_center_coord;
			public int exit_width;
			public int direction;
			public edge hc_edge;
			public edge lc_edge;
			};

		public struct recharge
			{
			public Point coord;
			public feature target;
			public double depth;
			public double ptp_width;
			public double sensor_offset;
			public int direction;
			};

		public struct feature
			{
			public FeatureType type;
			public Point coord;
			};

		public struct edge
			{
			public feature ef;
			public EdgeType type;
			public bool door_side;
			public DoorSwing ds;
			};

		public struct room_pt
			{
			public string name;
			public Point coord;
			};

		public struct surface
			{
			public Point start;
			public Point end;
			public int wall_offset;
			};

		public struct point_3d
			{
			public int X;
			public int Y;
			public int Z;

			public point_3d(int x, int y, int z)

				{
				X = x;
				Y = y;
				Z = z;
				}

			};

		public struct room_data
			{
			public string name;
			public string occupy_map;
			public string heading_cal_file;
			public point_3d building_coord;
			public Rectangle rect;
			public  ArrayList connections;
			public recharge recharge_station;
			public ArrayList features;
			public ArrayList open_area;
			public ArrayList room_pts;
			public ArrayList windows;
			public ArrayList open_walls;
			};



		internal static byte[,] detail_map = null;
		internal static byte[,] move_map = null;
		public static room_data rd = new room_data();
		internal static int[] heading_table;
		internal static ArrayList rooms = new ArrayList();
		public static bool connected = false;

		private static location current_location = new location();
		private static object dm_obj = new object();
		private static object mm_obj = new object();
		private static object ht_obj = new object();
		private static Graph building;
		private static Node[] rms;


		static NavData()

		{
			room_data rd = new room_data();
			feature ft;
			edge ed;
			connection connect = new connection();
			room_pt rp;
			surface srf;
			RoomDAO rm_dao = new RoomDAO();
			RoomPtDAO rptdao = new RoomPtDAO();
			SurfaceDAO surfdao = new SurfaceDAO();
			FeatureDAO featdao = new FeatureDAO();
			RechargeDAO rcdao = new RechargeDAO();
			ConnectionDAO cdao = new ConnectionDAO();
			EdgeDAO edao = new EdgeDAO();
			int i, j, k = -1;
			string[] coord;
			Int64 rid, rcid, cntid, edgid;
			DataTable dt = null, dt2 = null, dt3, dt4;
			SQLiteConnection connectn = null;

			try
			{
			for (k = 0; k < DataBase.rooms.Count; k++)
				{
				rd = new room_data();
				connectn = DataBase.Connection((string)DataBase.rooms[k]);
				dt = rm_dao.RoomList(connectn);
				rid = (Int64)dt.Rows[0][0];
				rd.name = (string)dt.Rows[0][1];
				rd.rect.X = 0;
				rd.rect.Y = 0;
				rd.rect.Height = (int)dt.Rows[0][3];
				rd.rect.Width = (int)dt.Rows[0][2];
				rd.occupy_map = (string)dt.Rows[0][4];
				rd.heading_cal_file = (string)dt.Rows[0][5];
				coord = ((string)dt.Rows[0][6]).Split(',');
				if (coord.Length == 3)
					rd.building_coord = new point_3d(int.Parse(coord[0]), int.Parse(coord[1]), int.Parse(coord[2]));
				rd.room_pts = new ArrayList();
				dt2 = rptdao.RoomPtList(connectn, rid);
				for (i = 0; i < dt2.Rows.Count; i++)
					{
					rp = new room_pt();
					rp.name = (string)dt2.Rows[i][1];
					coord = ((string)dt2.Rows[i][2]).Split(',');
					if (coord.Length == 2)
						rp.coord = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
					rd.room_pts.Add(rp);
					}
				rd.windows = new ArrayList();
				dt2 = surfdao.SurfaceList(connectn, (int)TypeSurface.WINDOW, rid);
				for (i = 0; i < dt2.Rows.Count; i++)
					{
					srf = new surface();
					coord = ((string)dt2.Rows[i][1]).Split(',');
					if (coord.Length == 2)
						srf.start = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
					coord = ((string)dt2.Rows[i][2]).Split(',');
					if (coord.Length == 2)
						srf.end = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
					rd.windows.Add(srf);
					}
				rd.open_walls = new ArrayList();
				dt2 = surfdao.SurfaceList(connectn, (int)TypeSurface.OPEN_WALL, rid);
				for (i = 0; i < dt2.Rows.Count; i++)
					{
					srf = new surface();
					coord = ((string)dt2.Rows[i][1]).Split(',');
					if (coord.Length == 2)
						srf.start = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
					coord = ((string)dt2.Rows[i][2]).Split(',');
					if (coord.Length == 2)
						srf.end = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
					rd.open_walls.Add(srf);
					}
				rd.features = new ArrayList();
				dt2 = featdao.RoomFeatureList(connectn, rid);
				for (i = 0; i < dt2.Rows.Count; i++)
					{
					ft = new feature();
					ft.type = (NavData.FeatureType)dt2.Rows[i][1];
					coord = ((string)dt2.Rows[i][2]).Split(',');
					if (coord.Length == 2)
						ft.coord = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
					rd.features.Add(ft);
					}
				dt2 = rcdao.RechargeStation(connectn, rid);
				if (dt2.Rows.Count > 0)
					{
					rcid = (Int64)dt2.Rows[0][0];
					coord = ((string)dt2.Rows[0][1]).Split(',');
					if (coord.Length == 2)
						rd.recharge_station.coord = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
					rd.recharge_station.depth = (double)dt2.Rows[0][2];
					rd.recharge_station.ptp_width = (double)dt2.Rows[0][3];
					rd.recharge_station.direction = (int)dt2.Rows[0][4];
					rd.recharge_station.sensor_offset = (int) dt2.Rows[0][5];
					ft = new feature();
					ft.type = FeatureType.TARGET;
					dt2 = featdao.RechargeFeature(connectn, rcid);
					if (dt2.Rows.Count > 0)
						{
						coord = ((string)dt2.Rows[0][1]).Split(',');
						if (coord.Length == 2)
							ft.coord = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
						}
					rd.recharge_station.target = ft;
					}
				rd.connections = new ArrayList();
				dt2 = cdao.RoomConnectionsList(connectn, rid);
				for (i = 0; i < dt2.Rows.Count; i++)
					{
					connect = new connection();
					cntid = (Int64)dt2.Rows[i][0];
					connect.name = (string)dt2.Rows[i][1];
					coord = ((string)dt2.Rows[i][2]).Split(',');
					if (coord.Length == 2)
						connect.exit_center_coord = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
					connect.exit_width = (int)dt2.Rows[i][3];
					connect.direction = (int)dt2.Rows[i][4];
					connect.hc_edge = new edge();
					connect.hc_edge.ef.type = FeatureType.NONE;
					connect.lc_edge = new edge();
					connect.lc_edge.ef.type = FeatureType.NONE;
					dt3 = edao.ConnectionEdgeList(connectn, cntid);
					for (j = 0; j < dt3.Rows.Count; j++)
						{
						if (j > 1)
							break;
						edgid = (Int64)dt3.Rows[j][0];
						ed = new edge();
						ed.type = (EdgeType)dt3.Rows[j][1];
						ed.door_side = Convert.ToBoolean((int)dt3.Rows[j][2]);
						ed.ds = (DoorSwing)dt3.Rows[j][3];
						dt4 = featdao.EdgeFeature(connectn, edgid);
						if (dt4.Rows.Count > 0)
							{
							ed.ef = new feature();
							ed.ef.type = FeatureType.OPENING_EDGE;
							coord = ((string)dt4.Rows[0][1]).Split(',');
							if (coord.Length == 2)
								ed.ef.coord = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
							}
						if (Convert.ToBoolean((int)dt3.Rows[j][4]))
							connect.hc_edge = ed;
						else
							connect.lc_edge = ed;
						}
					rd.connections.Add(connect);
					}
				rooms.Add(rd);
				}
			Log.LogEntry("In memory room data base with " + rooms.Count + " rooms created.");
			connected = true;
			}

			catch(Exception ex)
			{
			Log.LogEntry("NavData exception: " + ex.Message);
			Log.LogEntry("Prior exception: " + ex.InnerException.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			Log.LogEntry("Room data bases: " + DataBase.rooms.Count);
			Log.LogEntry("Current room db index: " + k);
			Log.LogEntry("Current room db file: " + DataBase.rooms[k]);
			if (connectn != null)
				Log.LogEntry("DB connection source: " + connectn.DataSource);
			else
				Log.LogEntry("connection is null");
			if (dt != null)
				{
				Log.LogEntry("dt: " + dt.TableName);
				if (dt2 != null)
					Log.LogEntry("dt2: " + dt2.TableName);
				else
					Log.LogEntry("dt2 is null");
				}
			else
				Log.LogEntry("dt is null");
			throw(ex);
			}

			ArrayList connections;
			building = new Graph();

			rms = new Node[rooms.Count];
			for (i = 0; i < rooms.Count; i++)
				rms[i] = building.AddNode(((room_data)rooms[i]).building_coord.X, ((room_data)rooms[i]).building_coord.Y, ((room_data)rooms[i]).building_coord.Z, ((room_data)rooms[i]).name);
			if (rooms.Count > 0)
				for (i = 0; i < rooms.Count; i++)
					{
					connections = ((room_data)rooms[i]).connections;
					for (j = 0; j < connections.Count; j++)
						{
						for (k = 0; k < rooms.Count; k++)
							{
							if (((room_data)rooms[k]).name == ((connection)connections[j]).name)
								{
								building.AddArc(rms[i], rms[k], 1);
								}
							}
						}
					}
			Log.LogEntry("Buidling graph with " + building.Nodes.Count + " rooms and " + building.Arcs.Count + " arcs created.");

			if (!ReadLastLocation(ref current_location) || (current_location.rm_name.Length == 0))
				{
				string room;
				int orient;

				try
				{
				Speech.Speak("I could not retrieve my last location.");
				room = Speech.DetermineRoom(true);
				if (room.Length > 0)
					{
					NavData.LoadRoomdata(room);
					new Room().CreateHeadingTable();
					current_location.rm_name = room;
					orient = Speech.DetermineOrientation(true);
					if (orient != -1)
						{
						Point acoord = new Point(0,0);

						current_location.orientation = orient;
						if (DeterminePlace(ref acoord))
							{
							current_location.coord = acoord;
							current_location.ls = LocationStatus.USR;
							NavData.SetCurrentLocation(current_location);
							}
						}
					}
				}

				catch(Exception ex)
				{
				Log.LogEntry("Determine location converstation exception: " + ex.Message);
				Log.LogEntry("Source: " + ex.Source);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				}
			else
				current_location.ls = LocationStatus.UNKNOWN;
		}



		private static bool DeterminePlace(ref Point accord)

		{
			bool rtn = false;
			ArrayList values;
			int i;
			string reply;
			NavData.room_pt rp;

			values = NavData.GetCurrentRoomPoints();
			for (i = 0; i < values.Count;i++)
				{
				rp = (NavData.room_pt) values[i];
				reply = Speech.Conversation("Am I at " + rp.name + "?","responseyn",5000,true);
				if (reply == "yes")
					{
					accord = rp.coord;
					Speech.Speak("Your response was yes");
					rtn = true;
					break;
					}
				else
					Speech.Speak("Your response was no");
				}
			if (!rtn)
				{
				NavData.recharge rst;

				rst = NavData.GetCurrentRoomRechargeStation();
				if (!rst.coord.IsEmpty)
					{
					reply = Speech.Conversation("Am I at the recharge station?","responseyn",5000,true);
					if (reply == "yes")
						{
						accord = rst.coord;
						Speech.Speak("Your response was yes");
						rtn = true;
						}
					else
						Speech.Speak("Your response was no");
					}
				}
			return(rtn);
		}



		private static bool ReadLastLocation(ref location loc)

		{
			bool rtn = false;
			LastLocationDAO lldao = new LastLocationDAO();
			DataTable dt;
			string[] coord;
			SQLiteConnection connect;

			connect = DataBase.Connection((string)DataBase.lastlocation);
			dt = lldao.LastLocation(connect);
			if (dt.Rows.Count > 0)
				{
				loc.rm_name = (string)dt.Rows[0][0];
				coord = ((string)dt.Rows[0][1]).Split(',');
				if (coord.Length == 2)
					loc.coord = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
				loc.orientation = (int)dt.Rows[0][2];
				loc.loc_name = (string)dt.Rows[0][3];
				loc.entrance = Convert.ToBoolean((int)dt.Rows[0][4]);
				rtn = true;
				Log.LogEntry("Read last location: room - " + current_location.rm_name + ", orientation () - " + current_location.orientation + ", coordinates - " + current_location.coord);
				}
			return (rtn);
		}



		public static byte[,] DetailMap

		{
			set
				{
				lock(dm_obj)
					{
					detail_map = value;
					}
				}
		}



		public static byte[,] MoveMap

		{
			set
				{
				lock(mm_obj)
					{
					move_map = value;
					}
				}
		}



		public static Bitmap MapToBitmap(byte[,] map)

		{
			Bitmap bm;
			int i,j,width,height;

			width = map.GetUpperBound(0);
			height = map.GetUpperBound(1);
			bm = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			for (i = 0; i < width; i++)
				for (j = 0; j < height; j++)
					{
					if (map[i, j] == 0)
						bm.SetPixel(i, j, Color.White);
					else
						bm.SetPixel(i, j, Color.Black);
					}
			return(bm);
		}



		public static int[] HeadingTable

		{
			set
				{
				lock(ht_obj)
					{
					heading_table  = value;
					}
				}
		}



		public static bool SaveLastLocation(location loc)

		{
			LastLocationDAO lldao = new LastLocationDAO();
			SQLiteConnection connect;

			connect = DataBase.Connection((string)DataBase.lastlocation);
			return (lldao.UpdateLastLocation(connect,loc.rm_name,loc.coord,loc.orientation,loc.loc_name,loc.entrance));
		}


		
		public static bool Connected()

		{
			return(connected);
		}



		public static location GetCurrentLocation()

		{
			return(current_location);
		}
		


		public static bool SetCurrentLocation(location loc)

		{
			current_location = loc;
			Log.LogEntry("SetCurrentLocation: " + loc.ToString());
			return(true);
		}



		public static void FlushCurrentLocation()

		{
		}


		public static ArrayList GetConnections(string rm_name)

		{
			ArrayList rtn = null;
			room_data rd;
			int i;

			for (i = 0;i < rooms.Count;i++)
				{
				rd = (room_data) rooms[i];
				if (rd.name == rm_name)
					{
					rtn = rd.connections;
					break;
					}
				}
			return(rtn);
		}



		public static ArrayList GetCurrentRoomConnections()

		{
			return (GetConnections(rd.name));
		}



		public static ArrayList GetPoints(string rm_name)

		{
			ArrayList rtn = null;
			room_data rd;
			int i;

			for (i = 0;i < rooms.Count;i++)
				{
				rd = (room_data) rooms[i];
				if (rd.name == rm_name)
					{
					rtn = rd.room_pts;
					break;
					}
				}
			return(rtn);
		}



		public static ArrayList GetCurrentRoomPoints()

		{
			return(GetPoints(rd.name));
		}



		public static bool GetCurrentRoomPoint(string pt_name,ref room_pt rmpt)

		{
			bool rtn = false;
			int i;
			room_pt rp;

			for (i = 0;i < rd.room_pts.Count;i++)
				{
				rp = (room_pt) rd.room_pts[i];
				if (rp.name == pt_name)
					{
					rtn = true;
					rmpt = rp;
					break;
					}
				}
			return(rtn);
		}



		public static ArrayList GetFeatures(string rm_name)

		{
			ArrayList rtn = new ArrayList();
			room_data rd;
			int i;

			for (i = 0;i < rooms.Count;i++)
				{
				rd = (room_data) rooms[i];
				if (rd.name == rm_name)
					{
					rtn = rd.features;
					break;
					}
				}
			return(rtn);
		}



		public static ArrayList GetCurrentRoomsFeatures()

		{
			if (rd.name.Length > 0)
				return(rd.features);
			else
				return(new ArrayList());
		}



		public static recharge GetRechargeStation(string rm_name)

		{
			NavData.recharge rtn = new recharge();
			room_data rd;
			int i;

			for (i = 0;i < rooms.Count;i++)
				{
				rd = (room_data) rooms[i];
				if (rd.name == rm_name)
					{
					rtn = rd.recharge_station;
					break;
					}
				}
			return(rtn);
		}



		public static recharge GetCurrentRoomRechargeStation()

		{
			return(GetRechargeStation(rd.name));
		}



		public static Rectangle GetRectangle(string rm_name)

		{
			Rectangle rtn = new Rectangle();
			room_data rd;
			int i;

			for (i = 0;i < rooms.Count;i++)
				{
				rd = (room_data) rooms[i];
				if (rd.name == rm_name)
					{
					rtn = rd.rect;
					break;
					}
				}
			return(rtn);
		}


		public static ArrayList GetRooms()

		{
			ArrayList rms = new ArrayList();
			room_data rd;
			int i;

			for (i = 0;i < rooms.Count;i++)
				{
				rd = (room_data) rooms[i];
				rms.Add(rd.name);
				}
			return (rms);
		}



		public static bool LoadRoomdata(string name)

		{
			int i;
			bool rtn = false;

			for (i = 0;i < rooms.Count;i++)
				{
				rd = (room_data) rooms[i];
				if (rd.name == name)
					{
					rtn = true;
					break;
					}
				}
			if (i == rooms.Count)
				{
				rd.name = "";
				}
			return (rtn);
		}



		public static int OpenWallOffset(string name)

		{
			int rtn = 0;
			surface srf;

			room_data rd;
			int i;

			for (i = 0;i < rooms.Count;i++)
				{
				rd = (room_data) rooms[i];
				if (rd.name == name)
					{
					if (rd.open_walls.Count > 0)
						{
						srf = (surface) rd.open_walls[0];
						rtn = srf.wall_offset;
						}
					break;
					}
				}
			return(rtn);
		}



		public static bool OpenWall(Point wp,int width)

		{
			bool rtn = false;
			int i;

			Log.LogEntry("NavData.OpenWall param: " + wp + ", " + width);
			if ((wp.Y == 0) || (wp.Y == rd.rect.Height - 1))
				{
				for (i = wp.X - (width/2); i < wp.X + (width/2);i++)
					if ((i >= 0) && (i < rd.rect.Width))
						if (detail_map[i, wp.Y] == (int) Room.MapCode.OPEN_WALL)
							{
							rtn = true;
							break;
							}
				}
			else
				{
				for (i = wp.Y - (width / 2); i < wp.Y + (width / 2); i++)
					if ((i >= 0) && (i < rd.rect.Height))
						if (detail_map[wp.X,i] == (int) Room.MapCode.OPEN_WALL)
							{
							rtn = true;
							break;
							}
				}
			Log.LogEntry("NavData.OpenWall return: " + rtn);
			return(rtn);
		}



		public static bool Exit(Point wp,int width)

		{
			bool rtn = false;
			int i;

			Log.LogEntry("NavData.Exit param: " + wp + ", " + width);
			if ((wp.Y == 0) || (wp.Y == NavData.rd.rect.Height - 1))
				{
				for (i = wp.X - (width/2); i < wp.X + (width/2);i++)
					if ((i >= 0) && (i < rd.rect.Width))
						if (detail_map[i, wp.Y] == (int) Room.MapCode.EXIT)
							{
							rtn = true;
							break;
							}
				}
			else
				{
				for (i = wp.Y - (width / 2); i < wp.Y + (width / 2); i++)
					if ((i >= 0) && (i < rd.rect.Height))
						if (detail_map[wp.X,i] == (int) Room.MapCode.EXIT)
							{
							rtn = true;
							break;
							}
				}
			Log.LogEntry("NavData.Exit return: " + rtn);
			return(rtn);
		}



		public static string[] BuildingPath(string start_rm_name,string end_rm_name)

		{
			Node snode = null,enode = null;
			int i;
			string[] rtn = null;

			for (i = 0;i < rms.Length;i++)
				{
				if (rms[i].ToString() == start_rm_name)
					{
					snode = rms[i];
					if (enode != null)
						break;
					}
				else if (rms[i].ToString() == end_rm_name)
					{
					enode = rms[i];
					if (snode != null)
						break;
					}
				}
			if ((snode != null) && (enode != null))
				{
				AStar AS = new AStar(building);
				if (AS.SearchPath(snode,enode))
					{
					rtn = new string[AS.PathByNodes.Length - 1];
					for (i = 1;i < AS.PathByNodes.Length;i++)
						rtn[i - 1] = AS.PathByNodes[i].ToString();
					}
				}
			if (rtn == null)
				Log.LogEntry("NavData.BuildingPath: no path found");
			else
				{
				StringBuilder sb = new StringBuilder("NavData.BuildingPath path found: ");
				for (i = 0;i < rtn.Length;i++)
					sb.Append(rtn[i] + "  ");
				Log.LogEntry(sb.ToString());
				}
			return(rtn);
		}



		public static string ClosestRechargeStation(string rm_name)

		{
			string croom = "";
			ArrayList  rcrooms = new ArrayList();
			int i,min_rms = building.Nodes.Count;
			string[] rms;
			room_data rmd;

			if (GetRechargeStation(rm_name).coord.IsEmpty)
				{
				for (i = 0;i < rooms.Count;i++)
					{
					rmd = (room_data) rooms[i];
					if (!rmd.recharge_station.coord.IsEmpty)
						rcrooms.Add(rmd.name);
					}
				if (rcrooms.Count > 0)
					{
					if (rcrooms.Count == 1)
						croom = (string) rcrooms[0];
					else
						{
						for (i = 0;i < rcrooms.Count;i++)
							{
							rms = BuildingPath(rm_name,(string) rcrooms[i]);
							if (rms.Length < min_rms)
								{
								min_rms = rms.Length;
								croom = (string) rcrooms[i];
								}
							}
						}
					}
				}
			else
				croom = rm_name;
			return(croom);
		}


		}
	}



