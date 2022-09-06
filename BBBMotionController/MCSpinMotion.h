#ifndef MCSPIN_MOTION
#define MCSPIN_MOTION

#include <semaphore.h>
#include "SpinMotion.h"



struct mcturn_param
{
	double target_angle;
	spin_directions direction;
	int max_speed;
	int accel;
	spin_speed ss;
	double gbias;
	double stop_offset;
	double so_slope;
	double so_intercept;
	double volts;
};


class MCSpinMotion

{

private:

	static MCSpinMotion *sm;
	static double raw_gyro[];
	static int st[];
	static int enc[];
	static unsigned char notes[];
	static int sample;
	static unsigned long long start_time, last_time;
	static double last_v;
	static double dist;
	static bool encoder_run;
	static int encoder;
	static mcturn_param tp;
	static double lastats;
	static char motion_error[100];
	static bool stopping, starting;
	static int samples;
	static int same_count;
	static sem_t motion_complete;
	static int missed_samples;
	static int sample_overflow;
	static double sd_dist;
	pthread_t alrm_handler;

	MCSpinMotion();
	MCSpinMotion(MCSpinMotion const&){};
	MCSpinMotion& operator=(MCSpinMotion const&){};

	static void* EncoderUpdateThread(void *);
	static void StopAlarm();
	static void CalcPosition(double, double);
	static void *AlarmHandler(void *);
	static void StopMotion();
	bool ReadParameters(char *);
	bool SetTurnParameters(spin_directions,int, spin_speed);


public:

	static MCSpinMotion* Instance();
	bool StartSpin(spin_directions,int, spin_speed);
	void StopSpin();
	char *MotionError();
	void RecordSensorData();
};

#endif