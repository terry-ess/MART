using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Data.SQLite;
using BuildingDataBase;


namespace DBMap
	{
	class RoomData
		{

		public enum FeatureType {NONE,CORNER,OPENING_EDGE,TARGET};
		public enum EdgeType {L,I};
		public enum LocationStatus {UNKNOWN,DR,VERIFIED};
		public enum MapCode { BLOCKED, CLEAR, WINDOW, OPEN_WALL,CORNER = 10, EXIT = 20,TARGET = 30 };
		public enum DoorSwing {NONE,IN,OUT};
		public enum TypeSurface {NONE,WINDOW,OPEN_WALL};


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
			};

		public struct point_3d
			{
			public int X;
			public int Y;
			public int Z;
			
			public point_3d(int x,int y,int z)

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
			public recharge recharge_station;
			public ArrayList connections;
			public ArrayList features;
			public ArrayList room_pts;
			public ArrayList windows;
			public ArrayList open_walls;
			};


		public room_data rd;


		public bool LoadRoomData(string db,TextBox tb)

		{
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
			int i,j;
			string[] coord;
			Int64 rid,rcid,cntid,edgid;
			DataTable dt,dt2,dt3,dt4;
			SQLiteConnection connectn;
			string path;
			bool rtn = false;

			path = db.Substring(0,db.LastIndexOf('\\') + 1);
			rd = new room_data();
			connectn = DataBase.Connection(db);
			dt = rm_dao.RoomList(connectn);
			if (dt.Rows.Count > 0)
				{
				rtn = true;
				rid = (Int64) dt.Rows[0][0];
				rd.name = (string) dt.Rows[0][1];
				rd.rect.X = 0;
				rd.rect.Y = 0;
				rd.rect.Height = (int) dt.Rows[0][3];
				rd.rect.Width = (int) dt.Rows[0][2];
				rd.occupy_map = path + (string) dt.Rows[0][4];
				rd.heading_cal_file = path + (string) dt.Rows[0][5];
				coord = ((string) dt.Rows[0][6]).Split(',');
				if (coord.Length == 3)
					rd.building_coord = new RoomData.point_3d(int.Parse(coord[0]),int.Parse(coord[1]),int.Parse(coord[2]));
				tb.AppendText("ROOM\r\n");
				tb.AppendText("  " + rd.name + "  width " + rd.rect.Width + " in.  height " + rd.rect.Height + " in.\r\n");
				rd.room_pts = new ArrayList();
				dt2 = rptdao.RoomPtList(connectn,rid);
				tb.AppendText("\r\nNAMED POINTS\r\n");
				for (i = 0;i < dt2.Rows.Count;i++)
					{
					rp = new room_pt();
					rp.name = (string) dt2.Rows[i][1];
					coord = ((string) dt2.Rows[i][2]).Split(',');
					if (coord.Length == 2)
						rp.coord = new Point(int.Parse(coord[0]),int.Parse(coord[1]));
					rd.room_pts.Add(rp);
					tb.AppendText("     " +rp.name + " @ (" + ((string) dt2.Rows[i][2]) + ")\r\n");
					}
				rd.windows = new ArrayList();
				dt2 = surfdao.SurfaceList(connectn,(int) TypeSurface.WINDOW,rid);
				for (i = 0;i < dt2.Rows.Count;i++)
					{
					srf = new surface();
					coord = ((string) dt2.Rows[i][1]).Split(',');
					if (coord.Length == 2)
						srf.start = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
					coord = ((string) dt2.Rows[i][2]).Split(',');
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
				dt2 = featdao.RoomFeatureList(connectn,rid);
				tb.AppendText("\r\nFEATURES\r\n");
				for (i = 0; i < dt2.Rows.Count; i++)
					{
					ft = new feature();
					ft.type = (RoomData.FeatureType) dt2.Rows[i][1];
					coord = ((string) dt2.Rows[i][2]).Split(',');
					if (coord.Length == 2)
						ft.coord = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
					rd.features.Add(ft);
					if (ft.type != FeatureType.OPENING_EDGE)
						tb.AppendText("      " + ft.type.ToString().ToLower() + " @ (" + ((string)dt2.Rows[i][2]) + ")\r\n");
					}
				dt2 = rcdao.RechargeStation(connectn,rid);
				if (dt2.Rows.Count > 0)
					{
					rcid = (Int64) dt2.Rows[0][0];
					coord = ((string) dt2.Rows[0][1]).Split(',');
					if (coord.Length == 2)
						rd.recharge_station.coord = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
					rd.recharge_station.depth = (double) dt2.Rows[0][2];
					rd.recharge_station.ptp_width = (double) dt2.Rows[0][3];
					rd.recharge_station.direction = (int) dt2.Rows[0][4];
					rd.recharge_station.sensor_offset = (Int64) dt2.Rows[0][5];
					ft = new feature();
					ft.type = FeatureType.TARGET;
					dt2 = featdao.RechargeFeature(connectn,rcid);
					if (dt2.Rows.Count > 0)
						{
						coord = ((string)dt2.Rows[0][1]).Split(',');
						if (coord.Length == 2)
							ft.coord = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
						}
					rd.recharge_station.target = ft;
					}
				rd.connections = new ArrayList();
				tb.AppendText("\r\nCONNECTORS\r\n");
				dt2 = cdao.RoomConnectionsList(connectn, rid);
				for (i = 0; i < dt2.Rows.Count; i++)
					{
					connect = new connection();
					cntid = (Int64)dt2.Rows[i][0];
					connect.name = (string) dt2.Rows[i][1];
					coord = ((string) dt2.Rows[i][2]).Split(',');
					if (coord.Length == 2)
						connect.exit_center_coord = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
					connect.exit_width = (int) dt2.Rows[i][3];
					connect.direction = (int)dt2.Rows[i][4];
					connect.hc_edge = new edge();
					connect.hc_edge.ef.type = FeatureType.NONE;
					connect.lc_edge = new edge();
					connect.lc_edge.ef.type = FeatureType.NONE;
					tb.AppendText("     " + connect.name + "  width " + connect.exit_width + " in.   direction " + connect.direction + "°  center @ (" +((string)dt2.Rows[i][2]) + ")   ");
					dt3 = edao.ConnectionEdgeList(connectn,cntid);
					for (j= 0;j < dt3.Rows.Count;j++)
						{
						if (j > 1)
							break;
						edgid = (Int64) dt3.Rows[j][0];
						ed = new edge();
						ed.type = (EdgeType) dt3.Rows[j][1];
						ed.door_side = Convert.ToBoolean((int) dt3.Rows[j][2]);
						ed.ds = (DoorSwing) dt3.Rows[j][3];
						dt4 = featdao.EdgeFeature(connectn,edgid);
						if (dt4.Rows.Count > 0)
							{
							ed.ef = new feature();
							ed.ef.type = FeatureType.OPENING_EDGE;
							coord = ((string)dt4.Rows[0][1]).Split(',');
							if (coord.Length == 2)
								ed.ef.coord = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
							}
						if (Convert.ToBoolean((int) dt3.Rows[j][4]))
							{
							connect.hc_edge = ed;
							tb.AppendText("left edge @ (" + ((string)dt4.Rows[0][1]) + ")   ");
							}
						else
							{
							connect.lc_edge = ed;
							tb.AppendText("right edge @ (" + ((string)dt4.Rows[0][1]) + ")   ");
							}
						}
					tb.AppendText("\r\n");
					rd.connections.Add(connect);
					}
				}
			else
				tb.AppendText("Could not open data base.\r\n");
			return(rtn);
		}



		public void CreateRoomMap(RoomData.room_data rd,ref byte[,] detail_map,ref Bitmap brbm)

		{
			int i, x, y, j;

			OccupyMap.ReadMap(rd.occupy_map, ref detail_map);
			brbm = new Bitmap(rd.rect.Width, rd.rect.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			for (x = 0; x < rd.rect.Width; x++)
				for (y = 0; y < rd.rect.Height; y++)
					{
					if (detail_map[x, y] == (int)RoomData.MapCode.BLOCKED)
						brbm.SetPixel(x, y, Color.Black);
					else
						brbm.SetPixel(x,y,Color.White);
					}

			for (i = 0;i < rd.windows.Count;i++)
				{
				RoomData.surface window;
				int start,end;

				window = (RoomData.surface) rd.windows[i];
				if (window.start.X == window.end.X)
					{
					if (window.start.Y < window.end.Y)
						{
						start = window.start.Y;
						end = window.end.Y;
						}
					else
						{
						start = window.end.Y;
						end = window.start.Y;
						}
					for (j = window.start.Y;j <= window.end.Y;j++)
						{
						detail_map[window.start.X,j] = (int) RoomData.MapCode.WINDOW;
						brbm.SetPixel(window.start.X, j, Color.Purple);
						}
					}
				else
					{
					if (window.start.X < window.end.X)
						{
						start = window.start.X;
						end = window.end.X;
						}
					else
						{
						start = window.end.X;
						end = window.start.X;
						}
					for (j = window.start.X;j <= window.end.X;j++)
						{
						detail_map[j,window.start.Y] = (int) RoomData.MapCode.WINDOW;
						brbm.SetPixel(j,window.start.Y,Color.Purple);
						}
					}
				}
			for (i = 0;i < rd.open_walls.Count;i++)
				{
				RoomData.surface owall;
				int start,end;

				owall = (RoomData.surface) rd.open_walls[i];
				if (owall.start.X == owall.end.X)
					{
					if (owall.start.Y < owall.end.Y)
						{
						start = owall.start.Y;
						end = owall.end.Y;
						}
					else
						{
						start = owall.end.Y;
						end = owall.start.Y;
						}
					for (j = start;j < end;j++)
						{
						detail_map[owall.start.X,j] = (int) RoomData.MapCode.OPEN_WALL;
						brbm.SetPixel(owall.start.X,j,Color.Blue);
						}
					}
				else
					{
					if (owall.start.X < owall.end.X)
						{
						start = owall.start.X;
						end = owall.end.X;
						}
					else
						{
						start = owall.end.X;
						end = owall.start.X;
						}
					for (j = start;j < end;j++)
						{
						detail_map[j,owall.start.Y] = (int) RoomData.MapCode.OPEN_WALL;
						brbm.SetPixel(j,owall.start.Y,Color.Blue);
						}
					}
				}
			for (i = 0;i < rd.connections.Count;i++)
				{
				RoomData.connection connect;
				int start,end;

				connect = (RoomData.connection) rd.connections[i];
				if ((connect.direction == 0) || (connect.direction == 180))
					{
					start = connect.exit_center_coord.X - connect.exit_width/2;
					end = connect.exit_center_coord.X + connect.exit_width/2;
					if (start < 0)
						start = 0;
					if (end > rd.rect.Width)
						end = rd.rect.Width;
					for (j = start; j < end; j++)
						{
						detail_map[j,connect.exit_center_coord.Y] = (byte) (RoomData.MapCode.EXIT + i);
						brbm.SetPixel(j,connect.exit_center_coord.Y,Color.Orange);
						}
					if ((connect.hc_edge.ef.type == RoomData.FeatureType.OPENING_EDGE) && connect.hc_edge.door_side && (connect.hc_edge.ds == RoomData.DoorSwing.IN))
						{
						if (((connect.direction == 0) && (connect.hc_edge.ef.coord.X > connect.exit_width)) ||
							 ((connect.direction == 180) && (rd.rect.Width - connect.hc_edge.ef.coord.X > connect.exit_width)))
							{
							for (j = 1; j < connect.exit_width; j++)
								{
								if (connect.direction == 180)
									x = connect.hc_edge.ef.coord.X - j;
								else
									x = connect.hc_edge.ef.coord.X + j;
								detail_map[x, connect.hc_edge.ef.coord.Y] = (byte)(RoomData.MapCode.BLOCKED);
								brbm.SetPixel(x, connect.hc_edge.ef.coord.Y, Color.Black);
								}
							}
						else
							{
							for (j = 1; j < connect.exit_width; j++)
								{
								if (connect.direction == 0)
									y = j;
								else
									y = connect.exit_center_coord.Y - j;
								detail_map[connect.hc_edge.ef.coord.X, y] = (byte)(RoomData.MapCode.BLOCKED);
								brbm.SetPixel(connect.hc_edge.ef.coord.X, y, Color.Black);
								}
							}
						}
					else if ((connect.lc_edge.ef.type == RoomData.FeatureType.OPENING_EDGE) && connect.lc_edge.door_side && (connect.lc_edge.ds == RoomData.DoorSwing.IN))
						{
						if (((connect.direction == 180) && (connect.lc_edge.ef.coord.X > connect.exit_width)) ||
							 ((connect.direction == 0) && (rd.rect.Width - connect.lc_edge.ef.coord.X > connect.exit_width)))
							{
							for (j = 1; j < connect.exit_width; j++)
								{
								if (connect.direction == 0)
									x = connect.lc_edge.ef.coord.X + j;
								else
									x = connect.lc_edge.ef.coord.X - j;
								detail_map[x, connect.lc_edge.ef.coord.Y] = (byte)(RoomData.MapCode.BLOCKED);
								brbm.SetPixel(x, connect.lc_edge.ef.coord.Y, Color.Black);
								}
							}
						else
							{
							for (j = 1; j < connect.exit_width; j++)
								{
								if (connect.direction == 0)
									y = j;
								else
									y = connect.exit_center_coord.Y - j;
								detail_map[connect.lc_edge.ef.coord.X, y] = (byte)(RoomData.MapCode.BLOCKED);
								brbm.SetPixel(connect.lc_edge.ef.coord.X, y, Color.Black);
								}
							}
						}
					}
				else
					{
					start = connect.exit_center_coord.Y - connect.exit_width / 2;
					end = connect.exit_center_coord.Y + connect.exit_width / 2;
					if (start < 0)
						start = 0;
					if (end > rd.rect.Height)
						end = rd.rect.Height;
					for (j = start; j < end; j++)
						{
						detail_map[connect.exit_center_coord.X,j] = (byte) (RoomData.MapCode.EXIT + i);
						brbm.SetPixel(connect.exit_center_coord.X,j,Color.Orange);
						}
					if ((connect.hc_edge.ef.type == RoomData.FeatureType.OPENING_EDGE) && connect.hc_edge.door_side && (connect.hc_edge.ds == RoomData.DoorSwing.IN))
						{
						if (((connect.direction == 90) && (connect.hc_edge.ef.coord.Y > connect.exit_width)) ||
							 ((connect.direction == 270) && (rd.rect.Height - connect.hc_edge.ef.coord.Y > connect.exit_width)))
							{
							for (j = 1;j < connect.exit_width;j++)
								{
								if (connect.direction == 270)
									y = connect.exit_center_coord.Y - j;
								else
									y = j;
								detail_map[connect.hc_edge.ef.coord.X,y] = (byte) (RoomData.MapCode.BLOCKED);
								brbm.SetPixel(connect.hc_edge.ef.coord.X,y, Color.Black);
								}
							}
						else
							{
							for (j = 1;j < connect.exit_width;j++)
								{
								if (connect.direction == 270)
									x = j;
								else
									x = connect.exit_center_coord.X - j;
								detail_map[x,connect.hc_edge.ef.coord.Y] = (byte) (RoomData.MapCode.BLOCKED);
								brbm.SetPixel(x, connect.hc_edge.ef.coord.Y, Color.Black);
								}
							}
						}
					else if ((connect.lc_edge.ef.type == RoomData.FeatureType.OPENING_EDGE) && connect.lc_edge.door_side && (connect.lc_edge.ds == RoomData.DoorSwing.IN))
						{
						if (((connect.direction == 270) && (connect.lc_edge.ef.coord.Y > connect.exit_width)) ||
							 ((connect.direction == 90) && (rd.rect.Height - connect.lc_edge.ef.coord.Y > connect.exit_width)))
							{
							for (j = 1;j < connect.exit_width;j++)
								{
								if (connect.direction == 270)
									y = connect.exit_center_coord.Y - j;
								else
									y = j;
								detail_map[connect.lc_edge.ef.coord.X,y] = (byte) (RoomData.MapCode.BLOCKED);
								brbm.SetPixel(connect.lc_edge.ef.coord.X,y, Color.Black);
								}
							}
						else
							{
							for (j = 1;j < connect.exit_width;j++)
								{
								if (connect.direction == 270)
									x = j;
								else
									x = connect.exit_center_coord.X - j;
								detail_map[x,connect.lc_edge.ef.coord.Y] = (byte) (RoomData.MapCode.BLOCKED);
								brbm.SetPixel(x, connect.lc_edge.ef.coord.Y, Color.Black);
								}
							}
						}
					}
				}
			for (i = 0;i <rd.features.Count;i++)
				{
				RoomData.feature f;

				f = (RoomData.feature) rd.features[i];
				if (f.type == RoomData.FeatureType.CORNER)
					{
					detail_map[f.coord.X,f.coord.Y] = (byte)(RoomData.MapCode.CORNER + i);
					brbm.SetPixel(f.coord.X, f.coord.Y, Color.Turquoise);
					}
				else if (f.type == RoomData.FeatureType.TARGET)
					{
					detail_map[f.coord.X,f.coord.Y] = (byte)(RoomData.MapCode.TARGET + i);
					brbm.SetPixel(f.coord.X, f.coord.Y, Color.Green);
					}
				}

		}


		}
	}
