using AutoUpdaterDotNET;
using Ink_Canvas.Helpers;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        System.Threading.Mutex mutex;

        public static string[] StartArgs = null;

        public App()
        {
            this.Startup += new StartupEventHandler(App_Startup);
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("抱歉，出现未预期的异常，可能导致程序运行不稳定。\n请保存墨迹后重启应用。", "未预期的异常", MessageBoxButton.OK, MessageBoxImage.Error);
            LogHelper.NewLog(e.ToString());
            e.Handled = true;
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            LogHelper.LogFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + LogHelper.LogFileName;

            LogHelper.NewLog(string.Format("Ink Canvas Starting (Version: {0})", Assembly.GetExecutingAssembly().GetName().Version.ToString()));

            bool ret;
            mutex = new System.Threading.Mutex(true, "Ink_Canvas", out ret);

            if (!ret && !e.Args.Contains("-m")) //-m multiple
            {
                LogHelper.NewLog("Detected existing instance");
                MessageBox.Show("已有一个程序实例正在运行");
                LogHelper.NewLog("Ink Canvas automatically closed");
                Environment.Exit(0);
            }

            StartArgs = e.Args;
            AutoUpdater.Start($"http://ink.wxriw.cn:1957/update");
            AutoUpdater.ApplicationExitEvent += () =>
            {
                Environment.Exit(0);
            };
        }
    }
}
