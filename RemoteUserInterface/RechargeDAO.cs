using System;
using System.Data.SQLite;
using System.Data;


namespace BuildingDataBase
	{
	class RechargeDAO
		{

		public void CreateTable(SQLiteConnection connect)

		{
			SQLiteCommand cmd;

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "CREATE TABLE RechargeStations (coord TEXT,depth REAL,ptp_width REAL,direction INTEGER,target_id INTEGER,room_id INTEGER)";
			cmd.ExecuteNonQuery();
			}
			
			catch(Exception)
			
			{
			}

		}



		public DataTable RechargeStation(SQLiteConnection connect, Int64 tableid)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,coord,depth,ptp_width,direction,sensor_offset FROM RechargeStations WHERE room_id=" + tableid;
			reader = cmd.ExecuteReader();
			col = new DataColumn();
			col.ColumnName = "rowid";
			col.DataType = System.Type.GetType("System.Int64");
			key[0] = col;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "coord";
			col.DataType = System.Type.GetType("System.String");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "depth";
			col.DataType = System.Type.GetType("System.Double");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "ptp_width";
			col.DataType = System.Type.GetType("System.Double");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "direction";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "sensor_offset";
			col.DataType = System.Type.GetType("System.Int64");
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
