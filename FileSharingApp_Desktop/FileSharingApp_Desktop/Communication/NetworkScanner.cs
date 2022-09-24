using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class NetworkScanner
{
    #region Parameters

    public static int PublishPort = 41019;
    public static int BasePort = 42009;
    public static int NumberOfBands = 1;
    public static DeviceTypes DeviceType = DeviceTypes.Core;

    #endregion

    #region Public Variables

    public delegate void ScanCompleteDelegate();
    public static event ScanCompleteDelegate OnScanCompleted;
    public static event ScanCompleteDelegate OnDeviceConnected;

    public class DeviceHandleTypeDef
    {
        public string Hostname;
        public string IP;
        public string Port;
        public DeviceTypes DeviceType;
        public ConnectionStatus Status;

        public DeviceHandleTypeDef DeepClone()
        {
            DeviceHandleTypeDef device = new DeviceHandleTypeDef();
            device.IP = this.IP;
            device.Port= this.Port;
            device.DeviceType = this.DeviceType;
            device.Status= this.Status;
            return device;
        }

    }
    public enum DeviceTypes
    {
        Core = 1,
        Desktop,
        Mobile,
    }
    public enum ConnectionStatus
    {
        NotConnected,
        Connected
    }

    public static List<DeviceHandleTypeDef> PublisherDevices = new List<DeviceHandleTypeDef>();
    public static List<DeviceHandleTypeDef> SubscriberDevices = new List<DeviceHandleTypeDef>();
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

    public static DeviceHandleTypeDef ConnectedDevice;
    public static string MyHostname;

    public static bool IsDevicePublished = false;

    public static string MyIP;
    private static string MyDeviceName;

    public static List<string> DeviceNames
    {
        get
        {
            List<string> deviceNames = new List<string>();
            if (PublisherDevices != null)
            {
                for (int i = 0; i < PublisherDevices.Count; i++)
                {
                    deviceNames.Add(PublisherDevices[i].Hostname);
                }
            }
            return deviceNames;
        }
    }

    #endregion

    #region Private Variables

    private static int[] scanProgressArr;

    private static int ConnectionTimeout;


    private static Server publisherServer;

    private static string IPHeader;

    private static bool _isScanning = false;
    private static int _scanPercentage = 0;

    private static object Lck_IsScanning = new object();
    private static object Lck_ScanPercentage = new object();

    private static int ScanCounter = 0;

    #endregion

    #region Public Functions

    /// <summary>
    /// Initialize Class for given IP.
    /// This Function is used to give static ip 
    /// </summary>
    /// <param name="myIp">Ip address of current device</param>
    public static void Init(string myIp)
    {
        MyIP = myIp;
    }

    /// <summary>
    /// Publishes this device info in the network for others to connect
    /// </summary>
    /// <param name="devicename"></param>
    public static void PublishDevice(string devicename = "DefaultDevice")
    {
        MyDeviceName = devicename;
        GetDeviceAddress(out MyIP, out MyHostname);
        publisherServer = new Server(port: PublishPort, ip: MyIP);
        publisherServer.SetupServer();
        publisherServer.StartListener();
        publisherServer.OnClientConnected += PublisherServer_OnClientConnected;
        Debug.WriteLine("Network Scanner Publisher started!");
        IsDevicePublished = true;
        // SubscriberDevices.Clear();
    }

    /// <summary>
    /// Scans current network to detect all available devices
    /// </summary>
    /// <param name="timeout">time to wait response from other device (ms)</param>
    public static void ScanAvailableDevices(int timeout = 200)
    {
        if (IsScanning)
            return;
        ConnectionTimeout = timeout;
        ScanPercentage = 0;
        GetDeviceAddress(out MyIP, out MyHostname);
        Debug.WriteLine("My IP: " + MyIP);
        PublisherDevices.Clear();
        char[] splitter = new char[] { '.' };
        var ipStack = MyIP.Split(splitter);
        IPHeader = "";
        for (int i = 0; i < 3; i++)
        {
            IPHeader += ipStack[i] + ".";
        }

        IsScanning = true;
        Task.Run(() =>
        {
            int numRobot = NumberOfBands;
            for (int k = 0; k < numRobot; k++)
            {
                int port = k + PublishPort;
                int numTasks = 8;
                int stackSize = 256 / numTasks;
                scanProgressArr = new int[numTasks];
                for (int i = 0; i < numTasks; i++)
                {
                    ParallelScan(stackSize * i, stackSize * (i + 1), i, port);
                }
                Task.Run(() =>
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    while (true)
                    {
                        int percentage = 0;
                        for (int i = 0; i < numTasks; i++)
                        {
                            percentage += scanProgressArr[i] / numRobot;
                        }
                        percentage /= numTasks;
                        //Debug.WriteLine("percentage: " + percentage);
                        ScanPercentage = percentage;
                        if (percentage >= 99 || stopwatch.Elapsed.TotalSeconds > 25)
                            break;
                        Thread.Sleep(50);
                    }
                    stopwatch.Stop();
                    Debug.WriteLine("scan time: " + stopwatch.Elapsed.TotalSeconds + " s");
                    if (OnScanCompleted != null)
                        OnScanCompleted?.Invoke();
                    else
                    {
                        ScanCounter++;
                        if (ScanCounter < 3 && PublisherDevices.Count < 1)
                            ScanAvailableDevices();
                        else
                            ScanCounter = 3;
                    }
                    IsScanning = false;
                });
            }
        });

    }

    /// <summary>
    /// Lists all the ip addresses that the current device have
    /// </summary>
    /// <returns></returns>
    public static IPAddress[] GetAllInternetworkIPs()
    {
        List<IPAddress> addressList = new List<IPAddress>();
        var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
        foreach (var i in interfaces)
            foreach (var ua in i.GetIPProperties().UnicastAddresses)
            {

                if (ua.Address.AddressFamily == AddressFamily.InterNetwork && i.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)// && i.Name.Equals("Wi-Fi"))
                {
                    Console.WriteLine("name: " + i.Name + " ip: " + ua.Address + "  type: ");
                    addressList.Add(ua.Address);
                    //  Debug.WriteLine("name: " + i.Name + " ip: " + ua.Address + "  type: ");
                }
            }
        return addressList.ToArray();
    }

    /// <summary>
    /// Used to detect current device's ip and hostname.
    /// if There is an IP provided by <see cref="Init(string)"/> function, this will return that IP.
    /// </summary>
    /// <param name="deviceIP">detected device IP</param>
    /// <param name="deviceHostname">device hostname</param>
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
        if (string.IsNullOrEmpty(MyIP))
            deviceIP = localAddr.ToString();
        else
            deviceIP = MyIP;
        deviceHostname = host.HostName;
    }

    /// <summary>
    /// Returns device's IP address.
    /// if There is an IP provided by <see cref="Init(string)"/> function, this will return that IP.
    /// </summary>
    /// <returns></returns>
    public static string GetDeviceAddress()
    {
        string deviceIP;
        IPAddress localAddr = null;
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localAddr = ip;
            }
        }
        if (string.IsNullOrEmpty(MyIP))
            deviceIP = localAddr.ToString();
        else
            deviceIP = MyIP;
        Console.WriteLine(" IP: " + deviceIP);
        return deviceIP;
    }

    /// <summary>
    /// Stops Device publisher. this is usually used to stop other devices to find this device when there is already another device connected.
    /// </summary>
    public static void StopPublisher()
    {
        if (publisherServer == null)
            return;
        publisherServer.CloseServer();
        publisherServer = null;
    }

    /// <summary>
    /// Updates connected devices information.
    /// </summary>
    /// <param name="deviceIP"></param>
    public static void UpdateConnectedDeviceInfo(string deviceIP)
    {
        if (SubscriberDevices == null || SubscriberDevices.Count < 1 || string.IsNullOrEmpty(deviceIP))
        {
            ConnectedDevice = new DeviceHandleTypeDef();
            return;
        }
        ConnectedDevice = SubscriberDevices.Find(x => deviceIP.Equals(x.IP));
        ConnectedDevice.Status = ConnectionStatus.Connected;
    }



    #endregion

    /// <summary>
    /// Scans network using parallel threads.
    /// start and end indexes are the last three digits of an ip addrees (yyy here=>. ###.###.###.yyy
    /// </summary>
    /// <param name="startx">start ip index </param>
    /// <param name="endx">end ip index</param>
    /// <param name="progressIndex">index of this thread</param>
    /// <param name="port">port to scan other devices</param>
    private static void ParallelScan(int startx, int endx, int progressIndex, int port)
    {
        Task.Run(() =>
        {
            Stopwatch stp = Stopwatch.StartNew();
            int progress = 0;
            for (int i = startx; i < endx; i++)
            {
                try
                {
                    string targetIP = IPHeader + i.ToString();
                    var device = GetDeviceData(targetIP, port);
                    if (device != null && !device.Hostname.Equals(MyDeviceName))
                        PublisherDevices.Add(device);
                    progress = (int)(((i - startx) / (double)(endx - startx - 1)) * 100.0);
                    scanProgressArr[progressIndex] = progress;
                }
                catch
                {

                }
            }

        });

    }

    /// <summary>
    /// Tries to connect to the device at given address and port.
    /// </summary>
    /// <param name="IP">ip of target device</param>
    /// <param name="port">port of target device</param>
    private static DeviceHandleTypeDef GetDeviceData(string IP, int port)
    {
        //Stopwatch stp = Stopwatch.StartNew();
        var client = new Client(port: port, ip: IP);
        string clientIP = client.ConnectToServer(ConnectionTimeout);
        if (string.IsNullOrEmpty(clientIP))
        {
            /// no ip specified
        }
        else
        {
            client.SendDataServer(Encoding.UTF8.GetBytes("IP:" + MyIP + "&Port:" + PublishPort + "&DeviceName:" + MyHostname + "&Type:" + (byte)DeviceType));
            var data = client.GetData();
            if (data == null)
            {
                Debug.WriteLine("Data was null: " + IP);
                return null;
            }
            client.DisconnectFromServer();
            var device = AnalyzeDeviceData(data);
            if (device != null)
                return device;
        }
        return null;
    }

    /// <summary>
    /// This event is triggered when a client device connected to this device's server
    /// </summary>
    /// <param name="clientIP">ip address of client device</param>
    private static void PublisherServer_OnClientConnected(string clientIP)
    {

        byte[] data = publisherServer.GetData();
        var device = AnalyzeDeviceData(data);
        if (device == null)
            return;
        bool ignoreDevice = false;
        if (ConnectedDevice != null && ConnectedDevice.Status == ConnectionStatus.Connected && ConnectedDevice.DeviceType == DeviceTypes.Desktop && device.DeviceType == DeviceTypes.Desktop)
            ignoreDevice = false;
        if (!ignoreDevice)
        {
            publisherServer.SendDataToClient(Encoding.UTF8.GetBytes("IP:" + MyIP + "&Port:" + BasePort + "&DeviceName:" + MyDeviceName + "&Type:" + (byte)DeviceType));
            if (!SubscriberDevices.Contains(device))
                SubscriberDevices.Add(device);
        }
        else
            publisherServer.SendDataToClient(Encoding.UTF8.GetBytes("I'm Full"));

        StopPublisher();
        OnDeviceConnected?.Invoke();
        PublishDevice(MyDeviceName);
        Debug.WriteLine("Client IP: " + clientIP + " is ignored=" + ignoreDevice);
    }

    /// <summary>
    /// Analyzes the data sent by publisher device and returns the device's structure.
    /// </summary>
    /// <param name="data">received byte array</param>
    /// <returns>device data</returns>
    private static DeviceHandleTypeDef AnalyzeDeviceData(byte[] data)
    {
        if (data == null || data.Length < 10)
            return null;
        string msg = Encoding.UTF8.GetString(data);
        char[] splitter = new char[] { '&' };
        string[] msgParts = msg.Split(splitter);
        DeviceHandleTypeDef device = new DeviceHandleTypeDef();
        device.IP = msgParts[0].Substring(3);
        device.Port = msgParts[1].Substring(5);
        device.Hostname = msgParts[2].Substring(11);
        device.DeviceType = (DeviceTypes)Convert.ToInt32(msgParts[3].Substring(5));
        Debug.WriteLine(" device.IP: " + device.IP + " device.Port: " + device.Port + " device.Hostname: " + device.Hostname + " device.Type: " + device.DeviceType);
        return device;
    }
}

