using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;


namespace AutoRobotControl
	{
	public static class Skills
		{

		private static SkillsInterface si = null;

		public static bool return_failed = false;


		public static void InitSkills()

		{
			CreateSkillsGrammar();
		}



		public static void OpenSkill(string file_name, string type_name, params object[] obj)

		{
			Log.LogEntry("OpenSkill " + file_name + " " + type_name + " " + obj.Length);
			if (si == null)
				{
				return_failed = false;

				try
				{
				Assembly DLL = Assembly.LoadFrom(file_name);
				Type ctype = DLL.GetType(type_name);
				dynamic c = Activator.CreateInstance(ctype);
				si = (SkillsInterface) c.Open();
				Speech.DisableAllCommands();
				Speech.EnableCommand(SkillsCommands.GRAMMAR);
				if (si.Open(obj))
					SharedData.status = SharedData.RobotStatus.SKILL_RUNNING;
				else
					si.Close();
				}

				catch (Exception ex)
				{
				OutputSpeech("Skill open exception " + ex.Message + ". Skill is not enabled.");
				Log.LogEntry("Stack trace: " + ex.StackTrace);
				si = null;
				Speech.EnableAllCommands();
				}

				}
		}



		public static void CloseSkill()

		{
			if (si != null)
				{

				try
				{
				si.Close();
				}

				catch(Exception)
				{
				}

				si = null;
				Speech.EnableAllCommands();
				SharedData.status = SharedData.RobotStatus.NORMAL_RUNNING;
				}
		}



		public static void ReturnFailed()

		{
			return_failed = true;
		}


		public static bool SkillInProgress()

		{
			bool rtn = false;

			if (si != null)
				rtn = true;
			return(rtn);
		}


		private static void OutputSpeech(string output)

		{
			Speech.Speak(output);
			Log.LogEntry(output);
		}


		
		private static void CreateSkillsGrammar()

		{
			TextReader tr;
			TextWriter tw1;
			string fname,rfname,wfname1,line,grammar_name;
			int i;
			string[] files;

			fname = Application.StartupPath + SharedData.SKILLS_SUB_DIR;
			files = Directory.GetFiles(fname, "*.dll");
			rfname = Application.StartupPath + SharedData.CAL_SUB_DIR + "basecommands.txt";
			wfname1 = Application.StartupPath + SharedData.CAL_SUB_DIR + "skillscommands.xml";
			if ((files.Length > 0) && (File.Exists(rfname)))
				{
				tr = File.OpenText(rfname);
				tw1 = File.CreateText(wfname1);
				while ((line = tr.ReadLine()) != null)
					{
					if (line.Equals("</grammar>"))
						{
						tw1.WriteLine("  <rule id=\"rootRule\">");
						tw1.WriteLine("    <one-of>");
						tw1.WriteLine("       <item> close skill </item>");
						tw1.WriteLine("       <item><ruleref uri=\"#run\" /></item>");
						tw1.WriteLine("    </one-of>");
						tw1.WriteLine("  </rule>");
						tw1.WriteLine();
						tw1.WriteLine("  <rule id=\"run\">");						
						tw1.WriteLine("    <item> run skill </item>");
						tw1.WriteLine("    <item>");
						tw1.WriteLine("       <one-of>");
						for (i = 0;i < files.Length;i++)
							{
							grammar_name = files[i].Substring(fname.Length, files[i].Length - fname.Length - 4);
							tw1.WriteLine("         <item>" + grammar_name + "</item>");
							}
						tw1.WriteLine("       </one-of>");
						tw1.WriteLine("     </item>");
						tw1.WriteLine("  </rule>");
						tw1.WriteLine();
						tw1.WriteLine(line);
						break;
						}
					else
						tw1.WriteLine(line);
					}
				tr.Close();
				tw1.Close();
				}
		}


		}
	}
