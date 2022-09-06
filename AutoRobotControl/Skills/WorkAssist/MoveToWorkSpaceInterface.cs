using System;
using System.Collections;

namespace Work_Assist
	{
	interface MoveToWorkSpaceInterface
		{

		bool Move(ref ArrayList path, ref Stack rtn_path, ref Stack final_adjust);

		}
	}
