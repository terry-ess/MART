using System;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using AutoRobotControl;

namespace MotionControl
	{
	public static class CtoCCom
		{

		public const string PARAM_FILE = "ctoc.param";

		private static UdpClient udp = null;
		private static IPEndPoint server, sserver;
		private static string last_error = "";
		private static string rcip, mcip;
		private static int rcport, mcport;


		static CtoCCom()

		{
			string fname,line;
			TextReader tr;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				rcip = tr.ReadLine();
				line = tr.ReadLine();
				rcport = int.Parse(line);
				mcip = tr.ReadLine();
				line = tr.ReadLine();
				mcport = int.Parse(line);
				tr.Close();
				Open(rcip,rcport,mcip,mcport);
				}
		}



		public static bool Open(string rcip,int rcport,string mcip,int mcport)

		{
			bool rtn = false;

			udp = new UdpClient(rcip,rcport,mcip,mcport);
			if (udp.Connected())
				{
				server = udp.Server();
				sserver = udp.SServer();
				rtn = true;
				}
			else
				{
				Log.LogEntry("CtoCCom open failed: " + udp.LastError());
				last_error = "CtoCCom open failed: " + udp.LastError();
				}
			return(rtn);
		}



		public static bool Open()

		{
			bool rtn = false;

			if (udp == null)
				rtn = Open(rcip,rcport,mcip,mcport);
			else
				rtn = true;
			return(rtn);
		}



		public static void Close()

		{
			if ((udp != null) && (udp.Connected()))
				{
				udp.Close();
				udp = null;
				SharedData.motion_controller_operational = false;
				}
		}



		public static bool Connected()

		{
			bool rtn = false;

			if ((udp != null) && (udp.Connected()))
				rtn = true;
			return(rtn);
		}



		public static bool SendStopCommand(string command)

		{
			byte[] cmd;
			byte[] rsp = new byte [UdpClient.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();
			bool rtn;

			cmd = encode.GetBytes(command);
			rtn = udp.Send(cmd.Length,cmd,sserver);
			if (rtn == false)
				last_error = udp.LastError();
			return(rtn);
		}



		public static string SendCommand(string command,int timeout_count)

		{
			string rtn = "";
			byte[] cmd;
			byte[] rsp = new byte [UdpClient.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();
			int len = 0;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any,0);
			int count = 0;

			if (timeout_count < 20)
				timeout_count = 20;
			cmd = encode.GetBytes(command);
			udp.ClearReceive();
			if (udp.Send(cmd.Length,cmd,server))
				{
				do
					{
					len = udp.Receive(rsp.Length,rsp,ref ep);
					if (len > 0)
						rtn = encode.GetString(rsp,0,len);
					else
						{
						count += 1;
						if (count < timeout_count)
							Thread.Sleep(10);
						}
					}
				while ((len == 0) && (count < timeout_count));
				if (count == timeout_count)
					{
					rtn = "fail " + SharedData.UDP_TIMEOUT;
					last_error = SharedData.UDP_TIMEOUT;
					}
				}
			else
				{
				rtn = "fail UDP send failure: " + udp.LastError();
				last_error = udp.LastError();
				}
			return(rtn);
		}



		public static string SendCommand(string command)

		{
			string rtn = "";
			byte[] cmd;
			byte[] rsp = new byte [UdpClient.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();
			int len = 0;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any,0);

			cmd = encode.GetBytes(command);
			udp.ClearReceive();
			if (udp.Send(cmd.Length,cmd,server))
				{
				do
					{
					len = udp.Receive(rsp.Length,rsp,ref ep);
					if (len > 0)
						rtn = encode.GetString(rsp,0,len);
					else
						Thread.Sleep(10);
					}
				while (len == 0);
				}
			else
				{
				rtn = "fail UDP send failure: " + udp.LastError();
				last_error = udp.LastError();
				}
			return(rtn);
		}




		public static int ReceiveFile(string fname,int len)

		{
			int recv = 0,rlen;
			FileStream fsw;
			byte[] rsp = new byte[512];
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			int count = 0,timeout_count = 40;

			fsw = File.Create(fname);
			if (fsw != null)
				{
				do
					{
					count = 0;
					do
						{
						rlen = udp.Receive(rsp.Length,rsp,ref ep);
						if (rlen > 0)
							{
							fsw.Write(rsp,0,rlen);
							fsw.Flush();
							recv += rlen;
							}
						else
							{
							count += 1;
							if (count < timeout_count)
								Thread.Sleep(10);
							}
						}
					while ((rlen == 0) && (count < timeout_count));
					if (count == timeout_count)
						{
						last_error = SharedData.UDP_TIMEOUT;
						break;
						}
					}
				while(recv < len);
				fsw.Close();
				}
			return(recv);
		}



		public static void SaveParams(string mc_ip_address,string mc_base_port_no, string rc_ip_address,string rc_port_no)

		{
			string dir, fname;
			TextWriter sw;

			dir = Application.StartupPath + SharedData.CAL_SUB_DIR;
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			fname = dir + CtoCCom.PARAM_FILE;
			sw = File.CreateText(fname);
			if (sw != null)
				{
				sw.WriteLine(rc_ip_address);
				sw.WriteLine(rc_port_no);
				sw.WriteLine(mc_ip_address);
				sw.WriteLine(mc_base_port_no);
				sw.Close();
				}
		}



		public static string LastError()

		{
			return(last_error);
		}


		}
	}
