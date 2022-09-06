#include <stdlib.h>
#include <pthread.h>
#include <time.h>
#include <unistd.h>
#include <signal.h>
#include <sys/stat.h>
#include <sys/time.h>
#include "log.h"
#include "udpsocket.h"
#include "SharedData.h"
#include "MotorController.h"
#include "Mpu6050.h"
#include "Record.h"
#include "LinearMotion.h"
#include "SpinMotion.h"
#include "EZSSonar.h"
#include "MCLinearMotion.h"
#include "MCSpinMotion.h"
#include "Gpio.h"
#include "TLinearMotion.h"
#include "VoltSensor.h"
#include "RefMotion.h"


#define PARAM_FILE "/cal/parameters"
#define LOG_RMCS
#define LOG_UDP_PROC


static char ip_address[16];
static int port;
static pthread_t udpserver;
static pthread_t sudpserver;
static bool app_run = false;
static bool recording = false;
static bool running_lm = false;
static bool running_man = false;
static bool running_sm = false;
static bool running_mclm = false;
static bool running_mcsm = false;
static bool running_tlm = false;
static bool running_rm = false;

static udpsocket udps;
static udpsocket sudps;
static Record rcd;
static LinearMotion* lm = NULL;
static SpinMotion* spin = NULL;
static MCLinearMotion* mclm = NULL;
static MCSpinMotion* mcsm = NULL;
static TLinearMotion* tlm = NULL;
static RefMotion* rm = NULL;

VoltSensor* vs = NULL;
Gpio* io = NULL;
Mpu6050* sensor = NULL;
Log app_log;
MotorController mc;
EzSSonar *sonar = NULL;
char last_move_file[200] = "";
char last_turn_file[200] = "";
char last_rcd_file[200] = "";
char last_sonar_file[200] = "";
char applog[200] = "";
Log slog;


void SetTime(char *buffer)

{
	int len, input, mnth, day, yr, hr, min, sec, rtn;
	char *cptr;
	char command[30];

	cptr = strtok(buffer, " ");
	input = 0;
	while (cptr != NULL)
		{
		cptr = strtok(NULL, " ");
		switch (input)
			{
			case 0:
				mnth = atoi(cptr);
				break;

			case 1:
				day = atoi(cptr);
				break;

			case 2:
				yr = atoi(cptr);
				break;

			case 3:
				hr = atoi(cptr);
				break;
			case 4:
				min = atoi(cptr);
				break;

			case 5:
				sec = atoi(cptr);
				break;
			}
		input += 1;
		}
	sprintf(command, "date -s \"%d%02d%02d %02d:%02d UTC\"", yr, mnth, day, hr, min);
	rtn = system(command);
}



bool Remove(char *buffer)

{
	 char cmd[200];
	 bool rtn = false;

	 sprintf(cmd, "rm %s", buffer);
	 if (system(cmd) != -1)
		  rtn = true;
	 return(rtn);
}



bool RefMotionCmd(char *cmd,char *rsp)

{
	bool rtn = false;
	char* pos;
	int val1,val2;

	if (strncmp(cmd, "RM ", 3) == 0)
		{
		rtn = true;
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			pos = strchr(cmd, ' ');
			if (pos > 0)
				{
				val1 = atoi(pos + 1);
				if (rm->StartRM(val1))
					{
					running_rm = true;
					strcpy(rsp, "ok");
					}
				else
					sprintf(rsp, "fail %s", lm->MotionError());
				}
			else
				strcpy(rsp, "fail improper command format");
			}
		else
			strcpy(rsp, "fail conflicting action in progress");
		}
	else if (strncmp(cmd, "RMCS ", 5) == 0)
		{

#ifdef LOG_RMCS
		app_log.LogEntry((char *) "RMCS start");
#endif

		rtn = true;
		if (running_rm)
			{
			pos = strchr(cmd, ' ');
			if (pos > 0)
				{
				val1 = atoi(pos + 1);
				pos = strchr(pos + 1, ' ');
				if (pos > 0)
					{
					val2 = atoi(pos + 1);
					if (rm->ChangeSpeed(val1, val2))
						sprintf(rsp, "ok %.2f",rm->RelativeAngle());
					else
						sprintf(rsp, "fail %s", rm->MotionError());
					}
				else
					strcpy(rsp, "fail improper command format");
				}
			else
				strcpy(rsp, "fail improper command format");
			}
		else
			strcpy(rsp, "fail no motion in progress");

#ifdef LOG_RMCS
		app_log.LogEntry((char *) "RMCS end");
#endif

		}
	else if (strcmp(cmd, "SRM") == 0)
		{
		rtn = true;
		if (running_rm)
			{
			rm->StopRM();
			running_rm = false;
			if (strlen(rm->MotionError()) > 0)
				sprintf(rsp, "fail %s", rm->MotionError());
			else
				strcpy(rsp, "ok");
			}
		else
			strcpy(rsp, "fail no motion in progress");
		}
	else if (strcmp(cmd, "RMRA") == 0)
		{
		rtn = true;
		sprintf(rsp, "ok %.2f",rm->RelativeAngle());
		}
	return(rtn);
}



