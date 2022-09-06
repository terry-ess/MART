using System;
using System.Drawing;
using System.Data.SQLite;
using System.Data;


namespace BuildingDataBase
	{
	class LastLocationDAO
		{

		public void CreateTable(SQLiteConnection connect)

		{
			SQLiteCommand cmd;

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "CREATE TABLE LastLocation (name TEXT,coord TEXT,orientation INTEGER,loc_name TEXT,entrance INTEGER)";
			cmd.ExecuteNonQuery();
			}
			
			catch(Exception)
			
			{
			}

		}



		public DataTable LastLocation(SQLiteConnection connect)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT name,coord,orientation,loc_name,entrance FROM LastLocation LIMIT 1";
			reader = cmd.ExecuteReader();
			col = new DataColumn();
			col.ColumnName = "name";
			col.Unique = false;
			col.DataType = System.Type.GetType("System.String");
			key[0] = col;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "coord";
			col.DataType = System.Type.GetType("System.String");
			col.Unique = false;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "orientation";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "loc_name";
			col.DataType = System.Type.GetType("System.String");
			col.Unique = false;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "entrance";
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



		public bool UpdateLastLocation(SQLiteConnection connect, string name,Point coord,int orient,string loc_name,bool entrance)

		{
			bool rtn = false;
			SQLiteCommand cmd;
			string crd;

			try
			{
			cmd = connect.CreateCommand();
			crd = coord.X + "," + coord.Y;
			if ((LastLocation(connect)).Rows.Count > 0)
				cmd.CommandText = "UPDATE LastLocation SET name='" + name + "',coord='" + crd +"',orientation=" + orient + ",loc_name='" + loc_name + "',entrance=" + Convert.ToInt32(entrance).ToString() +  " WHERE ROWID=1";
			else
				cmd.CommandText = "INSERT INTO LastLocation VALUES('" + name + "','" + crd + "'," + orient + ",'" + loc_name + "'," + Convert.ToInt32(entrance).ToString() + ")";
			if (cmd.ExecuteNonQuery() == 1)
				rtn = true;
			}

			catch(Exception)
			{
			}

			return (rtn);
		}


		}
	}
