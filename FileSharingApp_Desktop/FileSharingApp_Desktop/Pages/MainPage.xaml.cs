using Microsoft.Win32;
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
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Main.OnClientRequested += Main_OnClientRequested;
            Parameters.Init();
            NetworkScanner.GetDeviceAddress(out DeviceIP, out DeviceHostName);
            Main.FileSaveURL = Parameters.SavingPath;
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
            Debug.WriteLine(deviceName + " wants to send you files: " + totalTransferSize + " \n Do you want to receive?");
            var result = MessageBox.Show(deviceName + " wants to send you files: " + totalTransferSize + " \n Do you want to receive?", "Transfer Request!", button: MessageBoxButton.YesNo);
            Dispatcher.Invoke(() =>
            {
                if (result == MessageBoxResult.Yes)
                {
                    Navigator.Navigate("Pages/TransferPage.xaml");
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
        private string[] SelectFiles()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            openFileDialog1.Filter = " All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == true)
            {
                string[] selectedFileName = openFileDialog1.FileNames;
                return selectedFileName;
            }
            return null;
        }
        private void Btn_SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            string[] filePaths= SelectFiles();
            if (filePaths == null)
                return;
            Main.SetFilePaths(filePaths);
            Navigator.Navigate("Pages/DevicesPage.xaml");
        }
        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);

                for (int i = 0; i < filePaths.Length; i++)
                {
                    Debug.WriteLine("file " + i + " : " + filePaths[i]);
                }
                if (filePaths == null)
                    return;
                Main.SetFilePaths(filePaths);
                Navigator.Navigate("Pages/DevicesPage.xaml");
            }
        }
        private void ScanNetwork()
        {
            NetworkScanner.ScanAvailableDevices();
        }
        private void txt_DeviceName_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            txt_DeviceName.IsReadOnly = false;
        }
        private void txt_DeviceName_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.Enter)
            {
                Parameters.DeviceName = txt_DeviceName.Text;
                Parameters.Save();
            }
        }

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            Navigator.Navigate("Pages/OptionsPage.xaml");
        }
    }
}
