#ifndef EZ_SSONAR
#define EZ_SSONAR

#include <pthread.h>
#include "log.h"

enum sonar_sample { NONE,FRONT, REAR,BOTH };

class EzSSonar
	{
	private:


	int frontctl_line_fd;
	int rearctl_line_fd;
	int forward_fd;
	int backward_fd;
	pthread_t monitor;
	bool run ;
	bool sample_front;
	bool sample_rear;
	int rear_min_count;
	int front_min_count;
	static EzSSonar* sensor;
	static unsigned long long start_time;
	unsigned long long log_start_time;
	unsigned long long log_time;
	Log sd_log;
	bool log;
	bool log_front;
	

	EzSSonar();
	EzSSonar(EzSSonar const&){};
	EzSSonar& operator=(EzSSonar const&){};

	static void *MonitorThread(void*);
	bool ReadBytes(int,unsigned char *, int);
	bool SensorData(int *,int *);
	bool Expose(char *);
	bool InitFrontCtlLine();
	void CloseFrontCtlLine();
	bool FrontCtlLineHigh();
	bool FrontCtlLineLow();
	bool InitRearCtlLine();
	void CloseRearCtlLine();
	bool RearCtlLineHigh();
	bool RearCtlLineLow();
	bool Open(char  *, int *);
	bool InitSerial();
	void CloseSerial();
	void InitSonar();


	public:

	int front_clearance;
	int rear_clearance;

	static EzSSonar* Instance();
	bool Init();
	int ReadFrontSonar();
	int ReadRearSonar();
	void SetSampling(sonar_sample);
	void RecordSonarData(bool, int);
	void StopRecord();
	void Close();
	};

#endif