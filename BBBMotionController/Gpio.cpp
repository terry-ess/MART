#include <fcntl.h>
#include <unistd.h>
#include <stdio.h>
#include <stdlib.h>
#include "Gpio.h"
#include "SharedData.h"


#define RUNLED_GPIO_NO 60  // P9_12
#define DOCK_GPIO_NO 49  // P9_23


Gpio* Gpio::gpio = NULL;
int runledfd = -1;
int dockfd = -1;


Gpio::Gpio()

{
}



bool Gpio::Expose(int io_no)

{
	bool rtn = false;
	int fd,no;
	char stg[50];

	fd = open(GPIO_EXPORT, O_WRONLY);
	if (fd >= 0)
		{
		sprintf(stg, "%d",io_no);
		no = write(fd,stg, 2);
		close(fd);
		rtn = true;
		}
	return(rtn);
}



bool Gpio::InitIO(int io_no, bool in, int *bfd)

{
	bool rtn = false;
	int fd, no;
	char stg[50];

	sprintf(stg, "/sys/class/gpio/gpio%d/direction", io_no);
	fd = open(stg, O_WRONLY);
	if (fd < 0)
		{
		if (Expose(io_no))
			fd = open(stg,O_WRONLY);
		}
	if (fd >= 0)
		{
		if (in)
			no = write(fd, "in", 2);
		else
			no = write(fd, "low", 3);
		close(fd);
		if (in && (no = 2))
			{
			sprintf(stg, "/sys/class/gpio/gpio%d/value", io_no);
			*bfd = open(stg, O_RDONLY);
			if (*bfd > 0)
				rtn = true;
			}
		else if (!in && (no == 3))
			{
			sprintf(stg, "/sys/class/gpio/gpio%d/value", io_no);
			*bfd = open(stg, O_WRONLY);
			if (*bfd > 0)
				rtn = true;
			}
		}
	return(rtn);
}



void Gpio::CloseIO(int io_no,int *bfd)

{
	int fd,no;
	char io_id[5];

	close(*bfd);
	*bfd = -1;
}



Gpio* Gpio::Instance()

{
	if (gpio == NULL)
		gpio = new Gpio;
	return(gpio);
}



bool Gpio::Init()

{
	int fd, no;
	bool rtn = false;
	char io_id[5];

	if (InitIO(RUNLED_GPIO_NO,false,&runledfd))
		if (InitIO(DOCK_GPIO_NO,true,&dockfd))
			rtn = true;
	if (!rtn)
		Close();
	return(rtn);
}



void Gpio::Close()

{
	int fd,no;
	char io_id[5];

	if (runledfd > 0)
		CloseIO(RUNLED_GPIO_NO,&runledfd);
	if (dockfd > 0)
		CloseIO(DOCK_GPIO_NO,&dockfd);
}



void Gpio::RunLed(bool on)

{
	if (on)
		write(runledfd, "1", 1);
	else
		write(runledfd, "0", 1);
}



bool Gpio::Docked()

{
	int no;
	char buff[3];
	bool rtn = false;

	if (dockfd > 0)
		{
		lseek(dockfd, 0, SEEK_SET);
		no = read(dockfd, buff, sizeof(buff));
		if (no > 0)
			{
			buff[no] = 0;
			no = atoi(buff);
			if (no > 0)
				rtn = true;
			}
		}
	return(rtn);
}
