using System;
using System.Threading;
using FileSharingApp_Desktop.Pages;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

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
            Main.StartServer();
            NetworkScanner.PublishDevice();
            Navigator.Navigate("Pages/SplashScreen.xaml");
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
            Thread.Sleep(10);
        }
    }
}
