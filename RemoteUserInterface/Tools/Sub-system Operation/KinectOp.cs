using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;


namespace Sub_system_Operation
	{
	public partial class KinectOp : UserControl
		{

		public static double video_vert_fov = 0;
		public static double video_hor_fov = 0;
		public static int video_frame_width = 0;
		public static int video_frame_height = 0;
		public static Bitmap blank;

		private int current_tab = 0;

		public KinectOp()

		{
			InitializeComponent();
		}



		public void Open()

		{
			string rsp;
			string[] val;
			int i, j;
			string fname,tool_name;
			DateTime fdt;

			tool_name = new AssemblyName(Assembly.GetCallingAssembly().FullName).Name;
			fname = Application.StartupPath + Folders.TOOLS_SUB_DIR + tool_name + ".dll";
			fdt = File.GetLastWriteTime(fname);
			AboutTextBox.AppendText("\r\nBuild date: " + fdt.ToShortDateString() + "  " + fdt.ToShortTimeString() + "\r\n");
			rsp = SubSystemOpForm.SendToolCommand(UiConstants.SEND_VIDEO_PARAM, 20);
			if (rsp.StartsWith(UiConstants.OK))
				{
				val = rsp.Split(',');
				if (val.Length == 5)
					{
					video_frame_width = int.Parse(val[1]);
					video_frame_height = int.Parse(val[2]);
					video_hor_fov = float.Parse(val[3]);
					video_vert_fov = float.Parse(val[4]);
					blank = new Bitmap(video_frame_width, video_frame_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
					for (i = 0; i < video_frame_width; i++)
						for (j = 0; j < video_frame_height; j++)
							blank.SetPixel(i, j, Color.White);
					current_tab = 0;
					}
				else
					{
					AboutTextBox.AppendText("\r\nVideo parameters response incorrect format.");
					this.Enabled = false;
					}
				}
			else
				{
				this.Enabled = false;
				AboutTextBox.AppendText("\r\nCould not get video parameters.");
				}
		}



		public void Close()

		{
			if (current_tab == 1)
				KinectDistCalCtl.Close();
			else if (current_tab == 2)
				KinectMappingCntl.Close();
			else if (current_tab == 3)
				TargetCalCntl.Close();
		}



		private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)

		{
			if (current_tab == 1)
				KinectDistCalCtl.Close();
			else if (current_tab == 2)
				KinectMappingCntl.Close();
			else if (current_tab == 3)
				TargetCalCntl.Close();
			if (tabControl1.SelectedIndex == 1)
				KinectDistCalCtl.Open();
			else if (tabControl1.SelectedIndex == 2)
				KinectMappingCntl.Open();
			else if (tabControl1.SelectedIndex == 3)
				TargetCalCntl.Open();
			current_tab = tabControl1.SelectedIndex;
		}

		}
	}
