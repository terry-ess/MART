using System;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace RemoteUserInterface
	{
	class Recorder
		{
		private TextWriter ltw = null;
		private Stopwatch sw = new Stopwatch();
		private string log_dir = "";
		private bool ts;
		private object lock_obj = new object();


		public void OpenSession(string file,bool timestamps)

		{
			if (ltw == null)
				{
				ts = timestamps;
				ltw = File.CreateText(log_dir + file);
				if (ltw != null)
					{
					ltw.WriteLine(file);
					ltw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() );
					ltw.WriteLine();
					ltw.Flush();
					sw.Restart();
					}
				}
		}



		public void CloseSession()

		{
			lock(lock_obj)
			{
			if ((ltw != null))
				{
				ltw.Close();
				sw.Stop();
				ltw = null;
				}
			}
		}



		public  void Entry(string entry)

		{
			lock(lock_obj)
			{
			if (ltw != null)
				{
				if (ts)
					ltw.Write(sw.ElapsedMilliseconds.ToString() + " ");
				ltw.WriteLine(entry);
				ltw.Flush();
				}
			}
		}



		public bool IsOpen()

		{
			bool rtn = false;

			if (ltw != null)
				rtn = true;
			return(rtn);
		}

		}
	}
