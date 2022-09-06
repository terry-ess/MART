
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <pthread.h>
#include <signal.h>
#include <math.h>
#include <time.h>
#include <sys/time.h>
#include "SharedData.h"
#include "MCLinearMotion.h"
#include "MotionController.h"


#define PARAM_FILE "lm.param"
#define STOP_TIME_OUT 75
#define START_TIME_OUT 60
#define STOP_DETECT_COUNT 15
#define CORRECT_DELAY  200
#define CONNECTION_ERROR_TIME	1500
#define START_CONNECTION_ERROR_TIME 500

#define NOTE_0 ""
#define NOTE_1 "start detected"
#define NOTE_2 "stop detected"
#define NOTE_3 "error stop issued"
#define NOTE_4 "stop issued"
#define DATA_CAPTURE 3000


MCLinearMotion* MCLinearMotion::lm = NULL;
//EzSonar *MCLinearMotion::sonr = NULL;
EzSSonar *MCLinearMotion::sonr = NULL;
double MCLinearMotion::raw_accel[DATA_CAPTURE];
double MCLinearMotion::raw_gyro[DATA_CAPTURE];
int MCLinearMotion::st[DATA_CAPTURE];
int MCLinearMotion::enc[DATA_CAPTURE];
int MCLinearMotion::sonar[DATA_CAPTURE];
unsigned char MCLinearMotion::notes[DATA_CAPTURE];
double MCLinearMotion::last_a, MCLinearMotion::last_v,MCLinearMotion::dist,MCLinearMotion::gyroheading,MCLinearMotion::oldgyrov;
int MCLinearMotion::sample;
unsigned long long MCLinearMotion::start_time, MCLinearMotion::last_time,MCLinearMotion::sd_time;
bool MCLinearMotion::encoder_run = false;
int MCLinearMotion::encoder = 0;
mclinear_param MCLinearMotion::lp;
char MCLinearMotion::motion_error[100];
bool MCLinearMotion::stopping = false, MCLinearMotion::starting = false;
int MCLinearMotion::samples;
int MCLinearMotion::same_count = 0;
sem_t MCLinearMotion::motion_complete;
int MCLinearMotion::missed_samples;
int MCLinearMotion::sample_overflow;
double MCLinearMotion::stop_dist;
double MCLinearMotion::last_gh,MCLinearMotion::sum_gh;
int MCLinearMotion::applied_correct;
bool MCLinearMotion::first_calc;
double MCLinearMotion::sd_dist = 0;
int MCLinearMotion::max_dt;
int MCLinearMotion::min_dt;
bool MCLinearMotion::no_dist_calc;
int MCLinearMotion::front_clearance = MIN_FRONT_CLEARANCE;



void MCLinearMotion::StopAlarm()

{
	struct itimerval itm;

	itm.it_value.tv_sec = 0;
	itm.it_value.tv_usec = 0;
	itm.it_interval = itm.it_value;
	setitimer(ITIMER_REAL, &itm, NULL);
}



void MCLinearMotion::CalcPosition(double dt,double accel_y,double gyro_z)

{
	double  accel, aa, v;

	gyroheading += dt * ((oldgyrov + gyro_z) / 2);
	oldgyrov = gyro_z;
	if ((fabs(last_v) < lp.velocity_limit) || stopping)
		{
		accel = (lp.fconstant * last_a) + ((1.0 - lp.fconstant) * accel_y);
		aa = ((last_a + accel) / 2) * G_FT_SEC2;
		last_a = accel;
		v = last_v + (aa * dt);
		}
	else
		if (last_v > 0)
			v = lp.velocity_limit;
		else
			v = -lp.velocity_limit;
	dist += ((last_v + v) / 2) * dt;
	last_v = v;
}



void *MCLinearMotion::AlarmHandler(void *)

