using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace LnkEditor
{
    public partial class MainForm : Form
    {
        private string openState;
        private string currentFile;
        private AboutBox About;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            About = new AboutBox();
            openState = "";
            var args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                convert(args[1]);
                Application.Exit();
            }
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            var dataGridView = (DataGridView)sender;
            if (e.KeyCode == Keys.Delete)
            {
                var toRemove = new List<int>();
                foreach (DataGridViewCell cell in dataGridView.SelectedCells)
                    toRemove.Add(cell.RowIndex);
                toRemove.Sort();
                toRemove.Reverse();
                foreach (var index in toRemove)
                {
                    if (!dataGridView.Rows[index].IsNewRow)
                        dataGridView.Rows.RemoveAt(index);
                }
                checkCellsState();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                dataGridView.BeginEdit(false);
                e.Handled = true;
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            openFile(files[0]);
        }

        private void convertTxtToLnk(string src, string dest)
        {
            LnkFile lnk = new LnkFile();
            lnk.lnkRecords = new ArrayList();
            StreamReader file = new StreamReader(src);
            while (!file.EndOfStream)
            {
                string line = file.ReadLine();
                string[] record = line.Split('\t');
                if (record.Length < 2)
                    continue;

                LnkRecord link = new LnkRecord();
                link.child = record[0];
                link.parent = record[1];
                lnk.lnkRecords.Add(link);
            }
            file.Close();
            lnk.save(dest);
        }

        private void convertLnkToTxt(string src, string dest)
        {
            LnkFile lnk = new LnkFile();
            lnk.load(src);
            StreamWriter file = new StreamWriter(dest);
            for (int i = 0; i < lnk.lnkRecords.Count; i++)
            {
                var link = lnk.lnkRecords[i] as LnkRecord;
                file.WriteLine(link.child + "\t" + link.parent);
            }
            file.Close();
        }

        private void convert(string filename)
        {
            try
            {
                filename = Path.GetFullPath(filename);
                if (Path.GetExtension(filename).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                    convertTxtToLnk(filename, filename.Substring(0, filename.Length - 4));
                else
                    convertLnkToTxt(filename, filename + ".txt");
            }
            catch
            {
                MessageBox.Show(String.Format("Cannot convert file {0}!", filename), "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void setCurrentFile(string filename)
        {
            if (filename != null)
            {
                currentFile = Path.GetFullPath(filename);
                this.Text = currentFile + " - LnkEditor";
            }
            else
                this.Text = "LnkEditor";
        }

        private string getCellsState()
        {
            string result = "";
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                var row = dataGridView1.Rows[i];
                var child = (string)row.Cells[0].Value;
                var parent = (string)row.Cells[1].Value;
                if (child == null)
                    child = "";
                if (parent == null)
                    parent = "";

                if (child == "" && parent == "")
                    continue;
                result += child + "\t" + parent + "\n";
            }
            return result;
        }

        private void checkCellsState()
        {
            setCurrentFile(currentFile);
            if (openState != getCellsState())
                this.Text = "* " + this.Text;
        }

        private void openFile(string filename)
        {
            if (filename == null)
            {
                if (openFileDialog1.ShowDialog() != DialogResult.OK)
                    return;
                filename = openFileDialog1.FileName;
            }

            var newRows = new List<object[]>();
            try
            {
                LnkFile lnk = new LnkFile();
                lnk.load(filename);
                for (int i = 0; i < lnk.lnkRecords.Count; i++)
                {
                    var link = lnk.lnkRecords[i] as LnkRecord;
                    newRows.Add(new object[] { link.child, link.parent });
                }
                dataGridView1.Rows.Clear();
                foreach (var row in newRows)
                    dataGridView1.Rows.Add(row);
                setCurrentFile(filename);
                openState = getCellsState();
            }
            catch
            {
                MessageBox.Show(String.Format("Cannot open file {0}!", filename), "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void saveFileAs(string filename)
        {
            LnkFile lnk = new LnkFile();
            lnk.lnkRecords = new ArrayList();
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                var row = dataGridView1.Rows[i];
                var child = (string)row.Cells[0].Value;
                var parent = (string)row.Cells[1].Value;
                if (child == null)
                    child = "";
                if (parent == null)
                    parent = "";

                if (child == "" && parent == "")
                    continue;

                LnkRecord link = new LnkRecord();
                link.child = child;
                link.parent = parent;
                lnk.lnkRecords.Add(link);
            }

            try
            {
                lnk.save(filename);
                setCurrentFile(filename);
            }
            catch
            {
                MessageBox.Show(String.Format("Cannot save file {0}!", filename), "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void saveFile(bool saveAs)
        {
            if (saveAs || currentFile == null)
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                    return;
                saveFileAs(saveFileDialog1.FileName);
            }
            else
                saveFileAs(currentFile);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFile(null);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFile(false);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFile(true);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About.ShowDialog();
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                var table = (DataGridView)sender;
                var value = (string)table[e.ColumnIndex, e.RowIndex].Value;
                if (value != null)
                    table[e.ColumnIndex, e.RowIndex].Value = value.Replace("\t", "");
            }
            catch { }

            checkCellsState();
        }
    }

    public class LnkFile
    {
        public ArrayList lnkRecords;
        public void load(string path)
        {
            FileStream fs = File.Open(path, FileMode.Open);
            BinaryReader reader = new BinaryReader(fs);
            lnkRecords = new ArrayList();
            UInt32 linksCount = reader.ReadUInt32();
            for (int i = 0; i < linksCount; i++)
            {
                LnkRecord link = new LnkRecord();
                link.load(reader);
                lnkRecords.Add(link);
            }
            reader.Close();
        }
        public void save(string path)
        {
            FileStream fs = File.Open(path, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(fs);
            writer.Write((UInt32)lnkRecords.Count);
            for (int i = 0; i < lnkRecords.Count; i++)
            {
                LnkRecord link = lnkRecords[i] as LnkRecord;
                link.save(writer);
            }
            writer.Close();
        }
    }

    public class LnkRecord
    {
        public string child;
        public string parent;
        public void load(BinaryReader reader)
        {
            var encoding = Encoding.GetEncoding(1251);
            child = encoding.GetString(reader.ReadBytes(reader.ReadInt32()));
            parent = encoding.GetString(reader.ReadBytes(reader.ReadInt32()));
            if (child.Length > 0 && child[child.Length - 1] == '\0')
                child = child.Substring(0, child.Length - 1);
            if (parent.Length > 0 && parent[parent.Length - 1] == '\0')
                parent = parent.Substring(0, parent.Length - 1);
        }
        public void save(BinaryWriter writer)
        {
            var encoding = Encoding.GetEncoding(1251);

            if (child.Length > 0)
            {
                writer.Write((Int32)child.Length + 1);
                writer.Write(encoding.GetBytes(child));
                writer.Write((byte)0);
            }
            else
                writer.Write((Int32)0);

            if (parent.Length > 0)
            {
                writer.Write((Int32)parent.Length + 1);
                writer.Write(encoding.GetBytes(parent));
                writer.Write((byte)0);
            }
            else
                writer.Write((Int32)0);
        }
    }
}
