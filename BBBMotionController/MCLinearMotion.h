#ifndef MCLINEAR_MOTION
#define MCLINEAR_MOTION

#include <semaphore.h>
#include "LinearMotion.h"


struct mclinear_param
{
	linear_directions direction;
	int target_dist;
	int max_speed;
	int rnow;
	int lnow;
	int accel;
	double fconstant;
	double steady_stop_dist;
	int sonar_stop_dist;
	double velocity_limit;
	int max_vel_time_limit;
	linear_speed ls;
	double pgain, igain, dgain;
};



class MCLinearMotion

{

	private:

	static MCLinearMotion *lm;
//	static EzSonar *sonr;
	static EzSSonar *sonr;
	static double raw_accel[];
	static double raw_gyro[];
	static int st[];
	static int enc[];
	static int sonar[];
	static unsigned char notes[];
	static double last_a, last_v,dist,gyroheading,oldgyrov;
	static int sample;
	static unsigned long long start_time, last_time,sd_time;
	static bool encoder_run;
	static int encoder;
	static mclinear_param lp;
	static char motion_error[100];
	static bool stopping, starting;
	static int samples;
	static int same_count;
	static sem_t motion_complete;
	static int missed_samples;
	static int sample_overflow;
	static double stop_dist;
	static double sum_gh,last_gh;
	static int applied_correct;
	static bool first_calc;
	static double sd_dist;
	static int max_dt,min_dt;
	static bool no_dist_calc;
	static int front_clearance;
	pthread_t alrm_handler;

	MCLinearMotion();
	MCLinearMotion(MCLinearMotion const&){};
	MCLinearMotion& operator=(MCLinearMotion const&){};

	static void* EncoderUpdateThread(void *);
	static void StopAlarm();
	static void CalcPosition(double,double,double);
	static void ChngSpeed(int, int);
	static void *AlarmHandler(void *);
	static void StopMotion();
	static bool ClearanceOk();
	void StartBackward();
	void StartForward();
	bool ReadParameters(char *);
	bool ReadParameters(char *,mclinear_param *);
	bool SetLinearParameters(linear_directions, linear_speed);


	public:

	static MCLinearMotion* Instance();
	bool MoveLinear(linear_directions,int,linear_speed);
	void StopLinear();
	void SwitchToDR(int);
	int MaxNormalVelocity();
	void SetFrontClearance(int);
	char *MotionError();
	int LastMoveDist();
	void RecordSensorData();
};

#endif