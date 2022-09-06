using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using RobotConnection;
using Constants;

namespace RemoteUserInterface
	{
	public partial class MainForm : Form
		{

		public static bool in_tool = false;

		private Form tool = null;
		public delegate void ToolsEnableUpdate(bool enable);
		private ToolsEnableUpdate teu;
		public delegate void RemoteIntfInvoke(string name);
		private RemoteIntfInvoke rii;
		private string tool_file_name,tool_type_name;


		public MainForm()

		{
			string fname,name;
			DateTime dt = DateTime.Now,fdt;

			InitializeComponent();
			name = new AssemblyName(Assembly.GetCallingAssembly().FullName).Name;
			fname = Application.StartupPath + "\\" + name + ".exe";
			fdt = File.GetLastWriteTime(fname);
			this.Text = this.Text + "  " + fdt.ToShortDateString() + "  " + fdt.ToShortTimeString();
			AddTools();
			fname = "RUI Operations log " + dt.Month + "." + dt.Day + "." + dt.Year + " " + dt.Hour + "." + dt.Minute + "." + dt.Second + ".log";
			Log.OpenLog(fname,true);
			ToolsFlowLayoutPanel.Enabled = false;
			teu = SetToolsEnable;
			rii = InvokeRemoteInterface;
			MainCntrl.Open(this);
		}



		public void SetToolsEnable(bool enable)

		{
			if (this.InvokeRequired)
				this.BeginInvoke(teu,enable);
			else
				{
				if (enable != ToolsFlowLayoutPanel.Enabled)
					ToolsFlowLayoutPanel.Enabled = enable;
				}
		}



		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)

		{
			DialogResult rtn;

			if (MainCntrl.RobotOpStatus() != MainControl.RobotStatus.SHUTTING_DOWN.ToString())
				{
				rtn = MessageBox.Show("The robot is not shutting down. Continue?","Query",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
				if (rtn == DialogResult.No)
					e.Cancel = true;
				else
					MainCntrl.Close(true);
				}
			else
				MainCntrl.Close(true);
		}



		private void AddTools()

		{
			string fname;
			string[] files;
			int i;
			Button b;

			fname = Application.StartupPath + Folders.TOOLS_SUB_DIR;
			files = Directory.GetFiles(fname, "*.dll");
			for (i = 0;i < files.Length;i++)
				{
				b = new Button();
				b.Name = "button" + i;
				b.Text = files[i].Substring(fname.Length, files[i].Length - fname.Length - 4);
				b.Height = 28;
				b.Width = 10 * b.Text.Length;
				b.FlatStyle = FlatStyle.Flat;
				ToolsFlowLayoutPanel.Controls.Add(b);
				b.Click += Tools_Click;
				}
		}



		private void ShowTool()

		{
			try 
			{
			Assembly DLL = Assembly.LoadFrom(tool_file_name);
			Type ctype = DLL.GetType(tool_type_name);
			dynamic c = Activator.CreateInstance(ctype);
			tool = c.Open();
			if ((tool != null) && tool.Enabled)
				{
				tool.ShowDialog();
				tool.Dispose();
				tool = null;
				}
			else
				{
				MessageBox.Show("Attempt to open " + tool_file_name + " failed.","Error");
				if (tool != null)
					{
					tool.Dispose();
					tool = null;
					}
				}
			}

			catch(Exception ex)
			{
			Log.LogEntry("ShowTool exception for " + tool_file_name + ": " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			if (tool != null)
				{
				tool.Dispose();
				tool = null;
				}
			MessageBox.Show("Attempt to open " + tool_file_name + " failed.","Error");
			}

			SetToolsEnable(true);
			in_tool = false;
		}



		public void InvokeRemoteInterface(string name)

		{
			if (this.InvokeRequired)
				this.BeginInvoke(rii, name);
			else
				{
				if (name == UiConstants.ARM_MANUAL_MODE)
					{
					tool_file_name = Application.StartupPath + Folders.SKILL_REMOTE_INTF_DIR + "Arm Manual Mode.dll";
					if (File.Exists(tool_file_name))
						{
						in_tool = true;
						SetToolsEnable(false);
						Thread show_tool = new Thread(ShowTool);
						tool_type_name = "Arm_Manual_Mode.Tool";
						show_tool.Start();
						}
					}
				}
		}



		private void Tools_Click(object sender, EventArgs e)

		{
			string type_name;

			if (!in_tool)
				{
				in_tool = true;
				SetToolsEnable(false);
				Thread show_tool = new Thread(ShowTool);
				tool_file_name = Application.StartupPath + Folders.TOOLS_SUB_DIR + ((Button) sender).Text + ".dll";
				type_name = ((Button)sender).Text.Replace(' ', '_');
				type_name = type_name.Replace('-','_');
				tool_type_name = type_name + ".Tool";
				show_tool.Start();
				}
		}


		}
	}
