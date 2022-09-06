#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <pthread.h>
#include <signal.h>
#include <math.h>
#include <time.h>
#include <sys/time.h>
#include "SharedData.h"
#include "MCSpinMotion.h"
#include "MotionController.h"


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
#define CONNECTION_ERROR_TIME	1500
#define START_CONNECTION_ERROR_TIME 500
#define SLOW_SPEED_MAX_ANGLE 10


MCSpinMotion* MCSpinMotion::sm = NULL;
double MCSpinMotion::raw_gyro[DATA_CAPTURE];
int MCSpinMotion::st[DATA_CAPTURE];
int MCSpinMotion::enc[DATA_CAPTURE];
unsigned char MCSpinMotion::notes[DATA_CAPTURE];
int MCSpinMotion::sample;
unsigned long long MCSpinMotion::start_time, MCSpinMotion::last_time;
double MCSpinMotion::last_v;
double MCSpinMotion::dist;
bool MCSpinMotion::encoder_run = false;
int MCSpinMotion::encoder = 0;
mcturn_param MCSpinMotion::tp;
char MCSpinMotion::motion_error[100];
bool MCSpinMotion::stopping = false, MCSpinMotion::starting = false;
int MCSpinMotion::samples;
int MCSpinMotion::same_count = 0;
sem_t MCSpinMotion::motion_complete;
int MCSpinMotion::missed_samples;
int MCSpinMotion::sample_overflow;
double MCSpinMotion::sd_dist;


void MCSpinMotion::StopAlarm()

{
	struct itimerval itm;

	itm.it_value.tv_sec = 0;
	itm.it_value.tv_usec = 0;
	itm.it_interval = itm.it_value;
	setitimer(ITIMER_REAL, &itm, NULL);
}



void MCSpinMotion::CalcPosition(double dt, double gyro_z)

{
	dist += ((last_v + gyro_z) / 2) * dt;
	last_v = gyro_z;
}



void *MCSpinMotion::AlarmHandler(void *)

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
		if (sig == SIGALRM)
			{
			gettimeofday(&tv, NULL);
			msec = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
			if (((last_time > 0) && (msec - last_time > CONNECTION_ERROR_TIME)) || ((last_time == 0) && (msec - start_time > START_CONNECTION_ERROR_TIME)))
				{
				StopMotion();
				StopAlarm();
				strcpy(motion_error,MPU_FAIL);
				sem_post(&motion_complete);
				encoder_run = false;
				pthread_exit(NULL);
				}
			if (sensor->GetSensorData(&sd))
				{
				note = 0;
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
						strcpy(motion_error,START_TIMEOUT);
						note = 3;
						}
					else if (abs(encoder) >  0)
						{
						starting = false;
						note = 1;
						samples = 0;
						CalcPosition(dt, gyro_z);
						}
					}
				else if (stopping)
					{
					if (samples == 0)
						note = 4;
					samples += 1;
					CalcPosition(dt, gyro_z);
					if (samples == 1)
						{
						same_count = 0;
						}
					else if (samples == STOP_TIME_OUT)
						{
						StopMotion();
						strcpy(motion_error,START_TIMEOUT);
						note = 3;
						}
					else if ((int)enc[sample - 1] == encoder)
						{
						same_count += 1;
						if (same_count == STOP_DETECT_COUNT)
							note = 2;
						}
					else
						{
						same_count = 0;
						sd_dist = dist;
						}
					}
				else
					{
					CalcPosition(dt, gyro_z);
					if (fabs(dist) + tp.stop_offset >= tp.target_angle)
						{
						StopMotion();
						starting = false;
						stopping = true;
						samples = 0;
						}
					}
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
					sem_post(&motion_complete);
					encoder_run = false;
					pthread_exit(NULL);
					}
				}
			else if (sig == SIGALRM)
				missed_samples += 1;
			}
		}
}



void MCSpinMotion::StopMotion()

{
	mc.StopMotion();
	samples = 0;
}



void* MCSpinMotion::EncoderUpdateThread(void *)

{
	pthread_detach(pthread_self());
	while (encoder_run)
		{
		mc.ReadEncoder(&encoder);
		usleep(10000);
		}
}



bool MCSpinMotion::ReadParameters(char *fname)

{
	FILE *pfile = NULL;
	char line[128];
	bool rtn = false;
	char* pos;

	if ((pfile = fopen(fname, "r")) != NULL)
		{
		if (fgets(line, sizeof(line) - 1, pfile) != NULL)
			{
			tp.max_speed = atoi(line);
			if (fgets(line, sizeof(line) - 1, pfile) != NULL)
				{
				tp.accel = atoi(line);
				if (fgets(line, sizeof(line) - 1, pfile) != NULL)
					{
					pos = strchr(line, ',');
					if (pos != 0)
						{
						tp.so_slope = atof(line);
						pos += 1;
						tp.so_intercept = atof(pos);
						if ((tp.max_speed > 0) && (tp.accel > 0) && (tp.so_intercept != 0))
							rtn = true;
						}
					}
				}
			}
		fclose(pfile);
		}
	return(rtn);
}



bool MCSpinMotion::SetTurnParameters(spin_directions direction,int angle,spin_speed ss)

