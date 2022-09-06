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
	static class HeadTilt
		{

		private static byte[] videodata = null;
		private static UiConnection connect = null;
		private static DepthImagePixel[] depthdata = new DepthImagePixel[Kinect.nui.DepthStream.FramePixelDataLength];
		private static DepthImagePoint[] dips = new DepthImagePoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];


		public static bool Open(UiConnection con)

		{
			bool rtn = false;

			if ( (Kinect.nui != null) && Kinect.nui.IsRunning && HeadAssembly.Connected())
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
//				if (ms.Length > 20000)
					{
					cmd = UiConstants.VIDEO_FRAME + "," + ms.Length;
					rsp = connect.SendCommand(cmd, 20,rcvr);
					if (rsp.StartsWith(UiConstants.OK))
						connect.SendStream(ms,rcvr);
					}
//				else
//					connect.SendResponse(UiConstants.FAIL + ",short image",rcvr,true);
				}
			else
				connect.SendResponse(UiConstants.FAIL + ",could not obtain image",rcvr,true);
		}



		private static string HeadTiltTable()

		{
			string rsp,fname,line;
			TextReader tr;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + Documents.HEAD_TILT_TABLE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				if (tr != null)
					{
					rsp = UiConstants.OK;
					while ((line = tr.ReadLine()) != null)
						rsp += "," + line;
					tr.Close();
					}
				else
					rsp = UiConstants.FAIL + ",could not access table";
				}
			else
				rsp = UiConstants.FAIL + ",table does not exist";
			return(rsp);
		}



		private static string SaveHeadTiltTable(string msg)

		{
			string rsp, fname;
			TextWriter tw;
			string[] values;
			int i;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + Documents.HEAD_TILT_TABLE;
			values = msg.Split(',');
			if (values.Length == 15)
				{
				tw = File.CreateText(fname);
				if (tw != null)
					{
					rsp = UiConstants.OK;
					for (i = 1; i < 15;i += 2)
						{
						tw.WriteLine(values[i] + "," + values[i + 1]);
						}
					tw.Close();
					}
				else
					rsp = UiConstants.FAIL + ",could not create table";
				}
			else
				rsp = UiConstants.FAIL + ",wrong number parameters";
			return(rsp);
		}



		private static void SendDepthMap(IPEndPoint ep)

		{
			MemoryStream ms;
			BinaryWriter bw;
			string cmd,rsp;
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


		public static string MessageHandler(string msg, IPEndPoint ep)

		{
			string rsp = "";
			int val;
			string[] values;

			if ((videodata != null) && (connect != null))
				{
				if (msg == UiConstants.SEND_VIDEO)
					{
					rsp = UiConstants.OK + ",latter";
					connect.SendResponse(rsp, ep,true);
					StreamVideo(ep);
					rsp = "";
					}
				else if (msg == UiConstants.CURRENT_PAN)
					{
					val = HeadAssembly.PanAngle();
					rsp = UiConstants.OK + "," + val;
					}
				else if (msg == UiConstants.CURRENT_TILT)
					{
					val = HeadAssembly.TiltAngle();
					rsp = UiConstants.OK + "," + val;
					}
				else if (msg.StartsWith(UiConstants.SET_PAN_TILT + ","))
					{
					values = msg.Split(',');
					if (values.Length == 3)
						{
						if (HeadAssembly.Pan(int.Parse(values[1]),true))
							if (HeadAssembly.Tilt(int.Parse(values[2]),false))
								rsp = UiConstants.OK + "," + values[1] + "," + values[2];
							else
								rsp = UiConstants.FAIL + ",tilt command exeuction failed";
						else
							rsp = UiConstants.FAIL + ",pan command execution failed";
						}
					else
						rsp = UiConstants.FAIL + ",bad format";
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
				else if (msg == UiConstants.SEND_VIDEO_PARAM)
					{
					if ((Kinect.nui != null) && (Kinect.nui.IsRunning))
						rsp = UiConstants.OK + "," + Kinect.nui.ColorStream.FrameWidth + "," + Kinect.nui.ColorStream.FrameHeight + "," + Kinect.nui.ColorStream.NominalHorizontalFieldOfView + "," + Kinect.nui.ColorStream.NominalVerticalFieldOfView;
					else
						rsp = UiConstants.FAIL;
					}
				else if (msg == UiConstants.SEND_DEPTH_MAP)
					{
					SendDepthMap(ep);
					rsp = "";
					}
				else if (msg == UiConstants.SEND_TILT_TABLE)
					rsp = HeadTiltTable();
				else if (msg.StartsWith(UiConstants.TILT_TABLE))
					rsp = SaveHeadTiltTable(msg);
				else
					rsp = UiConstants.FAIL + ",unknown command";
				}
			else
				rsp = UiConstants.FAIL + ",not initialized";
			return(rsp);
		}

		}
	}
