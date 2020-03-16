using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace W3LPL
{
    public partial class Form1 : Form
    {
        private static Form1 _instance;
        private W3LPLClient w3lpl = null;
        readonly ConcurrentBag<string> clientQueue = new ConcurrentBag<string>();
        readonly ConcurrentBag<string> w3lplQueue = new ConcurrentBag<string>();
        private QServer server;
        readonly ToolTip tooltip = new ToolTip();
        private QRZ qrz;
        private int badCalls;

        public bool Debug { get; private set; }

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
            _instance = this;
            //richTextBox1.ScrollBars = ScrollBars.Vertical;
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
            tip = "Click to enable, ctrl-click to disable, ctrl-shift-click to delete, ctrl-shift-alt-click to delete all";
            tooltip.SetToolTip(checkedListBoxReviewedSpotters, tip);
            tip = "Click to see QRZ page, ctrl-click to transfer to Reviewed, ctrl-shift-click to clear all";
            tooltip.SetToolTip(checkedListBoxNewSpotters, tip);
            tip = "Backup user.config";
            tooltip.SetToolTip(buttonBackup, tip);
            tip = "Click to copy, ctrl-click to copy&erase";
            tooltip.SetToolTip(buttonCopy, tip);
            tip = "Enabled logging of cached spots, shift-click to toggle debug";
            tooltip.SetToolTip(checkBoxCached, tip);
            tip = "Enabled logging of filtered spots";
            tooltip.SetToolTip(checkBoxFiltered, tip);
            tip = "QRZ password";
            tooltip.SetToolTip(textBoxPassword, tip);
            tip = "QRZ cached/bad";
            tooltip.SetToolTip(labelQRZCache, tip);
            tip = "W3LPL cached";
            tooltip.SetToolTip(labelW3LPLCache, tip);
            tip = "RTTY Offset from spot freq";
            tooltip.SetToolTip(numericUpDownRTTYOffset, tip);
            var reviewedSpotters = Properties.Settings.Default.ReviewedSpotters;
            string[] tokens = reviewedSpotters.Split(';');
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
            //var newSpotters = Properties.Settings.Default.NewSpotters;
            //tokens = newSpotters.Split(';');
            //foreach (string arg in tokens)
            //{
            //    if (arg.Length == 0) continue;
            //    string[] tokens2 = arg.Split(',');
            //    if (tokens2.Length == 2 && tokens2[0].Length > 0)
            //    {
            //       CheckState myCheck = CheckState.Indeterminate;
            //        if (tokens2[1].Equals("1", StringComparison.InvariantCultureIgnoreCase))
            //        {
            //            myCheck = CheckState.Checked;
            //        }
            //        else if (tokens2[1].Equals("0", StringComparison.InvariantCultureIgnoreCase))
            //        {
            //            myCheck = CheckState.Unchecked;
            //        }
            //        checkedListBoxNewSpotters.Items.Add(tokens2[0], myCheck);
            //    }
            //    else
            //    {
            //        MessageBox.Show("Unknown reviewedSpotters entry '" + arg + "'");
            //    }
            //    checkedListBoxReviewedSpotters.Sorted = false;
            //    checkedListBoxReviewedSpotters.Sorted = true;
            //    checkedListBoxReviewedSpotters.Sorted = false;
            //}

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]

        public string TextStatus
        {
            get { return labelStatusQServer.Text; }
            set { labelStatusQServer.Text = value; }
        }

        public Color TextStatusColor
        {
            get { return labelStatusQServer.BackColor; }
            set { labelStatusQServer.BackColor = value;  }
        }
        public static Form1 Instance { get { return _instance;} }

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
            //if (!textBoxClusterServer.Text.Contains("dxc.w3lpl.net:7373"))
            //{
            //    MessageBox.Show("Not dxc.w3lpl.net:7373 in server box?");
            //    return false;
            //}
            var tokens = textBoxClusterServer.Text.Split(sep);
            if (tokens.Length != 2)
            {
                MessageBox.Show("Bad format for cluster server", "W3LPL");
                buttonStart.Enabled = true;
                return false;
            }
            string host = tokens[0];
            int port = Int32.Parse(tokens[1], CultureInfo.InvariantCulture);
            if (qrz != null) qrz.Dispose();
            qrz = new QRZ(textBoxCallsign.Text, textBoxPassword.Text, textBoxCacheLocation.Text);
            if (qrz == null || qrz.isOnline == false)
            {
                if (qrz != null) richTextBox1.AppendText("QRZ: " + qrz.xmlError + "\n");
                return false;
            }
            w3lpl = new W3LPLClient(host, port, w3lplQueue, qrz);
            w3lpl.rttyOffset = (float)numericUpDownRTTYOffset.Value;
            try
            {
                richTextBox1.AppendText("Trying to connect\n");
                Application.DoEvents();
                if (w3lpl.Connect(textBoxCallsign.Text, richTextBox1, clientQueue))
                {
                    //richTextBox1.AppendText("Connected\n");
                    timer1.Start();
                    buttonStart.Text = "Disconnect";
                    //richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    //richTextBox1.ScrollToCaret();
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
            if (Int32.TryParse(textBoxPortLocal.Text, out int qport))
            {
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
                bool result = Connect();
                if (result == false)
                {
                    buttonStart.Text = "Connect";
                    richTextBox1.AppendText("Disconnected due to error\n");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void Timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            if (w3lpl == null) return;
            string msg;
            while ((msg = w3lpl.Get(out bool cachedQRZ)) != null)
            {
                char[] delims = { '\n' };
                string[] lines = msg.Split(delims);
                foreach (string s in lines)
                {
                    if (s.Length == 0) continue;
                    string ss = s;
                    Color myColor = Color.Black;
                    while (richTextBox1.Lines.Length > 1000)
                    {
                        richTextBox1.Select(0, richTextBox1.GetFirstCharIndexFromLine(100));
                        //richTextBox1.Cut();
                        richTextBox1.SelectedText = "";
                    }
                    System.Drawing.Point p = richTextBox1.PointToClient(Control.MousePosition);
                    System.Drawing.Rectangle client = richTextBox1.ClientRectangle;
                    client.Width += 30;
                    if (!client.Contains(p))
                    {
                        if (s.Length < 3) continue;
                        // %% means qrz is cached valid call
                        // !! means spotter is filtered out
                        // ** means W3LPL is cached
                        // ## means bad call 
                        // #* means bad call cached
                        string firstFive = ss.Substring(0, 5);
                        bool qrzError = firstFive.Equals("ZZ de",StringComparison.InvariantCultureIgnoreCase);
                        bool badCall = firstFive.Equals("## de",StringComparison.InvariantCultureIgnoreCase);
                        bool badCallCached = firstFive.Equals("#* de", StringComparison.InvariantCultureIgnoreCase);
                        bool filtered = firstFive.Equals("!! de",StringComparison.InvariantCultureIgnoreCase);
                        bool w3lplCached = firstFive.Equals("** de", StringComparison.InvariantCultureIgnoreCase);
                        bool dxline = firstFive.Equals("Dx de",StringComparison.InvariantCultureIgnoreCase);
                        if (qrzError)
                        {
                            //this.WindowState = FormWindowState.Minimized;
                            //this.Show();
                            //this.WindowState = FormWindowState.Normal;
                        }
                        if (filtered && !checkBoxFiltered.Checked) continue;
                        else if (w3lplCached && !checkBoxCached.Checked) continue;
                        else if (!filtered && !w3lplCached && !dxline && !badCall && !badCallCached)
                        {
                            RichTextBoxExtensions.AppendText(richTextBox1, ss, myColor);
                            richTextBox1.SelectionStart = richTextBox1.Text.Length;
                            richTextBox1.ScrollToCaret();
                            Application.DoEvents();
                            continue;
                        }
                        // We should have display all non-spot lines in black above 
                        // So we can set our qrz cache color now for all spots
                        if (badCall)
                        {
                            myColor = Color.Red;
                            ++badCalls;
                        }
                        else if (badCallCached)
                        {
                            myColor = Color.IndianRed;
                            ++badCalls;
                        }
                        else if (cachedQRZ)
                        {
                            myColor = Color.Green;
                        }
                        else if (s.Contains(textBoxCallsign.Text))
                        {
                            myColor = Color.DarkBlue;
                            if (Debug)
                            {
                                File.AppendAllText("C:/Temp/" + textBoxCallsign.Text, qrz.xml);
                            }
                        }
                        else
                        {
                            myColor = Color.Orange;
                        }
                        labelQRZCache.Text = "" + qrz.cacheQRZ.Count + "/" + badCalls;
                        labelW3LPLCache.Text = "" + w3lpl.cacheSpottedCalls.Count;
                        ss = ss.Replace("\r", "");
                        ss = ss.Replace("\n", "");
                        RichTextBoxExtensions.AppendText(richTextBox1, ss + "\n", myColor);
                        richTextBox1.SelectionStart = richTextBox1.Text.Length;
                        richTextBox1.ScrollToCaret();
                        Application.DoEvents();
                    }
                }
            }
            TimeSpan tzone = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            string myTime = DateTime.UtcNow.ToString("HH:mm:ss");

            labelQDepth.Text = "Q(" + clientQueue.Count.ToString() + ") " + myTime + tzone.Hours.ToString("+00;-00;+00");


            // See if our filter list needs updating
            foreach (var s in w3lpl.callSuffixList)
            {

                string[] tokens = s.Split(':');
                string justcall = tokens[0];
                if (!checkedListBoxReviewedSpotters.Items.Contains(justcall) && !checkedListBoxNewSpotters.Items.Contains(justcall))
                {
                    //if (tokens[1].Equals("SK",StringComparison.InvariantCultureIgnoreCase))
                    //{
                    //}
                    checkedListBoxNewSpotters.Items.Add(justcall, true);
                }
            }
            timer1.Interval = 1000;
            timer1.Start();
            try
            {
                if (server.IsConnected())
                {
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
                    else if (labelStatusQServer.Text.Equals("W3LPL", StringComparison.InvariantCultureIgnoreCase))
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void Form1_Activated(object sender, EventArgs e)
        {
            if (textBoxCallsign.Text.Length > 0 && w3lpl == null && textBoxPassword.Text.Length > 0)
            {
                bool result = Connect();
                if (result == false)
                {
                    buttonStart.Enabled = true;
                    buttonStart.Text = "Start";
                    richTextBox1.AppendText("Disconnected due to error\n");
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //qrz.CacheSave(textBoxCacheLocation.Text);
            qrz.CacheSave("C:\\Temp\\qrzcache.txt");
            ReviewedSpottersSave(true);
            Properties.Settings.Default.Password = textBoxPassword.Text;
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
            if (ctrlKey && shiftKey && altKey)
            {
                checkedListBoxReviewedSpotters.Items.Clear();
                checkedListBoxReviewedSpotters.Items.Add("4U1UN", true);
                w3lpl.callSuffixList.Clear();
            }
            if (ctrlKey && shiftKey && !altKey)
            {
                checkedListBoxReviewedSpotters.Items.RemoveAt(selectedIndex);
            }
            else if (ctrlKey && !shiftKey && !altKey)
            {
                checkedListBoxReviewedSpotters.SetItemCheckState(selectedIndex, CheckState.Indeterminate);
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
                    reviewedSpotters += s + ",2;";
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

            if (checkedListBoxNewSpotters.SelectedItem == null)
                return;
            bool ctrlKey = ModifierKeys.HasFlag(Keys.Control);
            bool shiftKey = ModifierKeys.HasFlag(Keys.Shift);
            if (ctrlKey && shiftKey)
            {
                checkedListBoxNewSpotters.Items.Clear();
                w3lpl.callSuffixList.Clear();
            }
            else if (ctrlKey) // move to reviewed spotters
            {
                string curItem = checkedListBoxNewSpotters.SelectedItem.ToString();
                int index = checkedListBoxNewSpotters.FindString(curItem);
                if (!checkedListBoxReviewedSpotters.Items.Contains(curItem))
                {
                    checkedListBoxReviewedSpotters.Items.Insert(0, curItem);
                    checkedListBoxReviewedSpotters.TopIndex = 0;
                    checkedListBoxNewSpotters.Items.RemoveAt(index);
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
            string fileName = "user.config_" + DateTime.Now.ToString("yyyy-MM-ddTHHmmss", CultureInfo.InvariantCulture); // almost ISO 8601 format but have to remove colons
            SaveFileDialog myDialog = new SaveFileDialog
            {
                FileName = fileName,
                CheckPathExists = true,
                OverwritePrompt = true
            };
            if (myDialog.ShowDialog() == DialogResult.OK)
            {
                string myFile = myDialog.FileName;
                System.IO.File.Copy(userConfig, myFile, true);
            }
            myDialog.Dispose();
        }

        private void CheckedListBoxReviewedSpotters_MouseUp(object sender, MouseEventArgs e)
        {
            Application.DoEvents();
            ReviewedSpottersSave(true);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            bool ctrlKey = ModifierKeys.HasFlag(Keys.Control);
            if (richTextBox1.Text.Length > 0)
            {
                Clipboard.SetText(richTextBox1.Text);
                if (ctrlKey)
                {
                    richTextBox1.Clear();
                }
                else
                {
                    richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    richTextBox1.SelectionLength = 0;
                    richTextBox1.ScrollToCaret();
                }
            }
        }

        private void TextBoxClusterServer_TextChanged(object sender, EventArgs e)
        {

        }

        private void CheckBoxCached_CheckedChanged(object sender, EventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Shift))
            {
                Debug = !Debug;
                w3lpl.debug = Debug;
                qrz.debug = Debug;
                richTextBox1.AppendText("Debug = " + Debug +"\n");
            }
        }

        private void numericUpDownRTTYOffset_ValueChanged(object sender, EventArgs e)
        {
            w3lpl.rttyOffset = (float)numericUpDownRTTYOffset.Value;
        }
    }
    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            if (box == null) throw new ArgumentNullException(nameof(box));
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }
}
