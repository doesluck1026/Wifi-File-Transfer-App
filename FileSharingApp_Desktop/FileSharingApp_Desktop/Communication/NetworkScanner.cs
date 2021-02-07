using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rssdp;

class NetworkScanner
{
    public delegate void ScanCompleteDelegate(string IPandHostName);
    public event ScanCompleteDelegate OnScanCompleted;

    private static string IP;
    private static readonly int PublishPort=42019;
    private static Server publisherServer;
    private static Client client;
    public static void ScanAvailableDevices()
    {
        Thread.Sleep(500);
        string deviceIP, deviceHostname;
        GetDeviceAddress(out deviceIP,out deviceHostname);
        char[] splitter = new char[] { '.' };
        var ipStack = deviceIP.Split(splitter);
        string ipHeader = "";
        for(int i=0;i<3;i++)
        {
            ipHeader += ipStack[i] + ".";
        }
        Debug.WriteLine("ipHeader=" + ipHeader);
        for (int i=20;i<50;i++)
        {
            try
            {
                Debug.WriteLine("Pinging: " + ipHeader + i.ToString());
                GetDeviceData(ipHeader + i.ToString());
            }
            catch
            {

            }
        }
    }
    private static void GetDeviceData(string IP)
    {
        client = new Client(port: PublishPort,ip: IP);
        var t=Task.Run(()=>
        {
            client.ConnectToServer();
           
        });
        bool isConnected = t.Wait(TimeSpan.FromMilliseconds(1000));
        if(!isConnected)
        {

            Debug.WriteLine("Connection Failed on: " + IP);
        }
        else
        {
            var data = client.GetData();
            if (data == null)
            {
                Debug.WriteLine("Data was null: " + IP);
                return;
            }
            Debug.WriteLine(Encoding.ASCII.GetString(data));
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
    }

    private static void PublisherServer_OnClientConnected(string clientIP)
    {
        Debug.WriteLine("Client IP: " + clientIP);
        publisherServer.SendDataToClient(Encoding.ASCII.GetBytes("Selam Arkadaslar"));

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
        IP = localAddr.ToString();
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
