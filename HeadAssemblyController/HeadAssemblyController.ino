#include <Wire.h>
#include <LSM303.h>


#define ERROR_LED 2
#define RUN_LED 3
#define LA_POWER 12
#define BAUD_RATE 115200
#define MAX_DELAY_COUNT 100
#define LIGHT_SENSOR 0		//TEMT6000 ambient light sensor


LSM303 compass;


byte torque_on_cmd[] = { 0xff, 0xff, 0x0a,0x00, 0x03, 0x00, 0x00,0x34,0x01,0x60 };
byte break_on_cmd[] = { 0xff, 0xff, 0x0a,0x00, 0x03, 0x00, 0x00,0x34,0x01,0x40 };
byte move_cmd[] = {0xff,0xff,0x0c,0x00,0x05,0x00,0x00,0x00,0x00,0x00,0x00,0x5A};
byte clear_err_cmd[] = { 0xff, 0xff, 0x0a, 0x00, 0x03, 0x00, 0x00, 0x30, 0x01, 0x00 };
byte leds_off_cmd[] = { 0xff, 0xff, 0x0a, 0x00, 0x03, 0x00, 0x00, 0x35, 0x01, 0x00 };
byte stat_cmd[] = {0xff,0xff,0x07,0x00,0x07,0x00,0x00};
boolean torque_on[] = {false,false};  //supports 2 servos with ids of 0 and 1
int stat_error,stat_detail;
char error[100];


void ErrorDetected()
{
	cli();
	digitalWrite(RUN_LED,LOW);
	while(true)
		{
		digitalWrite(ERROR_LED,HIGH);
		delay(1000);
		digitalWrite(ERROR_LED,LOW);
		delay(1000);
		}
}



void setup()

{
	int avail;
	char buf[100];

	Serial.begin(BAUD_RATE);
	pinMode(ERROR_LED,OUTPUT);
	pinMode(RUN_LED,OUTPUT);
	pinMode(13,OUTPUT);
	digitalWrite(13,HIGH);
	pinMode(LA_POWER, OUTPUT);
	digitalWrite(LA_POWER, HIGH);
	Serial1.begin(BAUD_RATE);
	Wire.begin();
	compass.init(LSM303DLM_DEVICE,LSM303_SA0_A_LOW);
	compass.enableDefault();
}



boolean Status(byte id)

{
	int i,avail,pos = 0,delay_count = 0;
	unsigned int db1,db2;
	char input[50];
	char err[4];
	boolean rtn = false;
  
	while ((avail = Serial1.available()) > 0)
		{
		if (avail > sizeof(input))
			avail = sizeof(input);
		Serial1.readBytes(input,avail);
		}
	memset(input,0,sizeof(input));
	stat_cmd[3] = id;
	stat_cmd[5] = (byte) ((stat_cmd[0] ^ stat_cmd[1] ^ stat_cmd[2] ^ stat_cmd[3] ^ stat_cmd[4]) & 0xFE);
	stat_cmd[6] = (byte)(~stat_cmd[5] & 0xFE);
	Serial1.write(stat_cmd,sizeof(stat_cmd));
	while ((pos < 9) && (delay_count < MAX_DELAY_COUNT))
		{
		if ((avail = Serial1.available()) > 0)
			{
			for (i = 0;i < avail;i++)
				{
				input[pos] = Serial1.read();
				if ((pos == 0) && (input[pos] != -1))
					pos = 0;
				else if ((pos == 1) && (input[pos] != -1))
					pos = 0;
				else if ((pos == 2) && (input[pos] != 9))
					pos = 0;
				else if ((pos == 3) && (input[pos] != id))
					pos = 0;
				else if ((pos == 4) && (input[pos] != 0x47))
					pos = 0;
				else
					pos += 1;
				delay_count = 0;
				}
			}
		else
			{
			delay(10);
			delay_count += 1;
			}
		}
	if ((pos == 9) && (input[4] == 0x47))
		{
		stat_error = input[7];
		stat_detail = input[8];
		rtn = true;
		}
	else if ((pos == 16) && (input[11] == 0x47))
		{
		stat_error = input[14];
		stat_detail = input[15];
		rtn = true;
		}
	else
		{
		stat_error = pos;
		stat_detail = delay_count;
/*		Serial.print("fail,");
		for (i = 0; i < pos; i++)
			{
			sprintf(err,"%x",input[i]);
			Serial.print(err);
			Serial.print(",");
			} */
		}
	return(rtn);
}



void ClearError(byte id)

{
	clear_err_cmd[3] = id;
	clear_err_cmd[5] = (byte) ((clear_err_cmd[0] ^ clear_err_cmd[1] ^ clear_err_cmd[2] ^ clear_err_cmd[3] ^ clear_err_cmd[4] ^ clear_err_cmd[7] ^ clear_err_cmd[8] ^ clear_err_cmd[9]) & 0xFE);
	clear_err_cmd[6] = (byte)(~clear_err_cmd[5] & 0xFE);
	Serial1.write(clear_err_cmd,sizeof(clear_err_cmd));
	leds_off_cmd[3] = id;
	leds_off_cmd[5] = (byte) ((leds_off_cmd[0] ^ leds_off_cmd[1] ^ leds_off_cmd[2] ^ leds_off_cmd[3] ^ leds_off_cmd[4] ^ leds_off_cmd[7] ^ leds_off_cmd[8] ^ leds_off_cmd[9]) & 0xFE);
	leds_off_cmd[6] = (byte)(~leds_off_cmd[5] & 0xFE);
	Serial1.write(leds_off_cmd,sizeof(leds_off_cmd));
}



