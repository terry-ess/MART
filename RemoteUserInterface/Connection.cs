using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Constants;

namespace RobotConnection
	{
	public class Connection
		{

		public const string NETWORK_ADDRESS = "192.168.0";

		private UdpConnection udp = null;
		private IPEndPoint robot;
		private bool connected = false;


		public Connection(string robot_ip_address,int port_no,bool sender,int trys = 1,bool full_log = true, int input_buffer_size = 8192)

		{
			string rsp;
			int attempts = 0;
			IPAddress[] ipa;
			string ip_address = "";

			if (udp == null)
				{
				ipa = Dns.GetHostAddresses(Dns.GetHostName());
				foreach (IPAddress ip in ipa)
					{
					if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
						{
						ip_address = ip.ToString();
						if (ip_address.StartsWith(NETWORK_ADDRESS))
							break;
						else
							ip_address = "";
						}
					}
				if (ip_address.Length > 0)
					{
					if (sender)
						{
						udp = new UdpConnection(ip_address,port_no,robot_ip_address,port_no,input_buffer_size);
						if (udp.Connected())
							{
							robot = udp.Robot();
							do
								{
								rsp = SendCommand(UiConstants.HELLO,100);
								if (rsp.StartsWith(UiConstants.OK))
									{
									Log.LogEntry("Robot connection established on port " + port_no);
									connected = true;
									break;
									}
								attempts += 1;
								}
							while(attempts < trys);
							if (!connected)
								{
								udp.Close();
								if (full_log)
									{
									Log.LogEntry("Robot hello failed on port " + port_no);
									MessageBox.Show("Robot hello failed on port " + port_no, "Error");
									}
								}
							}
						else if (full_log)
							{
							Log.LogEntry("Could not open UDP connection on port " + port_no);
							MessageBox.Show("Could not open UDP connection on port " + port_no, "Error");
							}
						}
					else
						{
						udp = new UdpConnection(ip_address, port_no,input_buffer_size);
						connected = udp.Connected();
						if (connected)
							Log.LogEntry("Robot connection opened on " + ip_address.ToString() + ":" + port_no);
						else
							Log.LogEntry("Could not open UDP connection on " + ip_address.ToString() + ":" + port_no);
						}
					}
				else
					{
					Log.LogEntry("Could not resolve this computers IP address.");
					MessageBox.Show("Could not resolve this computers IP address.", "Error");
					}
				}
		}



		public bool Connected()

		{
			return(connected);
		}



		public string SendCommand(string command,int timeout_count,bool log = true)

		{
			string rtn = "";
			byte[] rsp = new byte [UdpConnection.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();

			if (udp != null)
				{
				udp.ClearReceive();
				if (timeout_count < 20)
					timeout_count = 20;
				if (Send(command,log))
					rtn = ReceiveResponse(timeout_count,log);
				else
					rtn = "fail UDP send failure";
				}
			else
				rtn = "fail UDP not open.";
			return(rtn);
		}



		public bool Send(string msg,bool log = true)

		{
			byte[] cmd;
			ASCIIEncoding encode = new ASCIIEncoding();
			bool rtn = false;

			if (log)
				Log.LogEntry(msg + " (" + robot.Port + ")");
			if (udp != null)
				{
				cmd = encode.GetBytes(msg);
				rtn = udp.Send(cmd.Length, cmd, robot);
				}
			return(rtn);
		}



		public bool Send(string msg,IPEndPoint rcvr,bool log = false)

		{
			byte[] cmd;
			ASCIIEncoding encode = new ASCIIEncoding();
			bool rtn = false;

			if (log)
				Log.LogEntry(msg + " (" + rcvr.Port + ")");
			if (udp != null)
				{
				cmd = encode.GetBytes(msg);
				rtn = udp.Send(cmd.Length, cmd,rcvr);
				}
			return(rtn);
		}




		public string ReceiveResponse(int timeout_count,bool log = false)

		{
			byte[] rsp = new byte[UdpConnection.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();
			int len = 0;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			int count = 0;
			string rtn = "";

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
				rtn = "fail UDP receive timedout";
			if (log)
				Log.LogEntry(rtn);
			return(rtn);
		}



		public string ReceiveResponse(int timeout_count,ref IPEndPoint ep,bool log = false)

		{
			byte[] rsp = new byte[UdpConnection.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();
			int len = 0;
			int count = 0;
			string rtn = "";

			do
				{
				len = udp.Receive(rsp.Length, rsp, ref ep);
				if (len > 0)
					rtn = encode.GetString(rsp, 0, len);
				else
					{
					count += 1;
					if (count < timeout_count)
						Thread.Sleep(10);
					}
				}
			while ((len == 0) && (count < timeout_count));
			if (count == timeout_count)
				rtn = "fail UDP receive timedout";
			if (log)
				Log.LogEntry(rtn);
			return (rtn);
		}



		public int ReceiveStream(ref MemoryStream ms,int len)

		{
			int recv = 0,rlen;
			byte[] rsp = new byte[UdpConnection.MAX_DG_SIZE];
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			int count = 0,timeout_count = 40;

			do
				{
				count = 0;
				do
					{
					rlen = udp.Receive(rsp.Length,rsp,ref ep);
					if (rlen > 0)
						{
						ms.Write(rsp,0,rlen);
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
					Log.LogEntry("ReceiveStream time out after receiving " + recv + " bytes.");
					recv = 0;
					break;
					}
				}
			while(recv < len);
			if (recv != len)
				Log.LogEntry("ReceiveStream failed.");
			return(recv);
		}



		public void ClearReceive()

		{
			udp.ClearReceive();
		}


		public void Close()

		{
			if (udp != null)
				{
				udp.Close();
				udp = null;
				connected = false;
				}
		}

		}
	}
