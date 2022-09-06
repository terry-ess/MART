#ifndef LINEAR_MOTION
#define LINEAR_MOTION

#include <semaphore.h>
#include "EZSSonar.h"


enum linear_directions { FORWARD, BACKWARD };
enum linear_speed { NORMAL, FAST,SLOW };


struct linear_param
{
	linear_directions direction;
	int max_speed;
	double fconstant;
	int rnow;
	int lnow;
	int accel;
	linear_speed ls;
};



struct motor_param
{
	int max_speed;
	int accel;
};


struct pid_param
{
	double pgain, igain, dgain;
};


class LinearMotion

{

	private:

	static LinearMotion *lm;
//	static EzSonar *sonr;
	static EzSSonar *sonr;
	static double raw_accel_y[];
//	static double raw_accel_x[];
//	static double raw_accel_z[];
//	static double raw_gyro_y[];
//	static double raw_gyro_x[];
	static double raw_gyro_z[];
	static int st[];
	static int enc[];
//	static int sonar[];
	static unsigned char notes[];
	static double last_a, last_v,dist;
	static int sample;
	static unsigned long long start_time, last_time,sd_time;
	static bool encoder_run;
	static int encoder;
	static linear_param lp;
	static char motion_error[100];
	static bool stopping, starting;
	static int samples;
	static int same_count;
	static sem_t motion_complete;
	static int missed_samples;
	static int sample_overflow;
	pthread_t alrm_handler;
	motor_param mp;
	bool mp_set;
	static pid_param pp;
	static bool pp_set;
	static double sum_gh, last_gh;
	static int applied_correct;
	static bool first_calc;
	static double gyroheading, oldgyrov;

	LinearMotion();
	LinearMotion(LinearMotion const&){};
	LinearMotion& operator=(LinearMotion const&){};

	static void StopAlarm();
	static void* EncoderUpdateThread(void *);
	static void CalcPosition(double,double,double);
	static void *AlarmHandler(void *);
	static void StopMotion();
	static bool ClearanceOk();
	void StartBackward();
	void StartForward();
	static bool ChngSpeed(int, int);
	bool ReadParameters(char *);
	bool SetLinearParameters(linear_directions, linear_speed);


	public:

	static LinearMotion* Instance();
	bool MoveLinear(linear_directions direc, linear_speed ls);
	void StopLinear();
	bool ChangeSpeed(int,int);
	char *MotionError();
	void RecordSensorData();
	void SetMotorParam(int,int);
	void ClearMotorParam();
	void SetPIDParam(double,double,double);
	void ClearPIDParam();

};

#endif