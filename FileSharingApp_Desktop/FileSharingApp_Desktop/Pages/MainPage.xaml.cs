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
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private string DeviceIP;
        private string DeviceHostName;
        private bool isScanned = false;
        public MainPage()
        {
            InitializeComponent();
            Main.OnClientRequested += Main_OnClientRequested;
            Main.StartServer();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Parameters.Init();
            NetworkScanner.GetDeviceAddress(out DeviceIP, out DeviceHostName);
            NetworkScanner.PublishDevice();
            Main.FileSaveURL = GetSaveFilePath();
            Debug.WriteLine("Save file path: " + Main.FileSaveURL);
            Dispatcher.Invoke(() =>
            {
                txt_DeviceIP.Content = DeviceIP;
                txt_DeviceName.Text = Parameters.DeviceName;
            });
            if (!isScanned)
            {
                ScanNetwork();
                isScanned = true;
            }
        }
        private void Main_OnClientRequested(string totalTransferSize, string deviceName)
        {
            /// Show file transfer request and ask for permission here
            Dispatcher.Invoke(() =>
            {
                var result = MessageBox.Show(deviceName + " wants to send you files: " + totalTransferSize + " \n Do you want to receive?", "Transfer Request!", button: MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    NavigationService.Navigate(new TransferPage());
                    Main.ResponseToTransferRequest(true);
                }
                else
                    Main.ResponseToTransferRequest(false);
            });
        }
        /// <summary>
        /// The address of the file to be processed is selected
        /// </summary>
        /// <returns>the address of the file in memory</returns>
        private async void SelectFile()
        {
            var pickResult = await FilePicker.PickMultipleAsync();
            if (pickResult != null)
            {
                var results = pickResult.ToArray();
                string[] filepaths = new string[results.Length];
                for (int i = 0; i < filepaths.Length; i++)
                {
                    filepaths[i] = results[i].FullPath;
                }
                Main.SetFilePaths(filepaths);
                await Navigation.PushModalAsync(new SendingPage());
            }
        }
        private void Btn_SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            SelectFile();
        }
        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                for (int i = 0; i < files.Length; i++)
                {
                    Debug.WriteLine("file " + i + " : " + files[i]);
                }
            }
        }
        private void ScanNetwork()
        {
            NetworkScanner.ScanAvailableDevices();
        }
        private string GetSaveFilePath()
        {
           
        }

    }
}
