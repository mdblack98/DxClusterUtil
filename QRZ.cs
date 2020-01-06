using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml;

namespace W3LPL
{
    class QRZ : IDisposable
    {
        const string server = "http://www.qrz.com/xml";
        private readonly DataSet QRZData = new DataSet("QData");
        private readonly WebClient wc = new WebClientWithTimeout();
        public bool isOnline = false;
        public string xmlSession = "";
        public string xmlError = "";
        public string xml = "";
        readonly string urlConnect = "";
        public bool debug = false;
        public ConcurrentDictionary<string, string> cacheQRZ = new ConcurrentDictionary<string, string>();
        public ConcurrentDictionary<string, string> cacheQRZBad = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentBag<string> aliasNeeded = new ConcurrentBag<string>();
        int stackcount = 0;
        public QRZ(string username, string password, string cacheFileName)
        {
            StreamReader aliasFile = new StreamReader("C:/Temp/qrzalias.txt");
            string s;
            while((s = aliasFile.ReadLine())!=null)
            {
                string[] tokens = s.Split(',');
                aliasNeeded.Add(tokens[0]);
            }
            aliasFile.Close();
            aliasFile.Dispose();
            if (debug) File.AppendAllText("C:/Temp/qrzerror.txt", "New QRZ instance\n");
            if (cacheQRZ.Count == 0)
            {
                CacheLoad(cacheFileName);
            }
            urlConnect = server + "?username=" + username + ";password=" + password;
            bool result = Connect(urlConnect);
            if (result == false)
            {
                isOnline = false;
            }
        }

        public void CacheSave(string filename)
        {
            if (cacheQRZ != null)
            {
                ConcurrentDictionary<string,string> tmpDict = new ConcurrentDictionary<string, string>();
                foreach (var d in cacheQRZ)
                {
                    if (!d.Value.Contains("BAD"))
                    {
                        tmpDict.TryAdd(d.Key, d.Value);
                    }
                }
                File.WriteAllText(filename, new JavaScriptSerializer().Serialize(tmpDict));
            }
        }

        private static string QRZField(DataRow row, string f)
        {
            if (row.Table.Columns.Contains(f)) return row[f].ToString(); else return "";
        }

