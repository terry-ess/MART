#include <time.h>
#include <sys/time.h>
#include <stdio.h>
#include "MotorController.h"
#include "SharedData.h"
#include "MotionController.h"


#define TTYDEVICE "/dev/ttyO2"	//RX - P9_22  TX - P9_21
#define TIME_OUT_COUNT 40
#define LOG_EXCESS_WR_TIME
#define EXCESS_TIME 100
//#define LOG_CS
#define DATA_CAPTURE 3000


static unsigned char get_error[] = { 0, 0x2D };
static unsigned char disable_reg[] = { 0, 0x36 };
static unsigned char enable_reg[] = { 0, 0x37 };
static unsigned char set_mode[] = { 0, 0x34, 0 };
static unsigned char stop[] = { 0, 0x31, 128, 0, 0x32, 128 };
static unsigned char set_accel[] = { 0, 0x33, 0x0 };
static unsigned char start[] = { 0, 0x31, 0, 0, 0x32, 0 };
static unsigned char disable_timeout[] = { 0, 0x38 };
static unsigned char reset_encoders[] = { 0, 0x35 };
static unsigned char get_encoder1[] = { 0, 0x23 };

bool MotorController::run_test;
int MotorController::ts[DATA_CAPTURE];
int MotorController::samples;


MotorController::MotorController()

{
	fd = 0;
	serial_access_mutex = PTHREAD_MUTEX_INITIALIZER;
}



bool MotorController::ReadBytes(unsigned char *buff, int bno)

{
	int pos = 0, trys = 0;
	bool rtn = false;
	int no;

	do
		{
		no = read(fd, &buff[pos], 1);
		if (no != 1)
			{
			trys += 1;
			usleep(5000);
			}
		else
			{
			pos += 1;
			trys = 0;
			}
		}
	while ((pos < bno) && (trys < TIME_OUT_COUNT));
	if (pos == bno)
		rtn = true;
	else
		rtn = false;
}



void MotorController::SetAccel(unsigned char accel)

{
	if (fd >= 0)
		{
		pthread_mutex_lock(&serial_access_mutex);
		lock_holder = 1;
		set_accel[2] = accel;
		write(fd,set_accel, 3);
		pthread_mutex_unlock(&serial_access_mutex);
		}
}



void MotorController::StopMotion()

{
	if (fd >= 0)
		{
		pthread_mutex_lock(&serial_access_mutex);
		lock_holder = 2;
		write(fd,stop,6);
		pthread_mutex_unlock(&serial_access_mutex);
		}
}



void MotorController::SetMode(unsigned char mode)

{
	if (fd >= 0)
		{
		pthread_mutex_lock(&serial_access_mutex);
		lock_holder = 3;
		set_mode[2] = mode;
		write(fd,set_mode,3);
		pthread_mutex_unlock(&serial_access_mutex);
		}
}



void MotorController::StartBackward(unsigned char max_speed)

{
	if (fd >= 0)
		{
		pthread_mutex_lock(&serial_access_mutex);
		lock_holder = 4;
		start[2] = (128 + max_speed);
		start[5] = (128 + max_speed);
		write(fd,start,6);
		pthread_mutex_unlock(&serial_access_mutex);
		}
}



void MotorController::StartForward(unsigned char max_speed)

{
	if (fd >= 0)
		{
		pthread_mutex_lock(&serial_access_mutex);
		lock_holder = 5;
		start[2] = (128 - max_speed);
		start[5] = (128 - max_speed);
		write(fd,start, 6);
		pthread_mutex_unlock(&serial_access_mutex);
		}
}



void MotorController::ChngSpeed(unsigned char right, unsigned char left)

{
	if (fd >= 0)
		{

#ifdef LOG_CS
		app_log.LogEntry((char *) "CS lock");
#endif

		pthread_mutex_lock(&serial_access_mutex);

#ifdef LOG_CS
		app_log.LogEntry((char *) "CS locked");
#endif

		lock_holder = 6;
		start[2] = right;
		start[5] = left;
		write(fd,start,6);
		pthread_mutex_unlock(&serial_access_mutex);

#ifdef LOG_CS
		app_log.LogEntry((char *) "CS unlocked");
#endif

		}
}



void MotorController::StartRightSpin(unsigned char max_speed)

{
	if (fd >= 0)
		{
		pthread_mutex_lock(&serial_access_mutex);
		lock_holder = 7;
		start[2] = (unsigned char)(128 + max_speed);
		start[5] = (unsigned char)(128 - max_speed);
		write(fd,start,6);
		pthread_mutex_unlock(&serial_access_mutex);
		}
}



void MotorController::StartLeftSpin(unsigned char max_speed)

{
	if (fd >= 0)
		{
		pthread_mutex_lock(&serial_access_mutex);
		lock_holder = 8;
		start[2] = (unsigned char)(128 - max_speed);
		start[5] = (unsigned char)(128 + max_speed);
		write(fd,start, 6);
		pthread_mutex_unlock(&serial_access_mutex);
		}
}



void MotorController::DisableTimeout()

{
	if (fd >= 0)
		{
		pthread_mutex_lock(&serial_access_mutex);
		lock_holder = 9;
		write(fd,disable_timeout,2);
		pthread_mutex_unlock(&serial_access_mutex);
		}
}



void MotorController::EnableRegulator()

{
	if (fd >= 0)
		{
		pthread_mutex_lock(&serial_access_mutex);
		lock_holder = 10;
		write(fd,enable_reg,2);
		pthread_mutex_unlock(&serial_access_mutex);
		}
}



void MotorController::DisableRegulator()

{
	if (fd >= 0)
		{
		pthread_mutex_lock(&serial_access_mutex);
		lock_holder = 11;
		write(fd,disable_reg,2);
		pthread_mutex_unlock(&serial_access_mutex);
		}
}



