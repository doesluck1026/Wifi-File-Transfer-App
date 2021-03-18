using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

class Client
{
    private int BufferSize;
    private TcpClient client;
    public byte StartByte;
    public bool IsConnectedToServer = false;
    private string IP;
    private int Port;

    public Client(int port = 38000, string ip = "", int bufferSize = 1024 * 64, byte StartByte = (byte)'A')
    {
        this.Port = port;
        this.IP = ip;
        this.BufferSize = bufferSize;
        this.StartByte = StartByte;
    }

    /// <summary>
    /// Connects to server with specified IP.
    /// </summary>
    /// <returns>Hostname of server</returns>
    public string ConnectToServer(int timeout = 0)
    {
        try
        {
            client = new TcpClient();
            client.NoDelay = true;
            Stopwatch stp = Stopwatch.StartNew();
            if (timeout > 0)
            {
                var result = client.BeginConnect(IP, Port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout));
                if (!success)
                {
                    //client.EndConnect(result);
                    client.Close();
                    throw new Exception("Failed to connect.");
                }
                else
                    client.EndConnect(result);
            }
            else
                client.Connect(IP, Port);
            stp.Restart();
            IsConnectedToServer = true;
            client.SendBufferSize = BufferSize;
            client.ReceiveBufferSize = BufferSize;
            Debug.WriteLine("Succesfully Connected to: " + IP + " on Port: " + Port+" time: "+ stp.Elapsed.TotalMilliseconds+" ms");
            return IP;
        }

        catch (Exception e)
        {
            //Debug.WriteLine("Connection Failed: " + e.ToString());
            return "";
        }
    }
    public bool DisconnectFromServer()
    {
        bool success = false;
        if (client != null)
        {
            try
            {
                client.Close();
                client.Dispose();           /// remove client object
                IsConnectedToServer = false;
                client = null;
                Debug.WriteLine("Disconnected!");
                success = true;
            }
            catch
            {

                Debug.WriteLine("Failed to Disconnect!");
            }
        }
        return success;
    }
    public bool SendDataServer(byte[] data)
    {
        bool success = false;
        byte[] headerBytes = PrepareDataHeader(data.Length);
        int DataLength = headerBytes.Length + data.Length;
        byte[] dataToSend = new byte[DataLength];
        headerBytes.CopyTo(dataToSend, 0);
        data.CopyTo(dataToSend, headerBytes.Length);
        try
        {
            if (client == null)
                return false;
            if (client.Connected)
            {
                NetworkStream stream = client.GetStream();
                if (DataLength < BufferSize)
                {
                    stream.Write(dataToSend, 0, DataLength);
                    success = true;
                }
                else
                {
                    int NumBytesLeft = DataLength;
                    int TotalBytesSent = 0;
                    byte[] tempData;
                    int len = BufferSize;
                    while (NumBytesLeft > 0)
                    {
                        tempData = new byte[len];
                        Array.Copy(dataToSend, TotalBytesSent, tempData, 0, len);
                        stream.Write(tempData, 0, len);
                        NumBytesLeft -= len;
                        TotalBytesSent += len;
                        len = Math.Min(NumBytesLeft, BufferSize);
                    }
                    success = true;
                }
            }
            else
                success = false;
            return success;
        }
        catch (Exception e)
        {
            Debug.WriteLine("Unable to send message to client!" + e.ToString());
            IsConnectedToServer = false;
            if (client == null)
                return false;
            client.Close();
            client = null;
            return false;
        }
    }
    /// <summary>
    /// Gets Data from server and retuns byte array as fuction code, in first byte, and data
    /// </summary>
    /// <returns></returns>
    public byte[] GetData()
    {
        try
        {
            if (client == null)
                return null;
            NetworkStream stream = client.GetStream();
            byte[] tempData = new byte[BufferSize];
            byte[] dataHeader = new byte[5];
            using (MemoryStream ms = new MemoryStream())
            {
                int numBytesRead = 0;
                int TotalBytesReceived = 0;
                bool isFirstsSampleReceived = false;
                int DataLength = 0;
                while (true)
                {
                    if (!isFirstsSampleReceived)
                    {
                        numBytesRead = stream.Read(dataHeader, 0, dataHeader.Length);
                        if (numBytesRead == dataHeader.Length)
                        {
                            if (dataHeader[0] != StartByte)
                                break;
                            DataLength = BitConverter.ToInt32(dataHeader, 1);
                            isFirstsSampleReceived = true;
                        }
                        else
                            break;
                    }
                    else
                    {
                        if (DataLength < BufferSize)
                        {
                            numBytesRead = stream.Read(tempData, 0, DataLength);
                            TotalBytesReceived += numBytesRead;
                            ms.Write(tempData, 0, numBytesRead);

                        }
                        else
                        {
                            int len = BufferSize;
                            while (TotalBytesReceived < DataLength)
                            {
                                numBytesRead = stream.Read(tempData, 0, len);
                                TotalBytesReceived += numBytesRead;
                                ms.Write(tempData, 0, numBytesRead);
                                len = Math.Min(DataLength - TotalBytesReceived, BufferSize);
                            }
                        }
                        if (TotalBytesReceived >= DataLength)
                            break;
                    }
                }
                if (TotalBytesReceived == DataLength)
                {
                    byte[] receivedData = ms.ToArray();
                    return receivedData;
                }
                else
                {
                    Debug.WriteLine("number of received bytes are incorrect");
                    return null;
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine("Failed to Receive Data From Client! :" + e.ToString());
            IsConnectedToServer = false;
            if (client == null)
                return null;
            client.Close();
            client = null;
            return null;
        }
    }
    private byte[] PrepareDataHeader(int len)
    {
        byte[] header = new byte[5];
        header[0] = StartByte;
        byte[] lengthBytes = BitConverter.GetBytes(len);
        lengthBytes.CopyTo(header, 1);
        return header;
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
        Debug.WriteLine("Server IP: " + localAddr);
        return localAddr;
    }
}
