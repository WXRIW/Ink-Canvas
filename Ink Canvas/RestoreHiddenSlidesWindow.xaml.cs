using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Application = System.Windows.Application;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for RestoreHiddenSlidesWindow.xaml
    /// </summary>
    public partial class RestoreHiddenSlidesWindow : Window
    {
        public RestoreHiddenSlidesWindow()
        {
            InitializeComponent();
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            foreach (Slide slide in MainWindow.slides)
            {
                if (slide.SlideShowTransition.Hidden == Microsoft.Office.Core.MsoTriState.msoTrue)
                {
                    slide.SlideShowTransition.Hidden = Microsoft.Office.Core.MsoTriState.msoFalse;
                }
            }

            Close();
        }

        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            MainWindow.IsShowingRestoreHiddenSlidesWindow = false;
        }
    }
}
