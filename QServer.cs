using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace W3LPL
{
    class QServer
    {
        readonly ConcurrentBag<string> clientQueue;
        readonly ConcurrentBag<string> w3lplQueue;
        bool running = false;
        bool stop = false;
        bool connected;
        readonly TcpListener listener;

        public QServer(int port, ConcurrentBag<string> clientQ, ConcurrentBag<string> w3lplQ)
        {
            clientQueue = clientQ;
            w3lplQueue = w3lplQ;
            if (listener == null)
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
            }
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
            listener.Stop();
            Thread.Sleep(500);
        }
        public void Start()
        {
            while (true && !stop)
            {
                running = true;
                TcpClient client;
                try
                {
                    client = listener.AcceptTcpClient();
                }
                //catch (Exception ex)
#pragma warning disable CA1031 // Do not catch general exception types
                catch 
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    running = false;
                    //MessageBox.Show("W3LPL client socket error\n" + ex.Message);
                    return;
                }
                client.ReceiveTimeout = 1000;
                client.SendTimeout = 1000;
                NetworkStream stream = client.GetStream();
                byte[] bytes;
                string msg;
                while (running)
                {
                    try
                    {
                        var myTime = DateTime.Now;
                        if (myTime.Second == 0)
                        {
                            // Let the clock get past the zero second mark
                            while(clientQueue.TryTake(out msg))
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
                                    stream.Write(bytes, 0, bytes.Length);
                                }
                            }
                            Thread.Sleep(2000);
                        }
                        if (!stream.CanWrite)
                            running = false;
                        if (!client.Connected)
                        {
                            connected = false;
                            running = false;
                        }
                        else
                        {
                            connected = true;
                        }
                        // Let's see if the client wants to send stuff
                        while (stream.DataAvailable)
                        {
                            bytes = new byte[8192];
                            int bytesRead = stream.Read(bytes, 0, bytes.Length);
                            w3lplQueue.Add(Encoding.ASCII.GetString(bytes, 0, bytesRead));
                        }
                        msg = "";
                        bytes = Encoding.ASCII.GetBytes(msg);
                        stream.Write(bytes, 0, bytes.Length);
                        Thread.Sleep(200);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        //if (!WSAGetLastError() == 10053)
                        //{
                        //    MessageBox.Show(ex.Message + "\n" + ex.StackTrace, "W3LPL");
                        //}
                        running = false;
                    }
                }
                stream.Close();
                client.Close();
                connected = false;
            }
        }
    }
}
