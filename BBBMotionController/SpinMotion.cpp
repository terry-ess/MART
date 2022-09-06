#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <pthread.h>
#include <signal.h>
#include <math.h>
#include <time.h>
#include <sys/time.h>
#include "SharedData.h"
#include "SpinMotion.h"
#include "MotionController.h"
#include "VoltSensor.h"


#define STOP_TIME_OUT 75
#define START_TIME_OUT 60
#define STOP_DETECT_COUNT 15
#define PARAM_FILE "sm.param"

#define NOTE_0 ""
#define NOTE_1 "start detected"
#define NOTE_2 "stop detected"
#define NOTE_3 "error"
#define NOTE_4 "stop issued"
#define DATA_CAPTURE 3000


SpinMotion* SpinMotion::sm = NULL;
double SpinMotion::raw_gyro[DATA_CAPTURE];
int SpinMotion::st[DATA_CAPTURE];
int SpinMotion::enc[DATA_CAPTURE];
unsigned char SpinMotion::notes[DATA_CAPTURE];
int SpinMotion::sample;
unsigned long long SpinMotion::start_time, SpinMotion::last_time;
double SpinMotion::last_v;
double SpinMotion::dist;
bool SpinMotion::encoder_run = false;
int SpinMotion::encoder = 0;
turn_param SpinMotion::tp;
char SpinMotion::motion_error[100];
bool SpinMotion::stopping = false, SpinMotion::starting = false;
int SpinMotion::samples;
int SpinMotion::same_count = 0;
sem_t SpinMotion::motion_complete;
int SpinMotion::missed_samples;
int SpinMotion::sample_overflow;


void SpinMotion::StopAlarm()

{
	struct itimerval itm;

	itm.it_value.tv_sec = 0;
	itm.it_value.tv_usec = 0;
	itm.it_interval = itm.it_value;
	setitimer(ITIMER_REAL, &itm, NULL);
}



void SpinMotion::CalcPosition(double dt, double gyro_z)

{
	dist += ((last_v + gyro_z) / 2) * dt;
	last_v = gyro_z;
}



void *SpinMotion::AlarmHandler(void *)

{
	struct sensor_data sd;
	double gyro_z;
	struct timeval tv;
	unsigned long long msec;
	double dt;
	unsigned char note = 0;
	sigset_t wait_sigs;
	int sig;

	pthread_detach(pthread_self());
	sigemptyset(&wait_sigs);
	sigaddset(&wait_sigs, SIGALRM);
	while (true)
		{
		sigwait(&wait_sigs, &sig);
		if ((sig == SIGALRM) && (sensor->GetSensorData(&sd)))
			{
			gettimeofday(&tv, NULL);
			note = 0;
			msec = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
			gyro_z = (((double)sd.gyro_z / 32767) * 250) - sensor->gbias;
			if (last_time > 0)
				{
				dt = msec - last_time;
				dt /= 1000;
				}
			else
				dt = 0;
			last_time = msec;
			if (starting)
				{
				samples += 1;
				if (samples == START_TIME_OUT)
					{
					StopMotion();
					starting = false;
					strcpy(motion_error, "start timedout");
					note = 3;
					}
				else if (abs(encoder) >  0)
					{
					starting = false;
					note = 1;
					samples = 0;
					CalcPosition(dt,gyro_z);
					sem_post(&motion_complete);
					}
				}
			else if (stopping)
				{
				if (samples == 0)
					note = 4;
				samples += 1;
				CalcPosition(dt, gyro_z);
				if (samples == 1)
					same_count = 0;
				else if (samples == STOP_TIME_OUT)
					{
					StopMotion();
					strcpy(motion_error, "stop time out");
					note = 3;
					}
				else if ((int)enc[sample - 1] == encoder)
					{
					same_count += 1;
					if (same_count == STOP_DETECT_COUNT)
						note = 2;
					}
				else
					same_count = 0;
				}
			else
				CalcPosition(dt, gyro_z);
			if (sample < DATA_CAPTURE)
				{
				st[sample] = msec - start_time;
				raw_gyro[sample] = gyro_z;
				enc[sample] = encoder;
				notes[sample] = note;
				sample += 1;
				}
			else
				sample_overflow += 1;
			if ((note == 2) || (note == 3))
				{
				StopAlarm();
				encoder_run = false;
				sem_post(&motion_complete);
				pthread_exit(NULL);
				}
			}
		else if (sig == SIGALRM)
			missed_samples += 1;
		}
}



void SpinMotion::StopMotion()

{
	mc.StopMotion();
	samples = 0;
}



void* SpinMotion::EncoderUpdateThread(void *)

{
	pthread_detach(pthread_self());
	while (encoder_run)
		{
		mc.ReadEncoder(&encoder);
		usleep(10000);
		}
}



bool SpinMotion::ReadParameters(char *fname)

{
	FILE *pfile = NULL;
	char line[128];
	bool rtn = false;

	if ((pfile = fopen(fname, "r")) != NULL)
		{
		fgets(line, sizeof(line) - 1, pfile);
		tp.max_speed = atoi(line);
		fgets(line, sizeof(line) - 1, pfile);
		tp.accel = atoi(line);
		fclose(pfile);
		rtn = true;
		}
	return(rtn);
}



void SpinMotion::SetParam(int accel,int mspeed)

{
	SpinMotion::accel = accel;
	max_speed = mspeed;
}



bool SpinMotion::SetTurnParameters(spin_directions direction, spin_speed ss)

