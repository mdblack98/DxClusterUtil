using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace DXClusterUtil
{
    sealed class QRZ : IDisposable
    {
        const string server = "http://xmldata.qrz.com/xml/current";
        private readonly DataSet QRZData = new("QData");
        private readonly MessageSender wc = new();
        public bool isOnline = false;
        public string xmlSession = "";
        public string xmlError = "";
        public string xml = "";
        readonly string urlConnect = "";
        public bool debug = false;
        public ConcurrentDictionary<string, string> cacheQRZ = new();
        public ConcurrentDictionary<string, string> cacheQRZBad = new();
        private readonly ConcurrentBag<string> aliasNeeded = new();
        readonly Mutex mutexQRZ = new();
        readonly private string pathQRZAlias = Environment.ExpandEnvironmentVariables("%TEMP%\\qrzalias.txt");
        readonly private string pathQRZLog = Environment.ExpandEnvironmentVariables("%TEMP%\\qrzlog.txt");
        //readonly private string pathQRZError = Environment.ExpandEnvironmentVariables("%TEMP%\\qrzerror.txt");
        readonly private string pathQRZBad = Environment.ExpandEnvironmentVariables("%TEMP%\\qrzbad.txt");
        readonly private string pathQRZCache = Environment.ExpandEnvironmentVariables("%TEMP%\\qrzcache.txt");

        public QRZ(string username, string password)
        {
            try
            {
                File.Delete(pathQRZLog);
                //File.Delete(pathQRZError);
                File.Delete(pathQRZBad);
                if (!File.Exists(pathQRZAlias))
                {
                    var stream = File.Create(pathQRZAlias);
                    stream.Dispose();

                }
                StreamReader aliasFile = new(pathQRZAlias);
                string? s;
                while ((s = aliasFile?.ReadLine()) != null)
                {
                    string[] tokens = s.Split(',');
                    aliasNeeded.Add(tokens[0]);
                }
                aliasFile?.Close();
                aliasFile?.Dispose();
                if (debug)
                {
                    try
                    {
                        File.AppendAllText(pathQRZAlias, "New QRZ instance\n");
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                }
                if (cacheQRZ.IsEmpty)
                {
                    CacheLoad(pathQRZCache);
                }
                urlConnect = server + "?username=" + username + ";password=" + password;
                bool result = Connect(urlConnect);
                if (result == false)
                {
                    isOnline = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("QRZ connect error:: " + ex.Message);
                throw;
            }
        }
        
        public void CacheSave(string filename)
        {
            if (cacheQRZ != null)
            {
                ConcurrentDictionary<string, string> tmpDict = new();
                foreach (var d in cacheQRZ)
                {
                    if (!d.Value.Contains("BAD", StringComparison.InvariantCulture))
                    {
                        tmpDict.TryAdd(d.Key, d.Value);
                    }
                }
                File.WriteAllText(filename, JsonSerializer.Serialize(tmpDict));
            }
        }

        private static string QRZField(DataRow? row, string f)
        {
            if (row is null) return "row is null?";
            if (row.Table.Columns.Contains(f))
            {
                return row[f]?.ToString() ?? "";
            }
            else
            {
                return "";
            }
        }

        public bool GetCallsign(string callSign, out bool cached)
        {
            if (callSign.Length < 3)
            {
                cached = false;
                if (debug)
                {
                    try
                    {
                        File.AppendAllText(pathQRZLog, "Callsign length < 3\n");
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                }
                xml = "callSign Length < 3 =" + callSign + "\n";
                return false; // no 2-char callsigns
            }
            //callSign = "5P9Z/P";
            // XML Lookup can fail on suffixes
            string callSignSplit = callSign;
            string[] tokens = callSignSplit.Split('/');
            if (tokens.Length > 1)
            {
                if (tokens[1].Length > tokens[0].Length) callSignSplit = tokens[1];
                else callSignSplit = tokens[0];
            }
            string myurl = server + "?s=" + xmlSession;
            if (debug)
            {
                try
                {
                    File.AppendAllText(pathQRZLog, DateTime.Now.ToShortTimeString() + " " + myurl + "\n");
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch { }
#pragma warning restore CA1031 // Do not catch general exception types
            }
            if (cacheQRZ.TryGetValue(callSign, out string? validCall))
            { // it's in the cache so check our previous result for BAD
                if (debug)
                {
                    try
                    {
                        File.AppendAllText(pathQRZLog, callSignSplit + " in qrz cache validCall=" + validCall + "\n");
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                }
                cached = true;
                if (validCall.Equals("BAD", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (debug)
                    {
                        try
                        {
                            File.AppendAllText(pathQRZLog, "In cache as bad call for callsign=" + callSignSplit + "\n");
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                    }
                    xml = "Bad call cached=" + callSignSplit + "\n";
                    return false;
                }
                return true;
            }
            else if (cacheQRZBad.TryGetValue(callSign, out _))
            {
                cached = true;
                return false;
            }
            if (debug)
            {
                try
                {
                    File.AppendAllText(pathQRZLog, callSignSplit + " not cached callsign=" + callSignSplit + "\n");
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch { }
#pragma warning restore CA1031 // Do not catch general exception types
            }
            cached = false;
            // Not in cache so have to look it up.
            bool valid;
            bool validfull;
            validfull = valid = CallQRZ(myurl, callSign, out _, out _);
            if (!validfull) // if whole callsign isn't valid we'll try the split callsign
            {
                valid = CallQRZ(myurl, callSignSplit, out string email2, out _);
                int n = 0;
                while (!isOnline)
                {
                    Thread.Sleep(5000);
                    ++n;
                    if (debug)
                    {
                        try
                        {
                            File.AppendAllText(pathQRZLog, "QRZ not online...retrying " + n + "\n");
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                    }
                    Connect(urlConnect);
                }
                if (!validfull && valid)
                {
                    if (!aliasNeeded.Contains(callSign))
                    {
                        try
                        {
                            File.AppendAllText(pathQRZAlias, callSign + "," + email2 + "\n");
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                        aliasNeeded.Add(callSign);
                    }
                }
            }
            string callValid = "BAD";
            if (valid) callValid = DateTime.UtcNow.ToShortDateString();
            if (isOnline & !valid)
            {
                cacheQRZBad.TryAdd(callSign, "BAD");
            }
            else if (isOnline && !cacheQRZ.TryAdd(callSign, callValid))
            {
                MessageBox.Show("Error adding " + callSignSplit + "/" + callValid + " to QRZ cache???");
            }
            if (!valid)
            {
                if (debug)
                {
                    try
                    {
                        File.AppendAllText(pathQRZLog, "Not valid after CallQRZ xml=" + xmlError + "\n");
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                }
            }
            if (!isOnline)
            {
                if (debug)
                {
                    try
                    {
                        File.AppendAllText(pathQRZLog, "Not online??\n");
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                }
            }
            return valid;
        }
        //
        public bool Connect(string url) // CallQRZ for getting sessionid
        {
            return CallQRZ(url, "", out _, out _);
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public bool CallQRZ(string url, string call, out string email, out int qrzdxcc)
        {
            qrzdxcc = 0;
            mutexQRZ.WaitOne();
            email = "none";
            String qrzstr;
            Stream? qrzstrm = null;
            try
            {
                QRZData.Clear();
                try
                {
                    if (call.Length > 0) url = url + ";callsign=" + call;
                    var uri = new System.Uri(url);
                    qrzstr = wc.GetMessage(uri);
                    //var settings = new XmlReaderSettings();
                    //settings.XmlResolver = ;
                    //XmlReader xMLReader = XmlReader.Create(url,settings);
                    //string xml = xMLReader.ReadContentAsString();
                    //_ = QRZData.ReadXml(xMLReader);
                    //_ = QRZData.ReadXml(xml);
                    byte[] byteArray = Encoding.ASCII.GetBytes(qrzstr);
                    qrzstrm = new MemoryStream(byteArray);
#pragma warning disable CA5366 // Use XmlReader For DataSet Read Xml
                    if (qrzstrm != null)
                    {
                        _ = QRZData.ReadXml(qrzstrm, XmlReadMode.InferSchema);
#pragma warning restore CA5366 // Use XmlReader For DataSet Read Xml
                        
                        xml = QRZData.GetXml();
                        qrzstrm.Close();
                    }
                    else { mutexQRZ.ReleaseMutex(); return false; }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    xmlError = "QRZ Error: " + ex.Message;
                    isOnline = false;
                    if (qrzstrm != null)
                    {
                        //qrzstrm.Close();
                        //qrzstrm.Dispose();
                    }
                    mutexQRZ.ReleaseMutex();
                    return false;
                }
                //xMLReader.Dispose();
                if (!QRZData.Tables.Contains("QRZDatabase"))
                {
                    //MessageBox.Show("Error: failed to receive QRZDatabase object", "XML Server Error");
                    isOnline = false;
                    mutexQRZ.ReleaseMutex();
                    return false;
                }
                if (QRZData is null || QRZData.Tables is null)
                {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    MessageBox.Show("QRZDatabase??");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                    return false;
                }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                DataRow dr = QRZData.Tables["QRZDatabase"].Rows[0];
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                              //Lversion.Text = QRZField(dr, "version");
                if (url.Contains("username", StringComparison.InvariantCulture))
                {
                    if (QRZData is null)
                    {
                        MessageBox.Show("QRZData is null??");
                        return false;
                    }
                    DataTable? sess = null;
                    if (QRZData is not null) sess = QRZData.Tables["Session"];
                    if (sess is null)
                    {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                        MessageBox.Show("Session??");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                        return false;
                    }    
                    DataRow sr = sess.Rows[0];
                    string? xx = null;
                    if (QRZData is not null) xx = QRZData.GetXml();
                    xmlError = QRZField(sr, "Error");
                    if (xmlError.Length > 0) return false;
                    if (QRZField(sr, "Key").Length > 0)
                    {
                        xmlSession = QRZField(sr, "Key");
                    }
                }
                else
                {
                    string version = QRZField(dr, "version");
                    //if (version.Equals("1.24")) MessageBox.Show("Version != 1.24, ==" + version);
                    DataTable? sess = QRZData.Tables["Session"];
                    DataRow? sr = sess?.Rows[0];
                    string xmlError = QRZField(sr, "Error");
                    xmlSession = QRZField(sr, "Key");
                    if (xmlError.Contains("Not found", StringComparison.InvariantCulture))
                    {

                        try
                        {
                            File.AppendAllText(pathQRZBad, call + "\n");
                            StreamWriter badFile = new(pathQRZBad, true);
                            badFile.WriteLine(call);
                            badFile.Close();
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                        mutexQRZ.ReleaseMutex();
                        return false;
                    }
                    else if (xmlError.Contains("password", StringComparison.InvariantCulture) || xmlError.Contains("Timeout", StringComparison.InvariantCulture))
                    {
                        isOnline = false;
                        //Connect(urlConnect);
                        mutexQRZ.ReleaseMutex();
                        return isOnline;
                    }
                    else if (xmlError.Length > 0)
                    {
                        if (debug)
                        {
                            try
                            {
                                File.AppendAllText(pathQRZLog, xml);
                            }
#pragma warning disable CA1031 // Do not catch general exception types
                            catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                        }
                    }
                    DataTable? callTable = QRZData.Tables?["Callsign"];
                    if (callTable == null)
                    {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                        MessageBox.Show("callTable problem?");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                        return false;
                    }
                    if (callTable.Rows.Count == 0) return false;
                    dr = callTable.Rows[0];
                    string callsign = QRZField(dr, "Call");
                    if (callsign.Length == 0) return false;
                    email = QRZField(dr, "email");
                    string fname = QRZField(dr, "fname");
                    if (email.Length == 0) email = "none" + "," + fname;
                    else email = email + "," + fname;
#pragma warning disable CA1305 // Specify IFormatProvider
                    string dxcc = QRZField(dr, "dxcc");
                    if (dxcc != null && dxcc.Length > 0)
                        qrzdxcc = Convert.ToInt32(QRZField(dr, "dxcc"));
                    else
                    {
                        MessageBox.Show("DXClusterUtil says callsign " + callsign + " does not have DXCC field in QRZ xml...need to notify the operator!!\n" + xml);
                        File.AppendAllText(pathQRZLog, "DXClusterUtil says callsign " + callsign + " does not have DXCC field in QRZ xml...need to notify the operator!!\n" + xml +"\n");
                    }

#pragma warning restore CA1305 // Specify IFormatProvider
                }

            }
            catch (Exception err)
            {
                mutexQRZ.ReleaseMutex();
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                _ = MessageBox.Show(err.Message + "\n" + err.StackTrace + "\n" + xml, "XML Error");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                throw;
            }
            isOnline = (xmlSession.Length > 0);
            mutexQRZ.ReleaseMutex();
            return true;
        }
        private void CacheLoad(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    var cacheData = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(File.ReadAllText(filename));
                    if (cacheData != null)
                    {
                        cacheQRZ = cacheData;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Dispose()
        {
            QRZData.Dispose();
            mutexQRZ.Dispose();
            wc.Dispose();
        }
    }
    public sealed class MessageSender : IDisposable
    {
        private readonly HttpClient httpClient;
        bool disposed = true;
        public MessageSender()
        {
            //Create the request sender object
            httpClient = new()
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            disposed = false;
            //Set the headers
            //httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("MessageService/3.1");

        }
        public string GetMessage(Uri uri)
        {
            /* Initialize the request content 
               and 
               Send the POST
            */
            //var response = await httpClient.GetAsync(uri).ConfigureAwait(false);
            var response = httpClient.GetAsync(uri).GetAwaiter().GetResult();

            //Check for error status
            response.EnsureSuccessStatusCode();

            //Get the response content
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        public void Dispose()
        {
            if (!this.disposed)
                httpClient?.Dispose();
            if (this != null) GC.SuppressFinalize(this);
            disposed = true;
        }
    }
}

    
