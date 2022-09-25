using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace FileSharingApp_Desktop.Pages
{
    /// <summary>
    /// Interaction logic for TransferPage.xaml
    /// </summary>
    public partial class TransferPage : Page
    {
        private Timer UpdateTimer;
        private int updatePeriod = 30;          ///ms
        public TransferPage()
        {
            InitializeComponent();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TransferEngine.OnTransferFinished += Main_OnTransferFinished;
            TransferEngine.OnTransferAborted += Main_OnTransferAborted;
            TransferEngine.OnTransferResponded += Main_OnTransferResponded;
            Task.Run(() =>
            {
                while (!TransferEngine.IsTransfering)//&& TimeSpan.FromSeconds(5).TotalSeconds<5) ;
                {
                    Thread.Sleep(2);
                }
                StartUpdatingUI();
            });
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            TransferEngine.OnTransferFinished -= Main_OnTransferFinished;
            TransferEngine.OnTransferAborted -= Main_OnTransferAborted;
            TransferEngine.OnTransferResponded -= Main_OnTransferResponded;
        }

        private void Main_OnTransferResponded(bool isAccepted)
        {
            Debug.WriteLine("Receiver Response: " + isAccepted);
            if (isAccepted)
            {
                Dispatcher.Invoke(() =>
                {
                    Navigator.Navigate("Pages/TransferPage.xaml");
                });
                TransferEngine.BeginSendingFiles();
            }
            else
            {
                var result = MessageBox.Show(Properties.Resources.Send_Warning_rejected, Properties.Resources.Rejected_Title, button: MessageBoxButton.OK);
            }
        }
        private void Main_OnTransferAborted()
        {
            MessageBox.Show(Properties.Resources.Transfer_AbortedMessage);
            Dispatcher.Invoke(() =>
            {
                TransferEngine.FilePaths = null;
                Navigator.Navigate("Pages/MainPage.xaml");
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
            Dispatcher.Invoke(() =>
            {
                var result = MessageBox.Show(Properties.Resources.Transfer_ConfirmAbortMessage, Properties.Resources.Transfer_ConfirmAbortTitle, MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    TransferEngine.OnTransferFinished -= Main_OnTransferFinished;
                    TransferEngine.AbortTransfer();
                    TransferEngine.FilePaths = null;
                    Navigator.Navigate("Pages/MainPage.xaml");
                }
            });
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
            var metrics = TransferEngine.TransferMetrics;
            Dispatcher.Invoke(() => {
                lbl_currentFileNumber.Content = metrics.IndexOfCurrentFile.ToString();
                lbl_FileCount.Content = metrics.CountOfFiles.ToString();
                lbl_FileSize.Content = metrics.CurrentFile.FileSize.ToString("0.00") + " " + metrics.CurrentFile.SizeUnit;
                lbl_FileName.Content = metrics.CurrentFile.FileName;
                lbl_progress.Content = "%" + metrics.Progress.ToString("0.0");
                int progress = (int)Math.Min(100, metrics.Progress);
                prg_Transfer.Value = Math.Max(0,progress);
                lbl_TransferSpeed.Content = metrics.TransferSpeed.ToString("0.00") + " MB/s";
                lbl_PassedTime.Content = ((int)metrics.TotalElapsedTime / 3600).ToString("00") + ":" + (((int)metrics.TotalElapsedTime % 3600) / 60).ToString("00") + ":" +
                    (((int)metrics.TotalElapsedTime % 3600) % 60).ToString("00");
                lbl_RemainingTime.Content = ((int)metrics.EstimatedTime / 3600).ToString("00") + ":" + (((int)metrics.EstimatedTime % 3600) / 60).ToString("00") + ":" +
                    (((int)metrics.EstimatedTime % 3600) % 60).ToString("00");
                lbl_totalSent.Content = metrics.TotalDataSent.ToString("0.00") + " " + metrics.SentSizeUnit.ToString();
                lbl_totalSize.Content = metrics.TotalDataSize.ToString("0.00") + " " + metrics.SizeUnit.ToString();
                lbl_Receiver.Content = metrics.ReceiverDevice;
            });

            if (!TransferEngine.IsTransfering)
                StopUpdateingUI();
        }

        
    }
}
