using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    #region "Definitions"
    private static int TimeoutTime = 5;
    private static int BufferSize = 1024;
    private static TcpListener server = null;
    public static bool ServerStarted = false;
    private static int HeaderLen = 15;// Communication.HeaderLength;
    private static byte StartByte = 65;// Communication.StartByte;
    public static bool _isClientConnected = false;
    private static TcpClient client;
    #endregion

    public static bool StartListener()
    {
        try
        {
            Console.WriteLine("Waiting for new connection...");
            client = server.AcceptTcpClient();
            _isClientConnected = true;
            IPEndPoint endPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            IPAddress ClientIpAddress = endPoint.Address;
            Console.WriteLine("Connected to: " + ClientIpAddress);
            return _isClientConnected;
        }
        catch (Exception e)
        {
            Console.WriteLine("Client Failed to connect!  " + e.ToString());
            return false;
        }
    }
    public static bool SetupServer(int port)
    {
        bool Success = false;
        try
        {
            ServerStarted = true;
            IPAddress localAddr = Dns.GetHostAddresses(Dns.GetHostName())[1];
            //IPAddress localAddr = IPAddress.Parse(IP);
            server = new TcpListener(localAddr, port);
            server.Start();
            Success = true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Client Failed to connect!  " + e.ToString());
            Success = false;
        }
        return Success;
    }
    public static void CloseServer()
    {
        try
        {
            server.Server.Close();
            server.Stop();
            Console.WriteLine("Server has been stopped");
            ServerStarted = false;
        }
        catch
        {
            Console.WriteLine("could not close the Server ");
        }
    }
    public static bool SendDataToClient(byte[] Data)
    {
        bool success = false;
        if (client.Connected)
        {
            try
            {
                var stream = client.GetStream();
                stream.Write(Data, 0, Data.Length);
                success = true;
            }
            catch
            {
                Console.WriteLine("Unable to Send Message");
                _isClientConnected = false;
                client.Close();
                client.Dispose();
            }
        }
        return success;
    }
    public static bool SendMessage(string msg)
    {
        byte[] message = Encoding.ASCII.GetBytes(msg);
        byte[] DataToSend = new byte[message.Length + 6];
        int len = message.Length;
        DataToSend[0] = StartByte;
        DataToSend[3] = (byte)(len & 0xff);
        DataToSend[4] = (byte)((len >> 8) & 0xff);
        DataToSend[5] = (byte)((len >> 16) & 0xff);
        Array.Copy(message, 0, DataToSend, 6, len);
        return SendDataToClient(DataToSend);
    }
    public static byte[] GetData()
    {
        if (_isClientConnected)
        {
            try
            {
                var stream = client.GetStream();
                if (stream.DataAvailable)
                {
                    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                    System.Diagnostics.Stopwatch watchdog = new System.Diagnostics.Stopwatch();
                    watchdog.Restart();
                    bool _isDataRececived = false;


                    byte[] data = new byte[BufferSize];
                    using (MemoryStream ms = new MemoryStream())
                    {

                        int numBytesRead;
                        bool _isfirstSampleReceived = false;
                        int totalbytesReceived = 0;
                        byte[] Header = new byte[BufferSize];
                        int DataLength = 0;
                        while (watchdog.ElapsedMilliseconds < TimeoutTime)
                        {
                            if (!_isfirstSampleReceived)
                            {
                                watch.Restart();
                                numBytesRead = stream.Read(data, 0, HeaderLen);
                                if (numBytesRead == HeaderLen)
                                {
                                    DataLength = data[3] | (data[4] << 8) | (data[5] << 16);
                                    _isfirstSampleReceived = true;
                                    ms.Write(data, 0, numBytesRead);
                                    watchdog.Restart();
                                }
                                else
                                {
                                    Console.WriteLine("Missing Header Bytes!");
                                    break;
                                }
                            }
                            else
                            {
                                if (DataLength <= BufferSize)
                                {
                                    numBytesRead = stream.Read(data, 0, DataLength);
                                    totalbytesReceived += numBytesRead;
                                    ms.Write(data, 0, numBytesRead);
                                    watchdog.Restart();
                                }
                                else
                                {
                                    int Len = BufferSize;
                                    while ((numBytesRead = stream.Read(data, 0, Len)) > 0)
                                    {
                                        watchdog.Restart();
                                        ms.Write(data, 0, numBytesRead);
                                        totalbytesReceived += numBytesRead;
                                        Len = Math.Min(DataLength - totalbytesReceived, BufferSize);
                                    }
                                }
                                if ((totalbytesReceived) >= DataLength)
                                {
                                    _isDataRececived = true;
                                    break;
                                }
                            }

                            if (_isDataRececived)
                                break;
                        }
                        watchdog.Stop();
                        if (watchdog.ElapsedMilliseconds >= TimeoutTime)
                            Console.WriteLine("Timeout");
                        if (_isDataRececived)
                        {
                            watch.Stop();
                            //Console.WriteLine("time: " + watch.Elapsed.TotalMilliseconds + " ms");
                            byte[] ReceivedData = new byte[ms.Length];
                            ReceivedData = ms.ToArray();
                            if (ReceivedData[0] == StartByte)
                            {
                                int DataLen = ReceivedData[3] | (ReceivedData[4] << 8) | (ReceivedData[5] << 16);
                                if (DataLen == ReceivedData.Length - 6)
                                {
                                    return ReceivedData;
                                }
                                else
                                {
                                    Console.WriteLine("Data Length does not match! Told: " + DataLen + " But received: " + (ReceivedData.Length - 6));
                                    return null;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Start byte was Wrong! received: " + ReceivedData[0]);
                                Console.WriteLine("Data: " + Encoding.ASCII.GetString(ReceivedData, 0, ReceivedData.Length));
                                stream.Flush();
                                return null;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Data is not received!");
                            return null;
                        }
                    }
                }
                else
                {
                    //Console.WriteLine("Data is not Available");
                    return null;
                }
            }
            catch
            {
                Console.WriteLine("Receive Data Failed!");
                _isClientConnected = false;
                client.Dispose();
                return null;
            }
        }
        else
        {
            Console.WriteLine("Client is not connected!");
            _isClientConnected = false;
            return null;
        }
    }
}

