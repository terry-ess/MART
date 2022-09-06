using System;
using System.Drawing;
using System.Data.SQLite;
using System.Threading;
using BuildingDataBase;

namespace DBMap
	{
	static class SharedData
		{
		public const double MM_TO_FT = 0.00328084;
		public const double MM_TO_IN = 0.0393701; 
		public const double FT_TO_MM = 304.8;
		public const double IN_TO_MM = 25.4;
		public const double RAD_TO_DEG = 180/Math.PI;
		public const double DEG_TO_RAD = Math.PI/180;

		public const int MAX_DIST_DIF = 3;

		public const int ROBOT_WIDTH = 19;
		public const int ROBOT_CORE_WIDTH = 15;
		public const int ROBOT_LENGTH = 15;
		public const int LIDAR_OFFSET = 2;
		public static double TURN_RADIUS;
		public static double RADIUS_ANGLE;

		public const int MIN_TURN_ANGLE = 2;

		public const int FRONT_SONAR_CLEARANCE = 15;
		public const int REAR_SONAR_CLEARANCE = 24;

		public const int MIN_BATTERY_VOLTAGE = 24;

		public const string CAL_FILE_EXT = ".cal";
		public const string CAL_SUB_DIR = "\\cal\\";
		public const string LOG_FILE_EXT = ".csv";
		public const string TEXT_TILE_EXT = ".txt";
		public const string PIC_FILE_EXT = ".jpg";
		public const string DATA_SUB_DIR = "\\data\\";


		public const string INSUFFICENT_REAR_CLEARANCE = "Insufficent rear clearance.";
		public const string INSUFFICENT_FRONT_CLEARANCE = "Insufficent front clearance.";
		public const string MPU_FAIL = "MPU6050 connection lost";
		public const string START_TIMEOUT = "start timedout";
		public const string STOP_TIMEOUT = "stop timedout";
		public const string UDP_TIMEOUT = "UDP receive timedout";

		public const string HEADING_TABLE_FILE = "heading";

		public const string RIGHT_TURN = "MCR";
		public const string LEFT_TURN = "MCL";
//		public const string FORWARD = "MCF";
		public const string FORWARD = "TF";
//		public const string BACKWARD = "MCB";
		public const string BACKWARD = "TB";
//		public const string DIST_MOVED = "MCD";
		public const string DIST_MOVED = "TD";

		public const int RECHARGE_OFFSET = 72;

		public const int LIDAR_MAX_DIST = 240;
		public const int KINECT_MAX_DIST = 156;
		public const int KINECT_MIN_DIST = 30;
		public const int KINECT_HOR_VIEW = 60;
		public const int MIN_PERP_ANGLE = 20;
		public const int MIN_WALL_DIST = 18;

		public enum RobotLocation { FRONT, REAR, RIGHT, LEFT };

		public enum MotionErrorType { NONE, MPU, START_TIMEOUT, STOP_TIMEOUT, INIT_FAIL, UDP_TIMEOUT, OBSTACLE, TURN_NOT_SAFE };




		static SharedData()

		{
			TURN_RADIUS = Math.Sqrt(Math.Pow(SharedData.ROBOT_LENGTH,2) + Math.Pow((double) SharedData.ROBOT_CORE_WIDTH/2,2));
			RADIUS_ANGLE = (Math.Atan(((double)SharedData.ROBOT_LENGTH / ((double) SharedData.ROBOT_WIDTH / 2))) * SharedData.RAD_TO_DEG);
		}



		static public void CreateRoomMap(RoomData.room_data rd,ref byte[,] detail_map,ref Bitmap brbm)

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
