using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;
using Renci.SshNet;


namespace AutoRobotControl
	{
	public static class SpeechDirection
		{

		private const string USER = "pi";
		private const string PASSWORD = "Rb351919";
		private const string RUN_DIRECT_SERVER = "/home/pi/usb_4_mic_array/DOAServer.sh";
		private const string SHUTDOWN = "sudo shutdown now";
		private const string NETWORK_ADDRESS = "192.168.2";
		private const NetworkInterfaceType NITYPE = NetworkInterfaceType.Ethernet;

		public const string PARAM_FILE = "speechdirect.param";

		private static string host;
		private static bool connected = false;
		private static Object com_access_lock = new Object();
		private static SshClient client = null;
		private static Thread direct_server;
		private static DeviceComm dc = new DeviceComm();
		private static string server_ip;
		private static int server_port;
		private static object direct_lock = new object();


		static SpeechDirection()

		{
			string fname;
			TextReader tr;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				
				try
				{
				host = tr.ReadLine();
				server_ip = tr.ReadLine();
				server_port = int.Parse(tr.ReadLine());
				}

				catch (Exception ex)
				{
				host = "";
				Log.LogEntry("SpeechDirection exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}
				
				tr.Close();
				}
			else
				Log.LogEntry("Could not find " + PARAM_FILE);
		}



		public static bool Open()

		{
			bool ssh_connect_done = false;
			int tries = 0;
			const int TRY_LIMIT = 2;

			if ((client == null) || !client.IsConnected)
				{
				if (host.Length > 0)
					{
					client = new SshClient(host, USER, PASSWORD);
					do
						{

						try
						{
						client.Connect();
						ssh_connect_done = true;
						}

						catch (Exception)
						{
						tries += 1;
						if (tries < TRY_LIMIT)
							{
							Thread.Sleep(1000);
							}
						else
							ssh_connect_done = true;
						}

						}
					while (!ssh_connect_done);
					Log.LogEntry("No ssh connection tries: " + tries);
					if (client.IsConnected)
						{
						connected = CoreOpen();
						}
					else
						{
						client = null;
						Log.LogEntry("SpeechDirection.Open: Could not connect ssh client.");
						}
					}
				else
					Log.LogEntry("Host not available.");
				}
			else
				connected = CoreOpen();
			if (!connected)
				if ((client != null) && client.IsConnected)
					{
					Thread sd = new Thread(ShutdownDirectServer);
					sd.Start();
					}
			return (connected);
		}



		public static void Close()

		{
			Thread sd;

			if (connected)
				{
				dc.SendCommand("exit",10);
				dc.Close();
				SharedData.speech_direct_operational = false;
				if ((client != null) && client.IsConnected)
					{
					sd = new Thread(ShutdownDirectServer);
					sd.Start();
					if ((direct_server != null) && direct_server.IsAlive)
						direct_server.Join();
					if (sd.IsAlive)
						sd.Abort();
					}
				connected = false;
				}
		}



		public static int GetSpeechDirection()

		{
			string rsp;
			int direct = -1;

			if (connected)
				{

				lock(direct_lock)
				{
				rsp = dc.SendCommand("direct", 200);
				if (rsp.StartsWith("OK"))
					{
					direct = int.Parse(rsp.Substring(3));
					direct = 360 - direct;
					}
				}

				}
			return (direct);
		}



		private static void ShutdownDirectServer()

		{
			try
			{
			client.RunCommand(SHUTDOWN);
			client = null;
			}

			catch (Exception)
			{
			client = null;
			}

			Log.LogEntry("ShutdownDirectServer closed.");
		}




		private static void RunDirectServer()

		{
			try
			{
			client.RunCommand(RUN_DIRECT_SERVER);
			}

			catch(Exception ex)
			{
			Log.LogEntry("RunDirectServer exception: " + ex.Message);
			}

			Log.LogEntry("RunDirectServer closed");
		}



		private static bool CoreOpen()

		{
			bool rtn = false;
			string rsp,client_ip;

			client_ip = ClientIP();
			if (client_ip.Length > 0)
				{
				direct_server = new Thread(RunDirectServer);
				direct_server.Start();
				Thread.Sleep(1000);
				if (direct_server.IsAlive && dc.Open(client_ip,server_port,server_ip,server_port))
					{
					rsp = dc.SendCommand("hello", 100);
					if (rsp.StartsWith("OK"))
						{
						Log.LogEntry("Speech direction server connection established.");
						SharedData.speech_direct_operational = true;
						rtn = true;
						}
					else
						Log.LogEntry("Speech direction server connection bad response. Response: " + rsp);
					}
				else
					Log.LogEntry("Speech direction server connection could not be opened.");
				}
			else
				Log.LogEntry("Could not determine client IP address.");
			return (rtn);
		}



		private static string ClientIP()

		{
			string ip_address = "";
			NetworkInterface[] nics;
			UnicastIPAddressInformationCollection uic;

			nics = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface adapter in nics)
				{
				if ((adapter.NetworkInterfaceType == NITYPE) && (adapter.OperationalStatus == OperationalStatus.Up))
					{
					uic = adapter.GetIPProperties().UnicastAddresses;
					foreach (UnicastIPAddressInformation ui in uic)
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
			return (ip_address);
		}

		}
	}
