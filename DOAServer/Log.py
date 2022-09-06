import io
import datetime

class Log:

	logfile = None
	name = None


	def Open(self):

		result = False
		if self.logfile:
			self.logfile.write("{0}\r\n".format(self.name))
			n = datetime.datetime.now()
			self.WriteLine("{0}/{1}/{2} {3}:{4}".format(n.month,n.day,n.year,n.hour,n.minute))
			result = True
		return result		



	def Close(self):

		if self.logfile:
			self.logfile.close()



	def WriteLine(self,line):
		if self.logfile:
			line = "".join((line,"\r\n"))
			self.logfile.write(line)



	def __init__(self,name):
		self.name = name
		name = "".join((name,".log"))
		self.logfile = open(name,"w")