{
	bool rtn = false;
	char fname[256];
	char ssname[10];

	tp.direction = direction;
	tp.ss = ss;
	if (ss == SSLOW)
		strcpy(ssname,"SLOW");
	else if (ss == SNORMAL)
		strcpy(ssname,"NORMAL");
	else if (ss == SMIDDLE)
		strcpy(ssname,"MIDDLE");
	else if (ss == SFAST)
		strcpy(ssname,"FAST");
	sprintf(fname, "%s%s%s%s", BASE_DIR,CAL_DIR,ssname, PARAM_FILE);
	if ((ss == SCUSTOM) || ((rtn = ReadParameters(fname)) == false))
		{
		if (ss == SSLOW)
			{
			rtn = true;
			tp.max_speed = 20;
			tp.accel = 5;
			}
		else if (ss == SNORMAL)
			{
			rtn = true;
			tp.ss = SNORMAL;
			tp.max_speed = 30;
			tp.accel = 5;
			}
		else if (ss == SMIDDLE)
			{
			rtn = true;
			tp.ss = SMIDDLE;
			tp.accel = 5;
			tp.max_speed = 60;
			}
		else if (ss == SFAST)
			{
			rtn = true;
			tp.ss = SFAST;
			tp.accel = 5;
			tp.max_speed = 120;
			}
		else if (ss == SCUSTOM)
			{
			rtn = true;
			tp.ss = SCUSTOM;
			tp.accel = accel;
			tp.max_speed = max_speed;
			}
		}
	return(rtn);
}



SpinMotion::SpinMotion()

{
	alrm_handler = -1;
	sem_init(&motion_complete, 0, 0);
}



SpinMotion* SpinMotion::Instance()

{
	if (sm == NULL)
		{
		sm = new SpinMotion;
		}
	return(sm);
}


bool SpinMotion::StartSpin(spin_directions direc, spin_speed ss)

{
	bool rtn = false;
	pthread_t exec;
	struct itimerval itm;
	struct timeval tv;

	if (SetTurnParameters(direc, ss))
		{
		tp.gbias = sensor->gbias;
		mc.SetAccel(tp.accel);
		mc.SetMode(0);
		mc.DisableRegulator();
		mc.DisableTimeout();
		stopping = false;
		mc.ResetEncoders();
		encoder = 0;
		encoder_run = true;
		samples = 0;
		last_v = 0;
		last_time = 0;
		dist = 0;
		missed_samples = 0;
		sample_overflow = 0;
		motion_error[0] = 0;
		starting = true;
		stopping = false;
		if (pthread_create(&exec, NULL, EncoderUpdateThread, 0) == 0)
			{
			volts = vs->GetVolts();
			if (tp.direction == RIGHT)
				mc.StartRightSpin(tp.max_speed);
			else
				mc.StartLeftSpin(tp.max_speed);
			sample = 0;
			if (pthread_create(&alrm_handler, NULL, AlarmHandler, this) == 0)
				{
				itm.it_value.tv_sec = 0;
				itm.it_value.tv_usec = 10000;
				itm.it_interval = itm.it_value;
				gettimeofday(&tv, NULL);
				start_time = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
				setitimer(ITIMER_REAL, &itm, NULL);
				sem_wait(&motion_complete);
				if (strlen(motion_error) == 0)
					rtn = true;
				}
			else
				strcpy(motion_error, "Could not start alarm handling thread.");
			}
		}
	else
		strcpy(motion_error,"Could not set motion parameters.");
	return(rtn);
}



void SpinMotion::StopSpin()

{
	if (!stopping)
		{
		sem_trywait(&motion_complete);
		StopMotion();
		starting = false;
		stopping = true;
		sem_wait(&motion_complete);
		}
}



char * SpinMotion::MotionError()

{
	return(motion_error);
}



int SpinMotion::LastMoveAngle()

{
	return(round(dist));
}


void SpinMotion::RecordSensorData()

{
	Log sd_log;
	time_t tt;
	tm* tmp;
	char buffer[200];
	char note[20];
	int i;

	time(&tt);
	sprintf(buffer, "%s%s/SpinSensorData%ld.csv", BASE_DIR, DATA_DIR, tt);
	if (sd_log.OpenLog(buffer, (char *)  "Spin sensor data log", false))
		{
		strcpy(last_turn_file, buffer);
		tmp = localtime(&tt);
		sprintf(buffer,"%d/%d/%d  %d:%d:%d",tmp->tm_mon + 1,tmp->tm_mday,tmp->tm_year + 1900,tmp->tm_hour,tmp->tm_min,tmp->tm_sec);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Calculated motion (°): %.2f", dist);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Gyro bias (deg/sec): %.3f", sensor->gbias);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Max speed: %d",tp.max_speed);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Accel: %d",tp.accel);
		sd_log.LogEntry(buffer);
		if (tp.direction == RIGHT)
			sd_log.LogEntry((char *) "Direction: right");
		else
			sd_log.LogEntry((char *) "Direction: left");
		sprintf(buffer,"Motion error: %s",motion_error);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Voltage: %.3f",volts );
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Missed sensor samples: %d", missed_samples);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Sensor sample overflow: %d", sample_overflow);
		sd_log.LogEntry(buffer);
		sd_log.LogEntry((char *) "");
		sd_log.LogEntry((char *) "Timestamp (msec),Note,Raw Gyro (°/sec),Encoder count");
		for (i = 0; i < sample; i++)
			{
			note[0] = 0;
			if (notes[i] == 1)
				strcpy(note,NOTE_1);
			else if (notes[i] == 2)
				strcpy(note,NOTE_2);
			else if (notes[i] == 3)
				strcpy(note,NOTE_3);
			else if (notes[i] == 4)
				strcpy(note, NOTE_4);
			sprintf(buffer, "%ld,%s,%.3f,%d", st[i], note, raw_gyro[i], enc[i]);
			sd_log.LogEntry(buffer);
			}
		sd_log.CloseLog((char *) "");
	}
}

