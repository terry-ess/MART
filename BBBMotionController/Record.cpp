#include <time.h>
#include <signal.h>
#include <sys/time.h>
#include "Record.h"
#include "MotionController.h"
#include "SharedData.h"


#define DATA_CAPTURE 2000


double Record::raw_accel_y[DATA_CAPTURE];
double Record::raw_accel_x[DATA_CAPTURE];
double Record::raw_accel_z[DATA_CAPTURE];
double Record::raw_gyro_y[DATA_CAPTURE];
double Record::raw_gyro_x[DATA_CAPTURE];
double Record::raw_gyro_z[DATA_CAPTURE];
int Record::st[DATA_CAPTURE];
int Record::sample;
unsigned long long Record::start_time, Record::last_time;
int Record::missed_samples;
int Record::sample_overflow;


void *Record::AlarmHandler(void *)

{
	struct sensor_data sd;
	struct timeval tv;
	unsigned long long msec;
	double dt;
	sigset_t wait_sigs;
	int sig;

	sigemptyset(&wait_sigs);
	sigaddset(&wait_sigs, SIGALRM);
	while(true)
		{
		sigwait(&wait_sigs,&sig);
		if (sig != SIGALRM)
			pthread_exit(NULL);
		if (sensor->GetSensorData(&sd))
			{
			gettimeofday(&tv, NULL);
			msec = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
			if (sample < DATA_CAPTURE)
				{
				st[sample] = msec - start_time;
				raw_accel_y[sample] = (((double)sd.accel_y / 32767) * 2);
				raw_accel_x[sample] = (((double)sd.accel_x / 32767) * 2);
				raw_accel_z[sample] = (((double)sd.accel_z / 32767) * 2);
				raw_gyro_y[sample] = (((double)sd.gyro_y / 32767) * 250);
				raw_gyro_x[sample] = (((double)sd.gyro_x / 32767) * 250);
				raw_gyro_z[sample] = (((double)sd.gyro_z / 32767) * 250);
				sample += 1;
				}
			else
				sample_overflow += 1;
			}
		else
			missed_samples += 1;
		}
}



void Record::RecordSensorData()

{
	Log sd_log;
	time_t tt;
	char buffer[200];
	int i;

	time(&tt);
	sprintf(buffer, "%s%s/SensorData%ld.csv", BASE_DIR, DATA_DIR, tt);
	if (sd_log.OpenLog(buffer, (char *)  "Sensor Data Log", false))
		{
		strcpy(last_rcd_file,buffer);
		sprintf(buffer, "Accelerator y bias (g): %.4f", sensor->abias);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Gyro z bias (°/sec): %.4f", sensor->gbias);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Missed sensor samples: %d", missed_samples);
		sd_log.LogEntry(buffer);
		sprintf(buffer, "Sensor sample overflow: %d", sample_overflow);
		sd_log.LogEntry(buffer);
		sd_log.LogEntry((char *) "");
		sd_log.LogEntry((char *) "Timestamp (msec),Raw Accel y (g),Raw Accel x (g),Raw Accel x (g),Raw Gyro y(°/sec),Raw Gyro x(°/sec),Raw Gyro z(°/sec)");
		for (i = 0; i < sample; i++)
			{
			sprintf(buffer, "%ld,%.4f,%.4f,%.4f,%.4f,%.4f,%.4f", st[i], raw_accel_y[i], raw_accel_x[i], raw_accel_z[i], raw_gyro_y[i], raw_gyro_x[i], raw_gyro_z[i]);
			sd_log.LogEntry(buffer);
			}
		sd_log.CloseLog((char *) "");
		}
}



Record::Record()

{
	alrm_handler = -1;
	recording = false;
}


bool Record::Start()

{
	struct itimerval itm;
	struct timeval tv;

	if (!recording)
		{
		sample = 0;
		sample_overflow = 0;
		missed_samples = 0;
		last_time = 0;
		sensor->ShortDetermineSensorBias();
		if (pthread_create(&alrm_handler, NULL, AlarmHandler, this) == 0)
			{
			itm.it_value.tv_sec = 0;
			itm.it_value.tv_usec = 10000;
			itm.it_interval = itm.it_value;
			gettimeofday(&tv, NULL);
			start_time = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
			setitimer(ITIMER_REAL, &itm, NULL);
			recording = true;
			}
		}
	return(recording);
}



void Record::Stop()

{
	struct itimerval itm;

	if (recording)
		{
		itm.it_value.tv_sec = 0;
		itm.it_value.tv_usec = 0;
		itm.it_interval = itm.it_value;
		setitimer(ITIMER_REAL, &itm, NULL);
		pthread_cancel(alrm_handler);
		recording = false;
		RecordSensorData();
		}
}
