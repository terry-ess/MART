#include <stdlib.h>
#include <stddef.h>
#include <unistd.h>
#include <fcntl.h>
#include <pthread.h>
#include <stdio.h>
#include <math.h>
#include <errno.h>
#include <sys/time.h>
#include "EZSSonar.h"
#include "SharedData.h"
#include "MotionController.h"


//#define DEBUG_REAR

#define FRONT_CTL_LINE_DIRECTION "/sys/class/gpio/gpio115/direction"
#define FRONT_CTL_LINE_VALUE "/sys/class/gpio/gpio115/value"
#define FRONT_CTL_LINE "115"	//P9_27
#define REAR_CTL_LINE_DIRECTION "/sys/class/gpio/gpio48/direction"
#define REAR_CTL_LINE_VALUE "/sys/class/gpio/gpio48/value"
#define REAR_CTL_LINE "48"	//P9_15
#define TIME_OUT_COUNT 40
#define FRONTTTYDEVICE "/dev/ttyO1"	//RX - P9_26  TX - P9_24
#define REARTTYDEVICE "/dev/ttyO4"	//RX - P9_11  TX - P9_13


EzSSonar* EzSSonar::sensor = NULL;
unsigned long long EzSSonar::start_time;


EzSSonar::EzSSonar()

{
	frontctl_line_fd = -1;
	rearctl_line_fd = -1;
	forward_fd = -1;
	backward_fd = -1;
	monitor = -1;
	run = false;
	sample_front = false;
	sample_rear = false;
	rear_min_count = 0;
	front_min_count = 0;
	int front_clearance = -1;
	int rear_clearance = -1;
	log = false;
	log_start_time = 0;
	log_time = 0;
}



void *EzSSonar::MonitorThread(void* param)

{
	EzSSonar* me;
	struct timeval tv;

	me = (EzSSonar *) param;
	gettimeofday(&tv, NULL);
	start_time = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
	while (me->run)
		{
		me->SensorData(&me->front_clearance,&me->rear_clearance);
		}
}



bool EzSSonar::ReadBytes(int fd,unsigned char *buff, int bno)

{
	int pos = 0, trys = 0;
	bool rtn = false;
	int no;

	do
		{
		no = read(fd, &buff[pos], 1);
		if (no != 1)
			{
			trys += 1;
			usleep(5000);
			}
		else
			{
			pos += 1;
			trys = 0;
			}
		}
	while ((pos < bno) && (trys < TIME_OUT_COUNT));
	if (pos == bno)
		rtn = true;
	else
		rtn = false;
}



bool EzSSonar::SensorData(int *front,int *rear)

{
	unsigned char buff[10];
	double val;
	struct timeval tv;
	unsigned long long msec;
	int sval;

	if (((forward_fd >= 0) && sample_front) || ((backward_fd >= 0) && sample_rear))
		{
		if (sample_front || (log && log_front))
			*front = ReadFrontSonar();
		if (sample_rear || (log && !log_front))
			*rear = ReadRearSonar();
		}
	else if (log)
		{
		if ((forward_fd >= 0) && log_front)
			*front = ReadFrontSonar();
		else if ((backward_fd >= 0) && !log_front)
			*rear = ReadRearSonar();
		}
	else
		{
		*front = -1;
		*rear = -1;
		usleep(50000);
		}
	if (log)
		{
		char buffer[200];

		gettimeofday(&tv, NULL);
		msec = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
		if (msec - log_start_time < log_time)
			{
			if (log_front)
				sprintf(buffer, "%d,%d",msec - log_start_time,*front);
			else
				sprintf(buffer, "%d,%d", msec - log_start_time, *rear);
			sd_log.LogEntry(buffer);
			}
		else
			{
			log = false;
			sd_log.CloseLog((char *) "");
			log_time = 0;
			log_start_time = 0;
			}
		}
}



bool EzSSonar::Expose(char * io)

{
	bool rtn = false;
	int fd,no;
	char stg[50];

	fd = open(GPIO_EXPORT, O_WRONLY);
	if (fd >= 0)
		{
		no = write(fd,io,strlen(io));
		close(fd);
		rtn = true;
		}
	return(rtn);
}



bool EzSSonar::InitFrontCtlLine()

