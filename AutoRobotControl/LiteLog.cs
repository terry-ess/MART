using System;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace AutoRobotControl
	{
	class LiteLog
		{

		private TextWriter ltw = null;
		private Stopwatch sw = new Stopwatch();
		private object access = new object();


		public LiteLog(string file)

		{
			string log_dir;

			if (ltw == null)
				{
				log_dir = Log.LogDir();
				if (!Directory.Exists(log_dir))
					Directory.CreateDirectory(log_dir);
				ltw = File.CreateText(log_dir + file);
				if (ltw != null)
					{
					ltw.WriteLine(file);
					ltw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
					ltw.WriteLine();
					ltw.Flush();
					sw.Restart();
					}
				}
		}



		public void Close()

		{
			if (ltw != null)
				{
				ltw.Close();
				sw.Stop();
				ltw = null;
				}
		}



		public void LogEntry(string entry)

		{
			if (ltw != null)
				{

				lock(access)
				{
				ltw.Write(sw.ElapsedMilliseconds.ToString() + " ");
				ltw.WriteLine(entry);
				ltw.Flush();
				}

				}
		}


		}
	}
