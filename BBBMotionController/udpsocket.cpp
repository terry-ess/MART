/*	UDP SOCKET LEVEL LINK CLASS IMPLEMENTATION */
/*	Filename: udpsocket.CPP
	Date: 9/20/07
	Copyright 2007 by T. H. E. Solution LLC
	Author: T. H. Ess
	Description:
	Provides the implementaiton of the UDP/IP (IPv4) socket based link level class.

	Changes:
		9/20/07	modify for use in general purpose CommStack
		5/25/05	original version
*/

/* DEFINITIONS */

#include "udpsocket.h"
#include "SharedData.h"


/* PRIVATE FUNCTIONS */

/* PUBLIC FUNCTIONS */

/*	CLASS CONSTRUCTOR
	Description:
	Provides class constructor.
	
	Passed parameters:	none
	
	Returned value:	none
*/

udpsocket::udpsocket()

{
	sock = INVALID_SOCKET;
	last_error = 0;
}



/*	CLASS DESTRUCTOR
	Description:
	Implements the class destructor.
	
	Passed parameters:	none
	
	Returned value:	none
*/

udpsocket::~udpsocket()

{
	Close();
}



/*	OPEN THE LINK
	Description:
	Opens the indicated port.
	
	Passed parameters:	ip_address - IP address string
						port - UDP port number
						broadcast - support broadcast (true) or not (false)
						
	Returned value:	sucessful (TRUE) or not (FALSE)
*/

bool udpsocket::Open(const char *ip_address,int port,bool broadcast)

{
	bool rtn = false;
	int error;
	struct sockaddr_in me;
	int optval = 1;
	
	if (sock == INVALID_SOCKET)
		{
		if ((sock = socket(AF_INET,SOCK_DGRAM,IPPROTO_UDP)) != INVALID_SOCKET )
			{
			error = setsockopt(sock,SOL_IP,IP_FREEBIND,&optval,sizeof(optval));
			me.sin_family = AF_INET;
			me.sin_addr.s_addr = inet_addr(ip_address);
			me.sin_port = htons(port);
			if (bind(sock,(struct sockaddr *) &me,sizeof(me)) == 0)
				{
				rtn = true;
				if (broadcast)
					{
					optval = 1;
					setsockopt(sock, SOL_SOCKET, SO_BROADCAST, (char*)&optval,sizeof(optval));
					}
				else
					{
					optval = 0;
					setsockopt(sock, SOL_SOCKET, SO_BROADCAST, (char*)&optval,sizeof(optval));
					}
				}
			else
				{
				last_error = WSAGetLastError();
				Close();
				}
			}
		else
			{
			error = WSAGetLastError();
			}
		}
	return(rtn);
}



/*	CLOSE THE LINK
	Description:
	Provides a means to close the link without destroying the object.

	Passed parameters:	none
	
	Returned value: none
*/

void udpsocket::Close(void)

{
	int err_code;
	
	if (sock != INVALID_SOCKET)
		{
		CLOSESOCKET(sock);
		while (getsockopt(sock,SOL_SOCKET,SO_ERROR,(char *) &err_code,NULL) == 0)
			SSLEEP(10);
		sock = INVALID_SOCKET;
		}
}



/* RECIEVE A DATAGRAM
	Description:
	Receive's an UDP datagram.

	Passed parameters:	data - buffer for datagram
								from - pointer to socket address struct
	
	Returned value:	length of received datagram
*/

int udpsocket::RecvPacket(char *data,int len,sockaddr *from)

{
	int stat = SOCKET_ERROR;
	socklen_t flen;
	
	if (sock != INVALID_SOCKET)
		{
		flen = sizeof(sockaddr);
		stat = recvfrom(sock,data,len,0,from,&flen);
		if (stat == 0)
			stat = SOCKET_ERROR;
		}
	return(stat);
}



/*	SEND A DATAGRAM
	Description:
	Send an UDP datagram.

	Passed parameters:	data - buffer to be sent
						len - length of buffer
						to - socket address to send to
	
	Returned value:	successful (true) or not (false)
*/

bool udpsocket::SendPacket(char *data,int len,sockaddr *to)

{
	bool rtn = false;
	int stat;
	
	if (sock != INVALID_SOCKET)
		{
		stat = sendto(sock,data,len,0,to,sizeof(sockaddr));
		if (stat == len)
			rtn = true;
		}
	return(rtn);
}



bool udpsocket::SendFile(char *fname,sockaddr *to)

{
	char buffer[512];
	FILE *pfile = NULL;
	char *pos;
	struct stat mstat;
	int rd;

	if ((pfile = fopen(fname, "r")) != NULL)
		{
		pos = strrchr(fname, '/');
		if (pos != NULL)
			{
			if ((stat(fname, &mstat) == 0) && (mstat.st_size > 0))
				{
				sprintf(buffer, "ok %s %d", pos + 1, mstat.st_size);
				SendPacket(buffer, strlen(buffer), to);
				while ((rd = fread(buffer,1,sizeof(buffer),pfile)) > 0)
					{
					SendPacket(buffer,rd,to);
					}
				}
			else
				{
				strcpy(buffer, "fail");
				SendPacket(buffer, strlen(buffer), to);
				}
			}
		else
			{
			strcpy(buffer, "fail");
			SendPacket(buffer, strlen(buffer), to);
			}
		}
	else
		{
		strcpy(buffer,"fail");
		SendPacket(buffer, strlen(buffer), to);
		}
}



/*	GET LAST ERROR
	Description.
	Returns the last error code for this UDP socket.
	
	Passed parameter:	none

	Returned value:	last error code (0 = no error)
*/

int udpsocket::GetLastError()

{
	return(last_error);
}
