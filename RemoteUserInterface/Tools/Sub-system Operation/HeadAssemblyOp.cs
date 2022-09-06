using System;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;


namespace Sub_system_Operation
	{

	public partial class HeadAssemblyOp : UserControl

	{
		public enum HA_STATUS { FAIL, CONNECT_NO_SERVOS, CONNECT };

		private HA_STATUS has = HA_STATUS.FAIL;


		public HeadAssemblyOp()

		{
			InitializeComponent();
		}



		public void Open()

		{
			string rsp;
			string[] val;

			ErrStatTextBox.Clear();
			StatusTextBox.Clear();
			rsp = SubSystemOpForm.SendToolCommand(UiConstants.HA_STAT, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				StatusTextBox.AppendText("Robot connection opened.\r\n");
				val = rsp.Split(',');
				if (val.Length == 2)
					{
					if (val[1] == HA_STATUS.CONNECT.ToString())
						{
						has = HA_STATUS.CONNECT;
						ServoGroupBox.Enabled = true;
						MHGroupBox.Enabled = true;
						LAGroupBox.Enabled = true;
						StatusTextBox.AppendText("Head assembly is fully operational.\r\n");
						PanTiltUpdate();
						GHButton_Click(null, null);
						GLAButton_Click(null, null);
						}
					else if (val[1] == HA_STATUS.CONNECT_NO_SERVOS.ToString())
						{
						has = HA_STATUS.CONNECT_NO_SERVOS;
						ServoGroupBox.Enabled = false;
						MHGroupBox.Enabled = true;
						LAGroupBox.Enabled = true;
						StatusTextBox.AppendText("Head assembly is patially operational.\r\n");
						GHButton_Click(null,null);
						GLAButton_Click(null,null);
						}
					else if (val[1] == HA_STATUS.FAIL.ToString())
						{
						ServoGroupBox.Enabled = false;
						MHGroupBox.Enabled = false;
						LAGroupBox.Enabled = false;
						StatusTextBox.AppendText("Head assembly is not operational.\r\n");
						}
					}
				else
					{
					ServoGroupBox.Enabled = false;
					MHGroupBox.Enabled = false;
					LAGroupBox.Enabled = false;
					StatusTextBox.AppendText("Head assembly status response has bad format.\r\n");
					}
				}
			else
				{
				ServoGroupBox.Enabled = false;
				MHGroupBox.Enabled = false;
				LAGroupBox.Enabled = false;
				StatusTextBox.AppendText("Head assembly connection failed.\r\n");
				}
			}



		public void Close()

		{
			if (has == HA_STATUS.CONNECT)
				SubSystemOpForm.SendToolCommand(UiConstants.SET_PAN_TILT + ",0,0",10);
		}



		private bool PanTiltUpdate()

		{
			string rsp;
			string[] values;
			bool rtn = false;

			rsp = SubSystemOpForm.SendToolCommand(UiConstants.CURRENT_PAN, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 2)
					{
					PanNumericUpDown.Value = int.Parse(values[1]);
					rtn = true;
					}
				else
					{
					PanNumericUpDown.Value = 0;
					Log.LogEntry((UiConstants.CURRENT_PAN + " failed, bad reply format"));
					StatusTextBox.AppendText(UiConstants.CURRENT_PAN + " failed, bad reply format\r\n");
					}
				}
			else
				StatusTextBox.AppendText("Could not obtain current pan.\r\n");
			if (rtn)
				{
				rtn = false;
				rsp = SubSystemOpForm.SendToolCommand(UiConstants.CURRENT_TILT, 100);
				if (rsp.StartsWith(UiConstants.OK))
					{
					values = rsp.Split(',');
					if (values.Length == 2)
						{
						TiltNumericUpDown.Value = int.Parse(values[1]);
						rtn = true;
						}
					else
						{
						TiltNumericUpDown.Value = 0;
						Log.LogEntry((UiConstants.CURRENT_TILT + " failed, bad reply format"));
						StatusTextBox.AppendText(UiConstants.CURRENT_TILT + " failed, bad reply format\r\n");
						}
					}
				}
			else
				StatusTextBox.AppendText("Could not obtain current tilt.\r\n");
			return (rtn);
		}



		private void SPButton_Click(object sender, EventArgs e)

		{
			string rsp;
			string[] values;

			rsp = SubSystemOpForm.SendToolCommand(UiConstants.SET_PAN_TILT + "," + PanNumericUpDown.Text + "," + TiltNumericUpDown.Text,1000);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 3)	
					{
					PanNumericUpDown.Value = int.Parse(values[1]);
					TiltNumericUpDown.Value = int.Parse(values[2]);
					}
				else
					{
					PanNumericUpDown.Value = 0;
					TiltNumericUpDown.Value = 0;
					StatusTextBox.AppendText(UiConstants.SET_PAN_TILT + " incorrect response format\r\n");
					}
				}
			else
				{
				PanNumericUpDown.Value = 0;
				TiltNumericUpDown.Value = 0;
				StatusTextBox.AppendText(rsp + "\r\n");
				}
		}



		private void GHButton_Click(object sender, EventArgs e)

		{
			string rsp;
			string[] values;

			rsp = SubSystemOpForm.SendToolCommand(UiConstants.MAG_HEADING,1000);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 2)	
					{
					MHTextBox.Text = values[1];
					}
				else
					{
					MHTextBox.Clear();
					StatusTextBox.AppendText(UiConstants.MAG_HEADING + " incorrect response format\r\n");
					}
				}
			else
				{
				MHTextBox.Clear();
				StatusTextBox.AppendText(rsp + "\r\n");
				}
		}



		private void GLAButton_Click(object sender, EventArgs e)
		
		{
			string rsp;
			string[] values;

			rsp = SubSystemOpForm.SendToolCommand(UiConstants.LIGHT_AMP,1000);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 2)	
					{
					LATextBox.Text = values[1];
					}
				else
					{
					LATextBox.Clear();
					StatusTextBox.AppendText(UiConstants.LIGHT_AMP + " incorrect response format\r\n");
					}
				}
			else
				{
				LATextBox.Clear();
				StatusTextBox.AppendText(rsp + "\r\n");
				}
		}



		private void CEButton_Click(object sender, EventArgs e)

		{
			string rsp;

			rsp = SubSystemOpForm.SendToolCommand(UiConstants.CLEAR_ERR, 1000);
			if (rsp.StartsWith(UiConstants.FAIL))
				{
				StatusTextBox.AppendText(rsp + "\r\n");
				}
		}




		private void StatusButton_Click(object sender, EventArgs e)

		{
			string rsp;
			string[] values;

			rsp = SubSystemOpForm.SendToolCommand(UiConstants.SERVO_STAT, 1000);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 3)
					{
					ErrStatTextBox.Text = "pan: " + values[1] + "\r\ntilt: " + values[2];
					}
				else
					{
					ErrStatTextBox.Clear();
					StatusTextBox.AppendText(UiConstants.SERVO_STAT + " incorrect response format\r\n");
					}
				}
			else
				{
				ErrStatTextBox.Clear();
				StatusTextBox.AppendText(rsp + "\r\n");
				}
		}




		private void RestartButton_Click(object sender, EventArgs e)
		
		{
			string rsp;

			rsp = SubSystemOpForm.SendToolCommand(UiConstants.HA_RESTART,1000);
			if (rsp.StartsWith(UiConstants.FAIL))
				{
				StatusTextBox.AppendText(rsp + "\r\n");
				}
		}

		}
	}