void MotorController::ResetEncoders()

{
	if (fd >= 0)
		{
		pthread_mutex_lock(&serial_access_mutex);
		lock_holder = 12;
		write(fd,reset_encoders,2);
		pthread_mutex_unlock(&serial_access_mutex);
		}
}



bool MotorController::ReadEncoder(int *e1)

{
	bool rtn = false;
	unsigned char buffer[4];

#ifdef LOG_EXCESS_WR_TIME
	struct timeval tv;
	unsigned long long initial;
	int dif;
	char buf[50];
#endif

	if (fd >= 0)
		{

#ifdef LOG_EXCESS_WR_TIME
		gettimeofday(&tv, NULL);
		initial = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
#endif

		pthread_mutex_lock(&serial_access_mutex);
		lock_holder = 13;
		tcflush(fd, TCIFLUSH);
		write(fd, get_encoder1, 2);
		rtn = ReadBytes(buffer,4);
		pthread_mutex_unlock(&serial_access_mutex);

#ifdef LOG_EXCESS_WR_TIME
		gettimeofday(&tv, NULL);
		dif = ((tv.tv_sec * 1000) + (tv.tv_usec / 1000)) - initial;
		if (dif > EXCESS_TIME)
			{
			sprintf(buf, "ReadEncoder write/read time: %d ms", dif);
			app_log.LogEntry(buf);
			}
#endif

		if (rtn)
			{
			*e1 = buffer[0] << 24;
			*e1 += buffer[1] << 16;
			*e1 += buffer[2] << 8;
			*e1 += buffer[3];
			}
		}
	return(rtn);
}



int MotorController::GetError()
{
	unsigned char err_code[] = {(char) -1};

#ifdef LOG_EXCESS_WR_TIME
	struct timeval tv;
	unsigned long long initial;
	int dif;
	char buffer[50];
#endif

	if (fd >= 0)
		{

#ifdef LOG_EXCESS_WR_TIME
		gettimeofday(&tv, NULL);
		initial = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
#endif

		pthread_mutex_lock(&serial_access_mutex);
		lock_holder = 14;
		tcflush(fd, TCIFLUSH);
		write(fd, get_error, 2);
		ReadBytes(err_code,1);
		pthread_mutex_unlock(&serial_access_mutex);

#ifdef LOG_EXCESS_WR_TIME
		gettimeofday(&tv, NULL);
		dif = ((tv.tv_sec * 1000) + (tv.tv_usec / 1000)) - initial;
		if (dif > EXCESS_TIME)
			{
			sprintf(buffer, "GetError write/read time: %d ms",dif);
			app_log.LogEntry(buffer);
			}
#endif

		}
	return(err_code[0]);
}


void *MotorController::SerialTestThread(void *)

{
	struct timeval tv;
	unsigned long long msec,start;

	pthread_detach(pthread_self());
	while (run_test)
		{
		if (samples < DATA_CAPTURE)
			{
			gettimeofday(&tv, NULL);
			msec = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
			if (samples == 0)
				{
				start = msec;
				ts[0] = 0;
				}
			else
				ts[samples] = msec - start;
			samples += 1;
			mc.ChngSpeed(128,128);
			if (run_test)
				usleep(90000);
			}
		else
			run_test = false;
		}
}



bool MotorController::StartSerialTest()

{
	pthread_t exec;
	bool rtn = false;

	run_test = true;
	samples = 0;
	SetMode(0);
	DisableRegulator();
	DisableTimeout();
	if (pthread_create(&exec, NULL, SerialTestThread, 0) == 0)
		rtn = true;
	return(rtn);
}



void MotorController::StopSerialTest()

{
	run_test = false;
}



void MotorController::RecordSerialTest()

{
	Log sd_log;
	time_t tt;
	tm* tmp;
	char buffer[200];
	char note[30];
	int i;

	time(&tt);
	sprintf(buffer, "%s%sSerialTestData%ld.csv", BASE_DIR, DATA_DIR, tt);
	if (sd_log.OpenLog(buffer, (char *)  "Motor controller serial test data log", false))
		{
		strcpy(last_move_file, buffer);
		tmp = localtime(&tt);
		sprintf(buffer, "%d/%d/%d  %d:%d:%d", tmp->tm_mon + 1, tmp->tm_mday, tmp->tm_year + 1900, tmp->tm_hour, tmp->tm_min, tmp->tm_sec);
		sd_log.LogEntry(buffer);
		sd_log.LogEntry((char *) "");
		sd_log.LogEntry((char *) "Timestamp (msec)");
		for (i = 0; i < samples; i++)
			{
			sprintf(buffer, "%ld", ts[i]);
			sd_log.LogEntry(buffer);
			}
		sd_log.CloseLog((char *) "");
		}
}



bool MotorController::Open()

{
	struct termios newtio;
	int no;
	char buff[1];
	bool rtn = false;

	fd = open(TTYDEVICE, O_RDWR | O_NOCTTY | O_NONBLOCK);
	if (fd >= 0)
		{
		bzero(&newtio, sizeof(newtio));
		newtio.c_cflag = B38400 | CS8 | CLOCAL | CREAD;
		newtio.c_iflag = IGNPAR;
		newtio.c_oflag = 0;
		newtio.c_lflag = 0;
		newtio.c_cc[VTIME] = 0;
		newtio.c_cc[VMIN] = 1;
		tcflush(fd, TCIFLUSH);
		tcsetattr(fd, TCSANOW, &newtio);
		lock_holder = 0;
		rtn = true;
		}
	return(rtn);
}



void MotorController::Close()

{
	if (fd > 0)
		{
		close(fd);
		fd = -1;
		}
}

