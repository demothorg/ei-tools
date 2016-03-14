using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SetMenuPositions
{
    public partial class MainForm : Form
    {
        Position[] positions;
        Position current;
        bool isSetting;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            positions = new Position[6];
            for (int i = 0; i < 6; i++)
                positions[i] = new Position();
            current = positions[0];
            listBox1.SelectedIndex = 0;
            isSetting = false;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image == null)
                return;
            current.x1 = e.X;
            current.y1 = e.Y;
            current.x2 = e.X;
            current.y2 = e.Y;
            isSetting = true;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isSetting)
                return;
            current.x2 = e.X;
            current.y2 = e.Y;
            pictureBox1.Refresh();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Pen p = new Pen(Color.FromArgb(150, 255, 255, 255), 3);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(p, Rectangle.FromLTRB(
                Math.Min(current.x1, current.x2), Math.Min(current.y1, current.y2),
                Math.Max(current.x1, current.x2), Math.Max(current.y1, current.y2)));
            e.Graphics.Save();
        }

        private static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isSetting)
                return;
            if (current.x2 < current.x1)
                Swap(ref current.x1, ref current.x2);
            if (current.y2 < current.y1)
                Swap(ref current.y1, ref current.y2);
            isSetting = false;
        }

        private void listBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            current = positions[listBox1.SelectedIndex];
            pictureBox1.Refresh();
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            pictureBox1.Size = new Size(
                this.Width - panel1.Width - pictureBox1.Left - 24,
                this.Height - 50);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.ImageLocation = openFileDialog1.FileName;
                pictureBox1.Load();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string escMenu = "[EscMenu]\r\n" +
                             "SaveGame=295\r\n" +
                             "SaveGame=79\r\n" +
                             "SaveGame=495\r\n" +
                             "SaveGame=160\r\n" +
                             "LoadGame=295\r\n" +
                             "LoadGame=160\r\n" +
                             "LoadGame=495\r\n" +
                             "LoadGame=215\r\n" +
                             "Options=295\r\n" +
                             "Options=220\r\n" +
                             "Options=495\r\n" +
                             "Options=300\r\n" +
                             "ResumeGame=275\r\n" +
                             "ResumeGame=312\r\n" +
                             "ResumeGame=510\r\n" +
                             "ResumeGame=355\r\n" +
                             "ExitMM=240\r\n" +
                             "ExitMM=380\r\n" +
                             "ExitMM=550\r\n" +
                             "ExitMM=444";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var sr = new StreamWriter(new FileStream(saveFileDialog1.FileName, FileMode.Create));
                sr.WriteLine("[MainMenu]");
                sr.Write(positions[0].ToString(listBox1.Items[0].ToString()));
                sr.Write(positions[1].ToString(listBox1.Items[1].ToString()));
                sr.Write(positions[2].ToString(listBox1.Items[2].ToString()));
                sr.Write(positions[3].ToString(listBox1.Items[3].ToString()));
                sr.Write(positions[4].ToString(listBox1.Items[4].ToString()));
                sr.Write(positions[5].ToString(listBox1.Items[5].ToString()));
                if (checkBox1.Checked)
                    sr.Write(escMenu);
                sr.Close();
            }
        }
    }

    public class Position
    {
        public int x1, y1, x2, y2;
        public Position()
        {
            x1 = x2 = y1 = y2 = 0;
        }
        public Position(int xx1, int yy1, int xx2, int yy2)
        {
            x1 = xx1;
            x2 = xx2;
            y1 = yy1;
            y2 = yy2;
        }
        public string ToString(string name)
        {
            string s = "";
            s += name + "=" + x1.ToString() + "\r\n";
            s += name + "=" + y1.ToString() + "\r\n";
            s += name + "=" + x2.ToString() + "\r\n";
            s += name + "=" + y2.ToString() + "\r\n";
            return s;
        }
    }
}
