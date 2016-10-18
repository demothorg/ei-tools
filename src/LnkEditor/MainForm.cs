using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using EILib;
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
            StreamReader file = new StreamReader(src);
            while (!file.EndOfStream)
            {
                string line = file.ReadLine();
                string[] record = line.Split('\t');
                if (record.Length < 2)
                    continue;

                LnkRecord link = new LnkRecord();
                link.Child = record[0];
                link.Parent = record[1];
                lnk.Records.Add(link);
            }
            file.Close();
            lnk.Save(dest);
        }

        private void convertLnkToTxt(string src, string dest)
        {
            LnkFile lnk = new LnkFile();
            lnk.Load(src);
            StreamWriter file = new StreamWriter(dest);
            for (int i = 0; i < lnk.Records.Count; i++)
            {
                var link = lnk.Records[i] as LnkRecord;
                file.WriteLine(link.Child + "\t" + link.Parent);
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
                lnk.Load(filename);
                for (int i = 0; i < lnk.Records.Count; i++)
                {
                    var link = lnk.Records[i] as LnkRecord;
                    newRows.Add(new object[] { link.Child, link.Parent });
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
                link.Child = child;
                link.Parent = parent;
                lnk.Records.Add(link);
            }

            try
            {
                lnk.Save(filename);
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
}
