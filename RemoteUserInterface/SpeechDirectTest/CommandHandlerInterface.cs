using System;
using Microsoft.Speech.Recognition;


namespace AutoRobotControl
	{
	interface CommandHandlerInterface
		{
		void RegisterCommandSpeech();
		void SpeechHandler(string msg);
		}
	}
