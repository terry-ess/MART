#ifndef SHARED_DATA
#define SHARED_DATA

#define __null 0

#define BASE_DIR "/app"
#define CAL_DIR "/cal/"
#define DATA_DIR "/data/"

#define G_FT_SEC2 32.174

#define MIN_REAR_CLEARANCE 15
#define MIN_FRONT_CLEARANCE 19

#define GPIO_EXPORT "/sys/class/gpio/export"
#define GPIO_UNEXPORT "/sys/class/gpio/unexport"

#define INSUFFICENT_REAR_CLEARANCE  "Insufficient rear clearance."
#define INSUFFICENT_FRONT_CLEARANCE  "Insufficient front clearance."

#define MPU_FAIL "MPU6050 connection lost"
#define START_TIMEOUT "start timedout"
#define STOP_TIMEOUT "stop timedout"
#define EXCESSIVE_GYRO_CORRECT "excessive gyro correct"


#endif