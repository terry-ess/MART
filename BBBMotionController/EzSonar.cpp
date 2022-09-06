#include <stdlib.h>
#include <stddef.h>
#include <unistd.h>
#include <fcntl.h>
#include <pthread.h>
#include <stdio.h>
#include <math.h>
#include <errno.h>
#include "EZSonar.h"
#include "SharedData.h"
#include "MotionController.h"


#define AIN0 "/sys/bus/iio/devices/iio:device0/in_voltage0_raw"
#define AIN1 "/sys/bus/iio/devices/iio:device0/in_voltage1_raw"
#define CTL_LINE_DIRECTION "/sys/class/gpio/gpio60/direction"
#define CTL_LINE_VALUE "/sys/class/gpio/gpio60/value"
#define CTL_LINE "60"

#define CONVERT_FACTOR 14.65


EzSonar* EzSonar::sensor = NULL;


EzSonar::EzSonar()

{
	ctl_line_fd = -1;
	forward_fd = -1;
	backward_fd = -1;
	monitor = -1;
	bool run = false;
	front_clearance = -1;
	rear_clearance = -1;
}



void *EzSonar::MonitorThread(void* param)

{
	EzSonar* me;

	me = (EzSonar *) param;
	while(me->run)
		{
		me->SensorData(&me->front_clearance,&me->rear_clearance);
		}
}



bool EzSonar::SensorData(int *front,int *rear)

{
	int no,err,no_tries,cno_tries,wtime;
	char buff[10];
	double val;

	if ((forward_fd >= 0) || (backward_fd >= 0))
		{
		CtlLineHigh();
		usleep(25);
		CtlLineLow();
		usleep(50000);
		if (forward_fd >= 0)
			{
			lseek(forward_fd, 0, SEEK_SET);
			no_tries = 0;
			do
				{
				no = read(forward_fd, buff, sizeof(buff));
				if (no < 0)
					{
					err = *(__errno_location());
					if (err == EAGAIN)
						{
						no_tries += 1;
						usleep(10000);
						}
					}
				}
			while ((no == -1) && (err == EAGAIN));
			if (no_tries > max_no_fr_tries)
				max_no_fr_tries = no_tries;
			cno_tries = no_tries;
			if (no > 0)
				{
				buff[no] = 0;
				val = atoi(buff);
				*front = round(val/CONVERT_FACTOR);
				}
			else
				{
				*front = -1;
				}
			}
		else
			*front = -1;
		if (backward_fd >= 0)
			{
			lseek(backward_fd, 0, SEEK_SET);
			do
				{
				no = read(backward_fd, buff, sizeof(buff));
				no_tries = 0;
				if (no < 0)
					{
					err = *(__errno_location());
					if (err == EAGAIN)
						{
						usleep(10000);
						no_tries += 1;
						}
					}
				}
			while ((no == -1) && (err == EAGAIN));
			if (no_tries > max_no_fr_tries)
				max_no_fr_tries = no_tries;
			cno_tries += no_tries;
			if (no > 0)
				{
				buff[no] = 0;
				val = atoi(buff);
				*rear = round(val/CONVERT_FACTOR);
				}
			else
				*rear = -1;
			}
		else
			*rear = -1;
		}
	else
		{
		*front = -1;
		*rear = -1;
		}
	wtime = 48000 - (cno_tries * 10000);
	if (wtime > 1000)
		usleep(wtime);
}



bool EzSonar::InitCtlLine()

{
	int fd, no;
	bool rtn = false;

	fd = open(GPIO_EXPORT, O_WRONLY);
	if (fd >= 0)
		{
		no = write(fd,CTL_LINE, 2);
		close(fd);
		fd = open(CTL_LINE_DIRECTION, O_WRONLY);
		if (fd >= 0)
			{
			no = write(fd, "low", 3);
			close(fd);
			if (no == 3)
				{
				ctl_line_fd = open(CTL_LINE_VALUE, O_WRONLY);
				if (ctl_line_fd >= 0)
					rtn = true;
				}
			}
		}
	return(rtn);
}



void EzSonar::CloseCtlLine()

{
	int fd, no;

	fd = open(GPIO_UNEXPORT, O_WRONLY);
	if (fd >= 0)
		{
		no = write(fd,CTL_LINE, 2);
		close(fd);
		}
	if (ctl_line_fd >= 0)
		close(ctl_line_fd);
}



bool EzSonar::CtlLineHigh()

{
	int no;
	bool rtn = false;

	if (ctl_line_fd >= 0)
		{
		no = write(ctl_line_fd, "1", 2);
		if (no > 0)
			rtn = true;
		}
	return(rtn);
}



bool EzSonar::CtlLineLow()

{
	int no;
	bool rtn = false;

	if (ctl_line_fd >= 0)
		{
		no = write(ctl_line_fd, "0", 2);
		if (no > 0)
			rtn = true;
		}
	return(rtn);
}



bool EzSonar::InitAI()

{
	bool rtn = false;

	forward_fd = open(AIN0, O_RDONLY);
	backward_fd = open(AIN1,O_RDONLY);
	if ((forward_fd >= 0) && (backward_fd >= 0))
		rtn = true;
	return(rtn);
}



void EzSonar::CloseAI()

{
	if (forward_fd >= 0)
		close(forward_fd);
	if (backward_fd >= 0)
		close(backward_fd);
}



void EzSonar::InitSonar()

{
	usleep(250000);
	CtlLineHigh();
	usleep(25);
	CtlLineLow();
	usleep(150000);
}



EzSonar* EzSonar::Instance()

{
	if (sensor == NULL)
		sensor = new EzSonar;
	return(sensor);
}



bool EzSonar::Init()

{
	bool rtn = false;

	if (InitCtlLine() && InitAI())
		{
		InitSonar();
		run = true;
		max_no_fr_tries = 0;
		if (pthread_create(&monitor, NULL, MonitorThread, this) == 0)
			{
			rtn = true;
			}
		}
	return(rtn);
}



void EzSonar::Close()

{
	char line[50];

	if (run)
		{
		run = false;
		pthread_join(monitor,NULL);
		}
	CloseCtlLine();
	CloseAI();
	sprintf(line,"Sonar max number file read tries: %d",max_no_fr_tries);
	app_log.LogEntry(line);
}
