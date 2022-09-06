using System;
using System.Data.SQLite;
using System.Data;

namespace BuildingDataBase
	{
	class ConnectionDAO
		{

		public void CreateTable(SQLiteConnection connect)

		{
			SQLiteCommand cmd;

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "CREATE TABLE Connections (name TEXT,exit_center_coord TEXT,exit_width INTEGER,direction INTEGER,room_id INTEGER)";
			cmd.ExecuteNonQuery();
			}
			
			catch(Exception)
			
			{
			}

		}



		public DataTable RoomConnectionsList(SQLiteConnection connect, Int64 tableid)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,name,exit_center_coord,exit_width,direction FROM Connections WHERE room_id=" + tableid;
			reader = cmd.ExecuteReader();
			col = new DataColumn();
			col.ColumnName = "rowid";
			col.DataType = System.Type.GetType("System.Int64");
			key[0] = col;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "name";
			col.DataType = System.Type.GetType("System.String");
			col.Unique = false;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "exit_center_coord";
			col.DataType = System.Type.GetType("System.String");
			col.Unique = false;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "exit_width";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "direction";
			col.DataType = System.Type.GetType("System.Int32");
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
