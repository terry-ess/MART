using System;
using System.Data.SQLite;
using System.Data;
using System.IO;
using System.Windows.Forms;
using BuildingDataBase;
using AutoRobotControl;


namespace Work_Assist
	{
	static class WorkSpaceDAO
		{

		private const string WORKAREA_DBS = "WorkArea\\";
		private const string WORKAREA_DB = "workarea." + DataBase.DB_FILE_EXT;


		static WorkSpaceDAO()

		{
			SQLiteConnection connect;

			Log.LogEntry("WorkSpaceDAO start");
			if (!File.Exists(Application.StartupPath + DataBase.DATA_BASE_DIR + WORKAREA_DBS + WORKAREA_DB))
				{
				if (!Directory.Exists(Application.StartupPath + DataBase.DATA_BASE_DIR + WORKAREA_DBS))
					Directory.CreateDirectory(Application.StartupPath + DataBase.DATA_BASE_DIR + WORKAREA_DBS);
				SQLiteConnection.CreateFile(Application.StartupPath + DataBase.DATA_BASE_DIR + WORKAREA_DBS + WORKAREA_DB);
				connect = new SQLiteConnection("DATA SOURCE=" + Application.StartupPath + DataBase.DATA_BASE_DIR + WORKAREA_DBS + WORKAREA_DB);
				connect.Open();
				CreateTable(connect);
				connect.Close();
				}
		}



		private static void CreateTable(SQLiteConnection connect)

		{
			SQLiteCommand cmd;
			
			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "CREATE TABLE WorkSpace (name TEXT,room TEXT,topology INTEGER,side INTEGER,person_coord TEXT,edge_perp_direct INTEGER,top_height REAL)";
			cmd.ExecuteNonQuery();
			}
			
			catch(Exception ex)
			
			{
			Log.LogEntry("CreateTable exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

		}



		public static DataTable WorkSpaceList(string name)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];
			SQLiteConnection connect = null;

			try
			{
			connect = DataBase.Connection(Application.StartupPath + DataBase.DATA_BASE_DIR + WORKAREA_DBS + WORKAREA_DB);
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT name,topology,side,person_coord,edge_perp_direct,top_height FROM WorkSpace WHERE room='" + name + "'";
			reader = cmd.ExecuteReader();
			col = new DataColumn();
			col.ColumnName = "name";
			col.DataType = System.Type.GetType("System.String");
			col.Unique = false;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "topology";
			col.DataType = System.Type.GetType("System.Int16");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "side";
			col.DataType = System.Type.GetType("System.Int16");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "person_coord";
			col.DataType = System.Type.GetType("System.String");
			col.Unique = false;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "edge_perp_direct";
			col.DataType = System.Type.GetType("System.Int16");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "top_height";
			col.DataType = System.Type.GetType("System.Double");
			dt.Columns.Add(col);
			dt.PrimaryKey = key;
			dt.Load(reader);
			reader.Close();
			connect.Close();
			}

			catch (Exception ex)
			{
			if (reader != null)
				reader.Close();
			if (connect != null)
				connect.Close();
			Log.LogEntry("WorkSpaceList exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			return (dt);
		}



		public static bool AddWorkSpace(string name,string room,Int16 top,Int16 side,string pcoord,Int16 edge_perp_direct,double top_height)

		{
			bool rtn = false;
			SQLiteCommand cmd;
			SQLiteConnection connect = null;

			try
			{
			connect = DataBase.Connection(Application.StartupPath + DataBase.DATA_BASE_DIR + WORKAREA_DBS + WORKAREA_DB);
			cmd = connect.CreateCommand();
			cmd.CommandText = "INSERT INTO WorkSpace VALUES('" + name + "','" + room + "'," + top + "," + side + ",'" + pcoord + "'," + edge_perp_direct + "," + top_height + ")";
			if (cmd.ExecuteNonQuery() == 1)
				rtn = true;
			connect.Close();
			}

			catch(Exception ex)
			{
			if (connect != null)
				connect.Close();
			Log.LogEntry("AddWorkspace exception: " + ex.Message);
			Log.LogEntry("Stack trace: " + ex.StackTrace);
			}

			return (rtn);
		}

		}
	}
