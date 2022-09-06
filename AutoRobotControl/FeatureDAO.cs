using System;
using System.Data.SQLite;
using System.Data;
using AutoRobotControl;

namespace BuildingDataBase
	{
	class FeatureDAO
		{


		public void CreateTable(SQLiteConnection connect)

		{
			SQLiteCommand cmd;

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "CREATE TABLE Features (type INTEGER,coord TEXT,room_id INTEGER,edge_id INTEGER,recharge_id INTEGER)";
			cmd.ExecuteNonQuery();
			}
			
			catch(Exception)
			
			{
			}

		}



		public DataTable RoomFeatureList(SQLiteConnection connect, Int64 tableid)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,type,coord FROM Features WHERE room_id=" + tableid;
			reader = cmd.ExecuteReader();
			col = new DataColumn();
			col.ColumnName = "rowid";
			col.DataType = System.Type.GetType("System.Int64");
			key[0] = col;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "type";
			col.DataType = System.Type.GetType("System.Int32");
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "coord";
			col.DataType = System.Type.GetType("System.String");
			col.Unique = false;
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



		public DataTable RoomFeatureList(SQLiteConnection connect,int type, Int64 tableid)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,coord FROM Features WHERE room_id=" + tableid + " AND type=" + type;
			reader = cmd.ExecuteReader();
			col = new DataColumn();
			col.ColumnName = "rowid";
			col.DataType = System.Type.GetType("System.Int64");
			key[0] = col;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "coord";
			col.DataType = System.Type.GetType("System.String");
			col.Unique = false;
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



		public DataTable EdgeFeature(SQLiteConnection connect, Int64 tableid)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,coord FROM Features WHERE edge_id=" + tableid + " AND type=" + ((int) NavData.FeatureType.OPENING_EDGE);
			reader = cmd.ExecuteReader();
			col = new DataColumn();
			col.ColumnName = "rowid";
			col.DataType = System.Type.GetType("System.Int64");
			key[0] = col;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "coord";
			col.DataType = System.Type.GetType("System.String");
			col.Unique = false;
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



		public DataTable RechargeFeature(SQLiteConnection connect, Int64 tableid)

		{
			SQLiteCommand cmd;
			SQLiteDataReader reader = null;
			DataTable dt = new DataTable();
			DataColumn col;
			DataColumn[] key = new DataColumn[1];

			try
			{
			cmd = connect.CreateCommand();
			cmd.CommandText = "SELECT ROWID,coord FROM Features WHERE recharge_id=" + tableid + " AND type=" + ((int)NavData.FeatureType.TARGET);
			reader = cmd.ExecuteReader();
			col = new DataColumn();
			col.ColumnName = "rowid";
			col.DataType = System.Type.GetType("System.Int64");
			key[0] = col;
			dt.Columns.Add(col);
			col = new DataColumn();
			col.ColumnName = "coord";
			col.DataType = System.Type.GetType("System.String");
			col.Unique = false;
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
