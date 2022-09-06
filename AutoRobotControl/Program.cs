using System;
using System.Threading;
using AutoRobotControl;

namespace AutoRobotControl
	{
	class Program
		{

		public static AutoResetEvent appl_exit = new AutoResetEvent(false);

		static void Main(string[] args)

		{
			Console.WriteLine("Autonomous robot starting.");
			Supervisor.Start();
			Console.WriteLine("Started.");
			appl_exit.WaitOne();
			Console.WriteLine("Shut down");
		}

		}
	}
