using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Coding4Fun.Kinect.WinForm;
using Microsoft.Kinect;
using MathNet.Numerics.Statistics;

namespace AutoRobotControl
	{
	public class PersonDetect
		{

		public const int PERSON_ID = 1;
		public const string PERSON_MODEL_NAME = "people";
		public const int FACE_ID = 0;
		public const string FACE_MODEL_NAME = "face";
		public const double SCORE_LIMIT = .6;

		public struct scan_data
		{
			public VisualObjectDetection.visual_detected_object vdo;
			public bool detected;
			public double dist;
			public double angle;
			public Point rm_location;
			public long ts;
		};

		public static int dist_error = Kinect.nui.DepthStream.TooFarDepth + 1;

		byte[] videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
		DepthImagePixel[] depthdata = new DepthImagePixel[Kinect.nui.DepthStream.FramePixelDataLength];
		DepthImagePoint[] dips = new DepthImagePoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];


		private bool Between(int angle,int right_angle,int left_angle)

		{
			bool rtn = false;

			if ((angle < right_angle) && (angle > left_angle))
				rtn = true;
			return(rtn);
		}


		public static scan_data Empty()

		{
			scan_data esd = new scan_data();

			esd.detected = false;
			esd.dist = -1;
			esd.angle = -1;
			esd.rm_location = new Point(0,0);
			esd.ts = -1;
			return(esd);
		}


		public bool ObstacleIsPerson(int center_angle)

		{
			bool rtn = false;
			ArrayList pdlist;
			Bitmap bm;
			Graphics g;
			string fname;
			int i,ar,al,col;
			VisualObjectDetection.visual_detected_object vdo;

			Log.LogEntry("ObstacleIsPerson: " + center_angle);
			if (Kinect.GetColorFrame(ref videodata, 40))
				{
				fname = Log.LogDir() + "obstacle pic " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				pdlist = VisualObjectDetection.DetectObject(bm,PERSON_MODEL_NAME,SCORE_LIMIT,PERSON_ID);
				if (SharedData.log_operations)
					{
					g = System.Drawing.Graphics.FromImage(bm);
					col = (int) Math.Round((Kinect.avg_hor_pixals_per_degree * -center_angle) + ((double) Kinect.nui.ColorStream.FrameWidth / 2));
					g.DrawLine(Pens.Red, col, 0, col, 479);
					File.Delete(fname);
					bm.Save(fname, ImageFormat.Jpeg);
					}
				for (i = 0;i < pdlist.Count;i++)
					{
					vdo = (VisualObjectDetection.visual_detected_object) pdlist[i];
					al = (int) Math.Round(Kinect.VideoHorDegrees((int) Math.Round(vdo.x - ((double)Kinect.nui.ColorStream.FrameWidth / 2))));
					ar = (int) Math.Round(Kinect.VideoHorDegrees((int) Math.Round((vdo.x + vdo.width) - ((double)Kinect.nui.ColorStream.FrameWidth / 2))));
					if (Between(-center_angle,ar,al))
						{
						rtn = true;
						Log.LogEntry("Person obstacle detected at [" + vdo.x + "," + vdo.y + "," + vdo.width + "," + vdo.height + "] in " + fname);
						break;
						}
					}
				}
			return(rtn);
		}



		public int FindDist(int row,int col,int width,ref Bitmap bm,bool graphics,Pen pen)

		{
			int i,start,end,tries = 0,samples = 0, min_dist = dist_error,dist = 0,bad_pts = 0;
			Graphics g = null;
			const int MAX_DEPTH_RETRY = 3;
			ArrayList dal = new ArrayList();
			double[] data;
			double p25,p75,med,maxl,minl;

			try 
			{
			start = row * Kinect.nui.ColorStream.FrameWidth + col + 2;
			end = start + width;
			if (graphics)
				g = System.Drawing.Graphics.FromImage(bm);
			do
				{
				for (i = start; i < end; i++)
					{
					samples += 1;
					dist = dips[i].Depth;
					if (dist > 0)
						{
						dal.Add((double) dist);
						}
					else
						bad_pts += 1;
					if (g != null)
						g.DrawRectangle(pen, col + i - start - 1, row - 1, 2, 2);
					}
				if ((dal.Count == 0) || (bad_pts/samples > .8))     //have detected garbage depth scan, so re-scan and try again
					{
					Log.LogEntry("Could not determine distance to object. [ last distance " + dist + " mm, " + samples + " samples taken, " + bad_pts + " bad points ]");
					tries += 1;
					samples = 0;
					bad_pts = 0;
					dal.Clear();
					if ((tries < MAX_DEPTH_RETRY) && (Kinect.GetDepthFrame(ref depthdata, 40)))
						Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, depthdata, dips);
					else
						tries = MAX_DEPTH_RETRY;
					}
				}
			while ((dal.Count == 0) && (tries < MAX_DEPTH_RETRY));
			if (dal.Count > 0)
				{
				data = new double[dal.Count];
				dal.CopyTo(data);
				p25 = MathNet.Numerics.Statistics.ArrayStatistics.PercentileInplace(data,25);
				p75 = MathNet.Numerics.Statistics.ArrayStatistics.PercentileInplace(data, 75);
				med = ArrayStatistics.MedianInplace(data);
				maxl = med + (1.5 * (p75-p25));
				minl = med - (1.5 * (p75-p25));
				for (i = 0;i < data.Length;i++)
					{
					if ((data[i] > minl) && (data[i] < min_dist))
							min_dist = (int) data[i];
					}
				}
			}

			catch(Exception ex)
			{
			Log.LogEntry("FindDist exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			Log.LogEntry("Parameters: " + row + "  " + col + "  " + width);
			min_dist = Kinect.nui.DepthStream.MaxDepth;
			}

			return(min_dist);
		}



		public bool NearestHCLPerson(bool near,ref scan_data pd,bool graphics = true,bool log = true)

		{
			bool rtn = false;
			ArrayList pdlist;
			Bitmap bm;
			int i,col,pangle,min_pangle = 30,pindx = -1,row,min_dist = Kinect.nui.DepthStream.MaxDepth;
			VisualObjectDetection.visual_detected_object vdo;
			DateTime now = DateTime.Now;
			Stopwatch sw = new Stopwatch();

			sw.Start();
			if (log)
				Log.LogEntry("NearestPerson");
			pd.detected = false;
			if (near)
				Kinect.SetNearRange();
			if (Kinect.GetColorFrame(ref videodata, 40) && (Kinect.GetDepthFrame(ref depthdata, 40)))
				{
				bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				pdlist = VisualObjectDetection.DetectObject(bm,PERSON_MODEL_NAME,SCORE_LIMIT,PERSON_ID,log);
				if (pdlist.Count > 0)
					{
					for (i = 0; i < pdlist.Count; i++)
						{
						vdo = (VisualObjectDetection.visual_detected_object) pdlist[i];
						pangle = (int)Math.Round(Kinect.VideoHorDegrees((int)Math.Round((vdo.x + ((double) vdo.width/2)) - ((double)Kinect.nui.ColorStream.FrameWidth / 2))));
						if (Math.Abs(pangle) < min_pangle)
							{
							min_pangle = Math.Abs(pangle);
							pindx = i;
							}
						}
					pd.vdo = (VisualObjectDetection.visual_detected_object) pdlist[pindx];
					Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, depthdata, dips);
					row = pd.vdo.y + (pd.vdo.height / 2);
					col = pd.vdo.x;
					min_dist = FindDist(row,col, pd.vdo.width - 4,ref bm,graphics && SharedData.log_operations, Pens.Red);
					pd.angle = Kinect.VideoHorDegrees((int) Math.Round((pd.vdo.x + pd.vdo.width/2) - ((double)Kinect.nui.ColorStream.FrameWidth / 2)));
					if (min_dist < Kinect.nui.DepthStream.MaxDepth)
						pd.dist = Kinect.CorrectedDistance(min_dist * SharedData.MM_TO_IN) / Math.Cos(Math.Abs(pd.angle) * SharedData.DEG_TO_RAD);
					else
						pd.dist = Kinect.nui.DepthStream.TooFarDepth * SharedData.MM_TO_IN;
					if (log)
						{
						Log.LogEntry("Person dist: " + pd.dist);
						Log.LogEntry("Person angle: " + pd.angle);
						}
					pd.detected = true;
					rtn = true;
					}
				else
					Log.LogEntry("No people detected");
				if (graphics && SharedData.log_operations)
					SavePersonData("Nearest person", pd,bm,Pens.Red);
				else if (!rtn)
					{
					string fname = Log.LogDir() + "Nearest person pic " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
					SavePic(fname,pd,bm,Pens.Red);
					}
				}
			else
				Log.LogEntry("NearestPerson could not get picture or depth data.");
			if (near)
				Kinect.SetFarRange();
			sw.Stop();
			if (sw.ElapsedMilliseconds > 100)
				Log.LogEntry("Nearest person took " + sw.ElapsedMilliseconds + " ms");
			return (rtn);
		}



		public bool SingleFace(Bitmap bm,ref scan_data fd)

		{
			ArrayList pdlist;
			bool rtn = false;

			pdlist = VisualObjectDetection.DetectObject(bm,FACE_MODEL_NAME,SCORE_LIMIT,FACE_ID);
			if (pdlist.Count == 1)
				{
				fd.vdo = (VisualObjectDetection.visual_detected_object) pdlist[0];
				rtn = true;
				}
			else
				{
				Log.LogEntry("Face detection failed with " + pdlist.Count + " faces detected");
				fd.detected = false;
				}
			return (rtn);
		}



		public bool NearestHCLPersonFace(bool near,ref scan_data pd,ref scan_data fd,bool graphics = true)

		{
			bool rtn = false;
			ArrayList pdlist;
			Bitmap bm,pbm;
			int i,col,pangle,min_pangle = 30,pindx = -1,row,min_dist = Kinect.nui.DepthStream.MaxDepth;
			VisualObjectDetection.visual_detected_object vdo;
			DateTime now = DateTime.Now;
			Rectangle rect;


			Log.LogEntry("NearestPersonFace");
			pd.detected = false;
			fd.detected = false;
			if (near)
				Kinect.SetNearRange();
			if (Kinect.GetColorFrame(ref videodata, 40) && (Kinect.GetDepthFrame(ref depthdata, 40)))
				{
				bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				pdlist = VisualObjectDetection.DetectObject(bm,PERSON_MODEL_NAME,SCORE_LIMIT,PERSON_ID);
				if (pdlist.Count > 0)
					{
					for (i = 0; i < pdlist.Count; i++)
						{
						vdo = (VisualObjectDetection.visual_detected_object) pdlist[i];
						pangle = (int)Math.Round(Kinect.VideoHorDegrees((int)Math.Round((vdo.x + ((double) vdo.width/2)) - ((double)Kinect.nui.ColorStream.FrameWidth / 2))));
						if (Math.Abs(pangle) < min_pangle)
							{
							min_pangle = Math.Abs(pangle);
							pindx = i;
							}
						}
					pd.vdo = (VisualObjectDetection.visual_detected_object) pdlist[pindx];
					Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, depthdata, dips);
					row = pd.vdo.y + (pd.vdo.height / 2);
					col = pd.vdo.x;
					min_dist = FindDist(row,col, pd.vdo.width - 4,ref bm,graphics, Pens.Red);
					if (min_dist < Kinect.nui.DepthStream.MaxDepth)
						{
						pd.detected = true;
						pd.angle = Kinect.VideoHorDegrees((int) Math.Round((pd.vdo.x + pd.vdo.width/2) - ((double)Kinect.nui.ColorStream.FrameWidth / 2)));
						pd.dist = Kinect.CorrectedDistance(min_dist * SharedData.MM_TO_IN) / Math.Cos(Math.Abs(pd.angle) * SharedData.DEG_TO_RAD);
						Log.LogEntry("Person dist: " + pd.dist);
						Log.LogEntry("Person angle: " + pd.angle);
						rtn = true;
						rect = new Rectangle();
						rect.X = pd.vdo.x;
						rect.Y = pd.vdo.y;
						rect.Width = pd.vdo.width;
						rect.Height = pd.vdo.height;
						pbm = bm.Clone(rect,System.Drawing.Imaging.PixelFormat.Format24bppRgb);
						if (SingleFace(pbm,ref fd))
							{
							fd.vdo.x += pd.vdo.x;
							fd.vdo.y += pd.vdo.y;
							fd.angle = Kinect.VideoHorDegrees((int)Math.Round((fd.vdo.x + fd.vdo.width / 2) - ((double)Kinect.nui.ColorStream.FrameWidth / 2)));
							row = fd.vdo.y + (fd.vdo.height / 2);
							col = fd.vdo.x;
							min_dist = FindDist(row, col, fd.vdo.width - 4, ref bm, graphics, Pens.Blue);
							if (min_dist < Kinect.nui.DepthStream.MaxDepth)
								{
								fd.dist = Kinect.CorrectedDistance(min_dist * SharedData.MM_TO_IN) / Math.Cos(Math.Abs(fd.angle) * SharedData.DEG_TO_RAD);
								fd.detected = true;
								Log.LogEntry("Face dist: " + fd.dist);
								Log.LogEntry("Face angle: " + fd.angle);
								}
							else
								fd.detected = false;
							}
						}
					}
				else
					Log.LogEntry("No people detected");
				if (SharedData.log_operations)
					SavePersonFaceData("Nearest person and face", pd,fd,bm);
				}
			else
				Log.LogEntry("NearestPerson could not get picture or depth data.");
			if (near)
				Kinect.SetFarRange();
			return (rtn);
		}



		public bool FindFace(bool near,ref scan_data fd,bool graphics = true)

		{
			bool rtn = false;
			Bitmap bm;
			int row,col, min_dist = Kinect.nui.DepthStream.MaxDepth;

			Log.LogEntry("FindFace");
			fd.detected = false;
			if (near)
				Kinect.SetNearRange();
			if (Kinect.GetColorFrame(ref videodata, 40) && (Kinect.GetDepthFrame(ref depthdata, 40)))
				{
				bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, depthdata, dips);
				if (SingleFace(bm, ref fd))
					{
					fd.angle = Kinect.VideoHorDegrees((int)Math.Round((fd.vdo.x + fd.vdo.width / 2) - ((double)Kinect.nui.ColorStream.FrameWidth / 2)));
					row = fd.vdo.y + (fd.vdo.height / 2);
					col = fd.vdo.x;
					min_dist = FindDist(row, col, fd.vdo.width - 4, ref bm, graphics, Pens.Blue);
					if (min_dist < Kinect.nui.DepthStream.MaxDepth)
						{
						fd.dist = Kinect.CorrectedDistance(min_dist * SharedData.MM_TO_IN) / Math.Cos(Math.Abs(fd.angle) * SharedData.DEG_TO_RAD);
						fd.detected = true;
						Log.LogEntry("Face dist: " + fd.dist);
						Log.LogEntry("Face angle: " + fd.angle);
						rtn = true;
						}
					else
						Log.LogEntry("Could not determined face distance");
					}
				else
					Log.LogEntry("Could not detect a face.");
				if (SharedData.log_operations)
					SavePersonData("Face", fd, bm,Pens.Blue);
				}
			else
				Log.LogEntry("FindFace could not get picture or depth data.");
			if (near)
				Kinect.SetFarRange();
			return (rtn);
		}



		private void SavePic(string fname, scan_data pd, Bitmap bm, Pen pen)

		{
			int col,row;
			Graphics g;

			g = System.Drawing.Graphics.FromImage(bm);
			col = (int)Math.Round((double)Kinect.nui.ColorStream.FrameWidth / 2);
			g.DrawLine(Pens.Red, col, 0, col, 479);
			row = (int)Math.Round((double)Kinect.nui.ColorStream.FrameHeight / 2);
			g.DrawLine(Pens.Red, 0, row, 639, row);
			if (pd.detected)
				g.DrawRectangle(pen, pd.vdo.x, pd.vdo.y, pd.vdo.width, pd.vdo.height);
			bm.RotateFlip(RotateFlipType.Rotate180FlipY);
			bm.Save(fname,ImageFormat.Jpeg);
			Log.LogEntry("Saved " + fname);
		}

		
		private void SavePersonData(string title,scan_data pd,Bitmap bm,Pen pen)

		{
			string fname;
			DateTime now = DateTime.Now;
			BinaryWriter bw;
			int i;

			fname = Log.LogDir() + title + " pic " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
			SavePic(fname,pd,bm,Pens.Red);
			fname = Log.LogDir() + title + " depth data " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".bin";
			bw = new BinaryWriter(File.Open(fname,FileMode.Create));
			for (i = 0; i < dips.Length; i++)
				bw.Write((short) dips[i].Depth);
			bw.Close();
			Log.LogEntry("Saved " + fname);
		}



		private void SavePersonFaceData(string title,scan_data pd,scan_data fd,Bitmap bm)

		{
			string fname;
			DateTime now = DateTime.Now;
			BinaryWriter bw;
			int i,col,row;
			Graphics g;

			fname = Log.LogDir() + title + " pic " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
			g = System.Drawing.Graphics.FromImage(bm);
			col = (int)Math.Round((double)Kinect.nui.ColorStream.FrameWidth / 2);
			g.DrawLine(Pens.Red, col, 0, col, 479);
			row = (int)Math.Round((double)Kinect.nui.ColorStream.FrameHeight / 2);
			g.DrawLine(Pens.Red, 0, row, 639, row);
			if (pd.detected)
				g.DrawRectangle(Pens.Red, pd.vdo.x, pd.vdo.y, pd.vdo.width, pd.vdo.height);
			if (fd.detected)
				g.DrawRectangle(Pens.Blue, fd.vdo.x, fd.vdo.y, fd.vdo.width, fd.vdo.height);
			bm.RotateFlip(RotateFlipType.Rotate180FlipY);
			bm.Save(fname,ImageFormat.Jpeg);
			Log.LogEntry("Saved " + fname);
			fname = Log.LogDir() + title + " depth data " + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".bin";
			bw = new BinaryWriter(File.Open(fname,FileMode.Create));
			for (i = 0; i < dips.Length; i++)
				bw.Write((short) dips[i].Depth);
			bw.Close();
			Log.LogEntry("Saved " + fname);
		}



		public DepthImagePoint[] LastDepthImage()

		{
			return(dips);
		}

		}
	}
