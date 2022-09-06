
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <pthread.h>
#include <signal.h>
#include <math.h>
#include <time.h>
#include <sys/time.h>
#include "SharedData.h"
#include "TLinearMotion.h"
#include "MotionController.h"
#include "Gpio.h"


#define PARAM_FILE "tlm.param"
#define START_TIME_OUT 60
#define STOP_DETECT_COUNT 15
#define CORRECT_DELAY  250
#define CONNECTION_ERROR_TIME	1500
#define START_CONNECTION_ERROR_TIME 500
#define STOP_TIME_OUT 75

#define NOTE_0 ""
#define NOTE_1 "start detected"
#define NOTE_2 "stop detected"
#define NOTE_3 "error stop issued"
#define NOTE_4 "stop issued"
#define NOTE_5 "stop issued (dock detect)"
#define NOTE_6 "insuf dist stop issued"
#define DATA_CAPTURE 2000


TLinearMotion* TLinearMotion::lm = NULL;
//EzSonar *TLinearMotion::sonr = NULL;
EzSSonar *TLinearMotion::sonr = NULL;
double TLinearMotion::raw_accel[DATA_CAPTURE];
double TLinearMotion::raw_gyro[DATA_CAPTURE];
int TLinearMotion::st[DATA_CAPTURE];
int TLinearMotion::enc[DATA_CAPTURE];
int TLinearMotion::sonar[DATA_CAPTURE];
unsigned char TLinearMotion::notes[DATA_CAPTURE];
int TLinearMotion::stop_enc[STOP_TIME_OUT + 1];
double TLinearMotion::gyroheading, TLinearMotion::oldgyrov;
int TLinearMotion::sample;
unsigned long long TLinearMotion::start_time, TLinearMotion::last_time, TLinearMotion::sd_time;
bool TLinearMotion::encoder_run = false;
int TLinearMotion::encoder = 0;
tlinear_param TLinearMotion::lp;
char TLinearMotion::motion_error[100];
bool TLinearMotion::stopping = false, TLinearMotion::starting = false;
int TLinearMotion::samples;
int TLinearMotion::same_count = 0;
sem_t TLinearMotion::motion_complete;
int TLinearMotion::missed_samples;
int TLinearMotion::sample_overflow;
int TLinearMotion::stop_time;
double TLinearMotion::last_gh, TLinearMotion::sum_gh;
int TLinearMotion::applied_correct;
bool TLinearMotion::first_calc;
int TLinearMotion::max_dt;
int TLinearMotion::min_dt;
bool TLinearMotion::no_dist_calc;
int TLinearMotion::front_clearance = MIN_FRONT_CLEARANCE;
double TLinearMotion::rdist;
bool TLinearMotion::dock_detected;
pthread_mutex_t TLinearMotion::counter_access;



void TLinearMotion::StopAlarm()

{
	struct itimerval itm;

	itm.it_value.tv_sec = 0;
	itm.it_value.tv_usec = 0;
	itm.it_interval = itm.it_value;
	setitimer(ITIMER_REAL, &itm, NULL);
}



void TLinearMotion::CalcPosition(double dt,double gyro_z)

{
	gyroheading += dt * ((oldgyrov + gyro_z) / 2);
	oldgyrov = gyro_z;
}



void *TLinearMotion::AlarmHandler(void *)