bool McSerialTestCmd(char *cmd, char *rsp)

{
	bool rtn = false;

	if (strcmp(cmd, "SMCST") == 0)
		{
		rtn = true;
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			if (mc.StartSerialTest())
				strcpy(rsp, "ok");
			else
				strcpy(rsp,"fail");
			}
		else
			strcpy(rsp, "fail conflicting action in progress");
		}
	else if (strcmp(cmd, "STPMCST") == 0)
		{
		rtn = true;
		mc.StopSerialTest();
		mc.RecordSerialTest();
		strcpy(rsp, "ok");
		}
	return(rtn);
}




void ExecuteCommand(char* cmd,char *rtn)

{
	time_t tt;
	char buffer[512];
	int val1,val2;
	char* pos;
	linear_speed ls;
	bool param_good = false;
	double volts;

	if (strncmp(cmd,"TIME ",5) == 0)
		{
		strcpy(rtn, "ok");
		SetTime(cmd);
		}
	else if (strcmp(cmd,"LOG") == 0)
		{
		if (!app_log.LogOpen())
			{
	 		sprintf(buffer, "%s%s*.log", BASE_DIR,DATA_DIR);
			Remove(buffer);
			sprintf(buffer, "%s%s*.csv", BASE_DIR, DATA_DIR);
			Remove(buffer);
			time(&tt);
			sprintf(buffer,"%s%sMotionController%ld.log",BASE_DIR,DATA_DIR,tt);
			if (app_log.OpenLog(buffer, (char *) "Motion controller log file opened",true))
				{
				strcpy(applog,buffer);
				strcpy(rtn, "ok");
				}
			else
				{
				slog.LogEntry((char *) "Could not open operation log file.");
				strcpy(rtn, "fail");
				}
			}
		else
			strcpy(rtn, "ok");
		}
	else if (strcmp(cmd,"VOLTS") == 0)
		{
		volts = vs->GetVolts();
		if (volts == -1)
			strcpy(rtn,"fail");
		else
			sprintf(rtn,"ok %.2f",volts);
		}
	else if (strcmp(cmd,"MCSTAT") == 0)
		{
		sprintf(rtn, "ok %d", mc.GetError());
		}
	else if (strcmp(cmd,"ENC") == 0)
		{
		if (mc.ReadEncoder(&val1))
			sprintf(rtn,"ok %ld",val1);
		else
			strcpy(rtn,"fail");
		}
	else if (strncmp(cmd, "RST ", 4) == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			if (strcmp(cmd,"RST IMU") == 0)
				{
				sensor->Close();
				if (sensor->Init())
					strcpy(rtn,"ok");
				else
					strcpy(rtn,"fail could not init");
				}
			else if (strcmp(cmd,"RST MC") == 0)
				{
				mc.Close();
				if (mc.Open())
					{
					if (mc.GetError() == 0)
						strcpy(rtn,"ok");
					else
						strcpy(rtn,"fail error condition");
					}
				else
					strcpy(rtn,"fail could not open");
				}
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strcmp(cmd,"SB") == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			if (sensor->DetermineSensorBias())
				strcpy(rtn, "ok");
			else
				strcpy(rtn,"fail");
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (RefMotionCmd(cmd, rtn))
		{
		}
	else if (McSerialTestCmd(cmd, rtn))
		{
		}
	else if (strcmp(cmd,"REC START") == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			recording = true;
			rcd.Start();
			strcpy(rtn, "ok");
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strcmp(cmd,"REC STOP") == 0)
		{
		if (recording)
			{
			recording = false;
			rcd.Stop();
			}
		strcpy(rtn, "ok");
		}
	else if (strncmp(cmd,"F",1) == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			if ((strlen(cmd) == 1) || (strcmp(cmd,"F NORMAL") == 0))
				{
				if (lm->MoveLinear(FORWARD,NORMAL))
					{
					running_lm = true;
					strcpy(rtn, "ok");
					}
				else
					sprintf(rtn, "fail %s",lm->MotionError());
				}
			else if (strcmp(cmd,"F FAST") == 0)
				{
				if (lm->MoveLinear(FORWARD,FAST))
					{
					running_lm = true;
					strcpy(rtn, "ok");
					}
				else
					sprintf(rtn, "fail %s", lm->MotionError());
				}
			else if (strcmp(cmd,"F SLOW") == 0)
				{
				if (lm->MoveLinear(FORWARD,SLOW))
					{
					running_lm = true;
					strcpy(rtn, "ok");
					}
				else
					sprintf(rtn, "fail %s", lm->MotionError());
				}
			else
				strcpy(rtn, "fail command not supported");
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strncmp(cmd,"SLMP",4) == 0)
		{
		int iv1, iv2;
		int i;

		pos = strchr(cmd, ' ');
		for (i = 0;i < 2;i++)
			{
			if (pos == 0)
				break;
			else if (i == 0)
				{
				pos += 1;
				iv1 = atoi(pos);
				pos = strchr(pos,' ');
				}
			else if (i == 1)
				{
				pos += 1;
				iv2 = atoi(pos);
				pos = strchr(pos,' ');
				param_good = true;
				}
			}
		if (param_good)
			{
			lm->SetMotorParam(iv1,iv2);
			strcpy(rtn, "ok");
			}
		else
			strcpy(rtn, "fail command missng parameter");
		}
	else if (strncmp(cmd, "SLPP", 4) == 0)
		{
		int g1,g2,g3;
		int i;

		pos = strchr(cmd, ' ');
		for (i = 0; i < 3; i++)
			{
			if (pos == 0)
				break;
			else if (i == 0)
				{
				pos += 1;
				g1 = atof(pos);
				pos = strchr(pos, ' ');
				}
			else if (i == 1)
				{
				pos += 1;
				g2 = atof(pos);
				pos = strchr(pos, ' ');
				}
			else if (i == 2)
				{
				pos += 1;
				g3 = atof(pos);
				pos = strchr(pos, ' ');
				param_good = true;
				}
			}
		if (param_good)
			{
			lm->SetPIDParam(g1,g2,g2);
			strcpy(rtn, "ok");
			}
		else
			strcpy(rtn, "fail command missng parameter");
		}
	else if (strncmp(cmd,"B",1) == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			if ((strlen(cmd) == 1) || (strcmp(cmd,"B NORMAL") == 0))
				{
				if (lm->MoveLinear(BACKWARD,NORMAL))
					{
					running_lm = true;
					strcpy(rtn, "ok");
					}
				else
					sprintf(rtn, "fail %s", lm->MotionError());
				}
			else if (strcmp(cmd, "B FAST") == 0)
				{
				if (lm->MoveLinear(BACKWARD,FAST))
					{
					running_lm = true;
					strcpy(rtn, "ok");
					}
				else
					sprintf(rtn, "fail %s", lm->MotionError());
				}
			else if (strcmp(cmd, "B SLOW") == 0)
				{
				if (lm->MoveLinear(BACKWARD,SLOW))
					{
					running_lm = true;
					strcpy(rtn, "ok");
					}
				else
					sprintf(rtn, "fail %s", lm->MotionError());
				}
			else
				strcpy(rtn, "fail command not supported");
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strncmp(cmd,"MCF",3) == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			pos = strchr(cmd, ' ');
			if (pos > 0)
				{
				val1 = atoi(pos + 1);
				running_mclm = true;
				if (mclm->MoveLinear(FORWARD, val1, NORMAL))
					strcpy(rtn, "ok");
				else
					sprintf(rtn, "fail %s",mclm->MotionError());
				mclm->RecordSensorData();
				running_mclm = false;
				}
			else
				strcpy(rtn, "fail command missng parameter");
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strncmp(cmd,"MCB",3) == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			pos = strchr(cmd, ' ');
			if (pos > 0)
				{
				val1 = atoi(pos + 1);
				running_mclm = true;
				if (mclm->MoveLinear(BACKWARD, val1, NORMAL))
					strcpy(rtn, "ok");
				else
					sprintf(rtn, "fail %s", mclm->MotionError());
				mclm->RecordSensorData();
				running_mclm = false;
				}
			else
				strcpy(rtn, "fail command missing parameter");
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strncmp(cmd,"MCD",3) == 0)
		{
		if (!running_mclm)
			sprintf(rtn,"ok %d",mclm->LastMoveDist());
		else
			strcpy(rtn,"fail in progress");
		}
	else if (strncmp(cmd, "TFSFC", 5) == 0)
		{
		val1 = atoi(cmd + 6);
		tlm->SetFrontClearance(val1);
		strcpy(rtn, "ok");
		}
	else if (strncmp(cmd,"TF",2) == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			bool docking = false;
			bool ncc = false;

			tlm->ClearLastMoveDist();
			if (strncmp(cmd,"TF NORMAL",9) == 0)
				{
				ls = NORMAL;
				pos = strchr(&cmd[8],' ');
				}
			else if (strncmp(cmd, "TF SLOW NCC", 11) == 0)
				{
				ls = SLOW;
				pos = strchr(&cmd[10], ' ');
				ncc = true;
				}
			else if (strncmp(cmd,"TF SLOW",7) == 0)
				{
				ls = SLOW;
				pos = strchr(&cmd[6], ' ');
				}
			else if (strncmp(cmd,"TF DOCK",7) == 0)
				{
				ls = SLOW;
				docking = true;
				ncc = true;
				pos = strchr(&cmd[6], ' ');
				}
			else
				{
				ls = NORMAL;
				pos = strchr(cmd, ' ');
				}
			if (pos > 0)
				{
				val1 = atoi(pos + 1);
				if (val1 >= 0)
					 {
					 running_tlm = true;
					 if (tlm->MoveLinear(FORWARD, val1,ls,docking,ncc))
						 sprintf(rtn, "ok %d",tlm->LastMoveDist());
					 else
						 sprintf(rtn, "fail %s",tlm->MotionError());
					 tlm->RecordSensorData();
					 running_tlm = false;
					 }
				else
					 strcpy(rtn,"fail bad distance value");
				}
			else
				strcpy(rtn, "fail command missng parameter");
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strncmp(cmd,"PTF",3) == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			double dv1, dv2, dv3;
			int i;

			tlm->ClearLastMoveDist();
			if (strncmp(cmd,"PTF NORMAL",10) == 0)
				{
				ls = NORMAL;
				pos = strchr(&cmd[9],' ');
				}
			else if (strncmp(cmd,"PTF SLOW",8) == 0)
				{
				ls = SLOW;
				pos = strchr(&cmd[7], ' ');
				}
			else
				{
				ls = NORMAL;
				pos = strchr(cmd, ' ');
				}
			for (i = 0;i < 4;i++)
				{
				if (pos == 0)
					break;
				else if (i == 0)
					{
					pos += 1;
					val1 = atoi(pos);
					pos = strchr(pos,' ');
					}
				else if (i == 1)
					{
					pos += 1;
					dv1 = atof(pos);
					pos = strchr(pos,' ');
					}
				else if (i == 2)
					{
					pos += 1;
					dv2 = atof(pos);
					pos = strchr(pos,' ');
					}
				else if (i == 3)
					{
					pos += 1;
					dv3 = atof(pos);
					param_good = true;
					}
				}
			if (param_good)
				{
				running_tlm = true;
				tlm->SetPidParam(dv1,dv2,dv3);
				if (tlm->MoveLinear(FORWARD, val1,ls,false,false))
					strcpy(rtn, "ok");
				else
					sprintf(rtn, "fail %s",tlm->MotionError());
				tlm->RecordSensorData();
				tlm->ClearPidParam();
				running_tlm = false;
				}
			else
				strcpy(rtn, "fail command missng parameter");
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strncmp(cmd,"TB",2) == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			bool ncc = false;

			tlm->ClearLastMoveDist();
			if (strncmp(cmd, "TB NORMAL", 9) == 0)
				{
				ls = NORMAL;
				pos = strchr(&cmd[8], ' ');
				}
			else if (strncmp(cmd, "TB SLOW NCC", 11) == 0)
				{
				ls = SLOW;
				pos = strchr(&cmd[10], ' ');
				ncc = true;
				}
			else if (strncmp(cmd, "TB SLOW", 7) == 0)
				{
				ls = SLOW;
				pos = strchr(&cmd[6], ' ');
				}
			else
				{
				ls = NORMAL;
				pos = strchr(cmd, ' ');
				}
			if (pos > 0)
				{
				val1 = atoi(pos + 1);
				if (val1 >= 0)
					{
					running_tlm = true;
					if (tlm->MoveLinear(BACKWARD, val1, ls,false,ncc))
						sprintf(rtn, "ok %d",tlm->LastMoveDist());
					else
						sprintf(rtn, "fail %s", tlm->MotionError());
					tlm->RecordSensorData();
					running_tlm = false;
					}
				else
					 strcpy(rtn,"fail bad distance value");
				}
			else
	 			strcpy(rtn, "fail command missng parameter");
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strncmp(cmd,"TD",2) == 0)
		{
		if (!running_tlm)
			sprintf(rtn,"ok %d",tlm->LastMoveDist());
		else
			strcpy(rtn,"fail in progress");
		}
	else if (strncmp(cmd,"R",1) == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			if ((strlen(cmd) == 1) || (strcmp(cmd,"R NORMAL") == 0))
				{
				if (spin->StartSpin(RIGHT,SNORMAL))
					{
					running_sm = true;
					strcpy(rtn, "ok");
					}
				else
					sprintf(rtn, "fail %s", spin->MotionError());
				}
			else if (strcmp(cmd, "R SLOW") == 0)
				{
				if (spin->StartSpin(RIGHT, SSLOW))
					{
					running_sm = true;
					strcpy(rtn, "ok");
					}
				else
					sprintf(rtn, "fail %s", spin->MotionError());
				}
			else if (strcmp(cmd, "R CUSTOM") == 0)
				{
				if (spin->StartSpin(RIGHT, SCUSTOM))
					{
					running_sm = true;
					strcpy(rtn, "ok");
					}
				else
					sprintf(rtn, "fail %s", spin->MotionError());
				}
			else
				strcpy(rtn, "fail command not supported");
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strncmp(cmd,"L",1) == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm)
			{
			if ((strlen(cmd) == 1) || (strcmp(cmd,"L NORMAL") == 0))
				{
				if (spin->StartSpin(LEFT,SNORMAL))
					{
					running_sm = true;
					strcpy(rtn, "ok");
					}
				else
					sprintf(rtn, "fail %s", spin->MotionError());
				}
			else if (strcmp(cmd, "L SLOW") == 0)
				{
				if (spin->StartSpin(LEFT, SSLOW))
					{
					running_sm = true;
					strcpy(rtn, "ok");
					}
				else
					sprintf(rtn, "fail %s", spin->MotionError());
				}
			else if (strcmp(cmd, "L CUSTOM") == 0)
				{
				if (spin->StartSpin(LEFT, SCUSTOM))
					{
					running_sm = true;
					strcpy(rtn, "ok");
					}
				else
					sprintf(rtn, "fail %s", spin->MotionError());
				}
			else
				strcpy(rtn, "fail command not supported");
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strncmp(cmd, "SSP",3) == 0)
		{
		int iv1, iv2;
		int i;

		pos = strchr(cmd, ' ');
		for (i = 0; i < 2; i++)
			{
			if (pos == 0)
				break;
			else if (i == 0)
				{
				pos += 1;
				iv1 = atoi(pos);
				pos = strchr(pos, ' ');
				}
			else if (i == 1)
				{
				pos += 1;
				iv2 = atoi(pos);
				pos = strchr(pos, ' ');
				param_good = true;
				}
			}
		if (param_good)
			{
			spin->SetParam(iv1, iv2);
			strcpy(rtn, "ok");
			}
		else
			strcpy(rtn, "fail command missing parameter");
		}
	else if (strncmp(cmd,"MCR",3) == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			if (strncmp(cmd,"MCR SLOW",8) == 0)
				{
				pos = strchr(cmd + 7, ' ');
				if (pos > 0)
					{
					val1 = atoi(pos + 1);
					running_mcsm = true;
					if (mcsm->StartSpin(RIGHT, val1, SSLOW))
						strcpy(rtn, "ok");
					else
						sprintf(rtn, "fail %s", mcsm->MotionError());
					mcsm->RecordSensorData();
					running_mcsm = false;
					}
				else
					strcpy(rtn, "fail command missing parameter");
				}
			else
				{
				pos = strchr(cmd, ' ');
				if (pos > 0)
					{
					val1 = atoi(pos + 1);
					running_mcsm = true;
					if (mcsm->StartSpin(RIGHT,val1,SNORMAL))
						strcpy(rtn, "ok");
					else
						sprintf(rtn, "fail %s", mcsm->MotionError());
					mcsm->RecordSensorData();
					running_mcsm = false;
					}
				else
					strcpy(rtn, "fail command missing parameter");
				}
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strncmp(cmd,"MCL",3) == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			if (strncmp(cmd,"MCL SLOW",8) == 0)
				{
				pos = strchr(cmd + 7, ' ');
				if (pos > 0)
					{
					val1 = atoi(pos + 1);
					running_mcsm = true;
					if (mcsm->StartSpin(LEFT, val1, SSLOW))
						strcpy(rtn, "ok");
					else
						sprintf(rtn, "fail %s", mcsm->MotionError());
					mcsm->RecordSensorData();
					running_mcsm = false;
					}
				else
					strcpy(rtn, "fail command missing parameter");
				}
			else
				{
				pos = strchr(cmd, ' ');
				if (pos > 0)
					{
					val1 = atoi(pos + 1);
					running_mcsm = true;
					if (mcsm->StartSpin(LEFT,val1,SNORMAL))
						strcpy(rtn, "ok");
					else
						sprintf(rtn, "fail %s", mcsm->MotionError());
					mcsm->RecordSensorData();
					running_mcsm = false;
					}
				else
					strcpy(rtn, "fail command missing parameter");
				}
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strcmp(cmd,"SM") == 0)
		{
		if (running_lm)
			{
			lm->StopLinear();
			lm->RecordSensorData();
			lm->ClearMotorParam();
			lm->ClearPIDParam();
			running_lm = false;
			if (strlen(lm->MotionError()) > 0)
				sprintf(rtn,"fail %s",lm->MotionError());
			else
				strcpy(rtn,"ok");
			}
		else if (running_sm)
			{
			spin->StopSpin();
			spin->RecordSensorData();
			running_sm = false;
			if (strlen(spin->MotionError()) > 0)
				sprintf(rtn,"fail %s",spin->MotionError());
			else
				sprintf(rtn,"ok %d", spin->LastMoveAngle());
			}
		else
			strcpy(rtn,"fail no motion");
		}
	else if (strncmp(cmd,"CS ",3) == 0)
		{
		if (running_lm)
			{
			pos = strchr(cmd,' ');
			val1 = atoi(pos + 1);
			pos = strchr(pos + 1,' ');
			val2 = atoi(pos + 1);
			if (lm->ChangeSpeed(val1, val2))
				strcpy(rtn, "ok");
			else
				strcpy(rtn,"fail");
			}
		else
			strcpy(rtn, "fail");
		}
	else if (strncmp(cmd,"MCS ",4) == 0)
		{
		if (!running_lm && !running_man && !recording && !running_sm && !running_mclm && !running_mcsm && !running_tlm && !running_rm)
			{
			if (!running_man)
				{
				mc.SetMode(0);
				mc.DisableTimeout();
				mc.DisableRegulator();
				running_man = true;
				}
			pos = strchr(cmd, ' ');
			val1 = atoi(pos + 1);
			pos = strchr(pos + 1,' ');
			val2 = atoi(pos + 1);
			mc.ChngSpeed(val1, val2);
			strcpy(rtn, "ok");
			}
		else
			strcpy(rtn, "fail conflicting action in progress");
		}
	else if (strcmp(cmd,"MSM") == 0)
		{
		if (running_man)
			{
			mc.StopMotion();
			running_man = false;
			}
		strcpy(rtn,"ok");
		}
	else if (strcmp(cmd,"SFC") == 0)
		{
		sprintf(rtn,"ok %d",sonar->ReadFrontSonar());
		}
	else if (strcmp(cmd,"SRC") == 0)
		{
		sprintf(rtn,"ok %d",sonar->ReadRearSonar());
		}
	else if (strncmp(cmd, "SFR", 3) == 0)
		{
		pos = strchr(cmd, ' ');
		val1 = atoi(pos + 1);
		sonar->RecordSonarData(true,val1);
		strcpy(rtn, "ok");
		}
	else if (strncmp(cmd, "SRR", 3) == 0)
		{
		pos = strchr(cmd, ' ');
		val1 = atoi(pos + 1);
		sonar->RecordSonarData(false, val1);
		strcpy(rtn, "ok");
		}
	else if (strcmp(cmd, "SSR") == 0)
		{
		sonar->StopRecord();
		strcpy(rtn,"ok");
		}
	else if (strcmp(cmd,"MNLV") == 0)
		{
		val1 = tlm->MaxNormalVelocity();
		if (val1 != -1)
			sprintf(rtn, "ok %d",val1);
		else
			strcpy(rtn,"fail");
		}
	else if (strcmp(cmd,"MD") == 0)
		{
		sprintf(rtn, "ok %d", mclm->LastMoveDist());
		}
	else if (strncmp(cmd,"MFC ",3) == 0)
		{
		pos = strchr(cmd, ' ');
		val1 = atoi(pos + 1);
		mclm->SetFrontClearance(val1);
		strcpy(rtn,"ok");
		}
	else if (strcmp(cmd,"DOCKED") == 0)
		{
		if (io->Docked())
			strcpy(rtn, "ok true");
		else
			strcpy(rtn, "ok false");
		}
	else if (strcmp(cmd, "HELLO") == 0)
		{
		strcpy(rtn, "ok BBB");
		if (!app_log.LogOpen())
			slog.LogEntry((char *) "ok BBB");
		}
	else
		{
		strcpy(rtn,"fail command not supported");
		}
}



void *UdpServer(void *)

{
	char buffer[50];
	sockaddr sa;
	int len,mp;
	time_t tt;
	struct itimerval itm;
	struct timeval tv;
	char rtn[50];

#ifdef LOG_UDP_PROC
	long initial;
	int dif;
	char buf[50];
#endif

	slog.LogEntry((char *) "UDP server starting.");
	while (app_run)
		{
		len = udps.RecvPacket(buffer, sizeof(buffer), &sa);
		if (len > 0)
			{

#ifdef LOG_UDP_PROC
			gettimeofday(&tv, NULL);
			initial = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
#endif 

			buffer[len] = 0;
			app_log.LogEntry(buffer);
			if (strcmp(buffer, "EXIT") == 0)
				{
				app_log.CloseLog((char *) "UDP server shutting down");
				app_run = false;
				if (strlen(applog) > 0)
					{
					udps.SendFile(applog,&sa);
					rtn[0] = 0;
					}
				else
					strcpy(rtn,"ok");
				sudps.Close();
				udps.Close();
				}
			else if (strcmp(buffer, "LAST MOVE") == 0)
				{
				if (strlen(last_move_file) > 0)
					{
					app_log.LogEntry(last_move_file);
					udps.SendFile(last_move_file,&sa);
					rtn[0] = 0;
					}
				else
					strcpy(rtn,"fail no file available");
				}
			else if (strcmp(buffer, "LAST TURN") == 0)
				{
				if (strlen(last_turn_file) > 0)
					{
					app_log.LogEntry(last_turn_file);
					udps.SendFile(last_turn_file,&sa);
					rtn[0] = 0;
					}
				else
					strcpy(rtn,"fail no file available");
				}
			else if (strcmp(buffer, "LAST IMU RCD") == 0)
				{
				if (strlen(last_rcd_file) > 0)
					{
					app_log.LogEntry(last_rcd_file);
					udps.SendFile(last_rcd_file,&sa);
					rtn[0] = 0;
					}
				else
					strcpy(rtn,"fail no file available");
				}
			else if (strcmp(buffer, "LAST SONAR RECORD") == 0)
				{
				if (strlen(last_rcd_file) > 0)
					{
					app_log.LogEntry(last_sonar_file);
					udps.SendFile(last_sonar_file, &sa);
					rtn[0] = 0;
					}
				else
					strcpy(rtn, "fail no file available");
				}
			else
				ExecuteCommand(buffer,rtn);
			if (strlen(rtn) > 0)
				{
				udps.SendPacket(rtn, strlen(rtn), &sa);
				app_log.LogEntry(rtn);
				}

#ifdef LOG_UDP_PROC
			gettimeofday(&tv, NULL);
			dif = ((tv.tv_sec * 1000) + (tv.tv_usec / 1000))- initial;
			sprintf(buf, "UDP server process time: %d ms", dif);
			app_log.LogEntry(buf);
#endif 

			}
		else
			{
			app_log.LogEntry((char *) "UDP receive failure, UDP server shutting down");
			app_run = false;
			}
		}
}



void *SUdpServer(void *)

{
	char buffer[50];
	sockaddr sa;
	int len;
	time_t tt;
	struct itimerval itm;
	struct timeval tv;
	int val;
	char *pos;

	slog.LogEntry((char *) "Stop UDP server starting.");
	while (app_run)
		{
		len = sudps.RecvPacket(buffer, sizeof(buffer), &sa);
		if (len > 0)
			{
			buffer[len] = 0;
			app_log.LogEntry(buffer);
			if (strcmp(buffer,"SL") == 0)
				{
				if (running_lm)
					{
					lm->StopLinear();
					}
				else if (running_mclm)
					{
					mclm->StopLinear();
					}
				else if (running_tlm)
					tlm->StopLinear();
				else if (running_rm)
					rm->StopRM();
				}
			else if (strcmp(buffer,"SS") == 0)
				{
				if (running_sm)
					{
					spin->StopSpin();
					}
				else if (running_mcsm)
					{
					mcsm->StopSpin();
					}
				}
			else if (strncmp(buffer,"SDR",3) == 0)
				{
				if (running_mclm)
					{
					pos = strchr(buffer,' ');
					val = atoi(pos + 1);
					mclm->SwitchToDR(val);
					}
				else if (running_tlm)
					{
					pos = strchr(buffer,' ');
					val = atoi(pos + 1);
					tlm->SwitchToDR(val);
					}
				}
			}
		else
			app_log.LogEntry((char *) "SUDP receive failure, SUDP server shutting down");
		}
}



bool ReadParameters()

{
	FILE *pfile = NULL;
	char line[128];
	bool rtn = false;

	sprintf(line, "%s%s", BASE_DIR, PARAM_FILE);
	if ((pfile = fopen(line, "r")) != NULL)
		{
		fgets(line, sizeof(line) - 1, pfile);
		strcpy(ip_address, line);
		fgets(line, sizeof(line) - 1, pfile);
		port = atoi(line);
		fclose(pfile);
		rtn = true;
		}
	return(rtn);
}



void TermHandler(int sig)

{
	app_run = false;
	sudps.Close();
	udps.Close();
	if (running_lm)
		lm->StopLinear();
	else if (running_sm)
		spin->StopSpin();
	else if (running_man)
		mc.StopMotion();
	else if (recording)
		rcd.Stop();
	else if (running_mclm)
		mclm->StopLinear();
	else if (running_mcsm)
		mcsm->StopSpin();
	else if (running_rm)
		rm->StopRM();
	if (recording)
		rcd.Stop();
	app_log.LogEntry((char *) "Received interrupt/terminate signal");
	signal(SIGTERM, SIG_DFL);
	signal(SIGINT, SIG_DFL);
}



int main(int argc, char *argv[])

{
	sigset_t blocked_sigs;
	char fname[195];
	char buffer[50];
	int dist = 0;
	bool rtn;
	double volts;

	sprintf(fname,"%s/mcstartuplog",BASE_DIR);
	slog.OpenLog(fname, (char *)  "Motion controller start up log file opened", true);
	puts("Start up log created");
	io = Gpio::Instance();
	io->Init();
	io->RunLed(true);
	nice(-20);
	sigemptyset(&blocked_sigs);
	sigaddset(&blocked_sigs, SIGALRM);
	pthread_sigmask(SIG_BLOCK, &blocked_sigs, NULL);
	if (ReadParameters())
		{
		puts("Parameters read");
		if (mc.Open())
			{
			puts("Motor controller opened.");
			rtn = mc.ReadEncoder(&dist);
			if (rtn)
				{
				sprintf(buffer,"Encoder: %d",dist);
				slog.LogEntry(buffer);
				puts(buffer);
				}
			else
				{
				slog.LogEntry((char *) "Encoder reading failed.");
				puts("Encoder reading failed.");
				}
			sensor = Mpu6050::Instance();
			if (sensor->Init())
				{
				puts("MPU6050 initialized");
				if (udps.Open(ip_address, port, false))
					{
					app_run = true;
					if (pthread_create(&udpserver, NULL, UdpServer, 0) == 0)
						{
						if (sudps.Open(ip_address, port + 1, false))
							pthread_create(&sudpserver,NULL,SUdpServer,0);
						else
							{
							slog.LogEntry((char *) "Stop UDP server initialization failed.");
							puts("Stop UDP server initialization failed.");
							}
						signal(SIGTERM, TermHandler);
						signal(SIGINT, TermHandler);
						lm = LinearMotion::Instance();
						spin = SpinMotion::Instance();
						sonar = EzSSonar::Instance();
						if (!sonar->Init())
							{
							slog.LogEntry((char *) "Sonar initialization failed.");
							puts("Sonar initialization failed.");
							}
						else
							{
							dist = sonar->ReadFrontSonar();
							sprintf(buffer, "Rear sonar: %d", dist);
							puts(buffer);
							slog.LogEntry(buffer);
							dist = sonar->ReadRearSonar();
							sprintf(buffer, "Front sonar: %d", dist);
							puts(buffer);
							slog.LogEntry(buffer);
							}
						mclm = MCLinearMotion::Instance();
						mcsm = MCSpinMotion::Instance();
						tlm = TLinearMotion::Instance();
						rm = RefMotion::Instance();
						vs = VoltSensor::Instance();
						if (!vs->Init())
							{
							slog.LogEntry((char *) "Volt sensor initialization failed.");
							puts("Volt sensor initialization failed.");
							}
						else
							{
							volts = vs->GetVolts();
							sprintf(buffer, "Volts: %.2f", volts);
							slog.LogEntry(buffer);
							puts(buffer);
							}
						pthread_join(udpserver, NULL);
						sonar->Close();
						app_log.CloseLog((char *) "Motion controller log closed");
						}
					else
						{
						slog.LogEntry((char *) "Could not create the UDP server thread");
						puts("Could not create the UDP server thread");
						}
					}
				else
					{
					slog.LogEntry((char *) "Could not open a UDP socket");
					puts("Could not create the UDP server thread");
					}
				sensor->Close();
				}
			else
				{
				slog.LogEntry((char *) "Could not initialize the MPU6050");
				puts("Could not initialize the MPU6050");
				}
			mc.Close();
			}
		else
			{
			slog.LogEntry((char *) "Could not open the motor controller");
			puts("Could not open the motor controller");
			}
		}
	else
		{
		slog.LogEntry((char *) "Could not read parameter file");
		puts("Could not read parameter file");
		}
	slog.CloseLog((char *) "start up log closed");
	io->RunLed(false);
	io->Close();
}
