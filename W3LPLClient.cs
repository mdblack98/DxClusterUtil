using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        public readonly Dictionary<string, int> cacheSpottedCalls = new Dictionary<string, int>();
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
        private readonly QRZ qrz = null;
        public bool debug = false;
        public float rttyOffset;
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
                //w3lplQueue.Add("W3LPL connect error\n");
                return false;
            }
            Form1.Instance.TextStatusColor = System.Drawing.ColorTranslator.FromHtml("#F0F0F0");
            Form1.Instance.TextStatus = "Client connected";

            Form1.Instance.TextStatus = "Connected";
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
            File.AppendAllText("C:/Temp/W3LPL_log.txt", "Logging started\r\n");
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
                            else if (response.Contains(callsign + " de "))
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
                        Form1.Instance.TextStatus = "Timeout";
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
                Form1.Instance.TextStatus = "Connect error";
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
            if (client == null | qrz == null)
            {
                string s1, s2;
                bool isNull = client == null;
                if (isNull)
                    s1 = "null";
                else
                    s1 = "OK";
                isNull = qrz == null;
                if (isNull)
                    s2 = "null";
                else
                    s2 = "OK";
                MessageBox.Show("client == null =" + s1 + " or qrz == null = " + s2);
            }
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
                    try
                    {
                        c = (char)nStream.ReadByte();
                        ss += c;
                    }
                    catch (IOException)
                    {
                        return null;
                    }
                } while (c != '\n');
                if (debug) File.AppendAllText("C:/Temp/w3lpl_log.txt", ss);
                //Int32 bytesRead = nStream.Read(databuf, 0, databuf.Length);
                //var responseData = System.Text.Encoding.ASCII.GetString(databuf, 0, bytesRead);
                char[] sep = { '\n', '\r' };
                //ss = "DX de W3LPL-3:    3584.3  PJ5/KG9N     RTTY Heard in AZ and MA        1311Z";
                var tokens = ss.Split(sep);
                string sreturn = "";
                if (tokens == null || tokens.Length == 0)
                {
                    Disconnect();
                    Connect();
                    return null;
                }
                foreach (string s in tokens)
                {
                    if (s.Length == 0) continue;
                    var swork = s;
                    if (s.ToUpperInvariant().StartsWith("DX DE",StringComparison.InvariantCultureIgnoreCase) && s.Length == 75)
                    {
                        var sfreq = s.Substring(17, 7);
                        var freq = sfreq;
                        var ffreq = float.Parse(freq,CultureInfo.InvariantCulture);
                        freq = Math.Round(ffreq).ToString(CultureInfo.InvariantCulture);
                        if (freq.Length > 4) freq = freq.Substring(0, freq.Length - 2);
                        var spot = s.Substring(26, 9);
                        var comment = s.Substring(38, 20);
                        var time = s.Substring(70, 3); // use 10 minute cache
                        var key = freq + "|" + spot + "|" + time;
                        if (comment.Contains("RTTY"))
                        {
                            ffreq += rttyOffset/1000;
                            swork = s.Replace(sfreq, String.Format(CultureInfo.InvariantCulture,"{0,7:0.0}",ffreq));
                            key = ffreq + "|" + spot + "|" + time;
                        }
                        if (!Int32.TryParse(s.Substring(73, 1), out int minute))
                        {
                            continue;
                        }
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
                        bool validCall = qrz.GetCallsign(spottedCall, out cachedQRZ);
                        if (!skimmer && validCall && !filteredOut) // if it's not a skimmer just let it through as long as valid call and hasn't been excluded
                        {
                            ++totalLinesKept;
                            // we may have changed the freq so we add the change to log4omQueue
                            log4omQueue.Add(swork + "\r\n");
                            File.AppendAllText("C:/Temp/W3LPL_log.txt", s + "\r\n");
                            if (!swork.Equals(s)) // then we'll also log the change
                            {
                                File.AppendAllText("C:/Temp/W3LPL_log.txt", swork + "\r\n");
                            }
                            sreturn += s + "\r\n";
                            return sreturn;
                        }
                        bool isW3LPLCached = cacheSpottedCalls.ContainsKey(key);
                        if (!isW3LPLCached && !filteredOut)
                        {
                            cacheSpottedCalls[key] = minute;
                            ++totalLinesKept;
                            string sss = s;
                            if (!validCall && cachedQRZ) // then the bad call is cached
                            {
                                sss = s.Replace("DX de", "#* de");
                            }
                            else if (!validCall) // then first time QRZ tried it
                            {
                                sss = s.Replace("DX de", "## de");
                            }
                            else
                            {
                                log4omQueue.Add(swork + "\r\n");
                                File.AppendAllText("C:/Temp/W3LPL_log.txt", s + "\r\n");
                                if (!swork.Equals(s)) // then we'll also log the change
                                {
                                    File.AppendAllText("C:/Temp/W3LPL_log.txt", swork + "\r\n");
                                }
                            }
                            sreturn += sss + "\r\n";
                        }
                        else if (s.Contains(myCallsign))  // allow our own spots through too
                        {
                            ++totalLinesKept;
                            log4omQueue.Add(swork + "\r\n");
                            File.AppendAllText("C:/Temp/W3LPL_log.txt", s + "\r\n");
                            if (!swork.Equals(s)) // then we'll also log the change
                            {
                                File.AppendAllText("C:/Temp/W3LPL_log.txt", swork + "\r\n");
                            }
                            sreturn += s + "\r\n";
                            myCallsignExists = true;
                            cachedQRZ = false;
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
                                if (qrz.isOnline == false)
                                {
                                    tag = "ZZ"; // show QRZ is sleeping
                                    log4omQueue.Add("QRZ not responding?\n");
                                    File.AppendAllText("C:/Temp/W3LPL_log.txt", s + "\r\n");
                                }
                                else if (!validCall)
                                {
                                    if (debug)
                                    {
                                        File.AppendAllText("C:/Temp/qrzerror.txt", "!valid??: " + qrz.xml + "\n");
                                    }
                                    if (qrz.xmlError.Contains("Error"))
                                    {
                                        log4omQueue.Add(qrz.xmlError + "\n");
                                        File.AppendAllText("C:/Temp/W3LPL_log.txt", s + "\r\n");
                                    }
                                    if (cachedQRZ)
                                    {
                                        tag = "#*";
                                    }
                                    else
                                    {
                                        tag = "##";
                                    }
                                }
                                else if (filteredOut)
                                {
                                    tag = "!!";
                                    //log4omQueue.Add(tag + s.Substring(2) + "\r\n");
                                }
                                else if (cacheSpottedCalls.ContainsKey(key))
                                {
                                    tag = "**";
                                    cachedQRZ = true;
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
                    if (qrz != null) qrz.Dispose();
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