void TorqueOn(byte id)

{
    torque_on_cmd[3] = (byte) id;
    torque_on_cmd[5] = (byte) ((torque_on_cmd[0] ^ torque_on_cmd[1] ^ torque_on_cmd[2] ^ torque_on_cmd[3] ^ torque_on_cmd[4] ^ torque_on_cmd[7] ^ torque_on_cmd[8] ^ torque_on_cmd[9]) & 0xFE);
    torque_on_cmd[6] = (byte)(~torque_on_cmd[5] & 0xFE);
    Serial1.write(torque_on_cmd,sizeof(torque_on_cmd));
    torque_on[id] = true;
}



void BreakOn(byte id)

{
    break_on_cmd[3] = (byte) id;
    break_on_cmd[5] = (byte) ((break_on_cmd[0] ^ break_on_cmd[1] ^ break_on_cmd[2] ^ break_on_cmd[3] ^ break_on_cmd[4] ^ break_on_cmd[7] ^ break_on_cmd[8] ^ break_on_cmd[9]) & 0xFE);
    break_on_cmd[6] = (byte)(~break_on_cmd[5] & 0xFE);
    Serial1.write(break_on_cmd,sizeof(break_on_cmd));
    torque_on[id] = false;
}



void MoveTo(byte id,int pos,int pt)

{
	if (!torque_on[id])
		TorqueOn(id);
	move_cmd[3] = (byte) id;
	move_cmd[10] = (byte) id;
	move_cmd[11] = (byte) pt;
	move_cmd[7] = (byte) (pos % 256);
	move_cmd[8] = (byte) (pos/256);
	move_cmd[5] = (byte) ((move_cmd[0] ^ move_cmd[1] ^ move_cmd[2] ^ move_cmd[3] ^ move_cmd[4] ^ move_cmd[7] ^ move_cmd[8] ^ move_cmd[9] ^ move_cmd[10] ^ move_cmd[11]) & 0xFE);
	move_cmd[6] = (byte) (~move_cmd[5] & 0xFE);
	Serial1.write(move_cmd,sizeof(move_cmd));
}



void loop()

{
	int avail,pos,id,i,mt;
	char input[15];
	char output[50];
	boolean eol = false;
	int heading,pvalue;
	char *ptr;

	pos = 0;
	id = 0;
	memset(input,0,sizeof(input));
	while (!eol)
		{
		if (Serial.available() > 0)
			{
			pos = Serial.readBytesUntil('\r', input, sizeof(input) -1);
			input[pos] = 0;
			if ((input[0] > 64) && (input[0] < 91))
				eol = true;
			else if ((input[0] > 47) && (input[0] < 58))
				eol = true;
			else
				pos = 0;
			}
		else
			delay(10);
		}
	if (!isdigit(input[0]))
		{
		if (input[0] == 'C')
			{
			ptr = strchr(input,',');
			if (ptr != NULL)
				{
				id = atoi(ptr + 1);
				ClearError(id);
				sprintf(output,"ok %s",input);
				}
			else
				sprintf(output,"fail %s",input);
			}
		else if (input[0] == 'T')
			{
			ptr = strchr(input,',');
			if (ptr != NULL)
				{
				id = atoi(ptr + 1);
				TorqueOn(id);
				sprintf(output,"ok %s",input);
				}
			else
				sprintf(output,"fail %s",input);
			} 
		else if (input[0] == 'B')
			{
			ptr = strchr(input,',');
			if (ptr != NULL)
				{
				id = atoi(ptr + 1);
				BreakOn(id);
				sprintf(output,"ok %s",input);
				}
			else
				sprintf(output,"fail %s",input);
			} 
		else if (input[0] == 'S')
			{
			ptr = strchr(input,',');
			if (ptr != NULL)
				{
				id = atoi(ptr + 1);
				if (Status(id))       
					sprintf(output,"ok %d %d",stat_error,stat_detail);
				else
					sprintf(output, "fail %d %d", stat_error, stat_detail);
//					output[0] = 0;
				}
			else
				sprintf(output,"fail %s",input);
			}
		else if (input[0] == 'R')
			{
			compass.read();
			heading = compass.heading();
			sprintf(output,"ok %d",heading);
			}
		else if (input[0] == 'H')
			{    
			strcpy(output,"ok ");
			}
		else if (input[0] == 'L')
			{
			pos = analogRead(LIGHT_SENSOR);
			sprintf(output,"ok %d",pos);
			}
		else
			{
			sprintf(output,"fail nc %s",input);
			}
		}
	else
		{
		pos = -1;
		mt = 0;
		pos = atoi(input);
		ptr = strchr(input,',');
		if (ptr != NULL)
			{
			id = atoi(ptr + 1);
			ptr = strchr(ptr + 1,',');
			if (ptr != NULL)
				mt = atoi(ptr + 1);
			}
		if ((pos >= 0) && (pos <=1023) && (mt >= 4))
			{
			MoveTo(id,pos,mt);
			sprintf(output,"ok %s",input);
			}
		else
			{
			sprintf(output,"fail %s",input);
			}
		}
	Serial.println(output);
}



int main(void)

{
	init();
	setup();
	digitalWrite(RUN_LED,HIGH);
//	Serial.println("Ready");
	while (true)
		{
		loop();
		}
	ErrorDetected();
}

