using System;
using System.Collections;
using System.Drawing;

namespace AutoRobotControl
	{
	public interface RoomInterface
		{
		bool Open();
		bool Open(NavData.connection connect);
		void OpenLimited(string rm_name);
		void Close();
		bool Leave(string tname,ref Point dif,ref int dist);
		bool GoToPoint(string pname);
		void Stop();
		bool InOpenArea(Point coord,int direction,ref ArrayList bad_pts);
		Point DetermineExitPt(NavData.connection connect,Point my_coord);
		bool GoToEntryPoint(NavData.connection connect);
		bool GoToExitPoint(NavData.connection connect, ref int dist);
		}
	}
