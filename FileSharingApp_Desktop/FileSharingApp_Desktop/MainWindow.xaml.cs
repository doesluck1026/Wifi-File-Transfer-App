using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

            System.Diagnostics.Debug.WriteLine("push yapıyorum");
            
        }

        private void btn_SendFile_Click(object sender, RoutedEventArgs e)
        {
            string FileURL = SelectFile();
            if(FileURL == null)
            {
                //show wrong url message
                return;
            }
            System.Diagnostics.Debug.WriteLine(" FileURL = " + FileURL);

        }

        private void btn_ReceiveFile_Click(object sender, RoutedEventArgs e)
        {
            string FileURL = SelectFile();
            if (FileURL == null)
            {
                //show wrong url message
                return;
            }
            System.Diagnostics.Debug.WriteLine(" FileURL = " + FileURL);

        }

        /// <summary>
        /// The address of the file to be processed is selected
        /// </summary>
        /// <returns>the address of the file in memory</returns>
        private string SelectFile()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = " All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = true;
            
            if (openFileDialog1.ShowDialog() == true)
            {
                string selectedFileName = openFileDialog1.FileName;
                return selectedFileName;
            }

            return null;
        }

    }
}
