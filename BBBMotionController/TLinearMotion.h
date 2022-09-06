#ifndef TLINEAR_MOTION
#define TLINEAR_MOTION

#include <semaphore.h>
#include "LinearMotion.h"


struct tlinear_param
{
	linear_directions direction;
	int target_dist;
	int max_speed;
	int rnow;
	int lnow;
	int accel;
	double volts;
	double base_top_speed;
	double top_speed;
	int accel_time;
	int decel_time;
	linear_speed ls;
	bool docking;
	double pgain, igain, dgain;
	double vc_slope,vc_intercept;
	bool ncc;
};



/*struct pid_param
{
	double pgain, igain, dgain;
}; */


class TLinearMotion

{

private:

	static TLinearMotion *lm;
	//	static EzSonar *sonr;
	static EzSSonar *sonr;
	static double raw_accel[];
	static double raw_gyro[];
	static int st[];
	static int enc[];
	static int sonar[];
	static unsigned char notes[];
	static int stop_enc[];
	static double gyroheading, oldgyrov;
	static int sample;
	static unsigned long long start_time, last_time, sd_time;
	static bool encoder_run;
	static int encoder;
	static tlinear_param lp;
	static char motion_error[100];
	static bool stopping, starting;
	static int samples;
	static int same_count;
	static sem_t motion_complete;
	static int missed_samples;
	static int sample_overflow;
	static int stop_time;
	static double sum_gh, last_gh;
	static int applied_correct;
	static bool first_calc;
	static int max_dt, min_dt;
	static bool no_dist_calc;
	static int front_clearance;
	static double rdist;
	static bool dock_detected;
	pthread_t alrm_handler;
	static pthread_mutex_t counter_access;
	pid_param pp;
	bool pp_set;

	TLinearMotion();
	TLinearMotion(TLinearMotion const&){};
	TLinearMotion& operator=(TLinearMotion const&){};

	static void* EncoderUpdateThread(void *);
	static void StopAlarm();
	static void CalcPosition(double,double);
	static void ChngSpeed(int, int);
	static void *AlarmHandler(void *);
	static void StopMotion();
	static bool ClearanceOk();
	void StartBackward();
	void StartForward();
	bool ReadParameters(char *);
	bool ReadParameters(char *, tlinear_param *);
	bool SetLinearParameters(linear_directions, linear_speed);


public:

	static TLinearMotion* Instance();
	bool MoveLinear(linear_directions, int, linear_speed,bool,bool);
	void StopLinear();
	void SwitchToDR(int);
	int MaxNormalVelocity();
	void SetFrontClearance(int);
	char *MotionError();
	int LastMoveDist();
	void RecordSensorData();
	void SetPidParam(double,double,double);
	void ClearPidParam();
	void ClearLastMoveDist();
};

#endif