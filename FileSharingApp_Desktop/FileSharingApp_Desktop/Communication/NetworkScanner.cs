using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


class NetworkScanner
{
    public delegate void ScanCompleteDelegate(string IPandHostName);
    public event ScanCompleteDelegate OnScanCompleted;

    private int IPend;
    public NetworkScanner(int ipend)
    {
        IPend = ipend;
    }
    public void ScanAvailableDevices()
    {
        string gate_ip = "192.168.1.1";
        string[] array = gate_ip.Split('.');
        string ping_var = array[0] + "." + array[1] + "." + array[2] + "." + IPend;
        //if(ping_var!= thisIP)
        Ping(ping_var, 1, 100);
        //OnScanCompleted(DeviceList.ToArray());
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
    }

    private void Ping(string host, int attempts, int timeout)
    {
        for (int i = 0; i < attempts; i++)
        {
            new Thread(delegate ()
            {
                try
                {
                    System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
                    ping.PingCompleted += new PingCompletedEventHandler(PingCompleted);
                    ping.SendAsync(host, timeout, host);
                }
                catch
                {
                    // Do nothing and let it try again until the attempts are exausted.
                    // Exceptions are thrown for normal ping failurs like address lookup
                    // failed.  For this reason we are supressing errors.
                }
            }).Start();
        }
    }
    private void PingCompleted(object sender, PingCompletedEventArgs e)
    {
        string ip = (string)e.UserState;
        if (e.Reply != null && e.Reply.Status == IPStatus.Success)
        {
            string hostname = GetHostName(ip);
            Debug.WriteLine("found device: " + ip);
            OnScanCompleted(ip);
        }
        else
        {
            // MessageBox.Show(e.Reply.Status.ToString());
        }
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
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    public string GetMacAddress(string ipAddress)
    {
        string macAddress = string.Empty;
        System.Diagnostics.Process Process = new System.Diagnostics.Process();
        Process.StartInfo.FileName = "arp";
        Process.StartInfo.Arguments = "-a " + ipAddress;
        Process.StartInfo.UseShellExecute = false;
        Process.StartInfo.RedirectStandardOutput = true;
        Process.StartInfo.CreateNoWindow = true;
        Process.Start();
        string strOutput = Process.StandardOutput.ReadToEnd();
        string[] substrings = strOutput.Split('-');
        if (substrings.Length >= 8)
        {
            macAddress = substrings[3].Substring(Math.Max(0, substrings[3].Length - 2))
                     + "-" + substrings[4] + "-" + substrings[5] + "-" + substrings[6]
                     + "-" + substrings[7] + "-"
                     + substrings[8].Substring(0, 2);
            return macAddress;
        }
        else
        {
            return "OWN Machine";
        }
    }
    /// <summary>
    /// Gets current device's hostname.
    /// </summary>
    /// <returns>ip as string</returns>
    public string GetDeviceHostName()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        return host.HostName;
    }
}
