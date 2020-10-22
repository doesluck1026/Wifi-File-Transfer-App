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
        private Brush CurrentStep = Brushes.LightSkyBlue;
        private Brush UnCompletedStep = Brushes.LightBlue;
        ResourceManager res_man;    // declare Resource manager to access to specific cultureinfo
        CultureInfo cul;            //declare culture info

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
                    if (Main.FirstStep)
                    {
                        lbl_FirstStep.Fill = CompletedStep;
                        lbl_SecondStep.Fill = CurrentStep;
                        lbl_ThirdStep.Fill = UnCompletedStep;
                        border_SecondStep.IsEnabled = true;
                        btn_Confirm.IsEnabled = true;
                        System.Diagnostics.Debug.WriteLine("first");
                        Main.FirstStep = false;
                    }
                    else if (Main.SecondStep)
                    {
                        lbl_SecondStep.Fill = CompletedStep;
                        lbl_ThirdStep.Fill = CurrentStep;
                        border_ThirdStep.IsEnabled = true;
                        System.Diagnostics.Debug.WriteLine("second");
                        if (TransferMode == FileOperations.TransferMode.Send) // ********************* Main içerisinde de proses tipi var biri seçilmeli
                        {
                            btn_Confirm.IsEnabled = false;
                        }
                        Main.SecondStep = false;
                    }
                    else if (Main.ThirdStep)
                    {
                        lbl_ThirdStep.Fill = CompletedStep;
                        System.Diagnostics.Debug.WriteLine("third");
                        Main.ThirdStep = false;

                    }

                    if (Main.ExportingVerification)
                    {
                        string sExportingVerification = res_man.GetString("sExportingVerification", cul);
                        string sConfirmation = res_man.GetString("sConfirmation", cul);
                        MessageBoxResult result = MessageBox.Show(sExportingVerification, sConfirmation, MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                            Main.TransferApproved = true;
                    }
             

                    if (TransferMode == FileOperations.TransferMode.Send)
                        txt_IpCode.Text = Main.IpCode;
                    string MainStatus = Main.InfoMsg;
                    if(!MainStatus.Equals(""))
                        txt_StatusInfo.Text = res_man.GetString(MainStatus, cul);
                    txt_FilePath.Text = Main.URL;
                    txt_FileName.Text = Main.FileName;
                    txt_HostName.Text = Main.HostName;
                    txt_FileSize.Text = Main.FileSize.ToString("0.00") + " " + Main.FileSizeType.ToString();
                    txt_TransferSpeed.Text = Main.TransferSpeed.ToString("0.00") + " MB/s";
                    txt_EstimatedTime.Text = Main.EstimatedMin.ToString() + " : " + Main.EstimatedSec.ToString();
                    txt_PassedTime.Text = Main.PassedMin.ToString() + " : " + Main.PassedSec.ToString();
                    pbStatus.Value = Main.CompletedPercentage;
                });

                while (UpdateWatch.ElapsedMilliseconds < UIUpdate_Period)
                {

                }
            }

        }

        private void btn_SendFile_Click(object sender, RoutedEventArgs e)
        {
            Reset();
            string FileURL = SelectFile();
            if (FileURL == null)
            {
                string sSelectionValidFile = res_man.GetString("sSelectionValidFile", cul);
                MessageBox.Show(sSelectionValidFile);
                return;
            }
            TransferMode = FileOperations.TransferMode.Send;
            Main.SetFileURL(FileURL);
            System.Diagnostics.Debug.WriteLine(" FileURL = " + FileURL);
        }
        private void btn_ReceiveFile_Click(object sender, RoutedEventArgs e)
        {
            Reset();
            FileURL = GetFolder();
            if (FileURL == null)
            {
                System.Diagnostics.Debug.WriteLine("File Url is null");
                return;
            }
            TransferMode = FileOperations.TransferMode.Receive;
            Main.FirstStep = true;
            System.Diagnostics.Debug.WriteLine(" FileURL = " + FileURL);
        }
        private void Reset()
        {
            Main.TransferApproved = false;
            Main.FirstStep = true;
            Main.SecondStep = false;
            Main.ThirdStep = false;
            Main.CompletedPercentage = 0;
            Main.PassedMin = 0;
            Main.PassedSec = 0;
            Main.EstimatedMin = 0;
            Main.EstimatedSec = 0;
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
                bool success = Main.EnterTheCode(code);
                if (success)
                {
                    Main.SetFilePathToSave(FileURL);
                    string FileName = Main.FileName;
                    string fileSizeType = Main.FileSizeType.ToString();
                    string fileSize = Main.FileSize.ToString("0.00") + " " + fileSizeType;

                    string sImportingVerification = res_man.GetString("sImportingVerification", cul);
                    string sConfirmation = res_man.GetString("sConfirmation", cul);
                    MessageBoxResult result = MessageBox.Show(sImportingVerification + "\n" + FileName + " file of " + fileSize + " size?", sConfirmation, MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        // Yes code here  
                        Main.RespondToTransferRequest(true);
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        // No code here  
                        Main.RespondToTransferRequest(false);
                    }

                    // Gönderimin alınıp alınmaması durmu burada sorulur
                }
                else
                {
                    MessageBox.Show("Entered code is incorrect!");
                    // kodun hatalı olduğu ise burada gösterilri
                }
            }
            else if (TransferMode == FileOperations.TransferMode.Send && Communication.isClientConnected)
            {
                Main.TransferApproved = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Main.Init(true);
            UI_Init();

            res_man = new ResourceManager("FileSharingApp_Desktop.Resource.resource", Assembly.GetExecutingAssembly());
            cul = CultureInfo.CreateSpecificCulture("en");        //create culture for english

            UIUpdate_thread = new Thread(UpdateUI);
            UIUpdate_thread.IsBackground = true;
            UIUpdate_Start = true;
            UIUpdate_thread.Start();

            //combo_LanguageSelection.SelectedItem = combo_LanguageSelection.Items.GetItemAt(0);
            //switch_language();

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            UIUpdate_Start = false;
            Main.CloseServer();
            Environment.Exit(0);
            Thread.Sleep(10);
        }

        private void UI_Init()
        {
            border_FirstStep.IsEnabled = true;
            border_SecondStep.IsEnabled = false;
            border_ThirdStep.IsEnabled = false;

            lbl_FirstStep.Fill = CurrentStep;
            lbl_SecondStep.Fill = UnCompletedStep;
            lbl_ThirdStep.Fill = UnCompletedStep;
        }

        private void switch_language()
        {
            if(res_man != null)
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
    }
}
