using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using AutoRobotControl;


namespace AutoRobotControl
	{
	public class UiConnection
		{

		public const string NETWORK_ADDRESS = "192.168.0";
		public const NetworkInterfaceType NITYPE = NetworkInterfaceType.Wireless80211;

		private UdpClient servr = null;
		private bool connected = false;
		private string ip_address = "";


		public UiConnection(int port_no)

		{
			NetworkInterface[] nics;
			UnicastIPAddressInformationCollection uic;

			nics = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface adapter in nics)
				{
				if ((adapter.NetworkInterfaceType == NITYPE) && (adapter.OperationalStatus == OperationalStatus.Up))
					{
					uic = adapter.GetIPProperties().UnicastAddresses;
					foreach(UnicastIPAddressInformation ui in uic)
						{
						if (ui.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
							{
							ip_address = ui.Address.ToString();
							if (ip_address.StartsWith(NETWORK_ADDRESS))
								break;
							else
								ip_address = "";
							}
						}
					}
				if (ip_address.Length > 0)
					break;
				}
			if (ip_address.Length > 0)
				{
				servr = new UdpClient(ip_address,port_no);
				connected = servr.Connected();
				}
		}



		public bool Connected()

		{
			return(connected);
		}



		public bool SendStream(MemoryStream ms,IPEndPoint rcvr)

		{
			bool rtn = false;
			byte[] buffer = new byte[UdpClient.MAX_DG_SIZE];
			int len,no_packets = 0;
			const int SLEEP_PACKETS = 15;

			ms.Seek(0, SeekOrigin.Begin);
			do
				{
				len = ms.Read(buffer,0,buffer.Length);
				if (len > 0)
					{
					if (servr.Send(len,buffer,rcvr))
						{
						no_packets += 1;								//THIS IS A KLUDGE BUT ATTEMPT TO USE BIGGER RECEIVE BUFFER ON REMOTE UI DID NOT WORK
						if (no_packets % SLEEP_PACKETS == 0)
							Thread.Sleep(5);
						}
					else
						{
						Log.LogEntry("stream send failed.");
						break;
						}
					}
				else
					rtn = true;
				}
			while(len > 0);
			return(rtn);
		}



		public string ReceiveResponse(int timeout_count)

		{
			byte[] rsp = new byte[UdpClient.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();
			int len = 0;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			int count = 0;
			string rtn = "";

			do
				{
				len = servr.Receive(rsp.Length,rsp,ref ep);
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
			return(rtn);
		}



		public bool SendResponse(string rsp,IPEndPoint rcvr,bool log = false)

		{
			byte[] buf = new byte[UdpClient.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();
			bool rtn = false;

			if ((servr != null) && (servr.Connected()))
				{
				buf = encode.GetBytes(rsp);
				rtn = servr.Send(buf.Length, buf,rcvr);
				if (log)
					{
					if (rtn)
						Log.LogEntry(rsp);
					else
						Log.LogEntry("Send failed for " + rsp);
					}
				}
			else
				Log.LogEntry("Send failed, UDP not open");
			return(rtn);
		}



		public string SendCommand(string command,int timeout_count,IPEndPoint rcvr)

		{
			string rtn = "";
			byte[] cmd;
			byte[] rsp = new byte [UdpClient.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();

//			Log.LogEntry(command);
			if (servr != null)
				{
				servr.ClearReceive();
				if (timeout_count < 20)
					timeout_count = 20;
				cmd = encode.GetBytes(command);
				if (servr.Send(cmd.Length,cmd,rcvr))
					rtn = ReceiveResponse(timeout_count);
				else
					rtn = "fail UDP send failure";
				}
			else
				rtn = "fail UDP not open.";
//			Log.LogEntry(rtn);
			return(rtn);
		}



		public string ReceiveCmd(ref IPEndPoint ep)

		{
			string msg = "";
			byte[] buf = new byte[UdpClient.MAX_DG_SIZE];
			ASCIIEncoding encode = new ASCIIEncoding();
			int len;

			if ((len = servr.Receive(buf.Length, buf, ref ep)) > 0)
				{
				msg = encode.GetString(buf, 0, len);
				if (msg != Constants.UiConstants.KEEP_ALIVE)
					Log.LogEntry(msg);
				}
			return (msg);
		}



		public void Close()

		{
			if ((servr != null) && servr.Connected())
				{
				servr.Close();
				servr = null;
				ip_address = "";
				connected = false;
				}
		}

		}
	}
