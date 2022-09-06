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
	public partial class BasicParamControl : UserControl
		{

		private Connection status_feed = null;
		private bool ts_run = false;
		private Thread ts_feed = null;
		private delegate void RunStatusUpdate(string status);
		private RunStatusUpdate rsupdate;
		private bool run_saved = false;


		public BasicParamControl()

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
				ts_feed = new Thread(CalStatusReceive);
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
				StatusTextBox.AppendText("Calibration run completed\r\n");
				StartButton.Enabled = true;
				StopButton.Enabled = false;
				SaveDataButton.Enabled = true;
				}
			else if (status.StartsWith(UiConstants.CAL_RUN_ABORTED))
				{
				StatusTextBox.AppendText(status + "\r\n");
				StartButton.Enabled = true;
				StopButton.Enabled = false;
				if (DataListView.Items.Count > 0)
					{
					SaveDataButton.Enabled = true;
					}
				}
			else
				{
				val = status.Split(',');
				if (val.Length == 8)
					{
					item = new ListViewItem(val[0]);
					item.SubItems.Add(val[1]);
					item.SubItems.Add(val[2]);
					item.SubItems.Add(val[3]);
					item.SubItems.Add(val[4]);
					item.SubItems.Add(val[5]);
					item.SubItems.Add(val[6]);
					item.SubItems.Add(val[7]);
					DataListView.Items.Add(item);
					item.Focused = true;
					}
				else if (val.Length == 1)
					StatusTextBox.AppendText(val[0] + "\r\n");
				}
		}



		private void CalStatusReceive()

		{
			string msg;
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
					if (val.Length == 2)
						this.BeginInvoke(rsupdate, msg.Substring(val[0].Length + 1));
					else if ((val.Length > 2) && ((val[1] == UiConstants.CAL_RUN_COMPLETED) || (val[1] == UiConstants.CAL_RUN_ABORTED)))
						this.BeginInvoke(rsupdate, msg.Substring(val[0].Length + 1));
					else if (val.Length == 9)
						this.BeginInvoke(rsupdate, msg.Substring(val[0].Length + 1));
					else
						Log.LogEntry("CalStatusReceive, bad parameter format: " + msg);
					}
				else if (!msg.StartsWith(UiConstants.FAIL))
					Log.LogEntry("CalStatusReceive, unexpected message: " + msg);
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
			Log.LogEntry("Cal status receiver closed.");
		}



		private void StartButton_Click(object sender, EventArgs e)

		{
			string msg,rsp;
			DialogResult dr;

			StatusTextBox.Clear();
			msg = UiConstants.RUN_BLM_CAL + "," + NCNumericUpDown.Value + "," + MTNumericUpDown.Value + "," + WACheckBox.Checked.ToString() + "," + AccelNumericUpDown.Value + "," + MSNumericUpDown.Value;
			if (PPCheckBox.Checked)
				{
				msg += "," + ((double) PGNumericUpDown.Value) + ","  + ((double) IGNumericUpDown.Value) + "," + ((double) DGNumericUpDown.Value);
				}
			rsp = LinearMtnCalForm.SendToolCommand(msg,100);
			if (!rsp.StartsWith(UiConstants.OK))
				 StatusTextBox.AppendText("Could not run calibration: " + rsp + "\r\n");
			else
				{
				if (!run_saved)
					{
					dr = MessageBox.Show("You have not saved the last test run.  Do you want to save it?","Questions",MessageBoxButtons.YesNo);
					if (dr == DialogResult.Yes)
						SaveData();
					}
				DataListView.Items.Clear();
				run_saved = false;
				StartButton.Enabled = false;
				StopButton.Enabled = true;
				SaveDataButton.Enabled = false;
				}
		}






		private void SaveData()
			
		{
			string fname;
			TextWriter dstw = null;
			int i,j,dlen;

			fname = Log.LogDir();
			dlen = fname.Length;
			fname += "Basic linear motion parameter data set " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + ".csv";
			dstw = File.CreateText(fname);
			if (dstw != null)
				{
				dstw.WriteLine("Basic linear motion parameter data set");
				dstw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
				dstw.WriteLine();
				dstw.WriteLine("Move time (sec): " + MTNumericUpDown.Value);
				dstw.WriteLine("Wheel align: " + WACheckBox.Checked);
				dstw.WriteLine("Acceleration factor: " + AccelNumericUpDown.Value);
				dstw.WriteLine("Max speed factor: " + MSNumericUpDown.Value);
				if (PPCheckBox.Checked)
					{
					dstw.WriteLine("Proportional gain: " + PGNumericUpDown.Value);
					dstw.WriteLine("Integral gain: " + IGNumericUpDown.Value);
					dstw.WriteLine("Differential gain: " + DGNumericUpDown.Value);
					}
				else
					dstw.WriteLine("PID parameters not set remotely");
				dstw.WriteLine();
				for (i = 0;i < DataListView.Columns.Count;i++)
					dstw.Write(DataListView.Columns[i].Text + ",");
				dstw.WriteLine();
				for (i = 0;i < DataListView.Items.Count;i++)
					{
					for (j = 0;j < DataListView.Columns.Count;j++)
						dstw.Write(DataListView.Items[i].SubItems[j].Text + ",");
					dstw.WriteLine();
					}
				dstw.WriteLine();
				dstw.Close();
				Log.LogEntry("Saved " + fname);
				SaveDataButton.Enabled = false;
				run_saved = true;
				}
		}



		private void SaveDataButton_Click(object sender, EventArgs e)

		{
			if (DataListView.Items.Count > 0)
				SaveData();
		}



		private void StopButton_Click(object sender, EventArgs e)

		{
			LinearMtnCalForm.SendToolCommand(UiConstants.STOP_BLM_CAL, 100);
			StopButton.Enabled = false;
			StartButton.Enabled = true;
			if (DataListView.Items.Count > 0)
				{
				SaveDataButton.Enabled = true;
				}
		}

		}
	}
