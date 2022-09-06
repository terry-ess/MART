using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;


namespace AutoRobotControl
	{
	static class Relays
		{

		public const string PARAM_FILE = "relays.param";

		private const string RELAYS_OPENED = "OFF";
		private const string RELAYS_CLOSED = "ON";

		private static SerialPort csp = new SerialPort();
		private static string port_name = "";


		static Relays()

		{
			string fname;
			TextReader tr;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				port_name = tr.ReadLine();
				tr.Close();
				}
		}



		public static bool Open()

		{
			bool rtn = false;
			string rsp;

			if (port_name.Length > 0)
				{
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
					rtn = true;
					}

				}

				catch (Exception ex)
				{
				Log.LogEntry("Relays.Open exception: " + ex.Message);
				Log.LogEntry("stack trace: " + ex.StackTrace);
				if (csp.IsOpen)
					csp.Close();
				}
				}
			else
				Log.LogEntry("No port name avaialble.");
			return (rtn);
		}



		public static void Close()

		{
			if (csp.IsOpen)
				csp.Close();
		}



		public static bool Relay(bool open)

		{
			bool rtn = false;
			string rsp;

			csp.DiscardInBuffer();
			if (open)
				csp.Write(RELAYS_OPENED + "\r");
			else
				csp.Write(RELAYS_CLOSED + "\r");
			rsp = csp.ReadLine();
			if (rsp.StartsWith("ok"))
				rtn = true;
			return(rtn);
		}



		public static bool SSRelay(bool open)

		{
			bool rtn = false;
			string rsp;

			csp.DiscardInBuffer();
			if (open)
				csp.Write(RELAYS_OPENED + ",SS\r");
			else
				csp.Write(RELAYS_CLOSED + ",SS\r");
			rsp = csp.ReadLine();
			if (rsp.StartsWith("ok"))
				rtn = true;
			return(rtn);
		}



		public static bool RCRelay(bool open)

		{
			bool rtn = false;
			string rsp;

			csp.DiscardInBuffer();
			if (open)
				csp.Write(RELAYS_OPENED + ",RC\r");
			else
				csp.Write(RELAYS_CLOSED + ",RC\r");
			rsp = csp.ReadLine();
			if (rsp.StartsWith("ok"))
				rtn = true;
			return(rtn);
		}



		public static bool HARelay(bool open)

		{
			bool rtn = false;
			string rsp;

			csp.DiscardInBuffer();
			if (open)
				csp.Write(RELAYS_OPENED + ",HA\r");
			else
				csp.Write(RELAYS_CLOSED + ",HA\r");
			rsp = csp.ReadLine();
			if (rsp.StartsWith("ok"))
				rtn = true;
			return(rtn);
		}



		public static bool ASRelay(bool open)

		{
			bool rtn = false;
			string rsp;

			csp.DiscardInBuffer();
			if (open)
				csp.Write(RELAYS_OPENED + ",AS\r");
			else
				csp.Write(RELAYS_CLOSED + ",AS\r");
			rsp = csp.ReadLine();
			if (rsp.StartsWith("ok"))
				rtn = true;
			else
				Log.LogEntry("ASRelay fail: " + rsp);
			return(rtn);
		}

		}
	}
