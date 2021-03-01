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
            list_Files.ItemsSource = Main.FileNames;
            Main.OnClientRequested += Main_OnClientRequested;
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

        private void list_Files_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedFileIndex = Main.FileNames.ToList().IndexOf(list_Files.SelectedItem.ToString());
        }

        private void btn_OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(@Parameters.SavingPath);
        }

        private void btn_MainMenu_Click(object sender, RoutedEventArgs e)
        {
            Main.FilePaths = null;
            Navigator.Navigate("Pages/MainPage.xaml");
        }
        private void btn_OpenFile_Click(object sender, RoutedEventArgs e)
        {
            string selectedFilePath = Main.FilePaths[selectedFileIndex];
            System.Diagnostics.Process.Start(@selectedFilePath);
        }
    }
}
