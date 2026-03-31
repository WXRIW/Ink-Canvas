using Ink_Canvas.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        private bool isSyncingModeFinger;
        private readonly MainWindowMethodInvoker mainWindowMethodInvoker;
        private readonly SettingsPageModeFingerService settingsPageModeFingerService;

        public SettingsPage()
        {
            mainWindowMethodInvoker = new MainWindowMethodInvoker(GetMainWindow);
            settingsPageModeFingerService = new SettingsPageModeFingerService(this, mainWindowMethodInvoker);
            InitializeComponent();
        }

        public object GetElement(string name)
        {
            return FindName(name);
        }

        private MainWindow GetMainWindow()
        {
            return Application.Current?.MainWindow as MainWindow;
        }

        private void ForwardToMainWindow(string methodName, params object[] parameters)
        {
            mainWindowMethodInvoker.Invoke(methodName, parameters);
        }

        private void SyncModeFingerToggle()
        {
            isSyncingModeFinger = true;
            settingsPageModeFingerService.SyncFromMainWindow();
            isSyncingModeFinger = false;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SyncModeFingerToggle();
        }

        private void ToggleSwitchSettingsModeFinger_Toggled(object sender, RoutedEventArgs e)
        {
            if (isSyncingModeFinger) return;

            settingsPageModeFingerService.SyncToMainWindow();
        }
    }
}
