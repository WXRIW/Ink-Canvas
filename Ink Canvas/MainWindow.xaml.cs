using System;
using System.IO;
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

                if (File.Exists("Thickness.ini"))
                {
                    try
                    {
                        int d = int.Parse(File.ReadAllText("Thickness.ini"));
                        drawingAttributes.Height = d;
                        drawingAttributes.Width = d;
                    }
                    catch
                    {
                        drawingAttributes.Height = 3;
                        drawingAttributes.Width = 3;
                    }
                }
                else
                {
                    drawingAttributes.Height = 3;
                    drawingAttributes.Width = 3;
                }

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
            //MessageBox.Show("Ink Canvas by WXRIW\n" +
            //    "Version 1.0.0_beta\n\n" +
            //    "HotKeys:\n" +
            //    "Alt + 1: Clean the ink and show or hide the canvas.\n" +
            //    "Alt + 2: Show or hide the canvas.\n" +
            //    "Alt + 3: Switch mode (Ink & Eraser)\n" +
            //    "Alt + 4: Exit.\n" +
            //    "Ctrl + Z: Erase the last inking.\n" +
            //    "\n" +
            //    "You can put an unsigned integer in Thinkness.ini to customize the ink's thinkness.\n" +
            //    "\n\n" +
            //    "墨迹画板 by WXRIW\n" +
            //    "版本 1.0.0_beta\n\n" +
            //    "快捷键：\n" +
            //    "Alt + 1: 清除墨迹并显示或隐藏画板\n" +
            //    "Alt + 2: 显示或隐藏画板\n" +
            //    "Alt + 3: 切换模式 (墨迹 & 橡皮擦)\n" +
            //    "Alt + 4: 退出\n" +
            //    "Ctrl + Z: 删除上一笔\n" +
            //    "\n" +
            //    "你可以新建Thinkness.ini文件，在里面放一个正整数，来自定义墨迹的粗细。\n" +
            //    "\n" +
            //    "GitHub: https://github.com/WXRIW/Ink-Canvas" +
            //    "");

            string failedHotKeys = "";

            if (Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.D1, () =>
            {
                if (isInkCanvasVisible)
                {
                    Main_Grid.Visibility = Visibility.Hidden;
                    isInkCanvasVisible = false;
                    //inkCanvas.Strokes.Clear();
                    WindowState = WindowState.Minimized;
                }
                else
                {
                    Main_Grid.Visibility = Visibility.Visible;
                    isInkCanvasVisible = true;
                    inkCanvas.Strokes.Clear();
                    WindowState = WindowState.Maximized;
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
                    WindowState = WindowState.Minimized;
                }
                else
                {
                    Main_Grid.Visibility = Visibility.Visible;
                    isInkCanvasVisible = true;
                    //inkCanvas.Strokes.Clear();
                    WindowState = WindowState.Maximized;
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
        
        private void KeyExit(object sender, ExecutedRoutedEventArgs e)
        {
            if (isInkCanvasVisible)
            {
                Main_Grid.Visibility = Visibility.Hidden;
                isInkCanvasVisible = false;
                //inkCanvas.Strokes.Clear();
                WindowState = WindowState.Minimized;
            }
            else
            {
                Main_Grid.Visibility = Visibility.Visible;
                isInkCanvasVisible = true;
                inkCanvas.Strokes.Clear();
                WindowState = WindowState.Maximized;
            }
        }
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                KeyExit(null, null);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnThickness_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnErase_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.Strokes.Clear();
        }

        private void BtnHideControl_Click(object sender, RoutedEventArgs e)
        {
            if (StackPanelControl.Visibility == Visibility.Visible)
            {
                StackPanelControl.Visibility = Visibility.Hidden;
            }
            else
            {
                StackPanelControl.Visibility = Visibility.Visible;
            }
        }

        private void BtnColorBlack_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.DefaultDrawingAttributes.Color = Colors.Black;
        }

        private void BtnColorRed_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.DefaultDrawingAttributes.Color = Colors.Red;
        }

        private void BtnColorGreen_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FF1ED760");
        }

        private void BtnColorBlue_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FF239AD6");
        }

        private void BtnColorYellow_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FFFFDC00");
        }

        private Color StringToColor(string colorStr)
        {
            Byte[] argb = new Byte[4];
            for (int i = 0; i < 4; i++)
            {
                char[] charArray = colorStr.Substring(i * 2 + 1, 2).ToCharArray();
                //string str = "11";
                Byte b1 = toByte(charArray[0]);
                Byte b2 = toByte(charArray[1]);
                argb[i] = (Byte)(b2 | (b1 << 4));
            }
            return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);//#FFFFFFFF
        }

        private static byte toByte(char c)
        {
            byte b = (byte)"0123456789ABCDEF".IndexOf(c);
            return b;
        }

        int BoundsWidth = 6;

        private void Main_Grid_TouchDown(object sender, TouchEventArgs e)
        {
            Label.Content = e.GetTouchPoint(null).Bounds.Width.ToString();
            if (e.GetTouchPoint(null).Bounds.Width > BoundsWidth)
            {
                inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
            }
            else
            {
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
        }

        int currentMode = 0;

        private void BtnSwitch_Click(object sender, RoutedEventArgs e)
        {
            switch ((++currentMode) % 3)
            {
                case 0:
                    GridBackgroundCover.Background = Brushes.Transparent;
                    break;
                case 1:
                    GridBackgroundCover.Background = Brushes.White;
                    break;
                case 2:
                    GridBackgroundCover.Background = Brushes.Black;
                    break;
            }
        }

        private void BtnColorYellow_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleSwitchModeWei_Toggled(object sender, RoutedEventArgs e)
        {
            if (ToggleSwitchModeWei.IsOn)
            {
                BoundsWidth = 10;
            }
            else
            {
                BoundsWidth = 6;
            }
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
