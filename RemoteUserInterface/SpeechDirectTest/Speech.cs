using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;
using System.Speech.Synthesis;
using Microsoft.Speech.Recognition;
using Microsoft.Kinect;
using Constants;


namespace AutoRobotControl
	{

	public static class Speech
		{

		public delegate void SpeechHandler(string message);
		public delegate bool STSHandler(string message);

		public struct pair_handlers
		{
		public SpeechHandler sh;
		public STSHandler stsh;
		};

		public const string STOP_GRAMMAR = "stop";

		private const double MIN_CONFIDENCE  = .8;
		private const string STARTUP_GRAMMAR = "start";

		private static SpeechSynthesizer ss = null;
		private static SpeechRecognitionEngine sre = null;
		private static Microsoft.Kinect.KinectAudioSource source = null;
		private static SortedList<string,Grammar> grammars = new SortedList<string,Grammar>();
		private static AutoResetEvent reply_ready = new AutoResetEvent(false);
		private static string reply;
		private static int all_command_disabled_count = 0;
		private static Stack stop_handlers = new Stack();
		private static bool handle_speech = false;
		
		public static SortedList<string, pair_handlers> handlers = new SortedList<string, pair_handlers>();
		public static int speaker_direction;


		static Speech()

		{
			ss = new SpeechSynthesizer();
		}


		private static void OutputSpeech(string output)

		{
			Speak(output);
			Log.LogEntry(output);
		}



		private static RecognizerInfo GetKinectRecognizer()

		{
			Func<RecognizerInfo, bool> matchingFunc = r =>
			{
			  string value;
			  r.AdditionalInfo.TryGetValue("Kinect", out value);
			  return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, 
				StringComparison.InvariantCultureIgnoreCase);
			};
			return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
		}



		public static void EnableAllCommands()

		{
			if (all_command_disabled_count > 0)
				all_command_disabled_count -= 1;
			if (all_command_disabled_count == 0)
				{
				Log.LogEntry("All commands enabled.");
				foreach (KeyValuePair<string, Grammar> kvp in grammars)
					{
					if (kvp.Key.Contains("command"))
						((Grammar) kvp.Value).Enabled = true;
					}
				}
		}



		public static void EnableCommand(string cmd)

		{
			try
			{
			grammars[cmd].Enabled = true;
			}

			catch(Exception)
			{
			}

		}



		public static void DisableAllCommands()

		{
			foreach (KeyValuePair<string, Grammar> kvp in grammars)
				{
				if (kvp.Key.Contains("command"))
					((Grammar) kvp.Value).Enabled = false;
				}
			all_command_disabled_count += 1;
			if (all_command_disabled_count == 1)
				Log.LogEntry("All commands disabled.");
		}



		public static void DisableCommand(string cmd)

		{
			try
			{
			grammars[cmd].Enabled = false;
			}

			catch(Exception)
			{
			}

		}



		private static void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)

		{
			if (handle_speech)
				{
				try
				{
				if (e.Result.Confidence > (float) MIN_CONFIDENCE)
					{
					speaker_direction = SpeechDirection.GetSpeechDirection();
					Log.LogEntry(e.Result.Text );
					if (e.Result.Grammar.Name == STARTUP_GRAMMAR)
						{
//						UiCom.SendActMessage(UiConstants.VOICE_INPUT + "," + e.Result.Text);
						OutputSpeech("yes");
						grammars[STARTUP_GRAMMAR].Enabled = false;
						grammars[STOP_GRAMMAR].Enabled = true;
						EnableAllCommands();
						}
					else
						{

						try
						{
						SpeechHandler shi;
						pair_handlers ph;

						ph = (pair_handlers)handlers[e.Result.Grammar.Name];
						shi = (SpeechHandler) ph.sh;
						if (shi != null)
							{
//							UiCom.SendActMessage(UiConstants.VOICE_INPUT + "," + e.Result.Text);
							shi(e.Result.Text);
							}
						else
							OutputSpeech("No handler is available for " + e.Result.Text);
						}

						catch(Exception ex)
						{
						OutputSpeech("Speech recognition exception: " + ex.Message);
						Log.LogEntry("Source: " + ex.Source);
						Log.LogEntry("Stack trace: " + ex.StackTrace);
						}

						}
					}
				else
					Log.LogEntry("Message: " + e.Result.Text + "   Confidence: " + e.Result.Confidence);
				}

				catch(Exception ex)
				{
				Log.LogEntry("Speech recognition exception: " + ex.Message);
				Log.LogEntry("                   stack trace: " + ex.StackTrace);
				}
			}
		}



		private static void ConversationSpeechHandler(string msg)

		{
			reply = msg;
			reply_ready.Set();
		}



		public static void StartSpeechCommandHandlers()

		{
			int i, j;
			Module[] mod = Assembly.GetExecutingAssembly().GetLoadedModules();
			Object instance;
			string fname,grammar_name;
			string[] files;
			pair_handlers ph;

			if (grammars.Count == 0)
				{
				fname = Application.StartupPath + SharedData.CAL_SUB_DIR;
				files = Directory.GetFiles(fname,"*.xml");
				for (i = 0;i < files.Length;i++)
					{
					Log.LogEntry(files[i]);
					grammar_name = files[i].Substring(fname.Length,files[i].Length - fname.Length - 4);
					ph.sh = null;
					ph.stsh = null;
					handlers.Add(grammar_name,ph);
					grammars.Add(grammar_name,new Grammar(files[i]));
					grammars[grammar_name].Enabled = true;
					grammars[grammar_name].Name = grammar_name;
					}
				for (i = 0;i < mod.Length;i++)
					{
					Type[] typ = mod[i].FindTypes(null,null);
					for (j = 0;j < typ.Length;j++)
						if (typ[j].IsClass)
							{
							if (typ[j].GetInterface("CommandHandlerInterface") != null)
								instance = Activator.CreateInstance(typ[j]);
							}
					}
				}
		}



		public static void AddGrammerHandler(string file)

		{
			string fname,grammar_name;
			pair_handlers ph;

			fname = Application.StartupPath + SharedData.CAL_SUB_DIR;

			try
			{
			grammar_name = file.Substring(fname.Length, file.Length - fname.Length - 4);
			ph.sh = null;
			ph.stsh = null;
			handlers.Add(grammar_name, ph);
			grammars.Add(grammar_name, new Grammar(file));
			grammars[grammar_name].Enabled = false;
			grammars[grammar_name].Name = grammar_name;
			sre.LoadGrammar(grammars[grammar_name]);
			Log.LogEntry(file);
			}

			catch(Exception)
			{
			}
		}



		public static bool StartSpeechRecognition()

		{
			bool rtn = false;
			RecognizerInfo ri;

			if ((Kinect.nui != null) && (Kinect.nui.IsRunning))
				{

				OutputSpeech("Loading speech recognition files.");

				try
				{
				source = Kinect.nui.AudioSource;
				source.EchoCancellationMode = Microsoft.Kinect.EchoCancellationMode.None;
				source.AutomaticGainControlEnabled = false;
				ri = GetKinectRecognizer();
				sre = new SpeechRecognitionEngine(ri.Id);
				foreach (KeyValuePair<string,Grammar> kvp in grammars)
						sre.LoadGrammar((Grammar) kvp.Value);
				sre.SpeechRecognized += SpeechRecognized;
				sre.SetInputToAudioStream(source.Start(), new Microsoft.Speech.AudioFormat.SpeechAudioFormatInfo(Microsoft.Speech.AudioFormat.EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
				sre.RecognizeAsync(RecognizeMode.Multiple);
				handle_speech = true;
//				grammars[STARTUP_GRAMMAR].Enabled = true;
				rtn = true;
				SharedData.speech_recognition_active = true;
				OutputSpeech("Speech recognition has been initialized");
				}

				catch(Exception e)
				{
				Log.LogEntry("Speech recognition initialization exception: " + e.Message);
				Log.LogEntry("Stack trace: " + e.StackTrace);
				if (sre.Grammars.Count > 0)
					{
					sre.RecognizeAsyncCancel();
					sre.UnloadAllGrammars();
					source.Stop();
					source = null;
					}
				}

				}
			return(rtn);
		}




		public static void StopSpeechRecognition()

		{
			handle_speech = false;
			if (sre != null)
				{
				sre.RecognizeAsyncCancel();
				sre.UnloadAllGrammars();
				sre = null;
				SharedData.speech_recognition_active = false;
				}
			if (source != null)
				{
				source.Stop();
				source = null;
				}
		}



		public static void UnloadGrammar(String grammar)

		{
			Grammar gram;
			
			try
			{
			gram = grammars[grammar];
			sre.UnloadGrammar(gram);
			grammars.Remove(grammar);
			handlers.Remove(grammar);
			}

			catch(Exception)
			{
			}
			
		}



		public static void ReturnStart()

		{
			DisableAllCommands();
			grammars[STOP_GRAMMAR].Enabled = false;
			grammars[STARTUP_GRAMMAR].Enabled = true;
		}



		public static void Speak(string message)

		{
			if (ss != null)
				{
				handle_speech = false;
				ss.Speak(message);
//				UiCom.SendActMessage(UiConstants.SPEECH_OUTPUT + "," + message);
				handle_speech = true;
				}
		}



		private static void SpeakAsyncThread(object message)

		{
			Speak((string) message);
		}



		public static void SpeakAsync(string message)

		{
			Thread sat;

			if (ss != null)
				{
				sat = new Thread(SpeakAsyncThread);
				sat.Start(message);
				}
		}



		public static bool RegisterHandler(string grammar,SpeechHandler sh,STSHandler stsh)

		{
			bool rtn = false;

			try
			{
			SpeechHandler cshi;
			STSHandler cstshi;
			pair_handlers ph;

			ph = (pair_handlers) handlers[grammar];
			if (grammar == STOP_GRAMMAR)
				{
				stop_handlers.Push(ph);
				ph.sh = sh;
				ph.stsh = stsh;
				}
			else
				{
				cshi = (SpeechHandler) ph.sh;
				cstshi = (STSHandler) ph.stsh;
				if (cshi == null)
					ph.sh = sh;
				if (cstshi == null)
					ph.stsh = stsh;
				}
			handlers[grammar] = ph;
			rtn = true;
			}

			catch(Exception)
			{
			}

			return(rtn);
		}



		public static void UnRegisterHandler(string grammar)

		{

			try
			{
			pair_handlers ph;

			ph = (pair_handlers) handlers[grammar];
			if ((grammar == STOP_GRAMMAR) && (stop_handlers.Count > 0))
				{
				ph = (pair_handlers) stop_handlers.Pop();
				}
			else
				{
				ph.sh = null;
				ph.stsh = null;
				}
			handlers[grammar] = ph;
			}

			catch(Exception)
			{
			}

		}



		public static string Conversation(string message,string response_grammar_name,int wait_time,bool disable_cmds)

		{
			bool rtn;

			if (sre != null)
				{

				try
				{
				if (disable_cmds)
					DisableAllCommands();
				RegisterHandler(response_grammar_name,ConversationSpeechHandler,null);
				grammars[response_grammar_name].Enabled = true;
				Speak(message);
				rtn = reply_ready.WaitOne(wait_time);
				UnRegisterHandler(response_grammar_name);
				grammars[response_grammar_name].Enabled = false;
				if (disable_cmds)
					EnableAllCommands();
				}

				catch(Exception)
				{
				rtn = false;
				EnableAllCommands();
				}

				if (rtn)
					return(reply);
				else
					return("");
				}
			else
				return("");
		}



		public static int ParseDigit(string digit)

		{
			int rtn = 0;

			if (digit == "zero")
				rtn = 0;
			else if (digit == "one")
				rtn = 1;
			else if (digit == "two")
				rtn = 2;
			else if (digit == "three")
				rtn = 3;
			else if (digit == "four")
				rtn = 4;
			else if (digit == "five")
				rtn = 5;
			else if (digit == "six")
				rtn = 6;
			else if (digit == "seven")
				rtn = 7;
			else if (digit == "eight")
				rtn = 8;
			else if (digit == "nine")
				rtn = 9;
			return (rtn);
		}



/*		public static string DetermineRoom(bool disable_cmds)

		{
			string room = "",croom = "";
			ArrayList rooms;
			int i;
			string reply;
			bool check_all_rooms = false;

			croom = NavData.GetCurrentLocation().rm_name;
			reply = Conversation("Am I in " + croom + "?", "responseyn", 5000, disable_cmds);
			if (reply == "yes")
				{
				room = croom;
				Speak("Your response was yes");
				}
			else if (reply == "no")
				{
				Speak("Your response was no");
				check_all_rooms = true;
				}
			else
				Speak("No response was received.");
			if (check_all_rooms)
				{
				rooms = NavData.GetRooms();
				for (i = 0;i < rooms.Count;i++)
					{
					if ((string) rooms[i] != croom)
						{
						reply = Conversation("Am I in " + ((string) rooms[i]) + "?","responseyn",5000,disable_cmds);
						if (reply == "yes")
							{
							room = ((string) rooms[i]);
							Speak("Your response was yes");
							break;
							}
						else if (reply == "no")
							Speak("Your response was no");
						else
							{
							Speak("No response was received.");
							break;
							}
						}
					}
				}
			return(room);
		}



		public static string DetermineRechargeRoom(bool disable_cmds)

		{
			string room = "";
			ArrayList rooms;
			int i;
			string reply;
			NavData.recharge rch;

			rooms = NavData.GetRooms();
			for (i = 0;i < rooms.Count;i++)
				{
				rch = NavData.GetRechargeStation((string) rooms[i]);
				if (!rch.coord.IsEmpty)
					{
					reply = Speech.Conversation("Am I in " + ((string) rooms[i]) + "?","responseyn",5000,disable_cmds);
					if (reply == "yes")
						{
						room = ((string) rooms[i]);
						Speech.Speak("Your response was yes");
						break;
						}
					else if (reply == "no")
						Speech.Speak("Your response was no");
					else
						{
						Speech.Speak("No response was received.");
						break;
						}
					}
				}
			return(room);
		}



		public static int DetermineOrientation(bool disable_cmds)

		{
			int mad,value = -1;

			mad = NavCompute.DetermineDirection(HeadAssembly.GetMagneticHeading());
			Speak("Based on my magnetic compass I would estimate my orientation to be " + mad + " degrees");
			reply = Conversation("Do you agree?","responseyn",5000,disable_cmds);
			if (reply == "yes")
				{
				value = mad;
				Speak("Your response was yes");
				}
			else if (reply == "no")
				{
				Speak("Your response was no");
				reply = Conversation("Add ten degrees?","responseyn",5000,disable_cmds);
				if (reply == "yes")
					{
					value = (mad + 10) % 360;
					Speak("Your response was yes");
					}
				else if (reply == "no")
					{
					Speak("Your response was no");
					reply = Conversation("Less ten degrees?","responseyn",5000,disable_cmds);
					if (reply == "yes")
						{
						value = mad - 10;
						if (value < 0)
							value += 360;
						Speak("Your response was yes");
						}
					else if (reply == "no")
						Speak("Your response was no");
					else
						Speak("No response was received.");
				
					}
				else
					Speak("No response was received.");
				}
			else
				Speak("No response was received.");
			return(value);
		}


		
/*		public static int DetermineOrientation(bool disable_cmds)

		{
			string orient;
			string[] words;
			int value = -1;
			int mad;
			bool reasonable_response = false;
			SensorFusion sf = new SensorFusion();

			do
				{
				orient = Conversation("What is my orientation in degrees?","responseorient",5000,disable_cmds);
				if (orient.Length > 0)
					{
					words = orient.Split(' ');
					if (words.Length == 3)
						value = (ParseDigit(words[0]) * 100) + (ParseDigit(words[1]) * 10) + ParseDigit(words[2]);
					else if (words.Length == 2)
						value = (ParseDigit(words[0]) * 10) + ParseDigit(words[1]);
					else if (words.Length == 1)
						value = ParseDigit(words[0]);
					Speak("Your response was " + value + " degrees");
					mad = NavCompute.DetermineDirection(HeadAssembly.GetMagneticHeading());
					if (sf.WithInMagLimit(value,mad))
						reasonable_response = true;
					else
						{
						reasonable_response = false;
						Speak("Your response does not agree with my magnectic heading.");
						}
					}
				else
					Speak("No response was received.");
				}
			while ((orient.Length > 0) && !reasonable_response);
			return(value);
		} */





		}
	}
