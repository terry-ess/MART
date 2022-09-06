#ifndef MOTION_CONTROLLER
#define MOTION_CONTROLLER

#include "log.h"
#include "Mpu6050.h"
#include "MotorController.h"
#include "EZSSonar.h"
#include "Gpio.h"
#include "VoltSensor.h"


extern Mpu6050* sensor;
extern Log app_log;
extern MotorController mc;
extern EzSSonar *sonar;
extern Gpio *io;
extern VoltSensor *vs;
extern char last_move_file[200];
extern char last_turn_file[200];
extern char last_rcd_file[200];
extern char last_sonar_file[200];

#endif