using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DXClusterUtil
{
    partial class ClusterClient: IDisposable
    {
        public TcpClient? client;
        private NetworkStream? nStream;
        ConcurrentBag<string>? log4omQueue;
        readonly ConcurrentBag<string> clusterQueue;
        public readonly Dictionary<string, int> cacheSpottedCalls = [];
        readonly string myHost;
        readonly int myPort;
        RichTextBox? myDebug;
        string? myCallsign;
        public UInt64 totalLines;
        public UInt64 totalLinesKept;
        private int lastMinute = 1; // for cache usage
        public bool filterOn = true;
        public bool filterUSA = false;
        public HashSet<string> callSuffixList = [];
        public string reviewedSpotters = "";
        public string ignoredSpottersAndSpots = "";
        private readonly QRZ qrz;
        public bool debug = true;
        public float rttyOffset;
        public ListBox? listBoxIgnore;
        private readonly string logFile = Environment.ExpandEnvironmentVariables("%TEMP%\\DxClusterUtil_Log.txt");
        readonly Mutex mutex = new(true);
        public int numericUpDownCwMinimum = 0;
        private string lastcall = "";

        private readonly string pathQRZError = Environment.ExpandEnvironmentVariables("%TEMP%\\DxClusterUtil_qrzerror.txt");

        public ClusterClient(string host, int port, ConcurrentBag<string> ClusterServerQ, QRZ qrz)
        {
            File.Delete(logFile);
            File.Delete(pathQRZError);
            this.qrz = qrz;
            myHost = host;
            myPort = port;
            clusterQueue = ClusterServerQ;
            if (!File.Exists(logFile))
            {
                var stream = File.Create(logFile);
                stream.Dispose();
            }
            long length = new System.IO.FileInfo(logFile).Length;
            if (length > 10000000) File.Delete(logFile);
        }

        ~ClusterClient()
        {
            mutex.Dispose();
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
            if (myCallsign is null || myDebug is null || log4omQueue is null || !Connect(myCallsign,myDebug,log4omQueue))
            {
                return false;
            }
            Form1.Instance.TextStatusColor = System.Drawing.ColorTranslator.FromHtml("#F0F0F0");
            Form1.Instance.TextStatus = "Client connected";

            totalLines = 0;
            totalLinesKept = 0;
            return true;
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public bool Connect(string callsign, RichTextBox debug, ConcurrentBag<string> clientQueue)
        {
            int counter = 0;
            myCallsign = callsign;
            myDebug = debug;
            log4omQueue = clientQueue;
            try
            {
                File.AppendAllText(logFile, "Logging started\r\n");
            }
            catch 
            {
                throw;
            }
            do 
            {
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
                        if (--loopcount > 0 && nStream.DataAvailable)
                        {
                            bytesRead = nStream.Read(buffer, 0, buffer.Length);
                            while (bytesRead > 0)
                            {
                                counter = 0;
                                var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                                debug.AppendText(response);
                                Application.DoEvents();
                                clientQueue.Add(response);
                                if (response.Contains("call:", StringComparison.InvariantCulture) || response.Contains("callsign.", StringComparison.InvariantCulture))
                                {
                                    var msg = Encoding.ASCII.GetBytes("\n" + callsign + "\n");
                                    Thread.Sleep(1000);
                                    nStream.Write(msg, 0, msg.Length);
                                    //nStream.Write(msg, 0, msg.Length);
                                    //return true;
                                }
                                else if (response.Contains(callsign + " de ", StringComparison.InvariantCulture) || response.Contains("Hello", StringComparison.InvariantCulture) || response.Contains("Welcome", StringComparison.InvariantCulture))
                                {
                                    loggedIn = true;
                                    //var msg = Encoding.ASCII.GetBytes("Set Dx Filter (skimmer and unique > 2 AND spottercont=na) OR (not skimmer and spottercont=na)\n");
                                    //var msg = Encoding.ASCII.GetBytes("SET/FILTER K,VE/PASS\n");
                                    //nStream.Write(msg, 0, msg.Length);
                                    return true;
                                }
                                if (nStream.DataAvailable)
                                    bytesRead = nStream.Read(buffer, 0, buffer.Length);
                                else
                                    bytesRead = 0;
                                    Application.DoEvents();
                                //Thread.Sleep(2000);
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
                catch (Exception)
                {
                    if (client is not null) { 
                        client.Close();
                        client.Dispose();
                        client = null;
                    }
                    Form1.Instance.TextStatus = "Connect error "+ ++counter;
                    throw;
                }
            } while (client != null);
            return false;
        }

        public bool ReviewedSpottersIsChecked(string s)
        {
            string check = s + ",1";
            bool gotem = reviewedSpotters.Contains(check, StringComparison.InvariantCulture);
            return gotem;
        }
        public bool ReviewedSpottersContains(string s)
        {
            string check = s + ",";
            bool gotem = reviewedSpotters.Contains(check, StringComparison.InvariantCulture);
            if (!gotem && newSpotters != null) gotem = newSpotters.Contains(check, StringComparison.InvariantCulture);
            return gotem;
        }
        public bool ReviewedSpottersIsNotChecked(string s)
        {
            string check = s + ",0";
            bool gotem = reviewedSpotters.Contains(check, StringComparison.InvariantCulture);
            return gotem;
        }

        public bool IgnoredSpottersContains(string s)
        {
            bool gotem = ignoredSpottersAndSpots.Contains(s, StringComparison.InvariantCulture);
            return gotem;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public string? Get(out bool cachedQRZ, RichTextBox debuglog, string callSign)
        {
            cachedQRZ = false;
            if (client == null || qrz == null)
            {
                string s1;
                bool isNull = client == null;
                if (isNull)
                    s1 = "null";
                else
                    s1 = "OK";
                debuglog.AppendText("cluster client is " + s1);
                isNull = qrz == null;
                if (isNull)
                    s1 = "null";
                else
                    s1 = "OK";
                debuglog.AppendText("qrz client is " + s1);
                return null;
            }
            if (clusterQueue.TryTake(out string? result)) // this is reading from Log4OM
            {
                try
                {
                    var outmsg = Encoding.ASCII.GetBytes(result);
                    nStream!.Write(outmsg, 0, outmsg.Length);
                    return result;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("oops!!\n" + ex.Message);
                    throw;
                }
            }
            if (client == null) { 
                return null; 
            }
            mutex.WaitOne();
            if (client.Connected && nStream != null && nStream.DataAvailable)
            {
                while (clusterQueue.TryTake(out string? command))
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
                        // this is reading from our cluster spotting source (e.g. W3LPL)
                        c = (char)nStream.ReadByte();
                        ss += c;
                    }
                    catch (IOException)
                    {
                        mutex.ReleaseMutex();
                        return null;
                    }
                } while (c != '\n');
                bool worked = false;
                do
                {
                    try
                    {
                        try
                        {
                            if (debug) File.AppendAllText(logFile, ss);
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                        worked = true;
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        var result1 = MessageBox.Show("File exception in " + logFile+"\n"+ex.Message, "DXClusterUtil Error", MessageBoxButtons.RetryCancel);
                        if (result1 == DialogResult.Retry)
                        {
                            try
                            {
                                File.AppendAllText(logFile, ss);
                            }
#pragma warning disable CA1031 // Do not catch general exception types
                            catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                        }
                    }
                } while (worked == false);
                //Int32 bytesRead = nStream.Read(databuf, 0, databuf.Length);
                //var responseData = System.Text.Encoding.ASCII.GetString(databuf, 0, bytesRead);
                char[] sep = ['\n', '\r'];
                //ss = "DX de W3LPL-3:    3584.3  PJ5/KG9N     RTTY Heard in AZ and MA        1311Z";
                var tokens = ss.Split(sep);
                string sreturn = "";
                if (tokens == null || tokens.Length == 0)
                {
                    Disconnect();
                    Connect();
                    mutex.ReleaseMutex();
                    return null;
                }
                foreach (string line in tokens)
                {
                    bool cacheAdded = false;
                    if (line.Length == 0) continue;
                    var swork = line;
                    if (line.ToUpperInvariant().StartsWith("DX DE", StringComparison.InvariantCultureIgnoreCase) && line.Length == 75)
                    {
                        var sfreq = line.Substring(17, 7);
                        var freq = sfreq;
                        var ffreq = float.Parse(freq, CultureInfo.InvariantCulture);
                        freq = Math.Round(ffreq).ToString(CultureInfo.InvariantCulture);
                        if (freq.Length > 4) freq = freq[0..^2];
                        var spot = line.Substring(26, 9);
                        // Remove any suffix from special callsigns
                        spot = HandleSpecialCalls(spot);
                        var comment = line.Substring(38, 20);
                        var time = line.Substring(70, 3); // use 10 minute cache
                        var key = freq + "|" + spot + "|" + time;
                        string myTime = line.Substring(70, 4);
                        if (DateTime.TryParseExact(myTime, "HHmm", null, System.Globalization.DateTimeStyles.None, out DateTime parsedTime))
                        {

                        }
                        if (!Int32.TryParse(line.AsSpan(73, 1), out int minute))
                        {
                            continue;
                        }
                        if (cacheSpottedCalls.ContainsKey(key))
                        {
                            var tag = "**";
                            cachedQRZ = true;
                            sreturn += tag + line[2..] + "\r\n";
                            return sreturn;
                        }
                        else
                        {
                            cacheAdded = true;
                            cacheSpottedCalls[key] = minute;
                        }
                        if (key.Equals(lastcall, StringComparison.Ordinal))
                        {
                            int i = 1;  // for debugging dups when dups aren't working -- this catches two calls in a row when the first should be cached.
                        }
                        lastcall = key;
                        if (comment.Contains("RTTY", StringComparison.InvariantCulture))
                        {
                            ffreq += rttyOffset / 1000;
                            swork = line.Replace(sfreq, String.Format(CultureInfo.InvariantCulture, "{0,7:0.0}", ffreq), StringComparison.InvariantCulture);
                            key = ffreq + "|" + spot + "|" + time;
                        }
                        // Don't remove good lookups from cache
                        if (minute != lastMinute)
                        {
                            int removeMinute = (minute + 1) % 10;
                            // when the minute rolls over we remove the 10 minute old bad cache entries
                            // cache.Clear();
                            // lastTime = time;
                            foreach (var m in cacheSpottedCalls.ToList())
                            {
                                if (m.Key.Contains("BAD",StringComparison.OrdinalIgnoreCase) && m.Value == removeMinute)
                                {
                                    cacheSpottedCalls.Remove(m.Key);
                                }
                            }
                            lastMinute = minute;
                        }
                        
                        ++totalLines;
                        bool filteredOut = false;
                        char[] delim = [' '];
                        string[] tokens2 = line.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                        string spotterCall = "";
                        string spottedCall = "";
                        // gotta get just the callsign to make Log4OM happy
                        // there's a bug in 1.40.0.0 where a suffix XXXX-2-# does not get processed by Log4OM
                        // So we remove all suffixes to send to Log4OM
                        if (MyRegexSuffix().IsMatch(tokens2[2]))
                        {

                            //spotterCall = Regex.Replace(tokens2[2], "-[0-9]-#:", "");
                            //swork = Regex.Replace(swork, "-[0-9]-#:", "-#:  ");
                            spotterCall = Regex.Replace(tokens2[2], MyRegexSuffix().ToString(), "");
                            swork = Regex.Replace(swork, MyRegexSuffix().ToString(), "-#:  ");
                        }
                        else
                        {
                            spotterCall = Regex.Replace(tokens2[2], "-#:", "");
                            if (spotterCall.Last() == ':') spotterCall = spotterCall.Remove(spotterCall.Length - 1);
                        }
                        if (tokens2.Length < 4)
                        {
                            mutex.ReleaseMutex();
                            return line;
                        }
                        myCallsignExists = false;
                        spottedCall = tokens2[4];

                        if (spotterCall == callSign)
                        {
                            log4omQueue?.Add(line + "\r\n");
                            return line;
                        }
                        // Spotter is either ignored or not in the reviewed list which would mean they are new
                        if (listBoxIgnore is not null && listBoxIgnore.Items.Contains(spotterCall))
                        {
                            mutex.ReleaseMutex();
                            return "!!" + line[2..] + " Ignoring " + spotterCall + "\r\n";
                        }
                        if (checkedListBoxReviewed is not null && !checkedListBoxReviewed.Items.Contains(spotterCall))
                        {
                            mutex.ReleaseMutex();
                            if (checkListBoxNewSpotters is not null && !checkListBoxNewSpotters.Items.Contains(spotterCall))
                            {
                                checkListBoxNewSpotters.Items.Insert(0, spotterCall);
                            }
                            return "!!" + line[2..] + " Not reviewed " + spotterCall + "\r\n";
                        }
                        // Remove any suffix from special callsigns
                        String specialCall = HandleSpecialCalls(spottedCall);
#pragma warning disable CA1307 // Specify StringComparison
                        if (!specialCall.Equals(spottedCall))
#pragma warning restore CA1307 // Specify StringComparison
                        {
                            //#pragma warning disable CA1806 // Do not ignore method results
                            int n1 = spottedCall.Length;
                            String spaces = "               "[..(n1 - 3)];
                            swork = line.Replace(spottedCall, specialCall + spaces, StringComparison.InvariantCulture);
                            //#pragma warning restore CA1806 // Do not ignore method results
                        }
                        bool skimmer = swork.Contains("WPM CQ", StringComparison.InvariantCulture) || swork.Contains("BPS CQ", StringComparison.InvariantCulture) || swork.Contains("WPM BEACON", StringComparison.InvariantCulture) || swork.Contains("WPM NCDXF", StringComparison.InvariantCulture);
                        if ((line.Contains('-', StringComparison.InvariantCulture) && !ReviewedSpottersContains(spotterCall)) || (skimmer && ReviewedSpottersIsNotChecked(spotterCall)) || IgnoredSpottersContains(spotterCall))
                        {
                            filteredOut = true; // we dont' filter here if it's not a skimmer
                            if (!callSuffixList.Contains(tokens2[2]))
                            {
                                if (skimmer) callSuffixList.Add(spotterCall + ":SK");
                                else callSuffixList.Add(spotterCall + ":OK");
                            }
                        }
                        if (filterUSA)
                        {
                            // don't spot USA callsigns
                            var firstChar = spottedCall[..1];
                            if ((firstChar == "A" && spottedCall[1] <= 'L') || firstChar == "K" || firstChar == "N" || firstChar == "W")
                            {
                                filteredOut = true;
                            }
                        }
                        bool tooWeak = false;
                        if (skimmer) { // filter out CW below minimum dB level
                            if (Form1.TryParseSignalStrength(ss, out var signalStrength))
                            {
                                if (signalStrength < numericUpDownCwMinimum)
                                {
                                    filteredOut = true;
                                    tooWeak = true;
                                    ss = swork.Replace("DX de", "<< de", StringComparison.InvariantCulture);
                                    mutex.ReleaseMutex();
                                    return ss;
                                }
                            }
                        }
                        if (spotterCall == callSign || spottedCall == callSign)
                        {
                            filteredOut = false;
                        }
                        bool validCall = qrz.GetCallsign(spottedCall, out cachedQRZ);
                        if (!tooWeak && validCall && !filteredOut) // if it's not a skimmer just let it through as long as valid call and hasn't been excluded
                        {
                            ++totalLinesKept;
                            // we may have changed the freq so we add the change to log4omQueue
                            log4omQueue?.Add(swork + "\r\n");
                            try
                            {
                                File.AppendAllText(logFile, swork + "\r\n");
                            }
#pragma warning disable CA1031 // Do not catch general exception types
                            catch { }
#pragma warning restore CA1031 // Do not catch general exception types
#pragma warning disable CA1307 // Specify StringComparison
                            if (!swork.Equals(swork)) // then we'll also log the change
#pragma warning restore CA1307 // Specify StringComparison
                            {
                                try
                                {
                                    File.AppendAllText(logFile, swork + "\r\n");
                                }
#pragma warning disable CA1031 // Do not catch general exception types
                                catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                            }
                            sreturn += swork + "\r\n";
                            mutex.ReleaseMutex();
                            return sreturn;
                        }
                        if (cacheAdded)
                        {
                            cacheSpottedCalls[key] = minute;
                            ++totalLinesKept;
                            string sss = swork;
                            if (!validCall && cachedQRZ) // then the bad call is cached
                            {
                                sss = swork.Replace("DX de", "#* de", StringComparison.InvariantCulture);
                            }
                            else if (!validCall) // then first time QRZ tried it
                            {
                                sss = swork.Replace("DX de", "## de", StringComparison.InvariantCulture);
                            }
                            else
                            {
                                if (!tooWeak) log4omQueue?.Add(swork + "\r\n");
                                try
                                {
                                    File.AppendAllText(logFile, swork + "\r\n");
                                }
#pragma warning disable CA1031 // Do not catch general exception types
                                catch { }
#pragma warning restore CA1031 // Do not catch general exception types
#pragma warning disable CA1307 // Specify StringComparison
                                if (!swork.Equals(swork)) // then we'll also log the change
#pragma warning restore CA1307 // Specify StringComparison
                                {
                                    try
                                    {
                                        File.AppendAllText(logFile, swork + "\r\n");
                                    }
#pragma warning disable CA1031 // Do not catch general exception types
                                    catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                                }
                            }
                            sreturn += sss + "\r\n";
                        }
                        else if (myCallsign is not null && swork.Contains(myCallsign, StringComparison.InvariantCulture))  // allow our own spots through too
                        {
                            ++totalLinesKept;
                            log4omQueue?.Add(swork + "\r\n");
                            try
                            {
                                File.AppendAllText(logFile, swork + "\r\n");
                            }
#pragma warning disable CA1031 // Do not catch general exception types
                            catch { }
#pragma warning restore CA1031 // Do not catch general exception types
#pragma warning disable CA1307 // Specify StringComparison
                            if (!swork.Equals(swork)) // then we'll also log the change
#pragma warning restore CA1307 // Specify StringComparison
                            {
                                try
                                {
                                    File.AppendAllText(logFile, swork + "\r\n");
                                }
#pragma warning disable CA1031 // Do not catch general exception types
                                catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                            }
                            sreturn += swork + "\r\n";
                            myCallsignExists = true;
                            cachedQRZ = false;
                        }
                        else // need to check if QRZ is offline and do something about it
                        {
                            if (swork.Length > 2)
                            {
                                // ** -- our built-in cache item -- 10 minutes
                                // !! -- filtered out
                                // ## -- bad QRZ lookup
                                // %% -- QRZ cached good call
                                string tag = "**";
                                if (qrz.isOnline == false)
                                {
                                    tag = "ZZ"; // show QRZ is sleeping
                                    log4omQueue?.Add("QRZ not responding?\n");
                                    try
                                    {
                                        File.AppendAllText(logFile, line + "\r\n");
                                    }
#pragma warning disable CA1031 // Do not catch general exception types
                                    catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                                }
                                else if (!validCall)
                                {
                                    if (debug)
                                    {
                                        try
                                        {
                                            File.AppendAllText(pathQRZError, "!valid??: " + qrz.xml + "\n");
                                        }
#pragma warning disable CA1031 // Do not catch general exception types
                                        catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                                    }
                                    if (qrz.xmlError.Contains("Error", StringComparison.InvariantCulture))
                                    {
                                        log4omQueue?.Add(qrz.xmlError + "\n");
                                        try
                                        {
                                            File.AppendAllText(logFile, line + "\r\n");
                                        }
#pragma warning disable CA1031 // Do not catch general exception types
                                        catch { }
#pragma warning restore CA1031 // Do not catch general exception types
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
                                else if (tooWeak)
                                {
                                    tag = "<<";
                                }
                                else if (filteredOut)
                                {
                                    tag = "!!";
                                    //log4omQueue.Add(tag + s.Substring(2) + "\r\n");
                                }
                                sreturn += tag + swork[2..] + "\r\n";
                            }
                        }

                    }
                    else
                    {
                        // Once in a while the time isn't on the DX message so we just skip it
                        if (line.Contains("DX de", StringComparison.InvariantCulture) && line.Length < 74)
                            MessageBox.Show("Length wrong??\n" + line);
                        if (line.Length > 1) sreturn += line + "\r\n";
                    }
                }
                mutex.ReleaseMutex();
                return sreturn;
            }
            try
            {
                var msg = "";
                var bytes = Encoding.ASCII.GetBytes(msg);
                nStream?.Write(bytes, 0, bytes.Length);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {

                Disconnect();
                int n = 1;
                while(!Connect())
                {
                    debuglog.AppendText("Error connecting to cluster server, try#"+n+++"\n");
                    //MessageBox.Show("Error connecting to cluster server", "DXClusterUtil");
                    Thread.Sleep(60 * 1000); // 1 minute wait until retry
                }
            }
            mutex.ReleaseMutex();
            return null;
        }

        static private String HandleSpecialCalls(String callsign)
        {
            // strip suffixes from special event callsigns
            MatchCollection mc = Regex.Matches(callsign, "[WKN][0-9][A-WYZ]/");
            foreach (Match m in mc)
            {
                callsign = m.Value[..3];
            }
            return callsign;
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
        internal string? newSpotters;
        internal ColorCodedCheckedListBox? checkedListBoxReviewed;
        internal CheckedListBox? checkListBoxNewSpotters;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (client != null) { client.Dispose(); client = null; }
                    if (nStream != null) { nStream.Dispose(); nStream = null; }
                    //if (qrz != null) { qrz.Dispose(); qrz = null; }
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DxClusterClient()
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

        [GeneratedRegex("-[0-9]-#:")]
        private static partial Regex MyRegexSuffix();
        #endregion
    }
}
