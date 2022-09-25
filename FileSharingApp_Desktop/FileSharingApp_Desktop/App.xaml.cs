using FileTransfer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FileSharingApp_Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        TextFileHelper TextFile = new TextFileHelper(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)+"\\debug.txt");
        public App()
        {
           
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args == null || e.Args.Length<1)
                return;
            for(int i=0;i<e.Args.Length;i++)
                TextFile.WriteToFile("arg "+i+" : " + e.Args[i],i);
            TransferEngine.FilePaths = e.Args;
        }
    }
}
