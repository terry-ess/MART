using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using AutoRobotControl;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Blob;
using OpenCvSharp.CPlusPlus;
using Constants;

namespace Work_Assist
	{

	class ArmManualMode
	{

		private UiConnection connect = null;
		private Thread srvr = null;
		private UiConnection kaconnect = null;
		private Thread kasrvr = null;
		private bool run = false;
		private AutoArm aa;


		public bool StartManualMode(AutoArm aap)

		{
			bool rtn = false;

			aa = aap;

			if (StartRemoteIntf())
				{
				Speech.SpeakAsync("okay");
				UiCom.SendActMessage(Constants.UiConstants.COMMAND_INPUT + "," + Constants.UiConstants.ARM_MANUAL_MODE);
				if (!StartKeepAlive())
					{
					SkillShared.OutputSpeech("Could not establish keep alive connection");
					CloseRemoteIntf(false, false);
					}
				else
					rtn = true;
				}
			return (rtn);
		}



		private void KaServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg;
			int no_msg_count = 0;

			Log.LogEntry("AutoArm keep alive server started.");
			while (run)
				{
				msg = kaconnect.ReceiveCmd(ref ep);
				if (run && (msg.Length > 0))
					{
					if (msg == UiConstants.KEEP_ALIVE)
						no_msg_count = 0;
					Thread.Sleep(10);
					}
				else
					{
					if (run)
						{
						no_msg_count += 1;
						if (no_msg_count < UiConstants.INTF_TIMEOUT_COUNT)
							Thread.Sleep(10);
						else
							{
							Log.LogEntry("AutoArm timed out.");
							CloseRemoteIntf(true,false);
							break;
							}
						}
					}
				}
			Log.LogEntry("AutoArm keep alive server closed");
		}



		private void StreamVideo(IPEndPoint rcvr)

		{
			Bitmap bm;
			MemoryStream ms;
			String cmd,rsp,fname;
			DateTime now = DateTime.Now;

			if (Kinect.GetColorFrame(ref SkillShared.videodata,40))
				{
				bm = SkillShared.videodata.ToBitmap(Kinect.nui.ColorStream.FrameWidth, Kinect.nui.ColorStream.FrameHeight);
				bm.RotateFlip(RotateFlipType.Rotate180FlipY);
				ms = new MemoryStream();
				bm.Save(ms,System.Drawing.Imaging.ImageFormat.Jpeg);
				fname = Log.LogDir() + "Manual mode pic" + now.Month + "." + now.Day + "." + now.Year + " " + now.Hour + "." + now.Minute + "-" + SharedData.GetUFileNo() + ".jpg";
				bm.Save(fname,ImageFormat.Jpeg);
				Log.LogEntry("Saved: " + fname);
				cmd = UiConstants.VIDEO_FRAME + "," + ms.Length;
				rsp = connect.SendCommand(cmd, 20,rcvr);
				if (rsp.StartsWith(UiConstants.OK))
					connect.SendStream(ms,rcvr);
				}
			else
				connect.SendResponse(UiConstants.FAIL + ",could not obtain image",rcvr,true);
		}



		private void SendDepthMap(IPEndPoint ep)

		{
			MemoryStream ms;
			BinaryWriter bw;
			string cmd,rsp;
			int i;

			if ((Kinect.nui != null) && (Kinect.nui.IsRunning))
				{
				if (Kinect.GetDepthFrame(ref SkillShared.depthdata, 40))
					{
					Kinect.nui.CoordinateMapper.MapColorFrameToDepthFrame(Kinect.nui.ColorStream.Format, Kinect.nui.DepthStream.Format, SkillShared.depthdata, SkillShared.dips);
					connect.SendResponse(UiConstants.OK, ep, true);
					ms = new MemoryStream();
					bw = new BinaryWriter(ms);
					for (i = 0;i < SkillShared.dips.Length;i++)
						bw.Write((short)SkillShared.dips[i].Depth);
					SkillShared.SaveDipsData("Manual mode depth ", SkillShared.dips);
					cmd = UiConstants.DEPTH_MAP + "," + ms.Length;
					rsp = connect.SendCommand(cmd, 20,ep);
					if (rsp.StartsWith(UiConstants.OK))
						{
						connect.SendStream(ms,ep);
						if (AAMap.MapWorkPlace())
							AAMap.SaveMap("Manual mode depth map ");
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



		private void UdpServer()

		{
			IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
			string msg, rsp = "";
			string[] val;
			int ival;
			string[] values;
			bool bval;

			Log.LogEntry("AutoArm control server started.");
			while (run)
				{

				try
				{
				msg = connect.ReceiveCmd(ref ep);
				if (run && (msg.Length > 0))
					{
					if (msg == UiConstants.HELLO)
						rsp = UiConstants.OK;
					else if (msg == UiConstants.CLOSE)
						{
						CloseRemoteIntf(false,true);
						rsp = "";
						}
					else if (msg == UiConstants.ARM_OFF)
						{
						Arm.ArmOff();
						rsp = UiConstants.OK;
						}
					else if (msg == UiConstants.SEND_VIDEO_PARAM)
						{
						if ((Kinect.nui != null) && (Kinect.nui.IsRunning))
							rsp = UiConstants.OK + "," + Kinect.nui.ColorStream.FrameWidth + "," + Kinect.nui.ColorStream.FrameHeight + "," + Kinect.nui.ColorStream.NominalHorizontalFieldOfView + "," + Kinect.nui.ColorStream.NominalVerticalFieldOfView;
						else
							rsp = UiConstants.FAIL;
						}
					else if (msg == UiConstants.SEND_ARM_WP_DATA)
						{
						rsp = UiConstants.OK + "," + Arm.ARM_Y_OFFSET + "," + (Arm.L2 + Arm.L3 + Arm.L4 + Arm.ARM_Z_OFFSET) + "," + SkillShared.wsd.top_height + "," + SkillShared.wsd.front_edge_dist;
						}
					else if (msg == UiConstants.SEND_VIDEO)
						{
						rsp = UiConstants.OK + ",latter";
						connect.SendResponse(rsp, ep,true);
						StreamVideo(ep);
						rsp = "";
						}
					else if (msg == UiConstants.SEND_DEPTH_MAP)
						{
						SendDepthMap(ep);
						rsp = "";
						}
					else if (msg.StartsWith(UiConstants.LOCATION_CALC))
						{
						val = msg.Split(',');
						if (val.Length == 3)
							{
							Arm.Loc3D  rc = SkillShared.DPtLocation(int.Parse(val[1]),int.Parse(val[2]),AAShare.ARM_KINECT_TILT_CORRECT);
							if ((rc.x == 0) && (rc.y == 0) && (rc.z == 0))
								rsp = UiConstants.FAIL + ",could not calc";
							else
								rsp = UiConstants.OK + "," + rc.x.ToString("F1") + "," + rc.y.ToString("F1") + "," + rc.z.ToString("F1");
							}
						else
							rsp = UiConstants.FAIL + ",wrong number parameters";
						}
					else if (msg.StartsWith(UiConstants.ARM_TO_PARK))
						{
						if (AAShare.arm_pos == AAShare.position.PARK)
							rsp = UiConstants.OK;
						else
							{
							val = msg.Split(',');
							if (val.Length == 2)
								{
								try
								{
								if (AAShare.arm_pos == AAShare.position.START)
									{
									Arm.EPark();
									AAShare.arm_pos = AAShare.position.PARK;
									}
								else
									{
									bval = bool.Parse(val[1]);
									if (!bval)
										{
										if (AutoRobotControl.Arm.StopPos())
											{
											rsp = UiConstants.OK;
											AAShare.arm_pos = AAShare.position.PARK;
											}
										else
											rsp = UiConstants.FAIL;
										}
									else
										{
										if (AAShare.RawMoveEntryExitPt())
											{
											Thread.Sleep(4000);
											if (Arm.StartPosOnly(AAShare.start_pos[0], AAShare.start_pos[1]))
												{
												Arm.EPark();
												AAShare.arm_pos = AAShare.position.PARK;
												rsp = UiConstants.OK;
												}
											else
												{
												rsp = UiConstants.FAIL;
												AAShare.arm_pos = AAShare.position.ENTRY_EXIT;
												}
											}
										else
											rsp = UiConstants.FAIL;
										}
									}
								}

								catch(Exception ex)
								{
								rsp = UiConstants.FAIL + "," + ex.Message;
								Log.LogEntry("Exception: " + ex.Message);
								Log.LogEntry("Stack trace: " + ex.StackTrace);
								}

								}
							else
								rsp = UiConstants.FAIL + ",bad parameters";
							}
						}
					else if (msg == UiConstants.ARM_TO_START)
						{
						bool pos = false;

						if (AAShare.arm_pos == AAShare.position.START)
							pos = true;
						else if (AAShare.arm_pos == AAShare.position.ENTRY_EXIT)
							pos = Arm.StartPosOnly(AAShare.start_pos[0], AAShare.start_pos[1]);
						else if (AAShare.arm_pos == AAShare.position.PARK)
							pos = Arm.StartPos(AAShare.start_pos[0],AAShare.start_pos[1]);
						if (pos)
							{
							AAShare.arm_pos = AAShare.position.START;
							rsp = UiConstants.OK;
							}
						else
							rsp = UiConstants.FAIL;
						}
					else if (msg == UiConstants.ARM_TO_EE)
						{
						bool pos = false;

						if ((AAShare.arm_pos == AAShare.position.IN_WS) || (AAShare.arm_pos == AAShare.position.START))
							pos = AAShare.RawMoveEntryExitPt();
						if (pos)
							{
							rsp = UiConstants.OK;
							AAShare.arm_pos = AAShare.position.ENTRY_EXIT;
							}
						else
							rsp = UiConstants.FAIL;
						}
					else if (msg.StartsWith(UiConstants.RAW_ARM_TO_POSITION))
						{
						if ((AAShare.arm_pos == AAShare.position.IN_WS) || (AAShare.arm_pos == AAShare.position.ENTRY_EXIT))
							{
							val = msg.Split(',');
							if (val.Length == 7)
								{
								string err = "";

								try
								{
								if (AutoRobotControl.Arm.RawPositionArm(double.Parse(val[1]),double.Parse(val[2]),double.Parse(val[3]),bool.Parse(val[4]),bool.Parse(val[5]),bool.Parse(val[6]),ref err))
									{
									rsp = UiConstants.OK;
									AAShare.arm_pos = AAShare.position.IN_WS;
									}
								else
									rsp = UiConstants.FAIL + "," + err;
								}

								catch(Exception ex)
								{
								rsp = UiConstants.FAIL + "," + ex.Message;
								Log.LogEntry("Exception: " + ex.Message);
								Log.LogEntry("Stack trace: " + ex.StackTrace);
								}

								}
							else
								rsp = UiConstants.FAIL + ",wrong number parameters";
							}
						else
							rsp = UiConstants.FAIL + ",arm not in work space or at entry position";
						}
					else if (msg.StartsWith(UiConstants.ARM_TO_POSITION))
						{
						if ((AAShare.arm_pos == AAShare.position.IN_WS) || (AAShare.arm_pos == AAShare.position.ENTRY_EXIT))
							{
							val = msg.Split(',');
							if (val.Length == 6)
								{
								string err = "";
								double mdist = 0;

								try
								{
								if (AutoRobotControl.Arm.PositionArm(double.Parse(val[1]),double.Parse(val[2]),double.Parse(val[3]),int.Parse(val[4]),bool.Parse(val[5]),ref mdist,ref err))
									{
									rsp = UiConstants.OK;
									AAShare.arm_pos = AAShare.position.IN_WS;
									}
								else
									rsp = UiConstants.FAIL + "," + err;
								}

								catch(Exception ex)
								{
								rsp = UiConstants.FAIL + "," + ex.Message;
								Log.LogEntry("Exception: " + ex.Message);
								Log.LogEntry("Stack trace: " + ex.StackTrace);
								}

								}
							else
								rsp = UiConstants.FAIL + ",wrong number parameters";
							}	
						else
							rsp = UiConstants.FAIL + ",arm not in work space or at entry position";
						}
					else if (msg == UiConstants.RUN_MAP_CORRECT)
						{
						AAMap.MapWorkPlace();
						AAMap.SaveMap("Prior to correction move map ");
						Arm.MapCorrect();
						AAMap.SaveMap("Corrected move map ");
						rsp = UiConstants.OK;
						}
					else if (msg.StartsWith(UiConstants.TEST_SWEEP))
						{
						if (AAShare.arm_pos == AAShare.position.IN_WS)
							{
							values = msg.Split(',');
							if (values.Length == 4)
								{
								Arm.Loc3D loc;

								try
								{
								loc.x = double.Parse(values[1]);
								loc.y = double.Parse(values[2]);
								loc.z = double.Parse(values[3]);
								if (Arm.TestSweep(loc))
									rsp = UiConstants.OK;
								else
									rsp = UiConstants.FAIL + ",sweep failed";
								}

								catch(Exception ex)
								{
								rsp = UiConstants.FAIL + "," + ex.Message;
								Log.LogEntry("Exception: " + ex.Message);
								Log.LogEntry("Stack trace: " + ex.StackTrace);
								}

								}
							else
								rsp = UiConstants.FAIL + ",wrong number parameters";
							}
						else
							rsp = UiConstants.FAIL + ",arm not positioned in work space";
						}
					else if (msg == UiConstants.CURRENT_PAN)
						{
						ival = HeadAssembly.PanAngle();
						rsp = UiConstants.OK + "," + ival;
						}
					else if (msg == UiConstants.CURRENT_TILT)
						{
						ival = HeadAssembly.TiltAngle();
						rsp = UiConstants.OK + "," + ival;
						}
					else if (msg.StartsWith(UiConstants.SET_PAN_TILT + ","))
						{
						values = msg.Split(',');
						if (values.Length == 3)
							{
							if (HeadAssembly.Pan(int.Parse(values[1]), true))
								if (HeadAssembly.Tilt(int.Parse(values[2]), false))
									rsp = UiConstants.OK + "," + values[1] + "," + values[2];
								else
									rsp = UiConstants.FAIL + ",tilt command exeuction failed";
							else
								rsp = UiConstants.FAIL + ",pan command execution failed";
							}
						else
							rsp = UiConstants.FAIL + ",wrong number parameters";
						}
					else
						rsp = UiConstants.FAIL + ",unknown command";
					if (rsp.Length > 0)
						connect.SendResponse(rsp,ep,true);
					}
				else
					{
					if (run)
						Thread.Sleep(10);
					}
				}

				catch(Exception ex)
				{
				Log.LogEntry("AutoArm UDP server exception: " + ex.Message);
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				CloseRemoteIntf(false,true);
				}

				}
			Log.LogEntry("AutoArm UDP server closed.");
		}



		private bool StartRemoteIntf()

		{
			bool rtn = false;

			connect = new UiConnection(Constants.UiConstants.TOOL_PORT_NO);
			if (connect.Connected())
				{
				run = true;
				srvr = new Thread(UdpServer);
				srvr.Start();
				rtn = true;
				}
			else
				{
				if (connect.Connected())
					connect.Close();
				connect = null;
				}
			return(rtn);
		}



		private bool StartKeepAlive()

		{
			bool rtn = false;

			kaconnect = new UiConnection(Constants.UiConstants.TOOL_KEEP_ALIVE_PORT_NO);
			if (kaconnect.Connected())
				{
				run = true;
				kasrvr = new Thread(KaServer);
				kasrvr.Start();
				rtn = true;
				}
			else
				{
				if (connect.Connected())
					connect.Close();
				connect = null;
				kaconnect = null;
				}
			return (rtn);
		}



		private void CloseRemoteIntf(bool keep_alive,bool udp_srvr)

		{
			run = false;
			if (!udp_srvr && (srvr != null) && srvr.IsAlive)
				srvr.Join();
			if (!keep_alive && (kasrvr != null) && kasrvr.IsAlive)
				kasrvr.Join();
			if (connect != null)
				{
				connect.Close();
				connect = null;
				}
			if (kaconnect != null)
				{
				kaconnect.Close();
				kaconnect = null;
				}
			AAShare.handle_speech = true;
			SkillShared.OutputSpeech("Manual mode terminated.");
		}

	}

	}
