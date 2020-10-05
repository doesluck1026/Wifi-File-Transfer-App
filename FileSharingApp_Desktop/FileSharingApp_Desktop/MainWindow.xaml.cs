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
        private Thread UIUpdate_thread;
        private bool UIUpdate_Start = false;
        private int UIUpdate_Period = 100;      // in ms
        private Brush CompletedStep = Brushes.LimeGreen;
        private Brush CurrentStep = Brushes.LightSkyBlue;
        private Brush UnCompletedStep = Brushes.LightBlue;

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
                        border_SecondStep.IsEnabled = true;
                        System.Diagnostics.Debug.WriteLine("first");
                        Main.FirstStep = false;
                    }
                    else if (Main.SecondStep)
                    {
                        lbl_SecondStep.Fill = CompletedStep;
                        lbl_ThirdStep.Fill = CurrentStep;
                        border_ThirdStep.IsEnabled = true;
                        System.Diagnostics.Debug.WriteLine("second");
                        if(TransferMode == FileOperations.TransferMode.Send) // ********************* Main içerisinde de proses tipi var biri seçilmeli
                                                                             // ********************* Ayrıca receive ve send modları için ayrı UI güncelleme fonksiyonları yazılmalı. bu şekilde olmaz
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
 


                    txt_StatusInfo.Text = Main.InfoMsg;
                   // txt_IpCode.Text = Main.IpCode;
                    txt_FilePath.Text = Main.URL;
                    txt_FileName.Text = Main.FileName;
                    txt_HostName.Text = Main.HostName;
                    txt_FileSize.Text = Main.FileSize.ToString("0.00");
                    txt_TransferSpeed.Text = Main.TransferSpeed.ToString("0.00");

                    pbStatus.Value = Main.CompletedPercentage;

                });


                while (UpdateWatch.ElapsedMilliseconds < UIUpdate_Period)
                {

                }

            }
        
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
            Main.FirstStep = true;
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
                bool success = Main.EnterTheCode(code);
                if (success)
                {
                    Main.SetFilePathToSave(FileURL);
                    string FileName = Main.FileName;
                    string fileSize = Main.FileSize.ToString("0.00");
                    MessageBoxResult result = MessageBox.Show("Do you want to import "+ FileName + " file of" + fileSize + " size?", "Confirmation", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        // Yes code here  
                        Main.RespondToTransferRequest(true);

                    }
                    else if(result == MessageBoxResult.No)
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
            else if (TransferMode == FileOperations.TransferMode.Send)
            {
                Main.TransferApproved = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Main.Init(true);
            UI_Init();
            UIUpdate_thread = new Thread(UpdateUI);
            UIUpdate_thread.IsBackground = true;
            UIUpdate_Start = true;
            UIUpdate_thread.Start();

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            UIUpdate_Start = false;
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

    }
}
