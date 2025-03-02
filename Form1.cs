﻿using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using Xamarin.Essentials;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.ComponentModel;
using Microsoft.Extensions.Hosting;
using System.Web.Services.Description;

namespace DXClusterUtil
{
    public partial class Form1 : Form
    {
        private static Form1? _instance;
        private ClusterClient? clusterClient;
        readonly ConcurrentBag<string> clientQueue = [];
        readonly ConcurrentBag<string> spotQueue = [];
        QServer? server;
        readonly ToolTip tooltip = new() { ShowAlways = true };
        private QRZ? qrz;
        private readonly string pathQRZCache = Environment.ExpandEnvironmentVariables("%TEMP%\\qrzcache.txt");
        private int badCalls;
        bool startupConnect = true;
        System.Drawing.Color qServerBackColor;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Debug { get; private set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int TimeIntervalAfter
        {
            get { if (server is not null) return server.TimeIntervalAfter; else return 0; }
            set { if (server is not null) server.TimeIntervalAfter = value; Properties.Settings.Default.TimeIntervalAfter = value; }
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int TimeIntervalForDump
        {
            get { if (server is not null) return server.TimeInterval; else return 60; }
            set { if (server is not null) server.TimeInterval = value; Properties.Settings.Default.TimeIntervalForDump = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool USA
        {
            get { return checkBoxUSA.Checked; }
            set { checkBoxUSA.Checked = value; Properties.Settings.Default.USA = value; }
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
            var tip = "!! means spotter is filtered out\n * *means spot is duplicate\n## means bad call\n#* means bad call cached\n<< means < CW db cutoff\nGreen is call sign good and QRZ is cached\nOrange is call sign good and new QRZ query\nRed is bad call\nDark Red is bad call cached\nBlue is your own spot";
            tooltip.AutoPopDelay = 36000;
            tooltip.InitialDelay = 1000;
            tooltip.AutoPopDelay = 10000;
            tooltip.ReshowDelay = 2000;
            tooltip.ShowAlways = true;
            tooltip.SetToolTip(richTextBox1, tip);

            tip = "Callsign for QRZ login";
            tooltip.SetToolTip(textBoxCallsign, tip);
            tip = "QRZ password";
            tooltip.SetToolTip(textBoxPassword, tip);
            tip = "Local port for client to connect to";
            tooltip.SetToolTip(textBoxPortLocal, tip);
            tip = "Interval to dump spots every X seconds";
            tooltip.SetToolTip(comboBoxTimeIntervalForDump, tip);
            tip = "Seconds after interval to dump spots";
            tooltip.SetToolTip(comboBoxTimeIntervalAfter, tip);
            tip = "Cluster server host:port";
            tooltip.SetToolTip(textBoxClusterServer, tip);
            tip = "Messages in Q(#) UTC Time(Local Offset)";
            tooltip.SetToolTip(labelQDepth, tip);
            tip = "Client status";
            tooltip.SetToolTip(labelStatusQServer, tip);
            tip = "Click to enable/disable\nShift-click for QRZ\nAlt-click to sort\nCtrl-alt-click to move to Ignore list\nCtrl-shift-click to delete\nCtrl-shift-alt-click to delete all";
            tooltip.SetToolTip(checkedListBoxReviewedSpotters, tip);
            tip = "New Spotters needing review\nClick to enable/disable\nShift-click for QRZ\nAlt-click to sort\nCtrl-Alt-click to move to Ignore list\nCtrl-shift-click to delete\nCtrl-click to transfer to Reviewed\nCtrl-shift-alt-click to delete all";
            tooltip.SetToolTip(checkedListBoxNewSpotters, tip);
            tip = "Will ignore spotters or spots\nClick to add\nShift-click for QRZ\nAlt-click to sort\nCtrl-shift-click to delete\nCtrl-shift-alt-click to delete all";
            tooltip.SetToolTip(listBoxIgnoredSpotters, tip);
            tip = "Backup user.config";
            tooltip.SetToolTip(buttonBackup, tip);
            tip = "Click to copy, ctrl-click to copy&erase";
            tooltip.SetToolTip(buttonCopy, tip);
            tip = "Enable to see duplicate spots below, shift-click to toggle debug";
            tooltip.SetToolTip(checkBoxCached, tip);
            tip = "Enable to see filtered spots";
            tooltip.SetToolTip(checkBoxFiltered, tip);
            tip = "QRZ cached/bad";
            tooltip.SetToolTip(labelQRZCache, tip);
            tip = "Dups cached";
            tooltip.SetToolTip(labelClusterCache, tip);
            tip = "RTTY Offset from spot freq";
            tooltip.SetToolTip(numericUpDownRTTYOffset, tip);
            tip = "Enable to filter out USA spots";
            tooltip.SetToolTip(checkBoxUSA, tip);
            tip = "Q(Depth), UTC Time(local time to UTC offset)";
            tooltip.SetToolTip(labelQDepth, tip);
            tip = "CW Skimmer Minimum dB";
            tooltip.SetToolTip(numericUpDownCwMinimum, tip);
            tip = "Ctrl-click erases QRZ good and bad cache";
            tooltip.SetToolTip(labelQRZCache, tip);
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
                    listBoxIgnoredSpotters.Items.Add(token);
                }
            }
            timer2.Start();
        }

        //[System.Diagnostics.C(odeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        //[System.Diagnostics.C(odeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]

        public string TextStatus
        {
            get { return labelStatusQServer.Text; }
            set { labelStatusQServer.Text = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color TextStatusColor
        {
            get { return labelStatusQServer.BackColor; }
            set { labelStatusQServer.BackColor = value; }
        }
        public static Form1 Instance
        {
            get
            {
                if (_instance == null)
                {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    throw new InvalidOperationException("Instance is not initialized.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                }
                return _instance;
            }
        }

        static void AddToLog(RichTextBox log, string message, Color myColor)
        {
            log.Select(0, 0);
            log.SelectionColor = myColor;
            log.SelectedText = message;
            if (!log.Focused)
            {
                log.SelectionStart = 0;
                log.ScrollToCaret();
            }
            var maxLines = 200;
            if (log.Lines.Length > maxLines)
            {
                int charIndex = log.GetFirstCharIndexFromLine(maxLines); // Get the first character of the 25th line
                // Select all text after the 25th line and remove it
                log.Select(charIndex, log.TextLength - charIndex);
                log.SelectedText = ""; // Remove the selected text while preserving formatting                Application.DoEvents();
            }
            Application.DoEvents();
        }

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
            char[] sep = [':'];
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
            qrz?.Dispose();
            //if (checkBoxQRZ.Checked)
            {
                qrz = new QRZ(textBoxCallsign.Text, textBoxPassword.Text);
                if (qrz == null || qrz.isOnline == false)
                {
                    if (qrz != null) AddToLog(richTextBox1, "QRZ: " + qrz.xmlError + "\n", Color.Black);
                    return false;
                }
            }
            clusterClient = new ClusterClient(host, port, spotQueue, qrz)
            {
                rttyOffset = (float)numericUpDownRTTYOffset.Value,
                numericUpDownCwMinimum = (int)numericUpDownCwMinimum.Value
            };
            try
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                AddToLog(richTextBox1, "Trying to connect\n", Color.Black);
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.ScrollToCaret();
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                Application.DoEvents();
                if (clusterClient.Connect(textBoxCallsign.Text, richTextBox1, clientQueue))
                {
                    clusterClient.filterUSA = checkBoxUSA.Checked;
                    //AddToLog(richTextBox1,"Connected\n");
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
                    AddToLog(richTextBox1, "Connect failed....hmmm...no answer from cluster server?\n", Color.Black);
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                }
                ReviewedSpottersSave(false);
                clusterClient.listBoxIgnore = listBoxIgnoredSpotters;
                clusterClient.checkedListBoxReviewed = checkedListBoxReviewedSpotters;
                clusterClient.checkListBoxNewSpotters = checkedListBoxNewSpotters;
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
#pragma warning disable CA1305 // Specify IFormatProvider
                    TimeIntervalForDump = Convert.ToInt32(comboBoxTimeIntervalForDump.SelectedItem);
                    TimeIntervalAfter = Convert.ToInt32(comboBoxTimeIntervalAfter.SelectedItem);
#pragma warning restore CA1305 // Specify IFormatProvider
                }
            }
            buttonStart.Enabled = true;
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void ButtonStart_Click(object? sender, EventArgs? e)
        {
            if (buttonStart.Text.Equals("Disconnect"))
            {
                Disconnect();
                buttonStart.Text = "Connect";
                AddToLog(richTextBox1, "Disconnected\n", Color.Black);
            }
            else
            {
                buttonStart.Text = "Disconnect";
                bool result = Connect();
                if (result == false)
                {
                    buttonStart.Text = "Connect";
                    buttonStart.Enabled = true;
                    AddToLog(richTextBox1, "Disconnected due to error\n", Color.Black);
                }
            }
        }
        public static bool TryParseSignalStrength(string input, out int value)
        {
            // Initialize out parameter
            value = 0;

            // Match the "CW [number] dB" pattern
            var match = MyRegexCW().Match(input); // CW\s\d+\sdB

            if (match.Success && int.TryParse(match.Groups[1].Value, out value))
            {
                return true;
            }

            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void Timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            if (clusterClient == null) return;
            string? msg;
            //TimeForDump = Convert.ToInt32(comboBoxTimeForDump.SelectedIndex+1);
            while ((msg = clusterClient.Get(out bool cachedQRZ, richTextBox1, textBoxCallsign.Text)) != null)
            {
                char[] delims = ['\n'];
                string[] lines = msg.Split(delims);
                foreach (string s in lines)
                {
                    if (s.Length == 0) continue;
                    string ss = s;
                    Color myColor = Color.Black;
                    if (richTextBox1.ReadOnly == true) richTextBox1.ReadOnly = false;
                    System.Drawing.Point p;
                    try
                    {
                        p = richTextBox1.PointToClient(Control.MousePosition);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        return;
                    }
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
                        string firstFive = ss[..5];
                        bool qrzError = firstFive.Equals("ZZ de", StringComparison.InvariantCultureIgnoreCase);
                        bool badCall = firstFive.Equals("## de", StringComparison.InvariantCultureIgnoreCase);
                        bool badCallCached = firstFive.Equals("#* de", StringComparison.InvariantCultureIgnoreCase);
                        bool filtered = firstFive.Equals("!! de", StringComparison.InvariantCultureIgnoreCase);
                        bool clusterCached = firstFive.Equals("** de", StringComparison.InvariantCultureIgnoreCase);
                        bool dxline = firstFive.Equals("Dx de", StringComparison.InvariantCultureIgnoreCase);
                        bool ignored = ss.Contains("Ignoring", StringComparison.InvariantCulture);
                        bool tooWeak = firstFive.Equals("<< de", StringComparison.InvariantCultureIgnoreCase);
                        if (qrzError)
                        {
                            //this.WindowState = FormWindowState.Minimized;
                            //this.Show();
                            //this.WindowState = FormWindowState.Normal;
                        }
                        if ((filtered || ignored || tooWeak) && !checkBoxFiltered.Checked) continue;
                        else if (clusterCached && !checkBoxCached.Checked) continue;
                        else if (ss.Contains(textBoxCallsign.Text + ":", StringComparison.InvariantCultureIgnoreCase))
                        {
                            AddToLog(richTextBox1, ss, myColor);
                            //log4omQueue?.Add(swork + "\r\n");
                            continue;
                        }
                        else if (ss[0] == ' ')
                        {
                            AddToLog(richTextBox1, ss + " DX Line???", myColor);
                            continue;
                        }
                        else if (!filtered && !clusterCached && !dxline && !badCall && !badCallCached && !tooWeak)
                        {
                            AddToLog(richTextBox1, ss, myColor);
                            //RichTextBoxExtensions.AppendText(richTextBox1, ss, myColor);
                            //richTextBox1.SelectionStart = richTextBox1.Text.Length;
                            //richTextBox1.ScrollToCaret();
                            Application.DoEvents();
                            continue;
                        }
                        // We should have display all non-spot lines in black above 
                        // So we can set our qrz cache color now for all spots
                        if (badCall)
                        {
                            myColor = Color.Red;
                            ++badCalls;
                            ss += " bad call";
                        }
                        else if (badCallCached)
                        {
                            myColor = Color.IndianRed;
                            ++badCalls;
                        }
                        else if (s.Contains(textBoxCallsign.Text, StringComparison.InvariantCulture))
                        {
                            myColor = Color.Blue;
                            ss += " " + textBoxCallsign.Text + " stuff";
                        }
                        else if (tooWeak)
                        {
                            myColor = Color.DarkGoldenrod;
                            ss += " too weak";
                        }
                        else if (ss[..2] != "DX" && !ss.Contains("review", StringComparison.OrdinalIgnoreCase))
                        {
                            myColor = Color.MediumBlue;
                            ss += " filtered";
                        }
                        else if (cachedQRZ)
                        {
                            myColor = Color.Green;
                            ss += " cached QRZ";
                        }
                        else
                        {
                            myColor = Color.Black;
                            if (!ss.Contains("reviewed", StringComparison.OrdinalIgnoreCase)) ss += " new QRZ";
                        }
                        labelQRZCache.Text = "" + qrz?.cacheQRZ.Count + "/" + badCalls;
                        labelClusterCache.Text = "" + clusterClient.cacheSpottedCalls.Count;
                        ss = ss.Replace("\r", "", StringComparison.InvariantCulture);
                        ss = ss.Replace("\n", "", StringComparison.InvariantCulture);
                        AddToLog(richTextBox1, ss + "\n", myColor);
                        //RichTextBoxExtensions.AppendText(richTextBox1, ss + "\n", myColor);
                        //richTextBox1.SelectionStart = richTextBox1.Text.Length;
                        //richTextBox1.ScrollToCaret();
                        Application.DoEvents();
                    }
                }
            }


            // See if our filter list needs updating
            foreach (var s in clusterClient.callSuffixList)
            {
                string[] tokens = s.Split(':');
                string justcall = tokens[0];
                if (!checkedListBoxReviewedSpotters.Items.Contains(justcall) && !checkedListBoxNewSpotters.Items.Contains(justcall) && !listBoxIgnoredSpotters.Items.Contains(justcall))
                {
                    //if (tokens[1].Equals("SK",StringComparison.InvariantCultureIgnoreCase))
                    //{
                    //}
                    checkedListBoxNewSpotters.Items.Add(justcall, false);
                    NewSpottersSave();
                }
            }
            timer1.Interval = 1000;
            timer1.Start();
#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                if (server != null && server.IsConnected() && server.stream is not null)
                {
                    if (labelStatusQServer.Text != "Client Connected")
                    {
                        labelStatusQServer.BackColor = qServerBackColor;
                        labelStatusQServer.Text = "Client Connected";
                    }

                }
                else
                {
                    if (labelStatusQServer.Text.Equals("Client Connected", StringComparison.InvariantCultureIgnoreCase))
                    { // then it disconnected
                        labelStatusQServer.BackColor = System.Drawing.Color.Red;
                        labelStatusQServer.Text = "Client disconnected";
                        WindowState = FormWindowState.Minimized;
                        WindowState = FormWindowState.Normal;
                    }
                    else if (labelStatusQServer.Text.Equals("Client Status", StringComparison.InvariantCultureIgnoreCase))
                    {
                        labelStatusQServer.BackColor = qServerBackColor;
                        labelStatusQServer.Text = "Ready for client";
                    }
                }
            }
            catch (Exception)
            {
                return; // any error should be handled by reconnecting
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }


        private void Disconnect()
        {
            timer1.Stop();
            Thread.Sleep(500);
            clusterClient?.Disconnect();
            clusterClient = null;
            server?.Stop();
            server = null;
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
                    AddToLog(richTextBox1, "Disconnected due to error\n", Color.Black);
                }
                startupConnect = false; // don't do it again
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                timer2.Stop();
                timer1.Stop();
                timer1.Enabled = false;
                timer2.Enabled = false;
                Thread.Sleep(1000);
                if (clusterClient is not null) clusterClient!.Dispose();
                if (server is not null && server.IsConnected())
                    server.Stop();
                qrz?.CacheSave(pathQRZCache);
                ReviewedSpottersSave(true);
                string group = "";
                foreach (string token in listBoxIgnoredSpotters.Items)
                {
                    group += token + "|";
                }
                Properties.Settings.Default.Ignore = group;

                // All the form settings
                Properties.Settings.Default.Callsign = textBoxCallsign.Text;
                Properties.Settings.Default.Password = textBoxPassword.Text;
                Properties.Settings.Default.ClusterServer = textBoxClusterServer.Text;
                Properties.Settings.Default.PortLocal = textBoxPortLocal.Text;
                Properties.Settings.Default.rttyOffset = numericUpDownRTTYOffset.Value;
                Properties.Settings.Default.Cached = checkBoxCached.Checked;
                Properties.Settings.Default.Filtered = checkBoxFiltered.Checked;
                Properties.Settings.Default.USA = checkBoxUSA.Checked;
                //var myIndex = comboBoxTimeIntervalForDump.SelectedIndex;
                //ComboBox.ObjectCollection items = comboBoxTimeIntervalForDump.Items;
                //int myItem = Int32.Parse(items?[myIndex]);
                //Properties.Settings.Default.TimeIntervalForDump = myItem;
                int tmp = comboBoxTimeIntervalForDump.SelectedIndex;
                Properties.Settings.Default.TimeIntervalForDump = tmp;
                Properties.Settings.Default.TimeIntervalAfter = comboBoxTimeIntervalAfter.SelectedIndex;
                Properties.Settings.Default.CWMinimum = (int)numericUpDownCwMinimum.Value;
                SaveWindowPosition();
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                _ = MessageBox.Show("Error closing form!!!", ex.Message + "\n" + ex.StackTrace);
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                throw;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void TextBoxCluster_Leave(object sender, EventArgs e)
        {
            char[] sep = [':'];
            var tokens = textBoxClusterServer.Text.Split(sep);
            if (tokens.Length > 0 && tokens.Length != 2)
            {
                MessageBox.Show("Bad web link for cluster server\nExpected server:port", "DxClusterUtil");
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
            if (clusterClient is not null)
            {
                clusterClient.CacheClear();
                clusterClient.totalLines = 0;
                clusterClient.totalLinesKept = 0;
            }
        }

        private void CheckedListBoxReviewedSpotters_SelectedIndexChanged(object sender, EventArgs e)
        {
            var box = checkedListBoxReviewedSpotters;
            bool ctrlKey = ModifierKeys.HasFlag(Keys.Control);
            bool shiftKey = ModifierKeys.HasFlag(Keys.Shift);
            bool altKey = ModifierKeys.HasFlag(Keys.Alt);
            int selectedIndex = box.SelectedIndex;
            if (selectedIndex == -1) return;
            if (ctrlKey && shiftKey && altKey) // delete all
            {
                box.Items.Clear();
                clusterClient?.callSuffixList.Clear();
            }
            if (ctrlKey && shiftKey && !altKey) //delete one
            {
                box.Items.RemoveAt(selectedIndex);
            }
            else if (ctrlKey && !shiftKey && !altKey) // no action on this box
            {
                // box.SetItemCheckState(selectedIndex, CheckState.Indeterminate);
            }
            else if (altKey && !shiftKey && !ctrlKey) // sort list
            {
                box.Sorted = false;
                box.Sorted = true;
                box.Sorted = false;
            }

            else if (shiftKey && !ctrlKey && !altKey) // let's look at the QRZ page
            {
                int selected = box.SelectedIndex;
                if (selected == -1) return;
                string? callsign = box?.Items[selected].ToString();
                box?.SetItemChecked(selected, false);
                string[]? tokens = callsign?.Split('-');  // have to remove any suffix like this
                string url = "https://qrz.com/db/" + tokens?[0];
                System.Diagnostics.Process.Start(url);
                return;
            }
            else if (ctrlKey && altKey && !shiftKey)
            {
                string? curItem = box?.SelectedItem?.ToString();
                if (curItem is not null)
                {
                    int? index = box?.FindString(curItem);
                    if (!listBoxIgnoredSpotters.Items.Contains(curItem))
                    {
                        listBoxIgnoredSpotters.Items.Insert(0, curItem);
                        listBoxIgnoredSpotters.TopIndex = 0;
                        if (index is not null)
                            box?.Items.RemoveAt((int)index);
                    }
                    ReviewedSpottersSave(true);
                }
            }
        }

        //  save the checked ones to a string for ClusterClient
        private void NewSpottersSave()
        {
            string newSpotters = "";
            for (int i = 0; i < checkedListBoxNewSpotters.Items.Count; ++i)
            {
                if (checkedListBoxNewSpotters.GetItemChecked(i))
                {
                    newSpotters += checkedListBoxNewSpotters.Items[i].ToString() + ",1;";
                }
            }
            if (clusterClient != null)
                clusterClient.newSpotters = newSpotters;

        }
        private void ReviewedSpottersSave(bool save)
        {
            var spottersChecked = checkedListBoxReviewedSpotters.CheckedItems;
            string reviewedSpotters = "";

            for (int i = 0; i < spottersChecked?.Count; ++i)
            {
                string? s = "??";
                if (spottersChecked is not null)
                    s = spottersChecked[i]?.ToString();
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
                if (spottersChecked is not null && !spottersChecked.Contains(s))
                {
                    reviewedSpotters += s + ",0;";
                }
            }
            if (save)
            {
                Properties.Settings.Default.ReviewedSpotters = reviewedSpotters;
                Properties.Settings.Default.Save();
            }
            if (clusterClient != null)
            {
                if (clusterClient != null)
                {
                    if (listBoxIgnoredSpotters.Items != null)
                    {
                        if (listBoxIgnoredSpotters.Items != null)
                        {
                            clusterClient.ignoredSpottersAndSpots = string.Join(",", listBoxIgnoredSpotters.Items.Cast<string>());
                        }
                        //clusterClient.ignoredSpottersAndSpots = listBoxIgnoredSpotters.Items.ToString();
                    }
                    else
                    {
                        clusterClient.ignoredSpottersAndSpots = string.Empty;
                    }
                    clusterClient.reviewedSpotters = reviewedSpotters;  // reviewSpotter also contains checked newSpotters
                }
                //clusterClient.ignoredSpottersAndSpots = listBoxIgnoredSpotters.Items.ToString();
                //clusterClient.reviewedSpotters = reviewedSpotters;  // reviewSpotter also contains checked newSpotters
            }
        }
        private void CheckedListBoxNewSpotters_SelectedIndexChanged(object sender, EventArgs e)
        {

            var box = checkedListBoxNewSpotters;
            if (box.SelectedItem == null)
                return;
            int selectedIndex = box.SelectedIndex;
            if (selectedIndex == -1) return;
            bool ctrlKey = ModifierKeys.HasFlag(Keys.Control);
            bool shiftKey = ModifierKeys.HasFlag(Keys.Shift);
            bool altKey = ModifierKeys.HasFlag(Keys.Alt);
            if (ctrlKey && !shiftKey && !altKey) // move to reviewed spotters
            {
                string? curItem = checkedListBoxNewSpotters?.SelectedItem?.ToString();
                if (curItem == null) return;
                int? index = checkedListBoxNewSpotters?.FindString(curItem);
                if (index is null) return;
                if (!checkedListBoxReviewedSpotters.Items.Contains(curItem))
                {
                    checkedListBoxReviewedSpotters.Items.Insert(0, curItem);
                    checkedListBoxReviewedSpotters.SetItemChecked(0, true);
                    checkedListBoxReviewedSpotters.TopIndex = 0;
                    checkedListBoxNewSpotters?.Items.RemoveAt((int)index);
                }
                ReviewedSpottersSave(true);
            }
            if (ctrlKey && shiftKey && altKey) // delete all
            {
                box.Items.Clear();
                clusterClient?.callSuffixList.Clear();
            }
            if (ctrlKey && shiftKey && !altKey) //delete one
            {
                box.Items.RemoveAt(selectedIndex);
            }
            else if (ctrlKey && !shiftKey && !altKey) // no action on this box
            {
                // box.SetItemCheckState(selectedIndex, CheckState.Indeterminate);
            }
            else if (altKey && !shiftKey && !ctrlKey) // sort list
            {
                box.Sorted = false;
                box.Sorted = true;
                box.Sorted = false;
            }

            else if (shiftKey && !ctrlKey && !altKey) // let's look at the QRZ page
            {
                int selected = box.SelectedIndex;
                if (selected == -1) return;
                string? callsign = box?.Items[selected].ToString();
                box?.SetItemChecked(selected, false);
                string[]? tokens = callsign?.Split('-');  // have to remove any suffix like this
                string url = "https://qrz.com/db/" + tokens?[0];
                //var uri = new Uri(url);
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = $"/c start {url}"
                };
                Process.Start(psi);
                return;
            }
            else if (ctrlKey && altKey && !shiftKey)
            {
                string? curItem = box?.SelectedItem.ToString();
                if (curItem is not null)
                {
                    int? index = box?.FindString(curItem);
                    if ((index is not null) && !listBoxIgnoredSpotters.Items.Contains(curItem))
                    {
                        listBoxIgnoredSpotters.Items.Insert(0, curItem);
                        listBoxIgnoredSpotters.TopIndex = 0;
                        box?.Items.RemoveAt((int)index);
                    }
                    //ReviewedSpottersSave(true);
                }
            }
            NewSpottersSave();
        }

        private void ButtonBackup_Click(object sender, EventArgs e)
        {
            //System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel level)
            var userConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            string fileName = "user.config_" + DateTime.Now.ToString("yyyy-MM-ddTHHmmss", CultureInfo.InvariantCulture); // almost ISO 8601 format but have to remove colons
            System.Windows.Forms.SaveFileDialog myDialog = new()
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
                //System.Windows.Clipboard.SetText(richTextBox1.Text);
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
                if (clusterClient is not null) clusterClient.debug = Debug;
                if (qrz is not null) qrz.debug = Debug;
                AddToLog(richTextBox1, "Debug = " + Debug + "\n", Color.Black);
            }
        }

        private void NumericUpDownRTTYOffset_ValueChanged(object sender, EventArgs e)
        {
            if (clusterClient != null)
            {
                clusterClient.rttyOffset = (float)numericUpDownRTTYOffset.Value;
            }
        }

        private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }
        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new();
            Label label = new();
            TextBox textBox = new();
            Button buttonOk = new();
            Button buttonCancel = new();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
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
            form.Controls.AddRange([label, textBox, buttonOk, buttonCancel]);
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            label.Dispose();
            textBox.Dispose();
            form.Dispose();
            return dialogResult;
        }

