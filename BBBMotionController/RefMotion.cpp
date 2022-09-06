
#include <time.h>
#include <sys/time.h>
#include <signal.h>
#include <string.h>
#include "MotionController.h"
#include "SharedData.h"
#include "RefMotion.h"


#define BASE_STOP_TIME_OUT 75
#define SLOW_STOP_TIME_OUT 188
#define START_TIME_OUT 60
#define STOP_DETECT_COUNT 15
#define DATA_CAPTURE 2000
#define SLOW_DECEL_LIMIT 45

RefMotion* RefMotion::rm = NULL;
EzSSonar* RefMotion::sonr = NULL;
unsigned long long RefMotion::start_time, RefMotion::last_time, RefMotion::sd_time;
bool RefMotion::encoder_run, RefMotion::encoder_sample;
int RefMotion::encoder, RefMotion::last_encoder;
char RefMotion::motion_error[100];
bool RefMotion::stopping, RefMotion::starting;
int RefMotion::samples;
sem_t RefMotion::motion_complete;
int RefMotion::same_count;
mtr_param RefMotion::rmmp;
int RefMotion::sample;
int RefMotion::st[DATA_CAPTURE];
int RefMotion::rmtr[DATA_CAPTURE];
int RefMotion::lmtr[DATA_CAPTURE];
double RefMotion::rangle;
double RefMotion::last_gz;
int RefMotion::stop_time_out;


void RefMotion::StopAlarm()

{
	struct itimerval itm;

	itm.it_value.tv_sec = 0;
	itm.it_value.tv_usec = 0;
	itm.it_interval = itm.it_value;
	setitimer(ITIMER_REAL, &itm, NULL);
}



void RefMotion::CalcPosition(double dt,double gyro_z)

{
	rangle += ((last_gz + gyro_z) / 2) * dt;
	last_gz = gyro_z;
}



void *RefMotion::AlarmHandler(void *)

{
	double dt;
	struct timeval tv;
	unsigned long long msec;
	unsigned char note = 0;
	sigset_t wait_sigs;
	int sig;
	int correction,delta_correct;
	struct sensor_data sd;
	double gyro_z;

	sample = 0;
	pthread_detach(pthread_self());
	sigemptyset(&wait_sigs);
	sigaddset(&wait_sigs, SIGALRM);
	while (true)
		{
		sigwait(&wait_sigs, &sig);
		if (sig == SIGALRM)
			{
			gettimeofday(&tv, NULL);
			if (sensor->GetSensorData(&sd))
				{
				note = 0;
				gyro_z = (((double)sd.gyro_z / 32767) * 250) - sensor->gbias;
				msec = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
				if (last_time > 0)
					{
					dt = msec - last_time;
					dt /= 1000;
					}
				else
					dt = 0;
				last_time = msec;
				CalcPosition(dt, gyro_z);
				if (!ClearanceOk())
					{
					note = 5;
					strcpy(motion_error,(char *) "insufficient clearance");
					app_log.LogEntry((char *) "insufficient clearance");
					}
				if (starting)
					{
					samples += 1;
					if (samples == START_TIME_OUT)
						{
						StopMotion();
						encoder_run = false;
						strcpy(motion_error, (char *) "start timedout");
						note = 3;
						StopAlarm();
						sem_post(&motion_complete);
						app_log.LogEntry((char *) "Start timeout");
						pthread_exit(NULL);
						}
					else if (abs(encoder) >  0)
						{
						starting = false;
						note = 1;
						samples = 0;
						sd_time = msec;
						encoder_sample = false;
						sem_post(&motion_complete);
						app_log.LogEntry((char *) "Started");
						}
					}
				else if (stopping)
					{
					if ((samples == 0) && (note != 5) && (note != 3))
						note = 4;
					samples += 1;
					if (samples == stop_time_out)
						{
						StopMotion();
						StopAlarm();
						strcpy(motion_error,"stop time out");
						note = 3;
						sem_post(&motion_complete);
						encoder_run = false;
						rm->RecordMotionData();
						app_log.LogEntry((char *) "Stop time out");
						pthread_exit(NULL);
						}
					else if (last_encoder == encoder)
						{
						same_count += 1;
						if (same_count == STOP_DETECT_COUNT)
							{
							StopAlarm();
							note = 2;
							}
						}
					else
						same_count = 0;
					last_encoder = encoder;
					}
				if (sample < DATA_CAPTURE)
					{
					st[sample] = msec - start_time;
					rmtr[sample] = rmmp.rnow;
					lmtr[sample] = rmmp.lnow;
					sample += 1;
					}
				if (note == 2)
					{
					sem_post(&motion_complete);
					encoder_run = false;
					encoder_sample = false;
					rm->RecordMotionData();
					app_log.LogEntry((char *) "Stopped");
					pthread_exit(NULL);
					}
				}
			}
		}
}



void RefMotion::StopMotion()

{
	if ((rmmp.lnow < SLOW_DECEL_LIMIT) || (rmmp.rnow < SLOW_DECEL_LIMIT))
		{
		mc.SetAccel(2);
		stop_time_out = SLOW_STOP_TIME_OUT;
		}
	mc.StopMotion();
	sonr->SetSampling(NONE);
	rmmp.rnow = rmmp.lnow = 128;
	samples = 0;
	last_encoder = 0;
	same_count = 0;
	encoder_sample = true;
	starting = false;
	stopping = true;
}



