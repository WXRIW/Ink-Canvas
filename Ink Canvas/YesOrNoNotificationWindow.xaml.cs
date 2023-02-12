using System;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for RestoreHiddenSlidesWindow.xaml
    /// </summary>
    public partial class YesOrNoNotificationWindow : Window
    {
        private readonly Action _yesAction;
        private readonly Action _noAction;

        public YesOrNoNotificationWindow(string text, Action yesAction = null, Action noAction = null)
        {
            _yesAction = yesAction;
            _noAction = noAction;
            InitializeComponent();
            Label.Content = text;
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            if (_yesAction == null)
            {
                Close();
                return;
            }

            _yesAction.Invoke();
            Close();

        }

        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            if (_noAction == null)
            {
                Close();
                return;
            }

            _noAction.Invoke();
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            MainWindow.IsShowingRestoreHiddenSlidesWindow = false;
        }
    }
}