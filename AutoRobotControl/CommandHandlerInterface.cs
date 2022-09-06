using System;
using Microsoft.Speech.Recognition;


namespace AutoRobotControl
	{
	public interface CommandHandlerInterface
		{
		void RegisterCommandSpeech();
		void SpeechHandler(string msg);
		}
	}
