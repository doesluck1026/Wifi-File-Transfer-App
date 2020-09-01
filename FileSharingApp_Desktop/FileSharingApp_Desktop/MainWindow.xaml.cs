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

namespace FileSharingApp_Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string FileURL = "";
        private FileOperations.TransferMode TransferMode;
        private uint prev_timePassed=0;
        private double _transferSpeed = 0;
        private uint mb = 1024*1024;
        private uint ETA = 0;
        public MainWindow()
        {
            InitializeComponent();

            Main.event_UpdateUI += UpdateUI;
            Main.Init(true);
        }
        private void UpdateUI(string IPCode, string HostName, bool TransferVerified, long numBytes=1,uint numPack=0,uint TimePassed=0)
        {
            if (_transferSpeed > 500 || _transferSpeed < 0)
                _transferSpeed = 0;
            Dispatcher.Invoke(() =>
            {
                if (!IPCode.Equals(""))
                {
                    txt_IpCode.Text = IPCode;
                }
                if(!HostName.Equals(""))
                {
                    lbl_Hostname.Content = HostName;
                }
                if(TransferVerified)
                {
                    lbl_SecondStep.Background = Brushes.Green; 
                }
                else
                {
                    lbl_SecondStep.Background = Brushes.AliceBlue;
                }
                uint NumberOfPacks = Communication.NumberOfPacks;
                if (NumberOfPacks != 0)
                {
                    double _completedPercentage = (((double)numPack / NumberOfPacks) * 100);
                    pbStatus.Value = _completedPercentage;
                }
                double deltaTime = (TimePassed - prev_timePassed)/1000.0;
                prev_timePassed = TimePassed;
                TimePassed /= 1000;
                _transferSpeed = _transferSpeed*0.5+0.5*(((double)numBytes / mb) /deltaTime);
                ETA = (uint)((((NumberOfPacks - numPack)*Main.PackSize / mb) / _transferSpeed));
                txt_TransferSpeed.Text = _transferSpeed.ToString("0.00")+" MB/s       " +" Estimated Time: "+(ETA/60)+" min "+ETA%60+" sec        TimePassed: "+(TimePassed / 60) + " min " + TimePassed % 60 + " sec";
            });
        }
        private void btn_SendFile_Click(object sender, RoutedEventArgs e)
        {
            Main.TransferApproved = false;
            string FileURL = SelectFile();
            if (FileURL == null)
            {
                MessageBox.Show("Please Select a Valid File");
                return;
            }
            TransferMode = FileOperations.TransferMode.Send;
            Main.SetFileURL(FileURL);
            System.Diagnostics.Debug.WriteLine(" FileURL = " + FileURL);
        }
        private void btn_ReceiveFile_Click(object sender, RoutedEventArgs e)
        {
            FileURL = GetFolder();
            if (FileURL == null)
            {
                System.Diagnostics.Debug.WriteLine("File Url is null");
                return;
            }
            TransferMode = FileOperations.TransferMode.Receive;
            System.Diagnostics.Debug.WriteLine(" FileURL = " + FileURL);
        }

        /// <summary>
        /// The address of the file to be processed is selected
        /// </summary>
        /// <returns>the address of the file in memory</returns>
        private string SelectFile()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = " All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = true;
            
            if (openFileDialog1.ShowDialog() == true)
            {
                string selectedFileName = openFileDialog1.FileName;
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
            dlg.InitialDirectory = "c:\\";

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
                var folder = dlg.FileName;
                System.Diagnostics.Debug.WriteLine("Selected Folder: " + folder);
                return folder;
            }
            return null;
        }
        private void btn_Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (TransferMode == FileOperations.TransferMode.Receive)
            {
                Main.Init(false);
                string code = txt_IpCode.Text;
                Main.EnterTheCode(code);
                Main.SetFilePathToSave(FileURL);
            }
            else if (TransferMode == FileOperations.TransferMode.Send)
            {
                Main.TransferApproved = true;
            }
        }
    }
}
