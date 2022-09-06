using System;
using System.Drawing;
using System.IO;
using Microsoft.Kinect;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using AutoRobotControl;
using Constants;
using Coding4Fun.Kinect.WinForm;

namespace Sensor_Alignment
	{
	class VisualSensorAlign
		{

		private static byte[] videodata = null;
		private static UiConnection connect = null;


		public static bool Open(UiConnection con)

		{
			bool rtn = false;

			if ((Kinect.nui != null) && Kinect.nui.IsRunning && (Lidar.Status() == Lidar.lidar_status.BOTH))
				{
				connect = con;
				videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
				rtn = true;
				}
			return(rtn);
		}



		public static void Close()

		{
			videodata = null;
			connect = null;
		}



		private static void StreamVideo(IPEndPoint rcvr)

		{
			Bitmap bm;
			MemoryStream ms;
			String cmd,rsp;

			if (Kinect.GetColorFrame(ref videodata,40))
				{
				bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				ms = new MemoryStream();
				bm.Save(ms,System.Drawing.Imaging.ImageFormat.Jpeg);
				cmd = UiConstants.VIDEO_FRAME + "," + ms.Length;
				rsp = connect.SendCommand(cmd, 20,rcvr);
				if (rsp.StartsWith(UiConstants.OK))
					connect.SendStream(ms,rcvr);
				}
			else
				connect.SendResponse(UiConstants.FAIL + ",could not obtain image",rcvr,true);
		}



		public static string MessageHandler(string msg, IPEndPoint ep)

		{
			string rsp = "";
			string[] values;
			MemoryStream ms;

			if ((videodata != null) && (connect != null))
				{
				if (msg == UiConstants.SEND_VIDEO)
					{
					rsp = UiConstants.OK + ",latter";
					connect.SendResponse(rsp, ep,true);
					StreamVideo(ep);
					rsp = "";
					}
				else if (msg.StartsWith(UiConstants.DETERMINE_ANGLES + ","))
					{
					int dc,dr;
					double ha,va;

					values = msg.Split(',');
					if (values.Length == 3)
						{
						try
						{
						dc = int.Parse(values[1]);
						dr = int.Parse(values[2]);
						ha = Kinect.VideoHorDegrees(dc);
						va = Kinect.VideoVerDegrees(dr);
						rsp = UiConstants.OK + "," + ha.ToString("F2") + "," + va.ToString("F2");
						}

						catch (Exception)
						{
						rsp = UiConstants.FAIL + ",bad format";
						}

						}
					else
						rsp = UiConstants.FAIL + ",bad format";
					}
				else if (msg == UiConstants.SEND_FRONT_LIDAR)
					{
					ms = new MemoryStream();
					if (Rplidar.RCaptureScan(ref ms))
						{
						connect.SendResponse(UiConstants.OK, ep, true);
						msg = UiConstants.LIDAR_FRAME + "," + ms.Length;
						rsp = connect.SendCommand(msg, 20, ep);
						if (rsp.StartsWith(UiConstants.OK))
							connect.SendStream(ms, ep);
						}
					else
						rsp = UiConstants.FAIL;
					}
				else if (msg == UiConstants.SEND_REAR_LIDAR)
					{
					ms = new MemoryStream();
					if (LS02CLidar.RCaptureScan(ref ms))
						{
						connect.SendResponse(UiConstants.OK, ep, true);
						msg = UiConstants.LIDAR_FRAME + "," + ms.Length;
						rsp = connect.SendCommand(msg, 20, ep);
						if (rsp.StartsWith(UiConstants.OK))
							connect.SendStream(ms, ep);
						}
					else
						rsp = UiConstants.FAIL;
					}
				else if (msg == UiConstants.SEND_VIDEO_PARAM)
					{
					if ((Kinect.nui != null) && (Kinect.nui.IsRunning))
						rsp = UiConstants.OK + "," + Kinect.nui.ColorStream.FrameWidth + "," + Kinect.nui.ColorStream.FrameHeight + "," + Kinect.nui.ColorStream.NominalHorizontalFieldOfView + "," + Kinect.nui.ColorStream.NominalVerticalFieldOfView;
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
