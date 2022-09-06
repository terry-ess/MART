using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;


namespace Tests
	{
	public partial class TestsForm : Form
		{

		private Connection tcntl = null;
		private string tool_name;

		public static Connection tconnect = null;
		public static SemaphoreSlim tool_access = new SemaphoreSlim(1);

		private Connection status_feed = null;
		private bool ts_run = false;
		private Thread ts_feed = null;
		private delegate void TestStatusUpdate(string status);
		private TestStatusUpdate tsupdate;

		private Connection kaconnect = null;
		private System.Timers.Timer katimer = null;
		private bool keep_alive_active = false;
		private IPEndPoint karcvr;


		public TestsForm()

		{
			string rsp;
			bool started = false;
			string fname;
			DateTime fdt;
			string[] values;
			int i;

			InitializeComponent();
			tsupdate = TsUpdate;
			tool_name = new AssemblyName(Assembly.GetCallingAssembly().FullName).Name;
			fname = Application.StartupPath + Folders.TOOLS_SUB_DIR + tool_name + ".dll";
			fdt = File.GetLastWriteTime(fname);
			this.Text += "  " + fdt.ToShortDateString() + "  " + fdt.ToShortTimeString();
			tcntl = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_CNTL_PORT_NO,true);
			if (tcntl.Connected())
				{
				rsp = tcntl.SendCommand(UiConstants.START + "," + tool_name,UiConstants.TOOL_START_WAIT_COUNT);
				if (rsp.StartsWith(UiConstants.OK))
					{
					tconnect = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_PORT_NO,true);
					kaconnect = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_KEEP_ALIVE_PORT_NO, false);
					if (tconnect.Connected() && kaconnect.Connected())
						{
						StatusTextBox.AppendText("Tool connections opened.\r\n");
						keep_alive_active = true;
						karcvr = new IPEndPoint(IPAddress.Parse(MainControl.robot_ip_address), UiConstants.TOOL_KEEP_ALIVE_PORT_NO);
						katimer = new System.Timers.Timer(500);
						katimer.Elapsed += KeepAliveTimer;
						katimer.Enabled = true;
						started = true;
						rsp = SendToolCommand(UiConstants.SEND_TESTS,100);
						if (rsp.StartsWith(UiConstants.OK))
							{
							values = rsp.Split(',');
							if (values.Length > 1)
								{
								for (i = 1;i < values.Length;i++)
									TestsListBox.Items.Add(values[i]);
								}
							else
								{
								TestsListBox.Items.Add("No tests supported.");
								this.Enabled = false;
								}
							}
						started = true;
						}
					else
						{
						tcntl.Close();
						tcntl = null;
						tconnect = null;
						Log.LogEntry("Could not open tool connections.");
						}
					}
				else
					{
					tcntl.Close();
					tcntl = null;
					Log.LogEntry("Could not start tool.");
					}
				}
			if (!started)
				{
				this.Enabled = false;
				StatusTextBox.AppendText("Could not establish a connection with the tool.\r\n");
				}
		}



		public static string SendToolCommand(string msg,int timeout_count)


		{
			string rtn = "";

			tool_access.Wait();
			rtn = tconnect.SendCommand(msg,timeout_count);
			tool_access.Release();
			return(rtn);
		}



		private void TestsForm_FormClosing(object sender, FormClosingEventArgs e)

		{
			if (katimer != null)
				{
				katimer.Enabled = false;
				katimer.Close();
				katimer = null;
				keep_alive_active = false;
				}
			if ((ts_feed != null) && (ts_feed.IsAlive))
				{
				SendToolCommand(UiConstants.STOP_TEST, 10);
				ts_run = false;
				ts_feed.Join();
				}
			if (status_feed != null)
				{
				status_feed.Close();
				status_feed = null;
				}
			if ((tcntl != null) && tcntl.Connected())
				{
				tcntl.SendCommand(UiConstants.STOP + "," + tool_name, 20);
				tcntl.Close();
				}
			if ((tconnect != null) && tconnect.Connected())
				tconnect.Close();
			if ((kaconnect != null) && kaconnect.Connected())
				{
				kaconnect.Close();
				kaconnect = null;
				}
		}



		private void TsUpdate(string msg)

		{
			string[] val;

			if (msg.StartsWith("Iteration "))
				{
				val = msg.Split(' ');
				if (val.Length > 1)
					CITextBox.Text = val[1];
				StatusTextBox.AppendText(msg + "\r\n");
				}
			else
				{
				StatusTextBox.AppendText(msg + "\r\n");
				if (msg.Contains(UiConstants.TEST_COMPLETED))
					{
					SSButton.Text = "Start Test";
					SSButton.BackColor = System.Drawing.Color.LightGreen;
					}
				}
		}



		private void TestStatusReceive()

		{
			string msg;
			string[] val;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

			Log.LogEntry("Test status receiver started.");
			while (ts_run)	
				{

				try
				{
				msg = status_feed.ReceiveResponse(100,ref ep);
				if (msg.StartsWith(UiConstants.TEST_STATUS) && ts_run)
					{
					val = msg.Split(',');
					if (val.Length > 1)
						{
						if (val[1] == UiConstants.TEST_COMPLETED)
							ts_run = false;
						this.BeginInvoke(tsupdate,msg.Substring(val[0].Length + 1));
						}
					else
						Log.LogEntry("TestStatusReceive, bad parameter format: " + msg);
					}
				else if (!msg.StartsWith(UiConstants.FAIL))
					Log.LogEntry("TestStatusReceive, unexpected message: " + msg);
				}

				catch(ThreadAbortException)
				{
				ts_run = false;
				}

				catch(Exception ex)
				{
				Log.LogEntry("TestStatusReceive exception: " + ex.Message);
				}

				}
			ts_feed = null;
			Log.LogEntry("Test status receiver closed.");
		}



		private void SSButton_Click(object sender, EventArgs e)

		{
			string rsp,msg = "";

			if (SSButton.Text.StartsWith("Start"))
				{
				if (TestsListBox.SelectedIndex >= 0)
					{
					CITextBox.Text = "0";
					StatusTextBox.Clear();
					if ((status_feed == null) || !status_feed.Connected())
						status_feed = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_FEED_PORT_NO,false);
					if (status_feed.Connected())
						{
						msg = UiConstants.RUN_TEST + "," + TestsListBox.SelectedItem.ToString() + "," + TINumericUpDown.Value.ToString() + "," + QuietCheckBox.Checked.ToString();
						rsp = SendToolCommand(msg,100);
						if (rsp.StartsWith(UiConstants.OK))
							{
							ts_run = true;
							ts_feed = new Thread(TestStatusReceive);
							ts_feed.Start();
							SSButton.Text = "Stop Test";
							SSButton.BackColor = System.Drawing.Color.OrangeRed;
							}
						else
							{
							msg = "Could not run test " + TestsListBox.SelectedItem.ToString();
							status_feed.Close();
							status_feed = null;
							}
						}
					else
						msg = "Could not open status feed connection.";
					}
				else
					MessageBox.Show("No test is selected.","Error");
				}
			else
				{
				if ((ts_feed != null) && (ts_feed.IsAlive))
					{
					SendToolCommand(UiConstants.STOP_TEST,10);
					ts_run = false;
					ts_feed.Join();
					SSButton.Text = "Start Test";
					SSButton.BackColor = System.Drawing.Color.LightGreen;
					}
				}
		}



		private void KeepAliveTimer(object sender, System.Timers.ElapsedEventArgs e)

		{
			if (keep_alive_active)
				{
				kaconnect.Send(UiConstants.KEEP_ALIVE,karcvr,false);
				}
		}



		}
	}
