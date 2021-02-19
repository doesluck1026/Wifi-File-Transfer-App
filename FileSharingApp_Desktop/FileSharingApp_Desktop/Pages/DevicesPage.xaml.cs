using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileSharingApp_Desktop.Pages
{
    /// <summary>
    /// Interaction logic for DevicesPage.xaml
    /// </summary>
    public partial class DevicesPage : Page
    {
        private string TargetDeviceIP;
        public DevicesPage()
        {
            InitializeComponent();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (NetworkScanner.DeviceNames != null)
            {
                list_Devices.ItemsSource = NetworkScanner.DeviceNames.ToArray();
            }
        }
        private void list_Devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TargetDeviceIP = NetworkScanner.DeviceIPs[NetworkScanner.DeviceNames.IndexOf(list_Devices.SelectedItem.ToString())];
            txt_DeviceIP.Text = TargetDeviceIP;//list_Clients.SelectedItem.ToString();
        }

        private void btn_Scan_Click(object sender, RoutedEventArgs e)
        {
            NetworkScanner.DeviceNames = new List<string>();
            NetworkScanner.ScanAvailableDevices();
            NetworkScanner.OnScanCompleted += NetworkScanner_OnScanCompleted;
        }

        private void NetworkScanner_OnScanCompleted()
        {
            if (NetworkScanner.DeviceNames != null)
            {
                Dispatcher.Invoke(() =>
                {
                    list_Devices.ItemsSource = NetworkScanner.DeviceNames.ToArray();
                });
            }
        }

        private void btn_Send_Click(object sender, RoutedEventArgs e)
        {
            bool didDeviceAccept = Main.ConnectToTargetDevice(txt_DeviceIP.Text);
            Debug.WriteLine("Receiver Response: " + didDeviceAccept);
            if (didDeviceAccept)
            {
                Main.BeginSendingFiles();
                NavigationService.Navigate(new TransferPage());
            }
        }
    }
}