{
	struct sensor_data sd;
	double accel_y, gyro_z,dt;
	struct timeval tv;
	unsigned long long msec;
	unsigned char note = 0;
	sigset_t wait_sigs;
	int sig;
	int correction,delta_correct;

	sigemptyset(&wait_sigs);
	sigaddset(&wait_sigs, SIGALRM);
	while (true)
		{
		sigwait(&wait_sigs, &sig);
		if (sig == SIGALRM) 
			{
			ClearanceOk();
			gettimeofday(&tv, NULL);
			msec = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
			if (((last_time > 0) && (msec - last_time > CONNECTION_ERROR_TIME)) || ((last_time == 0) &&(msec - start_time > START_CONNECTION_ERROR_TIME)))
				{
				StopMotion();
				StopAlarm();
				strcpy(motion_error,"MPU6050 connection lost");
				sem_post(&motion_complete);
				encoder_run = false;
				pthread_exit(NULL);
				}
			if (sensor->GetSensorData(&sd))
				{
				note = 0;
				accel_y = (((double)sd.accel_y / 32767) * 2) - sensor->abias;
				gyro_z = (((double)sd.gyro_z / 32767) * 250) - sensor->gbias;
				if (last_time > 0)
					{
					dt = msec - last_time;
					if (dt > max_dt)
						max_dt = dt;
					else if (dt < min_dt)
						min_dt = dt;
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
						StopAlarm();
						starting = false;
						encoder_run = false;
						strcpy(motion_error,"start timedout");
						note = 3;
						sem_post(&motion_complete);
						pthread_exit(NULL);
						}
					else if (abs(encoder) >  0)
						{
						starting = false;
						note = 1;
						samples = 0;
						sd_time = msec;
						CalcPosition(dt,accel_y,gyro_z);
						}
					}
				else if (stopping)
					{
					if (samples == 0)
						if (strlen(motion_error) == 0)
							note = 4;
						else
							note = 3;
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
						pthread_exit(NULL);
						}
					else if ((int) enc[sample - 1] == encoder)
						{
						same_count += 1;
						if (same_count == STOP_DETECT_COUNT)
							{
							note = 2;
							StopAlarm();
							sem_post(&motion_complete);
							encoder_run = false;
							}
						}
					else
						{
						same_count = 0;
						sd_dist = dist;
						}
					}
				else
					{
					CalcPosition(dt, accel_y,gyro_z);
					if (!no_dist_calc && (fabs(dist) > stop_dist))
						StopMotion();
					if (((msec - sd_time) > lp.max_vel_time_limit) && (fabs(last_v) != lp.velocity_limit))
						{
						if (last_v > 0)
							last_v = lp.velocity_limit;
						else
							last_v = -lp.velocity_limit;
						}
					if ((msec - sd_time) > CORRECT_DELAY)
						{
						sum_gh += gyroheading;
						correction = (int) (lp.pgain * gyroheading) + (lp.igain * sum_gh);
						if (first_calc)
							first_calc = false;
						else
							correction += (int) (lp.dgain * (gyroheading - last_gh));
						last_gh = gyroheading;
						delta_correct = correction - applied_correct;
						applied_correct += delta_correct;
						if (abs(applied_correct) >= 30)
							{
							StopMotion();
							StopAlarm();
							strcpy(motion_error, EXCESSIVE_GYRO_CORRECT);
							note = 3;
							sem_post(&motion_complete);
							encoder_run = false;
							pthread_exit(NULL);
							}
						else if (delta_correct != 0)
							{
							ChngSpeed(-delta_correct,delta_correct);
							}
						}
					}
				if (sample < DATA_CAPTURE)
					{
					st[sample] = msec - start_time;
					raw_accel[sample] = accel_y;
					raw_gyro[sample] = gyro_z;
					enc[sample] = encoder;
					if (lp.direction == FORWARD)
						sonar[sample] = sonr->front_clearance;
					else
						sonar[sample] = sonr->rear_clearance;
					notes[sample] = note;
					sample += 1;
					}
				else
					sample_overflow += 1;
				if (note == 2)
					pthread_exit(NULL);
				}
			else
				missed_samples += 1;
			}
		}
}



bool MCLinearMotion::ClearanceOk()

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
				strcpy(motion_error, INSUFFICENT_REAR_CLEARANCE);
				rtn = false;
				}
			}
		else
			{
			dist = sonr->front_clearance;
			if ((dist != -1) && (dist < MIN_FRONT_CLEARANCE))
				{
				StopMotion();
				strcpy(motion_error, INSUFFICENT_FRONT_CLEARANCE);
				rtn = false;
				}
			}
		}
	return(rtn);
}



void MCLinearMotion::StopMotion()

{
	mc.StopMotion();
	starting = false;
	stopping = true;
	lp.rnow = lp.lnow = 128;
	samples = 0;
	sonr->SetSampling(NONE);
	front_clearance = MIN_FRONT_CLEARANCE;
}



void* MCLinearMotion::EncoderUpdateThread(void *)

{
	while (encoder_run)
		{
		mc.ReadEncoder(&encoder);
		usleep(10000);
		}
}



