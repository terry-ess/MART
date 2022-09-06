using System;
using System.Net;
using System.Threading;
using AutoRobotControl;
using Constants;

namespace Tests
	{
	class Tests: ToolsInterface
		{

		private const string TEST0 = "comm test";
		private const string TEST1 = "office movement";
		private const string TEST2 = "top floor movement";
		private const string TEST3 = "recharge docking";
		private const string TEST4 = "hall movement";
		private const string TEST5 = "office - hall movement";
		private const string TEST6 = "mc serial test (2 min)";

		private UiConnection connect = null;
		private Thread srvr = null;
		private UiConnection kaconnect = null;
		private Thread kasrvr = null;
		private bool run = false;
		private Thread texec = null;
		private int iterations = 0;
		private bool quiet = false;
		private UiConnection status_feed = null;
		private IPEndPoint st_recvr;
		private bool test_run = false;


		public bool Open(params object[] obj)

		{
			bool rtn = false;

			if (SharedData.head_assembly_operational && SharedData.kinect_operational && SharedData.front_lidar_operational 
			&& SharedData.motion_controller_operational && SharedData.navdata_operational)
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
			if ((texec != null) && texec.IsAlive)
				{
				test_run = false;
				texec.Join();
				}
			if (status_feed != null)
				{
				status_feed.Close();
				status_feed = null;
				}
		}



		private void SendStatusMessage(string msg)

		{
			status_feed.SendResponse(UiConstants.TEST_STATUS + "," + msg,st_recvr,true);
		}



		private void KaServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg;
			int no_msg_count = 0;

			Log.LogEntry("Tests keep alive server started.");
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
							Log.LogEntry("Tests timed out.");
							run = false;
							if ((srvr != null) && srvr.IsAlive)
								srvr.Join();
							kasrvr = null;
							Tools.CloseTool();
							}
						}
					}
				}
			Log.LogEntry("Tests keep alive server closed");
		}



		private void UdpServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg,rsp = "";
			string[] values;

			Log.LogEntry("Tests server started.");
			while (run)
				{

				try
				{
				msg = connect.ReceiveCmd(ref ep);
				if (run && (msg.Length > 0))
					{
					if (msg == UiConstants.HELLO)
						rsp = UiConstants.OK;
					else if (msg == UiConstants.CLOSE)
						{
						connect.Close();
						run = false;
						rsp = "";
						}
					else if (msg == UiConstants.SEND_TESTS)
						{
						rsp = UiConstants.OK + "," + TEST0 + "," + TEST1 + "," + TEST2 + "," + TEST3 + "," + TEST4 + "," + TEST5 + "," + TEST6;
						}
					else if (msg == UiConstants.STOP_TEST)
						{
						test_run = false;
						rsp = UiConstants.OK;
						}
					else if (msg.StartsWith(UiConstants.RUN_TEST))
						{
						values = msg.Split(',');
						if ((values.Length == 4) && (status_feed == null))
							{
							status_feed = new UiConnection(UiConstants.TOOL_FEED_PORT_NO);
							if (status_feed.Connected())
								{
								st_recvr = new IPEndPoint(ep.Address,UiConstants.TOOL_FEED_PORT_NO);
								Log.LogEntry("Tool status feed connection open with end point: " + st_recvr.ToString());

								try
								{
								iterations = int.Parse(values[2]);
								quiet = Boolean.Parse(values[3]);
								rsp = UiConstants.OK;
								test_run = true;
								switch(values[1])
									{
										case TEST0:
											texec = new Thread(Test0Thread);
											texec.Start();
											break;

										case TEST1:
											texec = new Thread(Test1Thread);
											texec.Start();
											break;

										case TEST2:
											texec = new Thread(Test2Thread);
											texec.Start();
											break;

										case TEST3:
											texec = new Thread(Test3Thread);
											texec.Start();
											break;

										case TEST4:
											texec = new Thread(Test4Thread);
											texec.Start();
											break;

										case TEST5:
											texec = new Thread(Test5Thread);
											texec.Start();
											break;

										case TEST6:
											texec = new Thread(Test6Thread);
											texec.Start();
											break;

										default:
											rsp = UiConstants.FAIL + ", bad test name";
											test_run = false;
											break;
									}
								}

								catch(Exception)
								{
								rsp = UiConstants.FAIL + ", bad parameter";
								status_feed.Close();
								status_feed = null;
								}
								
								}
							else
								rsp = UiConstants.FAIL + ", could not create status feed";
							}
						else
							rsp = UiConstants.FAIL + ", bad format or already running a test";
						}
					else
						{
						rsp = UiConstants.FAIL + ",unknown command";
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
				Log.LogEntry("Manual control server exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				}

				}
			Log.LogEntry("Tests server stopped.");
		}



		private void Test0Thread()

		{
			int i;

			OutputSpeech("Starting  test. One iteration of " + TEST0, true);
			for (i = 0;i < 5;i++)
				{
				SendStatusMessage("test message " + (i + 1));
				OutputSpeech("test message " + (i + 1) + " again");
				Thread.Sleep(5000);
				}
			OutputSpeech("This is a test of a very long message to make sure that length is not a problem in communication of status of a test.", true);

			OutputSpeech(UiConstants.TEST_COMPLETED,true);
			texec = null;
			status_feed.Close();
			status_feed = null;
		}



		private void Test6Thread()

		{
			int i;
			string rtn;

			try
			{
			if (iterations == 1)
				OutputSpeech("Starting  test. One iteration " + TEST6, true);
			else
				OutputSpeech("Starting  test. " + iterations + " iterations of " + TEST6, true);
			for (i = 0; test_run && (i < iterations); i++)
				{
				SendStatusMessage("Executing 2 minute motor contoller serial test.");
				rtn = AutoRobotControl.MotionControl.SendCommand(SharedData.START_MCSERIAL_TEST, 200);
				if (rtn.StartsWith("ok"))
					{
					Thread.Sleep(2 * 60 * 1000);
					AutoRobotControl.MotionControl.SendCommand(SharedData.STOP_MCSERIAL_TEST, 200);
					SendStatusMessage("iteration " + (i + 1) + " completed.");
					}
				else
					{
					SendStatusMessage("Attempt to start test resulted in error: " + rtn );
					}
				}
			OutputSpeech(UiConstants.TEST_COMPLETED, true);
			}

			catch(Exception ex)
			{
			OutputSpeech("Test 6 exception: " + ex.Message,true);
			SendStatusMessage(UiConstants.TEST_COMPLETED);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			texec = null;
			status_feed.Close();
			status_feed = null;
		}



		private void Test5Thread()

		{
			Speech.STSHandler stshi;
			Speech.pair_handlers ph;
			int i;

			try
			{
			ph = (Speech.pair_handlers) Speech.handlers[SmartMotionCommands.GRAMMAR];
			stshi = (Speech.STSHandler) ph.stsh;
			if (stshi == null)
				{
				OutputSpeech("No handler is available for smart commands",true);
				SendStatusMessage(UiConstants.TEST_COMPLETED);
				}
			else
				{
				test_run = true;
				SharedData.start = NavData.GetCurrentLocation();
				if (iterations == 1)
					OutputSpeech("Starting  test. One iteration " + TEST5, true);
				else
					OutputSpeech("Starting  test. " + iterations + " iterations of " + TEST5,true);
				for (i = 0;test_run && (i < iterations);i++)
					{
					OutputSpeech("Executing go to hall.");
					if (stshi.Invoke("go to hall"))
						{
						if (test_run)
							OutputSpeech("Executing go to office desk.");
						if (test_run && stshi.Invoke("go to office desk"))
							{
							OutputSpeech("Executing go to hall exit gym.");
							if (test_run && stshi.Invoke("go to hall exit gym"))
								{
								if (test_run)
									OutputSpeech("Executing go to office recharge.");
								if (test_run && stshi.Invoke("go to office recharge"))
									{
									OutputSpeech("Iteration " + (i + 1) + " completed.");
									}
								else if (!test_run)
									OutputSpeech("The command was user terminated.");
								else
									{
									OutputSpeech("The command go to office recharge failed.",true);
									break;
									}
								}
							else if (!test_run)
								OutputSpeech("The command was user terminated.");
							else
								{
								OutputSpeech ("The command go to gym center failed.",true);
								break;
								}
							}
						else if (!run)
							OutputSpeech("The command was user terminated.");
						else
							{
							OutputSpeech("The command go to office desk failed.",true);
							break;
							}
						}
					else
						{
						OutputSpeech ("The command go to hall failed.",true);
						break;
						}
					}
				OutputSpeech(UiConstants.TEST_COMPLETED,true);
				}
			}

			catch(Exception ex)
			{
			OutputSpeech("Test 2 exception: " + ex.Message,true);
			SendStatusMessage(UiConstants.TEST_COMPLETED);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			texec = null;
			status_feed.Close();
			status_feed = null;
		}



		private void Test4Thread()

		{
			Speech.STSHandler stshi;
			Speech.pair_handlers ph;
			int i;

			try
			{
			ph = (Speech.pair_handlers) Speech.handlers[SmartMotionCommands.GRAMMAR];
			stshi = (Speech.STSHandler) ph.stsh;
			if (stshi == null)
				{
				OutputSpeech("No handler is available for smart commands",true);
				SendStatusMessage(UiConstants.TEST_COMPLETED);
				}
			else
				{
				test_run = true;
				if (iterations == 1)
					OutputSpeech("Starting  test. One iteration of " + TEST4, true);
				else
					OutputSpeech("Starting  test. " + iterations + " iterations of " + TEST4,true);
				for (i = 0;test_run && (i < iterations);i++)
					{
					OutputSpeech("Executing go to hall exit gym.");
					if (stshi.Invoke("go to hall exit gym"))
						{
						if (i == iterations -1)
							{
							if (test_run)
								OutputSpeech("Executing go to office recharge.");
							if (test_run && stshi.Invoke("go to office recharge"))
								{
								OutputSpeech("Iteration " + (i + 1) + " completed.");
								}
							else if (!test_run)
								OutputSpeech("The command was user terminated.");
							else
								{
								OutputSpeech("The command go to office recharge failed.",true);
								break;
								}
							}
						else
							{
							if (test_run)
								OutputSpeech("Executing go to office.");
							if (test_run && stshi.Invoke("go to office"))
								{
								OutputSpeech("Iteration " + (i + 1) + " completed.");
								}
							else if (!test_run)
								OutputSpeech("The command was user terminated.");
							else
								{
								OutputSpeech("The command go to office recharge failed.",true);
								break;
								}
							}
						}
					else
						{
						OutputSpeech ("The command go to hall exit gym failed.",true);
						break;
						}
					}
				OutputSpeech(UiConstants.TEST_COMPLETED,true);
				}
			}

			catch(Exception ex)
			{
			OutputSpeech("Test 4 exception: " + ex.Message,true);
			SendStatusMessage(UiConstants.TEST_COMPLETED);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			texec = null;
			status_feed.Close();
			status_feed = null;
		}



		private void Test3Thread()

		{
			Speech.STSHandler stshi;
			Speech.pair_handlers ph;
			int i;

			try
			{
			ph = (Speech.pair_handlers) Speech.handlers[SmartMotionCommands.GRAMMAR];
			stshi = (Speech.STSHandler) ph.stsh;
			if (stshi == null)
				{
				OutputSpeech("No handler is available for smart commands",true);
				SendStatusMessage(UiConstants.TEST_COMPLETED);
				}
			else
				{
				test_run = true;
				SharedData.start = NavData.GetCurrentLocation();
				if (iterations == 1)
					OutputSpeech("Starting test. One iteration of " + TEST3, true);
				else
					OutputSpeech("Starting test. " + iterations + " iterations of " + TEST3,true);
				for (i = 0;test_run && (i < iterations);i++)
					{
					OutputSpeech("Executing go to calibrate.");
					if (stshi.Invoke("go to calibrate"))
						{
						OutputSpeech("Executing go to recharge.");
						if (stshi.Invoke("go to recharge"))
							{
							OutputSpeech("Iteration " + (i + 1) + " completed.");
							}
						else
							{
							OutputSpeech("The command go to recharge failed.",true);
							break;
							}
						}
					else
						{
						OutputSpeech("The command go to calibrate failed.",true);
						break;
						}
					}
				if (!test_run)
					OutputSpeech("The command was user terminated.");
				OutputSpeech(UiConstants.TEST_COMPLETED,true);
				}
			}

			catch(Exception ex)
			{
			OutputSpeech("Test 3 exception: " + ex.Message,true);
			SendStatusMessage(UiConstants.TEST_COMPLETED);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}


			texec = null;
			status_feed.Close();
			status_feed = null;
		}



		private void Test2Thread()

		{
			Speech.STSHandler stshi;
			Speech.pair_handlers ph;
			int i;

			try
			{
			ph = (Speech.pair_handlers) Speech.handlers[SmartMotionCommands.GRAMMAR];
			stshi = (Speech.STSHandler) ph.stsh;
			if (stshi == null)
				{
				OutputSpeech("No handler is available for smart commands",true);
				SendStatusMessage(UiConstants.TEST_COMPLETED);
				}
			else
				{
				test_run = true;
				SharedData.start = NavData.GetCurrentLocation();
				if (iterations == 1)
					OutputSpeech("Starting  test. One iteration " + TEST2, true);
				else
					OutputSpeech("Starting  test. " + iterations + " iterations of " + TEST2,true);
				for (i = 0;test_run && (i < iterations);i++)
					{
					OutputSpeech("Executing go to hall.");
					if (stshi.Invoke("go to hall"))
						{
						if (test_run)
							OutputSpeech("Executing go to office desk.");
						if (test_run && stshi.Invoke("go to office desk"))
							{
							OutputSpeech("Executing go to gym center.");
							if (test_run && stshi.Invoke("go to gym center"))
								{
								if (test_run)
									OutputSpeech("Executing go to office recharge.");
								if (test_run && stshi.Invoke("go to office recharge"))
									{
									OutputSpeech("Iteration " + (i + 1) + " completed.");
									}
								else if (!test_run)
									OutputSpeech("The command was user terminated.");
								else
									{
									OutputSpeech("The command go to office recharge failed.",true);
									break;
									}
								}
							else if (!test_run)
								OutputSpeech("The command was user terminated.");
							else
								{
								OutputSpeech ("The command go to gym center failed.",true);
								break;
								}
							}
						else if (!run)
							OutputSpeech("The command was user terminated.");
						else
							{
							OutputSpeech("The command go to office desk failed.",true);
							break;
							}
						}
					else
						{
						OutputSpeech ("The command go to hall failed.",true);
						break;
						}
					}
				OutputSpeech(UiConstants.TEST_COMPLETED,true);
				}
			}

			catch(Exception ex)
			{
			OutputSpeech("Test 2 exception: " + ex.Message,true);
			SendStatusMessage(UiConstants.TEST_COMPLETED);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			texec = null;
			status_feed.Close();
			status_feed = null;
		}



		private void Test1Thread()

		{
			Speech.STSHandler stshi;
			Speech.pair_handlers ph;
			int i;

			try
			{
			ph = (Speech.pair_handlers) Speech.handlers[SmartMotionCommands.GRAMMAR];
			stshi = (Speech.STSHandler) ph.stsh;
			if (stshi == null)
				{
				OutputSpeech("No handler is available for smart commands",true);
				SendStatusMessage(UiConstants.TEST_COMPLETED);
				}
			else
				{
				test_run = true;
				SharedData.start = NavData.GetCurrentLocation();
				if (iterations == 1)
					OutputSpeech("Starting test. One iteration of " + TEST1,true);
				else
					OutputSpeech("Starting test. " + iterations + " iterations of " + TEST1,true);
				for (i = 0;test_run && (i < iterations);i++)
					{
					OutputSpeech("Executing go to exit.");
					if (stshi.Invoke("go to exit"))
						{
						if (test_run)
							OutputSpeech("Executing go to office desk.");
						if (test_run && stshi.Invoke("go to office desk"))
							{
							if (test_run)
								OutputSpeech("Executing go to calibrate.");
							if (test_run && stshi.Invoke("go to calibrate"))
								{
								OutputSpeech("Executing go to desk.");
								if (test_run && stshi.Invoke("go to desk"))
									{
									if (test_run)
										OutputSpeech("Executing go to office recharge.");
									if (test_run && stshi.Invoke("go to office recharge"))
										OutputSpeech("Iteration " + (i + 1) + " was completed.");
									else if (!test_run)
										OutputSpeech("The command was user terminated.");
									else
										{
										OutputSpeech("The command go to office recharge failed.",true);
										break;
										}
									}
								else if (!test_run)
									OutputSpeech("The command was user terminated.");
								else
									{
									OutputSpeech("The command go to desk failed.",true);
									break;
									}
								}
							else if (!test_run)
								OutputSpeech("The command was user terminated.");
							else
								{
								OutputSpeech("The command go to calibrate failed.",true);
								break;
								}
							}
						else if (!test_run)
							OutputSpeech("The command was user terminated.");
						else
							{
							OutputSpeech("The command go to office desk failed.",true);
							break;
							}
						}
					}
				OutputSpeech(UiConstants.TEST_COMPLETED,true);
				}
			}

			catch(Exception ex)
			{
			OutputSpeech("Test 1 exception: " + ex.Message,true);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			SendStatusMessage(UiConstants.TEST_COMPLETED);
			}

			texec = null;
			status_feed.Close();
			status_feed = null;
		}



		private void OutputSpeech(string output,bool must_hear = false)

		{
			if ((!quiet) || must_hear)
				Speech.Speak(output);
			SendStatusMessage(output);
			Log.LogEntry(output);
		}

		}

	}
