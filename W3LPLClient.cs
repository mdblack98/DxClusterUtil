using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace W3LPL
{
    class W3LPLClient: IDisposable
    {
        public TcpClient client = null;
        private NetworkStream nStream = null;
        ConcurrentBag<string> log4omQueue = null;
        readonly ConcurrentBag<string> w3lplQueue = null;
        readonly Dictionary<string, int> cacheSpottedCalls = new Dictionary<string, int>();
        readonly string myHost;
        readonly int myPort;
        RichTextBox myDebug;
        string myCallsign;
        public UInt64 totalLines;
        public UInt64 totalLinesKept;
        private int lastMinute = 1; // for cache usage
        public bool filterOn = true;
        public List<string> callSuffixList = new List<string>();
        public string reviewedSpotters = "";
        private QRZ qrz = null;
        public W3LPLClient(string host, int port, ConcurrentBag<string> w3lplQ, QRZ qrz)
        {
            this.qrz = qrz;
            myHost = host;
            myPort = port;
            w3lplQueue = w3lplQ;
            callSuffixList.Add("4U1UN");
        }

        ~W3LPLClient()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (client != null)
            {
                //nStream.Close();
                if (client.Connected)
                {
                    client.Client.Shutdown(SocketShutdown.Receive);
                }
                client.Close();
                client = null;
                nStream = null;
            }
        }
        public void Disconnect()
        {
            Cleanup();
        }

        private bool Connect()
        {
            if (!Connect(myCallsign,myDebug,log4omQueue))
            {
                return false;
            }
            totalLines = 0;
            totalLinesKept = 0;
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public bool Connect(string callsign, RichTextBox debug, ConcurrentBag<string> clientQueue)
        {
            myCallsign = callsign;
            myDebug = debug;
            log4omQueue = clientQueue;
            try
            {
                cacheSpottedCalls.Clear();
                client = new TcpClient
                {
                    ReceiveTimeout = 2000
                };
                client.Connect(myHost, myPort);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                nStream = client.GetStream();
                var buffer = new byte[8192];
                bool loggedIn = false;
                int loopcount = 5;
                while (!loggedIn)
                {
                    Application.DoEvents();
                    int bytesRead = 0;
                    if (--loopcount > 0 && nStream.DataAvailable )
                    {
                        bytesRead = nStream.Read(buffer, 0, buffer.Length);
                        while (bytesRead > 0)
                        {
                            var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                            debug.AppendText(response);
                            clientQueue.Add(response);
                            if (response.Contains("call:") || response.Contains("callsign:"))
                            {
                                var msg = Encoding.ASCII.GetBytes(callsign + "\r\n");
                                nStream.Write(msg, 0, msg.Length);
                            }
                            else if (response.Contains(callsign + " de W3LPL"))
                            {
                                loggedIn = true;
                                //var msg = Encoding.ASCII.GetBytes("Set Dx Filter (skimmer and unique > 2 AND spottercont=na) OR (not skimmer and spottercont=na)\n");
                                //var msg = Encoding.ASCII.GetBytes("SET/FILTER K,VE/PASS\n");
                                //nStream.Write(msg, 0, msg.Length);
                                return true;
                            }
                            bytesRead = nStream.Read(buffer, 0, buffer.Length);
                            Application.DoEvents();
                            Thread.Sleep(2000);
                        }
                    }
                    if (loopcount < 0)
                    {
                        client.Client.Shutdown(SocketShutdown.Receive);
                        client.Close();
                        client = null;
                        return false;
                    }
                    Thread.Sleep(1000);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                client.Close();
                client.Dispose();
                client = null;
                //MessageBox.Show(ex.Message, "W3LPL");
                //throw;
                //return false;
            }
            return false;
        }

        public bool ReviewedSpottersIsChecked(string s)
        {
            string check = s + ",1";
            bool gotem = reviewedSpotters.Contains(check);
            return gotem;
        }
        public bool ReviewedSpottersContains(string s)
        {
            string check = s + ",";
            bool gotem = reviewedSpotters.Contains(check);
            return gotem;
        }
        public bool ReviewedSpottersIsNotChecked(string s)
        {
            string check = s + ",0";
            bool gotem = reviewedSpotters.Contains(check);
            return gotem;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public string Get(out bool cachedQRZ)
        {
            cachedQRZ = false;
            if (w3lplQueue.TryTake(out string result))
            {
                var outmsg = Encoding.ASCII.GetBytes(result);
                nStream.Write(outmsg, 0, outmsg.Length);
            }
            if (client == null) { 
                return null; 
            }
            if (client.Connected && nStream != null && nStream.DataAvailable)
            {
                while (w3lplQueue.TryTake(out string command))
                {
                    var outmsg = Encoding.ASCII.GetBytes(command);
                    nStream.Write(outmsg, 0, outmsg.Length);
                }
                string ss = "";
                char c;
                do
                {
                    c = (char)nStream.ReadByte();
                    ss += c;
                } while (c != '\n');
                //Int32 bytesRead = nStream.Read(databuf, 0, databuf.Length);
                //var responseData = System.Text.Encoding.ASCII.GetString(databuf, 0, bytesRead);
                char [] sep = { '\n', '\r' };
                var tokens = ss.Split(sep);
                string sreturn = "";
                if (tokens == null || tokens.Length == 0)
                {
                    return null;
                }
                foreach (string s in tokens)
                {
                    //if (!s.Contains("W3LPL")&&!s.Contains("VE6WZ")) continue;
                    if (s.Length == 0) continue;
                    if (s.ToUpperInvariant().StartsWith("DX DE",StringComparison.InvariantCultureIgnoreCase) && s.Length == 75)
                    {
                        var freq = s.Substring(17, 9);
                        var ffreq = float.Parse(freq,CultureInfo.InvariantCulture);
                        freq = Math.Round(ffreq).ToString(CultureInfo.InvariantCulture);
                        if (freq.Length > 4) freq = freq.Substring(0, freq.Length - 2);
                        var spot = s.Substring(26, 9);
                        var time = s.Substring(70,3); // use 10 minute cache
                        if (!Int32.TryParse(s.Substring(73, 1), out int minute))
                        {
                            continue;
                        }
                        //if (cache == null) cache = new HashSet<string>();
                        var key = freq + "|" + spot + "|" + time;
                        //if (time != lastTime) // when our time rolls over clear the cache
                        if (minute != lastMinute)
                        {
                            int removeMinute = (minute + 1) % 10;
                            // when the minute rolls over we remove the 10 minute old cache entries
                            //cache.Clear();
                            //lastTime = time;
                            foreach (var m in cacheSpottedCalls.ToList())
                            {
                                if (m.Value == removeMinute)
                                {
                                    cacheSpottedCalls.Remove(m.Key);
                                }
                            }
                            lastMinute = minute;
                        }
                        ++totalLines;
                        bool filteredOut = false;
                        char[] delim = { ' ' };
                        string[] tokens2 = s.Split(delim,StringSplitOptions.RemoveEmptyEntries);
                        string spotterCall = "";
                        string spottedCall = "";
                        // gotta get just the callsign to make Log4OM happy
                        // there's a bug in 1.40.0.0 where a suffix XXXX-2-# does not get processed by Log4OM
                        // So we remove all suffixes to send to Log4OM
                        if (tokens2[2].Contains("-#")) 
                        {
                            spotterCall = tokens2[2].Substring(0, tokens2[2].Length - 3);
                        }
                        else
                        {
                            spotterCall = tokens2[2].Substring(0, tokens2[2].Length - 1);
                        }
                        if (tokens2.Length < 4) return s;
                        myCallsignExists = false;
                        spottedCall = tokens2[4];
                        bool skimmer = s.Contains("WPM CQ") || s.Contains("BPS CQ") || s.Contains("WPM BEACON") || s.Contains("WPM NCDXF");
                        if (!ReviewedSpottersContains(spotterCall) || (skimmer && ReviewedSpottersIsNotChecked(spotterCall)))
                        {
                            filteredOut = true;
                            if (!callSuffixList.Contains(tokens2[2]))
                            {
                                if (skimmer) callSuffixList.Insert(0, spotterCall+":SK");
                                else callSuffixList.Insert(0, spotterCall+":OK");
                            }
                        }
                        if (!cacheSpottedCalls.ContainsKey(key) && !filteredOut)
                        {
                            cacheSpottedCalls[key] = minute;
                            ++totalLinesKept;
                            log4omQueue.Add(s + "\r\n");
                            sreturn += s + "\r\n";
                            cachedQRZ = true;
                        }
                        else if (s.Contains(myCallsign))  // allow our own spots through too
                        {
                            ++totalLinesKept;
                            log4omQueue.Add(s + "\r\n");
                            sreturn += s + "\r\n";
                            myCallsignExists = true;
                        }
                        else // need to check if QRZ is offline and do something about it
                        {
                            if (s.Length > 2)
                            {
                                // ** -- our built-in cache item -- 10 minutes
                                // !! -- filtered out
                                // ## -- bad QRZ lookup
                                // %% -- QRZ cached good call
                                string tag = "**";
                                bool validCall = qrz.GetCallsign(spottedCall, out cachedQRZ);
                                if (qrz.isOnline == false)
                                {
                                    tag = "ZZ"; // show QRZ is sleeping
                                    log4omQueue.Add("QRZ not responding?\n");
                                }
                                else if (!validCall)
                                {
                                    if (qrz.xmlError.Contains("Error"))
                                    {
                                        log4omQueue.Add(qrz.xmlError + "\n");
                                    }
                                    tag = "##";
                                }
                                else if (filteredOut)
                                {
                                    tag = "!!";
                                    //log4omQueue.Add(tag + s.Substring(2) + "\r\n");
                                }
                                else if (cacheSpottedCalls.ContainsKey(key))
                                {
                                    tag = "**";
                                }
                                sreturn += tag + s.Substring(2) + "\r\n";
                            }
                        }

                    }
                    else
                    {
                        // Once in a while the time isn't on the DX message so we just skip it
                        if (s.Contains("DX de") && s.Length < 74)
                            MessageBox.Show("Length wrong??\n" + s);
                        if (s.Length > 1) sreturn += s + "\r\n";
                    }
                }
                return sreturn;
            }
            try
            {
                var msg = "";
                var bytes = Encoding.ASCII.GetBytes(msg);
                nStream.Write(bytes, 0, bytes.Length);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Disconnect();
                if (!Connect())
                {
                    MessageBox.Show("Error connecting to W3LPL", "W3LPL");
                }
            }
            return null;
        }

        internal void CacheClear()
        {
            totalLines = 0;
            totalLinesKept = 0;
            cacheSpottedCalls.Clear();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        public bool myCallsignExists;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (client != null) client.Dispose();
                    if (nStream != null) nStream.Dispose();
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~W3LPLClient()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
