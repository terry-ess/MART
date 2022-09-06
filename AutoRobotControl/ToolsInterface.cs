using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRobotControl
	{
	public interface ToolsInterface
		{
		bool Open(params object[] obj);
		void Close();
		}
	}
