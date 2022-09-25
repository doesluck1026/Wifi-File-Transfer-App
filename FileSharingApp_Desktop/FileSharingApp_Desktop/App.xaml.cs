﻿using FileTransfer;
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
        public App()
        {
           
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args == null || e.Args.Length<1)
                return;
            TransferEngine.FilePaths = e.Args;
        }
    }
}
