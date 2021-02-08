using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.Resources;
using System.Reflection;
using System.Collections;

namespace FileSharingApp_Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread UIUpdate_thread;
        private bool UIUpdate_Start = false;
        private int UIUpdate_Period = 100;      // in ms
        ResourceManager res_man;    // declare Resource manager to access to specific cultureinfo
        CultureInfo cul;            //declare culture info


        private string DeviceIP;
        private string DeviceHostName;
        private bool isScanned = false;
        private string TargetDeviceIP;
        public List<string> AvailableDeviceList = new List<string>();
        public MainWindow()
        {
            InitializeComponent();

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            UI_Init();

            res_man = new ResourceManager("FileSharingApp_Desktop.Resource.resource", Assembly.GetExecutingAssembly());
            cul = CultureInfo.CreateSpecificCulture("tr");        //create culture for english

            NetworkScanner.GetDeviceAddress(out DeviceIP, out DeviceHostName);
            NetworkScanner.PublishDevice();
            Debug.WriteLine("Save file path: " + Main.FileSaveURL);
            Main.OnClientRequested += Main_OnClientRequested;
            Main.StartServer();
            if (!isScanned)
            {
                ScanNetwork();
                isScanned = true;
            }

            UIUpdate_thread = new Thread(UpdateUI);
            UIUpdate_thread.IsBackground = true;
            UIUpdate_Start = true;
            UIUpdate_thread.Start();

            combo_LanguageSelection.SelectedItem = combo_LanguageSelection.Items.GetItemAt(0);
            switch_language();
        }
        private void UpdateUI()
        {
            Stopwatch UpdateWatch = new Stopwatch();

            while (UIUpdate_Start)
            {
                UpdateWatch.Restart();
                Dispatcher.Invoke(() =>
                {
                    list_Clients.ItemsSource = NetworkScanner.DeviceNames.ToArray();
                    lbl_SavePath.Content = Main.FileSaveURL;
                    pbStatus.Value = Main.TransferMetrics.Progress;
                    txt_FileName.Text= Main.TransferMetrics.CurrentFile.FileName;
                    txt_FileSize.Text = Main.TransferMetrics.CurrentFile.FileSize.ToString() + " " + Main.TransferMetrics.CurrentFile.SizeUnit.ToString();
                    txt_PassedTime.Text= ((int)Main.TransferMetrics.TotalElapsedTime / 3600).ToString("00") + ":" + (((int)Main.TransferMetrics.TotalElapsedTime % 3600) / 60).ToString("00") + ":" +
                    (((int)Main.TransferMetrics.TotalElapsedTime % 3600) % 60).ToString("00");
                    txt_EstimatedTime.Text = ((int)Main.TransferMetrics.EstimatedTime / 3600).ToString("00") + ":" + (((int)Main.TransferMetrics.EstimatedTime % 3600) / 60).ToString("00") + ":" +
                    (((int)Main.TransferMetrics.EstimatedTime % 3600) % 60).ToString("00");
                    txt_TransferSpeed.Text = Main.TransferMetrics.TransferSpeed.ToString("0.00") + " MB/s";

                });

                while (UpdateWatch.ElapsedMilliseconds < UIUpdate_Period)
                {
                    Thread.Sleep(1);
                }
            }

        }

        private void btn_SendFile_Click(object sender, RoutedEventArgs e)
        {
            string[] filepaths = SelectFiles();
            Main.SetFilePaths(filepaths);
        }
        private void btn_ReceiveFile_Click(object sender, RoutedEventArgs e)
        {
            
        }
        private void btn_SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            Main.FileSaveURL = GetFolder();
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
        /// <summary>
        /// this function is used to select a folder on current machine and returns folder path
        /// </summary>
        /// <returns>Folder path</returns>
        private string GetFolder()
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Select Target Folder";
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = "c:\\";
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var folder = dlg.FileName+"/";
                System.Diagnostics.Debug.WriteLine("Selected Folder: " + folder);
                return folder;
            }
            return null;
        }        
        private void Main_OnClientRequested(string totalTransferSize)
        {
            /// Show file transfer request and ask for permission here
            Main.ResponseToTransferRequest(true);
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            UIUpdate_Start = false;
            Environment.Exit(0);
            Thread.Sleep(10);
        }
        private void ScanNetwork()
        {
            NetworkScanner.ScanAvailableDevices();
           // NetworkScanner.BeginSearch();
           // await Task.Run(() => PartialScan(2, 100));
        }
        private void PartialScan(int startx, int endx)
        {
            for (int i = startx; i < endx; i++)
            {
                try
                {
                    PingADevice(i);
                    Debug.WriteLine("pinged: " + i);
                }
                catch
                {
                    Debug.WriteLine("failed: " + i);

                }
            }
        }
        private void PingADevice(int ipend)
        {
            //var dummyDevice = new NetworkScanner(ipend);
            //dummyDevice.OnScanCompleted += DummyDevice_OnScanCompleted;
            //dummyDevice.ScanAvailableDevices();
        }
        private void DummyDevice_OnScanCompleted(string IPandHostName)
        {
            if (!AvailableDeviceList.Contains(IPandHostName))
                AvailableDeviceList.Add(IPandHostName);
        }

        private void UI_Init()
        {
            try
            {
                var paramBag = new ParametersBag();
                paramBag.Load(" C:\\Users\\CDS_Software02\\Desktop/Parameters.dat");
                Debug.WriteLine("path: " + paramBag.SavingPath);
                Main.FileSaveURL = paramBag.SavingPath;
                NetworkScanner.DeviceName = paramBag.DeviceName;
            }
            catch
            {
                Debug.WriteLine("Failed to Load parameters");
                var paramBag = new ParametersBag();
                paramBag.SavingPath="C:\\Users\\CDS_Software02\\Desktop\\";
                paramBag.DeviceName = "Yahyanin ki";
                paramBag.Save(" C:\\Users\\CDS_Software02\\Desktop\\Parameters.dat");
            }
        }

        private void switch_language()
        {
            if (res_man != null)
            {
                btn_SendFile.Content = res_man.GetString("sSendFile", cul);
                btn_ReceiveFile.Content = res_man.GetString("sReceiveFile", cul);

                lbl_FilePath.Content = res_man.GetString("sFilePath", cul);
                lbl_FileName.Content = res_man.GetString("sFileName", cul);
                lbl_FileSize.Content = res_man.GetString("sFileSize", cul);
                lbl_HostName.Content = res_man.GetString("sHostName", cul);

                lbl_TransferSpeed.Content = res_man.GetString("sSpeed", cul);
                lbl_PassedTime.Content = res_man.GetString("sTimePassed", cul);
                lbl_EstimatedTime.Content = res_man.GetString("sEstimatedTime", cul);

            }
        }

        private void combo_LanguageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string SelectedLanguage = ((ComboBoxItem)combo_LanguageSelection.SelectedItem).Tag.ToString();

            if (SelectedLanguage.Equals("TR"))
            {
                cul = CultureInfo.CreateSpecificCulture("tr");        //create culture for english
            }
            else if (SelectedLanguage.Equals("EN"))
            {
                cul = CultureInfo.CreateSpecificCulture("en");        //create culture for english
            }
            switch_language();
        }

        private void btn_StartSending_Click(object sender, RoutedEventArgs e)
        {
            bool didDeviceAccept = Main.ConnectToTargetDevice(TargetDeviceIP);
            Debug.WriteLine("Receiver Response: " + didDeviceAccept);
            if (didDeviceAccept)
            {
                Main.BeginSendingFiles();
                /// open the third page here
            }
        }

        private void list_Clients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TargetDeviceIP= NetworkScanner.DeviceIPs[list_Clients.SelectedIndex];
            txt_ServerIP.Text = TargetDeviceIP;//list_Clients.SelectedItem.ToString();
        }
    }
}
