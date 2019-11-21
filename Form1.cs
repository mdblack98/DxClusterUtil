using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Globalization;

namespace W3LPL
{
    public partial class Form1 : Form
    {
        private W3LPLClient w3lpl = null;
        readonly ConcurrentBag<string> clientQueue = new ConcurrentBag<string>();
        readonly ConcurrentBag<string> w3lplQueue = new ConcurrentBag<string>();
        private QServer server;

        //public volatile static int keep;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public Form1()
        {
            InitializeComponent();
            Size = Properties.Settings.Default.Size;
            ToolTip tooltip = new ToolTip();
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
            tooltip.Dispose();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public bool Connect()
        {
            if (textBoxCallsign.Text.Length == 0)
            {
                MessageBox.Show("Need callsign!~", "QueueIt");
                return false;
            }
            char[] sep = { ':' };
            var tokens = textBoxClusterServer.Text.Split(sep);
            if (tokens.Length != 2)
            {
                MessageBox.Show("Bad format for cluster server", "W3LPL");
                return false;
            }
            string host = tokens[0];
            int port = Int32.Parse(tokens[1], CultureInfo.InvariantCulture);
            w3lpl = new W3LPLClient(host,port, w3lplQueue);
            try
            {
                if (w3lpl.Connect(textBoxCallsign.Text, richTextBox1, clientQueue))
                {
                    timer1.Start();
                    buttonStart.Text = "Disconnect";
                    richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    richTextBox1.ScrollToCaret();
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                buttonStart.Text = "Connect";
                MessageBox.Show(ex.Message, "QueueIt");
            }

            // Now start the server for Log4OM to get the spots from the queue
            if (Int32.TryParse(textBoxPortLocal.Text, out int qport)) {
                server = new QServer(qport,clientQueue, w3lplQueue);
                _ = Task.Run(() => server.Start());
            }
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
            float pct = 100;
            if (w3lpl.totalLines != 0) pct = w3lpl.totalLinesKept * 100 / w3lpl.totalLines;
            labelQDepth.Text = "  Spots:" + w3lpl.totalLines + "   Q(" + clientQueue.Count.ToString() + "/" + pct + "%)";
            //labelQDepth.Text = "Q(" + clientQueue.Count.ToString() + ")";

            timer1.Interval = 1000;
            timer1.Start();
            try
            {
                if (server.IsConnected()) labelStatusQServer.Text = "Client connected";
                else labelStatusQServer.Text = "Ready for client";
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
    }
}
