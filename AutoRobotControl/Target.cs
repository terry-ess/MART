using System;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.Windows.Forms;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Blob;


namespace AutoRobotControl
	{

	public class Target : FeatureMatch
		{

		private const int MAX_DIST_DIF = 12;


		public struct dblob
			{
			public int x;
			public int y;
			public uint area;
			public Rectangle rect;
			public double dist;
			public double ra;
			};

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

		public const string TARGET_CAL_FILE = "targetvision";

		private target_data td = new target_data(-1);
		private byte[] videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
		private byte[] videodata2 = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
		private DepthImagePixel[] depthdata = new DepthImagePixel[Kinect.nui.DepthStream.FramePixelDataLength];
		private DepthImagePoint[] dips = new DepthImagePoint[Kinect.nui.ColorStream.FrameHeight * Kinect.nui.ColorStream.FrameWidth];
		private dblob adb;
		private IplImage pic = new IplImage(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight, BitDepth.U8, 3);
		private IplImage gs , img;
		private CvBlobs blobs = new CvBlobs();
		private bool initialized = false;
		private int max_la = 0,retry_la;
		private int bcount = 0;


		public bool ReadParameters(ref target_data td)

		{
			string fname,line;
			TextReader tr;
			bool rtn = false;
			string[] values;
			blue_filter bf = new blue_filter();

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + TARGET_CAL_FILE + SharedData.CAL_FILE_EXT;
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
				if (td.blue_filters.Count > 0)
					{
					max_la = bf.light_amplitude;
					if (td.blue_filters.Count > 1)
						{
						bf = (blue_filter) td.blue_filters[td.blue_filters.Count - 2];
						retry_la = (bf.light_amplitude + max_la)/2;
						}
					else
						retry_la = max_la;
					}
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
				}
			if (!rtn)
				Log.LogEntry("Could not read the target parameter file.");
			return(rtn);
		}



		public bool SaveParameters(ref target_data td)

		{
			bool rtn = false;
			string fname;
			TextWriter tw;
			int i;
			blue_filter bf;
			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + TARGET_CAL_FILE + SharedData.CAL_FILE_EXT;
			if (File.Exists(fname))
				File.Move(fname,Application.StartupPath + SharedData.CAL_SUB_DIR + TARGET_CAL_FILE + DateTime.Now.Ticks + SharedData.CAL_FILE_EXT);
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



		public bool AddBlueFilter(blue_filter bf)

		{
			bool rtn = false;
			string fname;
			TextWriter tw;
			target_data otd = new target_data(-1);
			int i;
			blue_filter obf;
			bool new_bf_written = false;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + TARGET_CAL_FILE + SharedData.CAL_FILE_EXT;
			if (File.Exists(fname))
				{
				ReadParameters(ref otd);
				File.Move(fname, Application.StartupPath + SharedData.CAL_SUB_DIR + TARGET_CAL_FILE + DateTime.Now.Ticks + SharedData.CAL_FILE_EXT);
				tw = File.CreateText(fname);
				if (tw != null)
					{
					for (i = 0;i < otd.blue_filters.Count;i++)
						{
						obf = (blue_filter) otd.blue_filters[i];
						if (!new_bf_written && (bf.light_amplitude <= obf.light_amplitude))
							{
							tw.WriteLine(bf.light_amplitude + "," + bf.intensity + "," + bf.blueness);
							new_bf_written = true;
							}
						if (bf.light_amplitude != obf.light_amplitude)
							tw.WriteLine(obf.light_amplitude + "," + obf.intensity + "," + obf.blueness);
						}
					if (!new_bf_written)
						tw.WriteLine(bf.light_amplitude + "," + bf.intensity + "," + bf.blueness);
					tw.WriteLine(otd.min_blob_area);
					tw.WriteLine(otd.target_width);
					tw.WriteLine(otd.target_height);
					tw.Close();
					rtn = true;
					}
				}
			return(rtn);
		}




		public bool DetermineThresholds(int la,ref target_data td,ref int brthreshold,ref int bluthreshold)

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



		private bool ProcessFrame(ref Bitmap bm,byte[] videodata,int min_blob_area,int brthreshold,int bluthreshold,bool depth_only,int era = -1,int edist = -1)

		{
			int i, color;
			double bright;
			CvMemStorage store = new CvMemStorage();
			CvBlob b = null;
			bool rtn = false,loc_criteria_meet;

			bcount = 0;
			for (i = 0; i < Kinect.nui.ColorStream.FramePixelDataLength; i += 4)
				{
				bright = (videodata[i + 2] + videodata[i + 1] + videodata[i]) / 3;
				color = (videodata[i] - videodata[i + 1]) + (videodata[i] - videodata[i + 2]); //blue
				if ((bright < brthreshold) && (color > bluthreshold))
					{
					videodata2[i] = 255;
					videodata2[i + 1] = 255;
					videodata2[i + 2] = 255;
					}
				else
					{
					videodata2[i] = 0;
					videodata2[i + 1] = 0;
					videodata2[i + 2] = 0;
					}
				}
			bm = videodata2.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
			pic = bm.ToIplImage();
			Cv.CvtColor(pic, gs, ColorConversion.BgrToGray);
			blobs.Label(gs, img);
			if (blobs.Count > 0)
				{
				bcount = 0;
				do
					{
					b = (CvBlob)blobs[blobs.GreaterBlob()];
					if (b.Area > min_blob_area)
						{
						double ra, dist, max_ra_diff = 0;

						ra = Kinect.VideoHorDegrees((int)Math.Round(b.Centroid.X - ((double)Kinect.nui.ColorStream.FrameWidth / 2)));
						if ((edist > 0) && (era > 0))
							{
							max_ra_diff = (Math.Atan((double)MAX_DIST_DIF / edist) * SharedData.RAD_TO_DEG);
							loc_criteria_meet =  (((b.Rect.Y + b.Rect.Height) > Kinect.nui.ColorStream.FrameHeight / 2) && (b.Rect.Y < Kinect.nui.ColorStream.FrameHeight / 2) && (Math.Abs(ra - era) < max_ra_diff));
							}
						else
							loc_criteria_meet = (((b.Rect.Y + b.Rect.Height) > Kinect.nui.ColorStream.FrameHeight / 2) && (b.Rect.Y < Kinect.nui.ColorStream.FrameHeight / 2));
						if (loc_criteria_meet)
							{
							adb.area = b.Area;
							adb.x = (int)b.Centroid.X;
							adb.y = (int)b.Centroid.Y;
							adb.rect.X = b.Rect.X;
							adb.rect.Y = b.Rect.Y;
							adb.rect.Width = b.Rect.Width;
							adb.rect.Height = b.Rect.Height;
							adb.ra = ra;
							if (Kinect.GetDepthFrame(ref depthdata, 40))
								{
								Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, depthdata, dips);
								dist = dips[(adb.y * Kinect.nui.ColorStream.FrameWidth) + adb.x].Depth;
								if ((dist >= Kinect.nui.DepthStream.MinDepth) && (dist < Kinect.nui.DepthStream.MaxDepth))
									{
									dist = Kinect.CorrectedDistance(dist * SharedData.MM_TO_IN) / Math.Cos(Math.Abs(adb.ra) * SharedData.DEG_TO_RAD);
									adb.dist = dist;
									rtn = true;
									}
								else if (!depth_only)
									{
									adb.dist = (int)Math.Round((double)td.target_height / adb.rect.Height);
									Log.LogEntry("LocateTarget: depth frame determined distance (" + dist + " mm) not usable, using perspective to calculate distance.");
									rtn = true;
									}
								else
									Log.LogEntry("LocateTarget: could not determine distance.");
								}
							else if (!depth_only)
								{
								adb.dist = (int)Math.Round((double)td.target_height / adb.rect.Height);
								Log.LogEntry("LocateTarget: could not obtain depth frame, using perspective to calculate distance.");
								rtn = true;
								}
							else
								Log.LogEntry("LocateTarget: could not determine distance.");
							break;
							}
						else
							{
							blobs.Remove(blobs.GreaterBlob());
							bcount += 1;
							Log.LogEntry("Target top above mid-line:" + ((b.Rect.Y + b.Rect.Height) > Kinect.nui.ColorStream.FrameHeight / 2));
							Log.LogEntry("Target bottom below mid_line: " + (b.Rect.Y < Kinect.nui.ColorStream.FrameHeight / 2));
							if ((edist > 0) && (era > 0))
								Log.LogEntry("Determined angle within limits: " + (Math.Abs(ra - era) < max_ra_diff));
							}
						}
					}
				while (!rtn && (b.Area > td.min_blob_area) && (bcount < 5));
				}
			return (rtn);
		}



		public bool LocateTarget(ref dblob db, ref Bitmap bm, byte[] videodata, int min_blob_area, int brthreshold, int bluthreshold)

		{
			bool rtn = false;

			Log.LogEntry("LocateTarget");
			if (initialized)
				{
				rtn = ProcessFrame(ref bm, videodata, td.min_blob_area, brthreshold, bluthreshold, false);
				if (rtn)
					db = adb;
				else
					{
					Log.LogEntry("Could not find target:");
					Log.LogEntry("  min blob area: " + min_blob_area);
					Log.LogEntry("  intensity threshold: " + brthreshold);
					Log.LogEntry("  blueness threshold: " + bluthreshold);
					if (SharedData.log_operations)
						{
						string fname;

						fname = Log.LogDir() + "Target picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.PIC_FILE_EXT;
						bm.Save(fname);
						Log.LogEntry("Saved " + fname);
						}
					}
				}
			else
				Log.LogEntry("Target not initialized.");
			return (rtn);
		}



		public bool LocateTarget()
            
        {
			int la;
			Bitmap bm = null;
			CvMemStorage store = new CvMemStorage();
			bool rtn = false,process_done = false,retry_done = false;
			int brthreshold = 0,bluthreshold = 0;

			Log.LogEntry("LocateTarget");
			if (initialized)
				{
				la = HeadAssembly.GetLightAmplitude();
				if (Kinect.GetColorFrame(ref videodata,40) && DetermineThresholds(la,ref td,ref brthreshold,ref bluthreshold))
					{
					do
						{
						rtn = ProcessFrame(ref bm,videodata,td.min_blob_area,brthreshold,bluthreshold,false);
						if (!rtn && !retry_done && (la >= max_la))
							{
							retry_done = true;
							DetermineThresholds(retry_la, ref td, ref brthreshold, ref bluthreshold);
							}
						else
							process_done = true;
						}
					while(!process_done);
					if (!rtn)
						{
						Log.LogEntry("Could not find target:");
						Log.LogEntry("  bcount: " + bcount);
						Log.LogEntry("  initial light amplitude: " + la);
						if (retry_done)
							Log.LogEntry("  retry light amplitude: " + retry_la);
						if (SharedData.log_operations)
							{
							string fname;

							fname = Log.LogDir() + "Target picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.PIC_FILE_EXT;
							bm.Save(fname);
							Log.LogEntry("Saved " + fname);
							}
						}
					else if (SharedData.log_operations)
						{
						Graphics g;
						string fname;

						g = System.Drawing.Graphics.FromImage(bm);
						g.DrawLine(Pens.Black, 319, 0, 319, 479);
						g.DrawLine(Pens.Black, 320, 0, 320, 479);
						g.DrawRectangle(Pens.Red, adb.rect);
						fname = Log.LogDir() + "Target picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.PIC_FILE_EXT;
						bm.Save(fname);
						Log.LogEntry("Saved " + fname);
						Log.LogEntry("Target light amplitude: " + la);
						Log.LogEntry("Retry: " + retry_done);
						}
					}
				else
					Log.LogEntry("Could not obtain frame or determine threshold.");
				}
			else
				Log.LogEntry("Target not initialized.");
			return(rtn);
		}



		private bool LocateTarget(int edist,int era)
            
        {
			int la;
			Bitmap bm = null;
			CvMemStorage store = new CvMemStorage();
			int brthreshold = 0, bluthreshold = 0;
			bool rtn = false, process_done = false, retry_done = false;

			Log.LogEntry("LocateTarget: " + edist + "  " + era);
			if (initialized)
				{
				la = HeadAssembly.GetLightAmplitude();
				try
				{
				if (Kinect.GetColorFrame(ref videodata, 40) && DetermineThresholds(la, ref td, ref brthreshold, ref bluthreshold))
					{
					do
						{
						rtn = ProcessFrame(ref bm,videodata,td.min_blob_area,brthreshold,bluthreshold,false,era,edist);
						if (!rtn && !retry_done && (la >= max_la))
							{
							retry_done = true;
							DetermineThresholds(retry_la, ref td, ref brthreshold, ref bluthreshold);
							}
						else
							process_done = true;
						}
					while (!process_done);
					if (!rtn)
						{
						Log.LogEntry("Could not find target:");
						Log.LogEntry("  bcount: " + bcount);
						Log.LogEntry("  initial light amplitude: " + la);
						if (retry_done)
							Log.LogEntry("  retry light amplitude: " + retry_la);
						if (SharedData.log_operations)
							{
							string fname;

							fname = Log.LogDir() + "Target picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.PIC_FILE_EXT;
							bm.Save(fname);
							Log.LogEntry("Saved " + fname);
							}
						}
					else if (SharedData.log_operations)
						{
						Graphics g;
						string fname;

						g = System.Drawing.Graphics.FromImage(bm);
						g.DrawLine(Pens.Black, 319, 0, 319, 479);
						g.DrawLine(Pens.Black, 320, 0, 320, 479);
						g.DrawRectangle(Pens.Red, adb.rect);
						fname = Log.LogDir() + "Target picture " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo() + SharedData.PIC_FILE_EXT;
						bm.Save(fname);
						Log.LogEntry("Saved " + fname);
						Log.LogEntry("Target light amplitude: " + la);
						Log.LogEntry("Retry: " + retry_done);
						}
					}
				else
					Log.LogEntry("Could not obtain frame or determine thresholds.");
				}

				catch(Exception ex)
				{
				rtn = false;
				Log.KeyLogEntry("LocateTarget exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				Log.LogEntry("Target site: " + ex.TargetSite);
				}
			}
		else
			Log.LogEntry("Target not initialized.");

			return(rtn);
		}



		public Room.feature_match MatchKinect(NavData.feature f,params object[] obj)

		{
			Room.feature_match fm = new Room.feature_match();
			bool target_located = false;
			int edist,era;

			fm.matched = false;
			if (initialized)
				{
				if ((f.type == NavData.FeatureType.TARGET) && initialized)
					{
					if ((obj != null) && (obj.Length == 2))
						{
						edist = (int) obj[0];
						era = (int) obj[1];
						target_located = LocateTarget(edist,era);
						}
					else
						target_located = LocateTarget();
					if (target_located)
						{
						fm.matched = true;
						fm.ra = adb.ra;
						fm.distance = (int) Math.Round(adb.dist);
						}
					else
						Log.LogEntry("Target.MatchKinect: Could not locate target.");
					}
				else
					Log.LogEntry("Target.MatchKinect: incorrect type " + f.type.ToString() + " or initialization state " + initialized.ToString());
				}
			else
				Log.LogEntry("Target not initialized.");
			return(fm);
		}



		public Target()
		
		{
			if (ReadParameters(ref td))
				{
				gs = new IplImage(pic.Size, BitDepth.U8, 1);
				img = new IplImage(pic.Size, BitDepth.F32, 1);
				initialized = true;
				}
			else
				Log.LogEntry("Target: could not read parameters.");
		}

		}
	}
