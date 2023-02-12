using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                var MW = Application.Current.MainWindow as MainWindow;
                MW.BtnExit.Visibility = Visibility.Visible;
            });


        }
    }
}