        private void ListBox1_Click(object sender, EventArgs e)
        {
            var box = (ListBox)sender;
            bool shiftKey = ModifierKeys.HasFlag(Keys.Shift);
            bool ctrlKey = ModifierKeys.HasFlag(Keys.Control);
            bool altKey = ModifierKeys.HasFlag(Keys.Alt);
            if (shiftKey && ctrlKey && !altKey)
            {
                if (shiftKey && ctrlKey && !altKey)
                {
                    if (listBoxIgnoredSpotters.SelectedItem != null)
                    {
                        listBoxIgnoredSpotters.Items.Remove(listBoxIgnoredSpotters.SelectedItem);
                    }
                    return;
                }
                return;
            }
            if (!shiftKey && !ctrlKey && altKey)
            {
                listBoxIgnoredSpotters.Sorted = true;
                Application.DoEvents();
                listBoxIgnoredSpotters.Sorted = false;
            }
            else if (shiftKey && ctrlKey && altKey)
            {
                listBoxIgnoredSpotters.Items.Clear();
            }
            else if (shiftKey && !ctrlKey && !altKey) // let's look at the QRZ page
            {
                int selected = box.SelectedIndex;
                if (selected == -1) return;
                string? callsign = box.Items[selected].ToString();
                string[]? tokens = callsign?.Split('-');  // have to remove any suffix like this
                string url = "https://qrz.com/db/" + tokens?[0];
                System.Diagnostics.Process.Start(url);
                return;
            }
            else
            {
                String value = "";
                if (InputBox("Add Ignore Callsign", "Prompt", ref value) == DialogResult.OK)
                {
                    _ = listBoxIgnoredSpotters.Items.Add(value.ToUpper(new CultureInfo("en-US", false)));
                }
            }
        }

