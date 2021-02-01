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
        private string FileURL = "";
        private FileOperations.TransferMode TransferMode;
        private uint prev_timePassed = 0;
        private double _transferSpeed = 0;
        private uint mb = 1024 * 1024;
        private uint ETA = 0;
        private Thread UIUpdate_thread;
        private bool UIUpdate_Start = false;
        private int UIUpdate_Period = 100;      // in ms
        private Brush CompletedStep = Brushes.LimeGreen;
        private Brush CurrentStep = Brushes.Orange;
        private Brush UnCompletedStep = Brushes.LightBlue;
        ResourceManager res_man;    // declare Resource manager to access to specific cultureinfo
        CultureInfo cul;            //declare culture info
        BitmapImage btm_checked = new BitmapImage(new Uri(@"/Icons/checked.png", UriKind.Relative));
        public MainWindow()
        {
            InitializeComponent();

        }
        private void UpdateUI()
        {
            Stopwatch UpdateWatch = new Stopwatch();

            while (UIUpdate_Start)
            {
                UpdateWatch.Restart();
                Dispatcher.Invoke(() =>
                {
                    
                });

                while (UpdateWatch.ElapsedMilliseconds < UIUpdate_Period)
                {
                    Thread.Sleep(1);
                }
            }

        }

        private void btn_SendFile_Click(object sender, RoutedEventArgs e)
        {
           
        }
        private void btn_ReceiveFile_Click(object sender, RoutedEventArgs e)
        {
            
        }
        /// <summary>
        /// The address of the file to be processed is selected
        /// </summary>
        /// <returns>the address of the file in memory</returns>
        private string SelectFile()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
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
        private void btn_Confirm_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           
            UI_Init();

            res_man = new ResourceManager("FileSharingApp_Desktop.Resource.resource", Assembly.GetExecutingAssembly());
            cul = CultureInfo.CreateSpecificCulture("tr");        //create culture for english

            UIUpdate_thread = new Thread(UpdateUI);
            UIUpdate_thread.IsBackground = true;
            UIUpdate_Start = true;
            UIUpdate_thread.Start();

            combo_LanguageSelection.SelectedItem = combo_LanguageSelection.Items.GetItemAt(0);
            switch_language();
        }
        private void CheckUpdate()
        {
            bool isLatestVersion = AutoUpdater.UpdaterMain.CompareVersions();
            if (!isLatestVersion)
            {
               var res= MessageBox.Show("There is a new update! \n Would you like to download the new version?","Version Check",MessageBoxButton.YesNo);
                if(res==MessageBoxResult.Yes)
                {
                    AutoUpdater.UpdaterMain.BeginDownload();
                    MessageBox.Show("Download started!");
                }
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            UIUpdate_Start = false;
            Environment.Exit(0);
            Thread.Sleep(10);
        }

        private void UI_Init()
        {
        }

        private void switch_language()
        {
            if (res_man != null)
            {
                btn_SendFile.Content = res_man.GetString("sSendFile", cul);
                btn_ReceiveFile.Content = res_man.GetString("sReceiveFile", cul);
                btn_Confirm.Content = res_man.GetString("sConfirmation", cul);

                lbl_FilePath.Content = res_man.GetString("sFilePath", cul);
                lbl_FileName.Content = res_man.GetString("sFileName", cul);
                lbl_FileSize.Content = res_man.GetString("sFileSize", cul);
                lbl_HostName.Content = res_man.GetString("sHostName", cul);

                lbl_TransferSpeed.Content = res_man.GetString("sSpeed", cul);
                lbl_PassedTime.Content = res_man.GetString("sTimePassed", cul);
                lbl_EstimatedTime.Content = res_man.GetString("sEstimatedTime", cul);
                lbl_code.Content = res_man.GetString("sCode", cul);

                txt_IpCode.ToolTip = res_man.GetString("sstCode", cul);

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
        /// <summary>
        /// This function is used to prevent the user to type more than 6 characters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txt_IpCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }
    }
}
