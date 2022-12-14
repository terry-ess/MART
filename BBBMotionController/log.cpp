/*	LOG FILE IMPLEMENTATION */
/*	Filename: LOG.CPP
	Author: T. H. Ess
*/

/* GLOBAL DEFINTIONS */

#include <stdlib.h>
#include <pthread.h>
#include <time.h>
#include <sys/time.h>



/* CLASS DEFINITION */

#include "log.h"

#define __null 0



/* PRIVATE FUNCTIONS */

Log::Log()

{
	log_file = NULL;
	log_access = PTHREAD_MUTEX_INITIALIZER;
	tstamp = true;
}



/*	DATE-TIME STAMP
	Description:
	Places the current date - time stamp in the indicated string.

	Passed parameter:	line - pointer to string

	Returned value:	none
*/

void Log::TimeStamp(char *line)

{
	unsigned long long msec;
	struct timeval tv;

	gettimeofday(&tv, NULL);
	msec = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
	sprintf(line,"%ld ",msec - stime);
}



/* PUBLIC FUNCTIONS */

/*	OPEN LOG FILE
	Description:
	Takes care of initializing the log file.  This includes creating the file and adding
	the first line.

	Passed parameter:	fname - full path and file name for the log
						entry - opening entry;

	Returned value:	log file initialized (TRUE) or not (FALSE)
*/

bool Log::OpenLog(char *fname,char *entry,bool timestamp)

{
	char time_line[22];
	bool rtn = false;
	struct timeval tv;

	if ((log_file = fopen(fname,"w")) != NULL)
		{
		pthread_mutex_lock(&log_access);
		tstamp = timestamp;
		if (tstamp)
			{
			gettimeofday(&tv, NULL);
			stime = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
			TimeStamp(time_line);
			fprintf(log_file,time_line);
			}
		fprintf(log_file,"%s\n",entry);
		fflush(log_file);
		pthread_mutex_unlock(&log_access);
		rtn = true;
		}
	return(rtn);
}



bool Log::LogOpen()

{
	bool rtn;

	if (log_file == NULL)
		rtn = false;
	else
		rtn = true;
	return(rtn);
}


/*	CLOSE LOG FILE
	Description:
	Takes care of closing the log file.  This includes adding
	the last line.

	Passed parameter:	entry - opening entry;

	Returned value:	log file closed (TRUE) or not (FALSE)
*/

bool Log::CloseLog(char *entry)

{
	char time_line[22];
	bool rtn = false;

	if (log_file != NULL)
		{
		pthread_mutex_lock(&log_access);
		if (tstamp)
			{
			TimeStamp(time_line);
			fprintf(log_file,time_line);
			}
		fprintf(log_file,"%s\n",entry);
		fclose(log_file);
		log_file = NULL;
		pthread_mutex_unlock(&log_access);
		rtn = true;
		}
	return(rtn);
}



/*	MAKE A LOG ENTRY */
/*	Description:
	Attempts to make a log entry.  Since the log file is a shared resource and could
	be open by another "operation" to make an entry,  multiple attempts are made to
	open the log.

	Passed parameter:	entry - entry to append to log

	Returned value:	entry made (TRUE) or not (FALSE)
*/

bool Log::LogEntry(char *entry)

{
	bool rtn = false;
	char time_line[22];

	if (log_file != NULL)
		{
		pthread_mutex_lock(&log_access);
		if (tstamp)
			{
			TimeStamp(time_line);
			fprintf(log_file,time_line);
			}
		fprintf(log_file,"%s\n",entry);
		fflush(log_file);
		pthread_mutex_unlock(&log_access);
		rtn = true;
		}
	return(rtn);
}