        private void ListBoxIgnore_SelectedIndexChanged(object sender, EventArgs e)
        {
            var box = checkedListBoxNewSpotters;
            if (box.SelectedItem == null)
                return;
            int selectedIndex = box.SelectedIndex;
            if (selectedIndex == -1) return;
            bool ctrlKey = ModifierKeys.HasFlag(Keys.Control);
            bool shiftKey = ModifierKeys.HasFlag(Keys.Shift);
            bool altKey = ModifierKeys.HasFlag(Keys.Alt);
            if (ctrlKey && shiftKey && altKey) // delete all
            {
                box.Items.Clear();
                clusterClient?.callSuffixList.Clear();
            }
            if (ctrlKey && shiftKey && !altKey) //delete one
            {
                box.Items.RemoveAt(selectedIndex);
            }
            else if (ctrlKey && !shiftKey && !altKey) // no action on this box
            {
                // box.SetItemCheckState(selectedIndex, CheckState.Indeterminate);
            }
            else if (altKey && !shiftKey && !ctrlKey) // sort list
            {
                box.Sorted = false;
                box.Sorted = true;
                box.Sorted = false;
            }

            else if (shiftKey && !ctrlKey && !altKey) // let's look at the QRZ page
            {
                int selected = box.SelectedIndex;
                if (selected == -1) return;
                string? callsign = box?.Items[selected].ToString();
                box?.SetItemChecked(selected, false);
                string[]? tokens = callsign?.Split('-');  // have to remove any suffix like this
                string url = "https://qrz.com/db/" + tokens?[0];
                System.Diagnostics.Process.Start(url);
                return;
            }

        }
        private void ComboBoxInterval_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox box = (ComboBox)sender;
#pragma warning disable CA1305 // Specify IFormatProvider
            TimeIntervalForDump = Convert.ToInt32(box.SelectedItem);
#pragma warning restore CA1305 // Specify IFormatProvider
        }