{
	bool rtn = false;
	char fname[256];
	char ssname[10];

	tp.direction = direction;
	tp.so_intercept = 0;
	tp.so_slope = 0;
	if ((ss == SNORMAL) && (angle < SLOW_SPEED_MAX_ANGLE))
		tp.ss = SSLOW;
	else
		tp.ss = ss;
	if (tp.ss == SSLOW)
		strcpy(ssname, "SLOW");
	else if (tp.ss == SNORMAL)
		strcpy(ssname, "NORMAL");
	else if (tp.ss == SMIDDLE)
		strcpy(ssname, "MIDDLE");
	else if (tp.ss == SFAST)
		strcpy(ssname, "FAST");
	sprintf(fname, "%s%s%s%s", BASE_DIR, CAL_DIR, ssname, PARAM_FILE);
	if (ReadParameters(fname))
		{
		tp.volts = vs->GetVolts();
		tp.stop_offset = (tp.volts * tp.so_slope) + tp.so_intercept;
		rtn = true;
		}
	else
		{
		if (tp.ss == SSLOW)
			{
			rtn = true;
			tp.volts = vs->GetVolts();
			tp.max_speed = 20;
			tp.accel = 5;
			tp.stop_offset = .7;
			}
		else if (tp.ss == SNORMAL)
			{
			rtn = true;
			tp.volts = vs->GetVolts();
			tp.max_speed = 30;
			tp.accel = 5;
			tp.stop_offset = 3.16;
			}
	}
	return(rtn);
}



MCSpinMotion::MCSpinMotion()

{
	alrm_handler = -1;
	sem_init(&motion_complete, 0, 0);
}



MCSpinMotion* MCSpinMotion::Instance()

{
	if (sm == NULL)
		{
		sm = new MCSpinMotion;
		}
	return(sm);
}


bool MCSpinMotion::StartSpin(spin_directions direc,int angle, spin_speed ss)

{
	bool rtn = false;
	pthread_t exec;
	struct itimerval itm;
	struct timeval tv;

	if (SetTurnParameters(direc,angle, ss))
		{
		tp.gbias = sensor->gbias;
		tp.target_angle = angle;
		mc.SetAccel(tp.accel);
		mc.SetMode(0);
		mc.DisableRegulator();
		mc.DisableTimeout();
		stopping = false;
		mc.ResetEncoders();
		encoder = 0;
		encoder_run = true;
		sample = 0;
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
			if (tp.direction == RIGHT)
				mc.StartRightSpin(tp.max_speed);
			else
				mc.StartLeftSpin(tp.max_speed);
			gettimeofday(&tv, NULL);
			start_time = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
			if (pthread_create(&alrm_handler, NULL, AlarmHandler, this) == 0)
				{
				itm.it_value.tv_sec = 0;
				itm.it_value.tv_usec = 10000;
				itm.it_interval = itm.it_value;
				setitimer(ITIMER_REAL, &itm, NULL);
				sem_wait(&motion_complete);
				if (strlen(motion_error) == 0)
					rtn = true;
				}
			else
				{
				StopSpin();
				encoder_run = false;
				strcpy(motion_error, "Could not start alarm handling thread.");
				}
			}
		else
			strcpy(motion_error,"Could not start encoder thread.");
		}
	else
		strcpy(motion_error, "Could not set motion parameters.");
	return(rtn);
}



void MCSpinMotion::StopSpin()

{
	if (!stopping)
		{
		StopMotion();
		starting = false;
		stopping = true;
		}
}



char * MCSpinMotion::MotionError()

{
	return(motion_error);
}



void MCSpinMotion::RecordSensorData()

{
	Log sd_log;
	time_t tt;
	tm* tmp;
	char buffer[200];
	char note[20];
	int i;

	time(&tt);
	sprintf(buffer, "%s%sMCSpinSensorData%ld.csv", BASE_DIR, DATA_DIR, tt);
	if (sd_log.OpenLog(buffer, (char *)  "MC Spin sensor data log", false))
		{
		strcpy(last_turn_file, buffer);
		tmp = localtime(&tt);
		sprintf(buffer, "%d/%d/%d  %d:%d:%d", tmp->tm_mon + 1, tmp->tm_mday, tmp->tm_year + 1900, tmp->tm_hour, tmp->tm_min,tmp->tm_sec);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Target motion (°): %.2f", tp.target_angle);
		sd_log.LogEntry(buffer);
		sd_log.LogEntry((char *) "Calculated motion (°):");
		sprintf(buffer, "  @ stop detected - %.2f", dist);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "  @ encoder stop - %.2f", sd_dist);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Gyro bias (deg/sec): %.3f", sensor->gbias);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Max speed: %d", tp.max_speed);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Accel: %d", tp.accel);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Stop offset (°): %.2f",tp.stop_offset);
		sd_log.LogEntry(buffer);
		if ((tp.so_slope != 0) || (tp.so_intercept != 0))
			{
			sprintf(buffer,"SO slope: %f",tp.so_slope);
			sd_log.LogEntry(buffer);
			sprintf(buffer,"SO intercept: %f",tp.so_intercept);
			sd_log.LogEntry(buffer);
			}
		if (tp.direction == RIGHT)
			sd_log.LogEntry((char *) "Direction: right");
		else
			sd_log.LogEntry((char *) "Direction: left");
		sprintf(buffer, "Motion error: %s", motion_error);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Voltage: %.2f", tp.volts);
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
				strcpy(note, NOTE_1);
			else if (notes[i] == 2)
				strcpy(note, NOTE_2);
			else if (notes[i] == 3)
				strcpy(note, NOTE_3);
			else if (notes[i] == 4)
				strcpy(note, NOTE_4);
			sprintf(buffer, "%ld,%s,%.3f,%d", st[i], note, raw_gyro[i], enc[i]);
			sd_log.LogEntry(buffer);
			}
		sd_log.CloseLog((char *) "");
		}
}