{
	struct sensor_data sd;
	double gyro_z,accel_y,dt;
	struct timeval tv;
	unsigned long long msec;
	unsigned long long run_time;
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
		if (sig == SIGALRM) 
			{
			ClearanceOk();
			gettimeofday(&tv, NULL);
			msec = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
			if (((last_time > 0) && (msec - last_time > CONNECTION_ERROR_TIME)) || ((last_time == 0) && (msec - start_time > START_CONNECTION_ERROR_TIME)))
				{
				StopMotion();
				StopAlarm();
				strcpy(motion_error,MPU_FAIL);
				front_clearance = MIN_FRONT_CLEARANCE;
				sem_post(&motion_complete);
				encoder_run = false;
				run_time = msec - sd_time;
				rdist = (((((double)lp.accel_time + lp.decel_time) / 1000) * (lp.top_speed / 2)) + ((((double)run_time - lp.accel_time) / 1000) * lp.top_speed) * 12);
				pthread_exit(NULL);
				}
			if (lp.docking && !stopping && !starting)
				{
				if (io->Docked())
					{
					StopMotion();
					dock_detected = true;
					}
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
				pthread_mutex_lock(&counter_access);
				if (starting)
					{
					samples += 1;
					if (samples == START_TIME_OUT)
						{
						StopMotion();
						StopAlarm();
						starting = false;
						encoder_run = false;
						strcpy(motion_error,START_TIMEOUT);
						note = 3;
						front_clearance = MIN_FRONT_CLEARANCE;
						sem_post(&motion_complete);
						pthread_mutex_unlock(&counter_access);
						pthread_exit(NULL);
						}
					else if (abs(encoder) >  0)
						{
						starting = false;
						note = 1;
						samples = 0;
						sd_time = msec;
						CalcPosition(dt,gyro_z);
						}
					}
				else if (stopping)
					{
					if (samples == 0)
						if (strlen(motion_error) == 0)
							{
							if (dock_detected)
								note = 5;
							else
								note = 4;
							}
						else
							note = 6;
					CalcPosition(dt,gyro_z);
					samples += 1;
					if (samples == 1)
						{
						same_count = 0;
						}
					else if (samples == STOP_TIME_OUT)
						{
						StopMotion();
						strcpy(motion_error,STOP_TIMEOUT);
						note = 3;
						}
					else if ((int) stop_enc[samples - 2] == encoder)
						{
						same_count += 1;
						if (same_count == STOP_DETECT_COUNT)
							{
							note = 2;
							rdist = (((((double)lp.accel_time + lp.decel_time) / 1000) * (lp.top_speed / 2)) + ((((double) run_time - lp.accel_time - lp.decel_time)/1000) * lp.top_speed) * 12);
							}
						}
					else
						{
						same_count = 0;
						run_time = msec - sd_time;
						}
					 stop_enc[samples - 1] = encoder;
					}
				else
					{
					CalcPosition(dt,gyro_z);
					if (!no_dist_calc && (msec - sd_time >= stop_time))
						StopMotion();
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
							strcpy(motion_error, EXCESSIVE_GYRO_CORRECT);
							note = 3;
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
				if ((note == 2) || (note == 3))
					{
					StopAlarm();
					front_clearance = MIN_FRONT_CLEARANCE;
					sem_post(&motion_complete);
					encoder_run = false;
					pthread_mutex_unlock(&counter_access);
					pthread_exit(NULL);
					}
				pthread_mutex_unlock(&counter_access);
				}
			else
				missed_samples += 1;
			}
		}
}



bool TLinearMotion::ClearanceOk()

{
	bool rtn = true;
	int dist;

	if ((!starting) && (!stopping) && (!lp.ncc))
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
			if ((dist != -1) && (dist < front_clearance))
				{
				StopMotion();
				strcpy(motion_error, INSUFFICENT_FRONT_CLEARANCE);
				rtn = false;
				}
			}
		}
	return(rtn);
}



void TLinearMotion::StopMotion()

{
	mc.StopMotion();
	starting = false;
	stopping = true;
	lp.rnow = lp.lnow = 128;
	samples = 0;
	sonr->SetSampling(NONE);
	front_clearance = MIN_FRONT_CLEARANCE;
}



void* TLinearMotion::EncoderUpdateThread(void *)

{
	pthread_detach(pthread_self());
	while (encoder_run)
		{
		mc.ReadEncoder(&encoder);
		usleep(10000);
		}
}



void TLinearMotion::StartBackward()

{
	mc.StartBackward(lp.max_speed);
	lp.rnow = lp.lnow = 128 + lp.max_speed;
}



void TLinearMotion::StartForward()

{
	mc.StartForward(lp.max_speed);
	lp.rnow = lp.lnow = 128 - lp.max_speed;
}



void TLinearMotion::ChngSpeed(int right, int left)

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



bool TLinearMotion::ReadParameters(char *fname)

{
	return(ReadParameters(fname,&lp));
}



bool TLinearMotion::ReadParameters(char *fname,tlinear_param *lp)

