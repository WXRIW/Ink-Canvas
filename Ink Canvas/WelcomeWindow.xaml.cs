using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Ink_Canvas
{
    /// <summary>
    /// WelcomeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        public WelcomeWindow()
        {
            InitializeComponent();
        }

        public static bool IsNewBuilding = false;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CheckBoxSetAsRecommendation.IsChecked == true)
            {
                // 完成向导后先保存版本至 versions.ini，再强行重启，以读取设置
                MainWindow.SetSettingsToRecommendation();
            }

            if (CheckBoxRunAtStartup.IsChecked == true)
            {
                MainWindow.StartAutomaticallyCreate("InkCanvas");
            }

            if (CheckBoxAutoKillPptService.IsChecked == true)
            {
                MainWindow.Settings.Automation.IsAutoKillPptService = true;
            }

            if (CheckBoxAutoKillEasiNote.IsChecked == true)
            {
                MainWindow.Settings.Automation.IsAutoKillEasiNote = true;
            }

            if (CheckBoxNewBuildingOptimization.IsChecked == true)
            {
                MainWindow.Settings.Appearance.IsShowEraserButton = true;
                MainWindow.Settings.Startup.IsAutoEnterModeFinger = true;
            }

            MainWindow.SaveSettingsToFile();

            string str = string.Empty;

            try
            {
                str = File.ReadAllText(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Versions.ini");
            }
            catch { }

            str = (str + "\n" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "\n" + "NewWelcomeConfigured").Trim();
            try
            {
                File.WriteAllText("versions.ini", str);
            }
            catch { }
            Process.Start(System.Windows.Forms.Application.ExecutablePath);

            MainWindow.CloseIsFromButton = true;
            Application.Current.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsNewBuilding)
            {
                CheckBoxNewBuildingOptimization.IsChecked = true;
            }
        }
    }
}
