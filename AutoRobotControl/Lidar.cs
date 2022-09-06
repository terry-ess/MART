using System;
using System.Collections;
using System.IO;
using System.Threading;

namespace AutoRobotControl
	{

	public static class Lidar
		{

		public enum lidar_status {NONE,FRONT,REAR,BOTH };

		public struct rcscan_data
		{
			public double x;
			public double y;
		};

		public struct rscan_data
		{
			public float x;
			public float y;
		};

		private static lidar_status stat = lidar_status.NONE;
		private static Thread rpl,lsl;
		private static MemoryStream rpl_ms,lsl_ms;
		private static CountdownEvent evnt = new CountdownEvent(2);
		private static ArrayList fl,rl;


		public static lidar_status Open()

		{
			if (Rplidar.Open())
				if (LS02CLidar.Open())
					stat = lidar_status.BOTH;
				else
					stat = lidar_status.FRONT;
			else if (LS02CLidar.Open())
				stat = lidar_status.REAR;
			return(stat);
		}


		public static void Close()

		{
			if (SharedData.front_lidar_operational)
				Rplidar.Close();
			else if (SharedData.rear_lidar_operational)
				LS02CLidar.Close();
			stat = lidar_status.NONE;
		}



		public static lidar_status Status()

		{
			return(stat);
		}



		public static bool CaptureRCScan(ref ArrayList sdata)

		{
			bool rtn = false;
			int i;
			Rplidar.scan_data sd;
			LS02CLidar.scan_data lsd;
			rcscan_data rcsd;

			if (stat != lidar_status.NONE)
				{
				if (stat == lidar_status.FRONT)
					{
					fl = new ArrayList();
					Rplidar.CaptureScan(ref fl,true);
					for (i = 0;i < fl.Count;i++)
						{
						sd = (Rplidar.scan_data) fl[i];
						rcsd.y = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD)) + SharedData.FLIDAR_OFFSET;
						rcsd.x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
						sdata.Add(rcsd);
						}
					rtn = true;
					}
				else if (stat == lidar_status.REAR)
					{
					rl = new ArrayList();
					LS02CLidar.CaptureScan(ref rl);
					for (i = 0; i < fl.Count; i++)
						{
						lsd = (LS02CLidar.scan_data) fl[i];
						rcsd.y = (lsd.dist * Math.Cos((lsd.angle + 180) * SharedData.DEG_TO_RAD)) - SharedData.RLIDAR_OFFSET;
						rcsd.x = (lsd.dist * Math.Sin((lsd.angle + 180) * SharedData.DEG_TO_RAD));
						sdata.Add(rcsd);
						}
					rtn = true;
					}
				else if (stat == lidar_status.BOTH)
					{
					evnt.Signal();
					fl = new ArrayList();
					rpl = new Thread(Rpl_RCCaptureScan);
					rpl.Start();
					rl = new ArrayList();
					LS02CLidar.CaptureScan(ref rl);
					for (i = 0; i < fl.Count; i++)
						{
						lsd = (LS02CLidar.scan_data) fl[i];
						rcsd.y = (lsd.dist * Math.Cos((lsd.angle + 180) * SharedData.DEG_TO_RAD)) - SharedData.RLIDAR_OFFSET;
						rcsd.x = (lsd.dist * Math.Sin((lsd.angle + 180) * SharedData.DEG_TO_RAD));
						sdata.Add(rcsd);
						}
					evnt.Wait();
					evnt.Reset();
					for (i = 0; i < fl.Count; i++)
						{
						sd = (Rplidar.scan_data) fl[i];
						rcsd.y = (sd.dist * Math.Cos(sd.angle * SharedData.DEG_TO_RAD)) + SharedData.FLIDAR_OFFSET;
						rcsd.x = (sd.dist * Math.Sin(sd.angle * SharedData.DEG_TO_RAD));
						sdata.Add(rcsd);
						}
					rtn = true;
					}
				}
			return(rtn);
		}



		private static void Rpl_RCCaptureScan()

		{
			Rplidar.CaptureScan(ref fl,true);
			evnt.Signal();
		}



		public static string SaveLidarRCScan(ref ArrayList sdata, params object[] lines)
			
		{
			string fname = "";
			TextWriter dstw = null;
			int i;
			DateTime now = DateTime.Now;

			fname = Log.LogDir() + "LIDAR data set " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + SharedData.LOG_FILE_EXT;
			dstw = File.CreateText(fname);
			if (dstw != null)
				{
				dstw.WriteLine("LIDAR data set");
				dstw.WriteLine(now.ToShortDateString() + " " + now.ToShortTimeString());
				dstw.WriteLine();
				dstw.WriteLine();
				dstw.WriteLine();
				dstw.WriteLine("X,Y");
				i = 0;
				foreach (Lidar.rcscan_data sd in sdata)
					{
					dstw.WriteLine(sd.x + "," + sd.y.ToString("F4"));
					i += 1;
					}
				dstw.WriteLine();
				dstw.WriteLine("Comments:");
				for (i = 0; i < lines.Length; i++)
					dstw.WriteLine((string) lines[i]);
				dstw.Close();
				Log.LogEntry("Saved " + fname);
				}
			return(fname);
		}



		public static bool RCaptureScan(ref MemoryStream ms)

		{
			bool rtn = false;

			if (stat != lidar_status.NONE)
				{
				if (stat == lidar_status.BOTH)
					{
					rpl_ms = new MemoryStream();
					lsl_ms = new MemoryStream();
					rpl = new Thread(Rpl_RCaptureScan);
					rpl.Start();
					lsl = new Thread(Lsl_RCaptureScan);
					lsl.Start();
					}
				else if (stat == lidar_status.FRONT)
					{
					rpl_ms = ms;
					rpl = new Thread(Rpl_RCaptureScan);
					rpl.Start();
					evnt.Signal();
					}
				else if (stat == lidar_status.REAR)
					{
					lsl_ms = ms;
					lsl = new Thread(Lsl_RCaptureScan);
					evnt.Signal();
					}
				evnt.Wait();
				evnt.Reset();
				if (stat == lidar_status.BOTH)
					{
					if ((rpl_ms.Length == 0) && (lsl_ms.Length == 0))
						rtn = false;
					else if ((rpl_ms.Length == 0) || (lsl_ms.Length == 0))
						{
						if (rpl_ms.Length > 0)
							{
							rpl_ms.Seek(0,SeekOrigin.Begin);
							rpl_ms.CopyTo(ms);
							}
						else
							{
							lsl_ms.Seek(0,SeekOrigin.Begin);
							lsl_ms.CopyTo(ms);
							}
						rtn = true;
						}
					else
						{
						rpl_ms.Seek(0,SeekOrigin.Begin);
						rpl_ms.CopyTo(ms);
						lsl_ms.Seek(0,SeekOrigin.Begin);
						lsl_ms.CopyTo(ms);
						Log.LogEntry("front LIDAR data length: " + rpl_ms.Length + "   rear LIDAR data length: " + lsl_ms.Length + "   transmitted length: " + ms.Length);
						rtn = true;
						}
					}
				else
					{
					if (ms.Length > 0)
						rtn = true;
					}
				}
			return(rtn);
		}



		private static void Rpl_RCaptureScan()

		{
			Rplidar.RCaptureScan(ref rpl_ms);
			evnt.Signal();
		}



		private static void Lsl_RCaptureScan()

		{
			LS02CLidar.RCaptureScan(ref lsl_ms);
			evnt.Signal();
		}

		}
	}
