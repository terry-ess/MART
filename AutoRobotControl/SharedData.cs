using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.IO;
using System.Windows.Forms;


namespace AutoRobotControl
	{
	public static class SharedData
		{
		public const double MM_TO_FT = 0.00328084;
		public const double MM_TO_IN = 0.0393701;
		public const double M_TO_IN = 1000 * MM_TO_IN;
		public const double FT_TO_MM = 304.8;
		public const double IN_TO_MM = 25.4;
		public const double RAD_TO_DEG = 180/Math.PI;
		public const double DEG_TO_RAD = Math.PI/180;

		public const int MAX_DIST_DIF = 12;

		public const int ROBOT_WIDTH = 19;
		public const int ROBOT_CORE_WIDTH = 15;
		public const int ROBOT_LENGTH = 15;
		public const int ROBOT_WHEEL_LENGTH = 5;
		public const int FLIDAR_OFFSET = 2;
		public const double FRONT_PIVOT_PT_OFFSET = 2.5;
		public const int PROBE_LENGTH = 4;
		public const int PROBE_WIDTH = 3;
		public const int ARM_PERCH_OFFSET = 7;
		public const int RLIDAR_OFFSET = 15;

		public const double BASE_KINECT_HEIGHT = 61.75;
		public const double KINECT_FRONT_OFFSET = -1.5;
		public const double KINECT_CENTER_OFFSET = 4.125;
		public const int KINECT_180_DEPTH = 2;

		public const int MIN_TURN_ANGLE = 2;

		public const int FRONT_SONAR_CLEARANCE = 19;
		public const int FRONT_SONAR_RCMIN_CLEARANCE = 10;
		public const int REAR_SONAR_CLEARANCE = 15;
		public const int REAR_SONAR_MIN_CLEARANCE = 6;
		public const int MIN_FRONT_CLEARANCE = ARM_PERCH_OFFSET + 1;

		public const int MIN_BATTERY_VOLTAGE = 22;

		public const string CAL_FILE_EXT = ".cal";
		public const string CAL_SUB_DIR = "\\cal\\";
		public const string LOG_FILE_EXT = ".csv";
		public const string TEXT_TILE_EXT = ".txt";
		public const string PIC_FILE_EXT = ".jpg";
		public const string DATA_SUB_DIR = "\\data\\";
		public const string TOOLS_SUB_DIR = "\\tools\\";
		public const string SKILLS_SUB_DIR = "\\skills\\";


		public const string INSUFFICENT_REAR_CLEARANCE = "Insufficient rear clearance.";
		public const string INSUFFICENT_FRONT_CLEARANCE = "Insufficient front clearance.";
		public const string MPU_FAIL = "MPU6050 connection lost";
		public const string START_TIMEOUT = "start timedout";
		public const string STOP_TIMEOUT = "stop timedout";
		public const string UDP_TIMEOUT = "UDP receive timedout";
		public const string EXCESSIVE_GYRO_CORRECT = "excessive gyro correct";

		public const string RIGHT_TURN = "MCR";
		public const string LEFT_TURN = "MCL";
//		public const string FORWARD = "MCF";
		public const string FORWARD = "TF";
		public const string FORWARD_SLOW = "TF SLOW";
		public const string FORWARD_SLOW_NCC = "TF SLOW NCC";
		public const string FORWARD_DOCK = "TF DOCK";
//		public const string BACKWARD = "MCB";
		public const string BACKWARD = "TB";
		public const string BACKWARD_SLOW = "TB SLOW";
		public const string BACKWARD_SLOW_NCC = "TB SLOW NCC";
//		public const string DIST_MOVED = "MCD";
		public const string DIST_MOVED = "TD";
		public const string REF_MOVE_START = "RM";
		public const string REF_CHG_SPEED = "RMCS";
		public const string REF_MOVE_STOP = "SRM";
		public const string REF_MOVE_REL_ANGLE = "RMRA";
		public const string START_MCSERIAL_TEST = "SMCST";
		public const string STOP_MCSERIAL_TEST = "STPMCST";


		public const int RECHARGE_OFFSET = 72;

		public const int LIDAR_MAX_DIST = 240;
		public const int KINECT_MAX_DIST = 156;
		public const int KINECT_MIN_DIST = 30;
		public const int KINECT_HOR_VIEW = 60;
		public const int MIN_PERP_ANGLE = 20;
		public const int MIN_WALL_DIST = 18;

		public const string RECHARGE_LOC_NAME = "recharge";

		public const double CONNECTOR_SIDE_CLEAR = .5;
		public const int CONNECTOR_MAX_DEPTH = 5;

		public enum RobotLocation { FRONT, REAR, RIGHT, LEFT };

		public enum MotionErrorType { NONE, MPU, START_TIMEOUT, STOP_TIMEOUT, INIT_FAIL, UDP_TIMEOUT, OBSTACLE, TURN_NOT_SAFE, LOC_NOT_VERIFIED};

		public enum MoveType { SPIN, LINEAR };

		public enum RobotStatus {NONE,STARTUP,LIMITED_RUNNING,NORMAL_RUNNING,SKILL_RUNNING,SHUTTING_DOWN,TOOL_RUNNING };

		public const string OFFSET_PARAM_FILE = "sensoroffset.param";

		private const string PARAM_FILE = "startup.param";

		public struct move_error_descript
			{
			public MoveType mt;
			public MotionErrorType et;
			public ArrayList ob_descript;
			};


		public static readonly double REAR_TURN_RADIUS;
		public static readonly double REAR_RADIUS_ANGLE;
		public static readonly double FRONT_TURN_RADIUS;
		public static readonly double FRONT_RADIUS_ANGLE;

		public static RobotStatus status = RobotStatus.NONE;
		public static bool head_assembly_operational = false;
		public static bool motion_controller_operational = false;
		public static bool front_lidar_operational = false;
		public static bool rear_lidar_operational = false;
		public static bool arm_operational = false;
		public static bool kinect_operational = false;
		public static bool navdata_operational = false;
		public static bool speech_recognition_active = false;
		public static bool visual_obj_detect_operational = false;
		public static bool speech_direct_operational = false;
		public static bool log_operations = false;
		public static string last_error = "";
		public static double main_battery_volts = -1;
		public static Bitmap current_rm_map = null;
		public static Room current_rm = null;
		public static NavData.location start;
		public static move_error_descript med = new move_error_descript();

		public static Stopwatch app_time = new Stopwatch();

		private static uint ufile_no = 0;
		private static Mutex ufn_mut = new Mutex();


		static SharedData()

		{
			string fname,line;
			TextReader tr;

			REAR_TURN_RADIUS = Math.Sqrt(Math.Pow(SharedData.ROBOT_LENGTH - SharedData.FRONT_PIVOT_PT_OFFSET,2) + Math.Pow((double) SharedData.ROBOT_CORE_WIDTH/2,2));
			REAR_RADIUS_ANGLE = (Math.Atan(((double) SharedData.ROBOT_CORE_WIDTH / 2) / ((double)SharedData.ROBOT_LENGTH - SharedData.FRONT_PIVOT_PT_OFFSET)) * SharedData.RAD_TO_DEG);
			FRONT_TURN_RADIUS = Math.Sqrt(Math.Pow(SharedData.FRONT_PIVOT_PT_OFFSET,2) + Math.Pow((double) SharedData.ROBOT_WIDTH/2,2));
			FRONT_RADIUS_ANGLE = (Math.Atan(((double)SharedData.ROBOT_WIDTH / 2) / SharedData.FRONT_PIVOT_PT_OFFSET) * SharedData.RAD_TO_DEG);
			fname = Application.StartupPath + SharedData.CAL_SUB_DIR + PARAM_FILE;
			if (File.Exists(fname))
				{
				tr = File.OpenText(fname);
				line = tr.ReadLine();
				if (line == "true")
					log_operations = true;
				else
					log_operations = false;
				tr.Close();
				}
			med.et = MotionErrorType.NONE;
		}



		static public uint GetUFileNo()

		{
			uint rtn;
			
			ufn_mut.WaitOne();
			rtn = ufile_no;
			ufile_no += 1;
			ufn_mut.ReleaseMutex();
			return(rtn);	
		}



		public static string ArrayListToString(ArrayList al)

		{
			string stg = "";
			int i;

			for (i = 0;i < al.Count;i++)
				stg += al[i].ToString() + " ";
			return(stg);
		}


		}
	}
