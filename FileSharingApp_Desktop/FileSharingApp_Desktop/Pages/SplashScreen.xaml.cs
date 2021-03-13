using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
