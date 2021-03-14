using Microsoft.WindowsAPICodePack.Dialogs;
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
    /// Interaction logic for OptionsPage.xaml
    /// </summary>
    public partial class OptionsPage : Page
    {
        public OptionsPage()
        {
            InitializeComponent();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Main.OnClientRequested += Main_OnClientRequested;

            Dispatcher.Invoke(() =>
            {
                txt_DeviceName.Text = Parameters.DeviceName;
                txt_OutputFolder.Text = Parameters.SavingPath;
            });
        }
        private void Main_OnClientRequested(string totalTransferSize, string senderDevice)
        {
            /// Show file transfer request and ask for permission here
            Debug.WriteLine(senderDevice + " wants to send you files: " + totalTransferSize + " \n Do you want to receive?");
            var result = MessageBox.Show(senderDevice + " wants to send you files: " + totalTransferSize + " \n Do you want to receive?", "Transfer Request!", button: MessageBoxButton.YesNo);
            Dispatcher.Invoke(() =>
            {
                if (result == MessageBoxResult.Yes)
                {
                    Main.OnClientRequested -= Main_OnClientRequested;
                    Navigator.Navigate("Pages/TransferPage.xaml");
                    Main.ResponseToTransferRequest(true);
                }
                else
                    Main.ResponseToTransferRequest(false);
            });
        }
        private void btn_SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = GetFolder();
            if(string.IsNullOrEmpty(folderPath)==false)
            {
                txt_OutputFolder.Text = folderPath;
            }
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            Parameters.DeviceName = txt_DeviceName.Text;
            if (txt_OutputFolder.Text[txt_OutputFolder.Text.Length - 1] != '\\')
                txt_OutputFolder.Text += "\\";
            Parameters.SavingPath = txt_OutputFolder.Text;
            Debug.WriteLine("Parameters.DeviceName: " + Parameters.DeviceName + " Parameters.SavingPath: " + Parameters.SavingPath);
            Parameters.Save();
        }

        private void btn_MainMenu_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => Navigator.Navigate("Pages/MainPage.xaml"));
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
                var folder = dlg.FileName;
                System.Diagnostics.Debug.WriteLine("Selected Folder: " + folder);
                return folder;
            }
            return null;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Main.OnClientRequested -= Main_OnClientRequested;

        }
    }
}
