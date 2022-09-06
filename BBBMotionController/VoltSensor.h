#ifndef VOLT_SENSOR
#define VOLT_SENSOR

class VoltSensor
	{
	private:

	int volt_line_fd;
	static VoltSensor* sensor;

	VoltSensor();
	VoltSensor(VoltSensor const&){};
	VoltSensor& operator=(VoltSensor const&){};

	public:

	static VoltSensor* Instance();
	bool Init();
	void Close();
	double GetVolts();
	};


#endif