#ifndef RECORD
#define RECORD

#include "Mpu6050.h"


class Record

{
	private:

	static double raw_accel_y[];
	static double raw_accel_x[];
	static double raw_accel_z[];
	static double raw_gyro_y[];
	static double raw_gyro_x[];
	static double raw_gyro_z[];
	static int st[];
	static int sample;
	static unsigned long long start_time, last_time;
	pthread_t alrm_handler;
	static int missed_samples;
	static int sample_overflow;

	bool recording;

	static void *AlarmHandler(void *);
	void RecordSensorData();
	
	public:

	Record();
	bool Start();
	void Stop();
};

#endif