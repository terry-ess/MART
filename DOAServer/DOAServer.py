print("Loading libraries")

import usb.core
import usb.util
from tuning import Tuning
import time
import multiprocessing as mp
import Log
import socket
import datetime

END_VOICE = 35
DOA_CHUNKS = 10
STARTED = 'started'
EXIT = 'exit'
DIRECTION = 'direct'
FAILED = 'fail'
SAMPLE_TIME = .02
#HOST = '192.168.1.101'
HOST = '192.168.2.6'
PORT = 60000


def DOAProcess(cc):

	print("Opening USB connection with mic array")
	dev = usb.core.find(idVendor=0x2886, idProduct=0x0018)
	if dev:
		Mic_tuning = Tuning(dev)
		in_speech = False
		no_speech = 0
		not_vchunk = 0
		no_vchunk = 0
		doa_set = False
		direction = -1
		print("Running DOA process")
		cc.send(STARTED)
		while True:
			start = time.time()
			if (not in_speech):
				if (Mic_tuning.is_voice()):
					in_speech = True
					not_vchunk = 0
					no_vchunk = 0
					no_speech = 1
					doa_set = False
			else:
				if (not Mic_tuning.is_voice()):
					not_vchunk += 1
					if (not_vchunk == END_VOICE):
						in_speech = False
						no_vchunk = 0
						not_vchunk = 0
						no_speech = 0
				else:
					not_vchunk = 0
					no_speech += 1
			if (in_speech):
				no_vchunk += 1
				if (not doa_set and no_vchunk >= DOA_CHUNKS and no_speech >= no_vchunk/2):
					direction = Mic_tuning.direction
					print(direction)
					doa_set = True
			if cc.poll() > 0:
				cmd = cc.recv()
				if (cmd == EXIT):
					print("DOA process shutting down");
					break;
				elif (cmd == DIRECTION):
					cc.send(int(round(direction)))
			end = time.time()
			if (SAMPLE_TIME > end - start):
				time.sleep(SAMPLE_TIME - (end - start))
	else:
		print("Could not connect to mic array")
		cc.send(FAILED)



def main():

	n = datetime.datetime.now()
	lf = Log.Log("DOA server {0}.{1}.{2} {3}.{4}".format(n.month,n.day,n.year,n.hour,n.minute))
	lf.Open()
	print("Opening UDP socket")
	try:
		sock = socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
		sock.bind((HOST,PORT))
	except:
		print("Could not open UDP socket")
		lf.WriteLine("Could not open UDP socket")
		return(1)
	print("Starting DOA process")
	pc,cc = mp.Pipe()
	p = mp.Process(target=DOAProcess,args=(cc,))
	p.start()
	rsp = pc.recv()
	if (rsp == STARTED):
		print("Starting server loop")
		try:
			while True:
				try:
					data,conn = sock.recvfrom(1024)
				except KeyboardInterrupt:
					lf.WriteLine("user break")
					break
				except:
					lf.WriteLine("Socket recvfrom exception {0}".format(err.errno))
					break
				if (len(data) > 0):
					s = bytes.decode(data)
					lf.WriteLine(s)
					if s == "exit":
						break
					elif (s== "hello"):
						sock.sendto(bytes("OK","ascii"),conn)
						lf.WriteLine("OK")
					elif (s== DIRECTION):
						pc.send(DIRECTION)
						direct = pc.recv()
						sock.sendto(bytes("OK {}".format(direct),"ascii"),conn)
						lf.WriteLine("OK {}".format(direct))					
					else:
						sock.sendto(bytes("FAIL request not supported","ascii"),conn)
						lf.WriteLine("FAIL request not supported")
				else:
					lf.WriteLine("no data receiption")
					break

		except KeyboardInterrupt:
			lf.WriteLine("user break")

	pc.send('exit')
	p.join()
	lf.Close()



if __name__ == '__main__':
    main()

