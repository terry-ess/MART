#ifndef MPU6050_H
#define MPU6050_H

/* GLOBAL DEFINTIONS */

#include <unistd.h>
#include <stdlib.h>
#include <stdio.h>
#include <errno.h>
#include <endian.h>
#include <string.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/ioctl.h>
#include <fcntl.h>
#include <linux/i2c-dev.h>


/* CONSTANTS */

#define MAX_SENSOR_VALUE 32767


/* STRUCTURE DEFINITION */

struct sensor_data
{
	short int accel_x;
	short int accel_y;
	short int accel_z;
	short int gyro_x;
	short int gyro_y;
	short int gyro_z;
};

/* CLASS DEFINTION */

class Mpu6050
	{
	private:

	
	// DATA

	int fhandle;
	unsigned char accel_setting;
	static Mpu6050* sensor;

	
	// FUNCTIONS

	Mpu6050();
	Mpu6050(Mpu6050 const&){};
	Mpu6050& operator=(Mpu6050 const&){};

	bool I2cWrite(char *buf,int len);
	bool I2cRead(char *buf,int len);
	bool SetDlpfConfig(unsigned char value);
	bool SetAccelConfig(unsigned char value);
	bool SetGyroConfig(unsigned char value);
	bool SetPwrMgt1(unsigned char value);
	int GetDeviceID();
	bool ReadParameters();
	bool Initialize();

	public:

	double abias,gbias;
	int max_accel;

	static Mpu6050* Instance();
	bool GetSensorData(sensor_data *sd);
	bool DetermineSensorBias();
	void ShortDetermineSensorBias();
	bool Init();
	void Close();
	};

#endif