bool RefMotion::ClearanceOk()

{
	bool rtn = true;
	int dist;

	if ((!starting) && (!stopping))
		{
		dist = sonr->front_clearance;
		if ((dist != -1) && (dist < MIN_FRONT_CLEARANCE))
			{
			StopMotion();
			rtn = false;
			}
		}
	return(rtn);
}



void* RefMotion::EncoderUpdateThread(void *)

{
	pthread_detach(pthread_self());
	while (encoder_run)
		{
		if (encoder_sample)
			mc.ReadEncoder(&encoder);
		usleep(10000);
		}
}



bool RefMotion::ChngSpeed(int right, int left)

{
	bool rtn = true;
	int rwas,lwas;

	if ((!stopping) && (!starting))
		{
		if ((right != 0) || (left != 0))
			{
			rwas = rmmp.rnow;
			lwas = rmmp.lnow;
			rmmp.rnow -= right;
			rmmp.lnow -= left;
			if ((rmmp.rnow == 128) && (rmmp.lnow == 128))
				{
				if ((rwas < 45) || (lwas < 45))
					mc.SetAccel(2);
				}
			if (rmmp.rnow < 0)
				{
				rmmp.lnow += abs(rmmp.rnow);
				rmmp.rnow = 0;
				}
			if (rmmp.lnow < 0)
				{
				rmmp.rnow += abs(rmmp.lnow);
				rmmp.lnow = 0;
				}
			if (rmmp.rnow > 128)
				rmmp.rnow = 128;
			if (rmmp.lnow > 128)
				rmmp.lnow = 128;
			if (encoder_run && encoder_sample)
				app_log.LogEntry("RefMotion::ChngSpeed encoder sampling on");
			mc.ChngSpeed(rmmp.rnow,rmmp.lnow);
			}
		}
	else
		rtn = false;
	return(rtn);
}



RefMotion::RefMotion()

{
	alrm_handler = -1;
	sonr = EzSSonar::Instance();
	sem_init(&motion_complete, 0, 0);
}



RefMotion* RefMotion::Instance()

{
	if (rm == NULL)
		{
		rm = new RefMotion;
		}
	return(rm);
}



bool RefMotion::StartRM(int speed)

{
	bool rtn = false;
	pthread_t exec;
	struct itimerval itm;
	struct timeval tv;

	mc.SetAccel(5);
	stop_time_out = BASE_STOP_TIME_OUT;
	mc.SetMode(0);
	mc.DisableRegulator();
	mc.DisableTimeout();
	mc.ResetEncoders();
	motion_error[0] = 0;
	starting = true;
	stopping = false;
	encoder_run = true;
	encoder_sample = true;
	encoder = 0;
	rangle = 0;
	last_gz = 0;
	rangle = 0;
	if (pthread_create(&exec, NULL, EncoderUpdateThread, 0) == 0)
		{
		mc.StartForward(speed);
		rmmp.rnow = rmmp.lnow = 128 - speed;
		if (pthread_create(&alrm_handler, NULL, AlarmHandler, this) == 0)
			{
			sonr->SetSampling(FRONT);
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
			{
			StopMotion();
			encoder_run = false;
			strcpy(motion_error, "Could not start alarm handling thread.");
			}
		}
	else
		{
		encoder_run = false;
		strcpy(motion_error, "Could not start encoder update thread.");
		}
}



void RefMotion::StopRM()

{
	struct timespec ts;
	int r;

	if (encoder_run && !stopping)
		{
		sem_trywait(&motion_complete);
		StopMotion();
		sem_wait(&motion_complete);
		}
	else if (stopping)
		sem_wait(&motion_complete);
	else
		sem_trywait(&motion_complete);
}



bool RefMotion::ChangeSpeed(int right, int left)

{
	return(ChngSpeed(right, left));
}



char * RefMotion::MotionError()

{
	return(motion_error);
}



void RefMotion::RecordMotionData()

{

	Log sd_log;
	time_t tt;
	tm* tmp;
	char buffer[200];
	char note[30];
	int i;

	time(&tt);
	sprintf(buffer, "%s%sRefMotionData%ld.csv", BASE_DIR, DATA_DIR, tt);
	if (sd_log.OpenLog(buffer, (char *)  "Reference motion data log", false))
		{
		strcpy(last_move_file, buffer);
		tmp = localtime(&tt);
		sprintf(buffer, "%d/%d/%d  %d:%d:%d", tmp->tm_mon + 1, tmp->tm_mday, tmp->tm_year + 1900, tmp->tm_hour, tmp->tm_min, tmp->tm_sec);
		sd_log.LogEntry(buffer);
		sd_log.LogEntry((char *) "");
		sd_log.LogEntry((char *) "Timestamp (msec),Right motor setting,Left motor setting");
		for (i = 0; i < sample; i++)
			{
			sprintf(buffer, "%ld,%d,%d", st[i], rmtr[i], lmtr[i]);
			sd_log.LogEntry(buffer);
			}
		sd_log.CloseLog((char *) "");
		}
}



double RefMotion::RelativeAngle()

{
	return(rangle);
}