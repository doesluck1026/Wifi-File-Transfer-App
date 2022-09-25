using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FileSharingApp_Desktop.Pages
{
    /// <summary>
    /// Interaction logic for DevicesPage.xaml
    /// </summary>
    public partial class DevicesPage : Page
    {
        private string TargetDeviceIP;
        private bool isScanning = false;
        private bool isRequestSent = false;
        public DevicesPage()
        {
            InitializeComponent();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TransferEngine.OnTransferResponded += Main_OnTransferResponded;
            ShowDevices();
            bool isAnyDeviceAvailable = false;
            if (NetworkScanner.PublisherDevices ==null && NetworkScanner.PublisherDevices.Count>0)
                    isAnyDeviceAvailable = true;
            if (!isAnyDeviceAvailable)
                btn_Scan_Click(null, null);
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            TransferEngine.OnTransferResponded -= Main_OnTransferResponded;
        }
        private void Main_OnTransferResponded(bool isAccepted)
        {
            Debug.WriteLine("Receiver Response: " + isAccepted);
            isRequestSent = false;
            if (isAccepted)
            {
                Dispatcher.Invoke(() =>
                {
                    Navigator.Navigate("Pages/TransferPage.xaml");
                });
                TransferEngine.BeginSendingFiles();
            }
            else
            {
                var result = MessageBox.Show(Properties.Resources.Send_Warning_rejected, Properties.Resources.Rejected_Title, button: MessageBoxButton.OK);
            }
        }

        private void list_Devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NetworkScanner.PublisherDevices.Count <= 0 || list_Devices.SelectedItem == null)
                return;
            int index = NetworkScanner.PublisherDevices.FindIndex(x=> x.Hostname.Equals(list_Devices.SelectedItem.ToString()));
            TargetDeviceIP = NetworkScanner.PublisherDevices[index].IP;
            txt_DeviceIP.Text = TargetDeviceIP;//list_Clients.SelectedItem.ToString();
        }

        private void btn_Scan_Click(object sender, RoutedEventArgs e)
        {
            NetworkScanner.OnScanCompleted += NetworkScanner_OnScanCompleted;
            Dispatcher.Invoke(() =>
            {
                NetworkScanner.ScanAvailableDevices();
            });
            Task.Run(() =>
            {
                while (NetworkScanner.IsScanning)
                {
                    ShowDevices();
                    Thread.Sleep(100);
                }
            });
        }

        private void NetworkScanner_OnScanCompleted()
        {
            isScanning = false;
            ShowDevices();
        }

        private void btn_Send_Click(object sender, RoutedEventArgs e)
        {
            if (!isRequestSent)
            {
                if(list_Devices.SelectedItems.Count > 1)
                {
                    TransferEngine.MultipleSendMode = true;
                    List<string> deviceIps = new List<string>();
                    for( int i = 0; i < list_Devices.SelectedItems.Count; i++)
                    {
                        int index = NetworkScanner.PublisherDevices.FindIndex(x=>x.Hostname.Equals(list_Devices.SelectedItems[i].ToString()));
                        string targetDeviceIP = NetworkScanner.PublisherDevices[index].IP;
                        deviceIps.Add(targetDeviceIP);
                    }
                    TransferEngine.SendToMultipleDevices(deviceIps);
                }
                else
                {
                    TransferEngine.SendFileTo(txt_DeviceIP.Text);
                    TransferEngine.MultipleSendMode = false;

                }
                isRequestSent = true;
            }
        }
        private void btn_Back_Click(object sender, RoutedEventArgs e)
        {
            if(!isRequestSent)
                Navigator.GoBack();
        }
        private void ShowDevices()
        {
            Dispatcher.Invoke(() =>
            {
                list_Devices.Items.Clear();
                if (NetworkScanner.PublisherDevices != null)
                {
                    for (int i = 0; i < NetworkScanner.PublisherDevices.Count; i++)
                    {
                        list_Devices.Items.Add(NetworkScanner.PublisherDevices[i].Hostname);
                    }
                    if (NetworkScanner.PublisherDevices.Count > 0)
                        list_Devices.SelectedIndex = 0;
                }
            });
        }
    }
}
