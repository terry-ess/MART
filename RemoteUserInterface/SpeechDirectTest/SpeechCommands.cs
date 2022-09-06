using System;
using System.Threading;
using AutoRobotControl;


namespace SpeechDirectTest
	{
	class SpeechCommands:CommandHandlerInterface
		{

		private const string GRAMMAR = "speechcommands";


		public SpeechCommands()

		{
			RegisterCommandSpeech();
		}



		public void SpeechHandler(string msg)

		{
			int pan = 0, diff, adj;
//			PersonDetect pd = new PersonDetect();
//			PersonDetect.person_data pdd = new PersonDetect.person_data(); 

			Form1.test_pipe.Add(msg + " " + Speech.speaker_direction);
			if (Speech.speaker_direction > -1)
				{
				if (HeadAssembly.DirectInPanLimits(Speech.speaker_direction))
					{
					if (Speech.speaker_direction < 180)
						{
						pan = Speech.speaker_direction;
						diff = Math.Abs(Speech.speaker_direction - 90);       // a crude linear approximation of the angle difference from the Kinect or the speech direct board
						adj = (int)Math.Round(3 - ((double)3 * diff / 90));   // assumes a distance of ~ 10 ft to the speaker
						pan = Speech.speaker_direction + adj;
						}
					else
						{
						pan = Speech.speaker_direction - 360;
						diff = Math.Abs(pan + 90);
						adj = (int)Math.Round(3 - ((double)3 * diff / 90));
						pan = Speech.speaker_direction - adj;
						}
					HeadAssembly.Pan(pan,true);
//					pd.NearestHCLPerson(false, ref pdd);
					Thread.Sleep(5000);
					HeadAssembly.Pan(0,true);
					}
				else
					Log.LogEntry("Speaker not within pan limits.");
				}
			else
				Log.LogEntry("Could not determine speaker direction.");
		}



		public void RegisterCommandSpeech()

		{
			if (Speech.RegisterHandler(GRAMMAR,SpeechHandler,null))
				Log.LogEntry(GRAMMAR + " handler registered.");
			else
				Log.LogEntry("Could not register handler for " + GRAMMAR);
		}

		}
	}