{
	int fd, no;
	bool rtn = false;

	fd = open(FRONT_CTL_LINE_DIRECTION, O_WRONLY);
	if (fd < 0)
		{
		if (Expose((char *) FRONT_CTL_LINE))
			fd = open(FRONT_CTL_LINE_DIRECTION, O_WRONLY);
		}
	if (fd >= 0)
		{
		no = write(fd, "low", 3);
		close(fd);
		if (no == 3)
			{
			frontctl_line_fd = open(FRONT_CTL_LINE_VALUE, O_WRONLY);
			if (frontctl_line_fd >= 0)
				rtn = true;
			}
		}
	return(rtn);
}



void EzSSonar::CloseFrontCtlLine()

{
	int fd, no;

	if (frontctl_line_fd >= 0)
		close(frontctl_line_fd);
}



bool EzSSonar::FrontCtlLineHigh()

{
	int no;
	bool rtn = false;

	if (frontctl_line_fd >= 0)
		{
		no = write(frontctl_line_fd, "1", 1);
		if (no > 0)
			rtn = true;
		}
	return(rtn);
}



bool EzSSonar::FrontCtlLineLow()

{
	int no;
	bool rtn = false;

	if (frontctl_line_fd >= 0)
		{
		no = write(frontctl_line_fd, "0", 2);
		if (no > 0)
			rtn = true;
		}
	return(rtn);
}



bool EzSSonar::InitRearCtlLine()

{
	int fd, no;
	bool rtn = false;

	fd = open(REAR_CTL_LINE_DIRECTION, O_WRONLY);
	if (fd < 0)
		{
		if (Expose((char *) REAR_CTL_LINE))
			fd = open(REAR_CTL_LINE_DIRECTION, O_WRONLY);
		}
	if (fd >= 0)
		{
		no = write(fd, "low", 3);
		close(fd);
		if (no == 3)
			{
			rearctl_line_fd = open(REAR_CTL_LINE_VALUE, O_WRONLY);
			if (rearctl_line_fd >= 0)
				rtn = true;
			}
		}
	return(rtn);
}



void EzSSonar::CloseRearCtlLine()

{
	int fd, no;

	if (rearctl_line_fd >= 0)
		close(rearctl_line_fd);
}



bool EzSSonar::RearCtlLineHigh()

{
	int no;
	bool rtn = false;

	if (rearctl_line_fd >= 0)
		{
		no = write(rearctl_line_fd, "1", 1);
		if (no > 0)
			rtn = true;
		}
	return(rtn);
}



bool EzSSonar::RearCtlLineLow()

{
	int no;
	bool rtn = false;

	if (rearctl_line_fd >= 0)
		{
		no = write(rearctl_line_fd, "0", 1);
		if (no > 0)
			rtn = true;
		}
	return(rtn);
}



bool EzSSonar::Open(char  *tty_device,int *fd)

{
	struct termios newtio;
	int no;
	char buff[1];
	bool rtn = false;

	*fd = open(tty_device, O_RDONLY | O_NOCTTY | O_NONBLOCK);
	if (*fd >= 0)
		{
		bzero(&newtio, sizeof(newtio));
		newtio.c_cflag = B9600 | CS8 | CLOCAL | CREAD;
		newtio.c_iflag = IGNPAR;
		newtio.c_oflag = 0;
		newtio.c_lflag = 0;
		newtio.c_cc[VTIME] = 0;
		newtio.c_cc[VMIN] = 1;
		tcflush(*fd, TCIFLUSH);
		tcsetattr(*fd, TCSANOW, &newtio);
		rtn = true;
		}
	return(rtn);
}



bool EzSSonar::InitSerial()

{
	bool rtn = false;

	Open((char *) FRONTTTYDEVICE, &forward_fd);
	Open((char *) REARTTYDEVICE, &backward_fd);
	if ((forward_fd > 0)  || (backward_fd > 0))
		rtn = true;
	return(rtn);
}



void EzSSonar::CloseSerial()

{
	if (forward_fd >= 0)
		{
		close(forward_fd);
		forward_fd = -1;
		}
	if (backward_fd >= 0)
		{
		close(backward_fd);
		backward_fd = -1;
		}
}



void EzSSonar::InitSonar()

{
	usleep(250000);
	FrontCtlLineHigh();
	usleep(25);
	FrontCtlLineLow();
	usleep(150000);
	RearCtlLineHigh();
	usleep(25);
	RearCtlLineLow();
	usleep(150000);
}



