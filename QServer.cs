using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DXClusterUtil
{
    class QServer : IDisposable
    {
        readonly ConcurrentBag<string> clientQueue;
        readonly ConcurrentBag<string> spotQueue;
        bool running = false;
        bool stop = false;
        bool connected;
        private TcpListener? listener;
        NetworkStream? stream;
        Thread? myThreadID;

        public int TimeIntervalAfter { get; set; } // in seconds
        public int TimeInterval { get; set; } // Expecting 1, 15, 30, 60 

        public QServer(int port, ConcurrentBag<string> clientQ, ConcurrentBag<string> spotQ)
        {
            clientQueue = clientQ;
            spotQueue = spotQ;
            if (listener == null)
            {
                try
                {
                    listener = new TcpListener(IPAddress.Any, port);
                    //listener.Start();
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    _ = MessageBox.Show("Error starting DXClusterUtil server...another copy running?  Exiting DXClusterUtil", "DXClusterUtil", MessageBoxButtons.OK);
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                    return;
                }
            }
            connected = true;
        }

        ~QServer()
        {
            Stop();
        }

        public bool IsConnected()
        {
            return connected;
        }
        public void Stop()
        {
            running = false;
            stop = true;
            connected = false;
            listener?.Stop();
            if (listener is not null) listener = null;
            Thread.Sleep(500);
        }

        void ReadThread()
        {
            connected = true;
            while (connected)
            {
                try
                {
                    var bytes = new byte[8192];
                    if (stream is NetworkStream networkStream)
                    {
                        networkStream.ReadTimeout = 5000; // Set timeout to 5000ms (5 seconds)
                    }
                    int? bytesRead = stream?.Read(bytes, 0, bytes.Length);
                    if (bytesRead.HasValue && bytesRead.Value == 0)
                    {
                        connected = false;
                    }
                    if (bytesRead is null)
                    {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                        MessageBox.Show("bytesRead is null?");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                    }
                    if (bytesRead is not null)
                    {
                        string cmd = Encoding.ASCII.GetString(bytes, 0, (int)bytesRead);
                        if (cmd.Contains("bye", StringComparison.InvariantCulture))
                        {
                            //connected = running = false;
                        }
                        else if (bytesRead == 0)
                        {
                            connected = false;
                            running = false;
                        }
                        else
                        {
                            if (cmd.Length > 0)
                            {
                                spotQueue?.Add(cmd);
                            }
                        }
                    }
                }
                catch (IOException ex) when (ex.InnerException is SocketException socketEx &&
                                              socketEx.SocketErrorCode == SocketError.TimedOut)
                {
                    //Console.WriteLine("Read timeout occurred.");
                    connected = true;
                }
                catch (IOException)
                {
                    //Console.WriteLine($"IO error: {ex.Message}");
                    connected = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unexpected error: {ex.Message}");
                    connected = false;
                    throw;
                }
            }
        }

        public void Start()
        {
            while (true && !stop)
            {
                if (listener != null)
                {
                    listener?.Stop();
                    listener?.Start();
                }
                running = true;
                TcpClient? client;
                try
                {
                    client = listener?.AcceptTcpClient();
                    //Thread.Sleep(500);
                    connected = true;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch 
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    running = false;
                    return;
                }
                if (client is not null)
                {
                    client.ReceiveTimeout = 1000;
                    client.SendTimeout = 1000;
                    stream = client?.GetStream();
                }
                myThreadID = new Thread(new ThreadStart(ReadThread));
                myThreadID?.Start();
                byte[] bytes;
                while (running)
                {
                    try
                    {
                        var seconds = DateTime.Now.Second % TimeInterval;
                        var secondsChk = TimeInterval;
                        if (TimeInterval > 1) secondsChk += TimeIntervalAfter;
                        secondsChk %= TimeInterval;
                        if (seconds == secondsChk)
                        {
                            // Let the clock get past the zero second mark
                            while (clientQueue.TryTake(out string? msg))
                            {
                                /*
                                msg = msg.Replace("-1-#:", "-#:  ");
                                msg = msg.Replace("-2-#:", "-#:  ");
                                msg = msg.Replace("-3-#:", "-#:  ");
                                msg = msg.Replace("-4-#:", "-#:  ");
                                msg = msg.Replace("-5-#:", "-#:  ");
                                msg = msg.Replace("-6-#:", "-#:  ");
                                msg = msg.Replace("-7-#:", "-#:  ");
                                msg = msg.Replace("-8-#:", "-#:  ");
                                msg = msg.Replace("-9-#:", "-#:  ");
                                */
                                // we'll only send the ones that aren't dups
                                if (msg[0] != '*')
                                {
                                    bytes = Encoding.ASCII.GetBytes(msg);
                                    stream?.Write(bytes, 0, bytes.Length);
                                }
                            }
                            Thread.Sleep(2000);
                        }
                        if (stream is not null && !stream.CanWrite)
                            running = false;
                        if (client is not null && !client.Connected)
                        {
                            connected = false;
                            running = false;
                        }
                        else
                        {
                            //connected = true;
                        }
                        // Let's see if the client wants to send stuff
                        byte[] tmp = new byte[1];
                        //stream.Socket.Write(tmp, 0, 0);
                        //var xxx = stream.Read(tmp, 0, 0);
                        var xx = stream?.Socket.IsBound;
                        if (client is not null && !client.Connected)
                        {
                            connected = running = false;
                        }
                        /*
                        while (stream.DataAvailable && connected)
                        {
                            bytes = new byte[8192];
                            int bytesRead = stream.Read(bytes, 0, bytes.Length);
                            string cmd = Encoding.ASCII.GetString(bytes, 0, bytesRead);
                            if (cmd.Contains("bye",StringComparison.InvariantCulture))
                            {
                                //connected = running = false;
                            }
                            else
                            {
                                spotQueue.Add(cmd);
                            }

                        }
                        */
                        //msg = "";
                        //stream.Write(bytes, 0, bytes.Length);
                        Thread.Sleep(200);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        //if (!WSAGetLastError() == 10053)
                        //{
                        MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                        //}
                        running = false;
                    }
                }
                stream?.Close();
                if (client is not null && client.Connected) 
                    client?.Close();
                connected = false;
            }
            listener?.Stop();

        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            //throw new NotImplementedException();
        }
    }
}
