using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
        readonly string url = "";
        public ConcurrentDictionary<string, string> cacheQRZ = new ConcurrentDictionary<string, string>();
        public QRZ(string username, string password, string cacheFileName)
        {
            if (cacheQRZ.Count == 0)
            {
                CacheLoad(cacheFileName);
            }
            url = server + "?username=" + username + ";password=" + password;
            bool result = Connect(url);
            if (result == false)
            {
                isOnline = false;
            }
        }

        public void CacheSave(string filename)
        {
            if (cacheQRZ != null)
            {
                File.WriteAllText(filename, new JavaScriptSerializer().Serialize(cacheQRZ));
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
                return false; // no 2-char callsigns
            }
            //callSign = "5P9Z/P";
            // XML Lookup can fail on suffixes
            callSign = callSign.Split('/')[0];
            string myurl = server + "?s=" + xmlSession + ";callsign=" + callSign;
            if (cacheQRZ.TryGetValue(callSign,out string validCall))
            { // it's in the cache so check our previous result for BAD
                cached = true;
                if (validCall.Equals("BAD",StringComparison.InvariantCultureIgnoreCase)) return false;
                return true;
            }
            cached = false;
            // Not in cache so have to look it up.
            bool valid = CallQRZ(myurl,callSign);
            string callValid = "BAD";
            if (valid) callValid = DateTime.UtcNow.ToShortDateString();
            if (!cacheQRZ.TryAdd(callSign, callValid))
            {
                MessageBox.Show("Error adding " + callSign + "/" + callValid + "to QRZ cache???");
            }
            return valid;
        }
        //
        public bool Connect(string url) // CallQRZ for getting sessionid
        {
            return CallQRZ(url,"");
        }
        public bool CallQRZ(string url, string call)
        {
            Stream qrzstrm;
            try
            {
                QRZData.Clear();
                try
                {
                    qrzstrm = wc.OpenRead(url);
                    //var settings = new XmlReaderSettings();
                    //settings.XmlResolver = ;
                    //XmlReader xMLReader = XmlReader.Create(url,settings);
                    //string xml = xMLReader.ReadContentAsString();
                    //_ = QRZData.ReadXml(xMLReader);
                    //_ = QRZData.ReadXml(xml);
#pragma warning disable CA5366 // Use XmlReader For DataSet Read Xml
#pragma warning disable CA3075 // Insecure DTD processing in XML
                    _ = QRZData.ReadXml(qrzstrm, XmlReadMode.InferSchema);
#pragma warning restore CA3075 // Insecure DTD processing in XML
#pragma warning restore CA5366 // Use XmlReader For DataSet Read Xml
                    string xml = QRZData.GetXml();
                    qrzstrm.Close();
                }
                catch (Exception ex)
                {
                    xmlError = "QRZ Error: " + ex.Message;
                    isOnline = false;
                    return false;
                }
                //xMLReader.Dispose();
                if (!QRZData.Tables.Contains("QRZDatabase"))
                {
                    //MessageBox.Show("Error: failed to receive QRZDatabase object", "XML Server Error");
                    isOnline = false;
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
                        StreamWriter badFile = new StreamWriter("C:/Temp/w3lpl_bad.txt",true);
                        badFile.WriteLine(call);
                        badFile.Close();
                        return false;
                    }
                    //DataTable callTable = QRZData.Tables["Callsign"];
                    //if (callTable == null) return false;
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
