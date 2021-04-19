﻿using System;
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

namespace DXClusterUtil
{
    public partial class Form1 : Form
    {
        private static Form1 _instance;
        private ClusterClient clusterClient = null;
        readonly ConcurrentBag<string> clientQueue = new ConcurrentBag<string>();
        readonly ConcurrentBag<string> spotQueue = new ConcurrentBag<string>();
        QServer server;
        readonly ToolTip tooltip = new ToolTip();
        private QRZ qrz;
        private readonly string pathQRZCache = Environment.ExpandEnvironmentVariables("%TEMP%\\qrzache.txt");
        private int badCalls;
        bool startupConnect = true;
        public bool Debug { get; private set; }
        public int TimeForDump
        {
            get { return server.TimeForDump; }
            set { server.TimeForDump = value; }
        }
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
            Icon = Properties.Resources.filter3;
            _instance = this;
            //richTextBox1.ScrollBars = ScrollBars.Vertical;
            Size = Properties.Settings.Default.Size;
            var tip = "!! means spotter is filtered out\n * *means W3LPL is cached\n## means bad call\n#* means bad call cached\nGreen is call sign good and cached\nOrange is call sign good and new QRZ query\nRed is bad call\nDark Red is bad call cached";
            tooltip.SetToolTip(richTextBox1, tip);
            tip = "Callsign for QRZ login";
            tooltip.SetToolTip(textBoxCallsign, tip);
            tip = "QRZ password";
            tooltip.SetToolTip(textBoxPassword, tip);
            tip = "Local port for client to connect to";
            tooltip.SetToolTip(textBoxPortLocal, tip);
            tip = "Seconds after top of minute to dump spots";
            tooltip.SetToolTip(comboBoxTimeForDump, tip);
            tip = "Cluster server host:port";
            tooltip.SetToolTip(textBoxClusterServer, tip);
            tip = "Messages in Q(#) UTC Time(Local Offset)";
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
            tip = "QRZ cached/bad";
            tooltip.SetToolTip(labelQRZCache, tip);
            tip = "Cluster cached";
            tooltip.SetToolTip(labelClusterCache, tip);
            tip = "RTTY Offset from spot freq";
            tooltip.SetToolTip(numericUpDownRTTYOffset, tip);
            tip = "Click to add, shift-click to delete";
            tooltip.SetToolTip(listBoxIgnore, tip);
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
            var ignore = Properties.Settings.Default.Ignore;
            foreach (string token in ignore.Split('|'))
            {
                if (token.Length > 0)
                {
                    listBoxIgnore.Items.Add(token);
                }
            }
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]

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
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                MessageBox.Show("Need callsign!~", "QueueIt");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                buttonStart.Enabled = true;
                return false;
            }
            char[] sep = { ':' };
            var tokens = textBoxClusterServer.Text.Split(sep);
            if (tokens.Length > 0 && tokens.Length != 2)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                MessageBox.Show("Bad format for cluster server", "ClusterServer");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                buttonStart.Enabled = true;
                return false;
            }
            string host = tokens[0];
            int port = Int32.Parse(tokens[1], CultureInfo.InvariantCulture);
            if (qrz != null) qrz.Dispose();
            qrz = new QRZ(textBoxCallsign.Text, textBoxPassword.Text);
            if (qrz == null || qrz.isOnline == false)
            {
                if (qrz != null) richTextBox1.AppendText("QRZ: " + qrz.xmlError + "\n");
                return false;
            }
            clusterClient = new ClusterClient(host, port, spotQueue, qrz)
            {
                rttyOffset = (float)numericUpDownRTTYOffset.Value
            };
            try
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                richTextBox1.AppendText("Trying to connect\n");
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.ScrollToCaret();
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                Application.DoEvents();
                if (clusterClient.Connect(textBoxCallsign.Text, richTextBox1, clientQueue))
                {
                    //richTextBox1.AppendText("Connected\n");
                    timer1.Start();
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    buttonStart.Text = "Disconnect";
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                              //richTextBox1.SelectionStart = richTextBox1.Text.Length;
                              //richTextBox1.ScrollToCaret();
                }
                else
                {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    buttonStart.Text = "Start";
                    richTextBox1.AppendText("Connect failed....hmmm...no answer from cluster server?\n");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                }
                ReviewedSpottersSave(false);
                clusterClient.listBoxIgnore = listBoxIgnore;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                buttonStart.Enabled = true;
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                buttonStart.Text = "Connect";
                MessageBox.Show(ex.Message, "QueueIt");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            // Now start the server for Log4OM to get the spots from the queue
            if (Int32.TryParse(textBoxPortLocal.Text, out int qport))
            {
                if (server == null)
                {
                    server = new QServer(qport, clientQueue, spotQueue);
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
            if (clusterClient == null) return;
            string msg;
            TimeForDump = Convert.ToInt32(comboBoxTimeForDump.SelectedIndex+1);
            while ((msg = clusterClient.Get(out bool cachedQRZ)) != null)
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
                        // ** means cluster spot is cached
                        // ## means bad call 
                        // #* means bad call cached
                        string firstFive = ss.Substring(0, 5);
                        bool qrzError = firstFive.Equals("ZZ de",StringComparison.InvariantCultureIgnoreCase);
                        bool badCall = firstFive.Equals("## de",StringComparison.InvariantCultureIgnoreCase);
                        bool badCallCached = firstFive.Equals("#* de", StringComparison.InvariantCultureIgnoreCase);
                        bool filtered = firstFive.Equals("!! de",StringComparison.InvariantCultureIgnoreCase);
                        bool clusterCached = firstFive.Equals("** de", StringComparison.InvariantCultureIgnoreCase);
                        bool dxline = firstFive.Equals("Dx de",StringComparison.InvariantCultureIgnoreCase);
                        if (qrzError)
                        {
                            //this.WindowState = FormWindowState.Minimized;
                            //this.Show();
                            //this.WindowState = FormWindowState.Normal;
                        }
                        if (filtered && !checkBoxFiltered.Checked) continue;
                        else if (clusterCached && !checkBoxCached.Checked) continue;
                        else if (!filtered && !clusterCached && !dxline && !badCall && !badCallCached)
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
                            }
                        }
                        else
                        {
                            myColor = Color.Orange;
                        }
                        labelQRZCache.Text = "" + qrz.cacheQRZ.Count + "/" + badCalls;
                        labelClusterCache.Text = "" + clusterClient.cacheSpottedCalls.Count;
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

            labelQDepth.Text = "Q(" + clientQueue.Count.ToString() + ") " + myTime + "(" + tzone.Hours.ToString("+00;-00;+00") + ")";


            // See if our filter list needs updating
            foreach (var s in clusterClient.callSuffixList)
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
                    else if (labelStatusQServer.Text.Equals("Client", StringComparison.InvariantCultureIgnoreCase))
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
            clusterClient.Disconnect();
            clusterClient = null;
            server.Stop();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void Form1_Activated(object sender, EventArgs e)
        {
            if (startupConnect && textBoxCallsign.Text.Length > 0 && clusterClient == null && textBoxPassword.Text.Length > 0)
            {
                bool result = Connect();
                if (result == false)
                {
                    buttonStart.Enabled = true;
                    buttonStart.Text = "Start";
                    richTextBox1.AppendText("Disconnected due to error\n");
                }
                startupConnect = false; // don't do it again
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //qrz.CacheSave(textBoxCacheLocation.Text);
            if (qrz != null) qrz.CacheSave(pathQRZCache);
            ReviewedSpottersSave(true);
            string group = "";
            foreach (string token in listBoxIgnore.Items)
            {
                group += token + "|";
            }
            Properties.Settings.Default.Ignore = group;
            Properties.Settings.Default.Password = textBoxPassword.Text;
            Properties.Settings.Default.Location = this.Location;
            Properties.Settings.Default.Size = this.Size;
            Properties.Settings.Default.Save();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void TextBoxCluster_Leave(object sender, EventArgs e)
        {
            char[] sep = { ':' };
            var tokens = textBoxClusterServer.Text.Split(sep);
            if (tokens.Length > 0 && tokens.Length != 2)
            {
                MessageBox.Show("Bad web link for cluster server\nExpected server:port","DxClusterUtil");
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


        private void LabelQDepth_Click(object sender, EventArgs e)
        {
            clusterClient.CacheClear();
            clusterClient.totalLines = 0;
            clusterClient.totalLinesKept = 0;
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
                clusterClient.callSuffixList.Clear();
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
            if (clusterClient != null) clusterClient.reviewedSpotters = reviewedSpotters;
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
                clusterClient.callSuffixList.Clear();
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
                clusterClient.debug = Debug;
                qrz.debug = Debug;
                richTextBox1.AppendText("Debug = " + Debug +"\n");
            }
        }

        private void NumericUpDownRTTYOffset_ValueChanged(object sender, EventArgs e)
        {
            clusterClient.rttyOffset = (float)numericUpDownRTTYOffset.Value;
        }

        private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }
        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor |= AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            form.Dispose();
            return dialogResult;
        }

        private void ListBox1_Click(object sender, EventArgs e)
        {
            bool shiftKey = ModifierKeys.HasFlag(Keys.Shift);
            if (shiftKey)
            {
                listBoxIgnore.Items.Remove(listBoxIgnore.SelectedItem);
                return;
            }
            String value = "";
            if (InputBox("Add Ignore Callsign", "Prompt", ref value) == DialogResult.OK)
            {
                _ = listBoxIgnore.Items.Add(value.ToUpper(new CultureInfo("en-US", false)));
            }
        }

        private void ListBoxIgnore_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void ComboBoxTimeForDump_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox box = (ComboBox)sender;
#pragma warning disable CA1305 // Specify IFormatProvider
            TimeForDump = Convert.ToInt32(box.SelectedText);
#pragma warning restore CA1305 // Specify IFormatProvider
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Location = Properties.Settings.Default.Location;
            this.Size = Properties.Settings.Default.Size;
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
