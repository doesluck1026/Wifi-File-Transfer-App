using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class NetworkScanner
{
    public delegate void ScanCompleteDelegate();
    public static event ScanCompleteDelegate OnScanCompleted;
    public static List<string> DeviceNames = new List<string>();
    public static List<string> DeviceIPs = new List<string>();
    private static int ConnectionTimeout;
    public static bool IsScanning
    {
        get
        {
            lock (Lck_IsScanning)
                return _isScanning;
        }
        private set
        {
            lock (Lck_IsScanning)
                _isScanning = value;
        }
    }
    public static int ScanPercentage
    {
        get
        {
            lock (Lck_ScanPercentage)
                return _scanPercentage;
        }
        private set
        {
            lock (Lck_ScanPercentage)
                _scanPercentage = value;
        }
    }

    private static int[] scanProgressArr;
    private static string DeviceIP;
    private static readonly int PublishPort = 41019;
    private static Server publisherServer;

    private static string IPHeader;
    public static bool IsDevicePublished = false;

    private static bool _isScanning = false;
    private static int _scanPercentage = 0;

    private static object Lck_IsScanning = new object();
    private static object Lck_ScanPercentage = new object();

    private static int ScanCounter = 0;
    public static void ScanAvailableDevices(int timeout = 200)
    {
        if (IsScanning)
            return;
        ConnectionTimeout = timeout;
        ScanPercentage = 0;
        string deviceIP, deviceHostname;
        GetDeviceAddress(out deviceIP, out deviceHostname);
        DeviceIP = deviceIP;
        DeviceNames.Clear();
        DeviceIPs.Clear();
        char[] splitter = new char[] { '.' };
        var ipStack = deviceIP.Split(splitter);
        IPHeader = "";
        for (int i = 0; i < 3; i++)
        {
            IPHeader += ipStack[i] + ".";
        }

        IsScanning = true;
        Task.Run(() =>
        {
            int numTasks = 16;
            int stackSize = 256 / numTasks;
            scanProgressArr = new int[numTasks];
            for (int i = 0; i < numTasks; i++)
            {
                ParallelScan(stackSize * i, stackSize * (i + 1), i);
                // Debug.WriteLine("i: "+ i+"  stx:"+ (stackSize * i + 1)+" endx: "+(stackSize * (i + 1) + 1));
            }
            Task.Run(() =>
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (true)
                {
                    int percentage = 0;
                    for (int i = 0; i < numTasks; i++)
                    {
                        percentage += scanProgressArr[i];
                    }
                    percentage /= numTasks;
                    //Debug.WriteLine("percentage: " + percentage);
                    ScanPercentage = percentage;
                    if (percentage >= 99 || stopwatch.Elapsed.TotalSeconds>12)
                        break;
                    Thread.Sleep(50);
                }
                stopwatch.Stop();
                Debug.WriteLine("scan time: " + stopwatch.Elapsed.TotalSeconds + " s");
                if (OnScanCompleted != null)
                    OnScanCompleted();
                else
                {
                    ScanCounter++;
                    if (ScanCounter < 3 && DeviceIPs.Count < 1)
                        ScanAvailableDevices();
                    else
                        ScanCounter = 3;
                }
                IsScanning = false;
            });
        });

    }

    private static void ParallelScan(int startx, int endx, int progressIndex)
    {
        Task.Run(() =>
        {
            Stopwatch stp = Stopwatch.StartNew();
            int progress = 0;
            for (int i = startx; i < endx; i++)
            {
                try
                {
                    //Debug.WriteLine("Pinging: " + holder.IpHeader + i.ToString());
                    string targetIP = IPHeader + i.ToString();
                    if (targetIP == DeviceIP)
                        continue;
                    GetDeviceData(targetIP);
                    progress = (int)(((i - startx) / (double)(endx - startx - 1)) * 100.0);
                    scanProgressArr[progressIndex] = progress;
                    // Debug.WriteLine("index: "+progressIndex+" progress: "+ progress);
                }
                catch
                {

                }
            }

        });
    }
    private static void GetDeviceData(string IP)
    {
        //Stopwatch stp = Stopwatch.StartNew();
        var client = new Client(port: PublishPort, ip: IP);
        string clientIP = client.ConnectToServer(ConnectionTimeout);
        if (string.IsNullOrEmpty(clientIP))
        {
            //Debug.WriteLine("Connection Failed on: " + IP);
        }
        else
        {
            var data = client.GetData();
            if (data == null)
            {
                Debug.WriteLine("Data was null: " + IP);
                return;
            }
            string msg = Encoding.ASCII.GetString(data);
            char[] splitter = new char[] { '&' };
            string[] msgParts = msg.Split(splitter);
            string ip = msgParts[0].Substring(3);
            string deviceName = msgParts[1].Substring(11);
            DeviceNames.Add(deviceName);
            DeviceIPs.Add(ip);
            Debug.WriteLine("data: " + msg);
            client.SendDataServer(Encoding.ASCII.GetBytes("Gotcha"));
            client.DisconnectFromServer();
        }
    }
    public static void PublishDevice()
    {
        publisherServer = new Server(port: PublishPort);
        publisherServer.SetupServer();
        publisherServer.StartListener();
        publisherServer.OnClientConnected += PublisherServer_OnClientConnected;
        Debug.WriteLine("Publisher started!");
        IsDevicePublished = true;
    }

    private static void PublisherServer_OnClientConnected(string clientIP)
    {
        Debug.WriteLine("Client IP: " + clientIP);
        publisherServer.SendDataToClient(Encoding.ASCII.GetBytes("IP:" + DeviceIP + "&DeviceName:" + Parameters.DeviceName));

        publisherServer.GetData();

        publisherServer.CloseServer();
        PublishDevice();
    }

    public static void GetDeviceAddress(out string deviceIP, out string deviceHostname)
    {
        IPAddress localAddr = null;
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localAddr = ip;
            }
        }
        deviceIP = localAddr.ToString();
        deviceHostname = host.HostName;
        DeviceIP = localAddr.ToString();
    }
    public string GetHostName(string ipAddress)
    {
        try
        {
            IPHostEntry entry = Dns.GetHostEntry(ipAddress);
            if (entry != null)
            {
                return entry.HostName;
            }
        }
        catch (SocketException)
        {
            // MessageBox.Show(e.Message.ToString());
        }

        return null;
    }
    public string GetDeviceHostName()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        return host.HostName;
    }
}
