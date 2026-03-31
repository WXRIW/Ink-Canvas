namespace Ink_Canvas.Helpers
{
    internal sealed class SettingsPageModeFingerService
    {
        private readonly SettingsPage settingsPage;
        private readonly MainWindowMethodInvoker mainWindowMethodInvoker;

        public SettingsPageModeFingerService(SettingsPage settingsPage, MainWindowMethodInvoker mainWindowMethodInvoker)
        {
            this.settingsPage = settingsPage;
            this.mainWindowMethodInvoker = mainWindowMethodInvoker;
        }

        public void SyncFromMainWindow()
        {
            MainWindow mainWindow = mainWindowMethodInvoker.GetMainWindow();
            if (mainWindow == null) return;

            GetModeFingerToggle().IsOn = mainWindow.ToggleSwitchModeFinger.IsOn;
        }

        public void SyncToMainWindow()
        {
            MainWindow mainWindow = mainWindowMethodInvoker.GetMainWindow();
            if (mainWindow == null) return;

            mainWindow.ToggleSwitchModeFinger.IsOn = GetModeFingerToggle().IsOn;
        }

        private dynamic GetModeFingerToggle()
        {
            return settingsPage.GetElement("ToggleSwitchSettingsModeFinger");
        }
    }
}
