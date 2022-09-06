using System;
using System.Net;
using System.Text;
using System.Threading;


namespace AutoRobotControl
	{
	
	class DeviceComm
		{
		private UdpClient udp = null;
		private IPEndPoint server;


		public bool Open(string cip,int cport,string dip,int dport)

		{
			bool rtn = false;

			if ((udp == null) || !udp.Connected())
				{
				udp = new UdpClient(cip,cport,dip,dport);
				if (udp.Connected())
					{
					server = udp.Server();
					rtn = true;
					}
				else
					{
					Log.LogEntry("DeviceComm open failed.");
					}
				}
			else
				rtn = true;
			return(rtn);
		}



		public void Close()

		{
			if ((udp != null) && (udp.Connected()))
				{
				udp.Close();
				udp = null;
				}
		}



		public bool Connected()

		{
			bool rtn = false;

			if ((udp != null) && udp.Connected())
				rtn = true;
			return(rtn);
		}



		public string SendCommand(string command,int timeout_count)

		{
			string rtn = "";
			byte[] cmd;
			byte[] rsp = new byte [UdpClient.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();

//			Log.LogEntry(command);
			if (udp != null)
				{
				if (timeout_count < 20)
					timeout_count = 20;
				cmd = encode.GetBytes(command);
				udp.ClearReceive();
				if (udp.Send(cmd.Length,cmd,server))
					rtn = ReceiveResponse(timeout_count);
				else
					rtn = "fail UDP send failure";
				}
			else
				rtn = "fail UDP not open.";
			return(rtn);
		}



		private string ReceiveResponse(int timeout_count)

		{
			byte[] rsp = new byte[UdpClient.MAX_DG_SIZE];
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
//			Log.LogEntry(rtn);
			return(rtn);
		}


		}
	}