{
	FILE *pfile = NULL;
	char line[128];
	bool rtn = false;
	char *pos;

	if ((pfile = fopen(fname, "r")) != NULL)
		{
		fgets(line, sizeof(line) - 1, pfile);
		lp->max_speed = atoi(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->accel = atoi(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->base_top_speed = atof(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->accel_time = atoi(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->decel_time = atoi(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->pgain = atof(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->igain = atof(line);
		fgets(line, sizeof(line) - 1, pfile);
		lp->dgain = atof(line);
		if (fgets(line,sizeof(line) - 1,pfile) != NULL)
			{
			pos = strchr(line, ',');
			if (pos == 0)
				{
				lp->vc_slope = 0;
				lp->vc_intercept = lp->base_top_speed;
				}
			else
				{
				lp->vc_slope = atof(line);
				pos += 1;
				lp->vc_intercept = atof(pos);
				}
			}
		else
			{
			lp->vc_slope = 0;
			lp->vc_intercept = lp->base_top_speed;
			}
		fclose(pfile);
		rtn = true;
		}
	return(rtn);
}



bool TLinearMotion::SetLinearParameters(linear_directions direc, linear_speed ls)

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
	else if (ls == SLOW)
		strcpy(bname,"SLOW");
	sprintf(fname, "%s%s%s%s", BASE_DIR,CAL_DIR, bname, PARAM_FILE);
	if (ReadParameters(fname))
		{
		lp.volts = vs->GetVolts();
		lp.top_speed = (lp.volts * lp.vc_slope) + lp.vc_intercept;
		rtn = true;
		}
	else
		{
		if (ls == NORMAL)
			{
			rtn = true;
			lp.accel = 5;
			lp.max_speed = 30;
			lp.base_top_speed = .64;
			lp.top_speed = .64;
			lp.accel_time = 300;
			lp.decel_time = 120;
			lp.pgain = 4;
			lp.igain = .05;
			lp.dgain = 10;
			lp.vc_slope = 0;
			lp.vc_intercept = 0;
			}
		else if (ls == SLOW)
			{
			rtn = true;
			lp.accel = 5;
			lp.max_speed = 20;
			lp.base_top_speed = .34;
			lp.top_speed = .34;
			lp.accel_time = 200;
			lp.decel_time = 80;
			lp.pgain = 4;
			lp.igain = .05;
			lp.dgain = 10;
			lp.vc_slope = 0;
			lp.vc_intercept = 0;
			}
		else
			rtn = false;
		}
	if (pp_set)
		{
		lp.pgain = pp.pgain;
		lp.igain = pp.igain;
		lp.dgain = pp.dgain;
		}
	return(rtn);
}



TLinearMotion::TLinearMotion()

{
	alrm_handler = -1;
	pp_set = false;
	sem_init(&motion_complete, 0, 0);
	sonr = EzSSonar::Instance();
	counter_access = PTHREAD_MUTEX_INITIALIZER;
	front_clearance = MIN_FRONT_CLEARANCE;
}



TLinearMotion* TLinearMotion::Instance()

{
	if (lm == NULL)
		{
		lm = new TLinearMotion;
		}
	return(lm);
}


bool TLinearMotion::MoveLinear(linear_directions direc,int ldist, linear_speed ls,bool docking,bool ncc)

{
	bool rtn = false;
	pthread_t exec;
	struct itimerval itm;
	struct timeval tv;
	double rt;

	if (SetLinearParameters(direc, ls))
		{
		if (lp.direction == FORWARD)
			sonr->SetSampling(FRONT);
		else
			sonr->SetSampling(REAR);
		lp.docking = docking;
		lp.ncc = ncc;
		dock_detected = false;
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
		rdist = 0;
		if (ldist > 0)
			{
			rt = ((((double) ldist/12) - ((((double) lp.accel_time + lp.decel_time)/ 1000) * (lp.top_speed / 2)))/lp.top_speed) * 1000;
			stop_time = round(rt + lp.accel_time);
			no_dist_calc = false;
			}
		else
			no_dist_calc = true;
		sensor->ShortDetermineSensorBias();
		if (pthread_create(&exec, NULL, EncoderUpdateThread, 0) == 0)
			{
			if (lp.direction == FORWARD)
				StartForward();
			else
				StartBackward();
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
				{
				StopMotion();
				encoder_run = false;
				strcpy(motion_error, "Could not start alarm handling thread.");
				}
			}
		else
			strcpy(motion_error, "Could not start encoder reading thread.");
		}
	else
		strcpy(motion_error,"Could not set motion parameters.");
	if (front_clearance != MIN_FRONT_CLEARANCE)
		front_clearance = MIN_FRONT_CLEARANCE;
	return(rtn);
}



void TLinearMotion::StopLinear()

{
	if (!stopping)
		{
		pthread_mutex_lock(&counter_access);
		StopMotion();
		pthread_mutex_unlock(&counter_access);
		}
}



void TLinearMotion::SwitchToDR(int dist)

{
	double rt;

	if (no_dist_calc)
		{
		lp.target_dist = dist;
		rt = ((((double) dist / 12) - ((((double)lp.accel_time + lp.decel_time) / 1000) * (lp.top_speed / 2))) / lp.top_speed) * 1000;
		stop_time = round(rt + lp.accel_time);
		no_dist_calc = false;
		}
}



void TLinearMotion::SetFrontClearance(int dist)

{
	front_clearance = dist;
}



int TLinearMotion::MaxNormalVelocity()

{
	char fname[256];
	char bname[10];
	tlinear_param lp;
	int rtn = -1;

	sprintf(fname, "%s%s%s%s", BASE_DIR, CAL_DIR,"NORMAL", PARAM_FILE);
	if (ReadParameters(fname,&lp))
		rtn = round(lp.base_top_speed * 12);
	return(rtn);
}



char * TLinearMotion::MotionError()

{
	return(motion_error);
}



int TLinearMotion::LastMoveDist()

{
	return(round(rdist));
}



void TLinearMotion::RecordSensorData()

{
	Log sd_log;
	time_t tt;
	tm* tmp;
	char buffer[200];
	char note[30];
	int i;

	time(&tt);
	sprintf(buffer, "%s%sTLinearMotionSensorData%ld.csv", BASE_DIR, DATA_DIR, tt);
	if (sd_log.OpenLog(buffer, (char *)  "Timed Linear motion sensor data log", false))
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
			sprintf(buffer, "Motion stop time (msec): %d", stop_time);
			sd_log.LogEntry(buffer);
			}
		sprintf(buffer, "Accelerometer Y bias (g): %.3f", sensor->abias);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Gyro bias (°/sec): %.3f", sensor->gbias);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Max speed: %d",lp.max_speed);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Accel: %d",lp.accel);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Base top speed (ft/sec): %f", lp.base_top_speed);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Top speed (ft/sec): %f", lp.top_speed);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Accel time (msec): %d", lp.accel_time);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Decel time (msec): %d", lp.decel_time);
		sd_log.LogEntry(buffer);
		sd_log.LogEntry((char *) "Voltage correction parameters:");
		sprintf(buffer,"  slope: %f",lp.vc_slope);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"  intercept: %f", lp.vc_intercept);
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
		sprintf(buffer, "Voltage: %f",lp.volts);
		sd_log.LogEntry(buffer);
		sprintf(buffer,"Missed sensor samples: %d",missed_samples);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Sensor sample overflow: %d", sample_overflow);
		sd_log.LogEntry(buffer);
		if (lp.docking)
			{
			if (dock_detected)
				sd_log.LogEntry((char *) "Docking move: docking detected");
			else
				sd_log.LogEntry((char *) "Docking move: docking not detected");
			}
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
			else if (notes[i] == 5)
				strcpy(note, NOTE_5);
			else if (notes[i] == 6)
				strcpy(note,NOTE_6);
			sprintf(buffer, "%ld,%s,%.4f,%.4f,%d,%d", st[i], note, raw_accel[i],raw_gyro[i], enc[i], sonar[i]);
			sd_log.LogEntry(buffer);
			}
		sd_log.CloseLog((char *) "");
	}
}


void TLinearMotion:: SetPidParam(double p,double i,double d)

{
	pp.pgain = p;
	pp.igain = i;
	pp.dgain = d;
	pp_set = true;
}


void TLinearMotion::ClearPidParam()

{
	pp_set = false;
}



void TLinearMotion::ClearLastMoveDist()

{
	rdist = 0;
}

