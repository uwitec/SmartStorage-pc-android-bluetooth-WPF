using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace RFIDBackground
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static StorageDB storageDB = new StorageDB();
        public static StorageDB StorageDB
        {
            get { return storageDB; }
        }

        //protected override void OnStartup(StartupEventArgs e)
        //{
        //    SplashScreen splashScreen = new SplashScreen("images/begin.png");
        //    splashScreen.Show(false);
        //    splashScreen.Close(TimeSpan.FromSeconds(1));
        //    System.Threading.Thread.Sleep(1000);
        //    base.OnStartup(e);
        //}
    }
}
