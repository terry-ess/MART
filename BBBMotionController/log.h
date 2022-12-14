/*	LOG FILE DEFINTION */
/*	Filename: LOG.CPP
	Date: 9/14/01
	Copyright 2001 by T. H. E. SOLUTION LLC
	Author: T. H. Ess
	Description:
	Provides the impelementation of an event log file.  This implementation is safe
	for multi-threaded environments.
*/

#ifndef LOG_H
#define LOG_H

/* GLOBAL DEFINTIONS */

#include <stdio.h>
#include <pthread.h>

#define __null 0


/* CLASS DEFINTION */

class Log
	{
	private:

	// DATA STRUCTURES

	FILE *log_file;
	pthread_mutex_t log_access;
	bool tstamp;
	unsigned long long stime;


	// FUNCTIONS

	void TimeStamp(char *);

	public:

	Log();
	bool OpenLog(char *,char *,bool);
	bool LogOpen();
	bool CloseLog(char *);
	bool LogEntry(char *);
	};

#endif