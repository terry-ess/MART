#ifndef EZ_SONAR
#define EZ_SONAR



class EzSonar
	{
	private:


	int ctl_line_fd;
	int forward_fd;
	int backward_fd;
	pthread_t monitor;
	bool run;
	int max_no_fr_tries;
	static EzSonar* sensor;

	
	EzSonar();
	EzSonar(EzSonar const&){};
	EzSonar& operator=(EzSonar const&){};

	static void *MonitorThread(void*);
	bool SensorData(int *,int *);
	bool InitCtlLine();
	void CloseCtlLine();
	bool CtlLineHigh();
	bool CtlLineLow();
	bool InitAI();
	void CloseAI();
	void InitSonar();


	public:

	int front_clearance;
	int rear_clearance;

	static EzSonar* Instance();
	bool Init();
	void Close();
	};

#endif