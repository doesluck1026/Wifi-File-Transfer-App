using System;
using System.Threading;
using System.Windows;
using System.Globalization;
using System.Threading.Tasks;
using System.Diagnostics;
using Squirrel;

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
            CheckForUpdates();
            AddVersionNumber();
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
        private async Task CheckForUpdates()
        {
            using (var manager = new UpdateManager("https://drive.google.com/drive/folders/1tDKmbxeF8ptchEIssFop8pb6hSxmC1f9?usp=sharing"))
            {
                await manager.UpdateApp();
            }
        }
        private void AddVersionNumber()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            Dispatcher.Invoke(() =>
            {
                this.Title += $" v.{versionInfo.FileVersion}";
            });
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
            System.Diagnostics.Process.Start("https://www.patreon.com/buggycompany");
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