        public bool GetCallsign(string callSign, out bool cached)
        {
            if (callSign.Length < 3)
            {
                cached = false;
                if (debug) File.AppendAllText("C:/Temp/qrzerror.txt", "Callsign length < 3\n");
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
            if (debug) File.AppendAllText("C:/Temp/qrzlog.txt", DateTime.Now.ToShortTimeString()+ " "+myurl+"\n");
            if (cacheQRZ.TryGetValue(callSign,out string validCall))
            { // it's in the cache so check our previous result for BAD
                if (debug) File.AppendAllText("C:/Temp/qrzlog.txt", callSignSplit +" in qrz cache validCall="+validCall+"\n");
                cached = true;
                if (validCall.Equals("BAD", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (debug) File.AppendAllText("C:/Temp/qrzerror.txt", "In cache as bad call for callsign="+callSignSplit+"\n");
                    xml = "Bad call cached="+callSignSplit+"\n";
                    return false;
                }
                return true;
            }
            else if (cacheQRZBad.TryGetValue(callSign, out _))
            {
                cached = true;
                return false;
            }
            if (debug) File.AppendAllText("C:/Temp/qrzlog.txt", callSignSplit + " not cached callsign=" + callSignSplit + "\n");
            cached = false;
            // Not in cache so have to look it up.
            bool valid;
            bool validfull;
            validfull = valid = CallQRZ(myurl, callSign, out _);
            if (!validfull) // if whole callsign isn't valid we'll try the split callsign
            {
                valid = CallQRZ(myurl, callSignSplit, out string email2);
                int n = 0;
                while (!isOnline)
                {
                    Thread.Sleep(5000);
                    ++n;
                    if (debug) File.AppendAllText("C:/Temp/qrzlog.txt","QRZ not online...retrying " + n + "\n");
                    Connect(urlConnect);
                }
                if (!validfull && valid)
                {
                    if (!aliasNeeded.Contains(callSign))
                    {
                        File.AppendAllText("C:/Temp/qrzalias.txt", callSign + "," + email2 + "\n");
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
                if (debug) File.AppendAllText("C:/Temp/qrzerror.txt", "Not valid after CallQRZ xml="+xmlError+"\n");
            }
            if (!isOnline)
            {
                if (debug) File.AppendAllText("C:/Temp/qrzerror.txt", "Not online??\n");
            }
            return valid;
        }
        //
        public bool Connect(string url) // CallQRZ for getting sessionid
        {
            return CallQRZ(url,"",out _);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public bool CallQRZ(string url, string call, out string email)
        {
            ++stackcount;
            if (stackcount > 1)
            {
                MessageBox.Show("CallQRZ is recursing...should not see this!!!");
            }
            email = "none";
            Stream qrzstrm = null;
            try
            {
                QRZData.Clear();
                try
                {
                    if (call.Length > 0) url = url + ";callsign=" + call;
                    qrzstrm = wc.OpenRead(url);
                    //var settings = new XmlReaderSettings();
                    //settings.XmlResolver = ;
                    //XmlReader xMLReader = XmlReader.Create(url,settings);
                    //string xml = xMLReader.ReadContentAsString();
                    //_ = QRZData.ReadXml(xMLReader);
                    //_ = QRZData.ReadXml(xml);
#pragma warning disable CA5366 // Use XmlReader For DataSet Read Xml
#pragma warning disable CA3075 // Insecure DTD processing in XML
                    if (qrzstrm != null)
                    {
                        _ = QRZData.ReadXml(qrzstrm, XmlReadMode.InferSchema);
#pragma warning restore CA3075 // Insecure DTD processing in XML
#pragma warning restore CA5366 // Use XmlReader For DataSet Read Xml
                        xml = QRZData.GetXml();
                        qrzstrm.Close();
                    }
                    else { --stackcount;  return false;  }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    xmlError = "QRZ Error: " + ex.Message;
                    isOnline = false;
                    if (qrzstrm != null)
                    {
                        qrzstrm.Close();
                        qrzstrm.Dispose();
                    }
                    --stackcount;
                    return false;
                }
                //xMLReader.Dispose();
                if (!QRZData.Tables.Contains("QRZDatabase"))
                {
                    //MessageBox.Show("Error: failed to receive QRZDatabase object", "XML Server Error");
                    isOnline = false;
                    --stackcount;
                    return false;
                }
                DataRow dr = QRZData.Tables["QRZDatabase"].Rows[0];
                //Lversion.Text = QRZField(dr, "version");
                if (url.Contains("username"))
                {
                    DataTable sess = QRZData.Tables["Session"];
                    DataRow sr = sess.Rows[0];
                    string xx = QRZData.GetXml();
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
                    DataTable sess = QRZData.Tables["Session"];
                    DataRow sr = sess.Rows[0];
                    string xmlError = QRZField(sr, "Error");
                    xmlSession = QRZField(sr, "Key");
                    if (xmlError.Contains("Not found"))
                    {
                        File.AppendAllText("c:/Temp/w3lpl_bad.txt",call+"\n");
                        StreamWriter badFile = new StreamWriter("C:/Temp/w3lpl_bad.txt",true);
                        badFile.WriteLine(call);
                        badFile.Close();
                        --stackcount;
                        return false;
                    }
                    else if (xmlError.Contains("password") || xmlError.Contains("Timeout"))
                    {
                        isOnline = false;
                        //Connect(urlConnect);
                        --stackcount;
                        return isOnline;
                    }
                    else if (xmlError.Length > 0)
                    {
                        if (debug) File.AppendAllText("C:/Temp/qrzerror.txt", xml);
                    }
                    DataTable callTable = QRZData.Tables["Callsign"];
                    if (callTable.Rows.Count == 0) return false;
                    dr = callTable.Rows[0];
                    email = QRZField(dr, "email");
                    string fname = QRZField(dr, "fname");
                    if (email.Length == 0) email = "none" + "," + fname;
                    else email = email + "," + fname;
                }

            }
            catch (Exception err)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                _ = MessageBox.Show(err.Message, "XML Error"+err.StackTrace);
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                throw;
            }
            isOnline = (xmlSession.Length > 0) ? true : false;
            --stackcount;
            return true;
        }
        private void CacheLoad(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    cacheQRZ = new JavaScriptSerializer().Deserialize<ConcurrentDictionary<string, string>>(File.ReadAllText(filename));
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
            wc.Dispose();
        }
        public class WebClientWithTimeout : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest wr = base.GetWebRequest(address);
                wr.Timeout = 5000; // timeout in milliseconds (ms)
                return wr;
            }
        }
    }
}
