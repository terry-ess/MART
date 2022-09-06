using System;
using System.Collections;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;

namespace AutoRobotControl
	{
	public static class LS02CLidar
		{

		public const string PARAM_FILE = "ls02clidar.param";


		public struct scan_data
		{
			public short angle;
			public double dist;

			public override string ToString()

			{
				return(this.angle + "°  " + this.dist + " in");
			}

		};


		public struct rlc_scan_data
		{
			public double x;
			public double y;

			public override string ToString()
			
			{
				return("( " + x.ToString("F2") + "," + y.ToString("F2") + ")");
			}

		};

		
		private static string port_name = "";
		private static SerialPort csp = new SerialPort();
		private static int angle_offset;


		static LS02CLidar()

		{
			string fname;
			TextReader tr;
			double offset;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				port_name = tr.ReadLine();
				tr.Close();
				Open();
				}
			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + SharedData.OFFSET_PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				try
				{
				tr.ReadLine();
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
			csp.ReadTimeout = 100;
			csp.WriteTimeout = 100;
			csp.Handshake = Handshake.None;
			csp.Open();
			Thread.Sleep(1000);
			csp.DiscardOutBuffer();
			csp.ReadExisting();
			SharedData.rear_lidar_operational = true;
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
				csp.Close();
				SharedData.rear_lidar_operational = false;
				}
		}



		public static bool CaptureScan(ref ArrayList sdata)

		{
			int i,val,j,angle,bangle;
			byte[] data = new byte[21];
			int no_angles = 0;
			scan_data rdata = new scan_data();
			bool rtn = false;

			try
			{
			csp.ReadExisting();
			Thread.Sleep(40);
			for (j = 0;j < 29;j++)
				{
				do
					{
					val = csp.ReadByte();
					}
				while (val != 0xFA);
				csp.Read(data,0,21);
				bangle = ((data[0] - 0xA0) * 4) - 43;
				bangle += angle_offset;
				for (i = 0;i < 4;i++)
					{
					angle = bangle + i;
					no_angles += 1;
					rdata.dist =  ((data[(i * 4) + 4] & 0x3F) << 8) + data[(i * 4) + 3];
					rdata.dist = (rdata.dist * SharedData.MM_TO_IN);
					rdata.angle = (short) angle;
					sdata.Add(rdata);
					}
				}
			rtn = true;
			}

			catch(Exception ex)
			{
			rtn = false;
			Log.LogEntry("LS02CLidar.CaptureScan exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}
			
			return(rtn);
		}



		public static bool CaptureRLCScan(ref ArrayList sdata)

		{
			int i,val,j,angle,bangle;
			byte[] data = new byte[21];
			int no_angles = 0;
			scan_data rdata = new scan_data();
			rlc_scan_data rlcdata = new rlc_scan_data();
			bool rtn = false;

			try
			{
			csp.ReadExisting();
			Thread.Sleep(40);
			for (j = 0;j < 29;j++)
				{
				do
					{
					val = csp.ReadByte();
					}
				while (val != 0xFA);
				csp.Read(data,0,21);
				bangle = ((data[0] - 0xA0) * 4) - 43;
				bangle += angle_offset;
				for (i = 0;i < 4;i++)
					{
					angle = bangle + i;
					no_angles += 1;
					rdata.dist =  ((data[(i * 4) + 4] & 0x3F) << 8) + data[(i * 4) + 3];
					if (rdata.dist > 0)
						{
						rdata.dist = (rdata.dist * SharedData.MM_TO_IN);
						rdata.angle = (short) angle;
						rlcdata.y = (float)(rdata.dist * Math.Cos(angle * SharedData.DEG_TO_RAD));
						rlcdata.x = (float)(rdata.dist * Math.Sin(angle * SharedData.DEG_TO_RAD));
						sdata.Add(rlcdata);
						}
					}
				}
			rtn = true;
			}

			catch(Exception ex)
			{
			rtn = false;
			Log.LogEntry("LS02CLidar.CaptureScan exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}
			
			return(rtn);
		}



		public static string SaveRLCScan(ArrayList sdata, params object[] lines)
			
		{
			string fname = "";
			TextWriter dstw = null;
			int i;
			DateTime now = DateTime.Now;

			fname = Log.LogDir() + "Rear LIDAR data set " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
			dstw = File.CreateText(fname);
			if (dstw != null)
				{
				dstw.WriteLine("Rear LIDAR data set");
				dstw.WriteLine(now.ToShortDateString() + " " + now.ToShortTimeString());
				dstw.WriteLine();
				dstw.WriteLine();
				dstw.WriteLine();
				dstw.WriteLine("X,Y");
				foreach (rlc_scan_data sd in sdata)
					{
					dstw.WriteLine(sd.x.ToString("f4") + "," + sd.y.ToString("F4"));
					}
				dstw.WriteLine();
				dstw.WriteLine("Comments:");
				for (i = 0; i < lines.Length; i++)
					{
					try
					{
					dstw.WriteLine((string) lines[i]);
					}

					catch(Exception)
					{
					}

					}
				dstw.Close();
				Log.LogEntry("Saved " + fname);
				}
			return(fname);
		}



		public static bool RearClearence(ref double min_clear,int width)

		{
			bool rtn = false;
			double mclr = 158;
			ArrayList rcal = new ArrayList();
			ArrayList pa = new ArrayList();
			int i;
			double xmax;
			rlc_scan_data rlcdata = new rlc_scan_data();
			const int MAX_ANOMALY_DIST = 13; //ANOMALY PROBLEMS ON REAR FACING RIGHT SIDE @ LESS THEN 13 IN. AWAY
			const int MAX_ANOMALIES = 4;		//< 5 EXPECTED
														//SHOULD ANOMALY HANDLING BE IN SCAN CAPTURE???
			Log.LogEntry("RearClearence");
			if (CaptureRLCScan(ref rcal))
				{
				xmax = ((double) width/2);
				for (i = 0;i < rcal.Count;i++)
					{
					rlcdata = (rlc_scan_data) rcal[i];
					if ((Math.Abs(rlcdata.x) <= xmax))
						{
						if (!((rlcdata.x > 0) && (rlcdata.y < MAX_ANOMALY_DIST)))
							{
							if (rlcdata.y < mclr)
								mclr = rlcdata.y;
							}
						else
							{
							pa.Add(rlcdata.y);
							Log.LogEntry("Rear LIDAR anomaly @ " + rlcdata + " possibly ignored");
							}
						}
					}
				if (pa.Count > MAX_ANOMALIES)
					{
					Log.LogEntry("Possible LIDAR anomalies exceed limit (" + pa.Count + ") so included in clearence.");
					for (i = 0; i < pa.Count; i++)
						{
						if ((double)pa[i] < mclr)
							mclr = (double)pa[i];
						}
					pa.Clear();
					}
				min_clear = mclr;
				rtn = true;
				SaveRLCScan(rcal, "Min clear " + mclr.ToString("F2"));
				}
			else
				Log.LogEntry("RearClearence: could not capture a rear LIDAR scan.");
			return(rtn);
		}



		public static bool RCaptureScan(ref MemoryStream ms)

		{
			int i,val,j;
			byte[] data = new byte[21];
			int no_angles = 0,bangle;
			bool rtn = false;
			BinaryWriter tw;
			short angle;
			double dist;
			float x,y;

			try
			{
			tw = new BinaryWriter(ms);
			csp.ReadExisting();
			Thread.Sleep(40);
			for (j = 0;j < 29;j++)
				{
				do
					{
					val = csp.ReadByte();
					}
				while (val != 0xFA);
				csp.Read(data,0,21);
				bangle = (short) (((data[0] - 0xA0) * 4) - 43);
				bangle += angle_offset;
				for (i = 0;i < 4;i++)
					{
					angle = (short) (bangle + i);
					no_angles += 1;
					dist =  (((data[(i * 4) + 4] & 0x3F) << 8) + data[(i * 4) + 3]) * SharedData.MM_TO_IN;
					if (dist > 0)
						{
						angle += 180;
						y = (float) (dist * Math.Cos(angle * SharedData.DEG_TO_RAD)) - SharedData.RLIDAR_OFFSET;
						x = (float) (dist * Math.Sin(angle * SharedData.DEG_TO_RAD));
						tw.Write(x);
						tw.Write(y);
						}
					}
				}
			rtn = true;
			}

			catch(Exception ex)
			{
			rtn = false;
			Log.LogEntry("LS02CLidar.CaptureScan exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}
			
			return(rtn);
		}

		}
	}
