
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <pthread.h>
#include <signal.h>
#include <math.h>
#include <time.h>
#include <sys/time.h>
#include "SharedData.h"
#include "LinearMotion.h"
#include "MotionController.h"


#define PARAM_FILE "lm.param"
#define STOP_TIME_OUT 75
#define START_TIME_OUT 60
#define STOP_DETECT_COUNT 15

#define NOTE_0 ""
#define NOTE_1 "start detected"
#define NOTE_2 "stop detected"
#define NOTE_3 "error"
#define NOTE_4 "stop issued"
#define NOTE_5	"clearance error"
#define DATA_CAPTURE 2000
#define CORRECT_DELAY  250


LinearMotion* LinearMotion::lm = NULL;
//EzSonar *LinearMotion::sonr = NULL;
EzSSonar *LinearMotion::sonr = NULL;
double LinearMotion::raw_accel_y[DATA_CAPTURE];
//double LinearMotion::raw_accel_x[DATA_CAPTURE];
//double LinearMotion::raw_accel_z[DATA_CAPTURE];
//double LinearMotion::raw_gyro_y[DATA_CAPTURE];
//double LinearMotion::raw_gyro_x[DATA_CAPTURE];
double LinearMotion::raw_gyro_z[DATA_CAPTURE];
int LinearMotion::st[DATA_CAPTURE];
int LinearMotion::enc[DATA_CAPTURE];
unsigned char LinearMotion::notes[DATA_CAPTURE];
//int LinearMotion::sonar[DATA_CAPTURE];
double LinearMotion::last_a, LinearMotion::last_v,LinearMotion::dist;
int LinearMotion::sample;
unsigned long long LinearMotion::start_time, LinearMotion::last_time, LinearMotion::sd_time;
bool LinearMotion::encoder_run = false;
int LinearMotion::encoder = 0;
linear_param LinearMotion::lp;
char LinearMotion::motion_error[100];
bool LinearMotion::stopping = false, LinearMotion::starting = false;
int LinearMotion::samples;
int LinearMotion::same_count = 0;
sem_t LinearMotion::motion_complete;
int LinearMotion::missed_samples;
int LinearMotion::sample_overflow;
pid_param LinearMotion::pp;
bool LinearMotion::pp_set;
double LinearMotion::sum_gh, LinearMotion::last_gh;
int LinearMotion::applied_correct;
bool LinearMotion::first_calc;
double LinearMotion::gyroheading, LinearMotion::oldgyrov;



void LinearMotion::StopAlarm()

{
	struct itimerval itm;

	itm.it_value.tv_sec = 0;
	itm.it_value.tv_usec = 0;
	itm.it_interval = itm.it_value;
	setitimer(ITIMER_REAL, &itm, NULL);
}



void LinearMotion::CalcPosition(double dt,double accel_y,double gyro_z)

{
	double  accel, aa, v;

	accel = (lp.fconstant * last_a) + ((1.0 - lp.fconstant) * accel_y);
	aa = ((last_a + accel) / 2) * G_FT_SEC2;
	last_a = accel;
	v = last_v + (aa * dt);
	dist += ((last_v + v) / 2) * dt;
	last_v = v;
	gyroheading += dt * ((oldgyrov + gyro_z) / 2);
	oldgyrov = gyro_z;
}



void *LinearMotion::AlarmHandler(void *)

