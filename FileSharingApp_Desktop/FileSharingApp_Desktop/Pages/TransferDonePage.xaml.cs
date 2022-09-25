using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FileTransfer;
using FileTransfer.Communication;
using FileTransfer.FileOperation;


namespace FileSharingApp_Desktop.Pages
{
    /// <summary>
    /// Interaction logic for TransferDonePage.xaml
    /// </summary>
    public partial class TransferDonePage : Page
    {
        private int selectedFileIndex =0;
        public TransferDonePage()
        {
            InitializeComponent();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            list_Files.ItemsSource = TransferEngine.FileNames;
            TransferEngine.OnClientRequested += Main_OnClientRequested;
            TransferEngine.OnTransferResponded += Main_OnTransferResponded;
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            TransferEngine.OnClientRequested -= Main_OnClientRequested;
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

        private void list_Files_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedFileIndex = TransferEngine.FileNames.ToList().IndexOf(list_Files.SelectedItem.ToString());
        }

        private void btn_OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(@Parameters.SavingPath);
        }

        private void btn_MainMenu_Click(object sender, RoutedEventArgs e)
        {
            TransferEngine.FilePaths = null;
            Navigator.Navigate("Pages/MainPage.xaml");
        }
        private void btn_OpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectedFilePath = TransferEngine.FilePaths[selectedFileIndex];
                System.Diagnostics.Process.Start(@selectedFilePath);
            }
            catch
            {

            }
        }
    }
}
