using System;
using System.Drawing;
using System.Collections;
using System.IO;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;


namespace AutoRobotControl
	{

	public class HandDetect
		{

		public const int HAND_ID = 1;
		public const string HAND_MODEL_NAME = "hand";
		public const string HAND_ROBOT_ARM_MODEL_NAME = "hand_robotarm";
		public const double SCORE_LIMIT = .6;



		public struct hand_data
		{
			public VisualObjectDetection.visual_detected_object vdo;
			public double dist;
			public double angle;
		};

		byte[] videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
		DepthImagePixel[] depthdata = new DepthImagePixel[Kinect.nui.DepthStream.FramePixelDataLength];
		DepthImagePoint[] dips = new DepthImagePoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];



		public ArrayList DetectHands()

		{
			ArrayList rtn = new ArrayList();

			Log.LogEntry("DetectHands");
			if (Kinect.GetColorFrame(ref videodata, 40) && (Kinect.GetDepthFrame(ref depthdata, 40)))
				{
				Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, depthdata, dips);
				rtn = DetectHands(ref videodata,ref dips);
				}
			else
				Log.LogEntry("Could not obtain a video or depth frame.");
			return(rtn);
		}


		public ArrayList DetectHands(ref byte[] videodata,ref DepthImagePoint[] dips)

		{
			ArrayList hdlist = new ArrayList();
			ArrayList rlist = new ArrayList();
			Bitmap bm;
			Graphics g;
			string fname;
			int i,j,k,dist = 0,basey,basex,col,row;
			hand_data hdata = new hand_data();
			DateTime now = DateTime.Now;

			Log.LogEntry("DetectHands");
			bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
			hdlist = VisualObjectDetection.DetectObject(bm,HAND_MODEL_NAME,SCORE_LIMIT,HAND_ID);
			g = System.Drawing.Graphics.FromImage(bm);

			try
			{
			for (k = 0;k < hdlist.Count;k++)
				{
				hdata.vdo = (VisualObjectDetection.visual_detected_object) hdlist[k];
				hdata.angle = (int)Math.Round(Kinect.VideoHorDegrees((int)Math.Round((hdata.vdo.x + ((double) hdata.vdo.width / 2)) - ((double)Kinect.nui.ColorStream.FrameWidth / 2))));
				hdata.dist = -1;
				basey = hdata.vdo.y + (hdata.vdo.height / 2);
				basex = hdata.vdo.x + (hdata.vdo.width / 2);
				g.DrawRectangle(Pens.Red, hdata.vdo.x, hdata.vdo.y, hdata.vdo.width, hdata.vdo.height);
				for (i = -5; i < 5; i++)
					{
					for (j = -5; j < 5; j++)
						{
						g.DrawRectangle(Pens.Red, basex + i, basey + j, 1, 1);
						dist = dips[((basey + i) * Kinect.nui.ColorStream.FrameWidth) + (basex + j)].Depth;
						if ((dist >= Kinect.nui.DepthStream.MinDepth) && (dist < Kinect.nui.DepthStream.MaxDepth))
							{
							hdata.dist = Kinect.CorrectedDistance(dist * SharedData.MM_TO_IN) / Math.Cos(Math.Abs(hdata.angle) * SharedData.DEG_TO_RAD);
							rlist.Add(hdata);
							break;
							}
						}
					if ((dist >= Kinect.nui.DepthStream.MinDepth) && (dist < Kinect.nui.DepthStream.MaxDepth))
						break;
					}
				}
			}

			catch(Exception ex)
			{
			Log.LogEntry("DetectHands exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			if (SharedData.log_operations)
				{
				fname = Log.LogDir() + "hand detect pic " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				g = System.Drawing.Graphics.FromImage(bm);
				col = (int)Math.Round((double)Kinect.nui.ColorStream.FrameWidth / 2);
				g.DrawLine(Pens.Red, col, 0, col, 479);
				row = (int)Math.Round((double)Kinect.nui.ColorStream.FrameHeight / 2);
				g.DrawLine(Pens.Red, 0, row, 639, row);
				bm.RotateFlip(RotateFlipType.Rotate180FlipY);
				bm.Save(fname, ImageFormat.Jpeg);
				Log.LogEntry("Saved " + fname);
				}
			return (rlist);
		}



		public bool DetectHandRobotArm(ref byte[] videodata, ref DepthImagePoint[] dips,ref ArrayList hands,ref ArrayList robot_arm)

		{
			bool rtn = false;
			ArrayList halist;
			hand_data hadata = new hand_data();
			int i,j,k,basey,basex,dist,pixel;
			Bitmap bm;
			string fname;
			DateTime now = DateTime.Now;
			const int HAND_ID = 1;
			Graphics g;

			Log.LogEntry("DetectHandRobotArm");
			bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
			g = Graphics.FromImage(bm);
			halist = VisualObjectDetection.DetectObject(bm,HAND_ROBOT_ARM_MODEL_NAME,SCORE_LIMIT,0);
			for (k = 0;k < halist.Count;k++)
				{
				rtn = true;
				hadata.vdo = (VisualObjectDetection.visual_detected_object) halist[k];
				hadata.angle = (int)Math.Round(Kinect.VideoHorDegrees((int) Math.Round((hadata.vdo.x + ((double)hadata.vdo.width / 2)) - ((double)Kinect.nui.ColorStream.FrameWidth / 2))));
				hadata.dist = -1;
				basey = hadata.vdo.y + (hadata.vdo.height / 2);
				basex = hadata.vdo.x + (hadata.vdo.width / 2);
				for (i = -5; i < 5; i++)
					{
					for (j = -5; j < 5; j++)
						{
						pixel = ((basey + i) * Kinect.nui.ColorStream.FrameWidth) + (basex + j);
						dist = dips[pixel].Depth;
						if (dist > 0)
							{
							hadata.dist = Kinect.CorrectedDistance(dist * SharedData.MM_TO_IN) / Math.Cos(Math.Abs(hadata.angle) * SharedData.DEG_TO_RAD);
							break;
							}
						}
					}
				if (hadata.vdo.object_id == HAND_ID)
					{
					hands.Add(hadata);
					g.DrawRectangle(Pens.Red, hadata.vdo.x, hadata.vdo.y, hadata.vdo.width, hadata.vdo.height);
					}
				else
					{
					robot_arm.Add(hadata);
					g.DrawRectangle(Pens.Blue, hadata.vdo.x, hadata.vdo.y, hadata.vdo.width, hadata.vdo.height);
					}
				}
			if (SharedData.log_operations)
				{
				fname = Log.LogDir() + "hand-robotarm detect pic " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				bm.RotateFlip(RotateFlipType.Rotate180FlipY);
				bm.Save(fname, ImageFormat.Jpeg);
				Log.LogEntry("Saved " + fname);
				}
			return (rtn);
		}

		}
	}
