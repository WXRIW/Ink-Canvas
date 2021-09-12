using ModernWpf;
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
using Microsoft.Office.Interop.PowerPoint;
using System.Runtime.InteropServices;
using Application = System.Windows.Application;
using System.Timers;
using System.Threading;
using Timer = System.Timers.Timer;

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

            timerCheckPPT.Elapsed += TimerCheckPPT_Elapsed;
            timerCheckPPT.Interval = 1000;
            timerCheckPPT.Start();
        }

        Timer timerCheckPPT = new Timer();

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
                //drawingAttributes = new DrawingAttributes();
                drawingAttributes = inkCanvas.DefaultDrawingAttributes;
                drawingAttributes.Color = Ink_DefaultColor;

                if (File.Exists("Thickness.ini"))
                {
                    try
                    {
                        double d = double.Parse(File.ReadAllText("Thickness.ini"));
                        drawingAttributes.Height = d;
                        drawingAttributes.Width = d;
                    }
                    catch
                    {
                        drawingAttributes.Height = 2.5;
                        drawingAttributes.Width = 2.5;
                    }
                }
                else
                {
                    drawingAttributes.Height = 2.5;
                    drawingAttributes.Width = 2.5;
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

            //string failedHotKeys = "";

            //if (Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.D1, () =>
            //{
            //    if (isInkCanvasVisible)
            //    {
            //        Main_Grid.Visibility = Visibility.Hidden;
            //        isInkCanvasVisible = false;
            //        //inkCanvas.Strokes.Clear();
            //        WindowState = WindowState.Minimized;
            //    }
            //    else
            //    {
            //        Main_Grid.Visibility = Visibility.Visible;
            //        isInkCanvasVisible = true;
            //        inkCanvas.Strokes.Clear();
            //        WindowState = WindowState.Maximized;
            //    }
            //}) == false)
            //{
            //    failedHotKeys += Environment.NewLine + "Alt + 1";
            //}

            //if (Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.D2, () =>
            //{
            //    if (isInkCanvasVisible)
            //    {
            //        Main_Grid.Visibility = Visibility.Hidden;
            //        isInkCanvasVisible = false;
            //        //inkCanvas.Strokes.Clear();
            //        WindowState = WindowState.Minimized;
            //    }
            //    else
            //    {
            //        Main_Grid.Visibility = Visibility.Visible;
            //        isInkCanvasVisible = true;
            //        //inkCanvas.Strokes.Clear();
            //        WindowState = WindowState.Maximized;
            //    }
            //}) == false)
            //{
            //    failedHotKeys += Environment.NewLine + "Alt + 2";
            //}

            //if (Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.D3, () =>
            //{
            //    if (inkCanvas.EditingMode != InkCanvasEditingMode.Ink)
            //    {
            //        inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            //    }
            //    else
            //    {
            //        inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
            //    }
            //}) == false)
            //{
            //    failedHotKeys += Environment.NewLine + "Alt + 3";
            //}

            //if (Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.D4, () =>
            //{
            //    Close();
            //}) == false)
            //{
            //    failedHotKeys += Environment.NewLine + "Alt + 4";
            //}

            loadPenCanvas();

            BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
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
            //if (isInkCanvasVisible)
            //{
            //    Main_Grid.Visibility = Visibility.Hidden;
            //    isInkCanvasVisible = false;
            //    //inkCanvas.Strokes.Clear();
            //    WindowState = WindowState.Minimized;
            //}
            //else
            //{
            //    Main_Grid.Visibility = Visibility.Visible;
            //    isInkCanvasVisible = true;
            //    inkCanvas.Strokes.Clear();
            //    WindowState = WindowState.Maximized;
            //}
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

        bool forceEraser = false;

        private void BtnErase_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = true;
            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = false;
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

        #region Buttons - Color

        int inkColor = 0;

        private void ColorSwitchCheck()
        {
            if (Main_Grid.Background == Brushes.Transparent)
            {
                BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                if (currentMode == 1)
                {
                    currentMode = 0;
                    GridBackgroundCover.Visibility = Visibility.Hidden;
                }
            }
        }

        private void BtnColorBlack_Click(object sender, RoutedEventArgs e)
        {
            inkColor = 0;
            forceEraser = false;
            if (BtnSwitchTheme.Content.ToString() == "浅色")
            {
                inkCanvas.DefaultDrawingAttributes.Color = Colors.White;
            }
            else
            {
                inkCanvas.DefaultDrawingAttributes.Color = Colors.Black;
            }

            ColorSwitchCheck();
        }

        private void BtnColorRed_Click(object sender, RoutedEventArgs e)
        {
            inkColor = 1;
            forceEraser = false;
            inkCanvas.DefaultDrawingAttributes.Color = Colors.Red;

            ColorSwitchCheck();
        }

        private void BtnColorGreen_Click(object sender, RoutedEventArgs e)
        {
            inkColor = 2;
            forceEraser = false;
            inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FF1ED760");

            ColorSwitchCheck();
        }

        private void BtnColorBlue_Click(object sender, RoutedEventArgs e)
        {
            inkColor = 3;
            forceEraser = false;
            inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FF239AD6");

            ColorSwitchCheck();
        }

        private void BtnColorYellow_Click(object sender, RoutedEventArgs e)
        {
            inkColor = 4;
            forceEraser = false;
            inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FFFFDC00");

            ColorSwitchCheck();
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

        #endregion

        int BoundsWidth = 6;

        private void Main_Grid_TouchDown(object sender, TouchEventArgs e)
        {
            if (forceEraser) return;
            //Label.Content = e.GetTouchPoint(null).Bounds.Width.ToString();
            if (ToggleSwitchAutoWeight.IsOn && e.GetTouchPoint(null).Bounds.Width != 0)
            {
                inkCanvas.DefaultDrawingAttributes.Width = e.GetTouchPoint(null).Bounds.Width / 2 + 1;
                inkCanvas.DefaultDrawingAttributes.Height = inkCanvas.DefaultDrawingAttributes.Width;
            }
            else
            {
                if (e.GetTouchPoint(null).Bounds.Width > BoundsWidth)
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                }
                else
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                }
            }
        }

        int currentMode = 0;

        private void BtnSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (Main_Grid.Background == Brushes.Transparent)
            {
                BtnHideInkCanvas_Click(BtnHideInkCanvas, e);
                if (currentMode == 0)
                {
                    currentMode++;
                    GridBackgroundCover.Visibility = Visibility.Visible;
                }
            }
            else
            {
                switch ((++currentMode) % 2)
                {
                    case 0:
                        GridBackgroundCover.Visibility = Visibility.Hidden;
                        break;
                    case 1:
                        GridBackgroundCover.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        private void BtnSwitchTheme_Click(object sender, RoutedEventArgs e)
        {
            if (BtnSwitchTheme.Content.ToString() == "深色")
            {
                BtnSwitchTheme.Content = "浅色";
                BtnExit.Foreground = Brushes.White;
                GridBackgroundCover.Background = new SolidColorBrush(StringToColor("#FF1A1A1A"));
                BtnColorBlack.Background = Brushes.White;
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                if (inkColor == 0)
                {
                    inkCanvas.DefaultDrawingAttributes.Color = Colors.White;
                }
            }
            else
            {
                BtnSwitchTheme.Content = "深色";
                BtnExit.Foreground = Brushes.Black;
                GridBackgroundCover.Background = new SolidColorBrush(StringToColor("#FFF2F2F2"));
                BtnColorBlack.Background = Brushes.Black;
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                if (inkColor == 0)
                {
                    inkCanvas.DefaultDrawingAttributes.Color = Colors.Black;
                }
            }
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

        private void BtnHideInkCanvas_Click(object sender, RoutedEventArgs e)
        {
            if(Main_Grid.Background == Brushes.Transparent)
            {
                Main_Grid.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
                inkCanvas.Visibility = Visibility.Visible;
                GridBackgroundCoverHolder.Visibility = Visibility.Visible;
                BtnHideInkCanvas.Content = "隐藏\n画板";
            }
            else
            {
                Main_Grid.Background = Brushes.Transparent;
                inkCanvas.Visibility = Visibility.Collapsed;
                GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;
                BtnHideInkCanvas.Content = "显示\n画板";
            }
        }

        private void BtnSwitchSide_Click(object sender, RoutedEventArgs e)
        {
            if (StackPanelMain.HorizontalAlignment == HorizontalAlignment.Right)
            {
                StackPanelMain.HorizontalAlignment = HorizontalAlignment.Left;
            }
            else
            {
                StackPanelMain.HorizontalAlignment = HorizontalAlignment.Right;
            }
        }

        Microsoft.Office.Interop.PowerPoint.Application pptApplication = null;
        Microsoft.Office.Interop.PowerPoint.Presentation presentation = null;
        Microsoft.Office.Interop.PowerPoint.Slides slides = null;
        Microsoft.Office.Interop.PowerPoint.Slide slide = null;
        int slidescount = 0;
        private void BtnCheckPPT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                pptApplication = (Microsoft.Office.Interop.PowerPoint.Application)Marshal.GetActiveObject("PowerPoint.Application");
                //pptApplication.SlideShowWindows[1].View.Next();

                if (pptApplication != null)
                {
                    //获得演示文稿对象
                    presentation = pptApplication.ActivePresentation;
                    pptApplication.SlideShowBegin += PptApplication_SlideShowBegin;
                    pptApplication.SlideShowNextSlide += PptApplication_SlideShowNextSlide;
                    pptApplication.SlideShowEnd += PptApplication_SlideShowEnd;
                    // 获得幻灯片对象集合
                    slides = presentation.Slides;
                    // 获得幻灯片的数量
                    slidescount = slides.Count;
                    memoryStreams = new MemoryStream[slidescount + 2];
                    // 获得当前选中的幻灯片
                    try
                    {
                        // 在普通视图下这种方式可以获得当前选中的幻灯片对象
                        // 然而在阅读模式下，这种方式会出现异常
                        slide = slides[pptApplication.ActiveWindow.Selection.SlideRange.SlideNumber];
                    }
                    catch
                    {
                        // 在阅读模式下出现异常时，通过下面的方式来获得当前选中的幻灯片对象
                        slide = pptApplication.SlideShowWindows[1].View.Slide;
                    }
                }

                if (pptApplication == null) throw new Exception();
                //BtnCheckPPT.Visibility = Visibility.Collapsed;
                StackPanelPPTControls.Visibility = Visibility.Visible;
            }
            catch
            {
                //BtnCheckPPT.Visibility = Visibility.Visible;
                StackPanelPPTControls.Visibility = Visibility.Collapsed;
                MessageBox.Show("未找到幻灯片");
            }
        }

        private void TimerCheckPPT_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                pptApplication = (Microsoft.Office.Interop.PowerPoint.Application)Marshal.GetActiveObject("PowerPoint.Application");

                if (pptApplication != null)
                {
                    timerCheckPPT.Stop();
                    //获得演示文稿对象
                    presentation = pptApplication.ActivePresentation;
                    pptApplication.PresentationClose += PptApplication_PresentationClose;
                    pptApplication.SlideShowBegin += PptApplication_SlideShowBegin;
                    pptApplication.SlideShowNextSlide += PptApplication_SlideShowNextSlide;
                    pptApplication.SlideShowEnd += PptApplication_SlideShowEnd;
                    // 获得幻灯片对象集合
                    slides = presentation.Slides;
                    // 获得幻灯片的数量
                    slidescount = slides.Count;
                    memoryStreams = new MemoryStream[slidescount + 2];
                    // 获得当前选中的幻灯片
                    try
                    {
                        // 在普通视图下这种方式可以获得当前选中的幻灯片对象
                        // 然而在阅读模式下，这种方式会出现异常
                        slide = slides[pptApplication.ActiveWindow.Selection.SlideRange.SlideNumber];
                    }
                    catch
                    {
                        // 在阅读模式下出现异常时，通过下面的方式来获得当前选中的幻灯片对象
                        slide = pptApplication.SlideShowWindows[1].View.Slide;
                    }
                }

                if (pptApplication == null) throw new Exception();
                //BtnCheckPPT.Visibility = Visibility.Collapsed;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    BtnPPTSlideShow.Visibility = Visibility.Visible;
                });
            }
            catch
            {
                //StackPanelPPTControls.Visibility = Visibility.Collapsed;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    BtnPPTSlideShow.Visibility = Visibility.Collapsed;
                });
                timerCheckPPT.Start();
            }
        }

        private void PptApplication_PresentationClose(Presentation Pres)
        {
            timerCheckPPT.Start();
            BtnPPTSlideShow.Visibility = Visibility.Collapsed;
            BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;
        }

        private void PptApplication_SlideShowBegin(SlideShowWindow Wn)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StackPanelPPTControls.Visibility = Visibility.Visible;
                BtnPPTSlideShow.Visibility = Visibility.Collapsed;
                BtnPPTSlideShowEnd.Visibility = Visibility.Visible;
                StackPanelMain.Margin = new Thickness(10, 0, 10, 10);
            });
            previousSlideID = Wn.View.CurrentShowPosition;
        }

        private void PptApplication_SlideShowEnd(Presentation Pres)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                BtnPPTSlideShow.Visibility = Visibility.Visible;
                BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;
                StackPanelPPTControls.Visibility = Visibility.Collapsed;
                StackPanelMain.Margin = new Thickness(10, 0, 10, 55);
                inkCanvas.Strokes.Clear();
                if (Main_Grid.Background != Brushes.Transparent)
                {
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }
            });
        }

        int previousSlideID = 0;
        MemoryStream[] memoryStreams = new MemoryStream[50];

        private void PptApplication_SlideShowNextSlide(SlideShowWindow Wn)
        {
            if (Wn.View.CurrentShowPosition != previousSlideID)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MemoryStream ms = new MemoryStream();
                    inkCanvas.Strokes.Save(ms);
                    ms.Position = 0;
                    memoryStreams[previousSlideID] = ms;
                    
                    inkCanvas.Strokes.Clear();
                    try
                    {
                        if (memoryStreams[Wn.View.CurrentShowPosition].Length > 0)
                        {
                            inkCanvas.Strokes = new System.Windows.Ink.StrokeCollection(memoryStreams[Wn.View.CurrentShowPosition]);
                        }
                    }
                    catch (Exception ex)
                    { }
                });
                previousSlideID = Wn.View.CurrentShowPosition;
            }
        }

        private void BtnPPTSlidesUp_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == 1)
            {
                GridBackgroundCover.Visibility = Visibility.Hidden;
                currentMode = 0;
            }

            try
            {
                pptApplication.SlideShowWindows[1].View.Application.SlideShowWindows[1].Activate();
                pptApplication.SlideShowWindows[1].View.Previous();
            }
            catch
            {
                //BtnCheckPPT.Visibility = Visibility.Visible;
                StackPanelPPTControls.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnPPTSlidesDown_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == 1)
            {
                GridBackgroundCover.Visibility = Visibility.Hidden;
                currentMode = 0;
            }

            try
            {
                pptApplication.SlideShowWindows[1].View.Application.SlideShowWindows[1].Activate();
                pptApplication.SlideShowWindows[1].View.Next();
            }
            catch (Exception ex)
            {
                //BtnCheckPPT.Visibility = Visibility.Visible;
                StackPanelPPTControls.Visibility = Visibility.Collapsed;
                //MessageBox.Show(ex.ToString());
            }
        }

        private void ToggleSwitchAutoWeight_Toggled(object sender, RoutedEventArgs e)
        {

        }

        private void BtnPPTSlideShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new Thread(new ThreadStart(() => {
                    //presentation.SlideShowSettings.StartingSlide = 1;// pptApplication.ActiveWindow.Selection.SlideRange.SlideNumber;
                    //presentation.SlideShowSettings.EndingSlide = 1;
                    presentation.SlideShowSettings.Run();
                })).Start();
            }
            catch { }
        }

        private void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            new Thread(new ThreadStart(() => {
                try
                {
                    pptApplication.SlideShowWindows[1].View.Exit();
                }
                catch { }
            })).Start();
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
