using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Kinect;
using MathNet.Numerics.LinearAlgebra.Double;

namespace AutoRobotControl
	{
	public static class Arm
		{

		public const int BASE_CHANNEL = 0;
		public const int BASE_INITIAL = 1675;
		public const int BASE_STOP = 1675;
		public const int BASE_SPEED = 100;
		public const int SHOULDER_CHANNEL = 2;
		public const int SHOULDER_INITIAL = 1435;
		public const int SHOULDER_STOP = 1440;
		public const int SHOULDER_SPEED = 100;
		public const int ELBOW_CHANNEL = 4;
		public const int ELBOW_INITIAL = 1700;
		public const int ELBOW_STOP = 1745;
		public const int ELBOW_SPEED = 100;
		public const int WRIST_CHANNEL = 6;
		public const int WRIST_INLINE = 2090;
		public const int WRIST_SPEED = 100;
		public const int MAX_WRIST_PWM = 2360;
		public const int GROTATE_CHANNEL = 8;
		public const int GROTATE_PERP = 1350;
		public const int GROTATE_PAR = 2275;
		public const int GROTATE_SPEED = 500;
		public const int GRIP_CHANNEL = 10;
		public const int GRIP_OPEN = 1000;
		public const int GRIP_CLOSE = 2300;
		public const int GRIP_SPEED = 1000;
		public const double L2 = 9.0;
		public const double L3 = 12.0;
		public const double L4 = 4.5;
		public const double BASE_PULSE_PD1 = 3.1667;
		public const double BASE_PULSE_PD2 = 3.2777;
		public const double SHOULDER_PULSE_PD = 3.0;
		public const double ELBOW_PULSE_PD = 3.3333;
		public const double WRIST_PULSE_PD = 3.2222;
		public const double GROTATE_PULSE_PD = 10.278;
		public const int BASE_0 = 1390;
		public const int BASE_90 = 1685;
		public const int BASE_180 = 1970;
		public const int SHOULDER_0 = 1165;
		public const int ELBOW_0 = 1210;

		public const double ARM_Y_OFFSET = 37.5;
		public const double ARM_Z_OFFSET = .25;
		public const double ARM_HA_ERROR = -3.23;

		private const string SERIAL_PARAM_FILE = "servocntl.param";
		private const int GRIP_INC = 100;
		public const int MAX_GRIP_VALUE = 150;
		public const int OBS_HEIGHT_CLEAR = 3; // PRELIMIARY CHECK INDICATES NEED AT LEAST 2.25 IN CLEARANCE TO ASSURE CLEAR SEE 1.1.2018 15.22.21 DATA SET. HOW MANY FALSE POSITIVES DUE TO THIS???
		public const double ARM_MOVE_RATE = 2.85; //arm move speed in in/sec
		private const double M_TO_IN = 1000 * SharedData.MM_TO_IN;

		private enum motor {BASE,SHOULDER,ELBOW};

		private static DenseVector ex = new DenseVector(new[] {1.0,0.0,0.0});
		private static DenseVector ey = new DenseVector(new[] { 0.0, 1.0, 0.0 });
		private static DenseVector ez = new DenseVector(new[] {0.0,0.0,1.0});

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

			rtn = this.x.ToString("F3") + ", " + this.y.ToString("F3") + ", " + this.z.ToString("F3") ;
			return(rtn);
		}


		};


		public struct ServoCommand
		{
		public int channel;
		public int pwm;


		public ServoCommand(int chn,int pw)

		{
			channel = chn;
			pwm = pw;
		}

		};
		

		public struct ArmAngles
		{
		public double base_angle;
		public double shoulder_angle;
		public double elbow_angle;
		public double wrist_angle;
		public double xr;
		public double yr;
		public double zr;
		};
		

		public static SerialPort sp = new SerialPort();
		public static string run_date_time_str = "";


		private static string ac_error = "";
		private static ArmAngles current_aa = new ArmAngles(),prior_aa = new ArmAngles();

		public delegate bool ObstacleCheckMethod(Loc3D loc1, Loc3D loc2);
		private static ObstacleCheckMethod oc;
		public delegate string SaveMapMethod(string title);
		private static SaveMapMethod sm;
		public delegate void CorrectMoveMapMethod(Loc3D loc);
		private static CorrectMoveMapMethod cmm;
		public delegate bool MoveExitPt();
		private static MoveExitPt mep;


		static Arm()

		{
			UnRegisterMapDelegates();
		}



		public static void RegisterMapDelegates(ObstacleCheckMethod ocm,SaveMapMethod smm, CorrectMoveMapMethod cmmm,MoveExitPt mepp)

		{
			oc = ocm;
			sm = smm;
			cmm = cmmm;
			mep = mepp;
		}



		public static void UnRegisterMapDelegates()

		{
			oc = ObstacleCheck;
			sm = SaveMap;
			cmm = CorrectMoveMap;
			mep = MoveExitPoint;
		}



		public static bool OpenServoController()

		{
			bool rtn = false;
			TextReader tr;
			string fname,port_name;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + SERIAL_PARAM_FILE;
			if (File.Exists(fname))
				{
				if (Relays.ASRelay(false))
					{
					Thread.Sleep(1000);
					tr = File.OpenText(fname);
					port_name = tr.ReadLine();
					tr.Close();

					try
					{
					sp.PortName = port_name;
					sp.BaudRate = 9600;
					sp.DataBits = 8;
					sp.StopBits = StopBits.One;
					sp.Parity = Parity.None;
					sp.NewLine = "\r";
					sp.ReadTimeout = 500;
					sp.Open();
					if (ReadAnalogInput() >= 0)
						{
						SharedData.arm_operational = true;
						rtn = true;
						}
					else
						{
						Log.LogEntry("Could not read analog input.");
						sp.Close();
						}
					}
				
					catch (Exception ex)
					{
					Log.LogEntry("OpenSerial exception: " + ex.Message);
					if (sp.IsOpen)
						sp.Close();
					}

					}
				else
					Log.LogEntry("Could not close SSC-32 relay.");
				}
			else
				Log.LogEntry("Could not open the serial port parameter file");
			return(rtn);
		}



		public static void CloseServoController()

		{
			if (sp.IsOpen)
				{
				sp.Close();
				Relays.ASRelay(true);
				}
		}



		public static bool Position(int channel,int pw,int s)

		{
			string cmd;
			bool rtn = false;

			cmd = "#" + channel + "p" + pw + "S" + s;
			try
			{
			sp.WriteLine(cmd);
			rtn = true;
			}

			catch (Exception ex)
			{
			Log.LogEntry("Exception: " + ex.Message);
			Log.LogEntry("Position exception: " + ex.Message);
			}
			
			return(rtn);
		}



		public static bool Position(ArrayList axis,int t)

		{
			int i;
			ServoCommand ac;
			string cmd = "";
			bool rtn = false;

			if (axis.Count > 0)
				{
				for (i = 0;i < axis.Count;i++)
					{
					ac = (ServoCommand) axis[i];
					cmd += "#" + ac.channel + "P" + ac.pwm;
					}
				cmd += "T" + t;
				try
				{
				sp.WriteLine(cmd);
				rtn = true;
				}

				catch (Exception ex)
				{
				Log.LogEntry("Exception: " + ex.Message);
				Log.LogEntry("Postion exception: " + ex.Message);
				}

				}
			else
				Log.LogEntry("No move data provided.");
			return(rtn);
		}



		public static int ReadAnalogInput()

		{
			int value = -1;

			try
			{
			sp.DiscardInBuffer();
			sp.WriteLine("VA");
			value = sp.ReadByte();
			}

			catch(Exception ex)
			{
			Log.LogEntry("ReadAnalogInput exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			return(value);
		}



		public static int ServoPwm(int channel)

		{
			int value = -1;

			try
			{
			sp.DiscardInBuffer();
			sp.WriteLine("QP" + channel);
			value = sp.ReadByte();
			}

			catch(Exception ex)
			{
			Log.LogEntry("ServoPwm exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			return(value * 10);
		}



		public static bool StartPos(int a2,int a3)

		{
			int pwm2,pwm3;
			ArrayList al = new ArrayList();
			bool rtn = false;

			Position(SHOULDER_CHANNEL,SHOULDER_INITIAL,SHOULDER_SPEED);
			Thread.Sleep(500);
			Position(WRIST_CHANNEL,WRIST_INLINE,WRIST_SPEED);
			Position(GROTATE_CHANNEL, GROTATE_PERP, GROTATE_SPEED);
			Position(GRIP_CHANNEL, GRIP_CLOSE, GRIP_SPEED);
			Position(BASE_CHANNEL, BASE_INITIAL, BASE_SPEED);
			Thread.Sleep(500);
			Position(ELBOW_CHANNEL,ELBOW_INITIAL,ELBOW_SPEED);
			Thread.Sleep(1000);
			rtn = true;
			al = MapAnglesToPwms(90,a2,a3);
			pwm2 = (int) al[1];
			pwm3 = (int) al[2];
			if (Position(SHOULDER_CHANNEL,pwm2,SHOULDER_SPEED))
				{
				Thread.Sleep(1000);
				if (Position(ELBOW_CHANNEL,pwm3,ELBOW_SPEED))
					{
					current_aa.base_angle = 90;
					current_aa.shoulder_angle = a2;
					current_aa.elbow_angle = a3;
					current_aa.wrist_angle = 0;
					rtn = true;
					}
				}
			return(rtn);
		}



		public static bool StartPosOnly(int a2,int a3)

		{
			int pwm2, pwm3;
			ArrayList al = new ArrayList();
			bool rtn = false;

			al = MapAnglesToPwms(90, a2, a3);
			pwm2 = (int)al[1];
			pwm3 = (int)al[2];
			if (Position(SHOULDER_CHANNEL, pwm2, SHOULDER_SPEED))
				{
				Thread.Sleep(500);
				if (Position(ELBOW_CHANNEL, pwm3, ELBOW_SPEED))
					{
					current_aa.base_angle = 90;
					current_aa.shoulder_angle = a2;
					current_aa.elbow_angle = a3;
					current_aa.wrist_angle = 0;
					rtn = true;
					}
				}
			return (rtn);
		}



		public static bool StopPos()

		{
			bool rtn = false;

			mep();
			EPark();
			rtn = true;
			return(rtn);
		}



		public static void EPark()

		{
			Position(BASE_CHANNEL, BASE_STOP, BASE_SPEED);
			Thread.Sleep(500);
			Position(SHOULDER_CHANNEL, SHOULDER_STOP, SHOULDER_SPEED);
			Thread.Sleep(5000);
			Position(ELBOW_CHANNEL, ELBOW_STOP, ELBOW_SPEED);
			Thread.Sleep(3000);
			Position(SHOULDER_CHANNEL, 0, SHOULDER_SPEED);
			Thread.Sleep(500);
			ArmOff();
		}



		public static void ArmOff()

		{
			Position(ELBOW_CHANNEL, 0, ELBOW_SPEED);
			Position(BASE_CHANNEL, 0, BASE_SPEED);
			Position(WRIST_CHANNEL, 0, WRIST_SPEED);
			Position(GROTATE_CHANNEL, 0, GROTATE_SPEED);
			Position(GRIP_CHANNEL, 0, GRIP_SPEED);
		}



		public static double KinectForwardDist(double htilt)     //at 0 tilt, the center front edge of the kinect is at robot coord (0,base Kinect height,0)
																					//tilt and cfa are down from the horizon (i.e. negative angles)
			{																			
			double cfa,cfd,x2;

			cfa = Math.Atan(SharedData.KINECT_FRONT_OFFSET / SharedData.KINECT_CENTER_OFFSET) * SharedData.RAD_TO_DEG;
			cfd = Math.Sqrt((SharedData.KINECT_CENTER_OFFSET * SharedData.KINECT_CENTER_OFFSET) + (SharedData.KINECT_FRONT_OFFSET * SharedData.KINECT_FRONT_OFFSET));
			x2 = (cfd * Math.Sin((-(cfa + htilt)) * SharedData.DEG_TO_RAD)) + SharedData.KINECT_FRONT_OFFSET;
			return (x2);
		}



		public static double KinectHeight(double htilt) // tilt and cfa are down from the horizon (i.e. negative angles)

		{
			double kh,y2,cfa,cfd;

			cfa = Math.Atan(SharedData.KINECT_FRONT_OFFSET / SharedData.KINECT_CENTER_OFFSET) * SharedData.RAD_TO_DEG;
			cfd = Math.Sqrt((SharedData.KINECT_CENTER_OFFSET * SharedData.KINECT_CENTER_OFFSET) + (SharedData.KINECT_FRONT_OFFSET * SharedData.KINECT_FRONT_OFFSET));
			y2 = cfd * Math.Cos((cfa + htilt) * SharedData.DEG_TO_RAD);
			kh = SharedData.BASE_KINECT_HEIGHT + (y2 - SharedData.KINECT_CENTER_OFFSET);
			return(kh);
		}



		public static Loc3D MapKCToRC(double x,double cdist,double tilt_correct,double va)

		{
			Loc3D loc = new Loc3D();
			double pdist;
			int tilt;

			loc.x = x;
			tilt = HeadAssembly.TiltAngle();
			pdist = cdist/Math.Cos(va * SharedData.DEG_TO_RAD);
			loc.z = (pdist * Math.Sin((90 + tilt + tilt_correct + va) * SharedData.DEG_TO_RAD));
			loc.y = KinectHeight(tilt + tilt_correct) - (loc.z/Math.Tan((90 + tilt + va + tilt_correct) * SharedData.DEG_TO_RAD));
			loc.z += KinectForwardDist(tilt + tilt_correct);
			return (loc);
		}




		private static ArrayList MapRCToAC(double x,double y,double z)

		{
			ArrayList ac = new ArrayList();

			ac.Add(x);
			ac.Add(z - ARM_Z_OFFSET);
			ac.Add(y - ARM_Y_OFFSET);
			return (ac);
		}



		private static Loc3D MapACToRC(double x, double y, double z)

		{
			Loc3D loc = new Loc3D();
			
			loc.x = x;
			loc.z = y + ARM_Z_OFFSET;
			loc.y = z + ARM_Y_OFFSET;
			return(loc);
		}



		private static bool CalcAngles(double x,double y,double z,bool wrist_inline,ref ArrayList angles)

		{
			bool rtn = false;
			double td,r,c,a,b,a1,a2,a3,a4,al,l3,pos_offset,x0,y0;
			const int A4_CORRECT = 12;
			const int A2_HIGH_LIMIT = 170;
			const int A2_LOW_LIMIT = -7;
			const int A1_LOW_LIMIT = 10;
			const int A1_HIGH_LIMIT = 170;

			ac_error = "";
			td = Math.Sqrt((x*x) + (y*y) + (z*z));
			if (wrist_inline)
				{
				al = L2 + L3 + L4;
				l3 = L3 + L4;
				pos_offset = 0;
				}
			else
				{
				pos_offset = L4;
				al = L2 + L3;
				l3 = L3;
				}
			if (td - pos_offset <= al)
				{
				a1 = Math.Atan2(y,x) * SharedData.RAD_TO_DEG;
				a1 += ARM_HA_ERROR;
				if ((a1 >= A1_LOW_LIMIT) && (a1 <= A1_HIGH_LIMIT))
					{
					r = Math.Sqrt((x*x) + (y*y)) - pos_offset;
					c = Math.Atan2(z, r) * SharedData.RAD_TO_DEG;
					x0 = r * Math.Cos(a1 * SharedData.DEG_TO_RAD);
					y0 = r * Math.Sin(a1 * SharedData.DEG_TO_RAD);
					td = Math.Sqrt((x0 * x0) + (y0 * y0) + (z * z));
					a = Math.Acos(((td*td) + (L2*L2) - (l3*l3)) / (2 * td * L2)) * SharedData.RAD_TO_DEG;
					a2 = a + c;
					if ((a2 > A2_LOW_LIMIT) && (a2 < A2_HIGH_LIMIT))
						{
						b = Math.Acos(((l3*l3) + (L2*L2) - (td*td)) / (2 * l3 * L2)) * SharedData.RAD_TO_DEG;
						a3 = -(180 - b);
						angles.Add(a1);
						angles.Add(a2);
						angles.Add(a3);
						if (!wrist_inline)
							{
							a4 = 180 - (a + b + c);
							a4 += A4_CORRECT * Math.Cos(a4 * SharedData.DEG_TO_RAD);	//BASED ON 12/19/2017 TEST RUNS AND VISUALLY CHECKED WITH 12/20/2017 TEST RUN; WHY WORKS???
							angles.Add(a4);
							}
						else
							angles.Add(0.0);
						Log.LogEntry("Base angles (°): " + a.ToString("F3") + "  " + b.ToString("F3") +  " " + c.ToString("F3") );
						rtn = true;
						}
					else
						ac_error = a2.ToString("F3") + " exceeded a2 constraints.";
					}
				else
					ac_error = a1.ToString() + " exceeded a1 constraints.";
				}
			else
				ac_error = "Total distance of " + td.ToString("F3") + " exceeded arm length.";
			return(rtn);
		}



		private static ArrayList MapAnglesToPwms(double a1,double a2,double a3)

		{
			ArrayList p = new ArrayList();
			int pwm1 = 0,pwm2,pwm3;

			if (a1 == 0)
				pwm1 = BASE_0;
			else if (a1 <= 90)
				pwm1 = (int) Math.Round(BASE_90 - ((90 -a1) * BASE_PULSE_PD1));
			else if (a1 <= 180)
				pwm1 = (int) Math.Round(BASE_180 - ((180 - a1) * BASE_PULSE_PD2));
			pwm2 = (int) Math.Round(SHOULDER_0 + (a2  * SHOULDER_PULSE_PD));
			pwm3 = (int) Math.Round(ELBOW_0 - (a3 * ELBOW_PULSE_PD));
			p.Add(pwm1);
			p.Add(pwm2);
			p.Add(pwm3);
			return(p);
		}



		private static int MapWristAngleToPwm(double a)

		{
			int pwm = 0;

			pwm = (int) Math.Round(WRIST_INLINE + (a * WRIST_PULSE_PD));
			if (pwm > MAX_WRIST_PWM)
				pwm = MAX_WRIST_PWM;
			return(pwm);
		}


		
		private static ArrayList MapPwmsToAngles(int pwm1,int pwm2,int pwm3)

		{
			ArrayList a = new ArrayList();
			double a1 = 0,a2,a3;

			if (pwm1 == BASE_0)
				a1 = 90;
			else if (pwm1 <= BASE_90)
				a1 = 90 + ((pwm1 - BASE_90)/BASE_PULSE_PD1);
			else if (pwm1 <= BASE_180)
				a1 = 180 + ((pwm1 - BASE_180)/BASE_PULSE_PD2);
			a2 = (pwm2 - SHOULDER_0)/SHOULDER_PULSE_PD;
			a3 = -(pwm3 - ELBOW_0)/ELBOW_PULSE_PD;
			a.Add(a1);
			a.Add(a2);
			a.Add(a3);
			return(a);
		}


		private static double CorrectedY(double y,double z)

		{
			return(y);
		}



		private static double CorrectedX(double x)

		{ 
			return(x);
		}



		private static double CorrectedZ(double x,double y,double z)

		{ 
			return(z);
		}



		private static DenseVector CalcPosition(double a1,double a2,double a3,bool wrist_inline)

		{
			DenseVector er,pt2,pt3;
			double l3;

			if (wrist_inline)
				l3 = L3 + L4;
			else
				l3 = L3;
			a1 -= ARM_HA_ERROR;
			er = (Math.Cos(a1 * SharedData.DEG_TO_RAD) * ex) + (Math.Sin(a1 * SharedData.DEG_TO_RAD) * ey);
			pt2 = L2 * ((Math.Cos(a2 * SharedData.DEG_TO_RAD) * er) + (Math.Sin(a2 * SharedData.DEG_TO_RAD) * ez));
			pt3 = pt2 + (l3 * ((Math.Cos((a2 + a3) * SharedData.DEG_TO_RAD) * er) + (Math.Sin((a2 + a3) * SharedData.DEG_TO_RAD) * ez)));
			if (!wrist_inline)
				pt3 += L4 * er;
			return(pt3);
		}



		private static Loc3D CalcPositionActual(double a1, double a2, double a3, bool wrist_inline,double xr,double yr,double zr)

		{
			DenseVector pt;
			Loc3D loc;

			pt = CalcPosition(a1,a2,a3,wrist_inline);
			loc = MapACToRC(pt[0], pt[1], pt[2]);
			loc.x *= xr;
			loc.y *= yr;
			loc.z *= zr;
			return (loc);
		}



		public static Loc3D CurrentPositionCorrected()

		{
			DenseVector pt;

			pt = CalcPosition(current_aa.base_angle, current_aa.shoulder_angle, current_aa.elbow_angle, current_aa.wrist_angle == 0);
			return (MapACToRC(pt[0], pt[1], pt[2]));
		}



		public static Loc3D CurrentPositionActual()

		{
			DenseVector pt;
			Loc3D loc;

			pt = CalcPosition(current_aa.base_angle, current_aa.shoulder_angle, current_aa.elbow_angle, current_aa.wrist_angle == 0);
			loc = MapACToRC(pt[0], pt[1], pt[2]);
			loc.x *= current_aa.xr;
			loc.y *= current_aa.yr;
			loc.z *= current_aa.zr;
			return(loc);
		}



		private static bool ObstacleCheck(Loc3D loc1, Loc3D loc2)

		{
			return(false);
		}



		private static bool CheckSequence(Loc3D loc0, motor m1,motor m2,motor m3,double a1,double a2,double a3,bool wrist_inline,double xr,double yr,double zr)

		{
			bool rtn = false;
			Loc3D loc1,loc2,loc3;
			ArrayList al = new ArrayList();
			int i;

			Log.LogEntry("CheckSequence: " + m1 + "  " + m2 + "  " + m3);
			Log.LogEntry("Obstacle height clearence: " + OBS_HEIGHT_CLEAR);
			for (i = 1;i < 4;i++)
				{
				if (i == 1)
					{
					if (m1 == motor.BASE)
						{
						al.Add(a1);
						al.Add(current_aa.shoulder_angle);
						al.Add(current_aa.elbow_angle);
						}
					else if (m1 == motor.SHOULDER)
						{
						al.Add(current_aa.base_angle);
						al.Add(a2);
						al.Add(current_aa.elbow_angle);
						}
					else if (m1 == motor.ELBOW)
						{
						al.Add(current_aa.base_angle);
						al.Add(current_aa.shoulder_angle);
						al.Add(a3);
						}
					}
				else if (i == 2)
					{
					if (m1 == motor.BASE)
						{
						al.Add(a1);
						if (m2 == motor.SHOULDER)
							{
							al.Add(a2);
							al.Add(current_aa.elbow_angle);
							}
						else
							{
							al.Add(current_aa.shoulder_angle);
							al.Add(a3);
							}
						}
					else if (m1 == motor.SHOULDER)
						{
						if (m2 == motor.BASE)
							{
							al.Add(a1);
							al.Add(a2);
							al.Add(current_aa.elbow_angle);
							}
						else
							{
							al.Add(current_aa.base_angle);
							al.Add(a2);
							al.Add(a3);
							}
						}
					else if (m1 == motor.ELBOW)
						{
						if (m2 == motor.BASE)
							{
							al.Add(a1);
							al.Add(current_aa.shoulder_angle);
							al.Add(a3);
							}
						else
							{
							al.Add(current_aa.base_angle);
							al.Add(a2);
							al.Add(a3);
							}

						}
					}
				else
					{
					al.Add(a1);
					al.Add(a2);
					al.Add(a3);
					}
				}
//			pt = CalcPosition((double) al[0],(double) al[1],(double) al[2],wrist_inline);
//			loc1 = MapACToRC(pt[0], pt[1], pt[2]);
			loc1 = CalcPositionActual((double)al[0], (double)al[1], (double)al[2],wrist_inline,xr,yr,zr);
			Log.LogEntry("Position after " + m1 + " move (RC): " + loc1);
			if (oc(loc0,loc1))
				{
				loc2 = CalcPositionActual((double)al[3], (double)al[4], (double)al[5],wrist_inline,xr,yr,zr);
				Log.LogEntry("Position after " + m1 + " and " + m2 + " moves (RC): " + loc2);
				if (oc(loc1, loc2))
					{
					loc3 = CalcPositionActual((double)al[6], (double)al[7], (double)al[8],wrist_inline,xr,yr,zr);
					Log.LogEntry("Position after " + m1 + ", " + m2 + " and " + m3 + " moves (RC): " + loc3);
					rtn = oc(loc2, loc3);
					}
				}
			if (rtn)
				Log.LogEntry("Move sequence is good.");
			else
				Log.LogEntry("Move sequence is not useable.");
			return (rtn);
		}



		private static void CorrectMoveMap(Loc3D loc)

		{

		}



		public static string SaveMap(string name)

		{
			return("");
		}



		private static bool MoveExitPoint()

		{
			return(true);
		}


		private static ArrayList MoveSequence(Loc3D loc,double a1,double a2,double a3,bool wrist_inline,double xr,double yr,double zr)

		{
			ArrayList al = new ArrayList();

			Log.LogEntry("MoveSequence");
			Log.LogEntry("Start position (RC): " + loc);
			if (CheckSequence(loc,motor.BASE,motor.SHOULDER,motor.ELBOW,a1,a2,a3,wrist_inline,xr,yr,zr))
				{
				al.Add(motor.BASE);
				al.Add(motor.SHOULDER);
				al.Add(motor.ELBOW);
				}
			else if (CheckSequence(loc,motor.SHOULDER,motor.ELBOW,motor.BASE, a1, a2, a3,wrist_inline, xr, yr, zr))
				{
				al.Add(motor.SHOULDER);
				al.Add(motor.ELBOW);
				al.Add(motor.BASE);
				}
			else if (CheckSequence(loc, motor.SHOULDER, motor.BASE, motor.ELBOW, a1, a2, a3,wrist_inline, xr, yr, zr))
				{
				al.Add(motor.SHOULDER);
				al.Add(motor.BASE);
				al.Add(motor.ELBOW);
				}
			else if (CheckSequence(loc, motor.ELBOW, motor.BASE, motor.SHOULDER, a1, a2, a3,wrist_inline, xr, yr, zr))
				{
				al.Add(motor.ELBOW);
				al.Add(motor.BASE);
				al.Add(motor.SHOULDER);
				}
			else if (CheckSequence(loc, motor.ELBOW, motor.SHOULDER, motor.BASE, a1, a2, a3,wrist_inline, xr, yr, zr))
				{
				al.Add(motor.ELBOW);
				al.Add(motor.SHOULDER);
				al.Add(motor.BASE);
				}
			else if (CheckSequence(loc, motor.BASE, motor.ELBOW, motor.SHOULDER, a1, a2, a3,wrist_inline, xr, yr, zr))
				{
				al.Add(motor.BASE);
				al.Add(motor.ELBOW);
				al.Add(motor.SHOULDER);
				}
			else
				Log.LogEntry("Could not determine a move sequence.");
			return (al);
		}



		private static bool CheckSweep(Loc3D loc,double a1, double a2, double a3, bool wrist_inline,double xr,double yr,double zr)

		{
			bool rtn = false;
//			DenseVector pt;
			Loc3D floc;

			Log.LogEntry("CheckSweep");
			Log.LogEntry("Start position (RC): " + loc);
			Log.LogEntry("Obstacle height clearence: " + OBS_HEIGHT_CLEAR);
//			pt = CalcPosition(a1,a2,a3, wrist_inline);
//			floc = MapACToRC(pt[0], pt[1], pt[2]);
			floc = CalcPositionActual(a1, a2, a3, wrist_inline,xr,yr,zr);
			Log.LogEntry("Position after move (RC): " + floc);
			rtn = oc(loc,floc);
			return (rtn);
		}



		public static bool TestSweep(Loc3D floc)

		{
			bool rtn;
			DenseVector pt;
			Loc3D loc;

			pt = CalcPosition(current_aa.base_angle, current_aa.shoulder_angle, current_aa.elbow_angle, true);
			loc = MapACToRC(pt[0], pt[1], pt[2]);
			rtn = oc(loc,floc);
			return(rtn);
		}



		public static void MapCorrect()

		{
			DenseVector pt;
			Loc3D loc;

			pt = CalcPosition(current_aa.base_angle, current_aa.shoulder_angle, current_aa.elbow_angle,true);
			loc = MapACToRC(pt[0], pt[1], pt[2]);
			cmm(loc);
		}



		public static bool PositionArm(double x,double y,double z,double offset,bool wrist_inline,ref double dist,ref string err)

		{
			bool rtn = false;
			double xc,yc,zc,tax,tay,taz,a1,a2,a3,a4 = 0,xo,yo,zo,r;
			int pwm1,pwm2,pwm3,pwm4 = WRIST_INLINE;
			ArrayList al,seq;
			DenseVector pt;
			Loc3D loc;
			ServoCommand ac;

			Log.LogEntry("Target robot coord: " + x.ToString("F3") + ", " + y.ToString("F3") + ", " + z.ToString("F3"));
			pt = CalcPosition(current_aa.base_angle, current_aa.shoulder_angle, current_aa.elbow_angle, wrist_inline);
			loc = MapACToRC(pt[0], pt[1], pt[2]);
			dist = Math.Sqrt(Math.Pow(loc.x - x, 2) + Math.Pow(loc.y - y, 2) + Math.Pow(loc.z - z, 2));
			Log.LogEntry("Move distance: " + dist.ToString("F2"));
			a1 = Math.Atan2(z, x) * SharedData.RAD_TO_DEG;
			r = Math.Sqrt((x * x) + (z * z)) - offset;
			xo = r * Math.Cos(a1 * SharedData.DEG_TO_RAD);
			zo = r * Math.Sin(a1 * SharedData.DEG_TO_RAD);
			yo = y;
			Log.LogEntry("Offset robot coord: " + xo.ToString("F3") + ", " + yo.ToString("F3") + ", " + zo.ToString("F3"));
			xc = CorrectedX(xo);
			yc = CorrectedY(yo, zo);
//			zc = CorrectedZ(xo, yo, zo);
			zc = CorrectedZ(xc, yc, zo);
			Log.LogEntry("Corrected offset robot coord: " + xc.ToString("F3") + ", " + yc.ToString("F3") + ", " + zc.ToString("F3"));
			al = MapRCToAC(xc, yc, zc);
			if (al.Count == 3)
				{
				tax = (double) al[0];
				tay = (double) al[1];
				taz = (double) al[2];
				Log.LogEntry("Target arm coord: " + tax.ToString("F3") + ", " + tay.ToString("F3") + ", " + taz.ToString("F3"));
				al.Clear();
				if (CalcAngles(tax, tay, taz,wrist_inline,ref al))
					{
					a1 = (double) al[0];
					a2 = (double) al[1];
					a3 = (double) al[2];
					a4 = (double) al[3];
					Log.LogEntry("Calculated arm angles (°): " + a1.ToString("F3") + " " + a2.ToString("F3") + " " + a3.ToString("F3") + " " + a4.ToString("F3"));
					al.Clear();
					al = MapAnglesToPwms(a1, a2, a3);
					pwm1 = (int) al[0];
					pwm2 = (int) al[1];
					pwm3 = (int) al[2];
					pwm4 = MapWristAngleToPwm(a4);
					Log.LogEntry("Calculated arm PWMs: " + pwm1 + " " + pwm2 + " " + pwm3 + " " + pwm4);
					cmm(loc);
					if (CheckSweep(loc,a1,a2,a3,wrist_inline, xo / xc, yo / yc, zo / zc))
						{
						al.Clear();
						ac = new ServoCommand(BASE_CHANNEL, pwm1);
						al.Add(ac);
						ac = new ServoCommand(SHOULDER_CHANNEL, pwm2);
						al.Add(ac);
						ac = new ServoCommand(ELBOW_CHANNEL, pwm3);
						al.Add(ac);
						ac = new ServoCommand(WRIST_CHANNEL, pwm4);
						al.Add(ac);
						if (Position(al, 2000))
							{
							rtn = true;
							prior_aa = current_aa;
							current_aa.base_angle = a1;
							current_aa.shoulder_angle = a2;
							current_aa.elbow_angle = a3;
							current_aa.wrist_angle = a4;
							current_aa.xr = xo / xc;
							current_aa.yr = yo / yc;
							current_aa.zr = zo / zc;
							}
						else
							err = "Move attempt failed.";
						}
					else
						{
						seq = MoveSequence(loc,a1, a2, a3,wrist_inline,xo/xc,yo/yc,zo/zc);
						if (seq.Count == 3)
							{
							Position(WRIST_CHANNEL,pwm4, WRIST_SPEED);	// WRIST MOTION, IN LINE TO PARALLEL ETC., CAUSE PROBLEM?
							foreach (motor m in seq)
								{
								if (m == motor.BASE)
									Position(BASE_CHANNEL, pwm1,BASE_SPEED);
								else if (m == motor.SHOULDER)
									Position(SHOULDER_CHANNEL, pwm2,SHOULDER_SPEED);
								else if (m == motor.ELBOW)
									Position(ELBOW_CHANNEL, pwm3,ELBOW_SPEED);
								Thread.Sleep(1000);
								}
							prior_aa = current_aa;
							current_aa.base_angle = a1;
							current_aa.shoulder_angle = a2;
							current_aa.elbow_angle = a3;
							current_aa.wrist_angle = a4;
							current_aa.xr = xo/xc;
							current_aa.yr = yo/yc;
							current_aa.zr = zo/zc;
							rtn = true;
							}
						else
							{
							err = "Could not determine a movement sequence.";
							sm("Failed move sequence corrected work place map ");
							}
						}
					}
				else
					{
					err = "Could not calc angles, " + ac_error;
					Log.LogEntry(err);
					}
				}
			else
				{
				err = "Could not map RC to AC";
				Log.LogEntry("Could not map RC to AC");
				}
			return (rtn);
		}



		public static bool RawPositionArm(double x,double y,double z,bool wrist_inline,bool rot_pep,bool open,ref string err)

		{
			bool rtn = false;
			double xc,yc,zc,tax,tay,taz,a1,a2,a3,a4 = 0,xo,yo,zo,r;
			int pwm1,pwm2,pwm3,pwm4 = WRIST_INLINE;
			ArrayList al;
			ServoCommand ac;
			DenseVector pt;
			Loc3D loc;

			Log.LogEntry("Target robot coord: " + x.ToString("F3") + ", " + y.ToString("F3") + ", " + z.ToString("F3"));
			pt = CalcPosition(current_aa.base_angle, current_aa.shoulder_angle, current_aa.elbow_angle, wrist_inline);
			loc = MapACToRC(pt[0], pt[1], pt[2]);
			Log.LogEntry("Move distance: " + Math.Sqrt(Math.Pow(loc.x - x, 2) + Math.Pow(loc.y - y, 2) + Math.Pow(loc.z - z, 2)).ToString("F2"));
			a1 = Math.Atan2(z, x) * SharedData.RAD_TO_DEG;
			r = Math.Sqrt((x * x) + (z * z));
			xo = r * Math.Cos(a1 * SharedData.DEG_TO_RAD);
			zo = r * Math.Sin(a1 * SharedData.DEG_TO_RAD);
			yo = y;
			Log.LogEntry("Offset robot coord: " + xo.ToString("F3") + ", " + yo.ToString("F3") + ", " + zo.ToString("F3"));
			xc = CorrectedX(xo);
			yc = CorrectedY(yo, zo);
//			zc = CorrectedZ(xo, yo, zo);
			zc = CorrectedZ(xc, yc, zo);
			Log.LogEntry("Corrected offset robot coord: " + xc.ToString("F3") + ", " + yc.ToString("F3") + ", " + zc.ToString("F3"));
			al = MapRCToAC(xc, yc, zc);
			if (al.Count == 3)
				{
				tax = (double) al[0];
				tay = (double) al[1];
				taz = (double) al[2];
				Log.LogEntry("Target arm coord: " + tax.ToString("F3") + ", " + tay.ToString("F3") + ", " + taz.ToString("F3"));
				al.Clear();
				if (CalcAngles(tax, tay, taz,wrist_inline,ref al))
					{
					a1 = (double) al[0];
					a2 = (double) al[1];
					a3 = (double) al[2];
					a4 = (double) al[3];
					Log.LogEntry("Calculated arm angles (°): " + a1.ToString("F3") + " " + a2.ToString("F3") + " " + a3.ToString("F3") + " " + a4.ToString("F3"));
					al.Clear();
					al = MapAnglesToPwms(a1, a2, a3);
					pwm1 = (int) al[0];
					pwm2 = (int) al[1];
					pwm3 = (int) al[2];
					pwm4 = MapWristAngleToPwm(a4);
					Log.LogEntry("Calculated arm PWMs: " + pwm1 + " " + pwm2 + " " + pwm3 + " " + pwm4);
					al.Clear();
					ac = new ServoCommand(BASE_CHANNEL, pwm1);
					al.Add(ac);
					ac = new ServoCommand(SHOULDER_CHANNEL, pwm2);
					al.Add(ac);
					ac = new ServoCommand(ELBOW_CHANNEL, pwm3);
					al.Add(ac);
					ac = new ServoCommand(WRIST_CHANNEL, pwm4);
					al.Add(ac);
					if (rot_pep)
						Position(GROTATE_CHANNEL,GROTATE_PERP,GROTATE_SPEED);
					else
						Position(GROTATE_CHANNEL,GROTATE_PAR,GROTATE_SPEED);
					if (open)
						Position(GRIP_CHANNEL,GRIP_OPEN,GRIP_SPEED);
					else
						CloseGrip();
					if (Position(al, 2000))
						{
						rtn = true;
						prior_aa = current_aa;
						current_aa.base_angle = a1;
						current_aa.shoulder_angle = a2;
						current_aa.elbow_angle = a3;
						current_aa.wrist_angle = a4;
						current_aa.xr = xo / xc;
						current_aa.yr = yo / yc;
						current_aa.zr = zo / zc;
						}
					else
						err = "Move attempt failed.";
					}
				else
					{
					err = "Could not calc angles, " + ac_error;
					Log.LogEntry(err);
					}
				}
			else
				{
				err = "Could not map RC to AC";
				Log.LogEntry("Could not map RC to AC");
				}
			return (rtn);
		}



		public static bool IncrementalPostionArmOk(double x, double y, double z, bool wrist_inline)

		{
			bool rtn = false;
			double tax, tay, taz;
			ArrayList al;

			Log.LogEntry("Target robot coord: " + x.ToString("F3") + ", " + y.ToString("F3") + ", " + z.ToString("F3"));
			al = MapRCToAC(x, y, z);
			if (al.Count == 3)
				{
				tax = (double)al[0];
				tay = (double)al[1];
				taz = (double)al[2];
				Log.LogEntry("Target arm coord: " + tax.ToString("F3") + ", " + tay.ToString("F3") + ", " + taz.ToString("F3"));
				al.Clear();
				if (CalcAngles(tax, tay, taz,wrist_inline, ref al))
					rtn = true;
				}
			return (rtn);
		}



		public static bool IncrementalPositionArm(double x, double y, double z, int time,bool wrist_inline,ref string err)

		{
			bool rtn = false;
			double tax,tay,taz,a1,a2,a3,a4;
			int pwm1,pwm2,pwm3,pwm4;
			ArrayList al;
			ServoCommand ac;

			Log.LogEntry("Target robot coord: " + x.ToString("F3") + ", " + y.ToString("F3") + ", " + z.ToString("F3"));
			al = MapRCToAC(x, y, z);
			if (al.Count == 3)
				{
				tax = (double) al[0];
				tay = (double) al[1];
				taz = (double) al[2];
				Log.LogEntry("Target arm coord: " + tax.ToString("F3") + ", " + tay.ToString("F3") + ", " + taz.ToString("F3"));
				al.Clear();
				if (CalcAngles(tax, tay, taz,wrist_inline,ref al))
					{
					a1 = (double) al[0];
					a2 = (double) al[1];
					a3 = (double) al[2];
					a4 = (double) al[3];
					Log.LogEntry("Calculated arm angles (°): " + a1.ToString("F3") + " " + a2.ToString("F3") + " " + a3.ToString("F3") + " " + a4.ToString("F3"));
					al.Clear();
					al = MapAnglesToPwms(a1, a2, a3);
					pwm1 = (int) al[0];
					pwm2 = (int) al[1];
					pwm3 = (int) al[2];
					pwm4 = MapWristAngleToPwm(a4);
					Log.LogEntry("Calculated arm PWMs: " + pwm1 + " " + pwm2 + " " + pwm3 + " " + pwm4);
					al.Clear();
					ac = new ServoCommand(BASE_CHANNEL,pwm1);
					al.Add(ac);
					ac = new ServoCommand(SHOULDER_CHANNEL,pwm2);
					al.Add(ac);
					ac = new ServoCommand(ELBOW_CHANNEL,pwm3);
					al.Add(ac);
					ac = new ServoCommand(WRIST_CHANNEL,pwm4);
					al.Add(ac);
					if (Position(al,time))
						{
						prior_aa = current_aa;
						current_aa.base_angle = a1;
						current_aa.shoulder_angle = a2;
						current_aa.elbow_angle = a3;
						current_aa.wrist_angle = a4;
						rtn = true;
						}
					else
						err = "Move attempt failed.";
					}
				else
					{
					err = "Could not calc angles, " + ac_error;
					Log.LogEntry(err);
					}
				}
			else
				{
				err = "Could not map RC to AC";
				Log.LogEntry("Could not map RC to AC");
				}
			return (rtn);
		}



		public static bool ReturnLastPosition(ref string err)

		{
			bool rtn = false;
			double a1, a2, a3, a4 = 0;
			int pwm1, pwm2, pwm3, pwm4 = WRIST_INLINE;
			ArrayList al, seq;
//			DenseVector pt;
			Loc3D loc;
			ServoCommand ac;

			a1 = prior_aa.base_angle;
			a2 = prior_aa.shoulder_angle;
			a3 = prior_aa.elbow_angle;
			a4 = prior_aa.wrist_angle;
			if ((a1 == 0) && (a2 == 0) && (a3 == 0) && (a4 == 0))
				err = "No last position available.";
			else
				{
				Log.LogEntry("Last position arm angles (°): " + a1.ToString("F3") + " " + a2.ToString("F3") + " " + a3.ToString("F3") + " " + a4.ToString("F3"));
				al = MapAnglesToPwms(a1, a2, a3);
				pwm1 = (int)al[0];
				pwm2 = (int)al[1];
				pwm3 = (int)al[2];
				pwm4 = MapWristAngleToPwm(a4);
				Log.LogEntry("Calculated arm PWMs: " + pwm1 + " " + pwm2 + " " + pwm3 + " " + pwm4);
//				pt = CalcPosition(current_aa.base_angle, current_aa.shoulder_angle, current_aa.elbow_angle,pwm4 == WRIST_INLINE);
//				loc = MapACToRC(pt[0], pt[1], pt[2]);
				loc = CurrentPositionActual();
				cmm(loc);
//				CorrectMoveMap(loc);
				if (CheckSweep(loc, a1, a2, a3,pwm4 == WRIST_INLINE, current_aa.xr, current_aa.yr, current_aa.zr))
					{
					al.Clear();
					ac = new ServoCommand(BASE_CHANNEL, pwm1);
					al.Add(ac);
					ac = new ServoCommand(SHOULDER_CHANNEL, pwm2);
					al.Add(ac);
					ac = new ServoCommand(ELBOW_CHANNEL, pwm3);
					al.Add(ac);
					ac = new ServoCommand(WRIST_CHANNEL, pwm4);
					al.Add(ac);
					if (Position(al, 2000))
						{
						rtn = true;
						prior_aa = current_aa;
						current_aa.base_angle = a1;
						current_aa.shoulder_angle = a2;
						current_aa.elbow_angle = a3;
						}
					else
						err = "Move attempt failed.";
					}
				else
					{
					seq = MoveSequence(loc,a1, a2, a3,(pwm4 == WRIST_INLINE),current_aa.xr,current_aa.yr,current_aa.zr);
					if (seq.Count == 3)
						{
						Position(WRIST_CHANNEL, pwm4, WRIST_SPEED);   // WRIST MOTION, IN LINE TO PARALLEL ETC., CAUSE PROBLEM?
						foreach (motor m in seq)
							{
							if (m == motor.BASE)
								Position(BASE_CHANNEL, pwm1, BASE_SPEED);
							else if (m == motor.SHOULDER)
								Position(SHOULDER_CHANNEL, pwm2, SHOULDER_SPEED);
							else if (m == motor.ELBOW)
								Position(ELBOW_CHANNEL, pwm3, ELBOW_SPEED);
							Thread.Sleep(1000);
							}
						prior_aa = current_aa;
						current_aa.base_angle = a1;
						current_aa.shoulder_angle = a2;
						current_aa.elbow_angle = a3;
						current_aa.wrist_angle = a4;
						rtn = true;
						}
					else
						{
						err = "Could not determine a movement sequence.";
						}
					}
				}
			return (rtn);
		}



		public static int GRotateAngleToPwm(int angle)

		{
			int pwm = 0;

			if ((angle >= 0) && (angle <= 90))
				{
				if (angle == 0)
					pwm = GROTATE_PAR;
				else if (angle == 90)
					pwm = GROTATE_PERP;
				else
					pwm = (int) Math.Round(GROTATE_PAR - (angle * GROTATE_PULSE_PD));
				}
			return(pwm);
		}



		public static void CloseGrip()

		{
			int value = -1,pwm = GRIP_OPEN;

			pwm = ServoPwm(GRIP_CHANNEL);
			while (true)
				{
				pwm += GRIP_INC;
				if (pwm >= GRIP_CLOSE)
					{
					Position(GRIP_CHANNEL,GRIP_CLOSE, GRIP_SPEED);
					Thread.Sleep(250);
					value = ReadAnalogInput();
					break;
					}
				Position(GRIP_CHANNEL,pwm,GRIP_SPEED);
				Thread.Sleep(250);
				value = ReadAnalogInput();
				if (value >= MAX_GRIP_VALUE)
					break;
				}
			Log.LogEntry("CloseGrip: " + value + "  " + pwm);
		}

		}
	}

