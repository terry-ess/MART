using System;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;

namespace Sub_system_Operation
	{
	public partial class RoboticArmOp : UserControl
		{

		public RoboticArmOp()

		{
			InitializeComponent();
		}



		public void Open()

		{
			string rsp;

			StatusTextBox.Clear();
			rsp = SubSystemOpForm.SendToolCommand(UiConstants.ARM_STAT, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				StatusTextBox.AppendText("Robot connection opened.\r\n");
				}
			else
				StatusTextBox.AppendText("Arm is not operational.\r\n");
		}


		public void Close()

		{
			if (ParkButton.Enabled)
				SubSystemOpForm.SendToolCommand(UiConstants.ARM_TO_PARK,10000);
		}



		private void StartButton_Click(object sender, EventArgs e)

		{
			string rsp;

			rsp = SubSystemOpForm.SendToolCommand(UiConstants.ARM_TO_START,1000);
			if (rsp.StartsWith(UiConstants.OK))
				{
				StatusTextBox.AppendText("Arm is at start position.\r\n");
				StartButton.Enabled = false;
				ParkButton.Enabled = true;
				PosButton.Enabled = true;
				}
			else
				StatusTextBox.AppendText(rsp + "\r\n");
		}


		private void ParkButton_Click(object sender, EventArgs e)

		{
			string rsp;

			rsp = SubSystemOpForm.SendToolCommand(UiConstants.ARM_TO_PARK,10000);
			if (rsp.StartsWith(UiConstants.FAIL))
				StatusTextBox.AppendText(rsp + "\r\n");
			else
				{
				StatusTextBox.AppendText("Arm is parked.\r\n");
				StartButton.Enabled = true;
				ParkButton.Enabled = false;
				PosButton.Enabled = false;
				}
		}



		private void PosButton_Click(object sender, EventArgs e)

		{
			string rsp,cmd;

			cmd = UiConstants.RAW_ARM_TO_POSITION + "," + XNumericUpDown.Value + "," + YNumericUpDown.Value + "," + ZNumericUpDown.Value + "," + WIRadioButton.Checked + "," + RPRPRadioButton.Checked + "," + GORadioButton.Checked;
			rsp = SubSystemOpForm.SendToolCommand(cmd,10000);
			if (rsp.StartsWith(UiConstants.FAIL))
				StatusTextBox.AppendText(rsp + "\r\n");
			else
				StatusTextBox.AppendText("Arm is positioned.\r\n");
		}

		}
	}
