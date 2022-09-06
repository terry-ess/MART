using System;
using System.Net;
using AutoRobotControl;
using Constants;


namespace Sub_system_Operation
	{
	static class HeadAssembly
		{

		private static UiConnection connect = null;


		public static bool Open(UiConnection con)

		{
			bool rtn = false;

			if ( AutoRobotControl.HeadAssembly.Connected())
				{
				connect = con;
				rtn = true;
				}
			return(rtn);
		}



		public static void Close()

		{
			connect = null;
		}



		public static string MessageHandler(string msg, IPEndPoint ep)

		{
			string rsp = "";
			int val;
			string[] values;
			int[] stat = {-1,-1};
			AutoRobotControl.HeadAssembly.HA_STATUS has;

			if (connect != null)
				{
				if (msg == UiConstants.CURRENT_PAN)
					{
					val = AutoRobotControl.HeadAssembly.PanAngle();
					rsp = UiConstants.OK + "," + val;
					}
				else if (msg == UiConstants.CURRENT_TILT)
					{
					val = AutoRobotControl.HeadAssembly.TiltAngle();
					rsp = UiConstants.OK + "," + val;
					}
				else if (msg.StartsWith(UiConstants.SET_PAN_TILT + ","))
					{
					values = msg.Split(',');
					if (values.Length == 3)
						{
						if (AutoRobotControl.HeadAssembly.Pan(int.Parse(values[1]),true))
							if (AutoRobotControl.HeadAssembly.Tilt(int.Parse(values[2]),false))
								rsp = UiConstants.OK + "," + values[1] + "," + values[2];
							else
								rsp = UiConstants.FAIL + ",tilt command exeuction failed";
						else
							rsp = UiConstants.FAIL + ",pan command execution failed";
						}
					else
						rsp = UiConstants.FAIL + ",bad format";
					}
				else if (msg == UiConstants.MAG_HEADING)
					{
					val = AutoRobotControl.HeadAssembly.GetMagneticHeading();
					rsp = UiConstants.OK + "," + val;
					}
				else if (msg == UiConstants.LIGHT_AMP)
					{
					val = AutoRobotControl.HeadAssembly.GetLightAmplitude();
					rsp = UiConstants.OK + "," + val;
					}
				else if (msg == UiConstants.CLEAR_ERR)
					{
					if (AutoRobotControl.HeadAssembly.ClearError())
						rsp = UiConstants.OK;
					else
						rsp = UiConstants.FAIL;
					}
				else if (msg == UiConstants.HA_STAT)
					{
					has = AutoRobotControl.HeadAssembly.GetHeadAssemblyStatus();
					rsp = UiConstants.OK + "," + has.ToString();
					}
				else if (msg == UiConstants.SERVO_STAT)
					{
					stat = AutoRobotControl.HeadAssembly.GetErrorStats();
					if ((stat[0] != -1) && (stat[1] != -1))
						rsp = UiConstants.OK + "," + stat[0] + "," + stat[1];
					else
						rsp = UiConstants.FAIL;
					}
				else if (msg == UiConstants.HA_RESTART)
					{
					if (AutoRobotControl.HeadAssembly.Recycle())
						rsp = UiConstants.OK;
					else
						rsp = UiConstants.FAIL;
					}
				else
					rsp = UiConstants.FAIL + ",unknown command";
				}
			else
				rsp = UiConstants.FAIL + ",not initialized";
			return(rsp);
		}

		}
	}
