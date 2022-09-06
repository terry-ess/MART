using System;
using System.Diagnostics;


namespace AutoRobotControl
	{
	class UsbControl
		{
		public const string LIDAR = "USB\\VID_10C4*";
		public const string HEAD_ASSEMBLY = "USB\\VID_2341*";

		//THESE FUNCTIONS REQUIRE DEVCON.EXE BE IN THE SAME DIRECTORY AS THE APPLICATION
		//THESE FUNCTIONS REQUIRE THE APPLICATION TO BE RUN AS ADMINISTRATOR TO ACTUALLY WORK
		//THIS IS A KLUDGE

		public static bool DisablePort(string port)

		{
			bool rtn = false;

			try
			{
			Process.Start("devcon.exe", "disable " + port);
			rtn = true;
			}

			catch(Exception)
			{
			}

			return(rtn);
		}



		public static bool EnablePort(string port)

		{
			bool rtn = false;

			try
			{
			Process.Start("devcon.exe", "enable " + port);
			rtn = true;
			}

			catch(Exception)
			{
			}

			return(rtn);
		}


		}
	}
