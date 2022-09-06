using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Coding4Fun.Kinect.WinForm;
using Constants;
using RobotConnection;
using Microsoft.Kinect;


namespace Sub_system_Operation
	{
	public partial class KinectMapping : UserControl
		{

		public struct Loc3D
		{
		public double x;
		public double y;
		public double z;

		public Loc3D(double x,double y,double z)

		{
			this.x = x;
			this.y = y;
			this.z = z;
		}


		public override string ToString()

		{
			string rtn = "";

			rtn = "(" + this.x.ToString("F3") + ", " + this.y.ToString("F3") + ", " + this.z.ToString("F3")  + ")";
			return(rtn);
		}


		};

		private const string PARAM_FILE = "kinectcal.param";
		private const int MIN_DEPTH = 800;
		private const int MAX_DEPTH = 4000;

		private short[] depthmap;
		private delegate void DepthUpdate();
		private DepthUpdate du;
		private delegate void DUFail();
		private DUFail duf;
		private static double[,] depth_cal;
		private System.Windows.Forms.DataVisualization.Charting.Series ser;
		private System.Windows.Forms.DataVisualization.Charting.Series ser2;
		private SkeletonPoint[] sips;


		public KinectMapping()

		{
			InitializeComponent();
		}



		public void Open()

		{
			ReadCalibrationData();
			DepthPictureBox.Image = KinectOp.blank;
			depthmap = new short[KinectOp.video_frame_width * KinectOp.video_frame_height];
			ser = MapChart.Series.Add("data");
			ser.Color = Color.Black;
			ser.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
			ser2 = MapChart.Series.Add("data2");
			ser2.Color = Color.LightPink;
			ser2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
			PanTiltUpdate();
			SDButton.Enabled = true;
			ODGroupBox.Enabled = true;
			du = DepthUD;
			duf = DepthUF;
		}



		public void Close()

		{

		}



		private void PanTiltUpdate()

		{
			string rsp;
			string[] values;

			rsp = SubSystemOpForm.SendToolCommand(UiConstants.CURRENT_PAN, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 2)
					{
					PanNumericUpDown.Value = int.Parse(values[1]);
					}
				else
					{
					PanNumericUpDown.Value = 0;
					Log.LogEntry((UiConstants.CURRENT_PAN + " failed, bad reply format"));
					}
				}
			rsp = SubSystemOpForm.SendToolCommand(UiConstants.CURRENT_TILT, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 2)
					{
					TiltNumericUpDown.Value = int.Parse(values[1]);
					}
				else
					{
					TiltNumericUpDown.Value = 0;
					Log.LogEntry((UiConstants.CURRENT_TILT + " failed, bad reply format"));
					}
				}
		}

		private void ReadCalibrationData()

		{
			string fname;
			TextReader tr;
			int lines,i;
			string line;
			string[] values;

			fname = Application.StartupPath + Folders.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				lines = int.Parse(tr.ReadLine());
				depth_cal = new double[2,lines];
				for (i = 0;i < lines;i++)
					{
					line = tr.ReadLine();
					values = line.Split(',');
					depth_cal[0,i] = double.Parse(values[0]);
					depth_cal[1,i] = double.Parse(values[1]);
					}
				tr.Close();
				}
		}



		// the calibration data is in inches
		public static double CorrectedDistance(double dist)

		{
			double cdist = 0,dm,dc,dd;
			int i;

			if ((depth_cal == null) || (depth_cal.Length == 0) || (dist <= 0))
				cdist = dist;
			else
				{
				for (i = 0; i < depth_cal.Length / 2; i++)
					{
					if (dist < depth_cal[0, i])
						{
						dm = depth_cal[0, i] - depth_cal[0, i - 1];
						dd = dist - depth_cal[0, i - 1];
						dc = depth_cal[1, i] - depth_cal[1, i - 1];
						cdist = ((dd / dm) * dc) + depth_cal[1, i - 1];
						break;
						}
					}
				if (i == depth_cal.Length / 2)
					{
					dd = dist - depth_cal[0, i - 1];
					cdist = ((depth_cal[1, i - 1] / depth_cal[0, i - 1]) * dd) + depth_cal[1, i - 1];
					}
				}
			return (cdist);
		}



		public double VideoVerDegrees(int no_pixel)

		{
			double val = 0,adj;

			adj = ((double)KinectOp.video_frame_height /2) / Math.Tan((KinectOp.video_vert_fov /2) * MathConvert.DEG_TO_RAD);
			val = Math.Atan(no_pixel / adj) * MathConvert.RAD_TO_DEG;
			return (val);
		}



		public double VideoHorDegrees(int no_pixel)

		{
			double val = 0;
			double adj;

			adj = ((double)KinectOp.video_frame_width / 2) / Math.Tan((KinectOp.video_hor_fov /2) * MathConvert.DEG_TO_RAD);
			val = Math.Atan(no_pixel/adj) * MathConvert.RAD_TO_DEG;
			return (val);
		}



		private double KinectForwardDistDelta(double tilt)		//front shelf edge is robot corrd y 0 = front of Kinect when no tilt
																				//tilt and cfa are down from the horizon (i.e. negative angles)
		{
			double fdd,cfa,cfd,x2;

			cfa = Math.Atan(RobotMeasurements.KINECT_FRONT_OFFSET / RobotMeasurements.KINECT_CENTER_OFFSET) * MathConvert.RAD_TO_DEG;
			cfd = Math.Sqrt((RobotMeasurements.KINECT_CENTER_OFFSET * RobotMeasurements.KINECT_CENTER_OFFSET) + (RobotMeasurements.KINECT_FRONT_OFFSET * RobotMeasurements.KINECT_FRONT_OFFSET));
			x2 = cfd * Math.Sin((cfa + tilt) * MathConvert.DEG_TO_RAD);
			fdd = RobotMeasurements.KINECT_FRONT_OFFSET - x2;
			return (fdd);
		}


		private double KinectHeight(double tilt)				//tilt and cfa are down from the horizon (i.e. negative angles)

		{
			double kh,y2,cfa,cfd;

			cfa = Math.Atan(RobotMeasurements.KINECT_FRONT_OFFSET/RobotMeasurements.KINECT_CENTER_OFFSET) * MathConvert.RAD_TO_DEG;
			cfd = Math.Sqrt((RobotMeasurements.KINECT_CENTER_OFFSET * RobotMeasurements.KINECT_CENTER_OFFSET) + (RobotMeasurements.KINECT_FRONT_OFFSET * RobotMeasurements.KINECT_FRONT_OFFSET));
			y2 = cfd * Math.Cos((cfa + tilt) * MathConvert.DEG_TO_RAD);
			kh = RobotMeasurements.BASE_KINECT_HEIGHT + (y2 - RobotMeasurements.KINECT_CENTER_OFFSET);
			return(kh);
		}



		public Loc3D MapKCToRC(double x,double cdist,double va,double tilt)

		{
			Loc3D loc = new Loc3D();
			double pdist;

			loc.x = x;
			pdist = cdist/Math.Cos(va * MathConvert.DEG_TO_RAD);
			loc.z = (pdist * Math.Sin((90 + tilt + va) * MathConvert.DEG_TO_RAD));
			loc.y = KinectHeight(tilt) - (loc.z/Math.Tan((90 + tilt + va) * MathConvert.DEG_TO_RAD));
			loc.z += KinectForwardDistDelta(tilt);
			return (loc);
		}



		private void DOButton_Click(object sender, EventArgs e)

		{
			int row = 0,col = 0,dist_limit,pixel;//,wcy, wcx, wcz,mx;
			double ray,rax,x,depth,frow_fdist = 0;
			double tilt_angle,min_obs_dist,kinect_height,max_fheight = 0,min_fheight,tilt_correct;
			bool front_row_found = false;
			Loc3D target_loc;
			DateTime now = DateTime.Now;

			DOButton.Enabled = false;
			OATextBox.Clear();
			SaveButton.Enabled = false;
			ser.Points.Clear();
			ser2.Points.Clear();
			Application.DoEvents();
			tilt_angle = (double)TiltNumericUpDown.Value;
			if (tilt_angle < 0)
				{
				OATextBox.Text = now.ToShortDateString() + "  " + now.ToShortTimeString() + "\r\n";
				dist_limit = (int) FLNumericUpDown.Value;
				min_obs_dist = dist_limit;
				tilt_correct  = (double) FCNumericUpDown.Value;
				kinect_height = KinectHeight(tilt_angle + tilt_correct);
				min_fheight = kinect_height;

				try
				{
				for (row = KinectOp.video_frame_height - 1;row >= 0;row--)
					{
					ray = VideoVerDegrees((KinectOp.video_frame_height / 2) - row);
					for (col = 0;col < KinectOp.video_frame_width;col ++)
						{
						pixel = (row * KinectOp.video_frame_width) + col;
						if ((depthmap[pixel] >= MIN_DEPTH) && (depthmap[pixel] <= MAX_DEPTH))
							{
							if (CDCheckBox.Checked)
								depth = CorrectedDistance(depthmap[pixel] * MathConvert.MM_TO_IN);
							else
								depth = depthmap[pixel] * MathConvert.MM_TO_IN;
							rax = -VideoHorDegrees(col - (KinectOp.video_frame_width / 2));
							x = depth * Math.Tan(rax * MathConvert.DEG_TO_RAD);
							target_loc = MapKCToRC(x,depth,ray,tilt_angle + tilt_correct);
							target_loc.y += (double) HCNumericUpDown.Value;
							if (target_loc.y > (double) HLNumericUpDown.Value)
								{
								ser.Points.AddXY(target_loc.x,target_loc.z);
								if (target_loc.y < min_fheight)
									min_fheight = target_loc.y;
								if (target_loc.y > max_fheight)
									max_fheight = target_loc.y;
								}
							else if ((target_loc.y < 0) && (ShowNYCheckBox.Checked))
								{
								ser2.Points.AddXY(target_loc.x,target_loc.z);
								if (target_loc.y < min_fheight)
									min_fheight = target_loc.y;
								}
							if (!front_row_found &&  (col == KinectOp.video_frame_width /2))
								{
								frow_fdist = target_loc.z;
								front_row_found = true;
								}
							}
						}
					}
				}

				catch(Exception ex)
				{
				OATextBox.AppendText("Exception : " + ex.Message + "\r\n");
				OATextBox.AppendText("row " + row + "   col " + col + "\r\n");
				Log.LogEntry("Kinect detect obstacle exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				Log.LogEntry("row " + row + "   col " + col);
				}

				OATextBox.AppendText("Set tilt angle: " + ((int) TiltNumericUpDown.Value) + "°\r\n");
				OATextBox.AppendText("Tilt correction: " + tilt_correct + " °\r\n");
				OATextBox.AppendText("Height correction: " + HCNumericUpDown.Value + " in\r\n");
				OATextBox.AppendText("Height limit: " + HLNumericUpDown.Value + " in\r\n");
				OATextBox.AppendText("Kinect height: " + kinect_height.ToString("F2") + " in\r\n");
				OATextBox.AppendText("Kinect forward distance delta: " + KinectForwardDistDelta(tilt_angle).ToString("F2") + " in\r\n");
				OATextBox.AppendText("Dist limit: " + dist_limit + " in\r\n");
				OATextBox.AppendText("First observable row floor distance at horizontal center: " + frow_fdist.ToString("F2") + " in\r\n");
				OATextBox.AppendText("Show negative Y: " + ShowNYCheckBox.Checked + "\r\n");
				OATextBox.AppendText("Use corrected distance: " + CDCheckBox.Checked + "\r\n");
				OATextBox.AppendText("Max obstacle height: " + max_fheight.ToString("F2") + " in\r\n");
				OATextBox.AppendText("Min obstacle height: " + min_fheight.ToString("F2") + " in\r\n");
				MRadioButton.Checked = true;
				SaveButton.Enabled = true;
				}
			else
				OATextBox.AppendText("Tilt must be negative.\r\n");
			DOButton.Enabled = true;
		}



		private void RadioButton_CheckedChanged(object sender, EventArgs e)

		{
			if (((RadioButton)sender).Checked == true)
				{
				if (DMRadioButton.Checked)
					{
					DepthPictureBox.Visible = true;
					MapChart.Visible = false;
					}
				else if (MRadioButton.Checked)
					{
					DepthPictureBox.Visible = false;
					MapChart.Visible = true;
					}
				}
		}



		private void DepthUD()

		{
			Bitmap bm;

			bm = depthmap.ToBitmap(KinectOp.video_frame_width, KinectOp.video_frame_height,0,Color.White);
			bm.RotateFlip(RotateFlipType.Rotate180FlipY);
			DepthPictureBox.Image = bm;
			ODGroupBox.Enabled = true;
			DMRadioButton.Checked = true;
			SaveButton.Enabled = false;
			SDButton.Enabled = true;
		}



		private void DepthUF()

		{
			DepthPictureBox.Image = KinectOp.blank;
			ODGroupBox.Enabled = false;
			SaveButton.Enabled = false;
			OATextBox.AppendText("Depth download failed.\r\n");
			SDButton.Enabled = true;
		}



		private void DepthReceive()

		{
			string msg;
			string[] val;
			int vlen,rvlen;
			MemoryStream ms = new MemoryStream();
			BinaryReader br;
			int i;

			msg = SubSystemOpForm.tconnect.ReceiveResponse(100,true);
			if (msg.StartsWith(UiConstants.DEPTH_MAP))
				{
				val = msg.Split(',');
				if (val.Length == 2)
					{
					SubSystemOpForm.tconnect.Send(UiConstants.OK);
					vlen = int.Parse(val[1]);
					if (vlen == depthmap.Length * 2)
						{
						rvlen = SubSystemOpForm.tconnect.ReceiveStream(ref ms,vlen);
						if (vlen == rvlen)
							{
							ms.Seek(0,SeekOrigin.Begin);
							br = new BinaryReader(ms);
							for (i = 0;i < depthmap.Length;i++)
								depthmap[i] = br.ReadInt16();
							this.BeginInvoke(du);
							}
						else
							{
							this.BeginInvoke(duf);
							Log.LogEntry("DepthReceive incorrect length");
							}
						}
					else
						{
						this.BeginInvoke(duf);
						Log.LogEntry("DepthReceive bad stream length");
						}
					}
				else
					{
					this.BeginInvoke(duf);
					Log.LogEntry("DepthRecieve bad response format");
					}
				}
			else
				{
				this.BeginInvoke(duf);
				Log.LogEntry("DepthRecieve no video stream to receive");
				}
			SubSystemOpForm.tool_access.Release();
		}



		private void SDButton_Click(object sender, EventArgs e)

		{
			string rsp;
			Thread vrcv;

			SubSystemOpForm.tool_access.Wait();
			rsp = SubSystemOpForm.tconnect.SendCommand(UiConstants.SEND_DEPTH_MAP, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				vrcv = new Thread(DepthReceive);
				vrcv.Start();
				SDButton.Enabled = false;
				}
			else
				{
				SubSystemOpForm.tool_access.Release();
				ODGroupBox.Enabled = false;
				DepthPictureBox.Image = KinectOp.blank;
				OATextBox.AppendText("Depth shoot failed with response: " + rsp);
				}
		}



		private void SaveButton_Click(object sender, EventArgs e)

		{
			Bitmap bm;
			TextWriter tw;
			BinaryWriter bw;
			int i;
			String dpic, ompic,fname;
			DateTime now = DateTime.Now;
			double value;

			bm = (Bitmap) DepthPictureBox.Image;
			dpic = Log.LogDir() + "Depth " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + ".jpg";
			bm.Save(dpic, ImageFormat.Jpeg);
			ompic = Log.LogDir() + "Obstacle map " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + ".bmp";
			MapChart.SaveImage(ompic, System.Windows.Forms.DataVisualization.Charting.ChartImageFormat.Bmp);
			fname = Log.LogDir() + "depth data " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + now.Second + ".bin";
			bw = new BinaryWriter(File.Open(fname, FileMode.Create));
			for (i = 0; i < depthmap.Length; i++)
				bw.Write(depthmap[i]);
			bw.Close();
			tw = File.CreateText(Log.LogDir() + "Kinect obstacle maping results " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + ".txt");
			if (tw != null)
				{
				tw.WriteLine("Kinect obstacle maping results");
				tw.WriteLine(now.ToShortDateString() + " " + now.ToShortTimeString());
				tw.WriteLine();
				tw.WriteLine("Depth picture: " + dpic);
				tw.WriteLine();
				tw.WriteLine("Obstacle map: " + ompic);
				tw.WriteLine();
				tw.WriteLine("Depth binary data: " + fname);
				tw.WriteLine();
				tw.WriteLine("Results:");
				for (i = 0; i < OATextBox.Lines.Length; i++)
					tw.WriteLine(OATextBox.Lines[i]);
				tw.Close();
				}
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
					Log.LogEntry(UiConstants.SET_PAN_TILT + " incorrect response format");
					OATextBox.AppendText(UiConstants.SET_PAN_TILT + " incorrect response format");
					}
				}
			else
				{
				PanNumericUpDown.Value = 0;
				TiltNumericUpDown.Value = 0;
				OATextBox.AppendText(UiConstants.SET_PAN_TILT + " error: " + rsp + "\r\n");
				}
		}

		}


	}
