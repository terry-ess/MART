#ifndef REF_MOTION
#define REF_MOTION

#include <semaphore.h>
#include "EZSSonar.h"


struct mtr_param
{
	int rnow;
	int lnow;
};


class RefMotion

{
	private:

	static RefMotion *rm;
	static EzSSonar *sonr;

	RefMotion();
	RefMotion(RefMotion const&) {};
	RefMotion& operator=(RefMotion const&) {};

	static unsigned long long start_time, last_time, sd_time;
	static bool encoder_run,encoder_sample;
	static int encoder,last_encoder;
	static char motion_error[100];
	static bool stopping, starting;
	static int samples;
	static sem_t motion_complete;
	static int same_count;
	static mtr_param rmmp;
	pthread_t alrm_handler;
	static int sample;
	static int st[];
	static int rmtr[];
	static int lmtr[];
	static double last_gz;
	static double rangle;
	static int stop_time_out;


	static void StopAlarm();
	static void CalcPosition(double,double);
	static void* EncoderUpdateThread(void *);
	static void* AlarmHandler(void *);
	static void StopMotion();
	static bool ClearanceOk();
	static bool ChngSpeed(int, int);

	public:

	static RefMotion* Instance();
	bool StartRM(int);
	void StopRM();
	bool ChangeSpeed(int, int);
	char *MotionError();
	void RecordMotionData();
	double RelativeAngle();

};

#endif

