using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        System.Threading.Mutex mutex;

        public App()
        {
            this.Startup += new StartupEventHandler(App_Startup);
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            bool ret;
            mutex = new System.Threading.Mutex(true, "Ink_Canvas", out ret);

            if (!ret)
            {
                MessageBox.Show("已有一个程序实例正在运行");
                Environment.Exit(0);
            }

        }
    }
}
