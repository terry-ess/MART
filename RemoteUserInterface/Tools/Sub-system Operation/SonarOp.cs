using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;


namespace Sub_system_Operation
	{
	public partial class SonarOp : UserControl
		{
		private Connection sensor_feed = null;
		private bool sf_run = false;
		private Thread sf_feed = null;
		private delegate void SonarDataUpdate(string msg);
		private SonarDataUpdate sdupdate;


		public SonarOp()

		{
			InitializeComponent();
		}



		public void Open()

		{
			StatusTextBox.Clear();
			sensor_feed = new Connection(MainControl.robot_ip_address, UiConstants.TOOL_FEED_PORT_NO, false);
			if (sensor_feed.Connected())
				{
				sdupdate = DataUpdate;
				StatusTextBox.AppendText("Data feed connection opened.\r\n");
				}
			else
				{
				StatusTextBox.AppendText("Could not connect data feed.\r\n");
				}
		}



		public void Close()

		{
			if ((sf_feed != null) && sf_feed.IsAlive)
				{
				sf_run = false;
				SubSystemOpForm.SendToolCommand(UiConstants.STOP_RECORD, 100);
				sf_feed.Join();
				}
			if (sensor_feed != null)
				sensor_feed.Close();
		}



		private void DataUpdate(string msg)

		{
			TextReader tr;
			string line;
			string[] values;
			int x,y;

			StatusTextBox.AppendText(msg + "\r\n");
			if (msg.StartsWith("Sonar data set saved: "))
				{
				tr = File.OpenText(msg.Substring(22));
				if (tr  != null)
					{
					while ((line = tr.ReadLine()) != null)
						{
						values = line.Split(',');
						if (values.Length == 2)
							{
							try
							{
							x = int.Parse(values[0]);
							y = int.Parse(values[1]);
							ProfileChart.Series[0].Points.AddXY(x,y);
							}

							catch(Exception)
							{
							}

							}
						}
					tr.Close();
					StatusTextBox.AppendText("Recorded data displayed below.\r\n");
					}
				else
					StatusTextBox.AppendText("Attempt to display recorded data set failed.\r\n");
				}
			RecButton.Enabled = true;
		}



		private void GetLastRecordedData()

		{
			string rsp,fname = "";
			string[] val;
			int dslen,rlen;
			MemoryStream ms = new MemoryStream();
			StreamWriter tw;
			DateTime now = DateTime.Now;

			rsp = SubSystemOpForm.SendToolCommand(UiConstants.SEND_LAST_DS, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				rsp = SubSystemOpForm.tconnect.ReceiveResponse(100,true);
				if (rsp.StartsWith(UiConstants.LAST_SENSOR_DS))
					{
					val = rsp.Split(',');
					if (val.Length == 2)
						{
						SubSystemOpForm.tconnect.Send(UiConstants.OK);
						dslen = int.Parse(val[1]);
						ms = new MemoryStream();
						rlen = SubSystemOpForm.tconnect.ReceiveStream(ref ms,dslen);
						if (dslen == rlen)
							{
							fname = Log.LogDir() + "Sonar data set " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + ".csv";
							tw = File.CreateText(fname);
							if (tw != null)
								{
								ms.WriteTo(tw.BaseStream);
								tw.Close();
								this.BeginInvoke(sdupdate, "Sonar data set saved: " + fname);
								Log.LogEntry("Sonar data set saved: " + fname);
								}
							else
								{
								this.BeginInvoke(sdupdate, "Could not open file for sensor data");
								Log.LogEntry("Could not open file for sensor data.");
								}
							}
						else
							{
							this.BeginInvoke(sdupdate, "Incorrect data stream length");
							Log.LogEntry("incorrect data stream length");
							}
						}
					else
						{
						SubSystemOpForm.tconnect.Send(UiConstants.FAIL);
						this.BeginInvoke(sdupdate, "Incorrect response format");
						Log.LogEntry("bad response format");
						}
					}
				else
					{
					this.BeginInvoke(sdupdate,"No data stream to receive");
					Log.LogEntry("no data stream to receive");
					}
				}
		}



		private void SonarDataReceive()

		{
			string msg;
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

			Log.LogEntry("Sonar data receiver started.");
			while (sf_run)	
				{

				try
				{
				msg = sensor_feed.ReceiveResponse(20,ref ep);
				if (msg.StartsWith(UiConstants.SONAR_DATA_READY) && sf_run)
					{
					GetLastRecordedData();
					}
				else if (!msg.StartsWith(UiConstants.FAIL) && sf_run)
					Log.LogEntry("SonarDataReceive, unexpected message: " + msg);
				}

				catch(ThreadAbortException)
				{
				sf_run = false;
				}

				catch(Exception ex)
				{
				Log.LogEntry("SonarDataReceive exception: " + ex.Message);
				}

				}
			sf_feed = null;
			Log.LogEntry("Sonar data receiver closed.");
		}



		private void RecButton_Click(object sender, EventArgs e)
			
		{
			string msg,rsp;

			if (FRadioButton.Checked)
				msg = UiConstants.RECORD_FRONT_SONAR + "," + RTNumericUpDown.Value;
			else
				msg = UiConstants.RECORD_REAR_SONAR + "," + RTNumericUpDown.Value;
			rsp = SubSystemOpForm.SendToolCommand(msg, 100);
			if (!rsp.StartsWith(UiConstants.OK))
				StatusTextBox.AppendText("Could not start recording: " + rsp + "\r\n");
			else
				{
				StatusTextBox.AppendText("Recording started.\r\n");
				ProfileChart.Series[0].Points.Clear();
				RecButton.Enabled = false;
				}
		}

		}
	}
