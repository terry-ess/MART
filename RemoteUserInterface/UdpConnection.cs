using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace RobotConnection
	{

	public class UdpConnection
	{
	
		public static int MAX_DG_SIZE = 1400;

		
		private IPEndPoint ip_end;
		private Socket sock;
		private bool connected = false;
		private IPEndPoint robot_ip_end;
		private string last_error;
		private byte[] tc = new byte[UdpConnection.MAX_DG_SIZE];
		


		public bool Send(int length,byte[] buff,IPEndPoint to)

		{
			bool rtn = false;
		
			if ((connected == true) && (length <= MAX_DG_SIZE))
				{
				try
					{
					if (sock.SendTo(buff,0,length,SocketFlags.None,to) == length)
						rtn = true;
					}
					
				catch
					{
					last_error = "UDP send failed.";
					}
				}
			return(rtn);
		}		



		public int Receive(int length,byte[] buff,ref IPEndPoint from)
		
		{
			int rtn = 0;
			IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
			EndPoint senderep = sender;
			
			if (connected)
				{
				try
					{
					if (sock.Available > 0)
						rtn = sock.ReceiveFrom(buff,length,SocketFlags.None,ref senderep);
					}
						
				catch (SocketException)
					{
					rtn = 0;
					connected = false;
					last_error = "UDP receive socket exception, connection closed.";
					}
						
				catch
					{
					rtn = 0;
					last_error = "UDP receive exeception.";
					}
				}
			if (rtn > 0)
				{
				from = (IPEndPoint) senderep;
				}
			return(rtn);
		}



		public void ClearReceive()

		{
			IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
			EndPoint senderep = (EndPoint)sender;

			
			if (connected)
				{
				try
				{
				while (sock.Available > 0)
					sock.ReceiveFrom(tc,tc.Length,SocketFlags.None,ref senderep);
				}
						
						
				catch
				{
				}

				}
		}



		public bool Connected()

		{
			return(connected);
		}



		public void Close()

		{
			if (connected)
				{
				sock.Close();
				connected = false;
				}
		}



		public IPEndPoint Robot()
		
		{
			return(robot_ip_end);
		}



		public UdpConnection(string ip_address,int port_no, string robot_ip_address, int robot_port_no,int input_buffer_size = 8192)

		{
			IPAddress ip_addr;
			
			try
				{
				sock = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
				ip_addr = IPAddress.Parse(ip_address);
				ip_end = new IPEndPoint(ip_addr ,port_no);
				sock.Bind(ip_end);
				if (sock.ReceiveBufferSize < input_buffer_size)
					sock.ReceiveBufferSize = input_buffer_size;
				ip_addr = IPAddress.Parse(robot_ip_address);
				robot_ip_end = new IPEndPoint(ip_addr, robot_port_no);
				connected = true;
				}
				
			catch(Exception ex)
				{
				sock.Close();
				connected = false;
//				MessageBox.Show("Exception: " + ex.Message,"Error");
				Log.LogEntry("UdpClient exception: " + ex.Message);
				Log.LogEntry("stack trace: " + ex.StackTrace);
				Log.LogEntry("port number: " + port_no);
				last_error = "UDP open exception " + ex.Message;
				}
		}



		public UdpConnection(string ip_address, int port_no, int input_buffer_size = 8192)

		{
			IPAddress ip_addr;

			try
				{
				sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				ip_addr = IPAddress.Parse(ip_address);
				ip_end = new IPEndPoint(ip_addr, port_no);
				sock.Bind(ip_end);
				if (sock.ReceiveBufferSize < input_buffer_size)
					sock.ReceiveBufferSize = input_buffer_size;
				connected = true;
				}

			catch (Exception ex)
				{
				sock.Close();
				connected = false;
//				MessageBox.Show("Exception: " + ex.Message,"Error");
				Log.LogEntry("UdpClient exception: " + ex.Message);
				Log.LogEntry("          stack trace: " + ex.StackTrace);
				last_error = "UDP open exception " + ex.Message;
				}
		}



		public string LastError()

		{
			return(last_error);
		}

	}
}
