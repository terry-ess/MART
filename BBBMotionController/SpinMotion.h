#ifndef SPIN_MOTION
#define SPIN_MOTION

#include <semaphore.h>


enum spin_directions { RIGHT, LEFT };
enum spin_speed { SSLOW, SNORMAL, SMIDDLE, SFAST,SCUSTOM};


struct turn_param
{
	spin_directions direction;
	int max_speed;
	int accel;
	spin_speed ss;
	double gbias;
};


class SpinMotion

{

private:

	static SpinMotion *sm;
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
	static turn_param tp;
	static double lastats;
	static char motion_error[100];
	static bool stopping, starting;
	static int samples;
	static int same_count;
	static sem_t motion_complete;
	static int missed_samples;
	static int sample_overflow;
	pthread_t alrm_handler;
	int max_speed;
	int accel;
	double volts;

	SpinMotion();
	SpinMotion(SpinMotion const&){};
	SpinMotion& operator=(SpinMotion const&){};

	static void StopAlarm();
	static void* EncoderUpdateThread(void *);
	static void CalcPosition(double, double);
	static void *AlarmHandler(void *);
	static void StopMotion();
	bool ReadParameters(char *);
	bool SetTurnParameters(spin_directions, spin_speed);


public:

	static SpinMotion* Instance();
	void SetParam(int accel,int mspeed);
	bool StartSpin(spin_directions,spin_speed);
	void StopSpin();
	char *MotionError();
	int LastMoveAngle();
	void RecordSensorData();
};

#endif