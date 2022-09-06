using System;
using System.Net;
using AutoRobotControl;
using Constants;

namespace Sub_system_Operation
	{
	class ArmOp
		{

		private static UiConnection connect = null;


		public static bool Open(UiConnection con)

		{
			bool rtn = false;

			if (Arm.OpenServoController())
				{
				connect = con;
				rtn = true;
				}
			return(rtn);
		}



		public static void Close()

		{
			if (connect != null)
				{
				connect = null;
				Arm.CloseServoController();
				}
		}



		public static string MessageHandler(string msg, IPEndPoint ep)

		{
			string rsp = "";
			string[] val;

			if (connect != null)
				{
				if (msg == UiConstants.ARM_STAT)
					{
					if (AutoRobotControl.SharedData.arm_operational)
						rsp = UiConstants.OK;
					else
						rsp = UiConstants.FAIL;
					}
				else if (msg == UiConstants.ARM_TO_START)
					{
					AutoRobotControl.Arm.StartPos(90,-135);
					rsp = UiConstants.OK;
					}
				else if (msg == UiConstants.ARM_TO_PARK)
					{
					AutoRobotControl.Arm.EPark();
					rsp = UiConstants.OK;
					}
				else if (msg.StartsWith(UiConstants.RAW_ARM_TO_POSITION))
					{
					val = msg.Split(',');
					if (val.Length == 7)
						{
						string err = "";

						try
						{
						if (AutoRobotControl.Arm.RawPositionArm(int.Parse(val[1]),int.Parse(val[2]),int.Parse(val[3]),bool.Parse(val[4]),bool.Parse(val[5]),bool.Parse(val[6]),ref err))
							rsp = UiConstants.OK;
						else
							rsp = UiConstants.FAIL + "," + err;
						}

						catch(Exception ex)
						{
						rsp = UiConstants.FAIL + "," + ex.Message;
						}

						}
					else
						rsp = UiConstants.FAIL + ",wrong number parameters";
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
