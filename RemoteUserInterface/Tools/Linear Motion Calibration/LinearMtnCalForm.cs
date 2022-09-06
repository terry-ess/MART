using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;

namespace Linear_Motion_Calibration
	{
	public partial class LinearMtnCalForm : Form
		{
		private Connection tcntl = null;
		private int current_tab = 0;
		private string tool_name;

		public static Connection tconnect = null;
		public static SemaphoreSlim tool_access = new SemaphoreSlim(1);

		private Connection kaconnect = null;
		private System.Timers.Timer katimer = null;
		private bool keep_alive_active = false;
		private IPEndPoint karcvr;

		public LinearMtnCalForm()

		{
			string rsp;
			bool started = false;
			string fname;
			DateTime fdt;

			InitializeComponent();
			tool_name = new AssemblyName(Assembly.GetCallingAssembly().FullName).Name;
			fname = Application.StartupPath + Folders.TOOLS_SUB_DIR + tool_name + ".dll";
			fdt = File.GetLastWriteTime(fname);
			AboutTextBox.AppendText("\r\nBuild date: " + fdt.ToShortDateString() + "  " + fdt.ToShortTimeString() + "\r\n");
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
						keep_alive_active = true;
						karcvr = new IPEndPoint(IPAddress.Parse(MainControl.robot_ip_address), UiConstants.TOOL_KEEP_ALIVE_PORT_NO);
						katimer = new System.Timers.Timer(500);
						katimer.Elapsed += KeepAliveTimer;
						katimer.Enabled = true;
						started = true;
						}
					else
						{
						tcntl.Close();
						tcntl = null;
						tconnect = null;
						}
					}
				else
					{
					tcntl.Close();
					tcntl = null;
					}
				}
			if (!started)
				{
				this.Enabled = false;
				MessageBox.Show("Could not establish a connection with the tool.", "Error");
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



		private void KeepAliveTimer(object sender, System.Timers.ElapsedEventArgs e)

		{
			if (keep_alive_active)
				{
				kaconnect.Send(UiConstants.KEEP_ALIVE,karcvr,false);
				}
		}

		private void LinearMtnCalForm_FormClosing(object sender, FormClosingEventArgs e)

		{
			if (current_tab == 1)
				BasicParamCntl.Close();
			else if (current_tab == 2)
				MotionTestCntl.Close();
			if (katimer != null)
				{
				katimer.Enabled = false;
				katimer.Close();
				katimer = null;
				keep_alive_active = false;
				}
			if ((tcntl != null) && tcntl.Connected())
				{
				tcntl.SendCommand(UiConstants.STOP + "," + tool_name, 20);
				tcntl.Close();
				tcntl = null;
				}
			if ((tconnect != null) && tconnect.Connected())
				{
				tconnect.Close();
				tconnect = null;
				}
			if ((kaconnect != null) && kaconnect.Connected())
				{
				kaconnect.Close();
				kaconnect = null;
				}
		}



		private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)

		{
			string rsp;

			if (current_tab == 1)
				BasicParamCntl.Close();
			else if (current_tab == 2)
				MotionTestCntl.Close();
			rsp = tconnect.SendCommand(UiConstants.TAB + "," + tabControl1.SelectedIndex, 10);
			if (rsp.StartsWith(UiConstants.OK))
				{
				if (tabControl1.SelectedIndex == 1)
					BasicParamCntl.Open();
				else if (tabControl1.SelectedIndex == 2)
					MotionTestCntl.Open();
				current_tab = tabControl1.SelectedIndex;
				}
			else
				{
				tabControl1.SelectedIndex = current_tab;
				if (current_tab == 1)
					BasicParamCntl.Open();
				}
		}

		}
	}
