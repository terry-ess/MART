using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;

namespace Person_Move_PID_Calibration
	{
	public partial class PersonPIDCalForm : Form
		{

		private Connection tcntl = null;
		private string tool_name;

		private Connection tconnect = null;
		private SemaphoreSlim tool_access = new SemaphoreSlim(1);

		private Connection kaconnect = null;
		private System.Timers.Timer katimer = null;
		private bool keep_alive_active = false;
		private IPEndPoint karcvr;

		private Connection status_feed = null;
		private bool ts_run = false;
		private Thread ts_feed = null;
		private delegate void RunStatusUpdate(string status);
		private RunStatusUpdate rsupdate;
		private delegate void TableUpdate(string msg,string file);
		private TableUpdate tupdate;



		public PersonPIDCalForm()

		{
			string rsp;
			bool started = false;
			string fname;
			DateTime fdt;
			
			InitializeComponent();
			tool_name = new AssemblyName(Assembly.GetCallingAssembly().FullName).Name;
			fname = Application.StartupPath + Folders.TOOLS_SUB_DIR + tool_name + ".dll";
			fdt = File.GetLastWriteTime(fname);
			this.Text = this.Text + "  " + fdt.ToShortDateString() + "  " + fdt.ToShortTimeString();
			tcntl = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_CNTL_PORT_NO,true);
			if (tcntl.Connected())
				{
				StatusTextBox.AppendText("Connected with " + UiConstants.TOOL_CNTL_PORT_NO + "\r\n");
				rsp = tcntl.SendCommand(UiConstants.START + "," + tool_name,UiConstants.TOOL_START_WAIT_COUNT);
				if (rsp.StartsWith(UiConstants.OK))
					{
					StatusTextBox.AppendText("Tool connection opened.\r\n");
					tconnect = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_PORT_NO,true,1,true);
					kaconnect = new Connection(MainControl.robot_ip_address,UiConstants.TOOL_KEEP_ALIVE_PORT_NO,false);
					if (tconnect.Connected() && kaconnect.Connected())
						{
						StatusTextBox.AppendText("Tool connections opened.\r\n");
						keep_alive_active = true;
						karcvr = new IPEndPoint(IPAddress.Parse(MainControl.robot_ip_address), UiConstants.TOOL_KEEP_ALIVE_PORT_NO);
						katimer = new System.Timers.Timer(500);
						katimer.Elapsed += KeepAliveTimer;
						katimer.Enabled = true;
						started = true;
						rsupdate = StatUpdate;
						tupdate = DLVUpdate;
						}
					else
						{
						tcntl.Close();
						tcntl = null;
						if (tconnect.Connected())
							tconnect.Close();
						tconnect = null;
						kaconnect = null;
						Log.LogEntry("Could not open tool connections.");
						StatusTextBox.AppendText("Could not open tool connections.\r\n");
						this.Enabled = false;
						}
					}
				else
					{
					tcntl.Close();
					tcntl = null;
					this.Enabled = false;
					Log.LogEntry("Could not start tool.");
					StatusTextBox.AppendText("Could not start tool.\r\n");
					}
				}
			if (!started)
				{
				this.Enabled = false;
				Log.LogEntry("Could not establish a connection with the tool.");
				StatusTextBox.AppendText("Could not establish a connection with the tool.\r\n");
				}
		}



		private void StatUpdate(string msg)

		{
			StatusTextBox.AppendText(msg + "\r\n");
			if (msg == "Run done")
				{
				StartButton.Enabled = true;
				PIDGroupBox.Enabled = true;
				}
		}



		private void DLVUpdate(string msg,string file)

		{
			ListViewItem item;
			string[] val;

			val = msg.Split(',');
			item = new ListViewItem(val[2]);
			item.SubItems.Add(PGNumericUpDown.Value.ToString());
			item.SubItems.Add(IGNumericUpDown.Value.ToString());
			item.SubItems.Add(DGNumericUpDown.Value.ToString());
			item.SubItems.Add(file);
			DataListView.Items.Add(item);
			item.Focused = true;
		}



		private void KeepAliveTimer(Object source,System.Timers.ElapsedEventArgs e)

		{
			if (keep_alive_active)
				{
				kaconnect.Send(UiConstants.KEEP_ALIVE,karcvr,false);
				}
		}



		private void PersonPIDCalForm_FormClosing(object sender, FormClosingEventArgs e)

		{
			if (katimer != null)
				{
				katimer.Enabled = false;
				katimer.Close();
				katimer = null;
				keep_alive_active = false;
				}
			if ((ts_feed != null) && ts_feed.IsAlive)
				{
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



		private string SendToolCommand(string msg,int timeout_count,bool log = true)


		{
			string rtn = "";

			tool_access.Wait();
			rtn = tconnect.SendCommand(msg,timeout_count,log);
			tool_access.Release();
			return(rtn);
		}



		private bool GetLastRunData(ref string name)

		{
			string rsp,fname;
			string[] val;
			int dslen,rlen;
			MemoryStream ms = new MemoryStream();
			StreamWriter tw;
			DateTime now = DateTime.Now;
			bool rtn = false;

			rsp = SendToolCommand(UiConstants.SEND_LAST_DS, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				rsp = tconnect.ReceiveResponse(100,true);
				if (rsp.StartsWith(UiConstants.LAST_SENSOR_DS))
					{
					val = rsp.Split(',');
					if (val.Length == 2)
						{
						tconnect.Send(UiConstants.OK);
						dslen = int.Parse(val[1]);
						ms = new MemoryStream();
						rlen = tconnect.ReceiveStream(ref ms,dslen);
						if (dslen == rlen)
							{
							fname = "Move to person data set " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + ".csv";
							tw = File.CreateText(Log.LogDir() + fname);
							if (tw != null)
								{
								ms.WriteTo(tw.BaseStream);
								tw.Close();
								Log.LogEntry("Data set saved: " + Log.LogDir() + fname);
								rtn = true;
								name = fname;
								}
							else
								{
								Log.LogEntry("Could not open file for data set.");
								}
							}
						else
							{
							Log.LogEntry("Incorrect stream length");
							}
						}
					else
						{
						tconnect.Send(UiConstants.FAIL);
						Log.LogEntry("Bad response format");
						}
					}
				else
					{
					this.BeginInvoke(rsupdate,"No data stream to receive");
					Log.LogEntry("No data stream to receive");
					}
				}
			return(rtn);
		}



		private void CalStatusReceive()

		{
			string msg,name = "";
			string[] val;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

			Log.LogEntry("Cal status receiver started.");
			while (ts_run)
				{

				try
				{
				msg = status_feed.ReceiveResponse(100, ref ep);
				if (msg.StartsWith(UiConstants.CAL_STATUS) && ts_run)
					{
					val = msg.Split(',');
					if (val.Length > 1)
						{
						switch(val[1])
							{
							case UiConstants.CAL_RUN_COMPLETED:
								if (GetLastRunData(ref name))
									{
									this.BeginInvoke(tupdate,msg,name);
									this.BeginInvoke(rsupdate,"Run complete");
									}
								else
									{
									this.BeginInvoke(rsupdate,"Run completed but could not download data set");
									}
								break;

							case UiConstants.CAL_RUN_DONE:
								this.BeginInvoke(rsupdate,"Run done");
								ts_run = false;
								break;

							case UiConstants.CAL_RUN_ABORTED:
								this.BeginInvoke(rsupdate,"Run aborted");
								ts_run = false;
								break;

							default:
								this.BeginInvoke(rsupdate,"Unknown message: " + msg);
								Log.LogEntry("CalStatusReceive, unknown message: " + msg);
								break;
							}
						}
					else
						{
						this.BeginInvoke(rsupdate,"Bad parameter format: " + msg);
						Log.LogEntry("CalStatusReceive, bad parameter format: " + msg);
						}
					}
				else if (!msg.StartsWith(UiConstants.FAIL))
					{
					this.BeginInvoke(rsupdate,"Unexpected message: " + msg);
					Log.LogEntry("CalStatusReceive, unexpected message: " + msg);
					}
				}

				catch (ThreadAbortException)
				{
				ts_run = false;
				}

				catch (Exception ex)
				{
				Log.LogEntry("CalStatusReceive exception: " + ex.Message);
				}

				}
			ts_feed = null;
			status_feed.Close();
			Log.LogEntry("Cal status receiver closed.");
		}



		private void StartButton_Click(object sender, EventArgs e)

		{
			string rsp;
			bool pid_ok = true;

			status_feed = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_FEED_PORT_NO, false);
			if (status_feed.Connected())
				{
				if (PPCheckBox.Checked)
					{
					rsp = SendToolCommand(UiConstants.UPDATE_PID_PARAM + "," + PGNumericUpDown.Value + "," + IGNumericUpDown.Value + "," + DGNumericUpDown.Value ,100,true);
					if (rsp.StartsWith(UiConstants.FAIL))
						{
						StatusTextBox.AppendText("Could not load PID parameters.\r\n");
						pid_ok = false;
						}
					}
				if (pid_ok)
					{
					rsp = SendToolCommand(UiConstants.START_PPID_CAL, 100, true);
					if (rsp.StartsWith(UiConstants.OK))
						{
						ts_run = true;
						ts_feed = new Thread(CalStatusReceive);
						ts_feed.Start();
						StartButton.Enabled = false;
						PIDGroupBox.Enabled = false;
						}
					else
						StatusTextBox.AppendText("Could not start PPID calibration");
					}
				}
		}



		private void DisplayMove(string fname)

		{
			TextReader tr;
			string line;
			string[] values;
			int et;
			double rel_angle = 0;
			bool start_detect = false;

			if (fname.Length > 0)
				{
				tr = File.OpenText(fname);
				if (tr != null)
					{
					ProfileChart.Series[0].Points.Clear();
					while ((line = tr.ReadLine()) != null)
						{
						values = line.Split(',');
						if ((values != null) && (values.Length > 1))
							{
							if (!start_detect)
								start_detect = true;
							else if (start_detect)
								{
								et = int.Parse(values[0]);
								rel_angle = double.Parse(values[1]);
								ProfileChart.Series[0].Points.AddXY(et,rel_angle);
								}
							}
						}
					tr.Close();
					}
				else
					StatUpdate("Could not open move file.");
				}
			else
				StatUpdate("No move file available.");
		}



		private void DataListView_SelectedIndexChanged(object sender, EventArgs e)

		{
			string fname;

			try
			{
			if (DataListView.SelectedIndices.Count > 0)
				{
				fname = Log.LogDir() + DataListView.SelectedItems[0].SubItems[4].Text;
				DisplayMove(fname);
				}
			}

			catch (Exception ex)
			{
			Log.LogEntry("Display move data exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}
		}


		}
	}