        private void ComboBoxTimeForDump_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox box = (ComboBox)sender;
#pragma warning disable CA1305 // Specify IFormatProvider
            TimeIntervalAfter = Convert.ToInt32(box.SelectedItem);
#pragma warning restore CA1305 // Specify IFormatProvider
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                RestoreWindowPosition();
                if (this.Size.Width < 100 || this.Size.Height < 100)
                {
                    this.Size = new Size(676, 277);
                }
                textBoxCallsign.Text = Properties.Settings.Default.Callsign;
                textBoxPassword.Text = Properties.Settings.Default.Password;
                textBoxClusterServer.Text = Properties.Settings.Default.ClusterServer;
                textBoxPortLocal.Text = Properties.Settings.Default.PortLocal;
                numericUpDownRTTYOffset.Value = Properties.Settings.Default.rttyOffset;
                checkBoxCached.Checked = Properties.Settings.Default.Cached;
                checkBoxFiltered.Checked = Properties.Settings.Default.Filtered;
                checkBoxUSA.Checked = Properties.Settings.Default.USA;
                int tmp = Properties.Settings.Default.TimeIntervalForDump;
                comboBoxTimeIntervalForDump.SelectedIndex = tmp;
                comboBoxTimeIntervalAfter.SelectedIndex = Properties.Settings.Default.TimeIntervalAfter;
                numericUpDownCwMinimum.Value = Properties.Settings.Default.CWMinimum;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                _ = MessageBox.Show("Error restoring form", ex.Message + "\n" + ex.StackTrace);
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }
            Application.DoEvents();
            if (textBoxCallsign.Text.Length > 0 && textBoxPassword.Text.Length > 0 && textBoxClusterServer.Text.Length > 0)
            {
                ButtonStart_Click(null, null);
            }
            qServerBackColor = labelStatusQServer.BackColor;
        }
        private void RestoreWindowPosition()
        {
            if (Properties.Settings.Default.HasSetDefaults)
            {
                this.WindowState = Properties.Settings.Default.WindowState;
                this.Location = Properties.Settings.Default.Location;
                this.Size = Properties.Settings.Default.Size;
            }
        }

        private void SaveWindowPosition()
        {
            Properties.Settings.Default.WindowState = this.WindowState;

            if (this.WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.Location = this.Location;
                Properties.Settings.Default.Size = this.Size;
            }
            else
            {
                Properties.Settings.Default.Location = this.RestoreBounds.Location;
                Properties.Settings.Default.Size = this.RestoreBounds.Size;
            }

            Properties.Settings.Default.HasSetDefaults = true;

            //Properties.Settings.Default.Save();
        }

        private void CheckBoxUSA_CheckedChanged(object sender, EventArgs e)
        {
            if (clusterClient != null)
            {
                clusterClient.filterUSA = checkBoxUSA.Checked;
            }
            else
            {
                //checkBoxUSA.Checked = false;
            }
        }

        private void Form1_Validated(object sender, EventArgs e)
        {
            ButtonStart_Click(null, null);
        }

        private void Timer2_Tick(object sender, EventArgs e)
        {
            timer2.Stop();
            TimeSpan tzone = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
#pragma warning disable CA1305 // Specify IFormatProvider
            string myTime = DateTime.UtcNow.ToString("HH:mm:ss");
            labelQDepth.Text = "Q(" + clientQueue.Count.ToString() + ") " + myTime + "(" + tzone.Hours.ToString("+00;-00;+00") + ")";
#pragma warning restore CA1305 // Specify IFormatProvider
            timer2.Start();
        }

        private void NumericUpDownCwMinimum_ValueChanged(object sender, EventArgs e)
        {
            if (clusterClient is not null)
                clusterClient!.numericUpDownCwMinimum = (int)numericUpDownCwMinimum!.Value;
        }

        [GeneratedRegex(@"CW\s+(\+?\d+)\s+dB")]
        private static partial Regex MyRegexCW();

        private void LabelQRZCache_Click(object sender, EventArgs e)
        {
            //bool ctrlKey = ModifierKeys.HasFlag(Keys.Control);
            //if (ctrlKey)
            {
                qrz!.CacheClear();
                badCalls = 0;
            }
            labelQRZCache.Text = "" + qrz?.cacheQRZ.Count + "/" + badCalls;
        }
    }
    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            if (box is null)
            {
                ArgumentNullException.ThrowIfNull(box);
            }
            else
            {
                box.SelectionStart = box.TextLength;
                box.SelectionLength = 0;

                box.SelectionColor = color;
                box.AppendText(text);
                box.SelectionColor = box.ForeColor;
            }
        }

    }

}
