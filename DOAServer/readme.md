# Direction of Arrival Server

The server is a Python 3.8 application on the head assembly's Raspberry Pi that provides the capability to access the vocal direction of arrival from an attached ReSpeaker microphone array v 2.0.  It uses two process to perform this task.  The "main" process is a simple "UDP/IP" server accessed by the AutoRobotControl application over Ethernet.  The "background process" uses the USB connection with the microphone array to determine when a voice is active and its direction of arrival.  The [tunning.py file from Respeaker](https://github.com/respeaker/usb_4_mic_array) provides the USB control transfer based interface with the microphone array.  The two processes use a python pipe to communicate.

Supported commands:

- Are you there? - hello
- Shutdown - exit
- Direction of arrival - direct



