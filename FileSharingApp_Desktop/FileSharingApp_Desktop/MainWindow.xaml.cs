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
        public MainWindow()
        {
            InitializeComponent();

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
            CheckForUpdates();
            AddVersionNumber();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
            Thread.Sleep(10);
        }

        private async void CheckForUpdates()
        {
            try
            {
                using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/doesluck1026/Wifi-File-Transfer-App"))
                {
                    //updateManager = mgr;
                    var release = await mgr.UpdateApp();
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message + Environment.NewLine;
                //if (ex.InnerException != null)
                //    message += ex.InnerException.Message;
                //MessageBox.Show(message + " soooo: " + ex.ToString());
            }
        }
        //private async void CheckForUpdates()
        //{
        //    try
        //    {
        //        using (var mgr = new UpdateManager("https://disk.yandex.com.tr/d/Yc4HHNREmCRRrg?w=1")) 
        //            {
        //            await mgr.UpdateApp();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string message = ex.Message + Environment.NewLine;
        //        if (ex.InnerException != null)
        //            message += ex.InnerException.Message;
        //        MessageBox.Show(message + " soooo: " + ex.ToString());
        //    }

        //}
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

        private void Btn_PlayStore_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://play.google.com/store/apps/details?id=com.BuggyComp.BuggyFileTransfer");
        }
    }
}
