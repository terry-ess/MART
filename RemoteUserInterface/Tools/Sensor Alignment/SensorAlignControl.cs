using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RemoteUserInterface;
using Constants;
using RobotConnection;
using Coding4Fun.Kinect.WinForm;
using MathNet.Numerics.LinearAlgebra.Double;


namespace Sensor_Alignment
	{
	public partial class SensorAlignControl : UserControl
		{

		private const string OFFSET_FILE = "sensoroffset.param";

		private static DenseMatrix mat90 = DenseMatrix.OfArray(new[,] { { 0.0, -1.0 }, { 1.0, 0.0 } });
		private static DenseMatrix matxreflect = DenseMatrix.OfArray(new[,] { { 1.0, 0.0 }, { 0.0, -1.0 } });  //required because room's postive Y is down (GUI style coordinate system)

		public struct FPoint
			{
			public float X, Y;

			public FPoint(float x, float y)
				{
				X = x;
				Y = y;
				}
			};

		private delegate void VideoUpdate(int indx);
		private VideoUpdate vu;
		private delegate void VUFail(string err);
		private VUFail vuf;
		private MemoryStream[] ms = new MemoryStream[2];
		private int indx = 0;
		private Image vimg;
		private Bitmap blank;
		MemoryStream flms,rlms;


		public SensorAlignControl()

		{
			InitializeComponent();
		}



		public void Open()

		{
			int i,j;

			StatusTextBox.AppendText("Link established with robot\r\n");
			blank = new Bitmap(VideoPictureBox.Width, VideoPictureBox.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			for (i = 0; i < VideoPictureBox.Width; i++)
				for (j = 0; j < VideoPictureBox.Height; j++)
					blank.SetPixel(i, j, Color.White);
			VideoPictureBox.Image = blank;
			vu = VidUpdate;
			vuf = VidUpdateFail;
		}



		public void Close()

		{
		}



		private void DisplayImage(Image img)

		{
			Graphics g;
			int col;
			string rsp;
			string[] values;

			g = System.Drawing.Graphics.FromImage(img);
			col = (int) ColNumericUpDown.Value;
			g.DrawLine(Pens.Red, col,0,col,img.Height);
			VideoPictureBox.Image = img;
			col -= (int) Math.Round((double) VideoPictureBox.Width/2);
			rsp = SensorAlignForm.SendToolCommand(UiConstants.DETERMINE_ANGLES +"," + col + ",0", 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				values = rsp.Split(',');
				if (values.Length == 3)
					{
					HATextBox.Text = values[1];
					}
				else
					{
					HATextBox.Text = "unk";
					StatusTextBox.AppendText(UiConstants.DETERMINE_ANGLES + " incorrect response format\r\n");
					}
				}
		}



		private void VidUpdate(int indx)

		{
			Image img;

			img = Image.FromStream(ms[indx]);
			img.RotateFlip(RotateFlipType.Rotate180FlipY);
			vimg = (Image) img.Clone();
			DisplayImage(img);
			ShootButton.Enabled = true;
		}



		private void VidUpdateFail(string err)

		{
			VideoPictureBox.Image = blank;
			StatusTextBox.AppendText(err + "\r\n");
			ShootButton.Enabled = true;
		}



		private bool VideoReceive()

		{
			string msg;
			string[] val;
			int vlen,rvlen;
			object[] obj = new object[2];
			bool rtn = false;

			msg = SensorAlignForm.tconnect.ReceiveResponse(100,true);
			if (msg.StartsWith(UiConstants.VIDEO_FRAME))
				{
				val = msg.Split(',');
				if (val.Length == 2)
					{
					SensorAlignForm.tconnect.Send(UiConstants.OK);
					vlen = int.Parse(val[1]);
					ms[indx] = new MemoryStream();
					rvlen = SensorAlignForm.tconnect.ReceiveStream(ref ms[indx],vlen);
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
					SensorAlignForm.tconnect.Send(UiConstants.FAIL);
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
				this.BeginInvoke(vu, indx);
				indx = (indx + 1) % 2;
				SensorAlignForm.tool_access.Release();
				}
			else
				SensorAlignForm.tool_access.Release();
		}



		private void ShootButton_Click(object sender, EventArgs e)

		{
			string rsp;
			Thread rcv;

			SensorAlignForm.tool_access.Wait();
			rsp = SensorAlignForm.tconnect.SendCommand(UiConstants.SEND_VIDEO,100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				rcv = new Thread(ReciveVD);
				rcv.Start();
				}
			else
				{
				SensorAlignForm.tool_access.Release();
				VideoPictureBox.Image = VideoPictureBox.ErrorImage;
				StatusTextBox.AppendText(rsp + "\r\n");
				}
		}



		private void RowColChange(object sender,EventArgs e)

		{
			Image img;

			img = (Image) vimg.Clone();
			DisplayImage(img);
		}



		private bool LlidarReceive(ref MemoryStream ms,string frame_title)

		{
			string msg;
			string[] val;
			int vlen, rvlen;
			bool rtn = false;

			msg = SensorAlignForm.tconnect.ReceiveResponse(100, true);
			if (msg.StartsWith(frame_title))
				{
				val = msg.Split(',');
				if (val.Length == 2)
					{
					SensorAlignForm.tconnect.Send(UiConstants.OK);
					vlen = int.Parse(val[1]);
					rvlen = SensorAlignForm.tconnect.ReceiveStream(ref ms, vlen);
					if (vlen == rvlen)
						rtn = true;
					else
						{
						StatusTextBox.AppendText("LidarReceive incorrect length\r\n");
						Log.LogEntry("LidarReceive incorrect length");
						}
					}
				else
					{
					StatusTextBox.AppendText("LidarRecieve bad response format\r\n");
					Log.LogEntry("LidarRecieve bad response format");
					}
				}
			else
				{
				StatusTextBox.AppendText("LidarRecieve no depth stream to receive\r\n");
				Log.LogEntry("LidarRecieve no depth stream to receive");
				}
			return(rtn);
		}



		private FPoint RotatePoint(FPoint start,double offset,double shift_angle)

		{
			FPoint end;
			DenseMatrix mat;
			DenseVector vec;
			DenseVector result;
			double rangle;

			if (shift_angle < 0)
				shift_angle += 360;
			rangle = shift_angle % 90;
			mat = DenseMatrix.OfArray(new[,] { { Math.Cos(rangle * MathConvert.DEG_TO_RAD), -Math.Sin(rangle * MathConvert.DEG_TO_RAD) }, { Math.Sin(rangle * MathConvert.DEG_TO_RAD), Math.Cos(rangle * MathConvert.DEG_TO_RAD) } });
			vec = new DenseVector(new[] { start.X,(start.Y + offset) });
			if (shift_angle >= 90)
				vec = vec * mat90;
			if (shift_angle >= 180)
				vec = vec * mat90;
			if (shift_angle >= 270)
				vec = vec * mat90;
			result = vec * mat;
			end = new FPoint((float) result.Values[0],(float) (result.Values[1] - offset));
			return (end);
		}



		private void DisplayFrontLidar(MemoryStream lms,double shift_angle = 0)

		{
			BinaryReader br;
			int i;
			float x,y;
			FPoint sapt;

			if (lms != null)
				{
				FrontScanChart.Series[0].Points.Clear();
				br = new BinaryReader(lms);
				br.BaseStream.Seek(0, SeekOrigin.Begin);
				for (i = 0; i < lms.Length / 8; i++)
					{
					x = br.ReadSingle();
					y = br.ReadSingle();
					if (shift_angle != 0)
						{
						sapt = RotatePoint(new FPoint(x,y),RobotMeasurements.FRONT_PIVOT_PT_OFFSET,-shift_angle);
						x = sapt.X;
						y = sapt.Y;
						}
					if ((Math.Abs(x) <= FrontScanChart.ChartAreas[0].AxisX.Maximum) && (y >= FrontScanChart.ChartAreas[0].AxisY.Minimum) && (y <= FrontScanChart.ChartAreas[0].AxisY.Maximum))
						{
						FrontScanChart.Series[0].Points.AddXY(x, y);
						}
					}
				}
		}




		private void FLSButton_Click(object sender, EventArgs e)

		{
			string rsp;

			SensorAlignForm.tool_access.Wait();
			FrontScanChart.Series[0].Points.Clear();
			rsp = SensorAlignForm.tconnect.SendCommand(UiConstants.SEND_FRONT_LIDAR, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				flms = new MemoryStream();
				if (LlidarReceive(ref flms,UiConstants.LIDAR_FRAME))
					{
					SensorAlignForm.tool_access.Release();
					DisplayFrontLidar(flms,(double) FSANumericUpDown.Value);
					FShiftButton.Enabled = true;
					}
				else
					{
					SensorAlignForm.tool_access.Release();
					StatusTextBox.AppendText("Front LIDAR data transfer failed.\r\n");
					FShiftButton.Enabled = false;
					}
				}
			else
				{
				SensorAlignForm.tool_access.Release();
				FShiftButton.Enabled = false;
				StatusTextBox.AppendText("Front LIDAR request failed.\r\n");
				}
		}



		private void FShiftButton_Click(object sender, EventArgs e)

		{
			DisplayFrontLidar(flms, (double)FSANumericUpDown.Value);
		}



		private void DisplayRearLidar(MemoryStream lms,double shift_angle = 0)

		{
			BinaryReader br;
			int i;
			float x,y;
			FPoint sapt;

			if (lms != null)
				{
				RearScanChart.Series[0].Points.Clear();
				br = new BinaryReader(lms);
				br.BaseStream.Seek(0, SeekOrigin.Begin);
				for (i = 0; i < lms.Length / 8; i++)
					{
					x = br.ReadSingle() * -1;
					y = (br.ReadSingle() * -1) - RobotMeasurements.RLIDAR_OFFSET;
					if (shift_angle != 0)
						{
						sapt = RotatePoint(new FPoint(x,y),RobotMeasurements.FRONT_PIVOT_PT_OFFSET,-shift_angle);
						x = sapt.X;
						y = sapt.Y;
						}
					if ((Math.Abs(x) <= RearScanChart.ChartAreas[0].AxisX.Maximum) && (y >= RearScanChart.ChartAreas[0].AxisY.Minimum) && (y <= RearScanChart.ChartAreas[0].AxisY.Maximum))
						{
						RearScanChart.Series[0].Points.AddXY(x, y);
						}
					}
				}
		}


		
		private void RLSButton_Click(object sender, EventArgs e)

		{
			string rsp;

			SensorAlignForm.tool_access.Wait();
			RearScanChart.Series[0].Points.Clear();
			rsp = SensorAlignForm.tconnect.SendCommand(UiConstants.SEND_REAR_LIDAR, 100);
			if (rsp.StartsWith(UiConstants.OK))
				{
				rlms = new MemoryStream();
				if (LlidarReceive(ref rlms,UiConstants.LIDAR_FRAME))
					{
					SensorAlignForm.tool_access.Release();
					DisplayRearLidar(rlms,(double) RSANumericUpDown.Value);
					RShiftButton.Enabled = true;
					}
				else
					{
					SensorAlignForm.tool_access.Release();
					StatusTextBox.AppendText("Rear LIDAR data transfer failed.\r\n");
					RShiftButton.Enabled = false;
					}
				}
			else
				{
				SensorAlignForm.tool_access.Release();
				RShiftButton.Enabled = false;
				StatusTextBox.AppendText("Rear LIDAR request failed.\r\n");
				}
		}



		private void RShiftButton_Click(object sender, EventArgs e)

		{
			DisplayRearLidar(rlms, (double)RSANumericUpDown.Value);
		}



		private void SaveButton_Click(object sender, EventArgs e)

		{
			string fname,nfname = "";
			TextWriter tw;

			fname = Application.StartupPath + Folders.CAL_SUB_DIR + OFFSET_FILE;
			if (File.Exists(fname))
				{
				nfname = fname + "." + DateTime.Now.Ticks;
				File.Copy(fname,nfname);
				File.Delete(fname);
				}
			tw = File.CreateText(fname);
			if (tw != null)
				{
				tw.WriteLine(HATextBox.Text);
				tw.WriteLine(FSANumericUpDown.Value);
				tw.WriteLine(RSANumericUpDown.Value);
				tw.Close();
				StatusTextBox.AppendText("Saved " + fname);
				}
			else
				{
				StatusTextBox.AppendText("Could not create offset file.");
				}
		}

		}
	}
