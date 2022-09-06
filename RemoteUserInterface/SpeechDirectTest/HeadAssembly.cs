using System;
using System.Threading;
using System.IO.Ports;
using System.Windows.Forms;
using System.IO;

namespace AutoRobotControl
	{
	public static class HeadAssembly
		{
		public enum HA_STATUS { FAIL, CONNECT_NO_SERVOS, CONNECT };

		public const string PARAM_FILE = "ha.param";
		public const string TILT_PARAM_FILE = "head tilt.param";
		public const int MIN_HA_ANGLE = 0;
		public const int MAX_HA_ANGLE = 300;

		private const int PAN_SERVO_ID = 0;
		private const int BASE_PAN_CENTER_PT = 150;
		private const double PAN_MOVE_TIME_RATIO = 25;
		private const int TILT_SERVO_ID = 1;
		private const int TILT_ANGLE_POS_LIMIT = 30;
		private const int TILT_ANGLE_NEG_LIMIT = -30;
		private const int BASE_TILT_CENTER_PT = 150;
		private const double TILT_MOVE_TIME_RATIO = 90.0;
		private const double PT_CONSTANT = 11.2;
		private const int CMD_DELAY = 100;
		private const int SETTLE_DELAY = 300;
		private const int DISABLE_SLEEP_MS = 4000;
		private const int RECYCLE_SLEEP_MS = 20000;

		public static int HA_CENTER_ANGLE;

		private static SerialPort csp = new SerialPort();
		private static int current_pan_angle;
		private static int current_tilt_angle;
		private static string pname;
		private static HA_STATUS has = HA_STATUS.FAIL;
		private static int pan_center_pt = BASE_PAN_CENTER_PT;
		private static int tilt_center_pt = BASE_TILT_CENTER_PT;


		static HeadAssembly()

		{
			string fname;
			TextReader tr;
			double offset;
			string[] values;
			string line;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				pname = tr.ReadLine();
				tr.Close();
				Open(pname);
				}
			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + SharedData.OFFSET_PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				try
				{
				offset = double.Parse(tr.ReadLine());
				pan_center_pt -= (int) Math.Round(offset);
				}

				catch(Exception exp)
				{
				Log.LogEntry("Could not load tilt angle offset: " + exp.Message);
				}

				tr.Close();
				}
			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + TILT_PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);

				try
				{
				while ((line = tr.ReadLine()) != null)
					{
					values = line.Split(',');
					if (values.Length == 2)
						{
						if (values[1] == "0")
							{
							offset = double.Parse(values[2]);
							tilt_center_pt -= (int) Math.Round(offset);
							break;
							}
						}
					}
				}

				catch (Exception exp)
				{
				Log.LogEntry("Could not load pan angle offset: " + exp.Message);
				}

				tr.Close();
				}
			HA_CENTER_ANGLE = pan_center_pt;
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



		public static HA_STATUS Open(string port_name)

		{
			string rsp = "";
			int[] estats;

			try
			{
			csp.PortName = port_name;
			csp.BaudRate = 115200;
			csp.DataBits = 8;
			csp.StopBits = StopBits.One;
			csp.Parity = Parity.None;
			csp.ReadTimeout = 1000;
			csp.Open();
			Thread.Sleep(2000);
			csp.DiscardOutBuffer();
			csp.DiscardInBuffer();
			csp.Write("H\r");
			rsp = csp.ReadLine();
			if (rsp.StartsWith("ok"))
				{
				has = HA_STATUS.CONNECT_NO_SERVOS;
				estats = GetErrorStats();
				if ((estats.Length == 2) && (estats[0] != -1) && (estats[1] != -1))
					{
					if (ClearError() && TorqueOn() && Pan(0,false) && Tilt(0,false))
						{
						Thread.Sleep(1000);
						current_pan_angle = 0;
						current_tilt_angle = 0;
						has = HA_STATUS.CONNECT;
						}
					}
				else
					{
					if (estats.Length == 2)
						Log.LogEntry("HeadAssembly.Open: servos connection failed (pan " + estats[0] + "  tilt " + estats[1] + ")");
					else
						Log.LogEntry("HeadAssembly.Open: servos connection bad response.");
					}
				}
			else
				{
				if (csp.IsOpen)
					csp.Close();
				Log.LogEntry("HeadAssembly.Open: connection attempt failed. (hello response: " + rsp + ")");
				}
			}



			catch (Exception ex)
			{
			Log.LogEntry("HeadAssembly.Open exception: " + ex.Message);
			Log.LogEntry("                stack trace: " + ex.StackTrace); 
			if (csp.IsOpen)
				csp.Close();
			SharedData.last_error = "HeadAssembly open exception: " + ex.Message;
			has = HA_STATUS.FAIL;;
			}
			
			return(has);
		}



		public static HA_STATUS Open()

		{
			if (!csp.IsOpen && (pname.Length > 0))
				Open(pname);
			return(has);
		}



		public static void Close()

		{
			if (csp.IsOpen)
				{
				try
				{
				Tilt(0,true);
				Pan(0,true);
				csp.Close();
				}

				catch(Exception)
				{
				}

				SharedData.head_assembly_operational = false;
				}
		}



		public static bool Connected()

		{
			return(csp.IsOpen);
		}



		public static bool Recycle()

		{
			bool rtn = false;

			Log.LogEntry("Recycling head assembly");
			Speech.SpeakAsync("Recycling head assembly");
			Close();
//			UsbControl.DisablePort(UsbControl.HEAD_ASSEMBLY);
//			Thread.Sleep(DISABLE_SLEEP_MS);
//			UsbControl.EnablePort(UsbControl.HEAD_ASSEMBLY);
			Thread.Sleep(RECYCLE_SLEEP_MS);
			if ((Open() != HA_STATUS.FAIL) && Operational())
				rtn = true;
			else
				{
				Close();
				Log.LogEntry("Recycling head assembly failed");
				Speech.SpeakAsync("Recycling head assembly failed");
				}
			return(rtn);
		}



		private static bool PositionI(int pos,int id,int mt)

		{
			bool rtn = false;
			string rsp;
			int svalue;
			
			if (csp.IsOpen)
				{
				try
				{
				svalue = (int) Math.Round(51 + ((double) pos * 3.07));
				csp.DiscardInBuffer();
				csp.Write(svalue.ToString() + "," + id.ToString() + "," + mt.ToString() + "\r");
				rsp = csp.ReadLine();
				if (rsp.Contains("ok"))
					{
					if (id == PAN_SERVO_ID)
						Log.LogEntry("Head pan set to " + pos);
					else
						Log.LogEntry("Head tilt set to " + pos);
					rtn = true;
					}
				else
					Log.LogEntry(rsp);
				}

				catch(Exception ex)
				{
				Log.LogEntry("Servo position exception: " + ex.Message);
				Log.LogEntry("               stack trace: " + ex.StackTrace);
				rtn = false;
				SharedData.last_error = "HeadAssembly servo position exception: " + ex.Message;
				csp.Close();
				SharedData.head_assembly_operational = false;
				}

				}
			else
				{
				Log.LogEntry("Servo port not open");
				SharedData.last_error = "HeadAssembly servo port not open";
				}
			return(rtn);
		}



		public static bool Position(int pos, int id, int mt)

		{
			bool rtn = false;

			if (csp.IsOpen)
				{
				if (!(rtn = PositionI(pos,id,mt)))
					if (Recycle())
						rtn = PositionI(pos, id, mt);
				}
			else if (Recycle())
				rtn = PositionI(pos, id, mt);
			else
				SharedData.head_assembly_operational = false;
			return(rtn);
		}



		// positive angle is right
		public static bool Pan(int angle,bool wait)

		{
			bool rtn = false;
			int mt,mangle;

			mangle = pan_center_pt - angle;
			if ((mangle >= MIN_HA_ANGLE) && (mangle <= MAX_HA_ANGLE))
				{
				mt = (int) Math.Round((Math.Abs(angle - current_pan_angle) * PAN_MOVE_TIME_RATIO)/PT_CONSTANT);
				if (mt < 4)
					mt = 4;
				else if (mt > 255)
					mt = 255;
				rtn = Position(mangle,PAN_SERVO_ID,mt);
				if (rtn)
					{
					current_pan_angle = angle;
					if (wait)
						Thread.Sleep((int) ((mt * PT_CONSTANT) + CMD_DELAY + SETTLE_DELAY));
					}
				}
			else
				Log.LogEntry("Pan of angle " + angle + " is out of range.");
			return(rtn);
		}



		public static bool DirectInPanLimits(int direct)

		{
			bool rtn = false;
			int pangle;

			if (direct < 180)
				pangle = direct;
			else
				pangle = direct - 360;
			pangle = pan_center_pt - pangle;
			if ((pangle >= MIN_HA_ANGLE) && (pangle <= MAX_HA_ANGLE))
				rtn = true;
			return (rtn);
		}



		public static int PanAngle()

		{
			return(current_pan_angle);
		}



		public static string SendHeadAngle(int angle,bool wait)

		{
			string rsp;
			int pangle;

			if ((angle >= MIN_HA_ANGLE) && (angle < MAX_HA_ANGLE))
				{
				pangle = angle - pan_center_pt;
				if (Pan(pangle,wait))
					rsp = "ok";
				else
					rsp = "fail";
				}
			else
				rsp = "fail bad angle";
			return (rsp);
		}



		public static int CurrentHeadAngle()

		{
			return(current_pan_angle + pan_center_pt);
		}



