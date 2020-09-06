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
            Main.event_UpdateUI_FileInfo += event_UpdateUI_FileInfo;
            Main.event_UpdateUI_HostInfo += event_UpdateUI_HostInfo;
            Main.event_UpdateUI_ClientInfo += event_UpdateUI_ClientInfo;
            Main.event_UpdateUI_TransferInfo += event_UpdateUI_TransferInfo;
        }

        private void event_UpdateUI_FileInfo(string FilePath, string FileName, double FileSize)
        {
            Dispatcher.Invoke(() =>
            {
                if (!FilePath.Equals(""))
                {
                    txt_FilePath.Text= FilePath;
                }
                if (!FileName.Equals(""))
                {
                    txt_FileName.Text = FileName;
                }
                if (!FileSize.Equals(""))
                {
                    txt_FileSize.Text = FileSize.ToString();
                }
            });
        }

        private void event_UpdateUI_HostInfo(string HostIpCode, string HostName, bool TransferVerified, string ConnectionMsg)
        {
            Dispatcher.Invoke(() =>
            {
                if (!HostIpCode.Equals(""))
                {
                    txt_IpCode.Text = HostIpCode;
                }
                if (!HostName.Equals(""))
                {
                    txt_HostName.Text = HostName;
                }
                if (!ConnectionMsg.Equals(""))
                {
                    txt_StatusInfo.Text = ConnectionMsg;
                }
            });
        }

        private void event_UpdateUI_TransferInfo(double _transferSpeed, int _completedPercentage)
        {
            Dispatcher.Invoke(() =>
            {
                pbStatus.Value = _completedPercentage;
                txt_TransferSpeed.Text = _transferSpeed.ToString("0.00");
            });
        }

        private void event_UpdateUI_ClientInfo(bool isConnected,string FileName, double FileSize)
        {
            Dispatcher.Invoke(() =>
            {
                if (isConnected)
                {
                    string QuestionMsg = "Do you approve to receive the " + FileName + ", " + FileSize + " ?";
                    MessageBoxResult result = MessageBox.Show(QuestionMsg, "My App", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            txt_FileName.Text = FileName;
                            txt_FileName.Text = FileName;

                            Main.RespondToTransferRequest(true);
                            break;
                        case MessageBoxResult.No:
                            Main.RespondToTransferRequest(false);
                            break;
                    }
                }
                else
                {
                    string QuestionMsg = "Connection failed";
                    MessageBox.Show(QuestionMsg);

                }

            });
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bdr_Step2.IsEnabled = false;
            btn_Confirm.IsEnabled = false;
            txt_StatusInfo.Text = "Choose what kind of action you will take.";
            Main.Init();
        }

        private void btn_SendFile_Click(object sender, RoutedEventArgs e)
        {
            string FileURL = SelectFile();
            if(FileURL == null)
            {
                //show wrong url message
                MessageBox.Show("File is invalid");
                return;
            }
            Main.SetFileURL(FileURL);
            bdr_Step2.IsEnabled = true;

            // Update file info
            System.Diagnostics.Debug.WriteLine(" FileURL = " + FileURL);

        }

        private void btn_ReceiveFile_Click(object sender, RoutedEventArgs e)
        {
            FileURL = GetFolder();
            if (FileURL == null)
            {
                MessageBox.Show("File is invalid");
                System.Diagnostics.Debug.WriteLine("File Url is null");
                return;
            }
            txt_FilePath.Text = FileURL;
            txt_StatusInfo.Text = "Now, enter the code, from the sender. ";
            btn_Confirm.IsEnabled = true;
            bdr_Step2.IsEnabled = true;
            System.Diagnostics.Debug.WriteLine(" FileURL = " + FileURL);
        }

        private void btn_Confirm_Click(object sender, RoutedEventArgs e)
        {
            string code = txt_IpCode.Text;
            Main.EnterTheCode(code);
            Main.SetFilePathToSave(FileURL);
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

    }
}
