using System;
using System.Net;
using System.Threading;
using AutoRobotControl;
using Constants;


namespace Sub_system_Operation
	{
	class SubsysOp : ToolsInterface
		{

		private static UiConnection connect = null;
		private Thread srvr = null;
		private UiConnection kaconnect = null;
		private Thread kasrvr = null;
		private bool run = false;
		private int current_tab = 0;
		private delegate string MessageHandler(string msg,IPEndPoint ep);
		private MessageHandler MsgHandle = null;


		public bool Open(params object[] obj)

		{
			bool rtn = false;

			if (AutoRobotControl.HeadAssembly.Connected())
				{
				connect = new UiConnection(Constants.UiConstants.TOOL_PORT_NO);
				kaconnect = new UiConnection(Constants.UiConstants.TOOL_KEEP_ALIVE_PORT_NO);
				if (connect.Connected() && kaconnect.Connected())
					{
					run = true;
					srvr = new Thread(UdpServer);
					srvr.Start();
					kasrvr = new Thread(KaServer);
					kasrvr.Start();
					rtn = true;
					}
				else
					{
					if (connect.Connected())
						connect.Close();
					connect = null;
					kaconnect = null;
					}
				}
			return (rtn);
		}



		public void Close()

		{
			run = false;
			switch (current_tab)
				{
				case 1:
					HeadAssembly.Close();
					break;

				case 2:
					KinectOp.Close();
					break;

				case 3:
					ArmOp.Close();
					break;
				}
			if ((srvr != null) && srvr.IsAlive)
				srvr.Join();
			if ((kasrvr != null) && kasrvr.IsAlive)
				kasrvr.Join();
			if (connect != null)
				{
				connect.Close();
				connect = null;
				}
			if (kaconnect != null)
				{
				kaconnect.Close();
				kaconnect = null;
				}
		}



		private void KaServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg;
			int no_msg_count = 0;

			Log.LogEntry("Subsys op keep alive server started.");
			while (run)
				{
				msg = kaconnect.ReceiveCmd(ref ep);
				if (run && (msg.Length > 0))
					{
					if (msg == UiConstants.KEEP_ALIVE)
						no_msg_count = 0;
					Thread.Sleep(10);
					}
				else
					{
					if (run)
						{
						no_msg_count += 1;
						if (no_msg_count < UiConstants.INTF_TIMEOUT_COUNT)
							Thread.Sleep(10);
						else
							{
							Log.LogEntry("Subsys op timed out.");
							run = false;
							if ((srvr != null) && srvr.IsAlive)
								srvr.Join();
							kasrvr = null;
							Tools.CloseTool();
							}
						}
					}
				}
			Log.LogEntry("Sub sys keep alive server closed");
		}



		private void UdpServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg,rsp = "";
			int new_tab;
			string[] values;
			bool tab_opened = false;

			Log.LogEntry("Subsys op server started.");
			while (run)
				{

				try
				{
				msg = connect.ReceiveCmd(ref ep);
				if (msg.Length > 0)
					{
					if (msg == UiConstants.HELLO)
						rsp = UiConstants.OK;
					else if (msg == UiConstants.CLOSE)
						{
						connect.Close();
						run = false;
						rsp = "";
						}
					else if (msg.StartsWith(UiConstants.TAB + ","))
						{
						values = msg.Split(',');
						if (values.Length == 2)
							{
							new_tab = int.Parse(values[1]);
							if (new_tab != current_tab)
								{
								MsgHandle = null;
								switch(new_tab)
									{
									case 0:
										tab_opened = true;
										MsgHandle = null;
										break;

									case 1:
										if (HeadAssembly.Open(connect))
											{
											tab_opened = true;
											MsgHandle = HeadAssembly.MessageHandler;
											}
										break;

									case 2:
										if (KinectOp.Open(connect))
											{
											tab_opened = true;
											MsgHandle = KinectOp.MessageHandler;
											}
										break;

									case 3:
										if (ArmOp.Open(connect))
											{
											tab_opened = true;
											MsgHandle = ArmOp.MessageHandler;
											}
										break;

									case 4:
										if (SonarOp.Open(connect))
											{
											tab_opened = true;
											MsgHandle = SonarOp.MessageHandler;
											}
										break;

									default:
										break;
									}
								if (tab_opened)
									{
									switch(current_tab)
										{
										case 1:
											HeadAssembly.Close();
											break;

										case 2:
											KinectOp.Close();
											break;

										case 3:
											ArmOp.Close();
											break;

										case 4:
											SonarOp.Close();
											break;
										}
									current_tab = new_tab;
									rsp = UiConstants.OK;
									}
								else
									rsp = UiConstants.FAIL;
								}
							else
								rsp = UiConstants.OK;
							}
						else
							rsp = UiConstants.FAIL + ",bad format";
						}
					else
						{
						if (MsgHandle == null)
							rsp = UiConstants.FAIL + ",unknown command";
						else
							rsp = MsgHandle(msg,ep);
						}
					if (rsp.Length > 0)
						connect.SendResponse(rsp,ep,true);
					}
				else
					{
					if (run)					
						Thread.Sleep(10);
					}
				}

				catch (Exception ex)
				{
				Log.LogEntry("Subsys op server exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				}
			Log.LogEntry("Subsys op server stopped.");
		}


		}
	}
