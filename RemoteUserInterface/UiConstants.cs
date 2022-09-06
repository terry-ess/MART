using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Constants
	{

	static public class Folders
		{
		public const string DATA_SUB_DIR = "\\data\\";
		public const string TOOLS_SUB_DIR = "\\tools\\";
		public const string CAL_SUB_DIR = "\\cal\\";
		public const string SKILL_REMOTE_INTF_DIR = "\\remote\\";
		}


	static public class MathConvert
		{
		public const double RAD_TO_DEG = 180 / Math.PI;
		public const double DEG_TO_RAD = Math.PI / 180;
		public const double MM_TO_IN = 0.0393701;
		}

	static public class RobotMeasurements
		{
		public const int ROBOT_WIDTH = 19;
		public const int ROBOT_CORE_WIDTH = 15;
		public const int ROBOT_LENGTH = 15;
		public const int FLIDAR_OFFSET = 2;
		public const double FRONT_PIVOT_PT_OFFSET = 2.5;
		public const int RLIDAR_OFFSET = 15;
		public const double BASE_KINECT_HEIGHT = 61.75;
		public const double KINECT_FRONT_OFFSET = -1.5;
		public const double KINECT_CENTER_OFFSET = 4.125;
		}

	static public class UiConstants
		{
		public const int LOC_CNTL_PORT_NO = 20000;
		public const int LOC_FEED_PORT_NO = 25000;
		public const int VIDEO_CNTL_PORT_NO = 30000;
		public const int VIDEO_FEED_PORT_NO = 35000;
		public const int STATUS_CNTL_PORT_NO = 40000;
		public const int STATUS_FEED_PORT_NO = 45000;
		public const int ACTIVITY_CNTL_PORT_NO = 50000;
		public const int ACTIVITY_FEED_PORT_NO = 55000;
		public const int TOOL_CNTL_PORT_NO = 60000;
		public const int TOOL_PORT_NO = 65000;
		public const int TOOL_FEED_PORT_NO = 64000;
		public const int TOOL_KEEP_ALIVE_PORT_NO = 63000;

		public const int TOOL_START_WAIT_COUNT = 200;

		public const string RECHARGE_LOC_NAME = "recharge";

		public const string OK = "ok";					//STATUS, VIDEO, LOCATION AND ACTIVITY
		public const string FAIL = "fail";
		public const string HELLO = "HELLO";
		public const string START = "START";
		public const string RESTART = "RESTART";
		public const string STOP = "STOP";
		public const string KEEP_ALIVE = "ALIVE";
		public const string TAB = "TAB";
		public const string STATUS = "status";
		public const string VIDEO_FRAME = "video";
		public const string VOICE_INPUT = "input";
		public const string SPEECH_OUTPUT = "output";
		public const string COMMAND_INPUT = "command";
		public const string LOCATION = "location";
		public const string SHUTDOWN = "SHUTDOWN";
		public const string EMERGENCY_STOP = "ESTOP";
		public const string SUSPEND = "SUSPEND";
		public const string EXIT = "EXIT";
		public const string SET_LOCATION = "SET LOCATION";
		public const string HW_DIAG = "HW DIAG";

		public const string SEND_VIDEO = "VIDEO";		//SENSOR ALIGNMENT TOOL
		public const string SET_PAN_TILT = "SET";
		public const string CLOSE = "CLOSE";
		public const string CURRENT_PAN = "PAN?";
		public const string CURRENT_TILT = "TILT?";
		public const string DETERMINE_ANGLES = "ANGLES";
		public const string SEND_TILT_TABLE = "TABLE?";
		public const string TILT_TABLE = "TABLE";
		public const string SEND_DEPTH_MAP = "DEPTHMAP";
		public const string DEPTH_MAP = "depth map";
		public const string SEND_VIDEO_PARAM = "VIDEO?";
		public const string SEND_FRONT_LIDAR = "FRONT LIDAR";
		public const string SEND_REAR_LIDAR = "REAR LIDAR";

		public const string SEND_TESTS = "TESTS?";   //TESTS TOOL
		public const string RUN_TEST = "TEST";
		public const string STOP_TEST = "STOP TEST";
		public const string TEST_STATUS = "test status";
		public const string TEST_COMPLETED = "Test run completed.";

		public const string SENSOR_DATA = "sensor data";	//MANUAL CONTROL TOOL
		public const string FORWARD  = "FORWARD";
		public const string BACKWARD = "BACKWARD";
		public const string RIGHT_TURN = "RIGHT";
		public const string LEFT_TURN = "LEFT";
		public const string STOP_MOTION = "STOP MOTION";
		public const string SEND_DEPTH = "DEPTH";
		public const string DEPTH_FRAME = "depth";
		public const string SEND_LIDAR = "LIDAR";
		public const string LIDAR_FRAME = "lidar";
		public const string CHECK_TURN = "CHECK TURN";


		public const string MAG_HEADING = "MAG?";			//SUBSYSTEM OPERATION TOOL
		public const string LIGHT_AMP = "LIGHT?";
		public const string CLEAR_ERR = "CLEAR ERR";
		public const string SERVO_STAT = "SERVO?";
		public const string HA_STAT = "HA STAT?";
		public const string HA_RESTART = "HA RESTART";
		public const string START_POS = "START POS";
		public const string NEXT_POS = "NEXT POS";
		public const string TARGET_PROCESS_FRAME = "TARGET PROC";
		public const string TARGET_PROCESSED_FRAME = "target proc";
		public const string ARM_STAT = "ARM STAT?";
		public const string ARM_TO_START = "ARM START";
		public const string ARM_TO_PARK = "ARM PARK";
		public const string ARM_TO_POSITION = "ARM POS";
		public const string RAW_ARM_TO_POSITION = "RAW ARM POS";
		public const string RECORD_FRONT_SONAR = "RECORD_FRONT SONAR";
		public const string RECORD_REAR_SONAR = "RECORD_REAR SONAR";
		public const string STOP_RECORD = "STOP RECORD";
		public const string SONAR_DATA_READY = "sonar data ready";

		public const string RUN_BLM_CAL = "RUN BLM CAL";	//LINEAR MOTION CALIBRATION TOOL
		public const string STOP_BLM_CAL = "STOP BLM CAL";
		public const string RUN_MT = "RUN MT";
		public const string STOP_MT = "STOP MT";
		public const string CAL_STATUS = "cal status";
		public const string CAL_RUN_COMPLETED = "cal run completed";
		public const string CAL_RUN_ABORTED = "cal run abort";

		public const string RUN_BT_CAL = "RUN BLT CAL";   //TURN CALIBRATION TOOL
		public const string STOP_BT_CAL = "STOP BT CAL";
		public const string SEND_LAST_DS = "SEND LAST DS";
		public const string RUN_TT = "RUN TT";
		public const string STOP_TT = "STOP TT";
		public const string LAST_SENSOR_DS = "last ds";


		public const string START_PPID_CAL = "START PPID CAL";	//MOVE TO PERSON PID CALIBRATION TOOL
		public const string UPDATE_PID_PARAM = "PID_PARAM";
		public const string CAL_RUN_DONE = "cal run done";

		public const string ARM_MANUAL_MODE = "arm manual mode";	//ARM MANUAL MODE REMOTE INTERFACE
		public const string LOCATION_CALC = "LOC CALC";
		public const string SEND_ARM_WP_DATA = "SEND ARM WP DATA?";
		public const string RUN_MAP_CORRECT = "RUN MAP CORRECT";
		public const string TEST_SWEEP = "TEST SWEEP";
		public const string ARM_TO_EE = "ARM EE";
		public const string ARM_OFF = "ARM OFF";
		}
	}
