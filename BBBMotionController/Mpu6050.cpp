
#include "Mpu6050.h"
#include "SharedData.h"
#include "MotionController.h"
#include <time.h>
#include <errno.h>

// CONSTANTS

#define ADDRESS 0x68
#define TRANSACTION_TIMEOUT 1000
#define I2C_FILE_NAME "/dev/i2c-1"	 // I2C2 on P9 (pins 19 & 20) on Debian 7 (Wheezy)
		
#define PWR_MGMT_1 0x6B
#define NO_SLEEP_CLK_TO_GZ 0x03
		
#define WHO_AM_I 0x75
#define DEVICE_ID 0x68
		
#define DLPF_CONFIG 0x1A
#define DLPF_256 0X00
#define DLPF_188 0X01
#define DLPF_98 0X02
#define DLPF_42 0X03
#define DLPF_20 0X04
#define DLPF_10 0X05
#define DLPF_5 0X06

#define GYRO_CONFIG 0x1B
#define GYRO_FS_250 0x00
#define GYRO_FS_500 0x08
#define GYRO_FS_1000 0x10
#define GYRO_FS_2000 0x18

#define ACCEL_CONFIG 0x1C
#define ACCEL_FS_2  0x00
#define ACCEL_FS_4 0x08
#define ACCEL_FS_8 0x10
#define ACCEL_FS_16 0x18

#define SENSOR_DATA 0x3B

#define PARAM_FILE "/cal/mpu6050.param"


// DATA

Mpu6050* Mpu6050::sensor = NULL;


// FUNCTIONS

bool Mpu6050::I2cWrite(char *buf,int len)

{
	bool rtn = false;
	int rsp;
	char line[100];
	
	if ((rsp = write(fhandle,buf,len)) != len)
		{
		if (rsp > 0)
			sprintf(line,"I2c write failed, wrote %d bytes not %d bytes.",rsp,len);
		else if (rsp < 0)
			sprintf(line, "I2c write failed, error no %d", *(__errno_location()));
		else
			strcpy(line,"I2c write failed, end of file");
		app_log.LogEntry(line);
		}
	else
		rtn = true;
	return(rtn);
}



bool Mpu6050::I2cRead(char *buf,int len)

{
	bool rtn = false;
	int rsp;
	char line[100];

	if ((rsp = read(fhandle,buf,len)) != len)
		{
		if (rsp > 0)
			sprintf(line,"I2c read failed, read %d bytes not %d bytes.",rsp,len);
		else if (rsp < 0)
			sprintf(line, "I2c read failed, error no %d", *(__errno_location()));
		else
			strcpy(line,"I2c read failed, end of file");
		app_log.LogEntry(line);
		}
	else
		rtn = true;
	return(rtn);
}



bool Mpu6050::ReadParameters()

{
	FILE *pfile = NULL;
	char line[128];
	bool rtn = false;

	sprintf(line, "%s%s", BASE_DIR, PARAM_FILE);
	if ((pfile = fopen(line, "r")) != NULL)
		{
		fgets(line, sizeof(line)-1, pfile);
		max_accel = atoi(line);
		fclose(pfile);
		rtn = true;
		}
	return(rtn);
}



bool Mpu6050::Initialize()

{
	bool rtn = false;
	int id;

	id = GetDeviceID();
	SetPwrMgt1(NO_SLEEP_CLK_TO_GZ);
	SetDlpfConfig(DLPF_256);
	SetGyroConfig(GYRO_FS_250);
	if (ReadParameters())
		{
		switch (max_accel)
			{
			case 4:
				accel_setting = ACCEL_FS_4;
				break;

			case 8:
				accel_setting = ACCEL_FS_8;
				break;

			case 16:
				accel_setting = ACCEL_FS_16;
				break;

			default:
				accel_setting = ACCEL_FS_2;
				max_accel = 2;
				break;
			}
		}
	else
		{
		accel_setting = ACCEL_FS_2;
		max_accel = 2;
		}
	SetAccelConfig(accel_setting);
	if ((id = GetDeviceID()) == DEVICE_ID)
		rtn = true;
}
 



int Mpu6050::GetDeviceID() 
		
{
	unsigned char buf[2];
	int rtn = -1;

	buf[0] = WHO_AM_I;
	if (I2cWrite((char *) buf,1))
		{
		if (I2cRead((char *) buf,1))
			rtn = buf[0];
		}
	return(rtn);
}



bool Mpu6050::SetPwrMgt1(unsigned char value)
		 
{
	unsigned char buf[2];

	buf[0] = PWR_MGMT_1;
	buf[1] = value;
	return(I2cWrite((char *) buf,2));
}



bool Mpu6050::SetGyroConfig(unsigned char value)

{
	unsigned char buf[2];

	buf[0] = GYRO_CONFIG;
	buf[1] = value;
	return(I2cWrite((char *) buf,2));
}



