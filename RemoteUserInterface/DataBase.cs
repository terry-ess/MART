using System;
using System.Collections;
using System.IO;
using System.Data.SQLite;
using System.Windows.Forms;

namespace BuildingDataBase
	{

	static class DataBase
	{
		
		public const string DB_FILE_EXT = "db";
		internal const string DATA_BASE_DIR = "\\DataBases\\";
		internal const string ROOM_DBS = "\\Building\\";
		private const string LAST_LOCATION_DB = "lastlocation.db";

		public readonly static ArrayList rooms = new ArrayList();


		static DataBase()
		
		{

		}



		static public SQLiteConnection Connection(string db_source)
		
		{
			SQLiteConnection connect = null;

			try
			{
			connect = new SQLiteConnection("DATA SOURCE=" + db_source);
			connect.Open();
			}
			
			catch (Exception)
			
			{
			}
			
			return(connect);
		}



	}
}
