using System;
using System.Windows;
using System.Threading;
using FileSharingApp_Desktop.Pages;

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
            MainFrame.NavigationService.Navigate(new MainPage());
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
            Thread.Sleep(10);
        }
    }
}
