using System;
using System.Threading;
using FileSharingApp_Desktop.Pages;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Globalization;
using System.Threading.Tasks;

namespace FileSharingApp_Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance;
        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Parameters.DidInitParameters)
            {
                Parameters.Init();
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Parameters.DeviceLanguage);
                Properties.Resources.Culture = new CultureInfo(Parameters.DeviceLanguage);
                Task.Run(() => ScanNetwork());
            }
            Main.StartServer();
            NetworkScanner.PublishDevice();
            Navigator.Navigate("Pages/SplashScreen.xaml");
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
            Thread.Sleep(10);
        }
        private void Btn_Youtube_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.youtube.com/channel/UCkUWRx8ozzEgi7R2I6OY-BQ");
        }

        private void Btn_Instagram_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.youtube.com/channel/UCkUWRx8ozzEgi7R2I6OY-BQ");
        }

        private void Btn_Patreon_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.youtube.com/channel/UCkUWRx8ozzEgi7R2I6OY-BQ");
        }

        private void Btn_Blogger_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://freakingcyborg.blogspot.com/");
        }
        private void ScanNetwork()
        {
            Dispatcher.Invoke(() =>
            {
                NetworkScanner.ScanAvailableDevices();
            });
        }
    }
}
