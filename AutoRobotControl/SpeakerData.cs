using System;
using System.Drawing;

namespace AutoRobotControl
	{
	public static class SpeakerData
		{
			private const int FACE_DATA_TO = 60000;
			private const int NO_MOVE_DIST = 10;
		
			private static PersonDetect.scan_data person = PersonDetect.Empty();
			private static PersonDetect.scan_data face = PersonDetect.Empty();
			private static bool tight_quarters = false;

			private static Point prior_person_rmloc = new Point(0,0);
			private static Object pobj = new Object(),fobj = new Object(),tqobj = new Object();


			public static PersonDetect.scan_data Person

			{
				get
				{
					return(person);
				}

				set
				{
					lock(pobj)
					{
					prior_person_rmloc = person.rm_location;
					person = value;
					}
				}
			}



		public static PersonDetect.scan_data Face

		{
		get
			{
			return (face);
			}

		set
			{
			lock (fobj)
				{
				face = value;
				}
			}
		}


		public static bool FaceClear()

		{
			bool rtn = false;

			if (!person.detected)
				{
				face = PersonDetect.Empty();
				rtn = true;
				}
			else if ((face.detected) && ((NavCompute.DistancePtToPt(prior_person_rmloc, person.rm_location) > NO_MOVE_DIST) || (SharedData.app_time.ElapsedMilliseconds - face.ts > FACE_DATA_TO)))
				{
				face = PersonDetect.Empty();
				rtn = true;
				}
			return(rtn);
		}



		public static void ClearPersonFace()

		{
			person = PersonDetect.Empty();
			face = PersonDetect.Empty();
		}


		public static bool TightQuaters

		{

			get 
			{
				return(tight_quarters);
			}

			set
			{
				lock(tqobj)
				{
				tight_quarters = value;
				}
			}
		}

		}
	}
