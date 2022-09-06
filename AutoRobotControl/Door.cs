using System;
using System.Collections;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;


namespace AutoRobotControl
	{
	public class Door: FeatureMatch
		{

		public const int SAMPLE_ROW = 120;
		private const int DOOR_EDGE_MIN_ABRUPT_DIF = 12;
		private const int DIST_RANGE = 30;
		private const double WALL_DIST_RANGE = .4;
		private const int WALL_DIST_RANGE5 = 3;
		private const int MIN_WALL_LEN = 2;
		private const int EDGE_DROP_DIST = 2;
		private const int CLEAR_MARGIN = 3;
		private const int MIN_ADJUST_ANGLE = 4;
		private const double DELTA_SIG3TOSIG7 = 1.3333;
		private const int CONNECTOR_DEPTH = 5;

		public enum Edge {NONE,HIGH_COL, LOW_COL, BOTH};
		private enum SearchState {START,WALL_FOUND,EDGE_FOUND,EDGE_CONFIRMED,STOP};

		public struct exit_pt
			{
			public int hc_dist;
			public Room.EdgeType hc_et;
			public double hc_ra;
			public int hc_orient;
			public int lc_dist;
			public Room.EdgeType lc_et;
			public double lc_ra;
			public int lc_orient;
			public Door.Edge edge;
			};

		public struct opening_param
			{
			public int max_wall_dist_limit;
			public int min_wall_dist_limit;
			public int expected_wall_dist;
			public int opening_detect_limit;
			public int right_open_search_limit;
			public int left_open_search_limit;
			public int no_edges ;
			};

		private short[] depthdata = null;
		private AutoResetEvent frame_complete = new AutoResetEvent(false);


		public Room.feature_match MatchKinect(NavData.feature f,params object[] obj)

		{
			Room.feature_match fm = new Room.feature_match();
			NavData.room_data rd;
			Point ecoord;
			exit_pt ep = new exit_pt();
			int i;
			NavData.connection connect;

			fm.matched = false;
			if (obj.Length == 2)
				{

				try
				{
				rd = (NavData.room_data) obj[0];
				ecoord = (Point) obj[1];
				for (i = 0;i < rd.connections.Count;i++)
					{
					connect = (NavData.connection) rd.connections[i];
					if (f.coord == connect.hc_edge.ef.coord)
						{
						ep.hc_et = Room.EdgeType.EITHER;
						ep.hc_dist = NavCompute.DistancePtToPt(ecoord,f.coord);
						ep.edge = Edge.NONE;
						ep.lc_et = Room.EdgeType.NONE;
						FindDoorEdge(ref ep,15);
						break;
						}
					else if (f.coord == connect.lc_edge.ef.coord)
						{
						ep.lc_et = Room.EdgeType.EITHER;
						ep.lc_dist = NavCompute.DistancePtToPt(ecoord,f.coord);
						ep.edge = Edge.NONE;
						ep.hc_et = Room.EdgeType.NONE;
						FindDoorEdge(ref ep,-15);
						break;
						}
					}
				if (ep.edge != Edge.NONE)
					{
					fm.matched = true;
					if (ep.edge == Edge.HIGH_COL)
						{
						fm.distance = ep.hc_dist;
						fm.ra = ep.hc_ra;
						}
					else
						{
						fm.distance = ep.lc_dist;
						fm.ra = ep.lc_ra;
						}
					}
				}

				catch(Exception ex)
				{
				Log.LogEntry("Door.MatchKinect exception: " + ex.Message);
				Log.LogEntry("                 stack trace: " + ex.StackTrace);
				}

				}
			return(fm);
		}



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
				rtn = false;
			else
				rtn = true;
			return(rtn);
		}



		private bool FindDoorEdges(ref exit_pt ep)

		{
			bool rtn = false;
			int head_heading;

			ep.edge = Edge.NONE;
			head_heading = HeadAssembly.PanAngle();
			FindDoorEdge(ref ep,0);
			if (((ep.edge == Edge.NONE) || (ep.edge == Edge.LOW_COL)) && (ep.hc_et != Room.EdgeType.NONE))
				{
				HeadAssembly.Pan(head_heading - 30,true);
				FindDoorEdge(ref ep,15);
				if ((ep.edge == Edge.BOTH) || (ep.edge == Edge.HIGH_COL))
					{
					ep.hc_ra -= HeadAssembly.PanAngle();
					Log.LogEntry("Adjusted ra: " + ep.hc_ra);
					}
				}
			if (((ep.edge == Edge.NONE) || (ep.edge == Edge.HIGH_COL)) && (ep.lc_et != Room.EdgeType.NONE))
				{
				HeadAssembly.Pan(head_heading + 30,true);
				FindDoorEdge(ref ep,-15);
				if ((ep.edge == Edge.BOTH) || (ep.edge == Edge.LOW_COL))
					{
					ep.lc_ra -= HeadAssembly.PanAngle();
					Log.LogEntry("Adjusted ra: " + ep.lc_ra);
					}				
				}
			HeadAssembly.Pan(head_heading,true);
			if (ep.edge != Edge.NONE)
				rtn = true;
			return(rtn);
		}



		private void FindDoorEdge(ref exit_pt ep,int offset)

		{
			SensorFusion sf = new SensorFusion();
			int ra = 0,dist = 0,orient = 0;

			if (depthdata == null)
				depthdata = new short[Kinect.nui.DepthStream.FramePixelDataLength];
			while (!Kinect.GetDepthFrame(ref depthdata, 40))
				Thread.Sleep(10);
			if (((ep.edge == Edge.NONE) || (ep.edge == Edge.HIGH_COL)) && (ep.lc_et != Room.EdgeType.NONE))
				{
				if (sf.KinectFindEdge(ref depthdata,ref ra,ref dist,ref orient,offset,false))
					{
					if (ep.edge == Edge.NONE)
						ep.edge  = Edge.LOW_COL;
					else
						ep.edge = Edge.BOTH;
					ep.lc_dist = dist;
					ep.lc_ra = ra;
					ep.lc_orient = orient;
					}
				}
			if (((ep.edge == Edge.NONE) || (ep.edge == Edge.LOW_COL)) && (ep.hc_et != Room.EdgeType.NONE))
				{
				if (sf.KinectFindEdge(ref depthdata, ref ra, ref dist, ref orient,offset, true))
					{
					if (ep.edge == Edge.NONE)
						ep.edge = Edge.HIGH_COL;
					else
						ep.edge = Edge.BOTH;
					ep.hc_dist = dist;
					ep.hc_ra = ra;
					ep.hc_orient = orient;
					}
				}
		}



		public opening_param DetermineOpeningParam(NavData.connection connect, NavData.location expect)

		{
			opening_param rtn = new AutoRobotControl.Door.opening_param();
			Point pdf_limits = new Point();
			Rectangle pdf_rect;

			pdf_rect = MotionMeasureProb.PdfRectangle();
			pdf_limits.X = (int) Math.Round((double) pdf_rect.Width / 2);
			pdf_limits.Y = (int) Math.Round((double) pdf_rect.Height / 2);
			if ((connect.direction == 0) || (connect.direction == 180))
				{
				if (connect.direction == 0)
					{
					rtn.max_wall_dist_limit = expect.coord.Y + pdf_limits.Y;
					rtn.min_wall_dist_limit = expect.coord.Y - pdf_limits.Y;
					rtn.expected_wall_dist = expect.coord.Y;
					}
				else
					{
					rtn.max_wall_dist_limit = (NavData.rd.rect.Height - expect.coord.Y) + pdf_limits.Y;
					rtn.min_wall_dist_limit = (NavData.rd.rect.Height - expect.coord.Y) - pdf_limits.Y;
					rtn.expected_wall_dist = (NavData.rd.rect.Height - expect.coord.Y);
					}
				rtn.opening_detect_limit = (int) Math.Round(rtn.max_wall_dist_limit + (pdf_limits.Y * DELTA_SIG3TOSIG7)) + CONNECTOR_DEPTH;
				if (connect.lc_edge.ef.type == NavData.FeatureType.OPENING_EDGE)
					{
					if (((connect.direction == 0) && (connect.lc_edge.ef.coord.X < expect.coord.X + pdf_limits.X)) ||
						((connect.direction == 180) && (connect.lc_edge.ef.coord.X > expect.coord.X + pdf_limits.X)))
						rtn.left_open_search_limit = Math.Abs(connect.lc_edge.ef.coord.X - (expect.coord.X + pdf_limits.X));
					rtn.no_edges += 1;
					}
				if (connect.hc_edge.ef.type == NavData.FeatureType.OPENING_EDGE)
					{
					if (((connect.direction == 0) && (connect.hc_edge.ef.coord.X > expect.coord.X - pdf_limits.X)) ||
						((connect.direction == 180) && (connect.hc_edge.ef.coord.X < expect.coord.X - pdf_limits.X)))
						rtn.right_open_search_limit = Math.Abs((expect.coord.X - pdf_limits.X) - connect.hc_edge.ef.coord.X);
					rtn.no_edges += 1;
					}
				}
			else
				{
				if (connect.direction == 270)
					{
					rtn.max_wall_dist_limit = expect.coord.X + pdf_limits.X;
					rtn.min_wall_dist_limit = expect.coord.X - pdf_limits.X;
					rtn.expected_wall_dist = expect.coord.X;
					}
				else
					{
					rtn.max_wall_dist_limit = (NavData.rd.rect.Width - expect.coord.X) + pdf_limits.X;
					rtn.min_wall_dist_limit = (NavData.rd.rect.Width - expect.coord.X) - pdf_limits.X;
					rtn.expected_wall_dist = (NavData.rd.rect.Width - expect.coord.X);
					}
				rtn.opening_detect_limit = (int)Math.Round(rtn.max_wall_dist_limit + (pdf_limits.X * DELTA_SIG3TOSIG7)) + CONNECTOR_DEPTH;
				if (connect.lc_edge.ef.type == NavData.FeatureType.OPENING_EDGE)
					{
					if (((connect.direction == 90) && (connect.lc_edge.ef.coord.Y < expect.coord.Y + pdf_limits.Y)) ||
						((connect.direction == 270) && (connect.lc_edge.ef.coord.Y > expect.coord.Y + pdf_limits.Y)))
						rtn.left_open_search_limit = Math.Abs(connect.lc_edge.ef.coord.Y - (expect.coord.Y + pdf_limits.Y));
					rtn.no_edges += 1;
					}
				if (connect.hc_edge.ef.type == NavData.FeatureType.OPENING_EDGE)
					{
					if (((connect.direction == 90) && (connect.hc_edge.ef.coord.Y > expect.coord.Y - pdf_limits.Y)) ||
						((connect.direction == 270) && (connect.hc_edge.ef.coord.Y < expect.coord.Y - pdf_limits.Y)))
						rtn.right_open_search_limit = Math.Abs((expect.coord.Y - pdf_limits.Y) - connect.hc_edge.ef.coord.Y);
					rtn.no_edges += 1;
					}
				}
			return (rtn);
		}



		public int SearchEdges(ArrayList sdata,int start_indx,int angle_shift, int ewidth, int edge_detect,int expected_dist,ref int right_edge_indx,ref int left_edge_indx)

		{
			bool right;
			Rplidar.scan_data sd = new Rplidar.scan_data();
			int i,j,indx,swidth = 0,search_angle;
			double sangle,y,x,startx;

			sd = (Rplidar.scan_data)sdata[start_indx];
			startx = sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD);
			search_angle = (int)Math.Ceiling(Math.Atan(((double)ewidth) /expected_dist) * SharedData.RAD_TO_DEG) + 2;
			for (i = 0;i < 2;i++)
				{
				if (i == 0)
					right = true;
				else
					right = false;
				for (j = 0; j < sdata.Count; j++)
					{
					if (right)
						indx = (start_indx + j) % sdata.Count;
					else
						{
						indx = start_indx - j;
						if (indx < 0)
							indx += sdata.Count;
						}
					sd = (Rplidar.scan_data)sdata[indx];
					sangle = (sd.angle + angle_shift) % 360;
					if (sangle < 0)
						sangle += 360;
					y = sd.dist * Math.Cos(sangle * SharedData.DEG_TO_RAD);
					if (y <= edge_detect)
						{
						x = sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD);
						if (i == 0)
							{
							swidth = (int) Math.Round(Math.Abs(x - startx));
							right_edge_indx = indx;
							}
						else
							{
							swidth += (int) Math.Round(Math.Abs(x - startx));
							left_edge_indx = indx;
							}
						break;
						}
					else if (NavCompute.AngularDistance(sd.angle, 0) > search_angle)
						{
						swidth = -1;
						break;
						}
					}
				if (swidth == -1)
					break;
				}
			return (swidth);
		}




		public bool SearchOpeningEdge(ArrayList sdata,int start_indx,int angle_shift,int ewidth,int opening_xdist_limit,int open_detect,int edge_detect,int expected_dist,bool right,ref int width,ref int right_edge_indx,ref int left_edge_indx)

		{
			bool rtn = false,opening_found = false;
			Rplidar.scan_data sd = new Rplidar.scan_data();
			int i,indx,search_angle,oangle = 0,sangle;
			double x,y,startx,oxdist = 0;

			Log.LogEntry("SearchOpeningEdge: " + start_indx + " " + angle_shift + " " + ewidth + " " + opening_xdist_limit + " " + open_detect + " " + edge_detect + " " + expected_dist + " " + right);
			sd = (Rplidar.scan_data)sdata[start_indx];
			sangle = (sd.angle + angle_shift) % 360;
			if (sangle < 0)
				sangle += 360;
			startx = sd.dist * Math.Sin(sangle * SharedData.DEG_TO_RAD);
			search_angle = (int) Math.Ceiling(Math.Atan(((double) ewidth) / expected_dist) * SharedData.RAD_TO_DEG * 1.1);
			for (i = 0;i < sdata.Count;i++)
				{
				if (right)
					indx = (start_indx + i) % sdata.Count;
				else
					{
					indx = start_indx - i;
					if (indx < 0)
						indx += sdata.Count;
					}
				sd = (Rplidar.scan_data)sdata[indx];
				sangle = (sd.angle + angle_shift) % 360;
				if (sangle < 0)
					sangle += 360;
				y = sd.dist * Math.Cos(sangle * SharedData.DEG_TO_RAD);
				if (!opening_found)
					{
					if (y >= open_detect)
						{
						opening_found = true;
						if (right)
							left_edge_indx = indx;
						else
							right_edge_indx = indx;
						oangle = sangle;
						oxdist = sd.dist * Math.Sin(sangle * SharedData.DEG_TO_RAD);
						}
					else
						{
						x = sd.dist * Math.Sin(sangle * SharedData.DEG_TO_RAD);
						if (Math.Abs(x - startx) > opening_xdist_limit)
							break;
						}
					}
				else
					{
					if (y <= edge_detect)
						{
						x = sd.dist * Math.Sin(sangle * SharedData.DEG_TO_RAD);
						width = (int) Math.Round(Math.Abs(x - oxdist));
						if (right)
							right_edge_indx = indx;
						else
							left_edge_indx = indx;
						rtn = true;
						break;
						}
					else if (NavCompute.AngularDistance(sangle,oangle) > search_angle)
						break;
					}
				}
			return (rtn);
		}



		public double ActualWidth(NavData.connection connect,opening_param op,ArrayList sdata,int zero_indx,int useable_edge_indx,NavData.edge op_edge,int shift,bool useable_edge_right,ref string line)

		{
			double rtn = connect.exit_width,sangle,x,y,uey,uex,min_x = 100,ydist = 0;
			int i,indx,op_edge_indx = -1;
			Rplidar.scan_data sd = new Rplidar.scan_data();
			const int EDGE_MATCH_SPREAD = 2;
			bool outward_door;

			line += "ActualWidth: " + connect.name + "  " + op.no_edges + "  " + zero_indx + "  " +useable_edge_indx + "  " + op_edge.door_side + "  " + shift + "  " + useable_edge_right + "\r\n";
			if ((op.no_edges == 2) && (op_edge.door_side))
				{
				sd = (Rplidar.scan_data)sdata[useable_edge_indx];
				sangle = (sd.angle + shift) % 360;
				if (sangle < 0)
					sangle += 360;
				uey = sd.dist * Math.Cos(sangle * SharedData.DEG_TO_RAD);
				uex = sd.dist * Math.Sin(sangle * SharedData.DEG_TO_RAD);
				for (i = 0;i < sdata.Count;i++)
					{
					if (!useable_edge_right)
						indx = (zero_indx + i) % sdata.Count;
					else
						{
						indx = zero_indx - i;
						if (indx < 0)
							indx += sdata.Count;
						}
					sd = (Rplidar.scan_data)sdata[indx];
					sangle = (sd.angle + shift) % 360;
					if (sangle < 0)
						sangle += 360;
					y = sd.dist * Math.Cos(sangle * SharedData.DEG_TO_RAD);
					if ((y <= uey + EDGE_MATCH_SPREAD) && (y >= uey - EDGE_MATCH_SPREAD))
						{
						x = sd.dist * Math.Sin(sangle * SharedData.DEG_TO_RAD);
						if (x < min_x)
							{
							min_x = x;
							op_edge_indx = indx;
							ydist = y;
							}
						}
					else if (y < uey - EDGE_MATCH_SPREAD)
						break;
					}
				if (op_edge_indx != -1)
					{
					line += "Opposite edge detected at index " + op_edge_indx + "\r\n";
					if (useable_edge_right)
						outward_door = (connect.hc_edge.ds == NavData.DoorSwing.OUT);
					else
						outward_door = (connect.lc_edge.ds == NavData.DoorSwing.OUT);
					if (outward_door)
						{
						min_x = 100;
						for (i = 0; i < sdata.Count; i++)
							{
							if (useable_edge_right)
								indx = (op_edge_indx + i) % sdata.Count;
							else
								{
								indx = op_edge_indx - i;
								if (indx < 0)
									indx += sdata.Count;
								}
							sd = (Rplidar.scan_data)sdata[indx];
							sangle = (sd.angle + shift) % 360;
							if (sangle < 0)
								sangle += 360;
							y = sd.dist * Math.Cos(sangle * SharedData.DEG_TO_RAD);
							if (y < ydist + connect.exit_width + 1)
								{
								x = sd.dist * Math.Sin(sangle * SharedData.DEG_TO_RAD);
								if (x < min_x)
									{
									min_x = x;
									}
								}
							else
								break;
							}
						if (min_x != 100)
							{
							line += "Min X dist " + min_x.ToString("F2") + "\r\n";
							rtn = Math.Abs(min_x) + Math.Abs(uex);
							}
						else
							line += "Door min X dist not determined.\r\n";
						}
					else
						{
						sd = (Rplidar.scan_data)sdata[op_edge_indx];
						sangle = (sd.angle + shift) % 360;
						if (sangle < 0)
							sangle += 360;
						x = sd.dist * Math.Sin(sangle * SharedData.DEG_TO_RAD);
						rtn = Math.Abs(uex) + Math.Abs(x);
						}
					}
				else
					line += "Opposite edge not found.\r\n";
				}
			else
				line += "Door width per connection data.\r\n";
			if (rtn > connect.exit_width)
				{
				rtn = connect.exit_width;
				line += "Door width constrained to connection data.\r\n";
				}
			return (rtn);
		}



		public bool ExitPosition(NavData.connection connect,ref int turn_angle,ref int adist)

		{
			bool rtn = false;
			bool edge_right = false;
			ArrayList sdata = new ArrayList();
			Rplidar.scan_data sd = new Rplidar.scan_data();
			int search_angle,i,indx = -1,ad,min_ad = 180,start_angle,cangle,top,bottom,shift,rangle,langle,tangle,above_min_count = 0;
			double dx,min_dist,dy,last_distance = -1,theta,td,y;
			string lines = "";
			opening_param op;
			NavData.location expect;
			bool opening_found = false,edge_found = false;
			int approx_width = 0,right_edge_indx = -1,left_edge_indx = -1,turn,zero_indx = -1;
			NavData.edge op_edge;

			Log.LogEntry("ExitPosition: " + connect.name);
			if (Rplidar.CaptureScan(ref sdata,true))
				{
				expect = NavData.GetCurrentLocation();
				lines += "ExitPosition: " + connect.name + "\r\n";
				for (i = 0;i < sdata.Count;i++)
					{
					sd = (Rplidar.scan_data) sdata[i];
					if (sd.angle == 0)
						{
						zero_indx = i;
						break;
						}
					else
						{
						if ((ad = NavCompute.AngularDistance(0,sd.angle)) < min_ad)
							{
							min_ad = ad;
							zero_indx = i;
							}
						}
					}
				lines += "Zero index " + zero_indx + ".\r\n";
				op = DetermineOpeningParam(connect,expect);
				lines += "Opening parameters: max wall dist." + op.max_wall_dist_limit + "  min wall dist. " + op.min_wall_dist_limit + "  opening detect dist " + op.opening_detect_limit + "  right search limit " + op.right_open_search_limit + "  left search limit " + op.left_open_search_limit + "  no edges " + op.no_edges + "\r\n";
				shift = expect.orientation - connect.direction;
				if (shift > 180)
					shift -= 360;
				else if (shift < -180)
					shift += 360;
				sd = (Rplidar.scan_data) sdata[zero_indx];
				cangle = (sd.angle + shift) % 360;
				if (cangle < 0)
					cangle += 360;
				y = (sd.dist * Math.Cos(cangle * SharedData.DEG_TO_RAD));
				if (y >= op.opening_detect_limit)
					{
					opening_found = true;
					if (op.no_edges == 2)
						approx_width = SearchEdges(sdata,zero_indx,shift, connect.exit_width, op.max_wall_dist_limit,op.expected_wall_dist, ref right_edge_indx, ref left_edge_indx);
					}
				else if (op.no_edges == 2)
					{
					if (!SearchOpeningEdge(sdata,zero_indx,shift,connect.exit_width,op.right_open_search_limit,op.opening_detect_limit,op.max_wall_dist_limit,op.expected_wall_dist,true,ref approx_width,ref right_edge_indx,ref left_edge_indx))
						opening_found = SearchOpeningEdge(sdata,zero_indx,shift, connect.exit_width, op.left_open_search_limit,op.opening_detect_limit,op.max_wall_dist_limit,op.expected_wall_dist,false,ref approx_width, ref right_edge_indx, ref left_edge_indx);
					else
						opening_found = true;
					}
				if (opening_found && (op.no_edges == 2))
					lines += "Edges found at indexes " + left_edge_indx + " and " + right_edge_indx + " with approx width of " + approx_width + "\r\n";
				if (opening_found && (approx_width > SharedData.ROBOT_WIDTH + 2))
					{
					rangle = ((Rplidar.scan_data) sdata[right_edge_indx]).angle;
					langle = ((Rplidar.scan_data)sdata[left_edge_indx]).angle;
					tangle = (langle + NavCompute.AngularDistance(langle,rangle)/2) % 360;
					if (tangle < 90)
						turn = -NavCompute.AngularDistance(tangle,0);
					else
						turn = NavCompute.AngularDistance(tangle,0);
					if ((Math.Abs(turn) >= SharedData.MIN_TURN_ANGLE) && (Turn.TurnAngle(turn)))
						{
						lines += "Turned to face exit center: " + -turn + "°\r\n";
						Rplidar.SaveLidarScan(ref sdata, lines);
						sdata.Clear();
						if (Rplidar.CaptureScan(ref sdata, true))
							{
							lines = "ExitPosition: " + connect.name + " after turned to face exit center\r\n";
							min_ad = 180;
							zero_indx = -1;
							for (i = 0; i < sdata.Count; i++)
								{
								sd = (Rplidar.scan_data)sdata[i];
								if (sd.angle == 0)
									{
									zero_indx = i;
									break;
									}
								else
									{
									if ((ad = NavCompute.AngularDistance(0, sd.angle)) < min_ad)
										{
										min_ad = ad;
										zero_indx = i;
										}
									}
								}
							lines += "Zero index " + zero_indx + ".\r\n";
							expect.orientation = (expect.orientation - turn) % 360;
							if (expect.orientation < 0)
								expect.orientation += 360;
							NavData.SetCurrentLocation(expect);
							op = DetermineOpeningParam(connect, expect);
							lines += "Opening parameters: max wall dist. " + op.max_wall_dist_limit + "  min wall dist. " + op.min_wall_dist_limit + "  opening detect dist " + op.opening_detect_limit + "  right search limit " + op.right_open_search_limit + "  left search limit " + op.left_open_search_limit + "  no edges " + op.no_edges + "\r\n";
							}
						}
					else if (turn >= SharedData.MIN_TURN_ANGLE)
						{
						Log.KeyLogEntry("Attempt to face exit center failed.");
						opening_found  = false;
						}
					}
				if (opening_found && ((op.no_edges == 1) || (approx_width > SharedData.ROBOT_WIDTH + 2)))
					{
					if ((connect.lc_edge.ef.type != NavData.FeatureType.NONE) && !connect.lc_edge.door_side)
						edge_right = true;
					if (edge_right)
						{
						lines += "Useable edge to right\r\n";
						op_edge = connect.hc_edge;
						}
					else
						{
						lines += "Useable edge to left\r\n";
						op_edge = connect.lc_edge;
						}
					top = op.max_wall_dist_limit;
					bottom = op.min_wall_dist_limit;
					if (bottom < SharedData.FLIDAR_OFFSET)
						bottom = SharedData.FLIDAR_OFFSET;
					min_dist = op.opening_detect_limit;
					shift = expect.orientation - connect.direction;
					if (shift > 180)
						shift -= 360;
					else if (shift < -180)
						shift += 360;
					start_angle = ((Rplidar.scan_data)sdata[zero_indx]).angle;
					search_angle = (int) Math.Ceiling(Math.Atan(((double) connect.exit_width) / bottom) * SharedData.RAD_TO_DEG);
					if (!edge_right)
						search_angle = 360 - search_angle;
					lines += "Search angle " + search_angle + "°\r\n";
					i = zero_indx;
					indx = -1;
					do
						{
						sd = (Rplidar.scan_data) sdata[i];
						cangle = (sd.angle + shift) % 360;
						if (cangle < 0)
							cangle += 360;
						y = sd.dist * Math.Cos(cangle * SharedData.DEG_TO_RAD);
						if (y < top)
							{
							if ((last_distance - sd.dist) > 24)
								{
								lines += "Sudden distance change detected at index " + i + ".\r\n";
								indx = i;
								edge_found = true;
								break;
								}
							else if (sd.dist < min_dist)
								{
								min_dist = sd.dist;
								indx = i;
								above_min_count = 0;
								}
							else if ((indx != -1) && (sd.dist > min_dist))
								{
								above_min_count  += 1;
								if (above_min_count == 2)
									{
									lines += "Minimum distance detected at index " + indx + ".\r\n";
									edge_found = true;					
									break;
									}
								}
							}
						last_distance = sd.dist;
						if (edge_right)
							i = (i + 1) % sdata.Count;
						else
							{
							i -= 1;
							if (i < 0)
								i = sdata.Count - 1;
							}
						}
					while(NavCompute.AngularDistance(start_angle,sd.angle) < search_angle);
					if (edge_found)
						{
						sd = (Rplidar.scan_data) sdata[indx];
						cangle = (sd.angle + shift) % 360;
						if (cangle < 0)
							cangle += 360;
						dx = sd.dist * Math.Sin(cangle * SharedData.DEG_TO_RAD);
						dy = sd.dist * Math.Cos(cangle * SharedData.DEG_TO_RAD) + SharedData.FLIDAR_OFFSET + SharedData.FRONT_PIVOT_PT_OFFSET;
						double aw = ActualWidth(connect, op, sdata, zero_indx, indx, op_edge,shift, edge_right,ref lines);
						dx = ((aw/2) - Math.Abs(dx));
						theta = Math.Atan(dx/dy) * SharedData.RAD_TO_DEG;
						if (edge_right)
							td = (connect.direction - theta) % 360;
						else
							td = (connect.direction + theta) %360;
						if (td < 0)
							td += 360;
						turn_angle = NavCompute.AngularDistance(expect.orientation,(int) Math.Round(td));
						lines += "Edge data: shifted angle " + cangle + "°  " + "  useable exit width " + aw.ToString("F2") + "  dx " + dx.ToString("F2") + "  dy " + dy.ToString("F2") + "  theta " + theta.ToString("F1") + "°  turn direction " + td.ToString("F1") + "°  turn angle " + turn_angle + "°";
						if (((op.no_edges == 1) && (turn_angle > Math.Atan(((double)connect.exit_width / 2) / dy) * SharedData.RAD_TO_DEG))
							|| ((op.no_edges == 2) && (turn_angle > Math.Atan(((double)connect.exit_width / 4) / dy) * SharedData.RAD_TO_DEG)))
							lines += "\r\nCalculated turn angle of " + turn_angle + "° exceeds reasonable limits for " + op.no_edges + " edges.";
						else
							{
							if (NavCompute.ToRightDirect(expect.orientation, (int)Math.Round(td)))
								turn_angle *= -1;
							dy -= SharedData.FRONT_PIVOT_PT_OFFSET;
							adist = (int) Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));
							lines += "  distance" + adist;
							rtn = true;
							}
						}
					else
						lines += "No edge found.";
					}
				else
					{
					if (!opening_found)
						lines += "No opening found.\r\n";
					if ((approx_width < SharedData.ROBOT_WIDTH + 2) && (op.no_edges > 1))
						lines += "Opening width of " + approx_width + " in. not wide enought.";
					}
				Rplidar.SaveLidarScan(ref sdata, lines);
				}
			else
				Log.LogEntry("ExitPosition: could not capture LIDAR scan.");
			return(rtn);
		}






		}
	}
