#ifndef MOTOR_CONTROLLER
#define MOTOR_CONTROLLER

#include <fcntl.h>
#include <unistd.h>
#include <string.h>
#include <termios.h>
#include <pthread.h>


class MotorController

{

	private:

	int fd;
	int lock_holder;
	pthread_mutex_t serial_access_mutex;
	static bool run_test;
	static int ts[];
	static int samples;
	
	bool ReadBytes(unsigned char *,int no);
	static void* SerialTestThread(void *);

	public:

	MotorController();
	bool Open();
	void Close();
	int GetError();
	void SetAccel(unsigned char);
	void StopMotion();
	void SetMode(unsigned char);
	void StartBackward(unsigned char);
	void StartForward(unsigned char);
	void ChngSpeed(unsigned char, unsigned char);
	void StartRightSpin(unsigned char);
	void StartLeftSpin(unsigned char);
	void DisableTimeout();
	void EnableRegulator();
	void DisableRegulator();
	void ResetEncoders();
	bool ReadEncoder(int *);
	bool StartSerialTest();
	void StopSerialTest();
	void RecordSerialTest();
};

#endif