using System;
using System.Collections;
using System.IO;
using System.Data.SQLite;
using System.Windows.Forms;

namespace BuildingDataBase
	{

	static public class DataBase
	{
		
		public const string DB_FILE_EXT = "db";
		public const string DATA_BASE_DIR = "\\DataBases\\";
		internal const string ROOM_DBS = "Building\\";
		private const string LAST_LOCATION_DB = "lastlocation.db";

		public readonly static ArrayList rooms = new ArrayList();
		public readonly static string lastlocation;


		static DataBase()
		
		{
			string [] files;
			int i;
			SQLiteConnection connect;
			LastLocationDAO lldao;

			if (!File.Exists(Application.StartupPath + DATA_BASE_DIR + LAST_LOCATION_DB))
				{
				if (!Directory.Exists(Application.StartupPath + DATA_BASE_DIR))
					Directory.CreateDirectory(Application.StartupPath + DATA_BASE_DIR + ROOM_DBS);
				SQLiteConnection.CreateFile(Application.StartupPath + DATA_BASE_DIR + LAST_LOCATION_DB);
				connect = new SQLiteConnection("DATA SOURCE=" + Application.StartupPath + DATA_BASE_DIR + LAST_LOCATION_DB);
				connect.Open();
				lldao = new LastLocationDAO();
				lldao.CreateTable(connect);
				connect.Close();
				}
			lastlocation = Application.StartupPath + DATA_BASE_DIR + LAST_LOCATION_DB;
			files = Directory.GetFiles(Application.StartupPath + DATA_BASE_DIR + ROOM_DBS, "*." + DB_FILE_EXT);
			for (i = 0; i < files.Length; i++)
				rooms.Add(files[i]);
		}



		static public SQLiteConnection Connection(string db_source)
		
		{
			SQLiteConnection connect = null;

			try
			{
			connect = new SQLiteConnection("DATA SOURCE=" + db_source);
			connect.Open();
			}
			
			catch (Exception ex)
			
			{
			throw(ex);
			}
			
			return(connect);
		}



	}
}
