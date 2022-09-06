using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sub_system_Operation
	{
	public partial class MeasuredDistInputForm : Form
		{

		public double measured_dist;

		public MeasuredDistInputForm()

		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)

		{
			if (MDNumericUpDown.Value > 0)
				{
				this.DialogResult = DialogResult.OK;
				measured_dist = (double) MDNumericUpDown.Value;
				}
			else
				this.DialogResult= DialogResult.Cancel;
			this.Close();
		}

		}
	}
