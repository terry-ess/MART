using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;


namespace Sub_system_Operation
	{
	public partial class KinectTargetCal : UserControl
		{
		public struct blue_filter
			{
			public int light_amplitude;
			public int intensity;
			public int blueness;
			};

		public struct target_data
			{
			public ArrayList blue_filters;
			public int min_blob_area;
			public int target_height;
			public int target_width;

			public target_data(int val)
				{
				blue_filters = new ArrayList();
				min_blob_area = val;
				target_height = val;
				target_width = val;
				}

			};

		private const string TARGET_CAL_FILE = "targetvision";
		private const string CAL_FILE_EXT = ".cal";


		private MemoryStream[] ms = new MemoryStream[2];
		private MemoryStream tms = new MemoryStream();
		private int indx = 0;
		private int light_amp = -1;
		private delegate void VideoUpdate(int indx);
		private VideoUpdate vu;
		private delegate void VUFail(string err);
		private VUFail vuf;
		private Point center_loc;
		private int area;
		private int width;
		private int height;
		private double ra;
		private double dist;
		private delegate void TVideoUpdate();
		private TVideoUpdate tvu;
		private delegate void TVUFail(string err);
		private TVUFail tvuf;
		private target_data td = new target_data(-1);


		public KinectTargetCal()

		{
			InitializeComponent();
			BPListView.ListViewItemSorter = new ListViewComparer();
		}



		public void Open()

		{
			ErrorTextBox.Clear();
			BluePicBox.Image = KinectOp.blank;
			VideoPicBox.Image = KinectOp.blank;
			ShootButton.Enabled = true;
			ProcButton.Enabled = false;
			vu = VidUpdate;
			vuf = VidUpdateFail;
			tvu = TVidUpdate;
			tvuf = TVidUpdateFail;
			ReadParameters();
		}



		public void Close()

		{

		}



		private bool ReadParameters(ref target_data td)

		{
			string fname,line;
			TextReader tr;
			bool rtn = false;
			string[] values;
			blue_filter bf = new blue_filter();

			fname = Application.StartupPath + Folders.CAL_SUB_DIR + TARGET_CAL_FILE + CAL_FILE_EXT;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				do
					{
					line = tr.ReadLine();
					values = line.Split(',');
					if (values.Length == 3)
						{
						bf.light_amplitude = int.Parse(values[0]);
						bf.intensity = int.Parse(values[1]);
						bf.blueness = int.Parse(values[2]);
						td.blue_filters.Add(bf);
						}
					}
				while (values.Length == 3);
				if (values.Length == 1)
					{
					td.min_blob_area = int.Parse(line);
					line = tr.ReadLine();
					td.target_width = int.Parse(line);
					line = tr.ReadLine();
					td.target_height = int.Parse(line);
					rtn = true;
					}
				tr.Close();
				LVButton.Enabled = true;
				}
			if (!rtn)
				Log.LogEntry("Could not read the target parameter file.");
			return(rtn);
		}



		private void ReadParameters()

		{
			int i;
			blue_filter bf;
			ListViewItem item;

			BPListView.Items.Clear();
			td.blue_filters.Clear();
			if (ReadParameters(ref td))
				{
				for (i = 0;i < td.blue_filters.Count;i++)
					{
					bf = (blue_filter) td.blue_filters[i];
					item = new ListViewItem(bf.light_amplitude.ToString());
					item.SubItems.Add(bf.intensity.ToString());
					item.SubItems.Add(bf.blueness.ToString());
					BPListView.Items.Add(item);
					}
				MBATextBox.Text = td.min_blob_area.ToString();
				THFTextBox.Text = td.target_height.ToString();
				TWFTextBox.Text = td.target_width.ToString();
				LVButton.Enabled = true;
				}
			else
				ErrorTextBox.AppendText("Could not read parameter file.\r\n");
		}



		private bool SaveParameters(ref target_data td)

		{
			bool rtn = false;
			string fname;
			TextWriter tw;
			int i;
			blue_filter bf;

			fname = Application.StartupPath + Folders.CAL_SUB_DIR + TARGET_CAL_FILE + CAL_FILE_EXT;
			if (File.Exists(fname))
				File.Move(fname,Application.StartupPath + Folders.CAL_SUB_DIR + TARGET_CAL_FILE + DateTime.Now.Ticks + CAL_FILE_EXT);
			tw = File.CreateText(fname);
			if (tw != null)
				{
				for (i = 0;i < td.blue_filters.Count;i++)
					{
					bf = (blue_filter) td.blue_filters[i];
					tw.WriteLine(bf.light_amplitude + "," + bf.intensity + "," + bf.blueness);
					}
				tw.WriteLine(td.min_blob_area);
				tw.WriteLine(td.target_width);
				tw.WriteLine(td.target_height);
				tw.Close();
				rtn = true;
				}
			return(rtn);
		}



		private void UpdateTargetData()

		{
			int i;
			blue_filter bf = new blue_filter();
			ListViewItem item;

			td = new target_data(-1);
			td.min_blob_area = int.Parse(MBATextBox.Text);
			td.target_width = int.Parse(TWFTextBox.Text);
			td.target_height = int.Parse(THFTextBox.Text);
			for (i = 0; i < BPListView.Items.Count;i++)
				{
				item = BPListView.Items[i];
				bf.light_amplitude = int.Parse(item.Text);
				bf.intensity = int.Parse(item.SubItems[1].Text);
				bf.blueness = int.Parse(item.SubItems[2].Text);
				td.blue_filters.Add(bf);
				}
		}


		private bool DetermineThresholds(int la,ref target_data td,ref int brthreshold,ref int bluthreshold)

		{
			bool rtn = false;
			int i;
			blue_filter bf = new blue_filter(),pbf = new blue_filter();

			if ((la > 0) && (td.blue_filters.Count > 0))
				{
				for (i = 0;i < td.blue_filters.Count;i++)
					{
					bf = (blue_filter) td.blue_filters[i];
					if (la < bf.light_amplitude)
						{
						if (i != 0)
							{
							brthreshold = (int) Math.Round(pbf.intensity + ((bf.intensity - pbf.intensity) * ((double) (la - pbf.light_amplitude)/(bf.light_amplitude - pbf.light_amplitude))));
							bluthreshold = (int) Math.Round(pbf.blueness + ((bf.blueness - pbf.blueness) * ((double) (la - pbf.light_amplitude)/(bf.light_amplitude - pbf.light_amplitude))));
							rtn = true;
							break;
							}
						else
							break;
						}
					else if (la == bf.light_amplitude)
						{
						brthreshold = bf.intensity;
						bluthreshold = bf.blueness;
						rtn = true;
						break;
						}
					pbf = bf;
					}
				if (!rtn && (la > bf.light_amplitude))
					{
					brthreshold = bf.intensity;
					bluthreshold = bf.blueness;
					rtn = true;
					}
				}
			if (!rtn)
				Log.LogEntry("Could not determine thresholds for light amplitude " + la);
//			else
//				Log.LogEntry("Light amplitude " + la + "  Thresholds: " + brthreshold + ", " + bluthreshold);
			return(rtn);
		}



		private int GetLightAmplitude()

		{
			string rsp;
			string[] values;
			int rtn = -1;

			rsp = SubSystemOpForm.SendToolCommand(UiConstants.LIGHT_AMP,1000);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 2)	
					{
					rtn = int.Parse(values[1]);
					}
				else
					{
					LATextBox.Clear();
					ErrorTextBox.AppendText(UiConstants.LIGHT_AMP + " incorrect response format\r\n");
					}
				}
			else
				{
				LATextBox.Clear();
				ErrorTextBox.AppendText(rsp + "\r\n");
				}
			return(rtn);
		}



		private void VidUpdate(int indx)

		{
			Image img;

			img = Image.FromStream(ms[indx]);
			img.RotateFlip(RotateFlipType.Rotate180FlipY);
			VideoPicBox.Image = img;
			BluePicBox.Image = KinectOp.blank;
			if (light_amp > 0)
				LATextBox.Text = light_amp.ToString();
			else
				LATextBox.Clear();
			ShootButton.Enabled = true;
			ProcButton.Enabled = true;
		}



		private void VidUpdateFail(string err)

		{
			VideoPicBox.Image = KinectOp.blank;
			BluePicBox.Image = KinectOp.blank;
			ErrorTextBox.AppendText(err + "\r\n");
			ShootButton.Enabled = true;
			ProcButton.Enabled = false;
		}



		private bool VideoReceive()

		{
			string msg;
			string[] val;
			int vlen,rvlen;
			object[] obj = new object[2];
			bool rtn = false;

			msg = SubSystemOpForm.tconnect.ReceiveResponse(100,true);
			if (msg.StartsWith(UiConstants.VIDEO_FRAME))
				{
				val = msg.Split(',');
				if (val.Length == 2)
					{
					SubSystemOpForm.tconnect.Send(UiConstants.OK);
					vlen = int.Parse(val[1]);
					ms[indx] = new MemoryStream();
					rvlen = SubSystemOpForm.tconnect.ReceiveStream(ref ms[indx],vlen);
					if (vlen == rvlen)
						{
						rtn = true;
						}
					else
						{
						this.BeginInvoke(vuf, "VideoReceive: Incorrect video stream length");
						Log.LogEntry("incorrect video stream length");
						}
					}
				else
					{
					SubSystemOpForm.tconnect.Send(UiConstants.FAIL);
					this.BeginInvoke(vuf, "VideoReceive: Incorrect response format");
					Log.LogEntry("bad response format");
					}
				}
			else
				{
				this.BeginInvoke(vuf,"VideoReceive: No video stream to receive");
				Log.LogEntry("no video stream to receive");
				}
			return(rtn);
		}



		private void ReciveVD()

		{
			if (VideoReceive())
				{
				SubSystemOpForm.tool_access.Release();
				light_amp = GetLightAmplitude();
				this.Invoke(vu, indx);
				indx = (indx + 1) % 2;
				}
			else
				SubSystemOpForm.tool_access.Release();
		}



		private void ShootButton_Click(object sender, EventArgs e)

		{
			string rsp;
			Thread rcv;

			SubSystemOpForm.tool_access.Wait();
			rsp = SubSystemOpForm.tconnect.SendCommand(UiConstants.SEND_VIDEO, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				ShootButton.Enabled = false;
				ProcButton.Enabled = false;
				light_amp = -1;
				LATextBox.Clear();
				rcv = new Thread(ReciveVD);
				rcv.Start();
				}
			else
				{
				SubSystemOpForm.tool_access.Release();
				VideoPicBox.Image = KinectOp.blank;
				BluePicBox.Image = KinectOp.blank;
				ErrorTextBox.AppendText(rsp + "\r\n");
				}
		}



		private void TVidUpdate()

		{
			Image img;

			img = Image.FromStream(tms);
			img.RotateFlip(RotateFlipType.Rotate180FlipY);
			BluePicBox.Image = img;
			CLTextBox.Text = center_loc.X + "," + center_loc.Y;
			ATextBox.Text = area.ToString();
			RTextBox.Text = width.ToString();
			HTextBox.Text = height.ToString();
			RATextBox.Text = ra.ToString("F1");
			DITextBox.Text = dist.ToString("F2");
			ShootButton.Enabled = true;
			ProcButton.Enabled = true;
		}



		private void TVidUpdateFail(string err)

		{
			Image img;

			if (tms.Length > 0)
				{
				img = Image.FromStream(tms);
				img.RotateFlip(RotateFlipType.Rotate180FlipY);
				BluePicBox.Image = img;
				}
			else
				BluePicBox.Image = KinectOp.blank;
			CLTextBox.Clear();
			ATextBox.Clear();
			RTextBox.Clear();
			HTextBox.Clear();
			RATextBox.Clear();
			DITextBox.Clear();
			ErrorTextBox.AppendText(err + "\r\n");
			ProcButton.Enabled = true;
			ShootButton.Enabled = true;
		}



		private bool TargetVideoReceive()

		{
			string msg;
			string[] val;
			int vlen,rvlen;
			object[] obj = new object[2];
			bool rtn = false;

			msg = SubSystemOpForm.tconnect.ReceiveResponse(100,true);
			tms = new MemoryStream();
			if (msg.StartsWith(UiConstants.TARGET_PROCESSED_FRAME))
				{
				val = msg.Split(',');
				if (val.Length == 10)
					{
					SubSystemOpForm.tconnect.Send(UiConstants.OK);
					try
					{
					center_loc = new Point(int.Parse(val[2]),int.Parse(val[3]));
					area = int.Parse(val[4]);
					width = int.Parse(val[5]);
					height = int.Parse(val[6]);
					ra = double.Parse(val[7]);
					dist = double.Parse(val[8]);
					vlen = int.Parse(val[9]);
					rvlen = SubSystemOpForm.tconnect.ReceiveStream(ref tms,vlen);
					if (vlen == rvlen)
						{
						if (val[1] == true.ToString())
							rtn = true;
						else
							{
							this.BeginInvoke(tvuf, "TargetVideoReceive: target not located.");
							}
						}
					else
						{
						this.BeginInvoke(tvuf, "TargetVideoReceive: Incorrect video stream length");
						Log.LogEntry("incorrect video stream length");
						tms.SetLength(0);
						}
					}

					catch (Exception)
					{
					this.BeginInvoke(tvuf, "TargetVideoReceive: Bad parameter");
					Log.LogEntry("bad parameter");
					}

					}
				else
					{
					SubSystemOpForm.tconnect.Send(UiConstants.FAIL);
					this.BeginInvoke(tvuf, "TargetVideoReceive: Incorrect response format");
					Log.LogEntry("bad response format");
					}
				}
			else
				{
				this.BeginInvoke(tvuf,"TargetVideoReceive: No video stream to receive");
				Log.LogEntry("no video stream to receive");
				}
			return(rtn);
		}



		private void ReciveTV()

		{
			if (TargetVideoReceive())
				{
				this.Invoke(tvu);
				SubSystemOpForm.tool_access.Release();
				}
			else
				SubSystemOpForm.tool_access.Release();
		}



		private void ProcButton_Click(object sender, EventArgs e)

		{
			string rsp;
			Thread rcv;

			SubSystemOpForm.tool_access.Wait();
			rsp = SubSystemOpForm.tconnect.SendCommand(UiConstants.TARGET_PROCESS_FRAME + "," + ((int) MBANumericUpDown.Value) + "," + ((int) ITNumericUpDown.Value) + "," + ((int) BTNumericUpDown.Value), 200);
			if (rsp.StartsWith(UiConstants.OK))
				{
				ProcButton.Enabled = false;
				ShootButton.Enabled = false;
				rcv = new Thread(ReciveTV);
				rcv.Start();
				}
			else
				{
				SubSystemOpForm.tool_access.Release();
				BluePicBox.Image = KinectOp.blank;
				ErrorTextBox.AppendText(rsp + "\r\n");
				}
		}



		private void LVButton_Click(object sender, EventArgs e)

		{
			int brth = 0,bluth = 0;

			if (LATextBox.Text.Length > 0)
				{
				if (DetermineThresholds(int.Parse(LATextBox.Text), ref td, ref brth, ref bluth))
					{
					ITNumericUpDown.Value = brth;
					BTNumericUpDown.Value = bluth;
					MBANumericUpDown.Value = int.Parse(MBATextBox.Text);
					}
				else
					ErrorTextBox.AppendText("Could not load value from parameter file.\r\n");
				}
		}



		private void ABFPButton_Click(object sender, EventArgs e)

		{
			ListViewItem item;

			item = new ListViewItem(LATextBox.Text);
			item.SubItems.Add(((int) ITNumericUpDown.Value).ToString());
			item.SubItems.Add(((int) BTNumericUpDown.Value).ToString());
			BPListView.Items.Add(item);
			UpdateTargetData();
		}



		private void UDButton_Click(object sender, EventArgs e)

		{
			SaveParameters(ref td);
		}



		private void DIButton_Click(object sender, EventArgs e)

		{
			ListViewItem item;

			if (BPListView.SelectedItems.Count > 0)
				{
				item = BPListView.SelectedItems[0];
				BPListView.Items.Remove(item);
				UpdateTargetData();
				}
		}


		}

	class ListViewComparer : IComparer

		{
		public int Compare(object x, object y)

			{
			int a, b;

			a = int.Parse(((ListViewItem)x).Text);
			b = int.Parse(((ListViewItem)y).Text);
			return (a - b);
			}
		}

	}
