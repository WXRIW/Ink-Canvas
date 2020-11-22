using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        Color Ink_DefaultColor = Colors.Red;

        DrawingAttributes drawingAttributes;
        private void loadPenCanvas()
        {
            try
            {
                drawingAttributes = new DrawingAttributes();
                inkCanvas.DefaultDrawingAttributes = drawingAttributes;
                drawingAttributes.Color = Ink_DefaultColor;
                drawingAttributes.Height = 3;
                drawingAttributes.Width = 3;

                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
            catch { }
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        bool isInkCanvasVisible = true;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string failedHotKeys = "";

            if (Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.D1, () =>
            {
                if (isInkCanvasVisible)
                {
                    Main_Grid.Visibility = Visibility.Hidden;
                    isInkCanvasVisible = false;
                    //inkCanvas.Strokes.Clear();
                }
                else
                {
                    Main_Grid.Visibility = Visibility.Visible;
                    isInkCanvasVisible = true;
                    inkCanvas.Strokes.Clear();
                }
            }) == false)
            {
                failedHotKeys += Environment.NewLine + "Alt + 1";
            }

            if (Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.D2, () =>
            {
                if (isInkCanvasVisible)
                {
                    Main_Grid.Visibility = Visibility.Hidden;
                    isInkCanvasVisible = false;
                    //inkCanvas.Strokes.Clear();
                }
                else
                {
                    Main_Grid.Visibility = Visibility.Visible;
                    isInkCanvasVisible = true;
                    //inkCanvas.Strokes.Clear();
                }
            }) == false)
            {
                failedHotKeys += Environment.NewLine + "Alt + 2";
            }

            if (Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.D3, () =>
            {
                if (inkCanvas.EditingMode != InkCanvasEditingMode.Ink)
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                }
                else
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                }
            }) == false)
            {
                failedHotKeys += Environment.NewLine + "Alt + 3";
            }

            if (Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.D4, () =>
            {
                Close();
            }) == false)
            {
                failedHotKeys += Environment.NewLine + "Alt + 4";
            }

            loadPenCanvas();
        }

        private void back_HotKey(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                inkCanvas.Strokes.Remove(inkCanvas.Strokes[inkCanvas.Strokes.Count - 1]);
            }
            catch { }
        }
    }

    enum HotkeyModifiers
    {
        MOD_ALT = 0x1,
        MOD_CONTROL = 0x2,
        MOD_SHIFT = 0x4,
        MOD_WIN = 0x8
    }

}
