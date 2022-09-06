using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;

namespace Turn_Calibration
	{
	public partial class TurnTestControl : UserControl
		{

		private Connection status_feed = null;
		private bool ts_run = false;
		private Thread ts_feed = null;
		private delegate void RunStatusUpdate(string status);
		private RunStatusUpdate rsupdate;


		public TurnTestControl()

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
			int samples;

			if (status.StartsWith(UiConstants.CAL_RUN_COMPLETED))
				{
				StatusTextBox.AppendText("Calibration run completed\r\n");
				StartButton.Enabled = true;
				StopButton.Enabled = false;
				SDButton.Enabled = true;
				CDButton.Enabled = true;
				}
			else if (status.StartsWith(UiConstants.CAL_RUN_ABORTED))
				{
				StatusTextBox.AppendText(status + "\r\n");
				StartButton.Enabled = true;
				StopButton.Enabled = false;
				if (DataListView.Items.Count > 0)
					{
					SDButton.Enabled = true;
					CDButton.Enabled = true;
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
					item.SubItems.Add(TANumericUpDown.Value.ToString());
					item.SubItems.Add(val[6]);
					item.SubItems.Add(val[7]);
					DataListView.Items.Add(item);
					item.Focused = true;
					CDButton.Enabled = true;
					samples = int.Parse(STextBox.Text);
					samples += 1;
					STextBox.Text = samples.ToString();
					}
				else if (val.Length == 1)
					StatusTextBox.AppendText(val[0] + "\r\n");
				}
		}



		private void GetLastTurnSensorData()

		{
			string rsp,fname;
			string[] val;
			int dslen,rlen;
			MemoryStream ms = new MemoryStream();
			StreamWriter tw;
			DateTime now = DateTime.Now;

			rsp = TurnCalForm.SendToolCommand(UiConstants.SEND_LAST_DS, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				rsp = TurnCalForm.tconnect.ReceiveResponse(100,true);
				if (rsp.StartsWith(UiConstants.LAST_SENSOR_DS))
					{
					val = rsp.Split(',');
					if (val.Length == 2)
						{
						TurnCalForm.tconnect.Send(UiConstants.OK);
						dslen = int.Parse(val[1]);
						ms = new MemoryStream();
						rlen = TurnCalForm.tconnect.ReceiveStream(ref ms,dslen);
						if (dslen == rlen)
							{
							fname = Log.LogDir() + "Turn sensor data set " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + ".csv";
							tw = File.CreateText(fname);
							if (tw != null)
								{
								ms.WriteTo(tw.BaseStream);
								tw.Close();
								this.BeginInvoke(rsupdate, "Sensor data set saved: " + fname);
								Log.LogEntry("Sensor data set saved: " + fname);
								}
							else
								{
								this.BeginInvoke(rsupdate, "Could not open file for sensor data");
								Log.LogEntry("Could not open file for sensor data.");
								}
							}
						else
							{
							this.BeginInvoke(rsupdate, "Incorrect video stream length");
							Log.LogEntry("incorrect video stream length");
							}
						}
					else
						{
						TurnCalForm.tconnect.Send(UiConstants.FAIL);
						this.BeginInvoke(rsupdate, "Incorrect response format");
						Log.LogEntry("bad response format");
						}
					}
				else
					{
					this.BeginInvoke(rsupdate,"No data stream to receive");
					Log.LogEntry("no data stream to receive");
					}
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
						{
						this.BeginInvoke(rsupdate, msg.Substring(val[0].Length + 1));
						if (val[1] == UiConstants.CAL_RUN_COMPLETED && LSDCheckBox.Checked)
							GetLastTurnSensorData();
						}
					else if ((val.Length > 2) && (val[1] == UiConstants.CAL_RUN_ABORTED))
						{
						this.BeginInvoke(rsupdate, msg.Substring(val[0].Length + 1));
						}
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

			StatusTextBox.Clear();
			msg = UiConstants.RUN_TT + "," + NCNumericUpDown.Value + "," + TANumericUpDown.Value;
			rsp = TurnCalForm.SendToolCommand(msg,100);
			if (!rsp.StartsWith(UiConstants.OK))
				 StatusTextBox.AppendText("Could not run test: " + rsp + "\r\n");
			else
				{
				StartButton.Enabled = false;
				StopButton.Enabled = true;
				SDButton.Enabled = false;
				CDButton.Enabled = false;
				}
		}



		private void ClearDataButton_Click(object sender, EventArgs e)

		{
			DataListView.Items.Clear();
			CDButton.Enabled = false;
			STextBox.Text = "0";
		}



		private void SaveData()
			
		{
			string fname;
			TextWriter dstw = null;
			int i,j,dlen;

			fname = Log.LogDir();
			dlen = fname.Length;
			fname += "Turn test data set " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + ".csv";
			dstw = File.CreateText(fname);
			if (dstw != null)
				{
				dstw.WriteLine("Turn test data set");
				dstw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
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
				SDButton.Enabled = false;
				}
		}



		private void SaveDataButton_Click(object sender, EventArgs e)

		{
			if (DataListView.Items.Count > 0)
				SaveData();
		}



		private void StopButton_Click(object sender, EventArgs e)

		{
			TurnCalForm.SendToolCommand(UiConstants.STOP_TT, 100);
			StopButton.Enabled = false;
			StartButton.Enabled = true;
			if (DataListView.Items.Count > 0)
				{
				SDButton.Enabled = true;
				CDButton.Enabled = true;
				}
		}

		}
	}
