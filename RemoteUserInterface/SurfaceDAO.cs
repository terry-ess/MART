using System;
using System.Data.SQLite;
using System.Data;

namespace BuildingDataBase
	{
	class SurfaceDAO
		{

		public void CreateTable(SQLiteConnection connect)

		{
			SQLiteCommand cmd;

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "CREATE TABLE Surfaces (start TEXT,end TEXT,type INTEGER,room_id INTEGER)";
			cmd.ExecuteNonQuery();
			}
			
			catch(Exception)
			
			{
			}

		}



		public DataTable SurfaceList(SQLiteConnection connect,int type, Int64 tableid)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,start,end FROM Surfaces WHERE room_id=" + tableid + " AND type=" + type;
			reader = cmd.ExecuteReader();
			col = new DataColumn();
			col.ColumnName = "rowid";
			col.DataType = System.Type.GetType("System.Int64");
			key[0] = col;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "start";
			col.DataType = System.Type.GetType("System.String");
			col.Unique = false;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "end";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			dt.PrimaryKey = key;
			dt.Load(reader);
			reader.Close();
			}

			catch (Exception)
			{
			if (reader != null)
				reader.Close();
			}

			return (dt);
		}


		}
	}
