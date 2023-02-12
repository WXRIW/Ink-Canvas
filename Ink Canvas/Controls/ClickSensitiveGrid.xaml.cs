using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for ClickSensitiveGrid.xaml
    /// </summary>
    public partial class ClickSensitiveGrid : Grid
    {
        public ClickSensitiveGrid()
        {
            InitializeComponent();
        }

        public event RoutedEventHandler Click
        {
            add { AddHandler(ClickRoutedEvent, value); }
            remove { RemoveHandler(ClickRoutedEvent, value); }
        }
        public static readonly RoutedEvent ClickRoutedEvent = EventManager.RegisterRoutedEvent("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ClickSensitiveGrid));

        bool isMouseDown = false;

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isMouseDown)
            {
                try
                {
                    RoutedEventArgs _e = new RoutedEventArgs();
                    _e.RoutedEvent = ClickRoutedEvent;
                    _e.Source = this;
                    this.RaiseEvent(_e);
                }
                catch { }
            }
            isMouseDown = false;
        }
    }
}
