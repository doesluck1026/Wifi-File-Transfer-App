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

namespace FileSharingApp_Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string FileURL = "";
        public MainWindow()
        {
            InitializeComponent();

            System.Diagnostics.Debug.WriteLine("push yapıyorum");
            Main.event_UpdateUI += UpdateUI;
            Main.Init();
        }

        private void UpdateUI(string IPCode, string HostName, bool TransferVerified, double _transferSpeed, int _completedPercentage)
        {
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

                }
                pbStatus.Value = _completedPercentage;
                txt_TransferSpeed.Text = _transferSpeed.ToString("0.00");
            });
        }

        private void btn_SendFile_Click(object sender, RoutedEventArgs e)
        {
            string FileURL = SelectFile();
            if(FileURL == null)
            {
                //show wrong url message
                return;
            }
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
        private string GetFolder()
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "My Title";
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
            string code = txt_IpCode.Text;
            Main.EnterTheCode(code);
            Main.SetFilePathToSave(FileURL);
        }
    }
}
