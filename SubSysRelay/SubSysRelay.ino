
#define SS_RELAY 3
#define RC_RELAY 4
#define HA_RELAY 5
#define AS_RELAY 6
#define BAUD_RATE 115200


void setup()

{
	Serial.begin(BAUD_RATE);
	pinMode(SS_RELAY,OUTPUT);
	pinMode(RC_RELAY, OUTPUT);
	pinMode(HA_RELAY, OUTPUT);
	pinMode(AS_RELAY, OUTPUT);
	}



void loop()

{
	int pos;
	boolean eol = false;
	char input[7];
	char output[20];

	pos = 0;
	while (!eol)
		{
		if (Serial.available() > 0)
			{
			pos = Serial.readBytesUntil('\r', input, sizeof(input) - 1);
			input[pos] = 0;
			eol = true;
			}
		else
			delay(10);
		}
	if (input[0] == 'H')
		{
		strcpy(output,"ok");
		}
	else if (strcmp(input,"ON") == 0)
		{
		digitalWrite(SS_RELAY,HIGH);
		digitalWrite(RC_RELAY, HIGH);
		digitalWrite(HA_RELAY, HIGH);
		digitalWrite(AS_RELAY, HIGH);
		strcpy(output, "ok");
		}
	else if (strcmp(input, "ON,SS") == 0)
		{
		digitalWrite(SS_RELAY, HIGH);
		strcpy(output, "ok");
		}
	else if (strcmp(input, "ON,RC") == 0)
		{
		digitalWrite(RC_RELAY, HIGH);
		strcpy(output, "ok");
		}
	else if (strcmp(input, "ON,HA") == 0)
		{
		digitalWrite(HA_RELAY, HIGH);
		strcpy(output, "ok");
		}
	else if (strcmp(input, "ON,AS") == 0)
		{
		digitalWrite(AS_RELAY, HIGH);
		strcpy(output, "ok");
		}
	else if (strcmp(input, "OFF") == 0)
		{
		digitalWrite(SS_RELAY,LOW);
		digitalWrite(RC_RELAY, LOW);
		digitalWrite(HA_RELAY, LOW);
		digitalWrite(AS_RELAY, LOW);
		strcpy(output, "ok");
		}
	else if (strcmp(input, "OFF,SS") == 0)
		{
		digitalWrite(SS_RELAY, LOW);
		strcpy(output, "ok");
		}
	else if (strcmp(input, "OFF,RC") == 0)
		{
		digitalWrite(RC_RELAY, LOW);
		strcpy(output, "ok");
		}
	else if (strcmp(input, "OFF,HA") == 0)
		{
		digitalWrite(HA_RELAY,LOW);
		strcpy(output, "ok");
		}
	else if (strcmp(input, "OFF,AS") == 0)
		{
		digitalWrite(AS_RELAY, LOW);
		strcpy(output, "ok");
		}
	else
		{
		sprintf(output, "fail nc %s", input);
		}
	Serial.println(output);
}



int Main()

{
	init();
	Serial.println("Ready");
	while (true)
		{
		loop();
		}
}