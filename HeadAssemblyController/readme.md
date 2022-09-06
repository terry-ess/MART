# Head Assembly Controller
The controller is a Arduino  C application that implements a simple serial over USB connection to provide control of the KINECT's servos and access to the magnetic compass and ambient light sensor.  

A serial command is composed of ASCII text ended with carriage return. Supported commands:

| Command | Description |
|---------|---------|
| H | hello |
| C | clear servo error |
| T | servo torque on |
| B | servo break on |
| S | read servo status |
| R | read compass heading |
| L | read light sensor |
| position,id,move time *| position identified servo |

* text encoded integers