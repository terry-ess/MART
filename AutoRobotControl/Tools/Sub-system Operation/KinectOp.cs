using System;
using System.Drawing;
using System.Net;
using System.IO;
using Microsoft.Kinect;
using AutoRobotControl;
using Constants;
using Coding4Fun.Kinect.WinForm;


namespace Sub_system_Operation
	{
	static class KinectOp
		{

		private const string BACKWARD = "TB ";
		private const int START_POSITION = 36;
		private const string NEXT_POSITION = "12";

		private static UiConnection connect = null;
		private static DepthImagePixel[] depthdata = new DepthImagePixel[Kinect.nui.DepthStream.FramePixelDataLength];
		private static DepthImagePoint[] dips = new DepthImagePoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];
		private static SkeletonPoint[] sips = new SkeletonPoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];
		private static byte[] videodata = null;
		private static Target tar = new Target();
		private static int light_amp = -1;


		public static bool Open(UiConnection con)

		{
			bool rtn = false;

			if ((Kinect.nui != null) && Kinect.nui.IsRunning)
				{
				connect = con;
				videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
				rtn = true;
				}
			return (rtn);
		}



		public static void Close()

		{
			connect = null;
		}



		private static void SendDepthMap(IPEndPoint ep)

		{
			MemoryStream ms;
			BinaryWriter bw;
			string cmd,rsp,fname;
			DateTime now = DateTime.Now;
			double value;
			int i;

			if ((Kinect.nui != null) && (Kinect.nui.IsRunning))
				{
				if (Kinect.GetDepthFrame(ref depthdata, 40))
					{
					Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, depthdata, dips);
					connect.SendResponse(UiConstants.OK, ep, true);
					ms = new MemoryStream();
					bw = new BinaryWriter(ms);
					for (i = 0;i < dips.Length;i++)
						bw.Write((short) dips[i].Depth);
					cmd = UiConstants.DEPTH_MAP + "," + ms.Length;
					rsp = connect.SendCommand(cmd, 20,ep);
					if (rsp.StartsWith(UiConstants.OK))
						{
						connect.SendStream(ms,ep);
						}
					bw.Close();
					Kinect.nui.CoordinateMapper.MapDepthFrameToSkeletonFrame(Kinect.nui.DepthStream.Format, depthdata, sips);
					fname = Log.LogDir() + "\\Point cloud binary data " + +now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "." + now.Second + ".pc";
					bw = new BinaryWriter(File.Create(fname));
					if (bw != null)
						{
						for (i = 0; i < sips.Length; i++)
							{
							value = (short)(sips[i].X * 1000);
							bw.Write((short)value);
							value = (short)(sips[i].Y * 1000);
							bw.Write((short)value);
							value = (short)(sips[i].Z * 1000);
							bw.Write((short)value);
							}
						bw.Close();
						Log.LogEntry("Saved: " + fname);
						}
					}
				else
					{
					connect.SendResponse(UiConstants.FAIL, ep, true);
					Log.LogEntry("Depth frame not available.");
					}
				}
			else
				{
				connect.SendResponse(UiConstants.FAIL,ep,true);
				Log.LogEntry("Kinect not available.");
				}
		}



		private static void SendVideo(IPEndPoint rcvr)

		{
			Bitmap bm;
			MemoryStream ms;
			String cmd,rsp;
			int la1,la2;


			la1 = AutoRobotControl.HeadAssembly.GetLightAmplitude();
			if (Kinect.GetColorFrame(ref videodata,40))
				{
				la2 = AutoRobotControl.HeadAssembly.GetLightAmplitude();
				light_amp = (int) Math.Round((double) (la1 + la2)/2);
				bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				ms = new MemoryStream();
				bm.Save(ms,System.Drawing.Imaging.ImageFormat.Jpeg);
				cmd = UiConstants.VIDEO_FRAME + "," + ms.Length;
				rsp = connect.SendCommand(cmd, 20,rcvr);
				if (rsp.StartsWith(UiConstants.OK))
					connect.SendStream(ms,rcvr);
				}
			else
				{
				connect.SendResponse(UiConstants.FAIL + ",could not obtain image",rcvr,true);
				light_amp = -1;
				}
		}



		private static void TargetProcessFrame(int min_blob_area,int brthreshold, int bluthreshold, IPEndPoint rcvr)

		{
			Target.dblob db = new Target.dblob();
			Bitmap bm = null;
			string cmd,rsp;
			MemoryStream ms;
			bool rtn;
			Graphics g;

			rtn = tar.LocateTarget(ref db, ref bm, videodata, min_blob_area, brthreshold, bluthreshold);
			ms = new MemoryStream();
			if (rtn)
				{
				g = System.Drawing.Graphics.FromImage(bm);
				g.DrawRectangle(Pens.Red, db.rect);
				}
			bm.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
			if (rtn)
				cmd = UiConstants.TARGET_PROCESSED_FRAME + "," + true.ToString() + "," + db.x + "," + db.y + "," + db.area + "," + db.rect.Width + "," + db.rect.Height + "," + db.ra.ToString("F2") + "," + db.dist.ToString("F2") + "," + ms.Length;
			else
				cmd = UiConstants.TARGET_PROCESSED_FRAME + "," + false.ToString() + ",0,0,0,0,0,0,0," + ms.Length;
			rsp = connect.SendCommand(cmd, 20,rcvr);
			if (rsp.StartsWith(UiConstants.OK))
				connect.SendStream(ms,rcvr);
		}



		public static string MessageHandler(string msg, IPEndPoint ep)

		{
			string rsp = "";
			int val,val2,val3;
			string[] values;

			if (connect != null)
				{
				if (msg == UiConstants.CURRENT_PAN)
					{
					val = AutoRobotControl.HeadAssembly.PanAngle();
					rsp = UiConstants.OK + "," + val;
					}
				else if (msg == UiConstants.CURRENT_TILT)
					{
					val = AutoRobotControl.HeadAssembly.TiltAngle();
					rsp = UiConstants.OK + "," + val;
					}
				else if (msg.StartsWith(UiConstants.SET_PAN_TILT + ","))
					{
					values = msg.Split(',');
					if (values.Length == 3)
						{
						if (AutoRobotControl.HeadAssembly.Pan(int.Parse(values[1]), true))
							if (AutoRobotControl.HeadAssembly.Tilt(int.Parse(values[2]), false))
								rsp = UiConstants.OK + "," + values[1] + "," + values[2];
							else
								rsp = UiConstants.FAIL + ",tilt command exeuction failed";
						else
							rsp = UiConstants.FAIL + ",pan command execution failed";
						}
					else
						rsp = UiConstants.FAIL + ",bad format";
					}
				else if (msg.StartsWith(UiConstants.START_POS))
					{
					values = msg.Split(',');
					if (values.Length == 2)
						{

						try
						{
						val = int.Parse(values[1]);
						rsp = AutoRobotControl.MotionControl.SendCommand(BACKWARD + (START_POSITION - val));
						if (rsp.StartsWith(UiConstants.OK))
							rsp = UiConstants.OK;
						}

						catch(Exception)
						{
						rsp = UiConstants.FAIL + ",bad parameter";
						}

						}
					else
						rsp = UiConstants.FAIL + ",bad format";
					}
				else if (msg == UiConstants.NEXT_POS)
					{
					rsp = AutoRobotControl.MotionControl.SendCommand(BACKWARD + NEXT_POSITION);
					if (rsp.StartsWith(UiConstants.OK))
						rsp = UiConstants.OK;
					}
				else if (msg == UiConstants.SEND_DEPTH_MAP)
					{
					SendDepthMap(ep);
					rsp = "";
					}
				else if (msg == UiConstants.SEND_VIDEO)
					{
					rsp = UiConstants.OK + ",latter";
					connect.SendResponse(rsp, ep, true);
					SendVideo(ep);
					rsp = "";
					}
				else if (msg == UiConstants.SEND_VIDEO_PARAM)
					{
					if ((Kinect.nui != null) && (Kinect.nui.IsRunning))
						rsp = UiConstants.OK + "," + Kinect.nui.ColorStream.FrameWidth + "," + Kinect.nui.ColorStream.FrameHeight + "," + Kinect.nui.ColorStream.NominalHorizontalFieldOfView + "," + Kinect.nui.ColorStream.NominalVerticalFieldOfView;
					else
						rsp = UiConstants.FAIL;
					}
				else if (msg.StartsWith(UiConstants.TARGET_PROCESS_FRAME))
					{
					values = msg.Split(',');
					if (values.Length == 4)
						{

						try
						{
						val = int.Parse(values[1]);
						val2 = int.Parse(values[2]);
						val3 = int.Parse(values[3]);
						connect.SendResponse(UiConstants.OK, ep, true);
						TargetProcessFrame(val,val2,val3,ep);
						rsp = "";
						}

						catch(Exception)
						{
						rsp = UiConstants.FAIL + ",bad parameter";
						}

						}
					else
						rsp = UiConstants.FAIL + ",bad format";
					}
				else if (msg == UiConstants.LIGHT_AMP)
					{
					if (light_amp > 0)
						rsp = UiConstants.OK + "," + light_amp;
					else
						rsp = UiConstants.FAIL;
					}
				else
					rsp = UiConstants.FAIL + ",unknown command";
				}
			else
				rsp = UiConstants.FAIL + ",not initialized";
			return(rsp);
		}

		}
	}
