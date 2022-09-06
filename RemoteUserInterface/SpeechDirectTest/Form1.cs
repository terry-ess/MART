using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using AutoRobotControl;

namespace SpeechDirectTest
	{
	public partial class Form1 : Form
		{
		
		public static Pipe test_pipe = new Pipe();

		private bool run = false;
		private Thread pdt,sdqt;

		public Form1()
		
		{
			string dir;

			InitializeComponent();
			dir = Application.StartupPath + SharedData.DATA_SUB_DIR;
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			Log.OpenLog("Operation log " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second + SharedData.TEXT_TILE_EXT, true);
			test_pipe.Open();
		}



		private void PipeDequeueThread()

		{
			string msg;

			try
			{
			while (run)
				{
				if ((msg = test_pipe.Remove()).Length > 0)
					{
					StatusTextBox.AppendText(msg + "\r\n");
					}
				Thread.Sleep(50);
				}
			}

			catch
			{ }

			pdt = null;
		}



		private void SpeechDirectQueueThread()

		{
			int direct,last_direct = -1;

			try
			{
			while (run)
				{
				direct = SpeechDirection.GetSpeechDirection();
				if (direct != last_direct)
					{
					test_pipe.Add(direct.ToString());
					last_direct = direct;
					}
				}
			}

			catch
			{ }

			sdqt = null;
		}



		private void Form1_FormClosing(object sender, FormClosingEventArgs e)

		{
			if (SSButton.Text == "Stop")
				SSButton_Click(null,null);
		}




		private void SaveButton_Click(object sender, EventArgs e)
		
		{
			string fname;
			TextWriter tw;
			int i;

			fname = Log.LogDir() + "Speech direction test.txt";
			tw = File.CreateText(fname);
			if (tw != null)
				{
				for (i = 0;i < StatusTextBox.Lines.Length;i++)
					tw.WriteLine(StatusTextBox.Lines[i]);
				tw.Close();
				}
		}




		private void SSButton_Click(object sender, EventArgs e)

		{
			if (SSButton.Text == "Start")
				{
				if (Relays.Open())
					{
					if (Relays.SSRelay(false))
						{
						SSButton.Enabled = false;
						StatusTextBox.AppendText("Starting Kinect\r\n");
						Kinect.Open();
						StatusTextBox.AppendText("Starting Speech recognition\r\n");
						Speech.StartSpeechCommandHandlers();
						Speech.StartSpeechRecognition();
						StatusTextBox.AppendText("Starting head assembly\r\n");
						HeadAssembly.Open();
						//VisualObjectDetection.Open()
						StatusTextBox.AppendText("Waiting for RPI to initialize.\r\n");
						Thread.Sleep(30000);
						StatusTextBox.AppendText("Starting speech direction serve\r\n");
						SpeechDirection.Open();
						run = true;
						sdqt = new Thread(SpeechDirectQueueThread);
						sdqt.Start();
						pdt = new Thread(PipeDequeueThread);
						pdt.Start();
						SSButton.Text = "Stop";
						SSButton.Enabled = true;
						}
					}
				}
			else
				{
				run = false;
				Kinect.Close();
				Speech.StopSpeechRecognition();
				SpeechDirection.Close();
				Thread.Sleep(1000);
				Relays.SSRelay(true);
				if ((sdqt != null) && sdqt.IsAlive)
					sdqt.Join();
				if ((pdt != null) && pdt.IsAlive)
					pdt.Join();
				SSButton.Text = "Start";
				}
		}

		}
	}
