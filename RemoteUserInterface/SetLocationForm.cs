using System;
using System.Drawing;
using System.IO;
using System.Data.SQLite;
using System.Windows.Forms;
using BuildingDataBase;
using DBMap;

namespace RemoteUserInterface
	{
	public partial class SetLocationForm : Form
		{

		public string room;
		public Point coord;
		public int orientation;

		private RoomData rmdata;
		private byte[,] detail_map;
		private Bitmap brbm = null;


		public SetLocationForm()

		{
			string dir;
			string[] files;
			string file;
			int i;

			InitializeComponent();
			dir = Application.StartupPath + DataBase.DATA_BASE_DIR + DataBase.ROOM_DBS;
			files = Directory.GetFiles(dir,"*.db");
			for (i = 0;i < files.Length;i++)
				{
				file = files[i].Substring(dir.Length);
				file = file.Substring(0,file.Length - 3);
				NameComboBox.Items.Add(file);
				}
		}



		private void DoneButton_Click(object sender, EventArgs e)

		{
			if ((room.Length > 0) && (!coord.IsEmpty) && (orientation > -1))
				{
				orientation = (int) OrientNumericUpDown.Value;
				this.DialogResult = DialogResult.OK;
				}
			else
				this.DialogResult = DialogResult.Cancel;
			this.Close();
		}



		private void NameComboBox_SelectedIndexChanged(object sender, EventArgs e)

		{
			SQLiteConnection connectn = null;
			string rdb;

			if (NameComboBox.Text.Length > 0)
				{
				rdb = Application.StartupPath + DataBase.DATA_BASE_DIR + DataBase.ROOM_DBS + NameComboBox.Text + "." + DataBase.DB_FILE_EXT;
				connectn = DataBase.Connection(rdb);
				if (connectn != null)
					{
					rmdata = new RoomData();
					if (rmdata.LoadRoomData(rdb, RmTextBox))
						{
						detail_map = new byte[rmdata.rd.rect.Width, rmdata.rd.rect.Height];
						rmdata.CreateRoomMap(rmdata.rd, ref detail_map, ref brbm);
						MapPictureBox.Image = brbm;
						CoordTextBox.Enabled = true;
						OrientNumericUpDown.Enabled = true;
						room = rmdata.rd.name;
						}

					}
				}
			}

		private void CoordTextBox_Leave(object sender, EventArgs e)
		
		{
			string[] values;
			

			values = CoordTextBox.Text.Split(',');
			if (values.Length == 2)
				{

				try
				{
				coord.X = int.Parse(values[0]);
				coord.Y = int.Parse(values[1]);
				if ((coord.X < 0) || (coord.X > rmdata.rd.rect.Width) || (coord.Y < 0) || (coord.Y > rmdata.rd.rect.Height))
						CoordTextBox.Clear();
				}

				catch(Exception)
				{
				CoordTextBox.Clear();
				}

				}
			else
				CoordTextBox.Clear();
		}


		}
	}