EzSSonar* EzSSonar::Instance()

{
	if (sensor == NULL)
		sensor = new EzSSonar;
	return(sensor);
}



bool EzSSonar::Init()

{
	bool rtn = false;

	if (InitFrontCtlLine() && InitRearCtlLine() && InitSerial())
		{
		InitSonar();
		run = true;
		start_time = 0;
		log_start_time = 0;
		if (pthread_create(&monitor, NULL, MonitorThread, this) == 0)
			{
			rtn = true;
			}
		}
	return(rtn);
}



void EzSSonar::RecordSonarData(bool front,int ltime)

{
	time_t tt;
	tm* tmp;
	char buffer[200];
	int i;
	struct timeval tv;

	time(&tt);
	sprintf(buffer, "%s%sMCSonarData%ld.csv", BASE_DIR, DATA_DIR, tt);
	strcpy(last_sonar_file,buffer);
	if (sd_log.OpenLog(buffer, (char *)  "MC Sonar data log", false))
		{
		tmp = localtime(&tt);
		sprintf(buffer,"%d/%d/%d  %d:%d",tmp->tm_mon + 1,tmp->tm_mday,tmp->tm_year + 1900,tmp->tm_hour,tmp->tm_min);
		sd_log.LogEntry(buffer);
		sd_log.LogEntry((char *) "");
		if (front)
			{
			sd_log.LogEntry((char *) "Timestamp (msec),Front clearance (in)");
			}
		else
			{
			sd_log.LogEntry((char *) "Timestamp (msec),Rear clearance (in)");
			}
		log_time = ltime * 60000;
		gettimeofday(&tv, NULL);
		log_start_time = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
		log = true;
		}

}



void EzSSonar::StopRecord()

{
	if (log)
		{
		log = false;
		log_time = 0;
		log_start_time = 0;
		sd_log.CloseLog((char *) "stop record received");
		}
}



int EzSSonar::ReadFrontSonar()

{
	int sval = -1;
	unsigned char buff[10];

	if (forward_fd >= 0)
		{
		tcflush(forward_fd, TCIFLUSH);
		FrontCtlLineHigh();
		usleep(25);
		FrontCtlLineLow();
		usleep(50000);
		if (ReadBytes(forward_fd, buff, 5))
			{
			buff[5] = 0;
			sval = atoi((char *)&buff[1]);
			if ((sval <= 0) || (sval > 254))
				sval = -1;
			else if (sval <= MIN_FRONT_CLEARANCE)
				{
				front_min_count += 1;
				if (front_min_count < 3)
					sval = ReadFrontSonar();
				else
					front_min_count = 0;
				}
			else
				front_min_count = 0;
			}
		}
	return(sval);
}



int EzSSonar::ReadRearSonar()

{
	int sval = -1;
	unsigned char buff[10];

	if (backward_fd >= 0)
		{
		tcflush(backward_fd, TCIFLUSH);
		RearCtlLineHigh();
		usleep(25);
		RearCtlLineLow();
		usleep(50000);
		if (ReadBytes(backward_fd, buff, 5))
			{
			buff[5] = 0;

#ifdef DEBUG_REAR
			app_log.LogEntry((char *) buff);
#endif
			sval = atoi((char *)&buff[1]);
			if ((sval <= 0) || (sval > 254))
				{
				sval = -1;
				rear_min_count = 0;
				}
			else if (sval <= MIN_REAR_CLEARANCE)
				{
				rear_min_count += 1;
				if (rear_min_count < 3)
					sval = ReadRearSonar();
				else
					rear_min_count = 0;
				}
			else
				rear_min_count = 0;
			}
		}
	return(sval);
}



void EzSSonar::SetSampling(sonar_sample ss)

{
	if (ss == FRONT)
		{
		sample_front = true;
		sample_rear = false;
		}
	else if (ss == REAR)
		{
		sample_front = false;
		sample_rear = true;
		}
	else if (ss == BOTH)
		{
		sample_front = true;
		sample_rear = true;
		}
	else
		{
		sample_front = false;
		sample_rear = false;
		}
}



void EzSSonar::Close()

{
	char line[100];

	if (run)
		{
		run = false;
		pthread_join(monitor,NULL);
		}
	CloseFrontCtlLine();
	CloseRearCtlLine();
	CloseSerial();
}
