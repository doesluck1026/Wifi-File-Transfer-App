using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
namespace FileSharingApp_Desktop.Pages
{
    /// <summary>
    /// Interaction logic for OptionsPage.xaml
    /// </summary>
    public partial class OptionsPage : Page
    {
        Dictionary<string, string> LanguageList = new Dictionary<string, string>();
        public OptionsPage()
        {
            InitializeComponent();
            LanguageList.Add("English", "en");
            LanguageList.Add("Türkçe", "tr");
            LanguageList.Add("Deutsche", "de");
            LanguageList.Add("Française", "fr");
            LanguageList.Add("Española", "es");
            LanguageList.Add("中国人", "zh-Hans");
            LanguageList.Add("عربي", "ar");

        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TransferEngine.OnClientRequested += Main_OnClientRequested;
            Dispatcher.Invoke(() =>
            {
                var languageCodeList = LanguageList.Values.ToList();
                Combo_Languages.ItemsSource = LanguageList.Keys.ToArray();
                Combo_Languages.SelectedIndex = languageCodeList.IndexOf(Parameters.DeviceLanguage);
                txt_DeviceName.Text = Parameters.DeviceName;
                txt_OutputFolder.Text = Parameters.SavingPath;
                Chc_AllowRequests.IsChecked = Parameters.AcceptAllRequests;
                bool isStaticIpEnabled= !string.IsNullOrEmpty(Parameters.DeviceIP);
                Chc_EnableStaticIP.IsChecked = isStaticIpEnabled;
                txt_DeviceIP.IsEnabled = isStaticIpEnabled;
                if (isStaticIpEnabled)
                    txt_DeviceIP.Text = Parameters.DeviceIP;
            });
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            TransferEngine.OnClientRequested -= Main_OnClientRequested;
        }
        private void Main_OnClientRequested(string totalTransferSize, string deviceName, bool isAlreadyAccepted)
        {
            /// Show file transfer request and ask for permission here
            Debug.WriteLine(deviceName + " wants to send you files: " + totalTransferSize + " \n Do you want to receive?");

            if (!isAlreadyAccepted)
            {
                Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show(deviceName + " " + Properties.Resources.Permission_RequestMessage + " " + totalTransferSize + " \n " + Properties.Resources.Permission_RequestMessage,
                       Properties.Resources.Permission_InfoMessage, button: MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        Navigator.Navigate("Pages/TransferPage.xaml");
                        TransferEngine.ResponseToTransferRequest(true);
                    }
                    else
                        TransferEngine.ResponseToTransferRequest(false);
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    Navigator.Navigate("Pages/TransferPage.xaml");
                });
            }
        }
        private void btn_SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = GetFolder();
            if (string.IsNullOrEmpty(folderPath) == false)
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
            string selectedLanguage = Combo_Languages.SelectedItem.ToString();
            string languageCode;
            LanguageList.TryGetValue(selectedLanguage, out languageCode);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(languageCode);
            Properties.Resources.Culture = new CultureInfo(languageCode);
            Parameters.DeviceLanguage = languageCode;
            if (Chc_EnableStaticIP.IsChecked == true)
            {
                Parameters.DeviceIP = txt_DeviceIP.Text;
                NetworkScanner.MyIP = Parameters.DeviceIP;
            }
            else
            {
                Parameters.DeviceIP = null;
                NetworkScanner.MyIP = null;
                string ip, name;
                NetworkScanner.GetDeviceAddress(out ip, out name);
            }
            Parameters.Save();
            Dispatcher.Invoke(() => Navigator.Navigate("Pages/MainPage.xaml"));
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
            dlg.Title = Properties.Resources.Settings_SelectTargetFolder;
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

        private void Chc_AllowRequests_Click(object sender, RoutedEventArgs e)
        {
            Parameters.AcceptAllRequests = (bool)Chc_AllowRequests.IsChecked;
        }

        private void Chc_EnableStaticIP_Click(object sender, RoutedEventArgs e)
        {
            txt_DeviceIP.IsEnabled = (bool)Chc_EnableStaticIP.IsChecked;

        }
    }
}