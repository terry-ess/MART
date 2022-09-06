using System;
using System.Net;
using System.Windows.Forms;

namespace RemoteUserInterface
	{
	public partial class LoginForm : Form
		{

		public string password = "";
		public string user = "";
		public string host = "";
		public string run_ab_cmd = "";
		public string run_ovs_cmd = "";
		public string robot_ip_address = "";


		public LoginForm()

		{
			InitializeComponent();
		}



		private void DoneButton_Click(object sender, EventArgs e)

		{
			IPAddress[] ipa;

			if ((PassTextBox.Text.Length > 0) && (UserTextBox.Text.Length > 0) && (HostTextBox.Text.Length > 0))
				{
				password = PassTextBox.Text;
				user = UserTextBox.Text;
				host = HostTextBox.Text;

				try
				{
				ipa = Dns.GetHostAddresses(host);
				foreach(IPAddress ip in ipa)
					{
					if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
						{
						robot_ip_address = ip.ToString();
						break;
						}
					}
				}

				catch(Exception ex)
				{
				robot_ip_address = "";
				MessageBox.Show("Exception: " + ex.Message,"Error");
				}

				if (robot_ip_address.Length > 0)
					{
					if (host == "THE16")
						{
						run_ab_cmd = "/cygdrive/d/Workspace/Robot/AutoRobot/AutoRobot/AutoRobotControl.exe";
						this.DialogResult = DialogResult.OK;
						}
					else
						{
						this.DialogResult = DialogResult.Cancel;
						MessageBox.Show("Unknown host","Error");
						}
					}
				else
					{
					this.DialogResult = DialogResult.Cancel;
					MessageBox.Show("Could not resolve " + host + "'s IP address.","Error");
					}
				}
			else
				{
				this.DialogResult = DialogResult.Cancel;
				MessageBox.Show("All parameters must be entered.", "Error");
				}
			this.Close();
		}

		}
	}