/*		public static bool SetHeadHeading(int dheading)

		{
			bool rtn = false;
			int cheading;
			bool cw;
			int tangle,rcheading = -1,rdheading = -1;

			cheading = (int) GetMagneticHeading();
			rcheading = NavCompute.DetermineDirection(cheading);
			rdheading = NavCompute.DetermineDirection(dheading);
			if ((rcheading > -1) && (rdheading > -1) && (((int) Math.Abs(rcheading - rdheading)) > 0))
				{
				if (rdheading < rcheading)
					{
					cw = false;
					tangle = (int) (rcheading - rdheading);
					}
				else
					{
					cw = true;
					tangle = (int) (rdheading - rcheading);
					}
				if (tangle > 180)
					{
					tangle = 360 - tangle;
					if (cw)
						cw = false;
					else
						cw = true;
					}
				if (!cw)
					tangle = current_pan_angle - tangle;
				else
					tangle = current_pan_angle + tangle;
				rtn = Pan(tangle,true);
				}
			else if (((int)Math.Abs(rcheading - rdheading)) >= 0)
				rtn = true;
			return(rtn);
		} */


		public static bool TurnHeadToDirection(int current_dir,int desired_direc)

		{
			bool rtn = false;
			int tangle = 0;

			tangle = current_dir - desired_direc;
			if (tangle > 180)
				tangle -= 360;
			else if (tangle < -180)
				tangle += 360;
			rtn = Pan(-tangle,true);
			return(rtn);
		}



		// positive angle is up
		public static bool Tilt(int angle,bool wait)

		{
			bool rtn = false;
			int mt;

			if ((angle >= TILT_ANGLE_NEG_LIMIT) && (angle <= TILT_ANGLE_POS_LIMIT))
				{
				mt = (int) ((Math.Abs(angle - current_tilt_angle) * TILT_MOVE_TIME_RATIO)/PT_CONSTANT);
				if (mt < 40)
					mt = 40;
				else if (mt > 255)
					mt = 255;
				rtn = Position(tilt_center_pt - angle,TILT_SERVO_ID,mt);
				if (rtn)
					{
					current_tilt_angle = angle;
					if (wait)
						Thread.Sleep((int) (mt * PT_CONSTANT));
					}
				}
			else
				Log.LogEntry("Tilt of " + angle + " is out of range.");
			return(rtn);
		}



		public static int TiltAngle()

		{
			return(current_tilt_angle);
		}



		public static bool TorqueOn()

		{
			bool rtn = false;
			string rsp;

			if (csp.IsOpen)
				{
				try
				{
				csp.DiscardInBuffer();
				csp.Write("T," + PAN_SERVO_ID.ToString() + "\r");
				rsp = csp.ReadLine();
				if (rsp.Contains("ok"))
					{
					csp.Write("T," + TILT_SERVO_ID.ToString() + "\r");
					rsp = csp.ReadLine();
					if (rsp.Contains("ok"))
						rtn = true;
					}
				}

				catch(Exception ex)
				{
				SharedData.last_error = "HeadAssembly torque on exception: " + ex.Message;
				csp.Close();
				SharedData.head_assembly_operational = false;
				}

				}
			return(rtn);
		}



		public static bool BreakOn()

		{
			bool rtn = false;
			string rsp;

			if (csp.IsOpen)
				{
				try
				{
				csp.DiscardInBuffer();
				csp.Write("B," + PAN_SERVO_ID.ToString() + "\r");
				rsp = csp.ReadLine();
				if (rsp.Contains("ok"))
					{
					csp.Write("B," + TILT_SERVO_ID.ToString() + "\r");
					rsp = csp.ReadLine();
					if (rsp.Contains("ok"))
						rtn = true;
					}
				}

				catch(Exception ex)
				{
				SharedData.last_error = "HeadAssemly break on exception: " + ex.Message;
				}

				}
			return(rtn);
		}



		public static bool ClearError()

		{
			bool rtn = false;
			string rsp;
			
			if (csp.IsOpen)
				{
				try
				{
				csp.DiscardInBuffer();
				csp.Write("C," + PAN_SERVO_ID.ToString() + "\r");
				rsp = csp.ReadLine();
				if (rsp.Contains("ok"))
					{
					csp.Write("C," + TILT_SERVO_ID.ToString() + "\r");
					rsp = csp.ReadLine();
					if (rsp.Contains("ok"))
						rtn = true;
					}
				}

				catch(Exception ex)
				{
				SharedData.last_error = "HeadAssembly clear error exception: " + ex.Message;
				csp.Close();
				SharedData.head_assembly_operational = false;
				}

				}
			return(rtn);
		}



		public static HA_STATUS GetHeadAssemblyStatus()

		{
			return(has);
		}



		public static int[] GetErrorStats()

		{
			int[] rtn = {-1,-1};
			string rsp;
			string[] values;

			if (csp.IsOpen)
				{
				csp.DiscardInBuffer();

				try
				{
				csp.Write("S," + PAN_SERVO_ID.ToString() + "\r");
				rsp = csp.ReadLine();
				if (rsp.Contains("ok"))
					{
					values = rsp.Split(' ');
					if (values.Length == 3)
						rtn[0] = int.Parse(values[1]);
					else
						Log.LogEntry("GetErrorStats: pan reply = " + rsp);
					}
				else
					Log.LogEntry("GetErrorStats: pan reply = " + rsp);
				csp.DiscardInBuffer();
				csp.Write("S," + TILT_SERVO_ID.ToString() + "\r");
				rsp = csp.ReadLine();
				if (rsp.Contains("ok"))
					{
					values = rsp.Split(' ');
					if (values.Length == 3)
						rtn[1] = int.Parse(values[1]);
					else
						Log.LogEntry("GetErrorStats: tilt reply = " + rsp);
					}
				else
					Log.LogEntry("GetErrorStats: tilt reply = " + rsp);
				}

				catch(Exception ex)
				{
				Log.LogEntry("GetErrorStats exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				}

			return (rtn);
		}



		private static int GetMagneticHeadingI()

		{
			int rtn = -1;
			string rsp;

			if (csp.IsOpen)
				{

				try
				{
				csp.DiscardInBuffer();
				csp.Write("R\r");
				rsp = csp.ReadLine();
				if (rsp.Contains("fail"))
					rtn = -1;
				else
					{
					rtn = int.Parse(rsp.Substring(3));
					}
				}

				catch(Exception ex)
				{
				SharedData.last_error = "HeadAssembly get magnetic heading exception: " + ex.Message;
				csp.Close();
				SharedData.head_assembly_operational = false;
				rtn = -1;
				}

				}

			return (rtn);
		}



		public static int GetMagneticHeading()

		{
			int rtn = -1;

			if (csp.IsOpen)
				{
				if ((rtn = GetMagneticHeadingI()) == -1)
					if (Recycle())
						rtn = GetMagneticHeadingI();
				}
			else if (Recycle())
				rtn = GetMagneticHeadingI();
			else
				SharedData.head_assembly_operational = false;
			return(rtn);
		}



		private static int GetLightAmplitudeI()
		
		{
			int rtn = -1;
			string rsp;

			if (csp.IsOpen)
				{
				csp.DiscardInBuffer();
				csp.Write("L\r");

				try
				{
				rsp = csp.ReadLine();
				if (rsp.Contains("fail"))
					rtn = -1;
				else
					{
					rtn = int.Parse(rsp.Substring(3));
					}
				}

				catch(Exception ex)
				{
				SharedData.last_error = "HeadAssembly get light amplitude exception: " + ex.Message;
				csp.Close();
				SharedData.head_assembly_operational = false;
				rtn = -1;
				}

				}
			return (rtn);
		}	



		public static int GetLightAmplitude()

		{
			int rtn = -1;

			if (csp.IsOpen)
				{
				if ((rtn = GetLightAmplitudeI()) == -1)
					if (Recycle())
						rtn = GetLightAmplitudeI();
				}
			else if (Recycle())
				rtn = GetLightAmplitudeI();
			else
				SharedData.head_assembly_operational = false;
			return(rtn);
		}



		public static bool Operational()

		{
			bool rtn = false;

			if (Connected() && (GetMagneticHeadingI() != -1) && (GetLightAmplitudeI() != -1))
				{
				SharedData.head_assembly_operational = true;
				rtn = true;
				}
			else
				SharedData.head_assembly_operational = false;
			return(rtn);
		}


		}
	}
