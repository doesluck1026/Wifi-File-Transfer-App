using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    #region "Definitions"
    private int TimeoutTime = 50;
    public int BufferSize = 1024 * 64;
    private TcpListener server = null;
    public bool ServerStarted = false;
    public int HeaderLen = 7;
    private byte StartByte = (byte)('J');
    public bool _isClientConnected = false;
    private TcpClient client;
    private int ErrorCounter = 0;
    private int Port;
    public string IP = "";
    #endregion

    public Server(int port=41000)
    {
        this.Port = port;
    }
    public bool StartListener()
    {
        try
        {
            Debug.WriteLine("Waiting for new connection...");
            client = server.AcceptTcpClient();
            _isClientConnected = true;
            IPEndPoint endPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            IPAddress ClientIpAddress = endPoint.Address;
            client.ReceiveBufferSize = BufferSize;
            client.SendBufferSize = BufferSize;
            Debug.WriteLine("Connected to: " + client.Client.RemoteEndPoint.ToString());
            ErrorCounter = 0;
            return _isClientConnected;
        }
        catch (Exception e)
        {
            Debug.WriteLine("Client Failed to connect!  " + e.ToString());
            return false;
        }
    }
    public string SetupServer()
    {
        try
        {
            ServerStarted = true;
            IPAddress localAddr = null;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localAddr = ip;
                }
            }
            server = new TcpListener(localAddr, Port);
            Console.WriteLine("IP: " + localAddr);
            this.IP = localAddr.ToString();
            server.Start();
            return localAddr.ToString();
        }
        catch (Exception e)
        {
            Console.WriteLine("Client Failed to connect!  " + e.ToString());
            return null;
        }
    }
    public void CloseServer()
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
    public bool SendDataToClient(byte[] Data)
    {
        bool success = false;
        try
        {
            if (client.Connected)
            {
                NetworkStream stream = client.GetStream();
                int DataLength = Data.Length;
                if (DataLength <= BufferSize)
                {
                    if (stream.CanWrite)
                        stream.Write(Data, 0, DataLength);
                    else
                    {
                        stream.Flush();
                        return false;
                    }
                }
                else
                {
                    int numBytesRead = 0;
                    int bytesLeft = DataLength;
                    int totalBytesSent = 0;
                    int Len = BufferSize;
                    byte[] _data;
                    while (bytesLeft > 0)
                    {
                        _data = new byte[Len];
                        Array.Copy(Data, totalBytesSent, _data, 0, Len);
                        numBytesRead = Len;
                        if (stream.CanWrite)
                            stream.Write(_data, 0, Len);
                        else
                        {
                            stream.Flush();
                            return false;
                        }
                        bytesLeft -= numBytesRead;
                        totalBytesSent += numBytesRead;
                        Len = Math.Min(bytesLeft, BufferSize);
                    }
                }
                stream.Flush();
                success = true;
            }
        }
        catch
        {
            Debug.WriteLine("Unable to Send Message");
            _isClientConnected = false;
            client.Close();
            client.Dispose();
        }

        return success;
    }
    public byte[] GetData()
    {
        try
        {
            if (ErrorCounter > 10)
            {
                Debug.WriteLine("forced to disconnect from client!");
                _isClientConnected = false;
                client.Close();
                client.Dispose();
                ErrorCounter = 0;
                return null;
            }
            var stream = client.GetStream();
            System.Diagnostics.Stopwatch watchdog = new System.Diagnostics.Stopwatch();
            watchdog.Restart();
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
                        numBytesRead = stream.Read(data, 0, HeaderLen);
                        if (numBytesRead == HeaderLen)
                        {
                            DataLength = data[3] | (data[4] << 8) | (data[5] << 16) | (data[6] << 24);
                            _isfirstSampleReceived = true;
                            ms.Write(data, 0, numBytesRead);
                            watchdog.Restart();
                        }
                        else
                        {
                            Debug.WriteLine("Missing Header Bytes!");
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
                            while (true)
                            {
                                numBytesRead = stream.Read(data, 0, Len);
                                watchdog.Restart();
                                ms.Write(data, 0, numBytesRead);
                                totalbytesReceived += numBytesRead;
                                Len = Math.Min(DataLength - totalbytesReceived, BufferSize);
                                if ((totalbytesReceived) >= DataLength)
                                {
                                    break;
                                }
                            }
                        }
                        if ((totalbytesReceived) >= DataLength)
                        {
                            byte[] ReceivedData = new byte[ms.Length];
                            ReceivedData = ms.ToArray();
                            if (ReceivedData[0] == StartByte)
                            {
                                int DataLen = ReceivedData[3] | (ReceivedData[4] << 8) | (ReceivedData[5] << 16) | (ReceivedData[6] << 24);
                                if (DataLen == ReceivedData.Length - HeaderLen)
                                {
                                    stream.Flush();
                                    ErrorCounter = 0;
                                    return ReceivedData;
                                }
                                else
                                {
                                    Debug.WriteLine("Data Length does not match! Told: " + DataLen + " But received: " + (ReceivedData.Length - HeaderLen));
                                    stream.Flush();
                                    return null;
                                }
                            }
                            else
                            {
                                Debug.WriteLine("Start byte was Wrong! received: " + ReceivedData[0]);
                                stream.Flush();
                                return null;
                            }
                        }
                    }
                }
                Debug.WriteLine("Timeout : ");
                ErrorCounter++;
                return null;
            }
        }
        catch
        {
            Debug.WriteLine("Receive Data Failed!");
            _isClientConnected = false;
            client.Close();
            client.Dispose();
            return null;
        }
    }
}

