using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FileSharingApp_Desktop.Pages
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Page
    {
        public SplashScreen()
        {
            InitializeComponent();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                System.Threading.Thread.Sleep(500);
                OnTimedEvent();
            });
        }

        private void OnTimedEvent()
        {
            Dispatcher.Invoke(() =>
            {
                Navigator.Navigate("Pages/MainPage.xaml");
            });
        }
    }
}
