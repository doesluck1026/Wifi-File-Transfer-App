using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for TransferPage.xaml
    /// </summary>
    public partial class TransferPage : Page
    {
        private Timer UpdateTimer;
        private int updatePeriod = 50;          ///ms
        public TransferPage()
        {
            InitializeComponent();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Main.OnTransferFinished += Main_OnTransferFinished;
            Task.Run(() =>
            {
                while (!Main.IsTransfering)//&& TimeSpan.FromSeconds(5).TotalSeconds<5) ;
                {
                    Thread.Sleep(1);
                }
                StartUpdatingUI();
            });
        }
        private void Main_OnTransferFinished()
        {
            
            Dispatcher.Invoke(() => 
            {
                Navigator.Navigate("Pages/TransferDonePage.xaml");
            });
        }

        private void btn_AbortTransfer_Click(object sender, RoutedEventArgs e)
        {
            Main.AbortTransfer();
            Navigator.Navigate("Pages/MainPage.xaml");
        }
        private void StartUpdatingUI()
        {
            UpdateTimer = new Timer(Timer_Tick, null, 0, updatePeriod);
            UpdateTimer.Change(0, updatePeriod);
        }
        private void StopUpdateingUI()
        {
            if (UpdateTimer != null)
            {
                UpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
                UpdateTimer.Dispose();
                UpdateTimer = null;
            }
        }
        private void Timer_Tick(object sender)
        {
            if (UpdateTimer == null)
                return;
            UpdateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            Stopwatch stp = Stopwatch.StartNew();
            UpdateUI();
            long elapsedTime = stp.ElapsedMilliseconds;
            if (UpdateTimer != null)
                UpdateTimer.Change(Math.Max(0, updatePeriod - elapsedTime), updatePeriod);
        }

        private void UpdateUI()
        {
            Dispatcher.Invoke(() => {
                lbl_currentFileNumber.Content = Main.TransferMetrics.IndexOfCurrentFile.ToString();
                lbl_FileCount.Content = Main.TransferMetrics.CountOfFiles.ToString();
                lbl_FileSize.Content = Main.TransferMetrics.CurrentFile.FileSize.ToString("0.00") + " " + Main.TransferMetrics.CurrentFile.SizeUnit;
                lbl_FileName.Content = Main.TransferMetrics.CurrentFile.FileName;
                lbl_progress.Content = "%" + Main.TransferMetrics.Progress.ToString("0.0");
                prg_Transfer.Value = Main.TransferMetrics.Progress;
                lbl_TransferSpeed.Content = Main.TransferMetrics.TransferSpeed.ToString("0.00") + " MB/s";
                lbl_PassedTime.Content = ((int)Main.TransferMetrics.TotalElapsedTime / 3600).ToString("00") + ":" + (((int)Main.TransferMetrics.TotalElapsedTime % 3600) / 60).ToString("00") + ":" +
                    (((int)Main.TransferMetrics.TotalElapsedTime % 3600) % 60).ToString("00");
                lbl_RemainingTime.Content = ((int)Main.TransferMetrics.EstimatedTime / 3600).ToString("00") + ":" + (((int)Main.TransferMetrics.EstimatedTime % 3600) / 60).ToString("00") + ":" +
                    (((int)Main.TransferMetrics.EstimatedTime % 3600) % 60).ToString("00");
                lbl_totalSent.Content = Main.TransferMetrics.TotalDataSent.ToString("0.00") + " " + Main.TransferMetrics.SizeUnit.ToString();
                lbl_totalSize.Content = Main.TransferMetrics.TotalDataSize.ToString("0.00") + " " + Main.TransferMetrics.SizeUnit.ToString();
            });

            if (!Main.IsTransfering)
                StopUpdateingUI();
        }
    }
}