{
	struct sensor_data sd;
	double accel_y, gyro_z,dt;
	struct timeval tv;
	unsigned long long msec;
	unsigned char note = 0;
	sigset_t wait_sigs;
	int sig;
	int correction,delta_correct;

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
			accel_y = (((double)sd.accel_y / 32767) * 2) - sensor->abias;
			gyro_z = (((double)sd.gyro_z / 32767) * 250) - sensor->gbias;
			if (last_time > 0)
				{
				dt = msec - last_time;
				dt /= 1000;
				}
			else
				dt = 0;
			last_time = msec;
			if (!ClearanceOk())
				{
				note = 5;
				starting = false;
				stopping = true;
				strcpy(motion_error, "insufficent clearence");
				app_log.LogEntry((char *) "insufficent clearence");
				}
			if (starting)
				{
				samples += 1;
				if (samples == START_TIME_OUT)
					{
					StopMotion();
					starting = false;
					encoder_run = false;
					strcpy(motion_error,"start timedout");
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
					sem_post(&motion_complete);
					CalcPosition(dt,accel_y,gyro_z);
					}
				}
			else if (stopping)
				{
				if ((samples == 0) && (note != 5) && (note != 3))
					note = 4;
				CalcPosition(dt, accel_y,gyro_z);
				samples += 1;
				if (samples == 1)
					{
					same_count = 0;
					}
				else if (samples == STOP_TIME_OUT)
					{
					StopMotion();
					StopAlarm();
					strcpy(motion_error,"stop time out");
					note = 3;
					sem_post(&motion_complete);
					encoder_run = false;
					app_log.LogEntry((char *) "Stop time out");
					pthread_exit(NULL);
					}
				else if ((int) enc[sample - 1] == encoder)
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
				}
			else
				{
				CalcPosition(dt, accel_y,gyro_z);
				if (pp_set && ((msec - sd_time) > CORRECT_DELAY))
					{
					sum_gh += gyroheading;
					correction = (int)(pp.pgain * gyroheading) + (pp.igain * sum_gh);
					if (first_calc)
						first_calc = false;
					else
						correction += (int)(pp.dgain * (gyroheading - last_gh));
					last_gh = gyroheading;
					delta_correct = correction - applied_correct;
					applied_correct += delta_correct;
					if (abs(applied_correct) >= 30)
						{
						StopMotion();
						starting = false;
						stopping = true;
						strcpy(motion_error, EXCESSIVE_GYRO_CORRECT);
						note = 3;
						app_log.LogEntry((char *) "Excessive gyro correction");
						}
					else if (delta_correct != 0)
						{
						ChngSpeed(-delta_correct, delta_correct);
						}
					}
				}
			if (sample < DATA_CAPTURE)
				{
				st[sample] = msec - start_time;
				raw_accel_y[sample] = accel_y;
				raw_gyro_z[sample] = gyro_z;
/*				raw_accel_y[sample] = ((double)sd.accel_y / 16383.5);
				raw_accel_x[sample] = ((double)sd.accel_x / 16283.5);
				raw_accel_z[sample] = ((double)sd.accel_z / 16283.5);
				raw_gyro_y[sample] = ((double)sd.gyro_y /131.068);
				raw_gyro_x[sample] = ((double)sd.gyro_x / 131.068);
				raw_gyro_z[sample] = ((double)sd.gyro_z / 131.068); */
				enc[sample] = encoder;
				notes[sample] = note;
/*				if (lp.direction == FORWARD)
					sonar[sample] = sonr->front_clearance;
				else
					sonar[sample] = sonr->rear_clearance; */
				sample += 1;
				}
			else
				sample_overflow += 1;
			if (note == 2)
				{
				sem_post(&motion_complete);
				encoder_run = false;
				app_log.LogEntry((char *) "Stopped");
				pthread_exit(NULL);
				}
			}
		else if (sig == SIGALRM)
			missed_samples += 1;
		}
}



void LinearMotion::StopMotion()

{
	mc.StopMotion();
	sonr->SetSampling(NONE);
	lp.rnow = lp.lnow = 128;
	samples = 0;
}



bool LinearMotion::ClearanceOk()

{
	bool rtn = true;
	int dist;

	if ((!starting) && (!stopping))
		{
		if (lp.direction == BACKWARD)
			{
			dist = sonr->rear_clearance;
			if ((dist != -1) && (dist < MIN_REAR_CLEARANCE))
				{
				StopMotion();
				rtn = false;
				}
			}
		else
			{
			dist = sonr->front_clearance;
			if ((dist != -1) && (dist < MIN_FRONT_CLEARANCE))
				{
				StopMotion();
				rtn = false;
				}
			}
		}
	return(rtn);
}



void* LinearMotion::EncoderUpdateThread(void *)

{
	pthread_detach(pthread_self());
	while (encoder_run)
		{
		mc.ReadEncoder(&encoder);
		usleep(10000);
		}
}



void LinearMotion::StartBackward()

{
	mc.StartBackward(lp.max_speed);
	lp.rnow = lp.lnow = 128 + lp.max_speed;
}



void LinearMotion::StartForward()

{
	mc.StartForward(lp.max_speed);
	lp.rnow = lp.lnow = 128 - lp.max_speed;
}



bool LinearMotion::ChngSpeed(int right, int left)