bool Mpu6050::SetAccelConfig(unsigned char value)

{
	unsigned char buf[2];

	buf[0] = ACCEL_CONFIG;
	buf[1] = value;
	return(I2cWrite((char *) buf,2));
}




bool Mpu6050::SetDlpfConfig(unsigned char value)

{
	unsigned char buf[2];

	buf[0] = DLPF_CONFIG;
	buf[1] = value;
	return(I2cWrite((char *) buf,2));
}



Mpu6050* Mpu6050::Instance()

{
	if (sensor == NULL)
		sensor = new Mpu6050;
	return(sensor);
}



bool Mpu6050::GetSensorData(sensor_data *sd)

{
	bool rtn = false;
	unsigned char buffer[14];

	if (fhandle != -1)
		{
		buffer[0] = SENSOR_DATA;
		if (I2cWrite((char *) buffer,1))
			{
			if (I2cRead((char *) buffer,14))
				{
				sd->accel_x = (((buffer[0]) << 8) | buffer[1]);
				sd->accel_y = (((buffer[2]) << 8) | buffer[3]);
				sd->accel_z = (((buffer[4]) << 8) | buffer[5]);
				sd->gyro_x = (((buffer[8]) << 8) | buffer[9]);
				sd->gyro_y = (((buffer[10]) << 8) | buffer[11]);
				sd->gyro_z = (((buffer[12]) << 8) | buffer[13]);
				rtn = true;
				}
			}
		}	
	return(rtn);
}



Mpu6050::Mpu6050()

{
	fhandle = -1;
}


bool Mpu6050::DetermineSensorBias()

{
	bool rtn = false;
	int scount;
	struct sensor_data sd;
	double accel_y = 0,gyro_z = 0;
	timespec ts;

	if (Init())
		{
		scount = 0;
		ts.tv_sec = 0;
		ts.tv_nsec = 10000000;
		do
			{
			if (GetSensorData(&sd))
				{
				accel_y += ((double)sd.accel_y / MAX_SENSOR_VALUE) * max_accel;
				gyro_z += ((double)sd.gyro_z / MAX_SENSOR_VALUE) * 250;
				scount += 1;
				}
			nanosleep(&ts,NULL);
			}
		while (scount < 50);
		abias = accel_y/scount;
		gbias = gyro_z/scount;
		rtn = true;
		}
	return(rtn);
}



void Mpu6050::ShortDetermineSensorBias()

{
	int scount;
	struct sensor_data sd;
	double accel_y = 0, gyro_z = 0;
	timespec ts;

	scount = 0;
	ts.tv_sec = 0;
	ts.tv_nsec = 10000000;
	do
		{
		if (GetSensorData(&sd))
			{
			accel_y += ((double)sd.accel_y / MAX_SENSOR_VALUE) * max_accel;
			gyro_z += ((double)sd.gyro_z / MAX_SENSOR_VALUE) * 250;
			scount += 1;
			}
		nanosleep(&ts,NULL);
		}
	while (scount < 10);
	abias = accel_y/scount;
	gbias = gyro_z/scount;
}




bool Mpu6050::Init()

{
	bool rtn = false;
	int i,scount,fcount;
	sensor_data sd;
	double accel_x,accel_y,accel_z,total = 0;
	timespec ts;

	if (fhandle == -1)
		{
		fhandle = open(I2C_FILE_NAME, O_RDWR);
		if (fhandle < 0)
			app_log.LogEntry((char *) "Could not open i2c.");
		else
			{
			if (ioctl(fhandle,I2C_SLAVE,ADDRESS) < 0)
				{
				app_log.LogEntry((char *) "Could not set i2c address");
				close(fhandle);
				fhandle = -1;
				}
			else
				{
				ts.tv_sec = 0;
				ts.tv_nsec = 10000000;
				for (i = 1;i < 3;i++)
					{
					if (Initialize())
						{
						scount = 0;
						fcount = 0;
						do
							{
							if (GetSensorData(&sd))
								{
								accel_x = ((double)sd.accel_x / MAX_SENSOR_VALUE) * max_accel;
								accel_y = ((double)sd.accel_y / MAX_SENSOR_VALUE) * max_accel;
								accel_z = ((double)sd.accel_z / MAX_SENSOR_VALUE) * max_accel;
								total += accel_x  + accel_y + accel_z;
								scount += 1;
								fcount = 0;
								}
							else
								fcount += 1;
							nanosleep(&ts,NULL);
							}
						while ((scount < 10) && (fcount < 10));
						if ((fcount == 0) && (total != 0))
							{
							rtn = true;
							break;
							}
						}
					}
				if (i == 3)
					Close();
				}
			}
		}
	else
		rtn = true;
	return(rtn);
}



void Mpu6050::Close()

{
	if (fhandle != -1)
		{
		close(fhandle);
		fhandle = -1;
		}

}
