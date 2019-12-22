using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace W3LPL
{
    public partial class Form1 : Form
    {
        private W3LPLClient w3lpl = null;
        readonly ConcurrentBag<string> clientQueue = new ConcurrentBag<string>();
        readonly ConcurrentBag<string> w3lplQueue = new ConcurrentBag<string>();
        private QServer server;
        readonly ToolTip tooltip = new ToolTip();
        //BindingList<FilterItem> filterList = new BindingList<FilterItem>();
        //public volatile static int keep;

        /*
    private class FilterItem
    {
        string Text { get; set; }
        public FilterItem(string callsign)
        {
            Text = callsign;
        }
    }
    */
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public Form1()
        {
            InitializeComponent();
            Size = Properties.Settings.Default.Size;
            var tip = "Callsign";
            tooltip.SetToolTip(textBoxCallsign, tip);
            tip = "Local port";
            tooltip.SetToolTip(textBoxPortLocal, tip);
            tip = "Cluster server";
            tooltip.SetToolTip(textBoxClusterServer, tip);
            tip = "Messages in Q";
            tooltip.SetToolTip(labelQDepth, tip);
            tip = "Client status";
            tooltip.SetToolTip(labelStatusQServer, tip);
            tip = "Click to enable, ctrl-click to disable, ctrl-shift-click to delete";
            tooltip.SetToolTip(checkedListBoxReviewedSpotters, tip);
            tip = "Click to see QRZ page, ctrl-click to transfer to Reviewed";
            tooltip.SetToolTip(checkedListBoxNewSpotters, tip);
            tip = "Backup user.config";
            tooltip.SetToolTip(buttonBackup, tip);
            //tooltip.Dispose();
            var reviewedSpotters = Properties.Settings.Default.ReviewedSpotters;
            string[] tokens = reviewedSpotters.Split(';');
            foreach(string arg in tokens)
            {
                if (arg.Length == 0) continue;
                string[] tokens2 = arg.Split(',');
                if (tokens2.Length == 2 && tokens2[0].Length > 0)
                {
                    CheckState myCheck = CheckState.Indeterminate;
                    if (tokens2[1].Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        myCheck = CheckState.Checked;
                    }
                    else if (tokens2[1].Equals("0", StringComparison.InvariantCultureIgnoreCase))
                    {
                        myCheck = CheckState.Unchecked;
                    }
                    checkedListBoxReviewedSpotters.Items.Add(tokens2[0], myCheck);
                }
                else
                {
                    MessageBox.Show("Unknown reviewedSpotters entry '" + arg + "'");
                }
                checkedListBoxReviewedSpotters.Sorted = false;
                checkedListBoxReviewedSpotters.Sorted = true;
                checkedListBoxReviewedSpotters.Sorted = false;
            }
            var newSpotters = Properties.Settings.Default.NewSpotters;
            tokens = newSpotters.Split(';');
            foreach (string arg in tokens)
            {
                if (arg.Length == 0) continue;
                string[] tokens2 = arg.Split(',');
                if (tokens2.Length == 2 && tokens2[0].Length > 0)
                {
                    CheckState myCheck = CheckState.Indeterminate;
                    if (tokens2[1].Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        myCheck = CheckState.Checked;
                    }
                    else if (tokens2[1].Equals("0", StringComparison.InvariantCultureIgnoreCase))
                    {
                        myCheck = CheckState.Unchecked;
                    }
                    checkedListBoxNewSpotters.Items.Add(tokens2[0], myCheck);
                }
                else
                {
                    MessageBox.Show("Unknown reviewedSpotters entry '" + arg + "'");
                }
                checkedListBoxReviewedSpotters.Sorted = false;
                checkedListBoxReviewedSpotters.Sorted = true;
                checkedListBoxReviewedSpotters.Sorted = false;
            }

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public bool Connect()
        {
            buttonStart.Enabled = false;
            if (textBoxCallsign.Text.Length == 0)
            {
                MessageBox.Show("Need callsign!~", "QueueIt");
                buttonStart.Enabled = true;
                return false;
            }
            char[] sep = { ':' };
            var tokens = textBoxClusterServer.Text.Split(sep);
            if (tokens.Length != 2)
            {
                MessageBox.Show("Bad format for cluster server", "W3LPL");
                buttonStart.Enabled = true;
                return false;
            }
            string host = tokens[0];
            int port = Int32.Parse(tokens[1], CultureInfo.InvariantCulture);
            w3lpl = new W3LPLClient(host,port, w3lplQueue);
            try
            {
                richTextBox1.AppendText("Trying to connect\n");
                Application.DoEvents();
                if (w3lpl.Connect(textBoxCallsign.Text, richTextBox1, clientQueue))
                {
                    richTextBox1.AppendText("Connected\n");
                    timer1.Start();
                    buttonStart.Text = "Disconnect";
                    richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    richTextBox1.ScrollToCaret();
                }
                else
                {
                    buttonStart.Text = "Start";
                    richTextBox1.AppendText("Connect failed....hmmm...no answer from W3LPL?\n");
                }
                ReviewedSpottersSave(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                buttonStart.Text = "Connect";
                buttonStart.Enabled = true;
                MessageBox.Show(ex.Message, "QueueIt");
            }

            // Now start the server for Log4OM to get the spots from the queue
            if (Int32.TryParse(textBoxPortLocal.Text, out int qport)) {
                if (server == null)
                {
                    server = new QServer(qport, clientQueue, w3lplQueue);
                    _ = Task.Run(() => server.Start());
                }
            }
            buttonStart.Enabled = true;
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void ButtonStart_Click(object sender, EventArgs e)
        {
            if (buttonStart.Text.Equals("Disconnect"))
            {
                Disconnect();
                buttonStart.Text = "Connect";
                richTextBox1.AppendText("Disconnected\n");
            }
            else
            {
                buttonStart.Text = "Disconnect";
                Connect();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void Timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            if (w3lpl == null) return;
            string msg;
            while((msg = w3lpl.Get()) != null)
            {
                richTextBox1.AppendText(msg);
                //labelQDepth.Text = "Q(" + clientQueue.Count.ToString() + ")";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
                while (richTextBox1.Lines.Length > 1000)
                {
                    richTextBox1.Select(0, richTextBox1.GetFirstCharIndexFromLine(1));
                    richTextBox1.SelectedText = "";
                }
            }
            //float pct = 100;
            //if (w3lpl.totalLines != 0) pct = (int)clientQueue.Count * 100.0f / w3lpl.totalLines;
            string myTime = DateTime.Now.ToLongTimeString();
            labelQDepth.Text = "Q(" + clientQueue.Count.ToString() +") " + myTime;
            //labelQDepth.Text = "Q(" + clientQueue.Count.ToString() + ") "  +;

            
            // See if our filter list needs updating
            foreach(var s in w3lpl.callSuffixList)
            {

                string[] tokens = s.Split(':');
                string justcall = tokens[0];
                if (!checkedListBoxReviewedSpotters.Items.Contains(justcall) && !checkedListBoxNewSpotters.Items.Contains(justcall))
                {
                    if (tokens[1].Equals("SK",StringComparison.InvariantCultureIgnoreCase))
                    {
                    }
                    checkedListBoxNewSpotters.Items.Add(justcall,true);
                }
            }
            timer1.Interval = 1000;
            timer1.Start();
            try
            {
                if (server.IsConnected())
                {
                    System.Drawing.ColorTranslator.FromHtml("#F0F0F0");
                    labelStatusQServer.BackColor = System.Drawing.ColorTranslator.FromHtml("#F0F0F0");
                    labelStatusQServer.Text = "Client connected";
                }
                else
                {
                    if (labelStatusQServer.Text.Equals("Client connected", StringComparison.InvariantCultureIgnoreCase))
                    { // then it disconnected
                        labelStatusQServer.BackColor = System.Drawing.Color.Red;
                        labelStatusQServer.Text = "Client disconnected";
                        WindowState = FormWindowState.Minimized;
                        WindowState = FormWindowState.Normal;
                    }
                    else if (labelStatusQServer.Text.Equals("W3LPL",StringComparison.InvariantCultureIgnoreCase))
                    {
                        labelStatusQServer.BackColor = System.Drawing.ColorTranslator.FromHtml("#F0F0F0");
                        labelStatusQServer.Text = "Ready for client";
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return; // any error should be handled by reconnecting
            }
        }

        private void Disconnect()
        {
            timer1.Stop();
            Thread.Sleep(500);
            w3lpl.Disconnect();
            w3lpl = null;
            server.Stop();
        }
        private void Form1_Activated(object sender, EventArgs e)
        {
            if (textBoxCallsign.Text.Length > 0 && w3lpl == null)
            {
                Connect();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            NewSpottersSave(true);
            Properties.Settings.Default.Save();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void TextBoxCluster_Leave(object sender, EventArgs e)
        {
            char[] sep = { ':' };
            var tokens = textBoxClusterServer.Text.Split(sep);
            if (tokens.Length != 2)
            {
                MessageBox.Show("Bad web link for cluster server\nExpected server:port\ne.g. 'dxc.w3lpl.net:7373", "W3LPL");
                return;
            }
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            if (!Int32.TryParse(tokens[1], out int port))
#pragma warning restore IDE0059 // Unnecessary assignment of a value
            {
                MessageBox.Show("Couldn't parse port number!!");
            }
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            Properties.Settings.Default.Size = this.Size;
            Properties.Settings.Default.Save();
        }

        private void Form1_LocationChanged(object sender, EventArgs e)
        {
            if (Location.X > 0 && Location.Y > 0)
            {
                Properties.Settings.Default.Location = Location;
                Properties.Settings.Default.Save();
            }
            else if (WindowState != FormWindowState.Minimized)
            {
                SetDesktopLocation(0, 0);
            }
        }

        private void LabelQDepth_Click(object sender, EventArgs e)
        {
            w3lpl.CacheClear();
            w3lpl.totalLines = 0;
            w3lpl.totalLinesKept = 0;
        }

        private void CheckedListBoxReviewedSpotters_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool ctrlKey = ModifierKeys.HasFlag(Keys.Control);
            bool shiftKey = ModifierKeys.HasFlag(Keys.Shift);
            bool altKey = ModifierKeys.HasFlag(Keys.Alt);
            int selectedIndex = checkedListBoxReviewedSpotters.SelectedIndex;
            if (selectedIndex == -1) return;
            if (ctrlKey && shiftKey && !altKey)
            {
                checkedListBoxReviewedSpotters.Items.RemoveAt(selectedIndex);
            }
            else if (ctrlKey && !shiftKey && !altKey)
            {
                checkedListBoxReviewedSpotters.SetItemCheckState(selectedIndex, CheckState.Indeterminate);
            }
        }
        private void NewSpottersSave(bool save)
        {
            var spottersChecked = checkedListBoxNewSpotters.CheckedItems;
            string newSpotters = "";

            for (int i = 0; i < spottersChecked.Count; ++i)
            {
                string s = spottersChecked[i].ToString();
                if (checkedListBoxNewSpotters.GetItemCheckState(i) == CheckState.Indeterminate)
                {
                    newSpotters += s + ",2;";
                }
                else
                {
                    newSpotters += s + ",1;";
                }
            }
            if (save)
            {
                Properties.Settings.Default.NewSpotters = newSpotters;
                Properties.Settings.Default.Save();
            }
        }

        private void ReviewedSpottersSave(bool save)
        {
            var spottersChecked = checkedListBoxReviewedSpotters.CheckedItems;
            string reviewedSpotters = "";

            for (int i = 0; i < spottersChecked.Count; ++i) 
            {
                string s = spottersChecked[i].ToString();
                if (checkedListBoxReviewedSpotters.GetItemCheckState(i) == CheckState.Indeterminate)
                {
                    reviewedSpotters +=  s + ",2;";
                }
                else
                {
                    reviewedSpotters += s + ",1;";
                }
            }
            var spottersAll = checkedListBoxReviewedSpotters.Items;
            foreach (string s in spottersAll)
            {
                if (!spottersChecked.Contains(s))
                {
                    reviewedSpotters += s + ",0;";
                }
            }
            if (save)
            {
                Properties.Settings.Default.ReviewedSpotters = reviewedSpotters;
                Properties.Settings.Default.Save();
            }
            if (w3lpl != null) w3lpl.reviewedSpotters = reviewedSpotters;
        }
        private void CheckedListBoxNewSpotters_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool ctrlKey = ModifierKeys.HasFlag(Keys.Control);
            if (ctrlKey) // move to reviewed spotters
            {
                var items2 = checkedListBoxNewSpotters.CheckedItems;
                List<string> sitems = new List<string>();
                foreach (string s in items2)
                {
                    sitems.Add(s);
                }
                foreach (string s in sitems)
                {
                    checkedListBoxNewSpotters.Items.Remove(s);
                    if (!checkedListBoxReviewedSpotters.Items.Contains(s))
                    {
                        checkedListBoxReviewedSpotters.Items.Insert(0, s);
                        checkedListBoxReviewedSpotters.TopIndex = 0;
                    }
                }
                ReviewedSpottersSave(true);
            }
            else // let's look at the QRZ page
            {
                    int selected = checkedListBoxNewSpotters.SelectedIndex;
                    if (selected == -1) return;
                    string callsign = checkedListBoxNewSpotters.Items[selected].ToString();
                    checkedListBoxNewSpotters.SetItemChecked(selected, false);
                    string[] tokens = callsign.Split('-');  // have to remove any suffix like this
                    string url = "https://qrz.com/db/" + tokens[0];
                    System.Diagnostics.Process.Start(url);
                    return;
            }
        }

        private void ButtonBackup_Click(object sender, EventArgs e)
        {
            //System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel level)
            var userConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            string fileName = "user.config_" + DateTime.Now.ToString("yyyy-MM-ddTHHmmss",CultureInfo.InvariantCulture); // almost ISO 8601 format but have to remove colons
            SaveFileDialog myDialog = new SaveFileDialog
            {
                FileName = fileName,
                CheckPathExists = true,
                OverwritePrompt = true
            };
            if (myDialog.ShowDialog() == DialogResult.OK)
            {
                string myFile = myDialog.FileName;
                System.IO.File.Copy(userConfig, myFile,true);
            }
            myDialog.Dispose();
        }

        private void CheckedListBoxReviewedSpotters_MouseUp(object sender, MouseEventArgs e)
        {
            Application.DoEvents();
            ReviewedSpottersSave(true);
        }
    }
}
