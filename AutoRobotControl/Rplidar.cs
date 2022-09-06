using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;


namespace AutoRobotControl
	{

	public static class Rplidar
		{

		public enum EdgeDetectTrigger { NONE, DIST_CHANGE, MIN_DIST,EITHER };

		public const string PARAM_FILE = "rplidar.param";
		public const string ANOMALY_TABLE = "rplidaranomalydistance.table";
		public const int DISCONTINUITY_FACTOR = 5;
		public const int DEAD_ZONE_ANGLE = 44;

		private const int EDGE_DIST_LIMIT = 12;
		private const double MIN_DX_FACTOR = .5;
		private const int DISABLE_SLEEP_MS = 2000;
		private const int RECYCLE_SLEEP_MS = 5000;
		private const int MIN_ACCURACY = 4;
		
		public struct health_status
		{
			public byte status;
			public ushort error_code;
		};

		public struct info
		{
			public byte model;
			public byte firmware_minor;
			public byte firmware_major;
			public byte hardware;
			public string serial_no;
		};

		public struct scan_data
		{
			public ushort angle;
			public double dist;

			public override string ToString()

			{
				return(this.angle + "°  " + this.dist + " in");
			}

		};
		
		public struct rscan_data
		{
			public ushort angle;
			public ushort dist;
		};


		private static double[] adist_table = null;

		private static SerialPort csp = new SerialPort();

		private static byte[] get_health = { 0xa5, 0x52};
		private static byte[] get_info = {0xa5,0x50};
		private static byte[] scan = {0xa5,0x20};
		private static byte[] stop = {0xa5,0x25};
		private static byte[] reset = {0xa5,0x40};

		private static byte[] get_health_rsp = {0xa5,0x5a,0x03,0x00,0x00,0x00,0x06 };
		private static byte[] get_info_rsp = {0xa5,0x5a,0x14,0x00,0x00,0x00,0x04};
		private static byte[] scan_rsp = {0xa5,0x5a,0x05,0x00,0x00,0x40,0x81};

		private static string port_name;
		private static double cal_factor;
		private static string comments = "";
		private static int angle_offset;


		static Rplidar()

		{
			string fname;
			int i;
			TextReader tr;
			double offset;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				port_name = tr.ReadLine();
				
				try
				{
				cal_factor = double.Parse(tr.ReadLine());
				}

				catch(Exception ex)
				{
				cal_factor = 1;
				Log.LogEntry("Could not load LIDAR calibration factor: " + ex.Message );
				}

				tr.Close();
				Open();
				}
			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + ANOMALY_TABLE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				adist_table = new double[360];
				try
				{
				for (i = 0;i < 360;i++)
					adist_table[i] = double.Parse(tr.ReadLine());
				}

				catch(Exception exp)
				{
				adist_table = null;
				Log.LogEntry("Could not load anomaly table: " + exp.Message);
				}

				tr.Close();
				}
			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + SharedData.OFFSET_PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				try
				{
				tr.ReadLine();
				offset = double.Parse(tr.ReadLine());
				angle_offset = (int) Math.Round(offset);
				}

				catch(Exception exp)
				{
				Log.LogEntry("Could not load angle offset: " + exp.Message);
				}

				tr.Close();
				}
		}



		public static bool Open(string port_name)

		{
			bool rtn = false;

			try
			{
			csp.PortName = port_name;
			csp.BaudRate = 115200;
			csp.DataBits = 8;
			csp.StopBits = StopBits.One;
			csp.Parity = Parity.None;
			csp.ReadTimeout = 1000;
			csp.WriteTimeout = 1000;
			csp.Handshake = Handshake.None;
			csp.Open();
			Thread.Sleep(1000);
			csp.DiscardOutBuffer();
			csp.ReadExisting();
			rtn = true;
			}

			catch (Exception ex)
			{
			Log.LogEntry("Rplidar.Open exception: " + ex.Message);
			Log.LogEntry("           stack trace: " + ex.StackTrace);
			SharedData.last_error = "Lidar open exception: " + ex.Message;
			if (csp.IsOpen)
				csp.Close();
			rtn = false;
			}
			
			return(rtn);
		}



		public static bool Open()

		{
			bool rtn = false;

			if (!csp.IsOpen && (port_name.Length > 0))
				rtn = Open(port_name);
			else if (csp.IsOpen)
				rtn = true;
			return(rtn);
		}



		public static void Close()

		{
			if (csp.IsOpen)
				{
				csp.Write(stop, 0, 2);
				csp.Close();
				SharedData.front_lidar_operational = false;
				}
		}



		private static bool Recycle()

		{
			bool rtn = false;
			int i;
			const int MAX_TRIES = 3;

			Log.LogEntry("Recycling LIDAR");
			Speech.SpeakAsync("Recycling LIDAR");
			for (i = 0;i < MAX_TRIES;i++)
				{
				Close();
//				UsbControl.DisablePort(UsbControl.LIDAR);
//				Thread.Sleep(DISABLE_SLEEP_MS);
//				UsbControl.EnablePort(UsbControl.LIDAR);
//				Thread.Sleep(RECYCLE_SLEEP_MS);
				if (Open() && Operational())
					{
					rtn = true;
					break;
					}
				}
			if (rtn == false)
				{
				Log.LogEntry("Recycling LIDAR failed");
				Speech.SpeakAsync("Recycling LIDAR failed");
				}
			return(rtn);
		}



		public static bool Connected()

		{
			return(csp.IsOpen);
		}



		private static bool Matches(byte[] a1,byte[] a2)

		{
			bool rtn = false;
			int i;

			if (a1.Length == a2.Length)
				{
				rtn = true;
				for (i = 0;i < a1.Length;i++)
					if (a1[i] != a2[i])
						{
						rtn = false;
						break;
						}
				}
			return(rtn);
		}



		public static bool HealthStatus(ref health_status hs)

		{
			bool rtn = false;
			byte[] buffer = new byte[7];
			byte[] data = new byte[3];
			int i;

			if (csp.IsOpen)
				{

				try
				{
				csp.Write(get_health, 0, 2);
				for (i = 0;i < buffer.Length;i++)
					buffer[i] = (byte) csp.ReadByte();
				if (Matches(buffer,get_health_rsp))
					{
					for (i = 0;i < data.Length;i++)
						data[i] = (byte) csp.ReadByte();
					hs.status = data[0];
					hs.error_code = (ushort)((data[2] << 8) + data[1]);
					rtn = true;
					}
				}

				catch(Exception ex)
				{
				SharedData.last_error = "Lidar health status exception: " + ex.Message;
				try
				{
				csp.Close();
				}

				catch(Exception)
				{
				}

				SharedData.front_lidar_operational = false;
				}
				
				}
			else
				SharedData.front_lidar_operational = false;
			return (rtn);
		}



		public static bool Info(ref info inf)

		{
			bool rtn = false;
			byte[] buffer = new byte[7];
			byte[] data = new byte[20];
			int i;

			if (csp.IsOpen)
				{

				try
				{
				csp.Write(get_info, 0, 2);
				for (i = 0;i < buffer.Length;i++)
					buffer[i] = (byte) csp.ReadByte();
				if (Matches(buffer,get_info_rsp))
					{
					for (i = 0;i < data.Length;i++)
						data[i] = (byte) csp.ReadByte();
					inf.model = data[0];
					inf.firmware_minor = data[1];
					inf.firmware_major = data[2];
					inf.hardware = data[3];
					inf.serial_no = "";
					for (i = 4;i < data.Length;i++)
						inf.serial_no += data[i].ToString("X");
					rtn =true;
					}
				}

				catch(Exception ex)
				{
				SharedData.last_error = "Lidar info exception: " + ex.Message;
				
				try
				{
				csp.Close();
				}

				catch(Exception)
				{
				}

				SharedData.front_lidar_operational = false;
				}

				}
			else
				SharedData.front_lidar_operational = false;
			return (rtn);
		}



		public static void Reset()

		{
			if (csp.IsOpen)
				{
				csp.Write(reset,0,2);
				Thread.Sleep(10);
				}
		}



		private static bool CaptureScanI(ref ArrayList sdata,bool anomaly_filter)

		{
			bool rtn = false;
			byte[] buffer = new byte[7];
			byte[] data = new byte[5];
			scan_data rdata = new scan_data(),nrdata = new scan_data(),prdata = new scan_data();
			int i,j,last_angle = -1,start_angle = -1,zero_drop = 0,same_angle_drop = 0,out_seq_drop = 0,pout_seq_drop = 0,atable_drop = 0,iangle;

			comments = "";

			try
			{
			sdata.Clear();
			csp.Write(scan, 0, 2);
			for (i = 0;i < buffer.Length;i++)
				buffer[i] = (byte) csp.ReadByte();
			if (Matches(buffer,scan_rsp))
				{
				for (i = 0;i < 360;i++)
					{
					for (j = 0;j < data.Length;j++)
						{
						data[j] = (byte) csp.ReadByte();
						}
					iangle = (((data[2] << 7) + (data[1] >> 1))/64);
					iangle += angle_offset;
					if (iangle < 0)
						iangle += 360;
					else if (iangle > 360)
						iangle -= 360;
					rdata.angle = (ushort) iangle;
					if (i == 0)
						start_angle = rdata.angle;
					else if (i > 20)
						{
						if (rdata.angle == start_angle)
							{
							comments = "Break @ start angle\r\n";
							break;
							}
						else if ((NavCompute.AngularDistance(start_angle,rdata.angle) < 10) && (NavCompute.ToRightDirect(start_angle,rdata.angle)))
							{
							comments = "Break @ " + rdata.angle + "\r\n";
							break;
							}
						}
					if (rdata.angle != last_angle)
						{
						rdata.dist = ((data[4] << 8) + data[3])/4;
						if (rdata.dist > 0)
							{
							rdata.dist = (rdata.dist * SharedData.MM_TO_IN)/cal_factor;
							if (!anomaly_filter || (adist_table == null) || (anomaly_filter && (rdata.dist > adist_table[rdata.angle])))
								{
								sdata.Add(rdata);
								last_angle = rdata.angle;
								}
							else
								{
								atable_drop += 1;
								Log.LogEntry("Anomily table drop: " + rdata.angle + ",  " + rdata.dist);
								}
							}
						else
							zero_drop += 1;
						}
					else
						same_angle_drop += 1;
					}
				}
			csp.Write(stop,0,2);
			Thread.Sleep(100);
			csp.ReadExisting();
			if (sdata.Count > 0)
				{
				for (i = 0;i < sdata.Count;i++)
					{
					rdata = (scan_data) sdata[i];
					if (i > 0)
						prdata = (scan_data) sdata[i - 1];
					else
						prdata = (scan_data) sdata[sdata.Count - 1];
					if (rdata.angle - prdata.angle < 0)
						{
						nrdata = (scan_data) sdata[(i + 1) % sdata.Count];
						if ((nrdata.angle > rdata.angle) && (nrdata.angle < prdata.angle))
							{
							if ((prdata.angle  < 340) && (rdata.angle > 20))
								{
								if (i > 0)
									{
									i -= 1;
									sdata.RemoveAt(i);
									}
								else
									sdata.RemoveAt(sdata.Count - 1);
								Log.LogEntry("Prior out of sequence drop: " + prdata.angle  + ",  " + rdata.angle  + ",  " + nrdata.angle);
								}
							}
						else
							{
							sdata.RemoveAt(i);
							i -= 1;
							Log.LogEntry("Out of sequence drop: " + rdata.angle + ",  " + rdata.dist * SharedData.MM_TO_IN + ",  " + prdata.angle);
							}
						}
					}
				rtn = true;
				comments += "Zero drops: " + zero_drop + "  Same angle drops: " + same_angle_drop + "   Out of sequence drops: " + out_seq_drop + "  Prior out of sequence drops: " + pout_seq_drop + "   Anomaly table drops: " + atable_drop;
				}
			else
				Log.LogEntry("Rplidar.CaptureScan empty data set.");
			}

			catch(Exception ex)
			{
			sdata.Clear();

			try
			{
			csp.Close();
			}

			catch(Exception)
			{
			}

			Log.LogEntry("Rplidar.CaptureScan exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			SharedData.last_error = "Lidar capture scan exception: " + ex.Message;
			SharedData.front_lidar_operational = false;
			rtn = false;
			}

			return(rtn);
		}



		public static bool CaptureScan(ref ArrayList sdata,bool anomaly_filter)

		{
			bool rtn = false;

			if (csp.IsOpen)
				{
				if (!(rtn = CaptureScanI(ref sdata,anomaly_filter)))
					if (Recycle())
						rtn = CaptureScanI(ref sdata,anomaly_filter);
				}
			else if (Recycle())
				rtn = CaptureScanI(ref sdata,anomaly_filter);
			else
				SharedData.front_lidar_operational = false;
			return(rtn);
		}



		private static bool RCaptureScanI(ref MemoryStream ms)

		{
			bool rtn = false;
			byte[] buffer = new byte[7];
			byte[] data = new byte[5];
			ArrayList sdata = new ArrayList();
			rscan_data rdata = new rscan_data(),nrdata = new rscan_data(),prdata = new rscan_data();
			int i,j,last_angle = -1,start_angle = -1,iangle;
			BinaryWriter tw;
			double dist;
			float x,y;

			try
			{
			csp.Write(scan, 0, 2);
			for (i = 0;i < buffer.Length;i++)
				buffer[i] = (byte) csp.ReadByte();
			if (Matches(buffer,scan_rsp))
				{
				for (i = 0;i < 360;i++)
					{
					for (j = 0;j < data.Length;j++)
						{
						data[j] = (byte) csp.ReadByte();
						}
					iangle = (((data[2] << 7) + (data[1] >> 1))/64);
					iangle += angle_offset;
					if (iangle < 0)
						iangle += 360;
					else if (iangle > 360)
						iangle -= 360;
					rdata.angle = (ushort) iangle;
					if (i == 0)
						start_angle = rdata.angle;
					else if (i > 20)
						{
						if (rdata.angle == start_angle)
							{
							break;
							}
						else if ((NavCompute.AngularDistance(start_angle,rdata.angle) < 10) && (NavCompute.ToRightDirect(start_angle,rdata.angle)))
							{
							break;
							}
						}
					if (rdata.angle != last_angle)
						{
						rdata.dist = (ushort) (((data[4] << 8) + data[3])/4);
						if (rdata.dist > 0)
							{
							if ((adist_table == null) || (rdata.dist > (adist_table[rdata.angle] * SharedData.IN_TO_MM)))
								{
								sdata.Add(rdata);
								last_angle = rdata.angle;
								}
							}
						}
					}
				}
			csp.Write(stop,0,2);
			Thread.Sleep(100);
			csp.ReadExisting();
			if (sdata.Count > 0)
				{
				for (i = 0;i < sdata.Count;i++)
					{
					rdata = (rscan_data) sdata[i];
					if (i > 0)
						prdata = (rscan_data) sdata[i - 1];
					else
						prdata = (rscan_data) sdata[sdata.Count - 1];
					if (rdata.angle - prdata.angle < 0)
						{
						nrdata = (rscan_data) sdata[(i + 1) % sdata.Count];
						if ((nrdata.angle > rdata.angle) && (nrdata.angle < prdata.angle))
							{
							if ((prdata.angle  < 340) && (rdata.angle > 20))
								{
								if (i > 0)
									{
									i -= 1;
									sdata.RemoveAt(i);
									}
								else
									sdata.RemoveAt(sdata.Count - 1);
								}
							}
						else
							{
							sdata.RemoveAt(i);
							i -= 1;
							}
						}
					}
				tw = new BinaryWriter(ms);
				for (i = 0;i < sdata.Count;i++)
					{
					rdata = (rscan_data) sdata[i];
					dist = rdata.dist * SharedData.MM_TO_IN;
					y = (float) (dist * Math.Cos(rdata.angle * SharedData.DEG_TO_RAD)) + SharedData.FLIDAR_OFFSET;
					x = (float) (dist * Math.Sin(rdata.angle * SharedData.DEG_TO_RAD));
					tw.Write(x);
					tw.Write(y);
					}
				rtn = true;
				}
			}

			catch(Exception ex)
			{

			try
			{
			csp.Close();
			}

			catch(Exception)
			{
			}

			Log.LogEntry("Rplidar.CaptureScan exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			SharedData.last_error = "Lidar capture scan exception: " + ex.Message;
			SharedData.front_lidar_operational = false;
			rtn = false;
			}

			return(rtn);
		}



		public static bool RCaptureScan(ref MemoryStream ms)

		{
			bool rtn = false;

			if (csp.IsOpen)
				{
				if (!(rtn = RCaptureScanI(ref ms)))
					if (Recycle())
						rtn = RCaptureScanI(ref ms);
				}
			else if (Recycle())
				rtn = RCaptureScanI(ref ms);
			else
				SharedData.front_lidar_operational = false;
			return(rtn);
		}



		public static void SaveParamFile(string port_name)

		{
			string fname;
			TextWriter tw;

			if (csp.IsOpen)
				{
				fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PARAM_FILE;
				tw = File.CreateText(fname);
				if (tw != null)
					{
					tw.WriteLine(port_name);
					tw.Close();
					}
				}
		}



		// a Y axis bi-level filter
		private static bool OpenWallFilter(ref ArrayList org,int direc,int gta,int lta)

		{
			bool rtn = false;
			scan_data sd;
			int i, angle;
			bool calc = false;
			double min = 120,max = 0,y;
			ArrayList filtered = new ArrayList();

			for (i = 0;i < org.Count;i++)
				{
				sd = (scan_data) org[i];
				angle = sd.angle - direc;
				if (angle < 0)
					angle += 360;
				if (gta > lta)
					calc = ((angle >= gta) || (angle <= lta));
				else
					calc = ((angle >= gta) && (angle <= lta));
				if (calc)
					{
					y = (sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD));
					if (y < min)
						min = y;
					else if (y > max)
						max = y;
					}
				}
			for (i = 0;i < org.Count;i++)
				{
				sd = (scan_data) org[i];
				angle = sd.angle - direc;
				if (angle < 0)
					angle += 360;
				if (gta > lta)
					calc = ((angle >= gta) || (angle <= lta));
				else
					calc = ((angle >= gta) && (angle <= lta));
				if (calc)
					{
					y = (sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD));
					if (y < min + (max - min)/2)
						filtered.Add(sd);
					}
				}
			if (filtered.Count > 3)
				{
				rtn = true;
				org.Clear();
				org = filtered;
				}
			return(rtn);
		}



		public static void FindDistAnglePerpToWall(ref ArrayList sdata,ref int pa,ref double nsee,ref int dist,int center_shift_angle,int shift_angle,ref Room.perp_wall pw)

		{
			scan_data sd;
			int i,angle;
			double[] xy;
			ArrayList xydata = new ArrayList();
			double sx2 = 0, sxy = 0, sx = 0, sy = 0, m = 0,b = 0,see = 0,y,x;
			string dir, fname;
			TextWriter tw;
			double zdist = 0,cxdist = 0,mindd = 0,dd;
			int starti = 0,icount = 0,cangle = 180,cindx = 0;
			const int SEARCH_LIMIT = 12;
			const int MIN_DATA_PTS = 6;

			if (sdata.Count > 0)
				{
				dir = Log.LogDir();
				fname = dir + "LIDAR Perp Wall Data Set " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
				tw = File.CreateText(fname);
				tw.WriteLine("LIDAR perp wall data set: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				tw.WriteLine();
				tw.WriteLine("Center Shift angle (°): " + center_shift_angle);
				tw.WriteLine("Shift angle (°): " + shift_angle);
				tw.WriteLine("Raw data set size: " + sdata.Count);
				tw.Flush();
				for(i = 0;i < sdata.Count;i++)
					{
					sd = (scan_data)sdata[i];
					angle = sd.angle - center_shift_angle;
					if (angle < 0)
						angle += 360;
					else if (angle > 360)
						angle -= 360;
					if (angle == 0)
						{
						angle = sd.angle - shift_angle;
						if (angle < 0)
							angle += 360;
						else if (angle > 360)
							angle -= 360;
						cxdist = (sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD));
						cindx = i;
						break;
						}
					else if (NavCompute.AngularDistance(0,angle) < cangle)
						{
						cangle = angle;
						angle = sd.angle - shift_angle;
						if (angle < 0)
							angle += 360;
						else if (angle > 360)
							angle -= 360;
						cxdist = (sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD));
						cindx = i;
						}
					}
				tw.WriteLine("Center search point: index - " + cindx + "   x dist - " + cxdist);
				tw.WriteLine();
				tw.WriteLine("X,Y,Angle (°)");
				tw.Flush();
				for (i = 0;i < sdata.Count;i++)
					{
					sd = (scan_data) sdata[i];
					angle = sd.angle - shift_angle;
					if (angle < 0)
						angle += 360;
					else if (angle > 360)
						angle -= 360;
					y = (sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD));
					if (y > 0)
						{
						x = (sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD));
						if (Math.Abs(x - cxdist) <= SEARCH_LIMIT)
							{
							xy = new double[2];
							xy[0] = x;
							xy[1] = y;
							tw.WriteLine(xy[0] + "," + xy[1] + "," + sd.angle);
							xydata.Add(xy);
							if ((dd = x - cxdist) < mindd)
								{
								mindd = dd;
								starti = i;
								}
							icount += 1;
							}
						}
					}
				if (xydata.Count > MIN_DATA_PTS)
					{
					for (i = 0;i < xydata.Count;i++)
						{
						xy = (double[]) xydata[i];
						sx += xy[0];
						sx2 += xy[0] * xy[0];
						sy += xy[1];
						sxy += xy[0] * xy[1];
						}
					m = ((xydata.Count * sxy) - (sx * sy)) / ((xydata.Count * sx2) - Math.Pow(sx, 2));
					b = (sy/xydata.Count) - ((m * sx)/xydata.Count);
					for (i = 0;i < xydata.Count;i++)
						{
						xy = (double[]) xydata[i];
						x = xy[0];
						y = (x * m ) + b;
						see += Math.Pow(xy[1] - y,2);
						}
					see = Math.Sqrt(see/xydata.Count);
					pa = (int) Math.Round(Math.Atan(m) * SharedData.RAD_TO_DEG);
					zdist = b + (SharedData.FLIDAR_OFFSET * Math.Cos(shift_angle * SharedData.DEG_TO_RAD));
					dist = (int)(Math.Round(zdist * Math.Cos(Math.Atan(m))));
					nsee = see/(sy/xydata.Count);
					pw.start_indx = starti;
					pw.count = icount;
					}
				else
					{
					tw.WriteLine("X-Y data set of " + xydata.Count +  " too small to use.");
					pa = 0;
					dist = 0;
					nsee = double.PositiveInfinity;
					}
				tw.WriteLine();
				tw.WriteLine("number samples - " + xydata.Count + "   slope - " + m.ToString("F3") + "   intercept - " + b.ToString("F3") + "   wall distance @ intercept (in) - " + zdist.ToString("F3"));
				tw.WriteLine("wall perp angle (°) - " + pa + "  wall distance (in) - " + dist + "  start data index - " + starti);
				tw.WriteLine("est standard error - " + see.ToString("F3") + "   Norm est standard error - " + nsee.ToString("F4"));
				tw.Close();
				Log.LogEntry("Saved " + fname);
				}
			else
				{
				Log.LogEntry("RPLIDAR.FindDistAnglePerpToWall called with empty data set.");
				pa = 0;
				dist = 0;
				nsee = double.PositiveInfinity;				 
				}
		}



		// KNOW PERP DIRECTION OF WALL ADJACENT TO EDGE
		public static bool FindEdge(ref ArrayList sdata, int shift_angle, ref int ra, ref int dist, int starti,int scount, int travela, SharedData.RobotLocation edge_direc)

		{
			bool rtn = false;
			int angle,i,starta;
			scan_data sd,psd;
			string dir,fname;
			TextWriter tw;
			double sy = 0,mean = 0,y_var = 0,max_ydev;
			int index,ds_count = 0,traveld;
			double y, x,last_x = 0,last_y = 0,px,delta = 0,last_delta = 0,last_dist = 0,last_xdist = 0;
			bool edge_detected = false,delta_shift = false;
			const int Y_MAX_SIGMA = 6;
			const double MAX_SD = .5;
			const double MIN_MAX_DEV = .75;
			const int MAX_DS_COUNT = 2;

			dir = Log.LogDir();
			fname = dir + "LIDAR Find Edge Data Set " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
			tw = File.CreateText(fname);
			tw.WriteLine("LIDAR find edge data set: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
			tw.WriteLine();
			tw.WriteLine("Perp. shift angle (°): " + shift_angle);
			tw.WriteLine("Edge direction: " + edge_direc);
			tw.WriteLine("Start index: " + starti);
			tw.WriteLine("Sample count: " + scount);
			tw.WriteLine("Max. travel angle (°): " + travela);
			for (i = 0;i < scount;i++)
				{
				sd = (scan_data) sdata[(starti + i) % sdata.Count];
				angle = sd.angle - shift_angle;
				if (angle < 0)
					angle += 360;
				sy += sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD);
				}
			mean = sy/scount;
			for (i = 0; i < scount; i++)
				{
				sd = (scan_data) sdata[(starti + i) % sdata.Count];
				angle = sd.angle - shift_angle;
				if (angle < 0)
					angle += 360;
				y = sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD);
				y_var += Math.Pow((y - mean),2);
				}
			y_var /= (scount - 1);
			if (Math.Sqrt(y_var) < MAX_SD)
				{
				max_ydev = Math.Sqrt(y_var) * Y_MAX_SIGMA;
				if (max_ydev < MIN_MAX_DEV)
					max_ydev = MIN_MAX_DEV;
				tw.WriteLine("Calculated values: Y axis mean - " + mean.ToString("F3") + "   max deviation - " + max_ydev);
				tw.WriteLine();
				tw.WriteLine("X,Y,Angle (°)");
				if (edge_direc == SharedData.RobotLocation.RIGHT)
					{
					sd = (scan_data) sdata[starti];
					starta = sd.angle;
					traveld = (starta + travela) % 360;
					}
				else
					{
					starti = (starti + scount - 1) % sdata.Count;
					sd = (scan_data) sdata[starti];
					starta = sd.angle;
					traveld = starta - travela;
					if (traveld < 0)
						traveld += 360;
					}
				for (i = 0; i < travela; i++)
					{
					if (edge_direc == SharedData.RobotLocation.RIGHT)
						index = (starti + i) % sdata.Count;
					else
						{
						index = starti - i;
						if (index < 0)
							index += sdata.Count;
						}
					sd = (scan_data) sdata[index];
					if (sd.angle == traveld)
						break;
					else if (NavCompute.AngularDistance(starta,(int) sd.angle) > travela)
						break;
					angle = sd.angle - shift_angle;
					if (angle < 0)
						angle += 360;
					y = sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD);
					x = sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD);
					tw.WriteLine(x + "," + y + "," + sd.angle);
					if (i > 0)
						{
						delta = Math.Abs((x - last_x)/(y - last_y));
						if (i > 1)
							{
							if ((last_delta > 1) && (delta < 1))
								{
								delta_shift = true;
								ds_count = 0;
								}
							else if ((last_delta < 1) && (delta > 1))
								{
								delta_shift = false;;
								ds_count = 0;
								}
							else if (delta_shift)
								{
								ds_count += 1;
								if (ds_count > MAX_DS_COUNT)
									{
									delta_shift = false;
									ds_count = 0;
									}
								}
							}
						}
					if ((y - mean) > max_ydev)
						{
						if (Math.Abs(sd.dist - last_dist) > EDGE_DIST_LIMIT)
							edge_detected = true;
						else if (delta_shift)
							edge_detected = true;
						}
					last_xdist = x;
					if (edge_detected)
						{
						tw.WriteLine();
						if (Math.Abs(sd.dist - last_dist) > EDGE_DIST_LIMIT)
							{
							tw.WriteLine("Edge detect trigger: sudden distance change");
							ds_count = 0;
							}
						else
							tw.WriteLine("Edge detect trigger: max Y deviation and delta shift");
						rtn = true;
						if (edge_direc == SharedData.RobotLocation.RIGHT)
							{
							index -= (ds_count + 1);
							if (index < 0)
								index += sdata.Count;
							sd = (scan_data) sdata[index];
							index -= 1;
							if (index < 0)
								index += sdata.Count;
							psd = (scan_data) sdata[index];
							}
						else
							{
							sd = (scan_data) sdata[(index + (ds_count + 1)) % sdata.Count];
							psd = (scan_data) sdata[(index + (ds_count + 2)) % sdata.Count];
							}
						angle = sd.angle - shift_angle;
						if (angle < 0)
							angle += 360;
						x = sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD);
						angle = psd.angle - shift_angle;
						if (angle < 0)
							angle += 360;
						px = psd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD);
						if (Math.Abs((x - px)/(sd.angle - psd.angle)) > MIN_ACCURACY)
							{
							rtn = false;
							tw.WriteLine("Edge found with unusable accuracy of " + Math.Abs((x - px) / (sd.angle - psd.angle)) + " in/°");
							break;
							}
						x = sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD);
						y = sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD);
						y += SharedData.FLIDAR_OFFSET;
						dist = (int)Math.Round(Math.Sqrt((x * x) + (y * y)));
						if (y > 0)
							{
							ra = (int) Math.Round(-Math.Atan(x/y) * SharedData.RAD_TO_DEG);
							}
						else if (y < 0)
							{
							if (x > 0)
								ra = (int)Math.Round(-180 - (Math.Atan(x /y) * SharedData.RAD_TO_DEG));
							else
								ra = (int)Math.Round(180 - (Math.Atan(x /y) * SharedData.RAD_TO_DEG));
							}
						else
							ra = 180;
						tw.WriteLine("Edge found with relative angle - " + ra + "°   distance - " + dist + " in.");
						break;
						}
					last_x = x;
					last_y = y;
					last_delta = delta;
					last_dist = sd.dist;
					}
				}
			else
				{
				tw.WriteLine("Standard deviation exceeds limit, calculated values: Y axis mean - " + mean.ToString("F3") + "   standard deviation - " + Math.Sqrt(y_var));
				}
			tw.Close();
			Log.LogEntry("Saved " + fname);
			return(rtn);
		}



		public static int FindObstacles(int shift_angle,int dist,ArrayList sdata,double side_clear,bool departing,ref ArrayList obs)

		{
			int i,angle,min_dist = (int) Math.Round(6000 * SharedData.MM_TO_IN);
			double x,y,dx,dy;
			Rplidar.scan_data sd;
			double width_band,cwidth_band;

			Log.LogEntry("FindObstacles: " + shift_angle + "  " + dist + "  " + side_clear);
			width_band = ((double) SharedData.ROBOT_WIDTH/2) + side_clear;
			cwidth_band = ((double)SharedData.ROBOT_WIDTH / 2) + SharedData.CONNECTOR_SIDE_CLEAR;
			dx = (SharedData.FRONT_PIVOT_PT_OFFSET + SharedData.FLIDAR_OFFSET) * Math.Sin(shift_angle * SharedData.DEG_TO_RAD);
			dy = ((SharedData.FRONT_PIVOT_PT_OFFSET + SharedData.FLIDAR_OFFSET) * Math.Cos(shift_angle * SharedData.DEG_TO_RAD)) - SharedData.FRONT_PIVOT_PT_OFFSET;
			for (i = 0;i < sdata.Count;i++)
				{
				sd = (Rplidar.scan_data) sdata[i];
				angle = (sd.angle - shift_angle) %360;
				if (angle < 0)
					angle += 360;
				y = sd.dist * Math.Cos(angle * SharedData.DEG_TO_RAD) + dy;
				x = sd.dist * Math.Sin(angle * SharedData.DEG_TO_RAD) - dx;
				if (dist == -1)
					{
					if (departing && (y > 0) && (y < SharedData.CONNECTOR_MAX_DEPTH))
						{
						if (Math.Abs(x) <= cwidth_band)
							{
							obs.Add(sd);
							if (y < min_dist)
								min_dist = (int)Math.Ceiling(y);
							}
						}
					else if ((y > 0) && (Math.Abs(x) <= width_band))
						{
						obs.Add(sd);
						if (y < min_dist)
							min_dist = (int) Math.Ceiling(y);
						}
					}
				else
					{
					if (departing && (y > 0) && (y < SharedData.CONNECTOR_MAX_DEPTH))
						{
						if (Math.Abs(x) <= cwidth_band)
							{
							obs.Add(sd);
							if (y < min_dist)
								min_dist = (int)Math.Ceiling(y);
							}
						}
					else if ((y > 0) && (y <= dist) && (Math.Abs(x) <= width_band))
						{
						obs.Add(sd);
						if (y < min_dist)
							min_dist = (int) Math.Ceiling(y);
						}
					}
				}
			Log.LogEntry("Min obstacle distance: " + min_dist);
			Rplidar.SaveLidarScan(ref sdata, "FindObstacles: " + shift_angle + "  " + dist + "\r\nObstacles: " + obs.Count + "\r\n" + SharedData.ArrayListToString(obs));
			return (min_dist);
		}



		public static bool FrontClear(int clear_dist, int width,ref ArrayList obs,ref int min_dist,ref ArrayList sdata)

		{
			bool rtn = false;
			Rplidar.scan_data sd;
			int i;
			double x,y,xmax,ymin = clear_dist + 1;
			string obs_list = "";
			Point pt;

			if (CaptureScan(ref sdata,true))
				{
				xmax = ((double) width / 2);
				for (i = 0; i < sdata.Count; i++)
					{
					sd = (Rplidar.scan_data)sdata[i];
					if ((sd.angle < 90) || (sd.angle > 270))
						{
						x = sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD);
						if (Math.Abs(x) <= xmax)
							{
							y = sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD);
							if (y < clear_dist)
								{
								pt = new Point((int)Math.Round(x), (int)Math.Round(y));
								obs.Add(pt);
								obs_list += "  " + pt.ToString();
								if (y < ymin)
									ymin = y;
								}
							}
						}
					}
				if (obs.Count == 0)
					rtn = true;
				else
					min_dist = (int) ymin;
				if (SharedData.log_operations)
					SaveLidarScan(ref sdata,"Obstacles detected within " + clear_dist + "in:" + obs_list);
				}
			else
				{
				Log.LogEntry("Could not capture LIDAR scan.");
				}
			return(rtn);
		}



		public static string SaveLidarScan(ref ArrayList sdata, params object[] lines)
			
		{
			string fname = "";
			TextWriter dstw = null;
			int i;

			fname = Log.LogDir() + "RPLIDAR data set " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
			dstw = File.CreateText(fname);
			if (dstw != null)
				{
				dstw.WriteLine("RPLIDAR data set");
				dstw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				dstw.WriteLine();
				dstw.WriteLine();
				dstw.WriteLine();
				dstw.WriteLine("Index,Angle (°), Distance (in)");
				i = 0;
				foreach (Rplidar.scan_data sd in sdata)
					{
					dstw.WriteLine(i + "," + sd.angle + "," + sd.dist.ToString("F4"));
					i += 1;
					}
				dstw.WriteLine();
				dstw.WriteLine("Comments: " + comments);
				for (i = 0; i < lines.Length; i++)
					dstw.WriteLine((string) lines[i]);
				dstw.Close();
				Log.LogEntry("Saved " + fname);
				}
			return(fname);
		}



		public static string SaveLidarScan(ref ArrayList sdata)
			
		{
			return(SaveLidarScan(ref sdata,""));
		}



		public static bool Operational()

		{
			bool rtn = false;
			health_status hs = new health_status();
			ArrayList data = new ArrayList();

			if (Connected() && (HealthStatus(ref hs) && (hs.status == 0)) && (CaptureScanI(ref data,false)))
				{
				SharedData.front_lidar_operational = true;
				rtn = true;
				}
			else
				SharedData.front_lidar_operational = false;
			return(rtn);
		}


		}

	}
