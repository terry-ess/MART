using System;
using System.IO;
using System.Collections;
using System.Windows.Forms;


namespace AutoRobotControl
	{
	public class SkillsCommands: CommandHandlerInterface
		{

		public const string GRAMMAR = "skillscommands";
		public const string SKILL_TYPE_NAME = ".Skill";

		private ArrayList commands = new ArrayList();


		public SkillsCommands()

		{
			string fname;
			int i;
			string[] files;

			RegisterCommandSpeech();
			fname = Application.StartupPath + SharedData.SKILLS_SUB_DIR;
			files = Directory.GetFiles(fname, "*.dll");
			for (i = 0;i < files.Length;i++)
				commands.Add(files[i].Substring(fname.Length, files[i].Length - fname.Length - 4));
		}



		private void OutputSpeech(string output)

		{
			Speech.Speak(output);
			Log.LogEntry(output);
		}



		public void SpeechHandler(string msg)

		{
			string[] words;
			int i;
			string cmd,ucmd;

			words = msg.Split(' ');
			if ((msg.StartsWith("run skill")) && (words.Length >= 3))
				{
				for (i = 0;i < commands.Count;i++)
					{
					cmd = (string) commands[i];
					if (msg.EndsWith(cmd))
						{
						OutputSpeech("OK");
						Log.KeyLogEntry(msg);
						ucmd = cmd.Replace(' ','_');
						Skills.OpenSkill(Application.StartupPath + SharedData.SKILLS_SUB_DIR + cmd + ".dll", ucmd + SKILL_TYPE_NAME);
						break;
						}
					}
				}
			else if (msg == "close skill")
				{
				OutputSpeech("ok");
				Log.KeyLogEntry(msg);
				Skills.CloseSkill();
				}
			else
				OutputSpeech("Command " + msg + " is incorrect.");
		}
		


		public void RegisterCommandSpeech()

		{
			Speech.RegisterHandler(GRAMMAR,SpeechHandler,null);
		}

		}
	}
