using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Client
{
    private  int HeaderLen = 7;
    private  int TimeoutTime = 50;
    private  int BufferSize = 1024 * 64;
    private  TcpClient client;
    public  byte StartByte = (byte)('J');
    public  bool _isClientConnected = false;
    private string IP;
    private int Port;

    public Client(string IP,int port=41000)
    {
        this.IP = IP;
        this.Port = port;
    }

    /// <summary>
    /// Connects to server with specified IP.
    /// </summary>
    /// <param name="IP"></param>
    /// <returns></returns>
    public  bool ConnectToServer()
    {
        bool success;
        try
        {
            client = new TcpClient();           ///  create client object
            client.Connect(IP, Port);           /// Connect
            success = true;
            _isClientConnected = true;
            client.ReceiveBufferSize = BufferSize;
            client.SendBufferSize = BufferSize;
            Debug.WriteLine("Connected to: " + IP + " on Port: " + Port);
        }
        catch
        {
            success = false;
            Debug.WriteLine("Connection failed!");
        }
        return success;
    }
    public  bool DisconnectFromServer()
    {
        bool success = false;
        if (client != null)
        {
            try
            {
                client.Close();
                client.Dispose();           /// remove client object
                _isClientConnected = false;
                client = null;
                Console.WriteLine("Disconnected!");
                success = true;
            }
            catch
            {

                Console.WriteLine("Failed to Disconnect!");
            }
        }
        return success;
    }
    public  bool SendDataServer(byte[] Data)
    {
        bool success = false;
        if (client != null)
        {
            if (client.Connected)
            {
                try
                {
                    var stream = client.GetStream();
                    int DataLength = Data.Length;
                    if (DataLength <= BufferSize)
                    {
                        stream.Write(Data, 0, DataLength);
                    }
                    else
                    {
                        int numBytesRead = 0;
                        int totalbytesSent = 0;
                        int Len = BufferSize;
                        while (true)
                        {
                            byte[] _data = new byte[Len];
                            Array.Copy(Data, totalbytesSent, _data, 0, Len);
                            numBytesRead = Len;
                            stream.Write(_data, 0, Len);
                            totalbytesSent += numBytesRead;
                            Len = Math.Min(DataLength - totalbytesSent, BufferSize);
                            if (totalbytesSent >= DataLength)
                            {
                                break;
                            }
                        }
                    }
                    stream.Flush();
                    success = true;
                }
                catch
                {
                    Debug.WriteLine("Failed to send Data in Client.cs");
                }
            }
        }
        return success;
    }
    /// <summary>
    /// Gets Data from server and retuns byte array as fuction code, in first byte, and data
    /// </summary>
    /// <returns></returns>
    public  byte[] GetData()
    {
        try
        {
            var stream = client.GetStream();
            Stopwatch watchdog = new Stopwatch();
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
                                    return ReceivedData;
                                }
                                else
                                {
                                    Console.WriteLine("Data Length does not match! Told: " + DataLen + " But received: " + (ReceivedData.Length - HeaderLen));
                                    stream.Flush();
                                    return null;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Start byte was Wrong! received: " + ReceivedData[0]);
                                stream.Flush();
                                return null;
                            }
                        }
                    }
                }
                Debug.WriteLine("Timeout : ");
                return null;
            }
        }
        catch
        {
            Console.WriteLine("Receive Data Failed!");
            _isClientConnected = false;
            if (client != null)
            {
                client.Close();
                client.Dispose();
            }
            return null;
        }
    }
    /// <summary>
    /// Gets current device's ip4 address.
    /// </summary>
    /// <returns>ip as string</returns>
    public string GetDeviceIP()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        string localAddr = "";
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localAddr = ip.ToString();
            }
        }
        return localAddr;
    }
}
