using System;
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
        public DevicesPage()
        {
            InitializeComponent();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Main.OnTransferResponded += Main_OnTransferResponded;
            ShowDevices();
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Main.OnTransferResponded -= Main_OnTransferResponded;
        }
        private void Main_OnTransferResponded(bool isAccepted)
        {
            Debug.WriteLine("Receiver Response: " + isAccepted);
            if (isAccepted)
            {
                Dispatcher.Invoke(() =>
                {
                    Navigator.Navigate("Pages/TransferPage.xaml");
                });
                Main.BeginSendingFiles();
            }
            else
            {
                var result = MessageBox.Show("Transfer request is rejected by receiver", "Transfer Rejected", button: MessageBoxButton.OK);
            }
        }

        private void list_Devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NetworkScanner.DeviceNames.Count <= 0 || list_Devices.SelectedItem == null)
                return;
            int index = NetworkScanner.DeviceNames.IndexOf(list_Devices.SelectedItem.ToString());
            TargetDeviceIP = NetworkScanner.DeviceIPs[index];
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
            Main.ConnectToTargetDevice(txt_DeviceIP.Text);            
        }
        private void btn_Back_Click(object sender, RoutedEventArgs e)
        {
            Navigator.GoBack();
        }     
        private void ShowDevices()
        {
            if (NetworkScanner.DeviceNames != null)
            {
                Dispatcher.Invoke(() =>
                {
                    list_Devices.ItemsSource = NetworkScanner.DeviceNames.ToArray();
                    if (NetworkScanner.DeviceNames.Count > 0)
                    {
                        list_Devices.SelectedIndex = 0;
                    }
                });
            }
        }
    }
}
