/*	UDP SOCKET LEVEL CLASS DEFINITION */
/*	Filename: udpsocket.H
	Date: 8/12/07
	Copyright 2005 by T. H. E. Solution LLC
	Author: T. H. Ess
	Description:
	Defines the UDP/IP socket class.

	Changes:
		8/12/07	modify for use in general purpose CommStack
		5/24/05	original version
*/

#ifndef UDPSOCKET_H
#define UDPSOCKET_H

#include <errno.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <netinet/tcp.h>
#include <arpa/inet.h>
#include <netdb.h>
#include <unistd.h>
#include <sys/stat.h>
#include <string.h>
#include <ctype.h>
#include <stdlib.h>
#include <pthread.h>
#include <stdio.h>

#define SOCKET int
#define INVALID_SOCKET -1
#define SOCKET_ERROR -1
#define WSAGetLastError() errno 
#define CLOSESOCKET(x) shutdown(x,0);close(x)
#define SSLEEP(x) {struct timespec ts;ts.tv_sec=x/1000;ts.tv_nsec=(x%1000)*1000;nanosleep(&ts,&ts);}


/* CLASS DEFINITION */

class udpsocket
	{
	private:
	
	/* DATA STRUCTURES */

	SOCKET sock;
	int last_error;

	
	/* FUNCTIONS */

	public:
	
	udpsocket();
	~udpsocket();
	bool Open(const char *ip_address,int port,bool broadcast);
	void Close(void);
	int RecvPacket(char *,int,sockaddr *);
	bool SendPacket(char *,int,sockaddr *);
	bool SendFile(char *,sockaddr *);
	int GetLastError();
	};

#endif
