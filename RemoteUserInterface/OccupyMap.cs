using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using DBMap;

namespace BuildingDataBase
	{
	static class OccupyMap
		{

		static public bool SaveMap(string map_name, byte[,] map)

		{
			bool rtn = false;
			IFormatter formatter = new BinaryFormatter();
			Stream stream;

			try
			{
			stream = new FileStream(Application.StartupPath + DataBase.DATA_BASE_DIR + DataBase.ROOM_DBS + map_name, FileMode.Create, FileAccess.Write);
			formatter.Serialize(stream, map);
			stream.Close();
			rtn = true;
			}

			catch(Exception)
			{
			}

			return (rtn);
		}



		static public bool ReadMap(string map_name,ref byte[,] map)

		{
			bool rtn = false;
			IFormatter formatter = new BinaryFormatter();
			Stream stream;

			try
			{
			stream = new FileStream(map_name, FileMode.Open, FileAccess.Read);
			map = (byte[,]) formatter.Deserialize(stream);
			stream.Close();
			rtn = true;
			}

			catch(Exception)
			{
			}

			return (rtn);
		}


		}
	}
