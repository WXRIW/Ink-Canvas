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
            MessageBox.Show("Ink Canvas by WXRIW\n" +
                "Version 1.0.0_beta\n\n" +
                "HotKeys:\n" +
                "Alt + 1: Clean the ink and show or hide the canvas.\n" +
                "Alt + 2: Show or hide the canvas.\n" +
                "Alt + 3: Switch mode (Ink & Eraser)\n" +
                "Alt + 4: Exit.\n" +
                "Ctrl + Z: Erase the last inking.\n" +
                "\n" +
                "You can put an unsigned integer in Thinkness.ini to customize the ink's thinkness.\n" +
                "\n\n" +
                "墨迹画板 by WXRIW\n" +
                "版本 1.0.0_beta\n\n" +
                "快捷键：\n" +
                "Alt + 1: 清除墨迹并显示或隐藏画板\n" +
                "Alt + 2: 显示或隐藏画板\n" +
                "Alt + 3: 切换模式 (墨迹 & 橡皮擦)\n" +
                "Alt + 4: 退出\n" +
                "Ctrl + Z: 删除上一笔\n" +
                "\n" +
                "你可以新建Thinkness.ini文件，在里面放一个正整数，来自定义墨迹的粗细。\n" +
                "\n" +
                "GitHub: https://github.com/WXRIW/Ink-Canvas" +
                "");

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
