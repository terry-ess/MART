#ifndef GPIO_H
#define GPIO_H


class Gpio
{
private:

	static Gpio* gpio;
	int runledfd;

	
	Gpio();
	Gpio(Gpio const&){};
	Gpio& operator=(Gpio const&){};
	bool Expose(int io_no);
	bool InitIO(int io_no, bool in, int *bfd);
	void CloseIO(int,int *);

public:

	static Gpio* Instance();
	bool Init();
	void Close();
	void RunLed(bool on);
	bool Docked();

};

#endif