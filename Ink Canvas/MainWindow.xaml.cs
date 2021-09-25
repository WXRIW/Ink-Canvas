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
using System.Diagnostics;
using Newtonsoft.Json;
using IWshRuntimeLibrary;
using File = System.IO.File;
using System.Collections.ObjectModel;
using System.Net;
using Microsoft.VisualBasic;
using System.Reflection;

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
            BorderSettings.Visibility = Visibility.Collapsed;
        }

        Timer timerCheckPPT = new Timer();

        Settings Settings = new Settings();

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

                drawingAttributes.Height = 2.5;
                drawingAttributes.Width = 2.5;

                //if (File.Exists("Thickness.ini"))
                //{
                //    try
                //    {
                //        double d = double.Parse(File.ReadAllText("Thickness.ini"));
                //        drawingAttributes.Height = d;
                //        drawingAttributes.Width = d;
                //    }
                //    catch
                //    {
                //        drawingAttributes.Height = 2.5;
                //        drawingAttributes.Width = 2.5;
                //    }
                //}
                //else
                //{
                //    drawingAttributes.Height = 2.5;
                //    drawingAttributes.Width = 2.5;
                //}

                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                inkCanvas.Gesture += InkCanvas_Gesture;
            }
            catch { }
        }

        ApplicationGesture lastApplicationGesture = ApplicationGesture.AllGestures;
        DateTime lastGestureTime = DateTime.Now;
        private void InkCanvas_Gesture(object sender, InkCanvasGestureEventArgs e)
        {
            ReadOnlyCollection<GestureRecognitionResult> gestures = e.GetGestureRecognitionResults();

            foreach (GestureRecognitionResult gest in gestures)
            {
                //Trace.WriteLine(string.Format("Gesture: {0}, Confidence: {1}", gest.ApplicationGesture, gest.RecognitionConfidence));
                if ((DateTime.Now - lastGestureTime).TotalMilliseconds <= 1500 &&
                    StackPanelPPTControls.Visibility == Visibility.Visible &&
                    lastApplicationGesture == gest.ApplicationGesture)
                {
                    if (gest.ApplicationGesture == ApplicationGesture.Left)
                    {
                        BtnPPTSlidesDown_Click(BtnPPTSlidesDown, null);
                    }
                    if (gest.ApplicationGesture == ApplicationGesture.Right)
                    {
                        BtnPPTSlidesDown_Click(BtnPPTSlidesDown, null);
                    }
                }

                lastApplicationGesture = gest.ApplicationGesture;
                lastGestureTime = DateTime.Now;
            }

            inkCanvas.Strokes.Add(e.Strokes);
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        bool isInkCanvasVisible = true;
        bool isAutoUpdateEnabled = false;
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

            //检查
            new Thread(new ThreadStart(() => {
                try
                {
                    string response = GetWebClient("http://ink.wxriw.cn:1957");
                    if (response.Contains("Special Version"))
                    {
                        isAutoUpdateEnabled = true;

                        if (response.Contains("<notice>"))
                        {
                            string str = Strings.Mid(response, response.IndexOf("<notice>") + 9);
                            if (str.Contains("<notice>"))
                            {
                                str = Strings.Left(str, str.IndexOf("<notice>")).Trim();
                                if (str.Length > 0)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        GroupBoxMASEZVersion.Visibility = Visibility.Visible;
                                        TextBlockMASEZNotice.Text = str;
                                    });
                                }
                            }
                        }

                        try
                        {
                            if (response.Contains("<update>"))
                            {
                                string str = Strings.Mid(response, response.IndexOf("<update>") + 9);
                                if (str.Contains("<update>"))
                                {
                                    str = Strings.Left(str, str.IndexOf("<update>")).Trim();
                                    if (str.Length > 0)
                                    {
                                        string updateIP;
                                        int updatePort;

                                        string[] vs = str.Split(':');
                                        updateIP = vs[0];
                                        updatePort = int.Parse(vs[1]);

                                        if (OAUS.Core.VersionHelper.HasNewVersion(GetIp(updateIP), updatePort))
                                        {
                                            string updateExePath = AppDomain.CurrentDomain.BaseDirectory + "AutoUpdater\\AutoUpdater.exe";
                                            System.Diagnostics.Process myProcess = System.Diagnostics.Process.Start(updateExePath);

                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                Close();
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            })).Start();

            //加载设置
            if (File.Exists(settingsFileName))
            {
                try
                {
                    string text = File.ReadAllText(settingsFileName);
                    Settings = JsonConvert.DeserializeObject<Settings>(text);
                }
                catch { }
            }

            if (Settings.Startup.IsAutoEnterModeFinger)
            {
                ToggleSwitchModeFinger.IsOn = true;
                ToggleSwitchAutoEnterModeFinger.IsOn = true;
            }
            if (Settings.Startup.IsAutoHideCanvas)
            {
                BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                ToggleSwitchAutoHideCanvas.IsOn = true;
            }

            if (!Settings.Appearance.IsShowEraserButton)
            {
                BtnErase.Visibility = Visibility.Collapsed;
                ToggleSwitchShowButtonEraser.IsOn = false;
            }
            if (!Settings.Appearance.IsShowExitButton)
            {
                BtnExit.Visibility = Visibility.Collapsed;
                ToggleSwitchShowButtonExit.IsOn = false;
            }
            if (!Settings.Appearance.IsShowHideControlButton)
            {
                BtnHideControl.Visibility = Visibility.Collapsed;
                ToggleSwitchShowButtonHideControl.IsOn = false;
            }
            if (!Settings.Appearance.IsShowLRSwitchButton)
            {
                BtnSwitchSide.Visibility = Visibility.Collapsed;
                ToggleSwitchShowButtonLRSwitch.IsOn = false;
            }
            if (!Settings.Appearance.IsShowModeFingerToggleSwitch)
            {
                StackPanelModeFinger.Visibility = Visibility.Collapsed;
                ToggleSwitchShowButtonModeFinger.IsOn = false;
            }
            if (!Settings.Appearance.IsTransparentButtonBackground)
            {
                BtnExit.Background = new SolidColorBrush(StringToColor("#FFCCCCCC"));
            }

            if (Settings.Behavior.PowerPointSupport)
            {
                timerCheckPPT.Elapsed += TimerCheckPPT_Elapsed;
                timerCheckPPT.Interval = 1000;
                timerCheckPPT.Start();
            }
            else
            {
                ToggleSwitchSupportPowerPoint.IsOn = false;
            }
            if (!Settings.Behavior.IsShowCanvasAtNewSlideShow)
            {
                ToggleSwitchShowCanvasAtNewSlideShow.IsOn = false;
            }

            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\InkCanvas" + ".lnk"))
            {
                ToggleSwitchRunAtStartup.IsOn = true;
            }

            loadPenCanvas();

            if (Settings.Canvas != null)
            {
                drawingAttributes.Height = Settings.Canvas.InkWidth;
                drawingAttributes.Width = Settings.Canvas.InkWidth;

                InkWidthSlider.Value = Settings.Canvas.InkWidth * 2;

                if (Settings.Canvas.IsShowCursor)
                {
                    ToggleSwitchShowCursor.IsOn = true;
                    inkCanvas.ForceCursor = true;
                }
            }
            else
            {
                Settings.Canvas = new Canvas();
            }

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            TextBlockVersion.Text = version.ToString();

            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;

            isLoaded = true;
        }
        string settingsFileName = "settings.json";
        bool isLoaded = false;

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

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (BorderSettings.Visibility == Visibility.Visible)
            {
                BorderSettings.Visibility = Visibility.Collapsed;
            }
            else
            {
                BorderSettings.Visibility = Visibility.Visible;
            }
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
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
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
            inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FFFFB900");

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

        bool isTouchDown = false;
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

        private void Main_Grid_TouchUp(object sender, TouchEventArgs e)
        {
        }

        private void Main_Grid_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (e.Manipulators.Count() == 0)
            {
                if (forceEraser) return;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
        }
        private void Main_Grid_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {

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
            if (!Settings.Appearance.IsTransparentButtonBackground)
            {
                ToggleSwitchTransparentButtonBackground_Toggled(ToggleSwitchTransparentButtonBackground, null);
            }
        }


        int BoundsWidth = 5;
        private void ToggleSwitchModeFinger_Toggled(object sender, RoutedEventArgs e)
        {
            if (ToggleSwitchModeFinger.IsOn)
            {
                BoundsWidth = 10;
            }
            else
            {
                BoundsWidth = 5;
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
                Process[] processes = Process.GetProcessesByName("wpp");
                if (processes.Length > 0)
                {
                    return;
                }
                //processes = Process.GetProcessesByName("wps");
                //if (processes.Length > 0)
                //{
                //    return;
                //}

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
                if (Settings.Behavior.IsShowCanvasAtNewSlideShow && Main_Grid.Background == Brushes.Transparent)
                {
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }
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

        private void Main_Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (StackPanelPPTControls.Visibility != Visibility.Visible) return;

            if (e.Key == Key.Down || e.Key == Key.PageDown || e.Key == Key.Right)
            {
                BtnPPTSlidesDown_Click(BtnPPTSlidesDown, null);
            }
            if (e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Left)
            {
                BtnPPTSlidesUp_Click(BtnPPTSlidesUp, null);
            }
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
                new Thread(new ThreadStart(() => {
                    pptApplication.SlideShowWindows[1].View.Application.SlideShowWindows[1].Activate();
                    pptApplication.SlideShowWindows[1].View.Previous();
                })).Start();
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
                new Thread(new ThreadStart(() => {
                    pptApplication.SlideShowWindows[1].View.Application.SlideShowWindows[1].Activate();
                    pptApplication.SlideShowWindows[1].View.Next();
                })).Start();
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
            new Thread(new ThreadStart(() => {
                try
                {
                    presentation.SlideShowSettings.Run();
                }
                catch { }
            })).Start();

            if (currentMode == 1)
            {
                BtnSwitch_Click(BtnSwitch, e);
            }
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


        #region Settings

        #region Behavior

        private void ToggleSwitchRunAtStartup_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            if (ToggleSwitchRunAtStartup.IsOn)
            {
                StartAutomaticallyCreate("InkCanvas");
            }
            else
            {
                StartAutomaticallyDel("InkCanvas");
            }
        }

        private void ToggleSwitchSupportPowerPoint_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Behavior.PowerPointSupport = ToggleSwitchSupportPowerPoint.IsOn;
            SaveSettingsToFile();
        }

        private void ToggleSwitchShowCanvasAtNewSlideShow_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Behavior.IsShowCanvasAtNewSlideShow = ToggleSwitchShowCanvasAtNewSlideShow.IsOn;
            SaveSettingsToFile();
        }

        #endregion

        #region Startup

        private void ToggleSwitchAutoHideCanvas_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Startup.IsAutoHideCanvas = ToggleSwitchAutoHideCanvas.IsOn;
            SaveSettingsToFile();
        }

        private void ToggleSwitchAutoEnterModeFinger_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Startup.IsAutoEnterModeFinger = ToggleSwitchAutoEnterModeFinger.IsOn;
            SaveSettingsToFile();
        }

        #endregion

        #region Appearance

        private void ToggleSwitchShowButtonExit_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Appearance.IsShowExitButton = ToggleSwitchShowButtonExit.IsOn;
            SaveSettingsToFile();

            if (ToggleSwitchShowButtonExit.IsOn)
            {
                BtnExit.Visibility = Visibility.Visible;
            }
            else
            {
                BtnExit.Visibility = Visibility.Collapsed;
            }
        }

        private void ToggleSwitchShowButtonEraser_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Appearance.IsShowEraserButton = ToggleSwitchShowButtonEraser.IsOn;
            SaveSettingsToFile();

            if (ToggleSwitchShowButtonEraser.IsOn)
            {
                BtnErase.Visibility = Visibility.Visible;
            }
            else
            {
                BtnErase.Visibility = Visibility.Collapsed;
            }
        }

        private void ToggleSwitchShowButtonHideControl_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Appearance.IsShowHideControlButton = ToggleSwitchShowButtonHideControl.IsOn;
            SaveSettingsToFile();

            if (ToggleSwitchShowButtonHideControl.IsOn)
            {
                BtnHideControl.Visibility = Visibility.Visible;
            }
            else
            {
                BtnHideControl.Visibility = Visibility.Collapsed;
            }
        }

        private void ToggleSwitchShowButtonLRSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Appearance.IsShowLRSwitchButton = ToggleSwitchShowButtonLRSwitch.IsOn;
            SaveSettingsToFile();

            if (ToggleSwitchShowButtonLRSwitch.IsOn)
            {
                BtnSwitchSide.Visibility = Visibility.Visible;
            }
            else
            {
                BtnSwitchSide.Visibility = Visibility.Collapsed;
            }
        }

        private void ToggleSwitchShowButtonModeFinger_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Appearance.IsShowModeFingerToggleSwitch = ToggleSwitchShowButtonModeFinger.IsOn;
            SaveSettingsToFile();

            if (ToggleSwitchShowButtonModeFinger.IsOn)
            {
                StackPanelModeFinger.Visibility = Visibility.Visible;
            }
            else
            {
                StackPanelModeFinger.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Canvas

        private void InkWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isLoaded) return;

            drawingAttributes.Height = ((Slider)sender).Value / 2;
            drawingAttributes.Width = ((Slider)sender).Value / 2;

            Settings.Canvas.InkWidth = ((Slider)sender).Value / 2;

            SaveSettingsToFile();
        }

        #endregion

        private void SaveSettingsToFile()
        {
            string text = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            try
            {
                File.WriteAllText(settingsFileName, text);
            }
            catch { }
        }

        private void SCManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }

        private void HyperlinkSource_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/WXRIW/Ink-Canvas");
        }

        #endregion

        public static string GetWebClient(string url)
        {
            HttpWebRequest myrq = (HttpWebRequest)WebRequest.Create(url);

            myrq.Proxy = null;
            myrq.KeepAlive = false;
            myrq.Timeout = 30 * 1000;
            myrq.Method = "Get";
            myrq.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            myrq.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 UBrowser/6.2.4098.3 Safari/537.36";

            HttpWebResponse myrp;
            try
            {
                myrp = (HttpWebResponse)myrq.GetResponse();
            }
            catch (WebException ex)
            {
                myrp = (HttpWebResponse)ex.Response;
            }

            if (myrp.StatusCode != HttpStatusCode.OK)
            {
                return "null";
            }

            using (StreamReader sr = new StreamReader(myrp.GetResponseStream()))
            {
                return sr.ReadToEnd();
            }
        }

        #region 开机自启
        /// <summary>
        /// 开机自启创建
        /// </summary>
        /// <param name="exeName">程序名称</param>
        /// <returns></returns>
        public bool StartAutomaticallyCreate(string exeName)
        {
            try
            {
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + exeName + ".lnk");
                //设置快捷方式的目标所在的位置(源程序完整路径)
                shortcut.TargetPath = System.Windows.Forms.Application.ExecutablePath;
                //应用程序的工作目录
                //当用户没有指定一个具体的目录时，快捷方式的目标应用程序将使用该属性所指定的目录来装载或保存文件。
                shortcut.WorkingDirectory = System.Environment.CurrentDirectory;
                //目标应用程序窗口类型(1.Normal window普通窗口,3.Maximized最大化窗口,7.Minimized最小化)
                shortcut.WindowStyle = 1;
                //快捷方式的描述
                shortcut.Description = exeName + "_Ink";
                //设置快捷键(如果有必要的话.)
                //shortcut.Hotkey = "CTRL+ALT+D";
                shortcut.Save();
                return true;
            }
            catch (Exception) { }
            return false;
        }
        /// <summary>
        /// 开机自启删除
        /// </summary>
        /// <param name="exeName">程序名称</param>
        /// <returns></returns>
        public bool StartAutomaticallyDel(string exeName)
        {
            try
            {
                System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + exeName + ".lnk");
                return true;
            }
            catch (Exception) { }
            return false;
        }
        #endregion

        private void Window_Closed(object sender, EventArgs e)
        {
            //if (!isAutoUpdateEnabled) return;
            //try
            //{
            //    if (OAUS.Core.VersionHelper.HasNewVersion(GetIp("ink.wxriw.cn"), 19570))
            //    {
            //        string updateExePath = AppDomain.CurrentDomain.BaseDirectory + "AutoUpdater\\AutoUpdater.exe";
            //        System.Diagnostics.Process myProcess = System.Diagnostics.Process.Start(updateExePath);
            //    }
            //}
            //catch { }
        }

        /// <summary>
        /// 传入域名返回对应的IP 
        /// </summary>
        /// <param name="domainName">域名</param>
        /// <returns></returns>
        public static string GetIp(string domainName)
        {
            domainName = domainName.Replace("http://", "").Replace("https://", "");
            IPHostEntry hostEntry = Dns.GetHostEntry(domainName);
            IPEndPoint ipEndPoint = new IPEndPoint(hostEntry.AddressList[0], 0);
            return ipEndPoint.Address.ToString();
        }

        private void inkCanvas_EditingModeChanged(object sender, RoutedEventArgs e)
        {
            if (Settings.Canvas.IsShowCursor)
            {
                if(((InkCanvas)sender).EditingMode == InkCanvasEditingMode.Ink)
                {
                    ((InkCanvas)sender).ForceCursor = true;
                }
                else
                {
                    ((InkCanvas)sender).ForceCursor = false;
                }
            }
            else
            {
                ((InkCanvas)sender).ForceCursor = false;
            }
        }

        private void ToggleSwitchTransparentButtonBackground_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Appearance.IsTransparentButtonBackground = ToggleSwitchTransparentButtonBackground.IsOn;
            if (Settings.Appearance.IsTransparentButtonBackground)
            {
                BtnExit.Background = new SolidColorBrush(StringToColor("#7F909090"));
            }
            else
            {
                if (BtnSwitchTheme.Content.ToString() == "深色")
                {
                    //Light
                    BtnExit.Background = new SolidColorBrush(StringToColor("#FFCCCCCC"));
                }
                else
                {
                    //Dark
                    BtnExit.Background = new SolidColorBrush(StringToColor("#FF555555"));
                }
            }

            SaveSettingsToFile();
        }

        private void ToggleSwitchShowCursor_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Canvas.IsShowCursor = ToggleSwitchShowCursor.IsOn;
            inkCanvas_EditingModeChanged(inkCanvas, null);

            SaveSettingsToFile();
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
