using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;

namespace neww
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        int
            DELETED = 0,
            CREATED = 1,
            RENAMED = 2,
            UPDATED = 3,
            FILE_SAVED = 4,
            FILE_REPLACE_NAMES_WATCH = 5;
       
        int[] setingslist = new int[5];
        string[] files_to_watch = new string[] { };

        void updateList(string mode, string path, string old = "")
        {
            if (ext.stopped) return;

            //hide moving of files
            if (path.Contains("automaticDestinations-ms")) return;

            //if is in file watch mode
            if (files_to_watch.Length > 0 && !files_to_watch.Contains(path))
            {
                if (!files_to_watch.Contains(old))
                {
                    //skip adding to list
                    return;
                }
                else
                {
                    if (files_to_watch.Contains(old) && setingslist[FILE_REPLACE_NAMES_WATCH] == 1)
                    {
                        List<string> list = new List<string>(files_to_watch);
                        int index = list.FindIndex(s => s == old);
                        if (index != -1)
                        {
                            list[index] = path; ;
                        }
                        files_to_watch = list.ToArray();

                    }
                    else if (files_to_watch.Contains(old) && setingslist[FILE_REPLACE_NAMES_WATCH] == 2)
                    {
                        List<string> list = new List<string>(files_to_watch);
                        list.Add(path);
                        MessageBox.Show(string.Join("\n", list));
                        files_to_watch = list.ToArray();
                    }

                }
            }
            Color col = Color.Black;
            switch (mode)
            {
                case "Deleted":
                    col = Color.Red;
                    break;
                case "Renamed":
                    col = Color.Magenta;
                    break;
                case "Created":
                    col = Color.Green;
                    break;
                case "Updated":
                    col = Color.Blue;
                    break;
            }
            //this does not see files without extention needs a fix
            bool dirr = false;

            lock (ext.b_lock) { 
                if (Directory.Exists(path)) {
                        dirr = true;
                }
            }
            //Path.GetExtension(path) == String.Empty
            if (dirr)
            {
                if (skipdirupdate&&mode=="Updated") return;
                if (mode == "Updated") col = Color.Gray;
                dataGridView1.AsyncAddToList((string.IsNullOrEmpty(old) ? "" : "{ " + old + " --> } ") + path + "\\ " , "(DIR) " +Path.GetFileName(path), col,mode,dirr);
                setingslist[FILE_SAVED]=0;
                return;
            }

           dataGridView1.AsyncAddToList(path  + (string.IsNullOrEmpty(old) ? "" : "{ " + Path.GetFileName(old) + " --> " + Path.GetFileName(path) + " } "), Path.GetFileName(path), col, mode, dirr);
            setingslist[FILE_SAVED] = 0;
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            setingslist[DELETED] = 0; setingslist[CREATED] = 0; setingslist[RENAMED] = 0; setingslist[UPDATED] = 0; setingslist[4] = 0;
        }
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
            }
        }
        private void fileSystemWatcher1_Created(object sender, System.IO.FileSystemEventArgs e)
        {
                   
            if (setingslist[CREATED] == 1)
            {
                return;
            }
           
            updateList("Created", e.FullPath);
        }
        private void fileSystemWatcher1_Changed(object sender, System.IO.FileSystemEventArgs e)
        {

            if (setingslist[UPDATED] == 1)
            {
                return;
            }
            updateList("Updated", e.FullPath);

        }
        private void fileSystemWatcher1_Deleted(object sender, System.IO.FileSystemEventArgs e)
        {
            if (setingslist[DELETED] == 1)
            {
                return;
            }
           
            updateList("Deleted", e.FullPath);
        }

        private void fileSystemWatcher1_Renamed(object sender, System.IO.RenamedEventArgs e)
        {
                      
            if (setingslist[RENAMED] == 1)
            {
                return;
            }
           
            updateList("Renamed", e.FullPath, e.OldFullPath);
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show(null, "Do you want to save file before Clearing?", "save log file", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (res == DialogResult.Yes)
            {
                if (saveFile())
                {
                    dataGridView1.Rows.Clear();
                    ext.tempitemcount = 0;
                    ext.tempitemcount2 = 0;
                    ext.all_current_items = new List<ext.async_items>();
                    ext.item_add_count = 0;
                    ext.xcount = 0;
                    ext.xcountr = 1;
                }
            }
           else if (res == DialogResult.No)
            {
                dataGridView1.Rows.Clear();
                ext.tempitemcount = 0;
                ext.tempitemcount2 = 0;
                ext.all_current_items = new List<ext.async_items>();
                ext.item_add_count = 0;
                ext.xcount = 0;
                ext.xcountr = 1;
            }
            else if (res == DialogResult.Cancel)
            {
            }
     
        }

        string knownsavefile = string.Empty;
        bool saveFile(bool ask = false)
        {
            if (knownsavefile == string.Empty || ask)
            {
                timer2.Stop();
                SaveFileDialog dia = new SaveFileDialog();
                if (knownsavefile != string.Empty) dia.FileName = knownsavefile;
                dia.RestoreDirectory = true;
                dia.Filter = "Csv file *.csv|*.csv|Text file *.txt|*.txt|All files *.*|*.*";
                DialogResult result = dia.ShowDialog();

                if (result == DialogResult.Cancel)
                {
                    timer2.Start();
                    return false; //keep form open
                }
                if (result == DialogResult.OK)
                {
                    knownsavefile = dia.FileName;
                    saveToolStripMenuItem1.Text = "&Save " +Path.GetFileName(dia.FileName);
                    try
                    {
                        

                        if (Path.GetExtension(dia.FileName).ToLower().Contains("csv"))
                        {
                            int columnCount = dataGridView1.Columns.Count;
                            string columnNames = "";
                            string[] outputCsv = new string[dataGridView1.Rows.Count + 1];
                            for (int i = 0; i < columnCount; i++)
                            {
                                columnNames += dataGridView1.Columns[i].HeaderText.ToString() + ",";
                            }
                            outputCsv[0] += columnNames;

                            for (int i = 1; (i - 1) < dataGridView1.Rows.Count; i++)
                            {
                                for (int j = 0; j < columnCount; j++)
                                {
                                    outputCsv[i] += dataGridView1.Rows[i - 1].Cells[j].Value.ToString() + ",";
                                }
                            }

                            File.WriteAllLines(dia.FileName, outputCsv, Encoding.UTF8);
                        }
                        else
                        {
                                int columnCount = dataGridView1.Columns.Count;
                                string[] outputCsv = new string[dataGridView1.Rows.Count + 1];
                                for (int i = 1; (i - 1) < dataGridView1.Rows.Count; i++)
                                {
                                    for (int j = 0; j < columnCount; j++)
                                    {
                                        outputCsv[i] += dataGridView1.Rows[i - 1].Cells[j].Value.ToString() + "		";
                                    }
                                }

                                File.WriteAllLines(dia.FileName, outputCsv, Encoding.UTF8);
                            

                        }
                        timer2.Start();
                        setingslist[FILE_SAVED] = 1;
                        return true;
                    }
                    catch(Exception e){
                        timer2.Start();
                        MessageBox.Show("Can not save File! " + e.Message);
                        return false;
                    }
                }
            }
            else
            {
                try
                {
                    if (Path.GetExtension(knownsavefile).ToLower().Contains("csv"))
                    {
                        int columnCount = dataGridView1.Columns.Count;
                        string columnNames = "";
                        string[] outputCsv = new string[dataGridView1.Rows.Count + 1];
                        for (int i = 0; i < columnCount; i++)
                        {
                            columnNames += dataGridView1.Columns[i].HeaderText.ToString() + ",";
                        }
                        outputCsv[0] += columnNames;

                        for (int i = 1; (i - 1) < dataGridView1.Rows.Count; i++)
                        {
                            for (int j = 0; j < columnCount; j++)
                            {
                                outputCsv[i] += dataGridView1.Rows[i - 1].Cells[j].Value.ToString() + ",";
                            }
                        }

                        File.WriteAllLines(knownsavefile, outputCsv, Encoding.UTF8);
                    }
                    else
                    {
                        int columnCount = dataGridView1.Columns.Count;
                        string[] outputCsv = new string[dataGridView1.Rows.Count + 1];
                        for (int i = 1; (i - 1) < dataGridView1.Rows.Count; i++)
                        {
                            for (int j = 0; j < columnCount; j++)
                            {
                                outputCsv[i] += dataGridView1.Rows[i - 1].Cells[j].Value.ToString() + "		";
                            }
                        }

                        File.WriteAllLines(knownsavefile, outputCsv, Encoding.UTF8);


                    }
                    timer2.Start();
                    setingslist[FILE_SAVED] = 1;
                    return true;
                }
                catch (Exception e)
                {
                    timer2.Start();
                    MessageBox.Show("Can not save File! " + knownsavefile + " " + e.Message);
                    return false;
                }
 
            }
            return false; //keep form open
        }
        bool asktosave(bool ask = true)
        {
            if (setingslist[FILE_SAVED] == 1)
            {
                return true;
            }
            else if (ext.all_current_items.Count == 0)
            {
                return true;
            }
            else
            {
                DialogResult res = MessageBox.Show(null, "Do you want to save file before closing?", "save log file", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (res == DialogResult.Yes)
                {
                    this.Text = "Trying to save";
                    return saveFile(ask);

                }
                if (res == DialogResult.No)
                {
                    this.Text = "Closing cus:  no";
                    return true;

                }
                if (res == DialogResult.Cancel)
                {
                    return false;
                }
                return true;//exit
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!asktosave(true))
            {
                e.Cancel = true;//dont close
            }

        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFile();
        }
        
        private void dataGridView1_TextChanged(object sender, EventArgs e)
        {
            setingslist[FILE_SAVED] = 0;

        }

        bool changeopenpath(bool saveoldlist= false)
        {
            using (var fbd = new OpenFileDialog())
            {
                fbd.FileName = "~Directory~.";
                fbd.RestoreDirectory = true;
                fbd.Multiselect = true;
                fbd.CheckFileExists = false;
                fbd.CheckPathExists = false;
                DialogResult result = fbd.ShowDialog();
                string pathFile = fbd.FileName;

                pathFile = pathFile.Replace("~Directory~", "");

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(pathFile))
                {
                    string ppath = Path.GetFullPath(pathFile);
                    try
                    {
                        if(!string.IsNullOrEmpty(Path.GetFileName(ppath)))
                        ppath = ppath.Replace(Path.GetFileName(ppath), "");
                    }
                    catch { }
                    fileSystemWatcher1.Path = ppath;
                   if(!saveoldlist)files_to_watch = new string[] { };
                    toolStripMenuItem2.Enabled = false;
                    if (fbd.FileNames.Length > 0 && !fbd.FileNames[0].Contains("~Directory~"))
                    {
                        toolStripMenuItem2.Enabled = true;
                        fileSystemWatcher1.Path = Path.GetPathRoot(ppath);
                        if (saveoldlist)
                        {
                            List<string> list = new List<string>(files_to_watch);
                            foreach (string ff in fbd.FileNames)
                            {
                                if (!list.Contains(ff))
                                {
                                    list.Add(ff);
                                }
                            }
                            files_to_watch = list.ToArray();
                        }
                        else { 
                            files_to_watch = fbd.FileNames;
                        }
                        toolStripComboBox1.Enabled = true;
                    }
                    else
                    {
                        toolStripComboBox1.Text = "Directory";
                        toolStripComboBox1.Enabled = false;
                    }
                    fileSystemWatcher1.EnableRaisingEvents = true;
                    ext.stopped = false;
                    this.Text = "Enabled " + fileSystemWatcher1.Path;
                    this.startToolStripMenuItem.DropDownItems.Clear();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(fileSystemWatcher1.Path))
            {
                changeopenpath();
            }
            fileSystemWatcher1.EnableRaisingEvents = true;
            ext.stopped = false;
            this.Text = "Enabled " + fileSystemWatcher1.Path;
        }
      
        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fileSystemWatcher1.EnableRaisingEvents = false;
            ext.stopped = true;
            this.Text = "Stopped " + fileSystemWatcher1.Path;
        }

        private void selectFilesToWatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (asktosave(true))
                changeopenpath();
        }

        private void openDirectoryToWatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (asktosave(true))
                changeopenpath();
        }

        private void openFilesToWatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (asktosave(true))
                changeopenpath();
        }

        private void followOldNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripMenuItem2.Text ="Active (" + followOldNameToolStripMenuItem.Text +")";
            setingslist[FILE_REPLACE_NAMES_WATCH] = 0;
        }

        private void followBothFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripMenuItem2.Text = "Active (" + followBothFilesToolStripMenuItem.Text + ")";
            setingslist[FILE_REPLACE_NAMES_WATCH] = 2;
        }

        private void followRenamedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripMenuItem2.Text = "Active (" + followRenamedToolStripMenuItem.Text +")";
            setingslist[FILE_REPLACE_NAMES_WATCH] = 1;
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            changeopenpath(true);
        }

        private void editCurrentFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {   if (files_to_watch.Length > 0)
            {
                toolStripComboBox1.Items.Clear();
                toolStripComboBox1.Text = Path.GetFileName(files_to_watch[0]);
                foreach (string ff in files_to_watch)
                {
                    toolStripComboBox1.Items.Add(Path.GetFileName(ff));
                }
            }
           
        }

   
        bool skipdirupdate = false;
        private void onSkipDirectoryUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            skipdirupdate = !skipdirupdate;
            if(skipdirupdate)
            {
                onSkipDirectoryUpdatesToolStripMenuItem.Text = "Skip directory updates (on)";
            }
            else{
                onSkipDirectoryUpdatesToolStripMenuItem.Text = "Skip directory updates (off)";
            }
        }

        private void dataGridView1_MouseEnter(object sender, EventArgs e)
        {
            ext.autoscroll = false;
        }

        private void dataGridView1_MouseLeave(object sender, EventArgs e)
        {
            ext.autoscroll = true;
        }

   
        private void dataGridView1_MouseMove(object sender, MouseEventArgs e)
        {
           ext.autoscroll = false;

           timer1.Interval = 1000;
            timer1.Stop();
            timer1.Start();

        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            ext.autoscroll = false;
            timer1.Interval = 1000;
            timer1.Stop();
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ext.autoscroll = true;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            ext.autoscroll = false;
            
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            ext.autoscroll = false;
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            ext.autoscroll = false;
        }

      
        private void uutoscrollToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ext.autoscrollperm = !ext.autoscrollperm;
            if (ext.autoscrollperm)
            {
                uutoscrollToolStripMenuItem.Text = "Autoscroll (on)";
            }
            else
            {

                uutoscrollToolStripMenuItem.Text = "Autoscroll (off)";
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (asktosave(true))
            {
                if (changeopenpath())
                {
                    dataGridView1.Rows.Clear();
                    ext.tempitemcount = 0;
                    ext.tempitemcount2 = 0;
                    ext.all_current_items = new List<ext.async_items>();
                    ext.item_add_count = 0;
                    ext.xcount = 0;
                    ext.xcountr = 1;
                }
            }
        }

        private void saveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            saveFile();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFile(true);
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
         Application.Exit();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            if (setingslist[RENAMED] == 1)
            {
                setingslist[RENAMED] = 0;
                toolStripMenuItem3.Text = "&Renames (on)";
            }
            else
            {
                toolStripMenuItem3.Text = "&Renames (off)";
                setingslist[RENAMED] = 1;
            }
        }

        private void toolStripMenuItem1_Click_1(object sender, EventArgs e)
        {
            if (setingslist[CREATED] == 1)
            {
                setingslist[CREATED] = 0;
                toolStripMenuItem1.Text = "&Creates (on)";
            }
            else
            {
               toolStripMenuItem1.Text = "&Creates (off)";
                setingslist[CREATED] = 1;
            }
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            if (setingslist[UPDATED] == 1)
            {
                setingslist[UPDATED] = 0;
                toolStripMenuItem4.Text = "&Updates (on)";
            }
            else
            {
                toolStripMenuItem4.Text = "&Updates (off)";
                setingslist[UPDATED] = 1;
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (setingslist[DELETED] == 1)
            {
                setingslist[DELETED] = 0;
                copyToolStripMenuItem.Text = "&Deletes (on)";
            }
            else
            {
                copyToolStripMenuItem.Text = "&Deletes (off)";
                setingslist[DELETED] = 1;
            }
        }
    
        private void timer2_Tick(object sender, EventArgs e)
        {
            timer2.Stop();
            if (ext.autoscroll || (ext.all_current_items.Count > ext.item_add_count+10 || ext.tmp_antilagg++>=100))
            {
                ext.tmp_antilagg = 0;
                ext.tempitemcount = ext.all_current_items.Count;
                ext.tempitemcount2 = ext.all_current_items.Count-ext.item_add_count;

                for(int i = 0; i< ext.tempitemcount2; i++)
                {
                    try
                    {
                        dataGridView1.Rows.Add(new string[] { ext.all_current_items[(ext.tempitemcount - ext.tempitemcount2) + i].action, ext.all_current_items[(ext.tempitemcount - ext.tempitemcount2) + i].filenam, ext.all_current_items[(ext.tempitemcount - ext.tempitemcount2) + i].text });
                        ext.st = new DataGridViewCellStyle();
                        ext.st.ForeColor = ext.all_current_items[(ext.tempitemcount - ext.tempitemcount2) + i].col;
                        dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[0].Style = ext.st;
                        dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[1].Style = ext.st;
                        dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[2].Style = ext.st;
                        ext.item_add_count++;
                    }
                    catch { }
                }
                Text = " " + ext.all_current_items.Count + " " + ext.item_add_count;

                if (ext.autoscroll && ext.autoscrollperm && dataGridView1.RowCount > 0)
                {
                    dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.RowCount - 1;
                }
                Application.DoEvents();
            }
            timer2.Start();
        }


        private void debugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ext.debug = !ext.debug;
            ext.xcount = 0;
            ext.xcountr = 1;
    }

        private void timer3_Tick(object sender, EventArgs e)
        {
            Application.DoEvents();
        }
    }


    public static class ext
    {
        public class async_items
        {
            public string text, filenam, action;
            public bool dir;
            public Color col;
            public async_items(string _text, string _filenam, Color _co, string _actio, bool _dir)
            {
                text = _text; filenam = _filenam; action = _actio;
                dir = _dir;
                col = _co;
            }

        }

        public static object m_lock = new object();
        public static object b_lock = new object();
        public static bool autoscroll = true;
        public static bool autoscrollperm = true;
        
        public static int item_add_count = 0;
        public static List<async_items> all_current_items = new List<async_items>();
        public static DataGridViewCellStyle st = new DataGridViewCellStyle();

        public static int tempitemcount = 0;
        public static int tempitemcount2 = 0;
        public static int tmp_antilagg = 0;

    public static bool stopped = true;
        //debug
        public static int xcount = 0;
        public static int xcountr = 1;
        public static bool debug = false;
        //debug end

       

        public static void AsyncAddToList(this DataGridView box, string text,string filenam, Color color,string action,bool isdir=false)
        {
            async_items newrow = new async_items(text, filenam, color, action, isdir);
            lock (m_lock)
            {
                all_current_items.Add(newrow);
                xcount++;
            }
        }
    }



}
