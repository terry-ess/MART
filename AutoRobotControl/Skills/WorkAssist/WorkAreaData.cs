using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using AutoRobotControl;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Blob;
using MathNet.Numerics.LinearAlgebra.Double;
using BuildingDataBase;


namespace Work_Assist
	{
	class WorkAreaData
		{

		// assumptions
		//		1. the orientation of the work area is parallel or perpendicular to a "prime" direction (0,90,180,270)
		//		2. the person is at the work location [THIS IS KEY, the person is the absolute reference in terms of location of work area otherwise need extensive room context]
		//		3. "come here" used to get close to person therefore the current robot location is
		//			a. within room
		//			b. as close to the person as permitted with map based navigation
		//			c. know ~ location of person
		//			d. will be on same side of work area as person if person is not in "tight quarters"
		//			e. for edge or same side topologies within a single segment direct move OR a SLAM movement to the work location

		private const double MIN_HEIGHT_LIMIT = 2;
		private const double M_TO_IN = 1000 * SharedData.MM_TO_IN;
		private const int MIN_PERSON_DIST = 45;

		private PersonDetect pd = new PersonDetect();
		private DepthImagePixel[] depthdata = new DepthImagePixel[Kinect.nui.DepthStream.FramePixelDataLength];
		private byte[] videodata = new byte[Kinect.nui.ColorStream.FramePixelDataLength];
		private Bitmap map_pic = null;
		private CvBlobs blobs = null;
		private IplImage pic, gs, img;
		private SkillShared.work_space_arrange top;
		private SharedData.RobotLocation side = SharedData.RobotLocation.FRONT;
		private int prime_direct = 400;


		private bool SaveSipsData(string fname,SkeletonPoint[] sips)

		{
			bool rtn = false;
			BinaryWriter bw;
			int i;
			short value;

			fname += ".pc";
			bw = new BinaryWriter(File.Open(fname, FileMode.Create));
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
				rtn = true;
				}
			return (rtn);
		}


		
		private bool CaptureKinectPics()

		{
			bool rtn = false;
			DateTime now = DateTime.Now;
			string fname;
			Bitmap bm;
			ArrayList scan = new ArrayList();

			if (Kinect.GetColorFrame(ref videodata, 40) && Kinect.GetDepthFrame(ref depthdata, 60))
				{
				Kinect.nui.CoordinateMapper.MapDepthFrameToSkeletonFrame(Kinect.nui.DepthStream.Format, depthdata, SkillShared.sips);
				fname = Log.LogDir() + "Work area data " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "-" + SharedData.GetUFileNo();
				rtn = true;
				bm = videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				bm.RotateFlip(RotateFlipType.Rotate180FlipY);
				bm.Save(fname + ".jpg", ImageFormat.Jpeg);
				Log.LogEntry("Saved " + fname + ".jpg");
				SaveSipsData(fname, SkillShared.sips);
				if (Rplidar.CaptureScan(ref scan,true))
					Rplidar.SaveLidarScan(ref scan,"work area data ");
				}
			else
				Log.LogEntry("Could not capture the video or depth data.");
			return (rtn);
		}



		private bool RectContainsPoint(int x,int y,CvRect rect)

		{
			bool rtn = false;

			if (x >= rect.X)
				if (x <= rect.X + rect.Width)
					if (y >= rect.Y)
						if (y <= rect.Y + rect.Height)
							rtn = true;
			return(rtn);
		}



		private bool CircleOverlapsRect(Point cpt,int radius,CvRect rect)

		{
			bool rtn = false;
			Point pt = new Point();
			int i;

			pt = cpt;
			if (RectContainsPoint(pt.X,pt.Y,rect))
				rtn = true;
			else
				{
				pt.Y = cpt.Y;
				for (i = 0; i < 2 * radius;i++) 
					{
					pt.X = cpt.X - radius + i;
					if (RectContainsPoint(pt.X, pt.Y, rect))
						{
						rtn = true;
						break;
						}
					}
				if (! rtn)
					{
					pt.X = cpt.X;
					for (i = 0; i < 2 * radius; i++)
						{
						pt.Y = cpt.Y - radius + i;
						if (RectContainsPoint(pt.X, pt.Y, rect))
							{
							rtn = true;
							break;
							}
						}
					}
				}
			return (rtn);
		}



		private SkillShared.work_space_arrange DetermineTopology(int pdirect,int right,int left,int behind,int infront)

		{
			SkillShared.work_space_arrange top = SkillShared.work_space_arrange.NONE;
			int max1,max2,max;

			max1 = Math.Max(right,left);
			max2 = Math.Max(behind,infront);
			max = Math.Max(max1,max2);
			if (pdirect == 270)
				{
				if (max == behind)
					top = SkillShared.work_space_arrange.SAME_SIDE;
				else if ((max == left) || (max == right))
					top = SkillShared.work_space_arrange.EDGE;
				else if (max == infront)
					top = SkillShared.work_space_arrange.OPPOSITE_SIDE;
				}
			return(top);
		}



		private bool AnalyzeData()	//the depth data is taken with the kinect looking directly at the person

		{
			bool rtn = false;
			int row, col, idepth, iwidth, pixel, right = 0, left = 0, behind = 0, infront = 0, rot_angle,pan,dir;
			Arm.Loc3D pt;
			byte[] bdata;
			double depth, width, kh, hdlimit, h,tilt,tcorrect;
			CvBlob b = null;
			Point pc_rpt, rpt,pc_pt;
			NavData.location cl;
			string fname;
			DateTime now = DateTime.Now;
			Graphics g;
			Rectangle brect = new Rectangle();
			const int DEPTH_DIST_BEYOND_PERSON = 36;


			SkillShared.OutputSpeech("Analyzing data");
			if (SpeakerData.Face.detected)
				depth = SpeakerData.Face.dist;
			else
				depth = SpeakerData.Person.dist;
			depth += DEPTH_DIST_BEYOND_PERSON;
			width = depth * Math.Sin(31 * SharedData.DEG_TO_RAD);
			idepth = (int) Math.Ceiling(depth);
			iwidth = (int) Math.Ceiling(width);
			bdata = new byte[idepth * 2 * iwidth * 4];
			hdlimit = MIN_HEIGHT_LIMIT;
			Log.LogEntry("Forward limit (in): " + depth.ToString("F2"));
			Log.LogEntry("Width (in): " + width.ToString("F2"));
			tilt = HeadAssembly.TiltAngle();
			tcorrect = ((double)4 / 55) * tilt;
			Log.LogEntry("Tilt: " + (tilt + tcorrect).ToString("F2"));
			pc_pt = new Point(iwidth,DEPTH_DIST_BEYOND_PERSON);
			Log.LogEntry("Person center point: " + pc_pt);
			pan = HeadAssembly.PanAngle();
			cl = NavData.GetCurrentLocation();
			dir = (cl.orientation + pan) % 360;
			if (dir < 0)
				dir += 360;
			prime_direct = SkillShared.PrimeDirection(dir);
			rot_angle = (cl.orientation + pan - prime_direct) % 360;
			if (rot_angle < 0)
				rot_angle += 360;
			Log.LogEntry("Rotation angle: " + rot_angle);
			pc_rpt = SkillShared.RotatePoint(pc_pt,rot_angle);
			kh = Arm.KinectHeight(tilt + tcorrect);
			for (row = 0; row < Kinect.nui.ColorStream.FrameHeight; row++)
				{
				for (col = 0; col < Kinect.nui.ColorStream.FrameWidth; col++)
					{
					pt = SkillShared.PtLocation(row, col,tcorrect);
					if (pt.z > 0)
						{
						if (Math.Abs(pt.x) <= width)
							{
							if (pt.z <= depth)
								{
								if ((pt.x == 0) && (col != 319))
									{
									}
								else
									{
									h = (kh + pt.y);
									pixel = ((int) ((idepth - Math.Floor(pt.z)) * (2 * iwidth)) + (iwidth - (int) Math.Floor(-pt.x))) * 4;
									if ((h > hdlimit) && (pixel < idepth * 2 * iwidth * 4))
										{
										bdata[pixel] = 255;
										bdata[pixel + 1] = 255;
										bdata[pixel + 2] = 255;
										}
									}
								}
							}
						}
					}
				}
			map_pic = bdata.ToBitmap(2 * iwidth,idepth);
			pic = new IplImage(2 * iwidth, idepth, BitDepth.U8, 3);
			gs = new IplImage(pic.Size, BitDepth.U8, 1);
			img = new IplImage(pic.Size, BitDepth.F32, 1);
			blobs = new CvBlobs();
			pic = map_pic.ToIplImage();
			Cv.CvtColor(pic, gs, ColorConversion.BgrToGray);
			blobs.Label(gs, img);
			Log.LogEntry(blobs.Count + " blobs found");
			foreach (KeyValuePair<uint, CvBlob> item in blobs)
				{
				b = item.Value;
				if ((b.Area > 100) && CircleOverlapsRect(pc_pt,12,b.Rect))
					{
					brect = new Rectangle(b.Rect.X,b.Rect.Y,b.Rect.Width,b.Rect.Height);
					Log.LogEntry("Blob @ " + brect + " contains person" );
					for (row = b.Rect.Y; row < b.MaxY;row++)
						for (col = b.Rect.X;col < b.MaxX;col++)
							{
							pixel = (row * 2 * iwidth + col) * 4;
							if ((bdata[pixel] == 255) && (NavCompute.DistancePtToPt(pc_pt,new Point(col,row)) > SkillShared.PERSON_RADIUS))
								{
								rpt = SkillShared.RotatePoint(new Point(col,row),rot_angle);
								if (rpt.X < pc_rpt.X)
									left += 1;
								else if (rpt.X > pc_rpt.X)
									right += 1;
								if (rpt.Y < pc_rpt.Y)
									behind += 1;
								else if (rpt.Y > pc_rpt.Y)
									infront += 1;
								}
							}
					Log.LogEntry("   right: " + right);
					Log.LogEntry("   left: " + left);
					Log.LogEntry("   behind: " + behind);
					Log.LogEntry("   infront: " + infront);
					rtn = true;
					break;
					}
				}
			if (rtn)
				{
				top = DetermineTopology(prime_direct,right,left,behind,infront);
				if ((top == SkillShared.work_space_arrange.SAME_SIDE) || (top == SkillShared.work_space_arrange.EDGE))
					{
					if (left > right)
						side = SharedData.RobotLocation.LEFT;
					else
						side = SharedData.RobotLocation.RIGHT;
					}
				}
			else
				Log.LogEntry("Blob analysis produced no result.");
			g = Graphics.FromImage(map_pic);
			g.DrawEllipse(Pens.Red, pc_pt.X - SkillShared.PERSON_RADIUS, pc_pt.Y - SkillShared.PERSON_RADIUS, 2 * SkillShared.PERSON_RADIUS, 2 * SkillShared.PERSON_RADIUS);
			g.FillRectangle(Brushes.Red, pc_pt.X - 1, pc_pt.Y - 1, 1, 1);
			if (rtn)
				g.DrawRectangle(Pens.Blue, brect);
			fname = Log.LogDir() + "Point cloud map with speaker circle " + +now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".bmp";
			map_pic.Save(fname, ImageFormat.Bmp);
			Log.LogEntry("Saved: " + fname);
			return (rtn);
		}



		private bool DetermineWorkAreaParam(NavData.location cl,Point pcoord)

		{
			bool rtn = false;
			string msg,rsp,name = "";
			DataTable dt;
			int i;
			short tp,sd;
			Point pcpt;
			string[] coord;

			if (top != SkillShared.work_space_arrange.NONE)
				{
				dt = WorkSpaceDAO.WorkSpaceList(cl.rm_name);
				for (i = 0;i < dt.Rows.Count;i++)
					{
					name = (string) dt.Rows[0][0];
					tp = (short) dt.Rows[0][1];
					sd = (short) dt.Rows[0][2];
					coord = ((string) dt.Rows[0][3]).Split(',');
					if (coord.Length == 2)
						{
						pcpt = new Point(int.Parse(coord[0]), int.Parse(coord[1]));
						if (((short) top == tp ) && ((short) side == sd))
							{
							if (NavCompute.DistancePtToPt(pcoord,pcpt) <= SkillShared.PERSON_RADIUS)
								{
								msg = "Are we using the " + cl.rm_name + " " + name + " work area?";
								rsp = Speech.Conversation(msg, "responseyn", 5000, true);
								if (rsp == "yes")
									{
									SkillShared.wsd.name = name;
									SkillShared.wsd.room = cl.rm_name;
									SkillShared.wsd.arrange = top;
									SkillShared.wsd.side = (SharedData.RobotLocation) sd;
									SkillShared.wsd.person_coord = pcpt;
									SkillShared.wsd.edge_perp_direct = (short) dt.Rows[0][4];
									SkillShared.wsd.top_height = (double) dt.Rows[0][5];
									SkillShared.wsd.existing_area = true;
									SkillShared.wsd.prime_direct = prime_direct;
									Log.LogEntry("Work area matched with work space " + name);
									rtn = true;
									}
								}
							}
						}
					}
				if (!rtn)
					{
					if (name.Length == 0)
						name = "workspace 1";
					else
						{
						name = name.Substring("workspace ".Length - 1);
						name = "workspace " + (int.Parse(name) + 1);
						}
					SkillShared.wsd.name = name;
					if (top == SkillShared.work_space_arrange.SAME_SIDE)
						{
						if (side == SharedData.RobotLocation.RIGHT)
							msg = "It looks like the work area has a topology of same side with me to your right.  Is that correct?";
						else
							msg = "It looks like the work area has a topology of same side with me to your left.  Is that correct?";
						}
					else if (top == SkillShared.work_space_arrange.EDGE)
						if (side == SharedData.RobotLocation.RIGHT)
							msg = "It looks like the work area has a topology of edge with the work area to my right.  Is that correct?";
						else
							msg = "It looks like the work area has a topology of edge with the work area to my left.  Is that correct?";
					else
						msg = "It looks like the work area has a topology of opposite side.  Is that correct?";
					rsp = Speech.Conversation(msg, "responseyn", 5000, true);
					if (rsp != "yes")
						top = SkillShared.work_space_arrange.NONE;
					else
						{
						SkillShared.wsd.room = cl.rm_name;
						SkillShared.wsd.arrange = top;
						SkillShared.wsd.side = side;
						SkillShared.wsd.existing_area = false;
						SkillShared.wsd.prime_direct = prime_direct;
						rtn = true;
						}
					}
				}
			if (top == SkillShared.work_space_arrange.NONE)
				{
				msg = "Is the work area topology same side?";
				rsp = Speech.Conversation(msg, "responseyn", 5000, true);
				if (rsp != "yes")
					{
					msg = "Is the work area topology edge?";
					rsp = Speech.Conversation(msg, "responseyn", 5000, true);
					if (rsp != "yes")
						{
						msg = "Is the work area topology opposite side?";
						rsp = Speech.Conversation(msg, "responseyn", 5000, true);
						if (rsp == "yes")
							top = SkillShared.work_space_arrange.OPPOSITE_SIDE;
						}
					else
						top = SkillShared.work_space_arrange.EDGE;
					}
				else
					top = SkillShared.work_space_arrange.SAME_SIDE;
				if (top == SkillShared.work_space_arrange.SAME_SIDE)
					{
					msg = "Do you want me to your right?";
					rsp = Speech.Conversation(msg, "responseyn", 5000, true);
					if (rsp == "yes")
						side = SharedData.RobotLocation.RIGHT;
					else
						side = SharedData.RobotLocation.LEFT;
					}
				if (top == SkillShared.work_space_arrange.EDGE)
					{
					msg = "The edge is to my right?";
					rsp = Speech.Conversation(msg, "responseyn", 5000, true);
					if (rsp == "yes")
						side = SharedData.RobotLocation.RIGHT;
					else
						side = SharedData.RobotLocation.LEFT;

					}
				rtn = true;
				SkillShared.wsd.room = cl.rm_name;
				SkillShared.wsd.arrange = top;
				SkillShared.wsd.side = side;
				SkillShared.wsd.existing_area = false;
				SkillShared.wsd.prime_direct = prime_direct;
				}
			return (rtn);
		}



		private bool MoveBackwardToPoint(Point mp)

		{
			int sdist,ldist,dist;
			double cdist = 0;
			NavData.location cl;
			bool rtn = false;

			cl = NavData.GetCurrentLocation();
			dist = NavCompute.DistancePtToPt(cl.coord,mp);
			sdist = AutoRobotControl.MotionControl.GetSonarReading(SharedData.RobotLocation.REAR);
			if (LS02CLidar.RearClearence(ref cdist,SharedData.ROBOT_WIDTH + 2))
				{
				ldist = (int) Math.Round(cdist);
				sdist = Math.Min(sdist,ldist);
				}
			if (sdist > dist)
				rtn = SkillShared.MoveBackward(dist,cl);
			else
				Log.LogEntry("MoveBackwardToPt has insufficent rear clearence of " + sdist + " in.");
			return(rtn);
		}



		private int FindObstacles(int shift_angle)

		{
			int mdist = 0;
			ArrayList sdata = new ArrayList(),obs = new ArrayList();

			if ((Rplidar.CaptureScan(ref sdata, true)))
				{
				mdist = Rplidar.FindObstacles(shift_angle, -1, sdata,1,false, ref obs);
				Rplidar.SaveLidarScan(ref sdata);
				}
			else
				Log.LogEntry("Could not obtain RPLIDAR scan");
			return(mdist);
		}




		private bool PositionForDataCollect(int direct,int dist,ref PersonDetect.scan_data fd,ref PersonDetect.scan_data pdd)

		{
			bool rtn = false,positioned = false;				//how does this impact return to start?
			int pdirect;
			Room.rm_location rl;
			int ang,pan,shift;
			NavData.location cl;
			NavCompute.pt_to_pt_data ppd;
			MoveToStart mts = new Work_Assist.MoveToStart();

			SkillShared.OutputSpeech("Positioning for data collection.");
			cl = NavData.GetCurrentLocation();
			pdirect = SkillShared.PrimeDirection(direct);
			shift = NavCompute.AngularDistance(cl.orientation, (pdirect + 180) % 360);
			if (NavCompute.ToRightDirect(cl.orientation, pdirect))
				shift *= -1;
			if (FindObstacles(shift) > dist + SharedData.ROBOT_LENGTH)
				{
				ang = NavCompute.AngularDistance(cl.orientation, pdirect);
				if (NavCompute.ToRightDirect(cl.orientation, pdirect))
					ang *= -1;
				rl = NavCompute.PtDistDirectApprox(cl.coord, ang,dist);
				Log.LogEntry("revised postion: " + rl.coord);
				if (mts.TurnToFaceBackwardMP(rl.coord))
					positioned = MoveBackwardToPoint(rl.coord);
				}
			else
				positioned = SkillShared.MoveBackward(dist,cl);
			if (positioned)
				{
				cl = NavData.GetCurrentLocation();
				ppd = NavCompute.DetermineRaDirectDistPtToPt(SpeakerData.Person.rm_location, cl.coord);
				pan = NavCompute.AngularDistance(cl.orientation, ppd.direc);
				if (!NavCompute.ToRightDirect(cl.orientation, ppd.direc))
					pan *= -1;
				HeadAssembly.Pan(pan, true);
				if (SkillShared.FindSpeakerFace(pan, ref pdd, ref fd))
					{
					SpeakerData.Person = pdd;
					SpeakerData.Face = fd;
					rtn = true;
					}
				else
					{
					SkillShared.OutputSpeech("Could not find the speaker.");
					SpeakerData.ClearPersonFace();
					}
				}
			return (rtn);
		}



		public bool GatherWorkAreaData()
		
		{
			NavData.location cl;
			NavCompute.pt_to_pt_data ppd;
			int pan,tilt,x,y;
			PersonDetect.scan_data pdd = PersonDetect.Empty(), fd = PersonDetect.Empty();
			double angle;
			Stopwatch sw = new Stopwatch();
			bool positioned = false,rtn = false;

			try
			{
			SkillShared.OutputSpeech("Collecting work area data");
			cl = NavData.GetCurrentLocation();
			ppd = NavCompute.DetermineRaDirectDistPtToPt(SpeakerData.Person.rm_location, cl.coord);
			if (ppd.dist < MIN_PERSON_DIST)
				{
				if ((positioned = PositionForDataCollect(ppd.direc, MIN_PERSON_DIST - ppd.dist,ref fd,ref pdd)))
					cl = NavData.GetCurrentLocation();
				}
			else
				{
				pan = NavCompute.AngularDistance(cl.orientation, ppd.direc);
				if (!NavCompute.ToRightDirect(cl.orientation, ppd.direc))
					pan *= -1;
				HeadAssembly.Pan(pan, true);
				if (SkillShared.FindSpeakerFace(pan, ref pdd, ref fd))
					{
					SpeakerData.Person = pdd;
					SpeakerData.Face = fd;
					positioned = true;
					}
				else
					{
					SkillShared.OutputSpeech("Could not find the speaker.");
					SpeakerData.ClearPersonFace();
					}
				}
			if (positioned)
				{
				if (fd.detected)
					{
					x = fd.vdo.x + fd.vdo.width / 2;
					y = fd.vdo.y;
					}
				else
					{
					x = pdd.vdo.x + pdd.vdo.width/2;
					y = pdd.vdo.y;
					}
				x = Kinect.nui.ColorStream.FrameWidth - x;
				pan = HeadAssembly.PanAngle();
				angle = Kinect.VideoHorDegrees(x - Kinect.nui.ColorStream.FrameWidth / 2);
				angle = pan + angle;
				if (Math.Abs(angle) > 1)
					{
					Log.LogEntry("Pan: " + ((int)Math.Round(angle)));
					HeadAssembly.Pan((int)Math.Round(angle), true);
					}
				
				if (y > 2 * Kinect.DIPS_MARGIN)
					{
					tilt = HeadAssembly.TiltAngle();
					angle = Math.Abs((Kinect.nui.ColorStream.NominalVerticalFieldOfView / 2) - Kinect.VideoVerDegrees((Kinect.nui.ColorStream.FrameHeight / 2) - (y - Kinect.DIPS_MARGIN)));
					angle = tilt + angle;
					if (angle > 1)
						{
						Log.LogEntry("Tilt: " + ((int)-Math.Round(angle)));
						HeadAssembly.Tilt((int)-Math.Round(angle), true);
						}
					}
				if (CaptureKinectPics())
					{
					SkillShared.OutputSpeech("Data captured.");
					AnalyzeData();
					DetermineWorkAreaParam(cl,fd.rm_location);
					SkillShared.wsd.LogWSD();
					SkillShared.OutputSpeech("Work area analysis completed.");
					//if not expected top/side determine positioning needed (if any) to get detail information and move to this position; is that scenerio possible with "Come here"
					rtn = true;
					}
				else
					SkillShared.OutputSpeech("Could not obtain work area data.");
				}
			else
				SkillShared.OutputSpeech("Could not position to gather work area data.");
			}

			catch (Exception ex)
			{
			SkillShared.OutputSpeech("Work area data exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			Skills.ReturnFailed();
			}

			HeadAssembly.Pan(0, true);
			HeadAssembly.Tilt(0, true);
			return(rtn);
			}

		}
	}
