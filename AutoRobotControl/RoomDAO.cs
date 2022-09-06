using System;
using System.Data.SQLite;
using System.Data;

namespace BuildingDataBase
	{
	class RoomDAO
		{

		public void CreateTable(SQLiteConnection connect)

		{
			SQLiteCommand cmd;

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "CREATE TABLE Rooms (name TEXT,width INTEGER,height INTEGER,occupy_map TEXT,heading_cal_file TEXT,building_coord TEXT)";
			cmd.ExecuteNonQuery();
			}
			
			catch(Exception)
			
			{
			}

		}



		public Int64 FirstRoomId(SQLiteConnection connect)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			Int64 rtn = -1;

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID FROM Rooms LIMIT 1";
			reader = cmd.ExecuteReader();
			if (reader.Read())
				rtn = reader.GetInt64(0);
			reader.Close();
			}

			catch(Exception)
			{
			if (reader != null)
				reader.Close();
			}

			return(rtn);
		}



		public Int64 NextRoomId(SQLiteConnection connect,Int64 lastid)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			Int64 rtn = -1;

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID FROM Rooms WHERE ROWID>" + lastid + " LIMIT 1";
			reader = cmd.ExecuteReader();
			if (reader.Read())
				rtn = reader.GetInt64(0);
			reader.Close();
			}

			catch(Exception)
			{
			if (reader != null)
				reader.Close();
			}

			return(rtn);
		}



		public DataTable RoomList(SQLiteConnection connect)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,name,width,height,occupy_map,heading_cal_file,building_coord FROM Rooms";
			reader = cmd.ExecuteReader();
			dt = new DataTable();
			col = new DataColumn();
			col.ColumnName = "rowid";
			col.DataType = System.Type.GetType("System.Int64");
			key[0] = col;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "name";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "width";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "height";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "occupy_map";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "heading_cal_file";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "building_coord";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			col = new DataColumn();
			dt.PrimaryKey = key; 
			dt.Load(reader);
			reader.Close();
			}

			catch(Exception)
			{
			if (reader != null)
				reader.Close();
			}

			return(dt);
		}



		public DataTable RoomData(SQLiteConnection connect,Int64 rowid)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT name,width,height,occupy_map,heading_cal_file,building_coord FROM Rooms WHERE ROWID=" + rowid;
			reader = cmd.ExecuteReader();
			dt = new DataTable();
			col = new DataColumn();
			col.ColumnName = "name";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			key[0] = col;
			col = new DataColumn();
			col.ColumnName = "width";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "height";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "occupy_map";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "heading_cal_file";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "building_coord";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			col = new DataColumn();
			dt.PrimaryKey = key; 
			dt.Load(reader);
			reader.Close();
			}

			catch(Exception)
			{
			if (reader != null)
				reader.Close();
			}

			return(dt);
		}


		public DataTable RoomData(SQLiteConnection connect,string name)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,width,height,occupy_map,building_coord FROM Rooms WHERE name='" + name + "'";
			reader = cmd.ExecuteReader();
			dt = new DataTable();
			col = new DataColumn();
			col.ColumnName = "rowid";
			col.DataType = System.Type.GetType("System.Int64");
			dt.Columns.Add(col);
			key[0] = col;
			col = new DataColumn();
			col.ColumnName = "width";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "height";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "occupy_map";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			col = new DataColumn();
			col = new DataColumn();
			col.ColumnName = "building_coord";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			col = new DataColumn();
			dt.PrimaryKey = key; 
			dt.Load(reader);
			reader.Close();
			}

			catch(Exception)
			{
			if (reader != null)
				reader.Close();
			}

			return(dt);
		}

		}
	}