{
	bool rtn = true;

	if (!stopping)
		{
		lp.rnow -= right;
		lp.lnow -= left;
		if (lp.direction == FORWARD)
			{
			if (lp.rnow < 0)
				{
				lp.lnow += abs(lp.rnow);
				lp.rnow = 0;
				}
			if (lp.lnow < 0)
				{
				lp.rnow += abs(lp.lnow);
				lp.lnow = 0;
				}
			if (lp.rnow > 128)
				lp.rnow = 128;
			if (lp.lnow > 128)
				lp.lnow = 128;
			}
		else
			{
			if (lp.rnow > 255)
				{
				lp.lnow -= lp.rnow - 255;
				lp.rnow = 255;
				}
			if (lp.lnow > 255)
				{
				lp.rnow -= lp.lnow - 255;
				lp.lnow = 255;
				}
			if (lp.rnow < 128)
				lp.rnow = 128;
			if (lp.lnow < 128)
				lp.lnow = 128;
			}
		mc.ChngSpeed(lp.rnow,lp.lnow);
		}
	else
		rtn = false;
	return(rtn);
}



bool LinearMotion::ReadParameters(char *fname)

{
	FILE *pfile = NULL;
	char line[128];
	bool rtn = false;

	if ((pfile = fopen(fname, "r")) != NULL)
		{
		fgets(line, sizeof(line) - 1, pfile);
		lp.max_speed = atoi(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp.accel = atoi(line);
		fgets(line, sizeof(line) - 1, pfile);
		fgets(line, sizeof(line) - 1, pfile);
		fgets(line, sizeof(line) - 1, pfile);
		fgets(line, sizeof(line) - 1, pfile);
		fgets(line, sizeof(line) - 1, pfile);
		lp.fconstant = atof(line);
		fgets(line, sizeof(line) - 1, pfile);
		fclose(pfile);
		rtn = true;
		}
	return(rtn);
}



bool LinearMotion::SetLinearParameters(linear_directions direc, linear_speed ls)

{
	bool rtn = false;
	char fname[256];
	char bname[10];

	lp.direction = direc;
	lp.ls = ls;
	if (ls == NORMAL)
		strcpy(bname, "NORMAL");
	else if (ls == FAST)
		strcpy(bname, "FAST");
	else
		strcpy(bname,"SLOW");
	sprintf(fname, "%s%s%s%s", BASE_DIR,CAL_DIR, bname, PARAM_FILE);
	if ((rtn = ReadParameters(fname)) == false)
		{
		if (ls == NORMAL)
			{
			rtn = true;
			lp.accel = 5;
			lp.max_speed = 30;
			lp.fconstant = .8;
			}
		else if (ls == FAST)
			{
			rtn = true;
			lp.accel = 5;
			lp.max_speed = 60;
			lp.fconstant = .8;
			}
		else
			{
			rtn = true;
			lp.accel = 5;
			lp.max_speed = 20;
			lp.fconstant = .8;
			}
		}
	if (mp_set)
		{
		lp.accel = mp.accel;
		lp.max_speed = mp.max_speed;
		}
	return(true);
}



LinearMotion::LinearMotion()

{
	alrm_handler = -1;
	mp_set = false;
	pp_set = false;
	sonr = EzSSonar::Instance();
}



LinearMotion* LinearMotion::Instance()

{
	if (lm == NULL)
		{
		lm = new LinearMotion;
		}
	return(lm);
}


bool LinearMotion::MoveLinear(linear_directions direc, linear_speed ls)

{
	bool rtn = false;
	pthread_t exec;
	struct itimerval itm;
	struct timeval tv;

	if (SetLinearParameters(direc, ls))
		{
		if (lp.direction == FORWARD)
			sonr->SetSampling(FRONT);
		else
			sonr->SetSampling(REAR);
		usleep(50000);
		mc.SetAccel(lp.accel);
		mc.SetMode(0);
		mc.DisableRegulator();
		mc.DisableTimeout();
		stopping = false;
		mc.ResetEncoders();
		encoder = 0;
		encoder_run = true;
		samples = 0;
		last_a = 0;
		last_v = 0;
		dist = 0;
		last_time = 0;
		missed_samples = 0;
		sample_overflow = 0;
		motion_error[0] = 0;
		sum_gh = 0;
		gyroheading = 0;
		oldgyrov = 0;
		last_gh = 0;
		applied_correct = 0;
		first_calc = false;
		starting = true;
		stopping = false;
		sensor->ShortDetermineSensorBias();
		if (pthread_create(&exec, NULL, EncoderUpdateThread, 0) == 0)
			{
			if (lp.direction == FORWARD)
				StartForward();
			else
				StartBackward();
			sample = 0;
			if (pthread_create(&alrm_handler, NULL, AlarmHandler, this) == 0)
				{
				sem_init(&motion_complete, 0, 0);;
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
			strcpy(motion_error,"Could not start encoder update thread.");
			}
		}
	else
		strcpy(motion_error,"Could not set motion parameters.");
	return(rtn);
}



void LinearMotion::StopLinear()

{
	if (encoder_run && !stopping)
		{
		sem_trywait(&motion_complete);
		if (lp.max_speed > 80)
			mc.SetAccel(2);
		StopMotion();
		starting = false;
		stopping = true;
		sem_wait(&motion_complete);
		}
	else if (stopping)
		sem_wait(&motion_complete);
	else
		sem_trywait(&motion_complete);
}



bool LinearMotion::ChangeSpeed(int right, int left)

{
	return(ChngSpeed(right, left));
}



char * LinearMotion::MotionError()

{
	return(motion_error);
}



void LinearMotion::RecordSensorData()

{
	Log sd_log;
	time_t tt;
	tm* tmp;
	char buffer[200];
	char note[20];
	int i;

	time(&tt);
	sprintf(buffer, "%s%sLinearMotionSensorData%ld.csv", BASE_DIR, DATA_DIR, tt);
	if (sd_log.OpenLog(buffer, (char *)  "Linear motion sensor data log", false))
		{
		strcpy(last_move_file, buffer);
		tmp = localtime(&tt);
		sprintf(buffer,"%d/%d/%d  %d:%d:%d",tmp->tm_mon + 1,tmp->tm_mday,tmp->tm_year + 1900,tmp->tm_hour,tmp->tm_min,tmp->tm_sec);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Calculated motion (ft): %.2f",dist);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Accelerometer Y bias (g): %.3f", sensor->abias);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Gyro Z bias (°/sec): %.3f", sensor->gbias);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Max speed: %d",lp.max_speed);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Accel: %d",lp.accel);
		sd_log.LogEntry(buffer);
		if (lp.direction == FORWARD)
			sd_log.LogEntry((char *) "Direction: forward");
		else
			sd_log.LogEntry((char *) "Direction: backward");
		sprintf(buffer,"Motion error: %s",motion_error);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Voltage: %f", vs->GetVolts());
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Samples: %d",sample);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Missed sensor samples: %d",missed_samples);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Sensor sample overflow: %d", sample_overflow);
		sd_log.LogEntry(buffer);
		sd_log.LogEntry((char *) "");
		sd_log.LogEntry((char *) "Timestamp (msec),Note,Raw Accel Y (g),Raw Gyro Z (°/sec),Encoder count");
//		if (lp.direction == FORWARD)
//			sd_log.LogEntry("Timestamp (msec),Note,Raw Accel Y (g),Raw Accel X (g),Raw Accel Z (g),Raw Gyro Y (°/sec),Raw Gyro X (°/sec),Raw Gyro Z (°/sec),Encoder count");
//		else
//			sd_log.LogEntry("Timestamp (msec),Note,Raw Accel Y (g),Raw Accel X (g),Raw Accel Z (g),Raw Gyro (°/sec),Encoder count");
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
			else if (notes[i] == 5)
				strcpy(note, NOTE_5);
			sprintf(buffer, "%ld,%s,%.4f,%.4f,%d", st[i], note, raw_accel_y[i], raw_gyro_z[i], enc[i]);
//			sprintf(buffer, "%ld,%s,%.4f,%.4f,%.4f,%.4f,%.4f,%.4f,%d", st[i], note, raw_accel_y[i], raw_accel_x[i], raw_accel_z[i], raw_gyro_y[i], raw_gyro_x[i], raw_gyro_z[i], enc[i]);
			sd_log.LogEntry(buffer);
			}
		sd_log.CloseLog((char *) "");
	}
}



void LinearMotion::SetMotorParam(int accel,int max_spd)

{
	mp.accel = accel;
	mp.max_speed = max_spd;
	mp_set = true;
}


void LinearMotion::ClearMotorParam()

{
	mp_set = false;
}



void LinearMotion::SetPIDParam(double pgain,double igain,double dgain)

{
	pp.pgain = pgain;
	pp.igain = igain;
	pp.dgain = dgain;
	pp_set = true;
}



void LinearMotion::ClearPIDParam()

{
	pp_set = false;
}

