#include <fcntl.h>
#include <unistd.h>
#include <errno.h>
#include <stdlib.h>
#include "VoltSensor.h"
#include "log.h"

#define AIN0 "/sys/bus/iio/devices/iio:device0/in_voltage0_raw"

int volt_line_fd = -1;
VoltSensor* VoltSensor::sensor = NULL;


VoltSensor::VoltSensor()

{
}


bool VoltSensor::Init()

{
	bool rtn = false;

	volt_line_fd = open(AIN0,O_RDONLY);
	if (volt_line_fd >= 0)
		rtn = true;
	return(rtn);
}



void VoltSensor::Close()

{
	if (volt_line_fd >= 0)
		close(volt_line_fd);
}



double VoltSensor::GetVolts()

{
	double val = -1;
	int no,err;
	int ival;
	char buff[10];

	if (volt_line_fd >= 0)
		{
		lseek(volt_line_fd, 0, SEEK_SET);
		do
			{
			no = read(volt_line_fd, buff, sizeof(buff));
			if (no < 0)
				{
				err = *(__errno_location());
				if (err == EAGAIN)
					{
					usleep(10000);
					}
				}
			}
		while ((no == -1) && (err == EAGAIN));
		if (no > 0)
			{
			buff[no] = 0;
			ival = atoi(buff);
			val = ival * .00703	; // BBB is 1.8v (ADC value of 4095), input is 5v for 80v source so 1.8v = 28.8v source
			}
		}
	return(val);
}


VoltSensor* VoltSensor::Instance()

{
	if (sensor == NULL)
		sensor = new VoltSensor;
	return(sensor);		
}