void MCLinearMotion::StartBackward()

{
	mc.StartBackward(lp.max_speed);
	lp.rnow = lp.lnow = 128 + lp.max_speed;
}



void MCLinearMotion::StartForward()

{
	mc.StartForward(lp.max_speed);
	lp.rnow = lp.lnow = 128 - lp.max_speed;
}



void MCLinearMotion::ChngSpeed(int right, int left)

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



bool MCLinearMotion::ReadParameters(char *fname)

{
	return(ReadParameters(fname,&lp));
}



bool MCLinearMotion::ReadParameters(char *fname,mclinear_param *lp)

{
	FILE *pfile = NULL;
	char line[128];
	bool rtn = false;

	if ((pfile = fopen(fname, "r")) != NULL)
		{
		fgets(line, sizeof(line) - 1, pfile);
		lp->max_speed = atoi(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->accel = atoi(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->velocity_limit = atof(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->max_vel_time_limit = atoi(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->steady_stop_dist = atof(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->sonar_stop_dist = atoi(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->fconstant = atof(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->pgain = atof(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->igain = atof(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->dgain = atof(line);
		fclose(pfile);
		rtn = true;
		}
	return(rtn);
}



bool MCLinearMotion::SetLinearParameters(linear_directions direc, linear_speed ls)

{
	bool rtn = false;
	char fname[256];
	char bname[10];

	lp.direction = direc;
	lp.ls = ls;
	if (ls == NORMAL)
		strcpy(bname, "NORMAL");
	else 
		strcpy(bname, "FAST");
	sprintf(fname, "%s%s%s%s", BASE_DIR,CAL_DIR, bname, PARAM_FILE);
	if ((rtn = ReadParameters(fname)) == false)
		{
		if (ls == NORMAL)
			{
			rtn = true;
			lp.accel = 5;
			lp.max_speed = 30;
			lp.velocity_limit = .6;
			lp.max_vel_time_limit = 500;
			lp.steady_stop_dist = 1.1;
			lp.sonar_stop_dist = 3;
			lp.fconstant = .8;
			lp.pgain = 4;
			lp.igain = .05;
			lp.dgain = 10;
			}
		else if (ls == FAST)
			{
			rtn = true;
			lp.accel = 5;
			lp.max_speed = 60;
			lp.fconstant = .8;
			lp.velocity_limit = 1.5;
			lp.steady_stop_dist = 5;
			lp.fconstant = .8;
			lp.pgain = 2;
			lp.igain = 0;
			lp.dgain = 0;
			}
		}
	return(true);
}



MCLinearMotion::MCLinearMotion()

{
	alrm_handler = -1;
	sem_init(&motion_complete, 0, 0);
//	sonr = EzSonar::Instance();
	sonr = EzSSonar::Instance();
}



MCLinearMotion* MCLinearMotion::Instance()

{
	if (lm == NULL)
		{
		lm = new MCLinearMotion;
		}
	return(lm);
}


bool MCLinearMotion::MoveLinear(linear_directions direc,int ldist, linear_speed ls)

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
		lp.target_dist = ldist;
		mc.SetAccel(lp.accel);
		mc.SetMode(0);
		mc.DisableRegulator();
		mc.DisableTimeout();
		stopping = false;
		mc.ResetEncoders();
		encoder = 0;
		encoder_run = true;
		sample = 0;
		samples = 0;
		last_a = 0;
		last_v = 0;
		dist = 0;
		last_time = 0;
		missed_samples = 0;
		sample_overflow = 0;
		max_dt = 0;
		min_dt = 20;
		motion_error[0] = 0;
		sum_gh = 0;
		gyroheading = 0;
		oldgyrov = 0;
		last_gh = 0;
		applied_correct = 0;
		first_calc = false;
		starting = true;
		stopping = false;
		if (ldist > 0)
			{
			stop_dist = (ldist - lp.steady_stop_dist)/12.0;
			no_dist_calc = false;
			}
		else
			no_dist_calc = true;
		if (pthread_create(&exec, NULL, EncoderUpdateThread, 0) == 0)
			{
			if (lp.direction == FORWARD)
				StartForward();
			else
				StartBackward();
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
				strcpy(motion_error, "Could not start alarm handling thread.");
			}
		}
	else
		strcpy(motion_error,"Could not set motion parameters.");
	return(rtn);
}



void MCLinearMotion::StopLinear()

{
	if (!stopping)
		{
		StopMotion();
		starting = false;
		stopping = true;
		}
}



void MCLinearMotion::SwitchToDR(int dist)

{
	if (no_dist_calc)
		{
		lp.target_dist = dist;
		stop_dist = ((dist - lp.steady_stop_dist) / 12);
		no_dist_calc = false;
		}
}



void MCLinearMotion::SetFrontClearance(int dist)

{
	if (dist < MIN_FRONT_CLEARANCE)
		front_clearance = MIN_FRONT_CLEARANCE;
	else
		front_clearance = dist;
}



int MCLinearMotion::MaxNormalVelocity()

{
	char fname[256];
	char bname[10];
	mclinear_param lp;
	int rtn = -1;

	sprintf(fname, "%s%s%s%s", BASE_DIR, CAL_DIR,"NORMAL", PARAM_FILE);
	if (ReadParameters(fname,&lp))
		rtn = round(lp.velocity_limit * 12);
	return(rtn);
}



char * MCLinearMotion::MotionError()

{
	return(motion_error);
}



int MCLinearMotion::LastMoveDist()

{
	return((int) (sd_dist * 12));
}



void MCLinearMotion::RecordSensorData()

{
	Log sd_log;
	time_t tt;
	tm* tmp;
	char buffer[200];
	char note[20];
	int i;

	time(&tt);
	sprintf(buffer, "%s%sMCLinearMotionSensorData%ld.csv", BASE_DIR, DATA_DIR, tt);
	if (sd_log.OpenLog(buffer, (char *)  "MC Linear motion sensor data log", false))
		{
		strcpy(last_move_file,buffer);
		tmp = localtime(&tt);
		sprintf(buffer,"%d/%d/%d  %d:%d:%d",tmp->tm_mon + 1,tmp->tm_mday,tmp->tm_year + 1900,tmp->tm_hour,tmp->tm_min,tmp->tm_sec);
		sd_log.LogEntry(buffer);
		if (no_dist_calc)
			sd_log.LogEntry((char *) "No distance calc: true");
		else
			{
			sd_log.LogEntry((char *) "No distance calc: false");
			sprintf(buffer, "Target distance (ft): %.2f",(double) lp.target_dist/12);
			sd_log.LogEntry(buffer);
			sprintf(buffer, "Motion stop distance (ft): %.3f", stop_dist);
			sd_log.LogEntry(buffer);
			}
		sd_log.LogEntry((char *) "Calculated distance (ft):");
		sprintf(buffer,"  @ stop detected - %.2f",dist);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "  @ encoder stop - %.2f", sd_dist);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Accelerometer bias (g): %.3f", sensor->abias);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Gyro bias (°/sec): %.3f", sensor->gbias);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Max speed: %d",lp.max_speed);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Accel: %d",lp.accel);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Stop distance (in): %.2f", lp.steady_stop_dist);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Low pass filter factor: %.2f", lp.fconstant);
		sd_log.LogEntry(buffer);
		if (lp.direction == FORWARD)
			sd_log.LogEntry((char *) "Direction: forward");
		else
			sd_log.LogEntry((char *) "Direction: backward");
		sd_log.LogEntry((char *) "Turn correct PID parameters:");
		sprintf(buffer, "  Pgain - %.3f", lp.pgain);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "  Igain - %.3f",lp.igain);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "  Dgain - %.3f", lp.dgain);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"DT (msec): max %d   min %d",max_dt,min_dt);
		sd_log.LogEntry(buffer);
		app_log.LogEntry(buffer);
		sprintf(buffer, "Motion error: %s", motion_error);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Voltage: %f", vs->GetVolts());
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Missed sensor samples: %d",missed_samples);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Sensor sample overflow: %d", sample_overflow);
		sd_log.LogEntry(buffer);
		sd_log.LogEntry((char *) "");
		if (lp.direction == FORWARD)
			sd_log.LogEntry((char *) "Timestamp (msec),Note,Raw Accel (g),Raw Gyro (°/sec),Encoder count,Sonar front clearance (in)");
		else
			sd_log.LogEntry((char *) "Timestamp (msec),Note,Raw Accel (g),Raw Gyro (°/sec),Encoder count,Sonar rear clearance (in)");
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
			sprintf(buffer, "%ld,%s,%.3f,%.3f,%d,%d", st[i], note, raw_accel[i], raw_gyro[i], enc[i],sonar[i]);
			sd_log.LogEntry(buffer);
			}
		sd_log.CloseLog((char *) "");
	}
}

