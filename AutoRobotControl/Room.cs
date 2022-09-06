
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using MapAStar;
using BuildingDataBase;


namespace AutoRobotControl
	{

	public class Room: RoomInterface
		{
		public static Point ignor = new Point(-1,-1);

		internal const int MAX_DEST_WALL_DIST = 12 * 12;
		internal const int MAP_SHRINK_FACTOR = 15;

		private const int WIDTH_CLEARENCE = 1;
		private const int LENGTH_CLEARENCE = 1;
		public const int STD_EXIT_DISTANCE = 42;
		private const int EXIT_MARGIN = 4;
		private const int MAX_DISTANCE_NAMED_PT = 9;
		private enum DistRef {NONE,MOTOR_CONTROLLER,CENTER,HIGH_CENTER,DELAY_CENTER,DELAY_HIGH_CENTER};
		private enum EPDirect {NONE,RIGHT,LEFT,EITHER};

		public enum EdgeType {NONE,ABRUPT,GRADUAL,EITHER};
		public enum TurnLimits {NONE,MINUS_ONLY,PLUS_ONLY,NO_TURN};
		public enum MapCode { BLOCKED, CLEAR, WINDOW, OPEN_WALL, CORNER = 10, EXIT = 20, TARGET = 30 };


		public struct target
			{
			public int findex;
			public bool forward,plus,minus;
			};

		public struct perp_wall
			{
			public int start_indx;
			public int count;
			};

		public struct feature_match
			{
			public bool matched;
			public int distance;
			public double ra;
			public int index;
			public int head_angle;
			public int orient;
			public perp_wall pw;
			};


		public struct rm_location
			{
			public Point coord;
			public int orientation;
			};

		
		public struct bad_pt
			{
			public int index;
			public int dist;
			};


		internal Location rloc = new Location();
		internal int[] heading_table;

		private NavData.location clocation;
		private Bitmap rbm, brbm;
		private Graphics g;
		private Move rmove = new Move();
		private byte[,] detail_map;
		private byte[,] move_map;
		private byte[,] initial_move_map;
		private object vl_obj = new object();
		private bool verify_location = false;
		private object run_obj = new object();
		private bool run = false;
		private Point last_robot_display_loc = new Point();
		private Rectangle last_pdf_rect = new Rectangle();
		private string last_error = "";



		private bool SendCommand(string command,int timeout_count)

		{
			string rsp = "";
			bool rtn;

			Log.LogEntry(command);
			if (timeout_count < 20)
				timeout_count = 20;
			rsp = MotionControl.SendCommand(command,timeout_count);
			Log.LogEntry(rsp);
			if (rsp.Contains("fail"))
				{
				rtn = false;
				last_error = rsp;
				}
			else
				rtn = true;
			return(rtn);
		}



		public void CreateHeadingTable()
		
		{
			int ymheading, ypheading, xpheading, xmheading;
			string fname,line;
			TextReader tr;
			TextWriter tw;
			int i,val;

			heading_table = new int[360];
			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + NavData.rd.heading_cal_file;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				line = tr.ReadLine();
				ymheading = int.Parse(line);
				line = tr.ReadLine();
				xpheading = int.Parse(line);
				line = tr.ReadLine();
				ypheading = int.Parse(line);
				line = tr.ReadLine();
				xmheading  = int.Parse(line);
				tr.Close();
				tw = File.CreateText(Log.LogDir() + NavData.rd.name + " heading table.txt");
				for (i = 0;i < heading_table.Length;i++)  //assumes that heading_table[0] is "relatively" north
					{
					if (i == 0)
						heading_table[i] = ymheading;
					else if ((i > 0) && (i < 90))
						if (ymheading < xpheading)
							heading_table[i] = (int)(((double)(xpheading - ymheading) / 90) * i);
						else
							{
							val = (int)(((double)(360 - ymheading + xpheading) / 90) * i);
							val += ymheading;
							val %= 360;
							heading_table[i] = val;
							}
					else if (i == 90)
						heading_table[i] = xpheading;
					else if ((i > 90) && (i < 180))
						heading_table[i] = heading_table[90] +  (int) (((double)(ypheading - xpheading) / 90) * (i - 90));
					else if (i == 180)
						heading_table[i] = ypheading;
					else if ((i > 180) && (i < 270))
						heading_table[i] = heading_table[180] + (int)(((double)(xmheading - ypheading) / 90) * (i - 180));					
					else if (i == 270)
						heading_table[i] = xmheading;
					else if (i > 270)
						if (ymheading > xmheading)
							heading_table[i] = heading_table[270] + (int)(((double)(ymheading - xmheading) / 90) * (i - 270));
						else
							heading_table[i] = heading_table[270] + (int)(((double)(360 - xmheading + ymheading) / 90) * (i - 270));
					tw.WriteLine(i.ToString() + " " + heading_table[i].ToString());
					}
				tw.Close();
				}
			else
				for (i = 0;i < heading_table.Length;i++)
					heading_table[i] = i;
			NavData.HeadingTable = heading_table;
		}		


		
		internal void CreateRoomMap()

		{
			int i, x, y, w, h, j, w2, h2;
			bool open = false;
			Bitmap mbm;

			OccupyMap.ReadMap(NavData.rd.occupy_map, ref detail_map);
			brbm = new Bitmap(NavData.rd.rect.Width, NavData.rd.rect.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			for (x = 0; x < NavData.rd.rect.Width; x++)
				for (y = 0; y < NavData.rd.rect.Height; y++)
					{
					if (detail_map[x, y] == (int) MapCode.BLOCKED)
						brbm.SetPixel(x, y, Color.Black);
					else
						brbm.SetPixel(x, y, Color.White);
					}
			for (i = 0;i < NavData.rd.windows.Count;i++)
				{
				NavData.surface window;
				int start,end;

				window = (NavData.surface) NavData.rd.windows[i];
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
						detail_map[window.start.X,j] = (int) MapCode.WINDOW;
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
						detail_map[j,window.start.Y] = (int) MapCode.WINDOW;
						brbm.SetPixel(j,window.start.Y,Color.Purple);
						}
					}
				}
			for (i = 0;i < NavData.rd.open_walls.Count;i++)
				{
				NavData.surface owall;
				int start,end;

				owall = (NavData.surface) NavData.rd.open_walls[i];
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
						detail_map[owall.start.X,j] = (int) MapCode.OPEN_WALL;
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
						detail_map[j,owall.start.Y] = (int) MapCode.OPEN_WALL;
						brbm.SetPixel(j,owall.start.Y,Color.Blue);
						}
					}
				}
			for (i = 0;i < NavData.rd.connections.Count;i++)
				{
				NavData.connection connect;
				int start,end;

				connect = (NavData.connection) NavData.rd.connections[i];
				if ((connect.direction == 0) || (connect.direction == 180))
					{
					start = connect.exit_center_coord.X - connect.exit_width/2;
					end = connect.exit_center_coord.X + connect.exit_width/2;
					if (start < 0)
						start = 0;
					if (end > NavData.rd.rect.Width)
						end = NavData.rd.rect.Width;
					for (j = start; j < end; j++)
						{
						detail_map[j,connect.exit_center_coord.Y] = (byte) (MapCode.EXIT + i);
						brbm.SetPixel(j,connect.exit_center_coord.Y,Color.Orange);
						}
					if ((connect.hc_edge.ef.type == NavData.FeatureType.OPENING_EDGE) && connect.hc_edge.door_side && (connect.hc_edge.ds == NavData.DoorSwing.IN))
						{
						if (((connect.direction == 0) && (connect.hc_edge.ef.coord.X > connect.exit_width)) ||
							 ((connect.direction == 180) && (NavData.rd.rect.Width - connect.hc_edge.ef.coord.X > connect.exit_width)))
							{
							for (j = 1; j < connect.exit_width; j++)
								{
								if (connect.direction == 180)
									x = connect.hc_edge.ef.coord.X - j;
								else
									x = connect.hc_edge.ef.coord.X + j;
								detail_map[x, connect.hc_edge.ef.coord.Y] = (byte)(MapCode.BLOCKED);
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
								detail_map[connect.hc_edge.ef.coord.X, y] = (byte)(MapCode.BLOCKED);
								brbm.SetPixel(connect.hc_edge.ef.coord.X, y, Color.Black);
								}
							}
						}
					else if ((connect.lc_edge.ef.type == NavData.FeatureType.OPENING_EDGE) && connect.lc_edge.door_side && (connect.lc_edge.ds == NavData.DoorSwing.IN))
						{
						if (((connect.direction == 180) && (connect.lc_edge.ef.coord.X > connect.exit_width)) ||
							 ((connect.direction == 0) && (NavData.rd.rect.Width - connect.lc_edge.ef.coord.X > connect.exit_width)))
							{
							for (j = 1; j < connect.exit_width; j++)
								{
								if (connect.direction == 0)
									x = connect.lc_edge.ef.coord.X + j;
								else
									x = connect.lc_edge.ef.coord.X - j;
								detail_map[x, connect.lc_edge.ef.coord.Y] = (byte)(MapCode.BLOCKED);
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
								detail_map[connect.lc_edge.ef.coord.X, y] = (byte)(MapCode.BLOCKED);
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
					if (end > NavData.rd.rect.Height)
						end = NavData.rd.rect.Height;
					for (j = start; j < end; j++)
						{
						detail_map[connect.exit_center_coord.X,j] = (byte) (MapCode.EXIT + i);
						brbm.SetPixel(connect.exit_center_coord.X,j,Color.Orange);
						}
					if ((connect.hc_edge.ef.type == NavData.FeatureType.OPENING_EDGE) && connect.hc_edge.door_side && (connect.hc_edge.ds == NavData.DoorSwing.IN))
						{
						if (((connect.direction == 90) && (connect.hc_edge.ef.coord.Y > connect.exit_width)) ||
							 ((connect.direction == 270) && (NavData.rd.rect.Height - connect.hc_edge.ef.coord.Y > connect.exit_width)))
							{
							for (j = 1;j < connect.exit_width;j++)
								{
								if (connect.direction == 270)
									y = connect.exit_center_coord.Y - j;
								else
									y = j;
								detail_map[connect.hc_edge.ef.coord.X,y] = (byte) (MapCode.BLOCKED);
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
								detail_map[x,connect.hc_edge.ef.coord.Y] = (byte) (MapCode.BLOCKED);
								brbm.SetPixel(x, connect.hc_edge.ef.coord.Y, Color.Black);
								}
							}
						}
					else if ((connect.lc_edge.ef.type == NavData.FeatureType.OPENING_EDGE) && connect.lc_edge.door_side && (connect.lc_edge.ds == NavData.DoorSwing.IN))
						{
						if (((connect.direction == 270) && (connect.lc_edge.ef.coord.Y > connect.exit_width)) ||
							 ((connect.direction == 90) && (NavData.rd.rect.Height - connect.lc_edge.ef.coord.Y > connect.exit_width)))
							{
							for (j = 1;j < connect.exit_width;j++)
								{
								if (connect.direction == 270)
									y = connect.exit_center_coord.Y - j;
								else
									y = j;
								detail_map[connect.lc_edge.ef.coord.X,y] = (byte) (MapCode.BLOCKED);
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
								detail_map[x,connect.lc_edge.ef.coord.Y] = (byte) (MapCode.BLOCKED);
								brbm.SetPixel(x, connect.lc_edge.ef.coord.Y, Color.Black);
								}
							}
						}
					}
				}
			for (i = 0;i < NavData.rd.features.Count;i++)
				{
				NavData.feature f;

				f = (NavData.feature) NavData.rd.features[i];
				if (f.type == NavData.FeatureType.CORNER)
					{
					detail_map[f.coord.X,f.coord.Y] = (byte)(Room.MapCode.CORNER + i);
					brbm.SetPixel(f.coord.X, f.coord.Y, Color.Turquoise);
					}
				else if (f.type == NavData.FeatureType.TARGET)
					{
					detail_map[f.coord.X,f.coord.Y] = (byte)(Room.MapCode.TARGET + i);
					brbm.SetPixel(f.coord.X, f.coord.Y, Color.Green);
					}
				}
			rbm = (Bitmap) brbm.Clone();
			SharedData.current_rm_map = (Bitmap) brbm.Clone();
			g = Graphics.FromImage(rbm);
			w = NavData.rd.rect.Width / MAP_SHRINK_FACTOR;
			if ((double)NavData.rd.rect.Width / MAP_SHRINK_FACTOR > w)
				w += 1;
			w = 1 << (int) (Math.Log(w,2) + 1);
			h = NavData.rd.rect.Height / MAP_SHRINK_FACTOR;
			if ((double)NavData.rd.rect.Height / MAP_SHRINK_FACTOR > h)
				h += 1;
			h = 1 << (int) (Math.Log(h,2) + 1);
			move_map = new byte[w, h];
			mbm = new Bitmap(w,h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			for (x = 0; x < w; x++)
				for (y = 0; y < h; y++)
					{
					move_map[x, y] = (int) MapCode.BLOCKED;
					mbm.SetPixel(x, y, Color.Black);
					}
			for (x = 0; x < w; x++)
				for (y = 0; y < h; y++)
					{
					w2 = x * MAP_SHRINK_FACTOR;
					h2 = y * MAP_SHRINK_FACTOR;
					open = true;
					for (i = w2;(i < w2 + MAP_SHRINK_FACTOR) && open;i++)
						for (j = h2;(j < h2 + MAP_SHRINK_FACTOR) && open;j++)
							if ((i >= NavData.rd.rect.Width) || (j >= NavData.rd.rect.Height) || (detail_map[i, j] == (int) MapCode.BLOCKED))
								{
								open = false;
								}
					if (open)
						{
						move_map[x, y] = (byte) MapCode.CLEAR;
						mbm.SetPixel(x, y, Color.White);
						}
					}
			NavData.DetailMap = detail_map;
			NavData.MoveMap = move_map;
			initial_move_map = (byte[,]) move_map.Clone();
			brbm.Save(Log.LogDir() + NavData.rd.name + " room base map " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond + ".jpg");
			mbm.Save(Log.LogDir() + NavData.rd.name + " room move map " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond + ".jpg");
		}



		static public byte[,] CreateMoveMap(byte[,] detail_map)

		{
			int i, x, y, w, h, j, w2, h2;
			bool open = false;
			byte[,] move_map;
			Bitmap mbm;

			w = NavData.rd.rect.Width / MAP_SHRINK_FACTOR;
			if ((double)NavData.rd.rect.Width / MAP_SHRINK_FACTOR > w)
				w += 1;
			w = 1 << (int)(Math.Log(w, 2) + 1);
			h = NavData.rd.rect.Height / MAP_SHRINK_FACTOR;
			if ((double)NavData.rd.rect.Height / MAP_SHRINK_FACTOR > h)
				h += 1;
			h = 1 << (int)(Math.Log(h, 2) + 1);
			move_map = new byte[w, h];
			mbm = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			for (x = 0; x < w; x++)
				for (y = 0; y < h; y++)
					{
					move_map[x, y] = (int)MapCode.BLOCKED;
					mbm.SetPixel(x, y, Color.Black);
					}
			for (x = 0; x < w; x++)
				for (y = 0; y < h; y++)
					{
					w2 = x * MAP_SHRINK_FACTOR;
					h2 = y * MAP_SHRINK_FACTOR;
					open = true;
					for (i = w2;(i < w2 + MAP_SHRINK_FACTOR) && open;i++)
						for (j = h2;(j < h2 + MAP_SHRINK_FACTOR) && open;j++)
							if ((i >= NavData.rd.rect.Width) || (j >= NavData.rd.rect.Height) || (detail_map[i, j] == (int) MapCode.BLOCKED))
								{
								open = false;
								}
					if (open)
						{
						move_map[x, y] = (byte) MapCode.CLEAR;
						mbm.SetPixel(x, y, Color.White);
						}
					}
			mbm.Save(Log.LogDir() + NavData.rd.name + " obstacle move map " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second + "-" + SharedData.GetUFileNo() + ".jpg");
			return (move_map);
		}



		public bool InFullTurnOpenArea(Point coord,int direction)

		{
			bool rtn = true;
			int i,j,cd;
			Point pivot_pt;

			pivot_pt = NavCompute.MapPoint(new Point(0,-2),direction,coord);
			cd = (int) Math.Round(SharedData.REAR_TURN_RADIUS + 1);
			if ((pivot_pt.X + cd >= NavData.rd.rect.Width - 1) || (pivot_pt.X - cd < 0) || (pivot_pt.Y + cd >= NavData.rd.rect.Height - 1) || (pivot_pt.Y - cd < 0))
				rtn = false;
			else
				{
				for (i = pivot_pt.X - cd;i <= pivot_pt.X + cd;i++)
					{
					for (j = pivot_pt.Y - cd;j <= pivot_pt.Y + cd;j++)
						{
						if (detail_map[i,j] != (int) MapCode.CLEAR)
							{
							rtn = false;
							break;
							}
						}
					if (!rtn)
						break;
					}
				}
			return(rtn);
		}


		// HOW MUCH RISK OF MISSING OBSTACLE (E.G. DOOR) WITH THIS SAMPLING APPROACH
		public bool InOpenArea(Point coord,int direction,ref ArrayList bad_pts)

		{
			bool rtn = true;
			Point pt = new Point();
			int i,j;
			bad_pt bp = new bad_pt();
			int width_clearence;

			try
			{
			width_clearence = (int) Math.Ceiling((double)SharedData.ROBOT_WIDTH / 2);
			for (i = 0;i < 5;i++)
				{
				switch(i)
					{
					case 0:
						pt.X = 0;
						pt.Y = 0;
						pt = NavCompute.MapPoint(pt,direction,coord);
						break;

					case 1:
						pt.X = -width_clearence;
						pt.Y = 0;
						pt = NavCompute.MapPoint(pt,direction,coord);
						break;

					case 2:
						pt.X = width_clearence;
						pt.Y = 0;
						pt = NavCompute.MapPoint(pt,direction,coord);
						break;

					case 3:
						pt.X = -width_clearence;
						pt.Y = -SharedData.ROBOT_LENGTH;
						pt = NavCompute.MapPoint(pt,direction,coord);
						break;

					case 4:
						pt.X = width_clearence;
						pt.Y = -SharedData.ROBOT_LENGTH;
						pt = NavCompute.MapPoint(pt,direction,coord);
						break;
					}
				if ((pt.X > NavData.rd.rect.Width - 1) || (pt.X < 0) || (pt.Y > NavData.rd.rect.Height - 1) || (pt.Y < 0))
					{
					rtn = false;
					if (bad_pts != null)
						{
						bp.index = i;
						if (pt.X >= NavData.rd.rect.Width - 1)
							bp.dist = pt.X - (NavData.rd.rect.Width - 1);
						else if (pt.X < 0)
							bp.dist = -pt.X;
						else if (pt.Y >= NavData.rd.rect.Height - 1)
							bp.dist = pt.Y - (NavData.rd.rect.Height - 1);
						else
							bp.dist = -pt.Y;
						bad_pts.Add(bp);
						}
					else
						break;
					}
				else if ((detail_map[pt.X, pt.Y] - (detail_map[pt.X, pt.Y] % 10)) == (byte)Room.MapCode.EXIT)	//STOPS CHECK AT EXITS
					break;																													//FOR PATHS WITH EXIT END PT
				else if (detail_map[pt.X, pt.Y] != (int) MapCode.CLEAR)
					{
					rtn = false;
					if (bad_pts != null)
						{
						bp.index = i;
						bp.dist = -1;
						for (j = 1;j < SharedData.ROBOT_WIDTH;j++)
							{
							if (detail_map[pt.X + j, pt.Y] == (int) MapCode.CLEAR)
								{
								bp.dist = j;
								break;
								}
							else if (detail_map[pt.X - j, pt.Y] == (int) MapCode.CLEAR)
								{
								bp.dist = j;
								break;
								}
							else if (detail_map[pt.X, pt.Y + j] == (int) MapCode.CLEAR)
								{
								bp.dist = j;
								break;
								}
							else if (detail_map[pt.X, pt.Y - j] == (int) MapCode.CLEAR)
								{
								bp.dist = j;
								break;
								}
							}
						bad_pts.Add(bp);
						}
					else
						break;
					}
				}
			}

			catch(Exception ex)
			{
			Log.LogEntry("InOpenArea exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			rtn = false;
			}

			return(rtn);
		}



		private void MultiOutput(string msg)

		{
			Speech.SpeakAsync(msg);
			Log.KeyLogEntry(msg);
		}



		private EPDirect Direction(NavData.connection connect,Point cloc)

		{
			EPDirect  epd = EPDirect.NONE;

			if (connect.direction == 90)
				{
				if (connect.exit_center_coord.Y > cloc.Y)
					epd = EPDirect.RIGHT;
				else if (connect.exit_center_coord.Y == cloc.Y)
					epd = EPDirect.EITHER;
				else
					epd = EPDirect.LEFT;
				}
			else if (connect.direction == 180) 
				{
				if (connect.exit_center_coord.X < cloc.X)
					epd = EPDirect.RIGHT;
				else if (connect.exit_center_coord.X == cloc.X)
					epd = EPDirect.EITHER;
				else
					epd = EPDirect.LEFT;
				}
			else if (connect.direction == 270)
				{
				if (connect.exit_center_coord.Y < cloc.Y)
					epd = EPDirect.RIGHT;
				else if (connect.exit_center_coord.Y == cloc.Y)
					epd = EPDirect.EITHER;
				else
					epd = EPDirect.LEFT;
				}
			else if (connect.direction == 0)
				{
				if (connect.exit_center_coord.X > cloc.X)
					epd = EPDirect.RIGHT;
				else if (connect.exit_center_coord.X == cloc.X)
					epd = EPDirect.EITHER;
				else
					epd = EPDirect.LEFT;
				}
			return(epd);
		}



		private bool DetermineExitPointParameters(NavData.connection connect,Point cloc,ref EPDirect epd,ref int exit_dist)

		{
			Point wp, op;
			int direct, rdist, ldist,cdist;
			EPDirect ed;
			bool rtn = false;

			direct = (connect.direction + 180) % 360;
			wp = NavCompute.DetermineWallProjectPt(connect.exit_center_coord, direct, false);
			op = NavCompute.DetermineVisualObstacleProjectPt(connect.exit_center_coord, wp, NavData.detail_map, false);
			if (op.IsEmpty || (op == wp))
				cdist = NavCompute.DistancePtToPt(connect.exit_center_coord, wp);
			else
				cdist = NavCompute.DistancePtToPt(connect.exit_center_coord, op);
			direct = (connect.direction + 180 + 45) % 360;
			wp = NavCompute.DetermineWallProjectPt(connect.exit_center_coord, direct, false);
			op = NavCompute.DetermineVisualObstacleProjectPt(connect.exit_center_coord, wp, NavData.detail_map, false);
			if (op.IsEmpty || (op == wp))
				rdist = NavCompute.DistancePtToPt(connect.exit_center_coord, wp);
			else
				rdist = NavCompute.DistancePtToPt(connect.exit_center_coord, op);
			direct = (connect.direction + 180 - 45) % 360;
			wp = NavCompute.DetermineWallProjectPt(connect.exit_center_coord, direct, false);
			op = NavCompute.DetermineVisualObstacleProjectPt(connect.exit_center_coord, wp, NavData.detail_map, false);
			if (op.IsEmpty || (op == wp))
				ldist = NavCompute.DistancePtToPt(connect.exit_center_coord, wp);
			else
				ldist = NavCompute.DistancePtToPt(connect.exit_center_coord, op);
			if (cdist >= STD_EXIT_DISTANCE + SharedData.REAR_TURN_RADIUS + 2)
				exit_dist = STD_EXIT_DISTANCE;
			else if (rdist >= STD_EXIT_DISTANCE + (SharedData.REAR_TURN_RADIUS/Math.Cos(45 * SharedData.DEG_TO_RAD)))
				exit_dist = STD_EXIT_DISTANCE;
			else if (ldist >= STD_EXIT_DISTANCE + (SharedData.REAR_TURN_RADIUS/Math.Cos(45 * SharedData.DEG_TO_RAD)))
				exit_dist = STD_EXIT_DISTANCE;
			else
				exit_dist = cdist - ((int) Math.Round(SharedData.REAR_TURN_RADIUS) + 2);
			ed = Direction(connect,cloc);
			if (ed != EPDirect.NONE)
				{
				rtn = true;
				if ((ed == EPDirect.RIGHT) && (rdist >= exit_dist + SharedData.REAR_TURN_RADIUS))
					epd = EPDirect.RIGHT;
				else if ((ed == EPDirect.LEFT) && (ldist >= exit_dist + SharedData.REAR_TURN_RADIUS))
					epd = EPDirect.LEFT;
				else
					{
					if (ldist == rdist)
						epd = EPDirect.EITHER;
					else if (ldist > rdist)
						epd = EPDirect.LEFT;
					else
						epd = EPDirect.RIGHT;
					}
				}
			if (rtn)
				Log.LogEntry("DetermineExitPointParameters:  exit distance - " + exit_dist + "   direction - " + epd);
			else
				Log.LogEntry("DetermineExitPointParameters: could not determine.");
			return(rtn);
		}



		// assumes that a room is rectangular with walls (and therefore exits) perpendicular to the main axises
		public Point DetermineExitPt(NavData.connection connect,Point my_coord)

		{
			int i,org_x,org_y,j;
			double dx,dy;
			int deg_factor;
			Point expt = new Point(0,0);
			int exit_direction,exit_dist = 0;
			bool rtn = false;
			EPDirect epd = EPDirect.NONE;
			ArrayList bpt = null;

			if (DetermineExitPointParameters(connect,my_coord,ref epd,ref exit_dist))
				{
				for (j = 0;j < 2;j++)
					{
					exit_direction = connect.direction;
					if (connect.direction == 180)
						expt = new Point(connect.exit_center_coord.X, connect.exit_center_coord.Y - exit_dist);
					else if (connect.direction == 90)
						expt = new Point(connect.exit_center_coord.X - exit_dist, connect.exit_center_coord.Y);
					else if (connect.direction == 0)
						expt = new Point(connect.exit_center_coord.X, connect.exit_center_coord.Y + exit_dist);
					else if (connect.direction == 270)
						expt = new Point(connect.exit_center_coord.X + exit_dist, connect.exit_center_coord.Y);
					else
						exit_direction = -1;
					if (!expt.IsEmpty)
						{
						if (j == 0)
							rtn = InFullTurnOpenArea(expt,exit_direction);
						else
							rtn = InOpenArea(expt,exit_direction,ref bpt);
						if (!rtn && !my_coord.IsEmpty)
							{
							org_x = expt.X;
							org_y = expt.Y;
							deg_factor = (int) (Math.Atan(((double) SharedData.ROBOT_WIDTH/2 + WIDTH_CLEARENCE)/exit_dist) * SharedData.RAD_TO_DEG);
							i = 0;
							do
								{
								if (connect.direction == 180)
									{
									}
								else if (connect.direction == 90)
									{
									if (epd == EPDirect.RIGHT)
										{
										dy = exit_dist * Math.Sin((i + deg_factor) * SharedData.DEG_TO_RAD);
										expt.Y = (int)(org_y - dy);
										dx = exit_dist - (exit_dist * Math.Cos((i * deg_factor) * SharedData.DEG_TO_RAD));
										expt.X = (int)(org_x + dx);
										exit_direction = (exit_direction + deg_factor) % 360;
										}
									else
										{
										dy = exit_dist * Math.Sin((i + deg_factor) * SharedData.DEG_TO_RAD);
										expt.Y = (int)(org_y + dy);
										dx = exit_dist - (exit_dist * Math.Cos((i + deg_factor) * SharedData.DEG_TO_RAD));
										expt.X = (int)(org_x + dx);
										exit_direction -= deg_factor;
										if (exit_direction < 0)
											exit_direction += 360;
										}
									}
								else if (connect.direction == 0)
									{
									if (epd == EPDirect.RIGHT)
										{
										dy = exit_dist * Math.Sin((i + deg_factor) * SharedData.DEG_TO_RAD);
										expt.Y = (int)(org_y + dy);
										dx = exit_dist - (exit_dist * Math.Cos((i * deg_factor) * SharedData.DEG_TO_RAD));
										expt.X = (int)(org_x + dx);
										exit_direction = (exit_direction + deg_factor) % 360;
										}
									else
										{
										dy = exit_dist * Math.Sin((i + deg_factor) * SharedData.DEG_TO_RAD);
										expt.Y = (int)(org_y + dy);
										dx = exit_dist - (exit_dist * Math.Cos((i + deg_factor) * SharedData.DEG_TO_RAD));
										expt.X = (int)(org_x - dx);
										exit_direction -= deg_factor;
										if (exit_direction < 0)
											exit_direction += 360;
										}
									}
								else if (connect.direction == 270)
									{
									if (epd == EPDirect.RIGHT)
										{
										dy = exit_dist * Math.Sin((i + deg_factor) * SharedData.DEG_TO_RAD);
										expt.Y = (int)(org_y + dy);
										dx = exit_dist - (exit_dist * Math.Cos((i * deg_factor) * SharedData.DEG_TO_RAD));
										expt.X = (int)(org_x - dx);
										exit_direction -= deg_factor;
										if (exit_direction < 0)
											exit_direction += 360;
										}
									else
										{
										dy = exit_dist * Math.Sin((i + deg_factor) * SharedData.DEG_TO_RAD);
										expt.Y = (int)(org_y - dy);
										dx = exit_dist - (exit_dist * Math.Cos((i + deg_factor) * SharedData.DEG_TO_RAD));
										expt.X = (int)(org_x - dx);
										exit_direction = (exit_direction + deg_factor) % 360;
										}
									}
								i += deg_factor;
								if (j == 0)
									rtn = InFullTurnOpenArea(expt, exit_direction);    //should this be replaced with FindMapObstacle
								else                                                  //modified to handle current point in connection  
									rtn = InOpenArea(expt, exit_direction, ref bpt);	//and either full turn or open area criteria
								}
							while ((rtn == false) && (i < 45));
							if (rtn)
								break;
							else
								{
								expt.X = 0;
								expt.Y = 0;
								}
							}
						else
							break;
						}
					}
				}
			Log.LogEntry("DetermineExitPoint:  X - " + expt.X + "  Y - " + expt.Y);
			return(expt);
		}



		internal void DisplayRobotLoc(Point loc,Brush br)

		{
			Point rloc = new Point();
			float heading;

			if (!SharedData.log_operations)
				return;
			heading = HeadAssembly.GetMagneticHeading();
			if ((heading >= heading_table[45]) && (heading < heading_table[135]))
				{
				rloc.X = loc.X - 14;
				rloc.Y = loc.Y - 7;
				}
			else if ((heading >= heading_table[135]) && (heading < heading_table[225]))
				{
				rloc.X = loc.X -7;
				rloc.Y = loc.Y - 14;
				}
			else if ((heading >= heading_table[225]) && (heading < heading_table[315]))
				{
				rloc.X = loc.X;
				rloc.Y = loc.Y - 7;
				}
			else
				{
				rloc.X = loc.X -7;
				rloc.Y = loc.Y;
				}
			g.FillRectangle(br, rloc.X, rloc.Y, 14, 14);
			last_robot_display_loc = rloc;
		}



		public void DisplayRobotLoc(Point loc, Brush br,int direction)

		{
			Point rloc = new Point();

			if (!SharedData.log_operations)
				return;
			if ((direction >= 45) && (direction < 135))
				{
				rloc.X = loc.X - 14;
				rloc.Y = loc.Y - 7;
				}
			else if ((direction >= 35) && (direction < 225))
				{
				rloc.X = loc.X - 7;
				rloc.Y = loc.Y - 14;
				}
			else if ((direction >= 225) && (direction < 315))
				{
				rloc.X = loc.X;
				rloc.Y = loc.Y - 7;
				}
			else
				{
				rloc.X = loc.X - 7;
				rloc.Y = loc.Y;
				}
			g.FillRectangle(br, rloc.X, rloc.Y, 14, 14);
			last_robot_display_loc = rloc;
		}



		public void DisplayPDFEllipse()

		{
			last_pdf_rect = MotionMeasureProb.PdfRectangle();
			g.DrawEllipse(Pens.Black,last_pdf_rect);
		}



		public void ClearLastPDFEllipse()

		{
			if (!last_pdf_rect.IsEmpty)
				g.DrawEllipse(Pens.White,last_pdf_rect);
		}



		public void ClearLastRobotLocation()

		{
			if (SharedData.log_operations)
				g.FillRectangle(Brushes.White,last_robot_display_loc.X,last_robot_display_loc.Y,14,14);
		}



		internal void DisplayPoint(Point pt,Brush br)

		{
			if (SharedData.log_operations)
				g.FillEllipse(br, pt.X - 2, pt.Y - 2, 4, 4);
		}



		internal void DisplaySmallPoint(Point pt,Brush br)

		{
			if (SharedData.log_operations)
				g.FillRectangle(br, pt.X, pt.Y, 1, 1);
		}




		public void DisplayRmMap()

		{
			string fname;

			if (SharedData.log_operations)
				{
				fname = Log.LogDir() + NavData.rd.name + " room map " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				rbm.Save(fname);
				Log.LogEntry("Saved " + fname );
				}
		}



		internal Bitmap RmMapClone()

		{
			return((Bitmap) rbm.Clone());
		}



		internal void LocationMessage()

		{
			NavData.location loc;
			Rectangle rect;
			string msg;
			
			rect = MotionMeasureProb.PdfRectangle();
			loc = NavData.GetCurrentLocation();
			msg = loc.rm_name + "," + loc.coord.X + "," + loc.coord.Y + "," + loc.orientation + ","  + loc.loc_name + "," + loc.ls + "," + rect.Height + "," + rect.Width + "," + rect.X + "," + rect.Y;
			UiCom.SendLocMessage(msg);
		}


		internal bool WallObsticle(Point wp,int width)

		{
			bool wall_obsticle = false;
			int i,j,start,end;

			if ((wp.Y == 0) || (wp.Y == NavData.rd.rect.Height - 1))
				{
				for (i = wp.X - (width/2); i < wp.X + (width/2);i++)
					if ((i >= 0) && (i < NavData.rd.rect.Width))
						{
						if (wp.Y == 0)
							{
							start = 0;
							end = 7;
							}
						else
							{
							start = wp.Y - 6;
							end = wp.Y + 1;
							}
						for (j = start;j < end;j++)
							if (detail_map[i, j] == (int) MapCode.BLOCKED)
								{
								wall_obsticle = true;
								break;
								}
						}
				}
			else
				{
				for (i = wp.Y - (width/2); i < wp.Y + (width/2);i++)
					if ((i >= 0) && (i < NavData.rd.rect.Height))
						{
						if (wp.X == 0)
							{
							start = 0;
							end = 7;
							}
						else
							{
							start = wp.X - 6;
							end = wp.X + 1;
							}
						for (j = start;j < end;j++)
							if (detail_map[j, i] == (int) MapCode.BLOCKED)
								{
								wall_obsticle = true;
								break;
								}
						}
				
				}
			return(wall_obsticle);
		}



		internal bool Window(Point wp,int width)

		{
			bool rtn = false;
			int i;

			if ((wp.Y == 0) || (wp.Y == NavData.rd.rect.Height - 1))
				{
				for (i = wp.X - (width/2); i < wp.X + (width/2);i++)
					if ((i >= 0) && (i < NavData.rd.rect.Width))
						if (detail_map[i, wp.Y] == (int)MapCode.WINDOW)
							{
							rtn = true;
							break;
							}
				}
			else
				{
				for (i = wp.Y - (width / 2); i < wp.Y + (width / 2); i++)
					if ((i >= 0) && (i < NavData.rd.rect.Height))
						if (detail_map[wp.X,i] == (int) MapCode.WINDOW)
							{
							rtn = true;
							break;
							}
				}
			return(rtn);
		}



		internal bool OpenWall(Point wp,int width)

		{
			bool rtn = false;
			int i;

			if ((wp.Y == 0) || (wp.Y == NavData.rd.rect.Height - 1))
				{
				for (i = wp.X - (width/2); i < wp.X + (width/2);i++)
					if ((i >= 0) && (i < NavData.rd.rect.Width))
						if (detail_map[i, wp.Y] == (int)MapCode.OPEN_WALL)
							{
							rtn = true;
							break;
							}
				}
			else
				{
				for (i = wp.Y - (width / 2); i < wp.Y + (width / 2); i++)
					if ((i >= 0) && (i < NavData.rd.rect.Height))
						if (detail_map[wp.X,i] == (int) MapCode.OPEN_WALL)
							{
							rtn = true;
							break;
							}
				}
			return(rtn);
		}



		internal bool Exit(Point wp,int width)

		{
			bool rtn = false;
			int i;

			if ((wp.Y == 0) || (wp.Y == NavData.rd.rect.Height - 1))
				{
				for (i = wp.X - (width/2); i < wp.X + (width/2);i++)
					if ((i >= 0) && (i < NavData.rd.rect.Width))
						if (detail_map[i, wp.Y] == (int)MapCode.EXIT)
							{
							rtn = true;
							break;
							}
				}
			else
				{
				for (i = wp.Y - (width / 2); i < wp.Y + (width / 2); i++)
					if ((i >= 0) && (i < NavData.rd.rect.Height))
						if (detail_map[wp.X,i] == (int) MapCode.EXIT)
							{
							rtn = true;
							break;
							}
				}
			return(rtn);
		}
		


		private bool DeterminePlace(ref Point accord,ref bool recharge)

		{
			bool rtn = false;
			ArrayList values;
			int i;
			string reply;
			NavData.room_pt rp;
			NavData.connection con;
			Point  ep;

			recharge = false;
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
				values = NavData.GetCurrentRoomConnections();
				for (i = 0; i < values.Count;i++)
					{
					con = (NavData.connection) values[i];
					ep = DetermineExitPt(con,new Point(0,0));
					if (!ep.IsEmpty)
						{
						reply = Speech.Conversation("Am I at the exit point for " + con.name + "?","responseyn",5000,true);
						if (reply == "yes")
							{
							accord = ep;
							Speech.Speak("Your response was yes");
							rtn = true;
							break;
							}
						else
							Speech.Speak("Your response was no");
						}
					}
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
						recharge = true;
						rtn = true;
						}
					else
						Speech.Speak("Your response was no");
					}
				}
			return(rtn);
		}



		private bool DetermineCurrentLocationUsable(ref bool response_received)

		{
			bool rtn = false;
			string reply;

			Speech.Speak("My current assumed location is: room " + clocation.rm_name + ", orientation " + clocation.orientation + " degrees, coordinates " + clocation.coord.X + " " + clocation.coord.Y);
			reply = Speech.Conversation("Do you agree?", "responseyn", 5000, true);
			if (reply == "yes")
				{
				Speech.Speak("Your response was yes");
				rtn = true;
				response_received = true;
				}
			else if (reply == "no")
				{
				Speech.Speak("Your response was no");
				response_received = true;;
				}
			else 
				{
				Speech.Speak("No response was received.");
				response_received = true;
				}
			return(rtn);
		}



		public bool Open()

		{
			bool rtn = false;
			NavData.location my_loc;
			bool response_received = false;

			clocation = NavData.GetCurrentLocation();
			NavData.LoadRoomdata(clocation.rm_name);
			if (NavData.rd.name == clocation.rm_name)
				{
				CreateHeadingTable();
				CreateRoomMap();
				DisplayRobotLoc(clocation.coord, Brushes.Yellow,clocation.orientation);
				if (HeadAssembly.CurrentHeadAngle() != HeadAssembly.HA_CENTER_ANGLE)
					HeadAssembly.SendHeadAngle(HeadAssembly.HA_CENTER_ANGLE, true);
				my_loc = rloc.DetermineLocation(new Point(0,0),true);
				if (my_loc.coord.IsEmpty)
					{
					string room;
					int orient;

					try
					{
					Speech.Speak("I could not determine my location.");
					if (DetermineCurrentLocationUsable(ref response_received))
						{
						clocation.ls = NavData.LocationStatus.USR;
						NavData.SetCurrentLocation(clocation);
						MotionMeasureProb.UserLocalize(new MotionMeasureProb.Pose(clocation.coord,clocation.orientation));
						my_loc = rloc.DetermineLocation(new Point(0,0),true);
						if (my_loc.coord.IsEmpty)
							{
							Speech.Speak("I could not determine my location.");
							clocation.ls = NavData.LocationStatus.UNKNOWN;
							NavData.SetCurrentLocation(clocation);
							}
//						rloc.DetermineDRLocation(clocation, ref my_loc, true, clocation.coord, ref verified, false);
						}
					else if (response_received)
						{
						room = Speech.DetermineRoom(false);
						if (room.Length > 0)
							{
							if (clocation.rm_name != room)
								{
								NavData.LoadRoomdata(room);
								clocation.rm_name = room;
								CreateHeadingTable();
								CreateRoomMap();
								my_loc.rm_name = room;
								}
							orient = Speech.DetermineOrientation(false);
							if (orient != -1)
								{
								Point acoord = new Point(0,0);
								bool recharge = false;

								my_loc.orientation = orient;
								if (DeterminePlace(ref acoord,ref recharge))
									{
									my_loc.coord = acoord;
									if (recharge)
										my_loc.loc_name = SharedData.RECHARGE_LOC_NAME;
									else
										my_loc.loc_name = "";
									my_loc.ls = NavData.LocationStatus.USR;
									NavData.SetCurrentLocation(my_loc);
									MotionMeasureProb.Init(new MotionMeasureProb.Pose(my_loc.coord, my_loc.orientation), true);
									if (!rloc.DetermineDRLocation(ref my_loc, true, new Point(0,0)))
										{
										Speech.Speak("I could not determine my location.");
										my_loc.ls = NavData.LocationStatus.UNKNOWN;
										NavData.SetCurrentLocation(my_loc);
										}
									}
								}
							}
						}
					}

					catch(Exception ex)
					{
					MultiOutput("Determine location converstation exception: " + ex.Message);
					Log.LogEntry("Source: " + ex.Source);
					Log.LogEntry("Stack trace: " + ex.StackTrace);
					}

					}
				if (!my_loc.coord.IsEmpty)
					{
					DisplayRobotLoc(my_loc.coord, Brushes.White);
					clocation = my_loc;
					if (NavCompute.DistancePtToPt(my_loc.coord, clocation.coord) > MAX_DISTANCE_NAMED_PT)
						clocation.loc_name = "";
					NavData.SetCurrentLocation(clocation);
					if (clocation.ls == NavData.LocationStatus.VERIFIED)
						MotionMeasureProb.Init(new MotionMeasureProb.Pose(clocation.coord, clocation.orientation), false);
					DisplayRobotLoc(my_loc.coord, Brushes.Green);
					DisplayPDFEllipse();
					DisplayRmMap();
					LocationMessage();
					SharedData.current_rm = this;
					rtn = true;
					}
				}
			return(rtn);
		}



		public bool Open(NavData.connection connect)

		{
			bool rtn = false;
			rm_location my_loc = new rm_location();

			clocation = NavData.GetCurrentLocation();
			NavData.LoadRoomdata(clocation.rm_name);
			if (NavData.rd.name == clocation.rm_name)
				{
				CreateHeadingTable();
				CreateRoomMap();
				DisplayRobotLoc(clocation.coord, Brushes.Yellow,clocation.orientation);
				if (HeadAssembly.CurrentHeadAngle() != HeadAssembly.HA_CENTER_ANGLE)
					HeadAssembly.SendHeadAngle(HeadAssembly.HA_CENTER_ANGLE, true);
				if (clocation.entrance)
					{
					my_loc.coord = clocation.coord;
					rloc.DetermineDRLocation(ref clocation,false,new Point(0,0));
					if (NavCompute.DistancePtToPt(my_loc.coord, clocation.coord) > MAX_DISTANCE_NAMED_PT)
						clocation.loc_name = "";
					clocation.entrance = false;
					NavData.SetCurrentLocation(clocation);
					DisplayRobotLoc(my_loc.coord, Brushes.Green);
					DisplayPDFEllipse();
					DisplayRmMap();
					LocationMessage();
					SharedData.current_rm = this;
					rtn = true;
					}
				else
					{
					clocation.ls = NavData.LocationStatus.UNKNOWN;
					LocationMessage();
					}
				}
			return(rtn);
		}



		public void OpenLimited(string rm_name)

		{
			NavData.LoadRoomdata(rm_name); 
			CreateHeadingTable();
			CreateRoomMap();
		}


		public void Close()

		{
			clocation.rm_name = "";
			clocation.coord = new Point(0, 0);
			clocation.orientation = -1;
			clocation.ls = NavData.LocationStatus.UNKNOWN;
			NavData.rd.name = "";
			detail_map = null;
			NavData.DetailMap = detail_map;
			move_map = null;
			NavData.MoveMap = move_map;
			SharedData.current_rm = null;
			SharedData.current_rm_map = null;
		}



		public Location.LocationDeterminationStatus VerifyLocation()

		{
			Location.LocationDeterminationStatus rtn = Location.LocationDeterminationStatus.FAILED;
			NavData.location my_loc;

			try
			{
			clocation = NavData.GetCurrentLocation();
			if (NavData.rd.name == clocation.rm_name)
				{
				rbm = (Bitmap) brbm.Clone();
				g = System.Drawing.Graphics.FromImage(rbm);
				if (HeadAssembly.CurrentHeadAngle() != HeadAssembly.HA_CENTER_ANGLE)
					HeadAssembly.SendHeadAngle(HeadAssembly.HA_CENTER_ANGLE, true);
				my_loc = rloc.DetermineLocation(new Point(0,0),true);
				if (!my_loc.coord.IsEmpty)
					{
					clocation = my_loc;
					if (NavCompute.DistancePtToPt(my_loc.coord,clocation.coord) > MAX_DISTANCE_NAMED_PT)
						clocation.loc_name = "";
					NavData.SetCurrentLocation(clocation);
					DisplayRobotLoc(my_loc.coord, Brushes.Green);
					DisplayPDFEllipse();
					DisplayRmMap();
					LocationMessage();
					rtn = Location.LocationDeterminationStatus.GOOD_MATCH;
					}
				else
					rtn = Location.LocationDeterminationStatus.NO_MATCH;
				}
			}

			catch(Exception ex)
			{
			Log.LogEntry("VerifyLocation exception: " + ex.Message);
			Log.LogEntry("  stack trace: " + ex.StackTrace);
			rtn = Location.LocationDeterminationStatus.FAILED;
			}

			return(rtn);
		}



		public bool GoToEntryPoint(NavData.connection connect)

		{
			bool rtn = false;
			Point empt;
			NavData.location my_loc;
			NavData.room_pt rpt = new NavData.room_pt();

			my_loc = NavData.GetCurrentLocation();
			empt = DetermineExitPt(connect,my_loc.coord);
			if (!empt.IsEmpty && run)
				{
				rpt.coord = empt;
				if (rmove.MoveToPoint(empt,ignor,true) == Move.MoveToPointResult.TRUE)
					rtn = true;
				}
			else
				MultiOutput("Could not determine exit point");
			return (rtn);
		}



		public bool GoToExitPoint(NavData.connection connect,ref int dist)

		{
			bool rtn = false;
			Point empt;
			rm_location my_loc;
			NavData.room_pt rpt = new NavData.room_pt();
			ObstacleAdjust oa = new ObstacleAdjust();
			Door door = new Door();
			int turn_angle = 0;

			my_loc.coord = clocation.coord;
			my_loc.orientation = clocation.orientation;
			empt = DetermineExitPt(connect,my_loc.coord);
			if (!empt.IsEmpty && run)
				{
				rpt.coord = empt;
				if (rmove.GoToRoomPoint(rpt, connect.exit_center_coord))
					{
					if (rmove.TurnToFaceMP(connect.exit_center_coord))
						{
						if (door.ExitPosition(connect,ref turn_angle,ref dist))
							{
							clocation = NavData.GetCurrentLocation();
							if ((Math.Abs(turn_angle) < SharedData.MIN_TURN_ANGLE) || Turn.TurnAngle(turn_angle))
								{
								if (Math.Abs(turn_angle) >= SharedData.MIN_TURN_ANGLE)
									{
									clocation.orientation = (clocation.orientation - turn_angle) % 360;
									if (clocation.orientation < 0)
										clocation.orientation += 360;
									}
								clocation.coord = NavCompute.MapPoint(new Point(0, -dist), clocation.orientation, connect.exit_center_coord);
								clocation.ls = NavData.LocationStatus.DR;
								NavData.SetCurrentLocation(clocation);
								MotionMeasureProb.SinglePointLocalize(new MotionMeasureProb.Pose(clocation.coord,clocation.orientation));
								ClearLastPDFEllipse();
								ClearLastRobotLocation();
								DisplayRobotLoc(clocation.coord, Brushes.Purple,clocation.orientation);
								DisplayPDFEllipse();
								DisplayRmMap();
								LocationMessage();
								if (oa.DepartObstacleAvoid(dist,SharedData.FRONT_SONAR_CLEARANCE,2,(int) Math.Round(Math.Asin(((double)connect.exit_width / 4) /dist) * SharedData.RAD_TO_DEG)))
									rtn = true;
								else
									MultiOutput("Could not find clear path through exit.");
								}
							else 
								MultiOutput("Could not make final turn to exit.");
							}
						else
							MultiOutput("Could not establish exit position.");
						}
					else
						MultiOutput("Could not face exit");
					}
				}
			else if (run)
				MultiOutput("Could not determine exit point");
			return(rtn);
		}



		private bool GoToRechargeStation()

		{
			bool rtn = false;
			NavData.room_pt rpt = new NavData.room_pt();
			RechargeDock rcd = new RechargeDock();

			rpt.coord = NavData.rd.recharge_station.coord;
			if (NavData.rd.recharge_station.direction == 0)
				rpt.coord.Y += SharedData.RECHARGE_OFFSET;
			else if (NavData.rd.recharge_station.direction == 90)
				rpt.coord.X -= SharedData.RECHARGE_OFFSET;
			else if (NavData.rd.recharge_station.direction == 180)
				rpt.coord.Y -= SharedData.RECHARGE_OFFSET;
			else if (NavData.rd.recharge_station.direction == 270)
				rpt.coord.X += SharedData.RECHARGE_OFFSET;
			else
				rpt.coord = new Point(0,0);
			if (!rpt.coord.IsEmpty)
				{
				if (rmove.GoToRoomPoint(rpt, NavData.rd.recharge_station.coord))
					{
					if (run)
						if (rmove.TurnToFaceMP(NavData.rd.recharge_station.coord))
							{
							if (run)
								{
								if ((rtn = rcd.StartDocking(NavData.rd.recharge_station,ref clocation,false)))
									{
									ClearLastRobotLocation();
									ClearLastPDFEllipse();
									clocation.loc_name = SharedData.RECHARGE_LOC_NAME;
									DisplayRobotLoc(clocation.coord, Brushes.Purple, clocation.orientation);
									DisplayPDFEllipse();
									DisplayRmMap();
									NavData.SetCurrentLocation(clocation);
									LocationMessage();
									}
								else if (rcd.LastError() == RechargeDock.THRESHOLD_ERROR)
									{
									clocation = NavData.GetCurrentLocation();
									NavData.SetCurrentLocation(clocation);
									Log.LogEntry(rcd.LastError());
									MultiOutput("Docking attempt failed." + rcd.LastError());
									}
								else
									{
									clocation = NavData.GetCurrentLocation();
									clocation.ls = NavData.LocationStatus.UNKNOWN;
									NavData.SetCurrentLocation(clocation);
									LocationMessage();
									Log.LogEntry(rcd.LastError());
									MultiOutput("Docking attempt failed." + rcd.LastError());
									}
								}
							}
						else
							{
							clocation.ls = NavData.LocationStatus.UNKNOWN;
							NavData.SetCurrentLocation(clocation);
							MultiOutput("Could not face recharge station.");
							}
					}
				}
			else
				MultiOutput("Could not determine recharge approach point.");
			return(rtn);
		}



		public bool GoToPoint(string pt_name)

		{
			bool rtn = false;
			ArrayList rpts;
			int i;
			Location.LocationDeterminationStatus ldstat;
			
			clocation = NavData.GetCurrentLocation();
			if (clocation.loc_name != pt_name)
				{
				try
				{
				Log.KeyLogEntry("Starting go to " + pt_name);
				if ((Kinect.nui != null) && (Kinect.nui.IsRunning) && (MotionControl.Connected()) && (HeadAssembly.Connected()))
					{
					ldstat  = VerifyLocation();
					if (ldstat != Location.LocationDeterminationStatus.GOOD_MATCH)
						VerifyLoc  = true;
					Run = true;
					if (pt_name.Equals("exit"))
						{
						if (NavData.rd.connections.Count > 0)
							{
							int dist = 0;

							Log.LogEntry("Going to " + ((NavData.connection) NavData.rd.connections[0]).name + " exit point");
							if ((rtn = GoToExitPoint((NavData.connection) NavData.rd.connections[0],ref dist)))
								{
								clocation = NavData.GetCurrentLocation();
								clocation.loc_name = pt_name;
								NavData.SetCurrentLocation(clocation);
								}
							}
						else
							MultiOutput("No exit point defined.");
						}
					else if (pt_name.StartsWith("exit"))
						{
						string[] words;
						int dist = 0;

						words = pt_name.Split('-');
						if (words.Length > 1)
							{
							int index = int.Parse(words[1]);
							Log.LogEntry("Going to " + ((NavData.connection) NavData.rd.connections[index]).name + " exit point");
							if ((rtn = GoToExitPoint((NavData.connection) NavData.rd.connections[index],ref dist)))
								{
								clocation = NavData.GetCurrentLocation();
								clocation.loc_name = pt_name;
								NavData.SetCurrentLocation(clocation);
								}
							}
						}
					else if (pt_name.Equals("recharge"))
						{
						if (!NavData.rd.recharge_station.coord.IsEmpty)
							{
							Log.LogEntry("Going to " + NavData.rd.name + " recharge station");
							if ((rtn = GoToRechargeStation()))
								{
								clocation = NavData.GetCurrentLocation();
								clocation.loc_name = pt_name;
								NavData.SetCurrentLocation(clocation);
								}
							}
						}
					else
						{
						rpts = NavData.GetPoints(clocation.rm_name);
						for (i = 0;i < rpts.Count;i++)
							{
							if (((NavData.room_pt) rpts[i]).name.Equals(pt_name))
								{
								rtn = rmove.GoToRoomPoint((NavData.room_pt) rpts[i],new Point(0,0));
								clocation = NavData.GetCurrentLocation();
								if (rtn)
									{
									clocation.loc_name = pt_name;
									NavData.SetCurrentLocation(clocation);
									}
								break;
								}
							}
						if (i == rpts.Count)
							MultiOutput("Could not find the point named " + pt_name);
						}
					run = false;
					}
				else
					Log.LogEntry("Kinect, MotionControl and/or HeadAssembly not connected");
				}

				catch(Exception ex)
				{
				Log.LogEntry("GoToPoint exception: " + ex.Message);
				Log.LogEntry("  stack trace: " + ex.StackTrace);
				}

				}
			else
				{
				Log.LogEntry("At requested move point " + pt_name);
				rtn = true;
				}

			return (rtn);
		}




		public bool Leave(string dest_name,ref Point dif,ref int dist)

		{
			bool rtn = false;
			int i;
			NavData.connection connect = new NavData.connection();
			Location.LocationDeterminationStatus ldstat;

			try
			{
			clocation = NavData.GetCurrentLocation();
			Log.KeyLogEntry("Starting " + clocation.rm_name + " leave");
			if ((Kinect.nui != null) && (Kinect.nui.IsRunning) && (MotionControl.Connected()) && (HeadAssembly.Connected()))
				{
				Run = true;
				ldstat = VerifyLocation();
				if (ldstat == Location.LocationDeterminationStatus.NO_MATCH)
					VerifyLoc = true;
				if (ldstat != Location.LocationDeterminationStatus.FAILED)
					{
					for (i = 0; i < NavData.rd.connections.Count; i++)
						{
						connect = (NavData.connection) NavData.rd.connections[i];
						if (connect.name == dest_name)
							{
							break;
							}
						}
					if ((connect.name == dest_name) && (connect.exit_width > SharedData.ROBOT_WIDTH + EXIT_MARGIN) && run)
						{
						if (Run && GoToExitPoint(connect,ref dist))
							{
							if (SendCommand(SharedData.FORWARD + " "  + dist, (int)(((dist / 7.2) + 2) * 100)))
								{
								ClearLastRobotLocation();
								ClearLastPDFEllipse();
								clocation = NavData.GetCurrentLocation();
								clocation.coord = NavCompute.MapPoint(new Point(0,dist),clocation.orientation,clocation.coord);
								dif.X = clocation.coord.X - connect.exit_center_coord.X;
								dif.Y = clocation.coord.Y - connect.exit_center_coord.Y;
								DisplayRobotLoc(clocation.coord, Brushes.Purple);
								DisplayPDFEllipse();
								DisplayRmMap();
								clocation.ls = NavData.LocationStatus.DR;
								NavData.SetCurrentLocation(clocation);
								LocationMessage();
								rtn = true;
								}
							else
								{
								SharedData.med.mt = SharedData.MoveType.LINEAR;
								SharedData.med.et = rmove.MotionErrorType(last_error);
								SharedData.med.ob_descript = null;
								}
							}
						else if (Run)
							MultiOutput("Move to exit point failed.");
						}
					else if (Run)
						MultiOutput("Could not find useable connection data");
					}
				else if (Run)
					MultiOutput("Location status is unknown, can not proceed.");
				}
			else
				Log.LogEntry("Kinect, MotionControl and/or HeadAssembly not connected");
			}

			catch(Exception ex)
			{
			MultiOutput("Leave exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			return(rtn);
		}



		public void Stop()

		{
			run = false;
		}



		public bool Run

		{
			get
				{
				bool r;
				lock(run_obj)
					{
					r = run;
					}
				return r;
				}

			set
				{
				lock(run_obj)
					{
					run = value;
					}
				}
		}



		public bool VerifyLoc

		{
			get
				{
				bool vl;
				lock(vl_obj)
					{
					vl = verify_location;
					}
				return vl;
				}

			set
				{
				lock(vl_obj)
					{
					verify_location  = value;
					}
				}
		}

		}
	}
