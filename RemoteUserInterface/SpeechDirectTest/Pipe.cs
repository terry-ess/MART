using System;
using System.Collections;
using System.Threading;

namespace SpeechDirectTest
	{
	public class Pipe
		{

		private Queue q;
		private Object qlock = new object();

		public void Open()

		{
			q = new Queue();
		}


		public void Close()

		{
		}


		public void Add(string msg)

		{
			lock(qlock)
			{
			q.Enqueue(msg);
			}
		}



		public string Remove()

		{
			string stg = "";

			if (q.Count > 0)
				stg = (string) q.Dequeue();
			return(stg);
		}

		}
	}
