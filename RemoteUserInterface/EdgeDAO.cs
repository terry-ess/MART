using System;
using System.Data.SQLite;
using System.Data;


namespace BuildingDataBase
	{
	class EdgeDAO
		{

		public void CreateTable(SQLiteConnection connect)

		{
			SQLiteCommand cmd;

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "CREATE TABLE Edges (type INTEGER,door_side INTEGER,door_swing INTEGER,hc_side INTEGER,connect_id INTEGER)";
			cmd.ExecuteNonQuery();
			}
			
			catch(Exception)
			
			{
			}

		}



		public DataTable ConnectionEdgeList(SQLiteConnection connect, Int64 tableid)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,type,door_side,door_swing,hc_side FROM Edges WHERE connect_id=" + tableid;
			reader = cmd.ExecuteReader();
			col = new DataColumn();
			col.ColumnName = "rowid";
			col.DataType = System.Type.GetType("System.Int64");
			key[0] = col;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "type";
			col.DataType = System.Type.GetType("System.Int32");
			col.Unique = false;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "door_side";
			col.DataType = System.Type.GetType("System.Int32");
			col.Unique = false;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "door_swing";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "hc_side";
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
