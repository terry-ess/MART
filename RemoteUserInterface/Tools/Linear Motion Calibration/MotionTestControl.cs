using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;

namespace Linear_Motion_Calibration
	{
	public partial class MotionTestControl : UserControl
		{
		private Connection status_feed = null;
		private bool ts_run = false;
		private Thread ts_feed = null;
		private delegate void RunStatusUpdate(string status);
		private RunStatusUpdate rsupdate;


		public MotionTestControl()

		{
			InitializeComponent();
			rsupdate = StatusUpdate;
		}


		public void Open()

		{
			status_feed = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_FEED_PORT_NO, false);
			if (status_feed.Connected())
				{
				ts_run = true;
				ts_feed = new Thread(TestStatusReceive);
				ts_feed.Start();
				}
			else
				{
				StatusTextBox.AppendText("Could not open a status feed connection.");
				this.Enabled = false;
				}
			}



		public void Close()

		{
			if (StopButton.Enabled)
				StopButton_Click(null,null);
			if ((ts_feed != null) && (ts_feed.IsAlive))
				{
				ts_run = false;
				ts_feed.Abort();
				}
			if ((status_feed != null) && status_feed.Connected())
				status_feed.Close();
		}



		private void StatusUpdate(string status)

		{
			string[] val;
			ListViewItem item;

			if (status.StartsWith(UiConstants.CAL_RUN_COMPLETED))
				{
				StatusTextBox.AppendText("Test run completed\r\n");
				StartButton.Enabled = true;
				StopButton.Enabled = false;
				SaveDataButton.Enabled = true;
				ClearDataButton.Enabled = true;
				}
			else if (status.StartsWith(UiConstants.CAL_RUN_ABORTED))
				{
				StatusTextBox.AppendText(status + "\r\n");
				StartButton.Enabled = true;
				StopButton.Enabled = false;
				if (DataListView.Items.Count > 0)
					{
					SaveDataButton.Enabled = true;
					ClearDataButton.Enabled = true;
					}
				}
			else
				{
				val = status.Split(',');
				if (val.Length == 3)
					{
					item = new ListViewItem(val[0]);
					if (WACheckBox.Checked)
						item.SubItems.Add("Y");
					else
						item.SubItems.Add("N");
					if (SlowCheckBox.Checked)
						item.SubItems.Add("Y");
					else
						item.SubItems.Add("N");
					item.SubItems.Add(val[1]);
					item.SubItems.Add(MDNumericUpDown.Value.ToString());
					item.SubItems.Add(val[2]);
					DataListView.Items.Add(item);
					item.Focused = true;
					ClearDataButton.Enabled = true;
					}
				else if (val.Length == 1)
					StatusTextBox.AppendText(val[0] + "\r\n");
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
				msg = status_feed.ReceiveResponse(100, ref ep);
				if (msg.StartsWith(UiConstants.CAL_STATUS) && ts_run)
					{
					val = msg.Split(',');
					if (val.Length == 2)
						this.BeginInvoke(rsupdate, msg.Substring(val[0].Length + 1));
					else if ((val.Length > 2) && ((val[1] == UiConstants.CAL_RUN_COMPLETED) || (val[1] == UiConstants.CAL_RUN_ABORTED)))
						this.BeginInvoke(rsupdate, msg.Substring(val[0].Length + 1));
					else if (val.Length == 4)
						this.BeginInvoke(rsupdate, msg.Substring(val[0].Length + 1));
					else
						Log.LogEntry("TestStatusReceive, bad parameter format: " + msg);
					}
				else if (!msg.StartsWith(UiConstants.FAIL))
					Log.LogEntry("TestStatusReceive, unexpected message: " + msg);
				}

				catch (ThreadAbortException)
				{
				ts_run = false;
				}

				catch (Exception ex)
				{
				Log.LogEntry("TestStatusReceive exception: " + ex.Message);
				}

				}
			ts_feed = null;
			Log.LogEntry("Test status receiver closed.");
		}



		private void StartButton_Click(object sender, EventArgs e)

		{
			string msg,rsp;

			StatusTextBox.Clear();
			msg = UiConstants.RUN_MT + "," + NCNumericUpDown.Value + "," + MDNumericUpDown.Value + "," + WACheckBox.Checked.ToString() + "," + SlowCheckBox.Checked.ToString();
			rsp = LinearMtnCalForm.SendToolCommand(msg,100);
			if (!rsp.StartsWith(UiConstants.OK))
				 StatusTextBox.AppendText("Could not run test: " + rsp + "\r\n");
			else
				{
				StartButton.Enabled = false;
				StopButton.Enabled = true;
				SaveDataButton.Enabled = false;
				ClearDataButton.Enabled = false;
				}
		}



		private void ClearDataButton_Click(object sender, EventArgs e)

		{
			DataListView.Items.Clear();
			ClearDataButton.Enabled = false;
		}



		private void SaveData()
			
		{
			string fname;
			TextWriter dstw = null;
			int i,j,dlen;

			fname = Log.LogDir();
			dlen = fname.Length;
			fname += "Linear motion test data set " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + ".csv";
			dstw = File.CreateText(fname);
			if (dstw != null)
				{
				dstw.WriteLine("Linear motion test data set");
				dstw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				dstw.WriteLine();
				for (i = 0;i < DataListView.Columns.Count;i++)
					{
					if (i == 0)
						dstw.Write(DataListView.Columns[i].Text);
					else
						dstw.Write("," + DataListView.Columns[i].Text);
					}
				dstw.WriteLine();
				for (i = 0;i < DataListView.Items.Count;i++)
					{
					for (j = 0;j < DataListView.Columns.Count;j++)
						{
						if (j == 0)
							dstw.Write(DataListView.Items[i].SubItems[j].Text);
						else
							dstw.Write("," + DataListView.Items[i].SubItems[j].Text);
						}
					dstw.WriteLine();
					}
				dstw.WriteLine();
				dstw.Close();
				Log.LogEntry("Saved " + fname);
				SaveDataButton.Enabled = false;
				}
		}



		private void SaveDataButton_Click(object sender, EventArgs e)

		{
			if (DataListView.Items.Count > 0)
				SaveData();
		}



		private void StopButton_Click(object sender, EventArgs e)

		{
			LinearMtnCalForm.SendToolCommand(UiConstants.STOP_MT, 100);
			StopButton.Enabled = false;
			StartButton.Enabled = true;
			if (DataListView.Items.Count > 0)
				{
				SaveDataButton.Enabled = true;
				ClearDataButton.Enabled = true;
				}
		}

		}
	}
