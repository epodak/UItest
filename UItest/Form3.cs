using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UItest
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
            button1.Image = new Bitmap(button1.Image, button1.Height - 10, button1.Height - 10);
            DateTime timea = DateTime.Today;
            string stra = timea.ToString("yyyy-MM-dd");
            textBox2.Text = stra;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form3_Load(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Visible = false;
        }
    }
}
