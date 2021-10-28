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
using System.Collections.Generic;
using Point = System.Windows.Point;
using System.Windows.Input.StylusPlugIns;
using MessageBox = System.Windows.MessageBox;
using System.Drawing.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Ink.AnalysisCore;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Window Initialization

        public MainWindow()
        {
            InitializeComponent();
            BorderSettings.Visibility = Visibility.Collapsed;
            StackPanelToolButtons.Visibility = Visibility.Collapsed;

            InitTimers();
        }

        #endregion

        #region Timer

        Timer timerCheckPPT = new Timer();
        Timer timerKillProcess = new Timer();

        private void InitTimers()
        {
            timerCheckPPT.Elapsed += TimerCheckPPT_Elapsed;
            timerCheckPPT.Interval = 1000;

            timerKillProcess.Elapsed += TimerKillProcess_Elapsed;
            timerKillProcess.Interval = 5000;
        }

        private void TimerKillProcess_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                string arg = "/F";
                if (Settings.Automation.IsAutoKillPptService)
                {
                    Process[] processes = Process.GetProcessesByName("PPTService");
                    if (processes.Length > 0)
                    {
                        arg += " /IM PPTService.exe";
                    }
                }
                if (Settings.Automation.IsAutoKillEasiNote)
                {
                    Process[] processes = Process.GetProcessesByName("EasiNote");
                    if (processes.Length > 0)
                    {
                        arg += " /IM EasiNote.exe";
                    }
                }
                if (arg != "/F")
                {
                    Process p = new Process();
                    p.StartInfo = new ProcessStartInfo("taskkill", arg);
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.Start();

                    if (arg.Contains("EasiNote"))
                    {
                        MessageBox.Show("检测到“希沃白板 5”，已自动关闭。");
                    }
                }
            }
            catch { }
        }

        #endregion Timer

        #region Ink Canvas Functions

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
        private void inkCanvas_EditingModeChanged(object sender, RoutedEventArgs e)
        {
            if (Settings.Canvas.IsShowCursor)
            {
                if (((InkCanvas)sender).EditingMode == InkCanvasEditingMode.Ink || drawingShapeMode != 0)
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

        #endregion Ink Canvas

        #region Hotkeys

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                KeyExit(null, null);
            }
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
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

        #endregion Hotkeys

        #region Definations and Loading

        public static Settings Settings = new Settings();
        public static string settingsFileName = "Settings.json";
        bool isLoaded = false;
        bool isAutoUpdateEnabled = false;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //new CountdownTimerWindow().ShowDialog();
            //检查
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    string VersionInfo = "";
                    if (File.Exists(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "VersionInfo.ini"))
                    {
                        VersionInfo = File.ReadAllText(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "VersionInfo.ini");
                    }
                    string Url = "http://ink.wxriw.cn:1957";
                    if (VersionInfo != "")
                    {
                        Url += "/?verinfo=" + VersionInfo;// + "&pc=" + Environment.MachineName;
                    }
                    string response = GetWebClient(Url);
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

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Version version = Assembly.GetExecutingAssembly().GetName().Version;
                            TextBlockVersion.Text = version.ToString();

                            string lastVersion = "";
                            if (!File.Exists(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Versions.ini"))
                            {
                                new WelcomeWindow().ShowDialog();
                            }
                            else
                            {
                                try
                                {
                                    lastVersion = File.ReadAllText(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "versions.ini");
                                }
                                catch { }
                                if (!lastVersion.Contains(version.ToString()))
                                {
                                    new ChangeLogWindow().ShowDialog();
                                    lastVersion += "\n" + version.ToString();
                                    File.WriteAllText(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Versions.ini", lastVersion.Trim());
                                }

                                //第二次启动时才可以进入检查版本更新模式
                                new Thread(new ThreadStart(() => {
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
                                                            Application.Current.Shutdown();
                                                        });
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                })).Start();
                            }
                        });
                    }
                }
                catch { }
            })).Start();

            loadPenCanvas();

            //加载设置
            LoadSettings();

            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;

            TextBlockVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            isLoaded = true;
        }

        private void LoadSettings(bool isStartup = true)
        {
            if (File.Exists(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + settingsFileName))
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
            else
            {
                ToggleSwitchAutoEnterModeFinger.IsOn = false;
            }
            if (Settings.Startup.IsAutoHideCanvas)
            {
                if (isStartup)
                {
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }
                ToggleSwitchAutoHideCanvas.IsOn = true;
            }
            else
            {
                ToggleSwitchAutoHideCanvas.IsOn = false;
            }

            if (Settings.Appearance.IsShowEraserButton)
            {
                BtnErase.Visibility = Visibility.Visible;
                ToggleSwitchShowButtonEraser.IsOn = true;
            }
            else
            {
                BtnErase.Visibility = Visibility.Collapsed;
                ToggleSwitchShowButtonEraser.IsOn = false;
            }
            if (Settings.Appearance.IsShowExitButton)
            {
                BtnExit.Visibility = Visibility.Visible;
                ToggleSwitchShowButtonExit.IsOn = true;
            }
            else
            {
                BtnExit.Visibility = Visibility.Collapsed;
                ToggleSwitchShowButtonExit.IsOn = false;
            }
            if (Settings.Appearance.IsShowHideControlButton)
            {
                BtnHideControl.Visibility = Visibility.Visible;
                ToggleSwitchShowButtonHideControl.IsOn = true;
            }
            else
            {
                BtnHideControl.Visibility = Visibility.Collapsed;
                ToggleSwitchShowButtonHideControl.IsOn = false;
            }
            if (Settings.Appearance.IsShowLRSwitchButton)
            {
                BtnSwitchSide.Visibility = Visibility.Visible;
                ToggleSwitchShowButtonLRSwitch.IsOn = true;
            }
            else
            {
                BtnSwitchSide.Visibility = Visibility.Collapsed;
                ToggleSwitchShowButtonLRSwitch.IsOn = false;
            }
            if (Settings.Appearance.IsShowModeFingerToggleSwitch)
            {
                StackPanelModeFinger.Visibility = Visibility.Visible;
                ToggleSwitchShowButtonModeFinger.IsOn = true;
            }
            else
            {
                StackPanelModeFinger.Visibility = Visibility.Collapsed;
                ToggleSwitchShowButtonModeFinger.IsOn = false;
            }
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

            if (Settings.Behavior.PowerPointSupport)
            {
                ToggleSwitchSupportPowerPoint.IsOn = true;
                timerCheckPPT.Start();
            }
            else
            {
                ToggleSwitchSupportPowerPoint.IsOn = false;
                timerCheckPPT.Stop();
            }
            if (Settings.Behavior.IsShowCanvasAtNewSlideShow)
            {
                ToggleSwitchShowCanvasAtNewSlideShow.IsOn = true;
            }
            else
            {
                ToggleSwitchShowCanvasAtNewSlideShow.IsOn = false;
            }

            if (Settings.Gesture == null)
            {
                Settings.Gesture = new Gesture();
            }
            if (Settings.Gesture.IsEnableTwoFingerRotation)
            {
                ToggleSwitchEnableTwoFingerRotation.IsOn = true;
            }
            else
            {
                ToggleSwitchEnableTwoFingerRotation.IsOn = false;
            }
            if (Settings.Gesture.IsEnableTwoFingerGestureInPresentationMode)
            {
                ToggleSwitchEnableTwoFingerGestureInPresentationMode.IsOn = true;
            }
            else
            {
                ToggleSwitchEnableTwoFingerGestureInPresentationMode.IsOn = false;
            }

            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\InkCanvas" + ".lnk"))
            {
                ToggleSwitchRunAtStartup.IsOn = true;
            }

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
                else
                {
                    ToggleSwitchShowCursor.IsOn = false;
                    inkCanvas.ForceCursor = false;
                }

                if (Settings.Canvas.InkStyle != 0)
                {
                    ComboBoxPenStyle.SelectedIndex = Settings.Canvas.InkStyle;
                }
            }
            else
            {
                Settings.Canvas = new Canvas();
            }

            if (Settings.Automation != null)
            {
                if (Settings.Automation.IsAutoKillEasiNote || Settings.Automation.IsAutoKillPptService)
                {
                    timerKillProcess.Start();
                }
                else
                {
                    timerKillProcess.Stop();
                }

                if (Settings.Automation.IsAutoKillEasiNote)
                {
                    ToggleSwitchAutoKillEasiNote.IsOn = true;
                }
                else
                {
                    ToggleSwitchAutoKillEasiNote.IsOn = false;
                }

                if (Settings.Automation.IsAutoKillPptService)
                {
                    ToggleSwitchAutoKillPptService.IsOn = true;
                }
                else
                {
                    ToggleSwitchAutoKillPptService.IsOn = false;
                }
            }
            else
            {
                Settings.Automation = new Automation();
            }
        }

        #endregion Definations and Loading

        #region Right Side Panel

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(System.Windows.Forms.Application.ExecutablePath);

            Application.Current.Shutdown();
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
            drawingShapeMode = 0;
            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = false;

            if (inkCanvas.Strokes.Count != 0)
            {
                int whiteboardIndex = CurrentWhiteboardIndex;
                if (currentMode == 0)
                {
                    whiteboardIndex = 0;
                }
                strokeCollections[whiteboardIndex] = inkCanvas.Strokes.Clone();

                BtnUndo.IsEnabled = true;
                BtnUndo.Visibility = Visibility.Visible;

                BtnRedo.IsEnabled = false;
                BtnRedo.Visibility = Visibility.Collapsed;
            }

            inkCanvas.Strokes.Clear();

            CancelSingleFingerDragMode();
        }

        private void BtnClear_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
        }

        private void CancelSingleFingerDragMode()
        {
            if (isSingleFingerDragMode)
            {
                BtnFingerDragMode_Click(BtnFingerDragMode, null);
            }
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

        int currentMode = 0;

        private void BtnSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (Main_Grid.Background == Brushes.Transparent)
            {
                if (currentMode == 0)
                {
                    currentMode++;
                    GridBackgroundCover.Visibility = Visibility.Visible;

                    SaveStrokes(true);
                    inkCanvas.Strokes.Clear();
                    RestoreStrokes();

                    if (BtnSwitchTheme.Content.ToString() == "浅色")
                    {
                        BtnSwitch.Content = "黑板";
                        BtnExit.Foreground = Brushes.White;
                    }
                    else
                    {
                        BtnSwitch.Content = "白板";
                        if (isPresentationHaveBlackSpace)
                        {
                            BtnExit.Foreground = Brushes.White;
                            SymbolIconBtnColorBlackContent.Foreground = Brushes.White;
                            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                        }
                        else
                        {
                            BtnExit.Foreground = Brushes.Black;
                            SymbolIconBtnColorBlackContent.Foreground = Brushes.White;
                            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                        }
                    }
                    StackPanelPPTButtons.Visibility = Visibility.Visible;
                }
                BtnHideInkCanvas_Click(BtnHideInkCanvas, e);
            }
            else
            {
                switch ((++currentMode) % 2)
                {
                    case 0: //屏幕模式
                        currentMode = 0;
                        GridBackgroundCover.Visibility = Visibility.Hidden;

                        SaveStrokes();
                        inkCanvas.Strokes.Clear();
                        RestoreStrokes(true);

                        if (BtnSwitchTheme.Content.ToString() == "浅色")
                        {
                            BtnSwitch.Content = "黑板";
                            BtnExit.Foreground = Brushes.White;
                            SymbolIconBtnColorBlackContent.Foreground = Brushes.Black;
                            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                        }
                        else
                        {
                            BtnSwitch.Content = "白板";
                            if (isPresentationHaveBlackSpace)
                            {
                                BtnExit.Foreground = Brushes.White;
                                SymbolIconBtnColorBlackContent.Foreground = Brushes.White;
                                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                            }
                            else
                            {
                                BtnExit.Foreground = Brushes.Black;
                                SymbolIconBtnColorBlackContent.Foreground = Brushes.White;
                                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                            }
                        }
                        StackPanelPPTButtons.Visibility = Visibility.Visible;
                        break;
                    case 1: //黑板或白板模式
                        currentMode = 1;
                        GridBackgroundCover.Visibility = Visibility.Visible;

                        SaveStrokes(true);
                        inkCanvas.Strokes.Clear();
                        RestoreStrokes();

                        BtnSwitch.Content = "屏幕";
                        if (BtnSwitchTheme.Content.ToString() == "浅色")
                        {
                            BtnExit.Foreground = Brushes.White;
                            SymbolIconBtnColorBlackContent.Foreground = Brushes.Black;
                            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                        }
                        else
                        {
                            BtnExit.Foreground = Brushes.Black;
                            SymbolIconBtnColorBlackContent.Foreground = Brushes.White;
                            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                        }
                        StackPanelPPTButtons.Visibility = Visibility.Collapsed;
                        break;
                }
            }

            BtnUndo.IsEnabled = false;
            BtnUndo.Visibility = Visibility.Visible;

            BtnRedo.IsEnabled = false;
            BtnRedo.Visibility = Visibility.Collapsed;
        }

        private void BtnSwitchTheme_Click(object sender, RoutedEventArgs e)
        {
            if (BtnSwitchTheme.Content.ToString() == "深色")
            {
                BtnSwitchTheme.Content = "浅色";
                if (BtnSwitch.Content.ToString() != "屏幕")
                {
                    BtnSwitch.Content = "黑板";
                }
                BtnExit.Foreground = Brushes.White;
                GridBackgroundCover.Background = new SolidColorBrush(StringToColor("#FF1A1A1A"));
                BtnColorBlack.Background = Brushes.White;
                BtnColorRed.Background = new SolidColorBrush(StringToColor("#FFFF3333"));
                BtnColorGreen.Background = new SolidColorBrush(StringToColor("#FF1ED760"));
                BtnColorYellow.Background = new SolidColorBrush(StringToColor("#FFFFC000"));
                SymbolIconBtnColorBlackContent.Foreground = Brushes.Black;
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                if (inkColor == 0)
                {
                    inkCanvas.DefaultDrawingAttributes.Color = Colors.White;
                }
                else if (inkColor == 2)
                {
                    inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FF1ED760");
                }
                else if (inkColor == 4)
                {
                    inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FFFFC000");
                }
            }
            else
            {
                BtnSwitchTheme.Content = "深色";
                if (BtnSwitch.Content.ToString() != "屏幕")
                {
                    BtnSwitch.Content = "白板";
                }
                BtnExit.Foreground = Brushes.Black;
                GridBackgroundCover.Background = new SolidColorBrush(StringToColor("#FFF2F2F2"));
                BtnColorBlack.Background = Brushes.Black;
                BtnColorRed.Background = Brushes.Red;
                BtnColorGreen.Background = new SolidColorBrush(StringToColor("#FF169141"));
                BtnColorYellow.Background = new SolidColorBrush(StringToColor("#FFF38B00"));
                SymbolIconBtnColorBlackContent.Foreground = Brushes.White;
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                if (inkColor == 0)
                {
                    inkCanvas.DefaultDrawingAttributes.Color = Colors.Black;
                }
                else if (inkColor == 2)
                {
                    inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FF169141");
                }
                else if (inkColor == 4)
                {
                    inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FFF38B00");
                }
            }
            AdjustStrokeColor();
            if (!Settings.Appearance.IsTransparentButtonBackground)
            {
                ToggleSwitchTransparentButtonBackground_Toggled(ToggleSwitchTransparentButtonBackground, null);
            }
        }

        private void AdjustStrokeColor()
        {
            if (BtnSwitchTheme.Content.ToString() == "浅色")
            {
                foreach (Stroke stroke in inkCanvas.Strokes)
                {
                    if (stroke.DrawingAttributes.Color == Colors.Black)
                    {
                        stroke.DrawingAttributes.Color = Colors.White;
                    }
                    else if (stroke.DrawingAttributes.Color == Colors.Red)
                    {
                        stroke.DrawingAttributes.Color = StringToColor("#FFFF3333");
                    }
                    else if (stroke.DrawingAttributes.Color.Equals(StringToColor("#FF169141")))
                    {
                        stroke.DrawingAttributes.Color = StringToColor("#FF1ED760");
                    }
                    else if (stroke.DrawingAttributes.Color.Equals(StringToColor("#FFF38B00")))
                    {
                        stroke.DrawingAttributes.Color = StringToColor("#FFFFC000");
                    }
                }
            }
            else
            {
                foreach (Stroke stroke in inkCanvas.Strokes)
                {
                    if (stroke.DrawingAttributes.Color == Colors.White)
                    {
                        stroke.DrawingAttributes.Color = Colors.Black;
                    }
                    else if (stroke.DrawingAttributes.Color.Equals(StringToColor("#FFFF3333")))
                    {
                        stroke.DrawingAttributes.Color = Colors.Red;
                    }
                    else if (stroke.DrawingAttributes.Color.Equals(StringToColor("#FF1ED760")))
                    {
                        stroke.DrawingAttributes.Color = StringToColor("#FF169141");
                    }
                    else if (stroke.DrawingAttributes.Color.Equals(StringToColor("#FFFFC000")))
                    {
                        stroke.DrawingAttributes.Color = StringToColor("#FFF38B00");
                    }
                }
            }
        }

        int BoundsWidth = 5;
        private void ToggleSwitchModeFinger_Toggled(object sender, RoutedEventArgs e)
        {
            if (ToggleSwitchModeFinger.IsOn)
            {
                BoundsWidth = 10; //35
            }
            else
            {
                BoundsWidth = 5; //20
            }
        }

        private void BtnHideInkCanvas_Click(object sender, RoutedEventArgs e)
        {
            if (Main_Grid.Background == Brushes.Transparent)
            {
                Main_Grid.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
                inkCanvas.Visibility = Visibility.Visible;
                GridBackgroundCoverHolder.Visibility = Visibility.Visible;
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

                if (GridBackgroundCover.Visibility == Visibility.Hidden)
                {
                    if (BtnSwitchTheme.Content.ToString() == "浅色")
                    {
                        BtnSwitch.Content = "黑板";
                    }
                    else
                    {
                        BtnSwitch.Content = "白板";
                    }
                    StackPanelPPTButtons.Visibility = Visibility.Visible;
                }
                else
                {
                    BtnSwitch.Content = "屏幕";
                    StackPanelPPTButtons.Visibility = Visibility.Collapsed;
                }

                BtnHideInkCanvas.Content = "隐藏\n画板";
            }
            else
            {
                Main_Grid.Background = Brushes.Transparent;
                inkCanvas.Visibility = Visibility.Collapsed;
                GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;
                if (currentMode != 0)
                {
                    SaveStrokes();
                    RestoreStrokes(true);
                }
                if (BtnSwitchTheme.Content.ToString() == "浅色")
                {
                    BtnSwitch.Content = "黑板";
                }
                else
                {
                    BtnSwitch.Content = "白板";
                }
                StackPanelPPTButtons.Visibility = Visibility.Visible;
                BtnHideInkCanvas.Content = "显示\n画板";
            }
        }

        private void BtnSwitchSide_Click(object sender, RoutedEventArgs e)
        {
            if (ViewBoxStackPanelMain.HorizontalAlignment == HorizontalAlignment.Right)
            {
                ViewBoxStackPanelMain.HorizontalAlignment = HorizontalAlignment.Left;
                ViewBoxStackPanelShapes.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                ViewBoxStackPanelMain.HorizontalAlignment = HorizontalAlignment.Right;
                ViewBoxStackPanelShapes.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }


        private void StackPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (((StackPanel)sender).Visibility == Visibility.Visible)
            {
                GridForLeftSideReservedSpace.Visibility = Visibility.Collapsed;
            }
            else
            {
                GridForLeftSideReservedSpace.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region Right Side Panel (Buttons - Color)

        int inkColor = 1;

        private void ColorSwitchCheck()
        {
            if (Main_Grid.Background == Brushes.Transparent)
            {
                if (currentMode == 1)
                {
                    currentMode = 0;
                    GridBackgroundCover.Visibility = Visibility.Hidden;
                }
                BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
            }
            inkCanvas.IsManipulationEnabled = true;
            drawingShapeMode = 0;
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            CancelSingleFingerDragMode();

            // 改变选中提示
            ViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
            switch (inkColor)
            {
                case 0:
                    ViewboxBtnColorBlackContent.Visibility = Visibility.Visible;
                    break;
                case 1:
                    ViewboxBtnColorRedContent.Visibility = Visibility.Visible;
                    break;
                case 2:
                    ViewboxBtnColorGreenContent.Visibility = Visibility.Visible;
                    break;
                case 3:
                    ViewboxBtnColorBlueContent.Visibility = Visibility.Visible;
                    break;
                case 4:
                    ViewboxBtnColorYellowContent.Visibility = Visibility.Visible;
                    break;
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
            if (BtnSwitchTheme.Content.ToString() == "浅色")
            {
                inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FFFF3333");
                BtnColorRed.Background = new SolidColorBrush(StringToColor("#FFFF3333"));
            }
            else
            {
                inkCanvas.DefaultDrawingAttributes.Color = Colors.Red;
                BtnColorRed.Background = Brushes.Red;
            }

            ColorSwitchCheck();
        }

        private void BtnColorGreen_Click(object sender, RoutedEventArgs e)
        {
            inkColor = 2;
            forceEraser = false;
            if (BtnSwitchTheme.Content.ToString() == "浅色")
            {
                inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FF1ED760");
                BtnColorGreen.Background = new SolidColorBrush(StringToColor("#FF1ED760"));
            }
            else
            {
                inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FF169141");
                BtnColorGreen.Background = new SolidColorBrush(StringToColor("#FF169141"));
            }

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
            if (BtnSwitchTheme.Content.ToString() == "浅色")
            {
                inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FFFFC000");
                BtnColorYellow.Background = new SolidColorBrush(StringToColor("#FFFFC000"));
            }
            else
            {
                inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FFF38B00");
                BtnColorYellow.Background = new SolidColorBrush(StringToColor("#FFF38B00"));
            }

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

        #region Touch Events

        int lastTouchDownTime = 0, lastTouchUpTime = 0;

        bool isTouchDown = false; Point iniP = new Point(0, 0);
        bool isLastTouchEraser = false;
        private void Main_Grid_TouchDown(object sender, TouchEventArgs e)
        {
            iniP = e.GetTouchPoint(inkCanvas).Position;

            double boundsWidth = e.GetTouchPoint(null).Bounds.Width;
            if (boundsWidth > BoundsWidth)
            {
                isLastTouchEraser = true;
                if (drawingShapeMode == 0 && forceEraser) return;
                if (boundsWidth > BoundsWidth * 1.7)
                {
                    inkCanvas.EraserShape = new EllipseStylusShape(boundsWidth * 1.5, boundsWidth * 1.5);
                    inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                }
                else
                {
                    inkCanvas.EraserShape = new RectangleStylusShape(8, 8);
                    //inkCanvas.EraserShape = new EllipseStylusShape(boundsWidth * 1.5, boundsWidth * 1.5);
                    inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                }
            }
            else
            {
                isLastTouchEraser = false;
                if (forceEraser) return;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
        }

        //记录触摸设备ID
        private List<int> dec = new List<int>();
        //中心点
        System.Windows.Point centerPoint;
        InkCanvasEditingMode lastInkCanvasEditingMode = InkCanvasEditingMode.Ink;
        bool isSingleFingerDragMode = false;

        private void inkCanvas_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            dec.Add(e.TouchDevice.Id);
            //设备1个的时候，记录中心点
            if (dec.Count == 1)
            {
                TouchPoint touchPoint = e.GetTouchPoint(inkCanvas);
                centerPoint = touchPoint.Position;

                //记录第一根手指点击时的 StrokeCollection
                lastTouchDownStrokeCollection = inkCanvas.Strokes.Clone();
            }
            //设备两个及两个以上，将画笔功能关闭
            if (dec.Count > 1 || isSingleFingerDragMode)
            {
                if (inkCanvas.EditingMode != InkCanvasEditingMode.None && inkCanvas.EditingMode != InkCanvasEditingMode.Select)
                {
                    lastInkCanvasEditingMode = inkCanvas.EditingMode;
                    inkCanvas.EditingMode = InkCanvasEditingMode.None;
                }
            }
        }

        private void inkCanvas_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            //手势完成后切回之前的状态
            if (dec.Count > 1)
            {
                if (inkCanvas.EditingMode == InkCanvasEditingMode.None)
                {
                    inkCanvas.EditingMode = lastInkCanvasEditingMode;
                }
            }
            dec.Remove(e.TouchDevice.Id);
            if (dec.Count == 0)
            {
                if (lastTouchDownStrokeCollection != inkCanvas.Strokes)
                {
                    int whiteboardIndex = CurrentWhiteboardIndex;
                    if (currentMode == 0)
                    {
                        whiteboardIndex = 0;
                    }
                    strokeCollections[whiteboardIndex] = lastTouchDownStrokeCollection;

                    BtnUndo.IsEnabled = true;
                    BtnUndo.Visibility = Visibility.Visible;

                    BtnRedo.IsEnabled = false;
                    BtnRedo.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void inkCanvas_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.Mode = ManipulationModes.All;
        }

        private void inkCanvas_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
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
        private MatrixTransform imageTransform;
        private void Main_Grid_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if ((dec.Count >= 2 && (Settings.Gesture.IsEnableTwoFingerGestureInPresentationMode || StackPanelPPTControls.Visibility != Visibility.Visible || StackPanelPPTButtons.Visibility == Visibility.Collapsed)) || isSingleFingerDragMode)
            {
                ManipulationDelta md = e.DeltaManipulation;
                Vector trans = md.Translation;  // 获得位移矢量
                double rotate = md.Rotation;  // 获得旋转角度
                Vector scale = md.Scale;  // 获得缩放倍数

                Matrix m = new Matrix();

                // Find center of element and then transform to get current location of center
                FrameworkElement fe = e.Source as FrameworkElement;
                Point center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
                center = m.Transform(center);  // 转换为矩阵缩放和旋转的中心点

                // Update matrix to reflect translation/rotation
                m.Translate(trans.X, trans.Y);  // 移动
                if (Settings.Gesture.IsEnableTwoFingerRotation)
                {
                    m.RotateAt(rotate, center.X, center.Y);  // 旋转
                }
                m.ScaleAt(scale.X, scale.Y, center.X, center.Y);  // 缩放

                StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
                if (strokes.Count != 0)
                {
                    foreach (Stroke stroke in strokes)
                    {
                        stroke.Transform(m, false);

                        try
                        {
                            stroke.DrawingAttributes.Width *= md.Scale.X;
                            stroke.DrawingAttributes.Height *= md.Scale.Y;
                        }
                        catch { }
                    }
                }
                else
                {
                    foreach (Stroke stroke in inkCanvas.Strokes)
                    {
                        stroke.Transform(m, false);

                        try
                        {
                            stroke.DrawingAttributes.Width *= md.Scale.X;
                            stroke.DrawingAttributes.Height *= md.Scale.Y;
                        }
                        catch { }
                    }
                }
            }
        }

        #endregion Touch Events

        #region PowerPoint

        public static Microsoft.Office.Interop.PowerPoint.Application pptApplication = null;
        public static Microsoft.Office.Interop.PowerPoint.Presentation presentation = null;
        public static Microsoft.Office.Interop.PowerPoint.Slides slides = null;
        public static Microsoft.Office.Interop.PowerPoint.Slide slide = null;
        public static int slidescount = 0;
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
        private void ToggleSwitchSupportWPS_Toggled(object sender, RoutedEventArgs e)
        {
            isWPSSupportOn = ToggleSwitchSupportWPS.IsOn;
        }

        public static bool isWPSSupportOn = false;

        public static bool IsShowingRestoreHiddenSlidesWindow = false;

        private void TimerCheckPPT_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (IsShowingRestoreHiddenSlidesWindow) return;
            try
            {
                Process[] processes = Process.GetProcessesByName("wpp");
                if (processes.Length > 0 && !isWPSSupportOn)
                {
                    return;
                }

                //使用下方提前创建 PowerPoint 实例，将导致 PowerPoint 不再有启动界面
                //pptApplication = (Microsoft.Office.Interop.PowerPoint.Application)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("91493441-5A91-11CF-8700-00AA0060263B")));
                //new ComAwareEventInfo(typeof(EApplication_Event), "SlideShowBegin").AddEventHandler(pptApplication, new EApplication_SlideShowBeginEventHandler(this.PptApplication_SlideShowBegin));
                //new ComAwareEventInfo(typeof(EApplication_Event), "SlideShowEnd").AddEventHandler(pptApplication, new EApplication_SlideShowEndEventHandler(this.PptApplication_SlideShowEnd));
                //new ComAwareEventInfo(typeof(EApplication_Event), "SlideShowNextSlide").AddEventHandler(pptApplication, new EApplication_SlideShowNextSlideEventHandler(this.PptApplication_SlideShowNextSlide));
                //ConfigHelper.Instance.IsInitApplicationSuccessful = true;

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

                //检查是否有隐藏幻灯片
                bool isHaveHiddenSlide = false;
                foreach (Slide slide in slides)
                {
                    if (slide.SlideShowTransition.Hidden == Microsoft.Office.Core.MsoTriState.msoTrue)
                    {
                        isHaveHiddenSlide = true;
                        break;
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (isHaveHiddenSlide && !IsShowingRestoreHiddenSlidesWindow)
                    {
                        IsShowingRestoreHiddenSlidesWindow = true;
                        new RestoreHiddenSlidesWindow().ShowDialog();
                    }

                    BtnPPTSlideShow.Visibility = Visibility.Visible;
                });

                //如果检测到已经开始放映，则立即进入画板模式
                if (pptApplication.SlideShowWindows.Count >= 1)
                {
                    PptApplication_SlideShowBegin(pptApplication.SlideShowWindows[1]);
                }
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
            pptApplication = null;
            timerCheckPPT.Start();
            BtnPPTSlideShow.Visibility = Visibility.Collapsed;
            BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;
        }

        bool isPresentationHaveBlackSpace = false;
        //bool isButtonBackgroundTransparent = true; //此变量仅用于保存用于幻灯片放映时的优化
        private void PptApplication_SlideShowBegin(SlideShowWindow Wn)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //调整颜色
                double screenRatio = SystemParameters.PrimaryScreenWidth / SystemParameters.PrimaryScreenHeight;
                if (Math.Abs(screenRatio - 16.0 / 9) <= 0.01)
                {
                    if (Wn.Presentation.PageSetup.SlideWidth / Wn.Presentation.PageSetup.SlideHeight < 1.65)
                    {
                        isPresentationHaveBlackSpace = true;
                        //isButtonBackgroundTransparent = ToggleSwitchTransparentButtonBackground.IsOn;

                        if (BtnSwitchTheme.Content.ToString() == "深色")
                        {
                            //Light
                            BtnExit.Foreground = Brushes.White;
                            SymbolIconBtnColorBlackContent.Foreground = Brushes.White;
                            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                            //BtnExit.Background = new SolidColorBrush(StringToColor("#AACCCCCC"));
                        }
                        else
                        {
                            //Dark
                            //BtnExit.Background = new SolidColorBrush(StringToColor("#AA555555"));
                        }
                    }
                }
                else if(screenRatio == 256 / 135)
                {

                }

                slidescount = Wn.Presentation.Slides.Count;
                memoryStreams = new MemoryStream[slidescount + 2];

                StackPanelPPTControls.Visibility = Visibility.Visible;
                BtnPPTSlideShow.Visibility = Visibility.Collapsed;
                BtnPPTSlideShowEnd.Visibility = Visibility.Visible;
                ViewBoxStackPanelMain.Margin = new Thickness(10, 10, 10, 10);

                if (Settings.Behavior.IsShowCanvasAtNewSlideShow && Main_Grid.Background == Brushes.Transparent)
                {
                    if (currentMode != 0)
                    {
                        currentMode = 0;
                        GridBackgroundCover.Visibility = Visibility.Hidden;

                        //SaveStrokes();
                        inkCanvas.Strokes.Clear();

                        if (BtnSwitchTheme.Content.ToString() == "浅色")
                        {
                            BtnSwitch.Content = "黑板";
                        }
                        else
                        {
                            BtnSwitch.Content = "白板";
                        }
                        StackPanelPPTButtons.Visibility = Visibility.Visible;
                    }
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }
                //if (GridBackgroundCover.Visibility == Visibility.Visible)
                //{
                //    SaveStrokes();
                //    currentMode = 0;
                //    GridBackgroundCover.Visibility = Visibility.Hidden;
                //}

                BtnRedo.IsEnabled = false;
                BtnRedo.Visibility = Visibility.Collapsed;

                BtnUndo.IsEnabled = false;
                BtnUndo.Visibility = Visibility.Visible;

                inkCanvas.Strokes.Clear();
            });
            previousSlideID = Wn.View.CurrentShowPosition;
        }

        private void PptApplication_SlideShowEnd(Presentation Pres)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                isPresentationHaveBlackSpace = false;

                //if (isButtonBackgroundTransparent == ToggleSwitchTransparentButtonBackground.IsOn &&
                //    isButtonBackgroundTransparent == true)
                //{
                    //if (Settings.Appearance.IsTransparentButtonBackground)
                    //{
                    //    BtnExit.Background = new SolidColorBrush(StringToColor("#7F909090"));
                    //}
                    //else
                    //{
                        if (BtnSwitchTheme.Content.ToString() == "深色")
                        {
                            //Light
                            BtnExit.Foreground = Brushes.Black;
                            SymbolIconBtnColorBlackContent.Foreground = Brushes.White;
                            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                            //BtnExit.Background = new SolidColorBrush(StringToColor("#FFCCCCCC"));
                        }
                        else
                        {
                            //Dark
                            //BtnExit.Background = new SolidColorBrush(StringToColor("#FF555555"));
                        }
                    //}
                //}

                BtnPPTSlideShow.Visibility = Visibility.Visible;
                BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;
                StackPanelPPTControls.Visibility = Visibility.Collapsed;
                ViewBoxStackPanelMain.Margin = new Thickness(10, 10, 10, 55);

                if (currentMode != 0)
                {
                    currentMode = 0;
                    GridBackgroundCover.Visibility = Visibility.Hidden;

                    //SaveStrokes();
                    inkCanvas.Strokes.Clear();
                    //RestoreStrokes(true);

                    if (BtnSwitchTheme.Content.ToString() == "浅色")
                    {
                        BtnSwitch.Content = "黑板";
                    }
                    else
                    {
                        BtnSwitch.Content = "白板";
                    }
                    StackPanelPPTButtons.Visibility = Visibility.Visible;
                }
                //if (GridBackgroundCover.Visibility == Visibility.Visible)
                //{
                //    SaveStrokes();
                //}

                BtnRedo.IsEnabled = false;
                BtnRedo.Visibility = Visibility.Collapsed;

                BtnUndo.IsEnabled = false;
                BtnUndo.Visibility = Visibility.Visible;

                inkCanvas.Strokes.Clear();

                if (Main_Grid.Background != Brushes.Transparent)
                {
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }
            });
        }

        private void Main_Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (StackPanelPPTControls.Visibility != Visibility.Visible || currentMode != 0) return;

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

                    BtnRedo.IsEnabled = false;
                    BtnRedo.Visibility = Visibility.Collapsed;

                    BtnUndo.IsEnabled = false;
                    BtnUndo.Visibility = Visibility.Visible;

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
                new Thread(new ThreadStart(() =>
                {
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
                new Thread(new ThreadStart(() =>
                {
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

        private void BtnPPTSlideShow_Click(object sender, RoutedEventArgs e)
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    presentation.SlideShowSettings.Run();
                }
                catch { }
            })).Start();
        }

        private void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    pptApplication.SlideShowWindows[1].View.Exit();
                }
                catch { }
            })).Start();
        }

        #endregion

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

            if (Settings.Behavior.PowerPointSupport)
            {
                timerCheckPPT.Start();
            }
            else
            {
                timerCheckPPT.Stop();
            }
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


        private void SideControlOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

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

        #endregion

        #region Canvas

        private void ComboBoxPenStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Canvas.InkStyle = ComboBoxPenStyle.SelectedIndex;
            SaveSettingsToFile();
        }

        private void InkWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isLoaded) return;

            drawingAttributes.Height = ((Slider)sender).Value / 2;
            drawingAttributes.Width = ((Slider)sender).Value / 2;

            Settings.Canvas.InkWidth = ((Slider)sender).Value / 2;

            SaveSettingsToFile();
        }

        #endregion

        #region Automation

        private void ToggleSwitchAutoKillPptService_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Automation.IsAutoKillPptService = ToggleSwitchAutoKillPptService.IsOn;
            SaveSettingsToFile();

            if (Settings.Automation.IsAutoKillEasiNote || Settings.Automation.IsAutoKillPptService)
            {
                timerKillProcess.Start();
            }
            else
            {
                timerKillProcess.Stop();
            }
        }

        private void ToggleSwitchAutoKillEasiNote_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Automation.IsAutoKillEasiNote = ToggleSwitchAutoKillEasiNote.IsOn;
            SaveSettingsToFile();

            if (Settings.Automation.IsAutoKillEasiNote || Settings.Automation.IsAutoKillPptService)
            {
                timerKillProcess.Start();
            }
            else
            {
                timerKillProcess.Stop();
            }
        }
        #endregion

        #region Gesture

        private void ToggleSwitchEnableTwoFingerRotation_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Gesture.IsEnableTwoFingerRotation = ToggleSwitchEnableTwoFingerRotation.IsOn;

            SaveSettingsToFile();
        }

        private void ToggleSwitchEnableTwoFingerGestureInPresentationMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Gesture.IsEnableTwoFingerGestureInPresentationMode = ToggleSwitchEnableTwoFingerGestureInPresentationMode.IsOn;

            SaveSettingsToFile();
        }

        #endregion

        #region Reset

        public static void SetSettingsToRecommendation()
        {
            bool IsAutoKillPptService = Settings.Automation.IsAutoKillPptService;
            bool IsAutoKillEasiNote = Settings.Automation.IsAutoKillEasiNote;
            Settings = new Settings();
            Settings.Appearance.IsShowEraserButton = false;
            Settings.Appearance.IsShowExitButton = false;
            Settings.Startup.IsAutoHideCanvas = true;
            Settings.Automation.IsAutoKillEasiNote = IsAutoKillEasiNote;
            Settings.Automation.IsAutoKillPptService = IsAutoKillPptService;
            Settings.Canvas.InkWidth = 2.5;
        }

        private void BtnResetToSuggestion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isLoaded = false;
                SetSettingsToRecommendation();
                SaveSettingsToFile();
                LoadSettings(false);
                isLoaded = true;

                if (ToggleSwitchRunAtStartup.IsOn == false)
                {
                    ToggleSwitchRunAtStartup.IsOn = true;
                }
            }
            catch { }
            SymbolIconResetSuggestionComplete.Visibility = Visibility.Visible;
            new Thread(new ThreadStart(() => {
                Thread.Sleep(5000);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SymbolIconResetSuggestionComplete.Visibility = Visibility.Collapsed;
                });
            })).Start();
        }

        private void BtnResetToDefault_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isLoaded = false;
                File.Delete("settings.json");
                Settings = new Settings();
                LoadSettings(false);
                isLoaded = true;
            }
            catch { }
            SymbolIconResetDefaultComplete.Visibility = Visibility.Visible;
            new Thread(new ThreadStart(() => {
                Thread.Sleep(5000);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SymbolIconResetDefaultComplete.Visibility = Visibility.Collapsed;
                });
            })).Start();
        }
        #endregion

        public static void SaveSettingsToFile()
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

        #region Left Side Panel

        #region Shape Drawing

        int drawingShapeMode = 0;

        private void BtnPen_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = false;
            drawingShapeMode = 0;
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawLine_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 1;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawArrow_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 2;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawRectangle_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 3;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawEllipse_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 4;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void inkCanvas_TouchMove(object sender, TouchEventArgs e)
        {
            if (isSingleFingerDragMode) return;
            if (drawingShapeMode != 0)
            {
                if (isLastTouchEraser)
                {
                    //if (inkCanvas.EditingMode != InkCanvasEditingMode.EraseByStroke)
                    //{
                    //    inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    //}
                    //MessageBox.Show(inkCanvas.EditingMode.ToString());
                    //if (inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint || inkCanvas.EditingMode == InkCanvasEditingMode.EraseByStroke)
                    //{

                    //}
                    //double boundsWidth = e.GetTouchPoint(null).Bounds.Width;
                    //if (boundsWidth > BoundsWidth * 1.7)
                    //{
                    //    inkCanvas.EraserShape = new EllipseStylusShape(boundsWidth * 1.5, boundsWidth * 1.5);
                    //    inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                    //}
                    //else
                    //{
                    //    inkCanvas.EraserShape = new RectangleStylusShape(8, 8);
                    //    //inkCanvas.EraserShape = new EllipseStylusShape(boundsWidth * 1.5, boundsWidth * 1.5);
                    //    inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    //}
                    return;
                }
                if (isWaitUntilNextTouchDown) return;
                if (dec.Count > 1)
                {
                    isWaitUntilNextTouchDown = true;
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStroke);
                    }
                    catch { }
                    return;
                }
                if (inkCanvas.EditingMode != InkCanvasEditingMode.None)
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.None;
                }
            }
            MouseTouchMove(e.GetTouchPoint(inkCanvas).Position);
        }

        private void MouseTouchMove(Point endP)
        {
            //System.Windows.Point endP = e.GetTouchPoint(inkCanvas).Position;
            List<System.Windows.Point> pointList;
            StylusPointCollection point;
            Stroke stroke;
            switch (drawingShapeMode)
            {
                case 1:
                    pointList = new List<System.Windows.Point>{
                        new System.Windows.Point(iniP.X, iniP.Y),
                        new System.Windows.Point(endP.X, endP.Y)
                    };
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStroke);
                    }
                    catch { }
                    lastTempStroke = stroke;
                    inkCanvas.Strokes.Add(stroke);
                    break;
                case 2:
                    double w = 30, h = 10;
                    double theta = Math.Atan2(iniP.Y - endP.Y, iniP.X - endP.X);
                    double sint = Math.Sin(theta);
                    double cost = Math.Cos(theta);

                    pointList = new List<Point>
                    {
                        new Point(iniP.X, iniP.Y),
                        new Point(endP.X , endP.Y),
                        new Point(endP.X + (w*cost - h*sint), endP.Y + (w*sint + h*cost)),
                        new Point(endP.X,endP.Y),
                        new Point(endP.X + (w*cost + h*sint), endP.Y - (h*cost - w*sint))
                    };
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStroke);
                    }
                    catch { }
                    lastTempStroke = stroke;
                    inkCanvas.Strokes.Add(stroke);
                    break;
                case 3:
                    pointList = new List<System.Windows.Point>{
                        new System.Windows.Point(iniP.X, iniP.Y),
                        new System.Windows.Point(iniP.X, endP.Y),
                        new System.Windows.Point(endP.X, endP.Y),
                        new System.Windows.Point(endP.X, iniP.Y),
                        new System.Windows.Point(iniP.X, iniP.Y)
                    };
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStroke);
                    }
                    catch { }
                    lastTempStroke = stroke;
                    inkCanvas.Strokes.Add(stroke);
                    break;
                case 4:
                    pointList = GenerateEclipseGeometry(iniP, endP);
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStroke);
                    }
                    catch { }
                    lastTempStroke = stroke;
                    inkCanvas.Strokes.Add(stroke);
                    break;
            }
        }

        private void Main_Grid_TouchUp(object sender, TouchEventArgs e)
        {
            lastTempStroke = null;
            if (dec.Count == 0)
            {
                isWaitUntilNextTouchDown = false;
            }
        }
        Stroke lastTempStroke = null; bool isWaitUntilNextTouchDown = false;
        private List<System.Windows.Point> GenerateEclipseGeometry(System.Windows.Point st, System.Windows.Point ed)
        {
            double a = 0.5 * (ed.X - st.X);
            double b = 0.5 * (ed.Y - st.Y);
            List<System.Windows.Point> pointList = new List<System.Windows.Point>();
            for (double r = 0; r <= 2 * Math.PI; r = r + 0.01)
            {
                pointList.Add(new System.Windows.Point(0.5 * (st.X + ed.X) + a * Math.Cos(r), 0.5 * (st.Y + ed.Y) + b * Math.Sin(r)));
            }
            return pointList;
        }

        bool isMouseDown = false;
        private void inkCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            iniP = e.GetPosition(inkCanvas);
            isMouseDown = true;
        }

        private void inkCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                MouseTouchMove(e.GetPosition(inkCanvas));
            }
        }

        private void inkCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            lastTempStroke = null;
            isMouseDown = false;
        }

        #endregion Shape Drawing

        #region Other Controls

        private void BtnPenWidthDecrease_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InkWidthSlider.Value -= 1;
            }
            catch { }
        }

        private void BtnPenWidthIncrease_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InkWidthSlider.Value += 1;
            }
            catch { }
        }


        private void BtnFingerDragMode_Click(object sender, RoutedEventArgs e)
        {
            if (isSingleFingerDragMode)
            {
                isSingleFingerDragMode = false;
                BtnFingerDragMode.Content = "单指\n拖动";
            }
            else
            {
                isSingleFingerDragMode = true;
                BtnFingerDragMode.Content = "多指\n拖动";
            }
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            int whiteboardIndex = CurrentWhiteboardIndex;
            if (currentMode == 0)
            {
                whiteboardIndex = 0;
            }

            StrokeCollection strokes = inkCanvas.Strokes.Clone();
            inkCanvas.Strokes = strokeCollections[whiteboardIndex].Clone();
            strokeCollections[whiteboardIndex] = strokes;

            BtnRedo.IsEnabled = true;
            BtnRedo.Visibility = Visibility.Visible;

            BtnUndo.IsEnabled = false;
            BtnUndo.Visibility = Visibility.Collapsed;
        }

        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            int whiteboardIndex = CurrentWhiteboardIndex;
            if (currentMode == 0)
            {
                whiteboardIndex = 0;
            }

            StrokeCollection strokes = inkCanvas.Strokes.Clone();
            inkCanvas.Strokes = strokeCollections[whiteboardIndex].Clone();
            strokeCollections[whiteboardIndex] = strokes;

            BtnUndo.IsEnabled = true;
            BtnUndo.Visibility = Visibility.Visible;

            BtnRedo.IsEnabled = false;
            BtnRedo.Visibility = Visibility.Collapsed;
        }

        private void Btn_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!isLoaded) return;
            try
            {
                if (((Button)sender).IsEnabled)
                {
                    ((StackPanel)((Button)sender).Content).Opacity = 1;
                }
                else
                {
                    ((StackPanel)((Button)sender).Content).Opacity = 0.2;
                }
            }
            catch { }
        }
        #endregion Other Controls

        #region Selection Gestures

        private void GridInkCanvasSelectionCover_MouseUp(object sender, MouseButtonEventArgs e)
        {
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 0;
            inkCanvas.EditingMode = InkCanvasEditingMode.Select;
            inkCanvas.IsManipulationEnabled = false;
        }

        private void inkCanvas_SelectionChanged(object sender, EventArgs e)
        {
            if (inkCanvas.GetSelectedStrokes().Count == 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

            }
            else
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Visible;
                //GridInkCanvasSelectionCover.Height = inkCanvas.GetSelectionBounds().Height;
                //GridInkCanvasSelectionCover.Width = inkCanvas.GetSelectionBounds().Width;
                //GridInkCanvasSelectionCover.Margin = new Thickness(inkCanvas.GetSelectionBounds().Left, inkCanvas.GetSelectionBounds().Top, 0, 0);
            }
        }

        private void GridInkCanvasSelectionCover_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.Mode = ManipulationModes.All;
        }

        private void GridInkCanvasSelectionCover_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {

        }

        private void GridInkCanvasSelectionCover_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (dec.Count >= 1)
            {
                ManipulationDelta md = e.DeltaManipulation;
                Vector trans = md.Translation;  // 获得位移矢量
                double rotate = md.Rotation;  // 获得旋转角度
                Vector scale = md.Scale;  // 获得缩放倍数

                Matrix m = new Matrix();

                // Find center of element and then transform to get current location of center
                FrameworkElement fe = e.Source as FrameworkElement;
                Point center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
                center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                    inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
                center = m.Transform(center);  // 转换为矩阵缩放和旋转的中心点

                // Update matrix to reflect translation/rotation
                m.Translate(trans.X, trans.Y);  // 移动
                if (Settings.Gesture.IsEnableTwoFingerRotation)
                {
                    m.RotateAt(rotate, center.X, center.Y);  // 旋转
                }
                m.ScaleAt(scale.X, scale.Y, center.X, center.Y);  // 缩放

                StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
                foreach (Stroke stroke in strokes)
                {
                    stroke.Transform(m, false);

                    try
                    {
                        stroke.DrawingAttributes.Width *= md.Scale.X;
                        stroke.DrawingAttributes.Height *= md.Scale.Y;
                    }
                    catch { }
                }
            }
        }

        private void GridInkCanvasSelectionCover_TouchDown(object sender, TouchEventArgs e)
        {
        }

        private void GridInkCanvasSelectionCover_TouchUp(object sender, TouchEventArgs e)
        {
        }

        Point lastTouchPointOnGridInkCanvasCover = new Point(0, 0);
        private void GridInkCanvasSelectionCover_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            dec.Add(e.TouchDevice.Id);
            //设备1个的时候，记录中心点
            if (dec.Count == 1)
            {
                TouchPoint touchPoint = e.GetTouchPoint(null);
                centerPoint = touchPoint.Position;
                lastTouchPointOnGridInkCanvasCover = touchPoint.Position;
            }
            ////设备两个及两个以上，将画笔功能关闭
            //if (dec.Count > 1)
            //{
            //    if (inkCanvas.EditingMode != InkCanvasEditingMode.None && inkCanvas.EditingMode != InkCanvasEditingMode.Select)
            //    {
            //        lastInkCanvasEditingMode = inkCanvas.EditingMode;
            //        inkCanvas.EditingMode = InkCanvasEditingMode.None;
            //    }
            //}
        }

        private void GridInkCanvasSelectionCover_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            if (lastTouchPointOnGridInkCanvasCover == e.GetTouchPoint(null).Position)
            {
                if (lastTouchPointOnGridInkCanvasCover.X < inkCanvas.GetSelectionBounds().Left ||
                    lastTouchPointOnGridInkCanvasCover.Y < inkCanvas.GetSelectionBounds().Top ||
                    lastTouchPointOnGridInkCanvasCover.X > inkCanvas.GetSelectionBounds().Right ||
                    lastTouchPointOnGridInkCanvasCover.Y > inkCanvas.GetSelectionBounds().Bottom)
                {
                    inkCanvas.Select(new StrokeCollection());
                }
            }
            ////手势完成后切回之前的状态
            //if (dec.Count > 1)
            //{
            //    if (inkCanvas.EditingMode == InkCanvasEditingMode.None)
            //    {
            //        inkCanvas.EditingMode = lastInkCanvasEditingMode;
            //    }
            //}
            dec.Remove(e.TouchDevice.Id);
        }

        #endregion Selection Gestures

        #endregion Left Side Panel

        #region Whiteboard Controls


        StrokeCollection[] strokeCollections = new StrokeCollection[100];
        bool[] whiteboadLastModeIsRedo = new bool[100];
        int currentStrokeCollectionIndex = 0;
        StrokeCollection lastTouchDownStrokeCollection = new StrokeCollection();

        int CurrentWhiteboardIndex = 1;
        int WhiteboardTotalCount = 1;
        MemoryStream[] WhiteboardStrokesStreams = new MemoryStream[101]; //最多99页，0用来存储非白板时的墨迹以便还原

        private void SaveStrokes(bool isBackupMain = false)
        {
            MemoryStream ms = new MemoryStream();
            inkCanvas.Strokes.Save(ms);
            ms.Position = 0;
            if (isBackupMain)
            {
                WhiteboardStrokesStreams[0] = ms;
            }
            else
            {
                WhiteboardStrokesStreams[CurrentWhiteboardIndex] = ms;
            }
        }

        private void RestoreStrokes(bool isBackupMain = false)
        {
            try
            {
                if (isBackupMain)
                {
                    if (WhiteboardStrokesStreams[0].Length > 0)
                    {
                        inkCanvas.Strokes = new System.Windows.Ink.StrokeCollection(WhiteboardStrokesStreams[0]);
                    }
                }
                else
                {
                    if (WhiteboardStrokesStreams[CurrentWhiteboardIndex].Length > 0)
                    {
                        inkCanvas.Strokes = new System.Windows.Ink.StrokeCollection(WhiteboardStrokesStreams[CurrentWhiteboardIndex]);
                    }
                }
                AdjustStrokeColor();
            }
            catch { }
        }

        private void BtnWhiteBoardSwitchPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentWhiteboardIndex <= 1) return;

            SaveStrokes();

            inkCanvas.Strokes.Clear();
            CurrentWhiteboardIndex--;

            RestoreStrokes();

            UpdateIndexInfoDisplay();
        }

        private void BtnWhiteBoardSwitchNext_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentWhiteboardIndex >= WhiteboardTotalCount) return;

            SaveStrokes();

            inkCanvas.Strokes.Clear();
            CurrentWhiteboardIndex++;

            RestoreStrokes();

            UpdateIndexInfoDisplay();
        }

        private void BtnWhiteBoardAdd_Click(object sender, RoutedEventArgs e)
        {
            SaveStrokes();
            inkCanvas.Strokes.Clear();

            WhiteboardTotalCount++;
            CurrentWhiteboardIndex++;

            if (CurrentWhiteboardIndex != WhiteboardTotalCount)
            {
                for (int i = WhiteboardTotalCount; i > CurrentWhiteboardIndex; i--)
                {
                    WhiteboardStrokesStreams[i] = WhiteboardStrokesStreams[i - 1];
                }
            }

            WhiteboardStrokesStreams[CurrentWhiteboardIndex] = new MemoryStream();

            UpdateIndexInfoDisplay();

            if (WhiteboardTotalCount >= 99) BtnWhiteBoardAdd.IsEnabled = false;
        }

        private void BtnWhiteBoardDelete_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.Strokes.Clear();

            if (CurrentWhiteboardIndex != WhiteboardTotalCount)
            {
                for (int i = CurrentWhiteboardIndex; i <= WhiteboardTotalCount; i++)
                {
                    WhiteboardStrokesStreams[i] = WhiteboardStrokesStreams[i + 1];
                }
            }
            else
            {
                CurrentWhiteboardIndex--;
            }

            WhiteboardTotalCount--;

            RestoreStrokes();

            UpdateIndexInfoDisplay();

            if (WhiteboardTotalCount < 99) BtnWhiteBoardAdd.IsEnabled = true;
        }

        private void UpdateIndexInfoDisplay()
        {
            BtnUndo.IsEnabled = false;
            BtnUndo.Visibility = Visibility.Visible;

            BtnRedo.IsEnabled = false;
            BtnRedo.Visibility = Visibility.Collapsed;

            TextBlockWhiteBoardIndexInfo.Text = string.Format("{0} / {1}", CurrentWhiteboardIndex, WhiteboardTotalCount);

            if (CurrentWhiteboardIndex == 1)
            {
                BtnWhiteBoardSwitchPrevious.IsEnabled = false;
            }
            else
            {
                BtnWhiteBoardSwitchPrevious.IsEnabled = true;
            }

            if (CurrentWhiteboardIndex == WhiteboardTotalCount)
            {
                BtnWhiteBoardSwitchNext.IsEnabled = false;
            }
            else
            {
                BtnWhiteBoardSwitchNext.IsEnabled = true;
            }

            if (WhiteboardTotalCount == 1)
            {
                BtnWhiteBoardDelete.IsEnabled = false;
            }
            else
            {
                BtnWhiteBoardDelete.IsEnabled = true;
            }
        }

        #endregion Whiteboard Controls

        #region Simulate Pen Pressure

        StrokeCollection newStrokes = new StrokeCollection();

        //此函数中的所有代码版权所有 WXRIW，在其他项目中使用前必须提前联系（wxriw@outlook.com），谢谢！
        private void inkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            try
            {
                newStrokes.Add(e.Stroke);
                if (newStrokes.Count > 4) newStrokes.RemoveAt(0);
                var result = RecognizeShape(newStrokes);

                //InkDrawingNode result = ShapeRecogniser.Instance.Recognition(strokes);
                if (result.InkDrawingNode.GetShapeName() == "Circle")
                {
                    var shape = result.InkDrawingNode.GetShape();
                    if (shape.Width > 75 && shape.Height > 75)
                    {
                        Point iniP = new Point(result.Centroid.X - shape.Width / 2, result.Centroid.Y - shape.Height / 2);
                        Point endP = new Point(result.Centroid.X + shape.Width / 2, result.Centroid.Y + shape.Height / 2);
                        var pointList = GenerateEclipseGeometry(iniP, endP);
                        var point = new StylusPointCollection(pointList);
                        var stroke = new Stroke(point)
                        {
                            DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                        };
                        inkCanvas.Strokes.Add(stroke);
                        inkCanvas.Strokes.Remove(result.InkDrawingNode.Strokes);
                        newStrokes.Remove(result.InkDrawingNode.Strokes);
                    }
                }
                else if (result.InkDrawingNode.GetShapeName().Contains("Triangle"))
                {
                    var shape = result.InkDrawingNode.GetShape();
                    var p = result.InkDrawingNode.HotPoints;
                    if ((Math.Max(Math.Max(p[0].X, p[1].X), p[2].X) >= 75 || Math.Max(Math.Max(p[0].Y, p[1].Y), p[2].Y) >= 75) && result.InkDrawingNode.HotPoints.Count == 3)
                    {
                        //纠正垂直与水平关系
                        var newPoints = FixPointsDirection(p[0], p[1]);
                        p[0] = newPoints[0];
                        p[1] = newPoints[1];
                        newPoints = FixPointsDirection(p[0], p[2]);
                        p[0] = newPoints[0];
                        p[2] = newPoints[1];
                        newPoints = FixPointsDirection(p[1], p[2]);
                        p[1] = newPoints[0];
                        p[2] = newPoints[1];

                        var pointList = p.ToList();
                        pointList.Add(p[0]);
                        var point = new StylusPointCollection(pointList);
                        var stroke = new Stroke(point)
                        {
                            DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                        };
                        inkCanvas.Strokes.Add(stroke);
                        inkCanvas.Strokes.Remove(result.InkDrawingNode.Strokes);
                        newStrokes.Remove(result.InkDrawingNode.Strokes);
                    }
                }
                Label.Visibility = Visibility.Visible;
                Label.Content = result.InkDrawingNode.GetShapeName();
            }
            catch
            {

            }

            // 检查是否是压感笔书写
            foreach (StylusPoint stylusPoint in e.Stroke.StylusPoints)
            {
                if (stylusPoint.PressureFactor!= 0.5 && stylusPoint.PressureFactor != 0)
                {
                    return;
                }
            }

            switch (Settings.Canvas.InkStyle)
            {
                case 1:
                    try
                    {
                        StylusPointCollection stylusPoints = new StylusPointCollection();
                        int n = e.Stroke.StylusPoints.Count - 1;
                        string s = "";

                        for (int i = 0; i <= n; i++)
                        {
                            double speed = GetPointSpeed(e.Stroke.StylusPoints[Math.Max(i - 1, 0)].ToPoint(), e.Stroke.StylusPoints[i].ToPoint(), e.Stroke.StylusPoints[Math.Min(i + 1, n)].ToPoint());
                            s += speed.ToString() + "\t";
                            StylusPoint point = new StylusPoint();
                            if (speed >= 0.25)
                            {
                                point.PressureFactor = (float)(0.5 - 0.3 * (Math.Min(speed, 1.5) - 0.3) / 1.2);
                            }
                            else if (speed >= 0.05)
                            {
                                point.PressureFactor = (float)0.5;
                            }
                            else
                            {
                                point.PressureFactor = (float)(0.5 + 0.4 * (0.05 - speed) / 0.05);
                            }
                            point.X = e.Stroke.StylusPoints[i].X;
                            point.Y = e.Stroke.StylusPoints[i].Y;
                            stylusPoints.Add(point);
                        }
                        //Label.Visibility = Visibility.Visible;
                        //Label.Content = s;
                        e.Stroke.StylusPoints = stylusPoints;
                    }
                    catch
                    {

                    }
                    break;
                case 0:
                    try
                    {
                        StylusPointCollection stylusPoints = new StylusPointCollection();
                        int n = e.Stroke.StylusPoints.Count - 1;
                        double pressure = 0.1;
                        int x = 10;
                        if(n >= x)
                        {
                            for (int i = 0; i < n - x; i++)
                            {
                                StylusPoint point = new StylusPoint();

                                point.PressureFactor = (float)0.5;
                                point.X = e.Stroke.StylusPoints[i].X;
                                point.Y = e.Stroke.StylusPoints[i].Y;
                                stylusPoints.Add(point);
                            }
                            for (int i = n - x; i <= n; i++)
                            {
                                StylusPoint point = new StylusPoint();

                                point.PressureFactor = (float)((0.5 - pressure) * (n - i) / x + pressure);
                                point.X = e.Stroke.StylusPoints[i].X;
                                point.Y = e.Stroke.StylusPoints[i].Y;
                                stylusPoints.Add(point);
                            }
                        }
                        else
                        {
                            for (int i = 0; i <= n; i++)
                            {
                                StylusPoint point = new StylusPoint();

                                point.PressureFactor = (float)(0.4 * (n - i) / n + pressure);
                                point.X = e.Stroke.StylusPoints[i].X;
                                point.Y = e.Stroke.StylusPoints[i].Y;
                                stylusPoints.Add(point);
                            }
                        }
                        e.Stroke.StylusPoints = stylusPoints;
                    }
                    catch
                    {

                    }
                    break;
                case 3: //根据 mode == 0 改写，目前暂未完成
                    try
                    {
                        StylusPointCollection stylusPoints = new StylusPointCollection();
                        int n = e.Stroke.StylusPoints.Count - 1;
                        double pressure = 0.1;
                        int x = 8;
                        if (lastTouchDownTime < lastTouchUpTime)
                        {
                            double k = (lastTouchUpTime - lastTouchDownTime) / (n + 1); // 每个点之间间隔 k 毫秒
                            Label.Visibility = Visibility.Visible;
                            Label.Content = k.ToString();
                            x = (int)(1000 / k); // 取 1000 ms 内的点
                        }

                        if(n >= x)
                        {
                            for (int i = 0; i < n - x; i++)
                            {
                                StylusPoint point = new StylusPoint();

                                point.PressureFactor = (float)0.5;
                                point.X = e.Stroke.StylusPoints[i].X;
                                point.Y = e.Stroke.StylusPoints[i].Y;
                                stylusPoints.Add(point);
                            }
                            for (int i = n - x; i <= n; i++)
                            {
                                StylusPoint point = new StylusPoint();

                                point.PressureFactor = (float)((0.5 - pressure) * (n - i) / x + pressure);
                                point.X = e.Stroke.StylusPoints[i].X;
                                point.Y = e.Stroke.StylusPoints[i].Y;
                                stylusPoints.Add(point);
                            }
                        }
                        else
                        {
                            for (int i = 0; i <= n; i++)
                            {
                                StylusPoint point = new StylusPoint();

                                point.PressureFactor = (float)(0.4 * (n - i) / n + pressure);
                                point.X = e.Stroke.StylusPoints[i].X;
                                point.Y = e.Stroke.StylusPoints[i].Y;
                                stylusPoints.Add(point);
                            }
                        }
                        e.Stroke.StylusPoints = stylusPoints;
                    }
                    catch
                    {

                    }
                    break;
            }
        }

        public double GetPointSpeed(Point point1, Point point2, Point point3)
        {
            return (Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) + (point1.Y - point2.Y) * (point1.Y - point2.Y))
                + Math.Sqrt((point3.X - point2.X) * (point3.X - point2.X) + (point3.Y - point2.Y) * (point3.Y - point2.Y)))
                / 20;
        }

        public Point[] FixPointsDirection(Point p1, Point p2)
        {
            if (Math.Abs(p1.X - p2.X) / Math.Abs(p1.Y - p2.Y) > 8)
            {
                //水平
                double x = Math.Abs(p1.Y - p2.Y) / 2;
                if (p1.Y > p2.Y)
                {
                    p1.Y -= x;
                    p2.Y += x;
                }
                else
                {
                    p1.Y += x;
                    p2.Y -= x;
                }
            }
            else if(Math.Abs(p1.Y - p2.Y) / Math.Abs(p1.X - p2.X) > 8)
            {
                //垂直
                double x = Math.Abs(p1.X - p2.X) / 2;
                if (p1.X > p2.X)
                {
                    p1.X -= x;
                    p2.X += x;
                }
                else
                {
                    p1.X += x;
                    p2.X -= x;
                }
            }

            return new Point[2] { p1, p2 };
        }

        #endregion Simulate Pen Pressure

        #region Functions

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
        public static bool StartAutomaticallyCreate(string exeName)
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
        public static bool StartAutomaticallyDel(string exeName)
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

        #endregion Functions

        #region Screenshot

        private void BtnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GridNotifications.Visibility = Visibility.Collapsed;

                new Thread(new ThreadStart(() => {
                    Thread.Sleep(20);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Drawing.Rectangle rc = System.Windows.Forms.SystemInformation.VirtualScreen;
                        var bitmap = new System.Drawing.Bitmap(rc.Width, rc.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                        using (System.Drawing.Graphics memoryGrahics = System.Drawing.Graphics.FromImage(bitmap))
                        {
                            memoryGrahics.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, System.Drawing.CopyPixelOperation.SourceCopy);
                        }

                        if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Ink Canvas Screenshots"))
                        {
                            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Ink Canvas Screenshots");
                        }

                        bitmap.Save(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) +
                            @"\Ink Canvas Screenshots\" + DateTime.Now.ToString("u").Replace(':', '-') + ".png", ImageFormat.Png);

                        ShowNotification("截图成功保存至 " + Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) +
                            @"\Ink Canvas Screenshots\" + DateTime.Now.ToString("u").Replace(':', '-') + ".png");
                    });
                })).Start();
            }
            catch
            {
                ShowNotification("截图保存失败");
            }
        }

        #endregion

        #region Notification

        int lastNotificationShowTime = 0;
        int notificationShowTime = 2500;

        private void ShowNotification(string notice, bool isShowImmediately = true)
        {
            lastNotificationShowTime = Environment.TickCount;

            GridNotifications.Visibility = Visibility.Visible;
            //GridNotifications.Opacity = 1;
            TextBlockNotice.Text = notice;

            new Thread(new ThreadStart(() => {
                Thread.Sleep(notificationShowTime + 200);
                if (Environment.TickCount - lastNotificationShowTime >= notificationShowTime)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        GridNotifications.Visibility = Visibility.Collapsed;
                        //DoubleAnimation daV = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.15)));
                        //GridNotifications.BeginAnimation(UIElement.OpacityProperty, daV);

                        //new Thread(new ThreadStart(() => {
                        //    Thread.Sleep(200);
                        //    Application.Current.Dispatcher.Invoke(() =>
                        //    {
                        //        if (GridNotifications.Opacity == 0)
                        //        {
                        //            GridNotifications.Visibility = Visibility.Collapsed;
                        //            GridNotifications.Opacity = 1;
                        //        }
                        //    });
                        //})).Start();
                    });
                }
            })).Start();
        }

        #endregion

        #region Tools

        private void BtnTools_Click(object sender, RoutedEventArgs e)
        {
            if (StackPanelToolButtons.Visibility == Visibility.Visible)
            {
                StackPanelToolButtons.Visibility = Visibility.Collapsed;
            }
            else
            {
                StackPanelToolButtons.Visibility = Visibility.Visible;
            }
        }

        private void BtnCountdownTimer_Click(object sender, RoutedEventArgs e)
        {
            StackPanelToolButtons.Visibility = Visibility.Collapsed;
            new CountdownTimerWindow().ShowDialog();
        }

        private void BtnRand_Click(object sender, RoutedEventArgs e)
        {
            StackPanelToolButtons.Visibility = Visibility.Collapsed;
            new RandWindow().ShowDialog();
        }

        #endregion Tools



        //识别形状
        public static ShapeRecognizeResult RecognizeShape(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0)
                return default;

            var analyzer = new InkAnalyzer();
            analyzer.AddStrokes(strokes);
            analyzer.SetStrokesType(strokes, System.Windows.Ink.StrokeType.Drawing);

            AnalysisAlternate analysisAlternate = null;
            var sfsaf = analyzer.Analyze();
            if (sfsaf.Successful)
            {
                var alternates = analyzer.GetAlternates();
                if (alternates.Count > 0)
                {
                    analysisAlternate = alternates[0];
                }
            }

            analyzer.Dispose();

            if (analysisAlternate != null && analysisAlternate.AlternateNodes.Count > 0)
            {
                var node = analysisAlternate.AlternateNodes[0] as InkDrawingNode;
                
                return new ShapeRecognizeResult(node.Centroid, node.HotPoints, analysisAlternate, node);
            }

            return default;
        }
    }

    //Recognizer 的实现

    public enum RecognizeLanguage
    {
        SimplifiedChinese = 0x0804,
        TraditionalChinese = 0x7c03,
        English = 0x0809
    }

    public class ShapeRecognizeResult
    {
        public ShapeRecognizeResult(Point centroid, PointCollection hotPoints, AnalysisAlternate analysisAlternate, InkDrawingNode node)
        {
            Centroid = centroid;
            HotPoints = hotPoints;
            AnalysisAlternate = analysisAlternate;
            InkDrawingNode = node;
        }

        public AnalysisAlternate AnalysisAlternate { get; }

        public Point Centroid { get; }

        public PointCollection HotPoints { get; }

        public InkDrawingNode InkDrawingNode { get; }
    }

    /// <summary>
    /// 图形识别类
    /// </summary>
    //public class ShapeRecogniser
    //{
    //    public InkAnalyzer _inkAnalyzer = null;

    //    private ShapeRecogniser()
    //    {
    //        this._inkAnalyzer = new InkAnalyzer
    //        {
    //            AnalysisModes = AnalysisModes.AutomaticReconciliationEnabled
    //        };
    //    }

    //    /// <summary>
    //    /// 根据笔迹集合返回图形名称字符串
    //    /// </summary>
    //    /// <param name="strokeCollection"></param>
    //    /// <returns></returns>
    //    public InkDrawingNode Recognition(StrokeCollection strokeCollection)
    //    {
    //        if (strokeCollection == null)
    //        {
    //            //MessageBox.Show("dddddd");
    //            return null;
    //        }

    //        InkDrawingNode result = null;
    //        try
    //        {
    //            this._inkAnalyzer.AddStrokes(strokeCollection);
    //            if (this._inkAnalyzer.Analyze().Successful)
    //            {
    //                result = _internalAnalyzer(this._inkAnalyzer);
    //                this._inkAnalyzer.RemoveStrokes(strokeCollection);
    //            }
    //        }
    //        catch (System.Exception ex)
    //        {
    //            //result = ex.Message;
    //            System.Diagnostics.Debug.WriteLine(ex.Message);
    //        }

    //        return result;
    //    }

    //    /// <summary>
    //    /// 实现笔迹的分析，返回图形对应的字符串
    //    /// 你在实际的应用中根据返回的字符串来生成对应的Shape
    //    /// </summary>
    //    /// <param name="ink"></param>
    //    /// <returns></returns>
    //    private InkDrawingNode _internalAnalyzer(InkAnalyzer ink)
    //    {
    //        try
    //        {
    //            ContextNodeCollection nodecollections = ink.FindNodesOfType(ContextNodeType.InkDrawing);
    //            foreach (ContextNode node in nodecollections)
    //            {
    //                InkDrawingNode drawingNode = node as InkDrawingNode;
    //                if (drawingNode != null)
    //                {
    //                    return drawingNode;//.GetShapeName();
    //                }
    //            }
    //        }
    //        catch (System.Exception ex)
    //        {
    //            System.Diagnostics.Debug.WriteLine(ex.Message);
    //        }

    //        return null;
    //    }


    //    private static ShapeRecogniser instance = null;
    //    public static ShapeRecogniser Instance
    //    {
    //        get
    //        {
    //            return instance == null ? (instance = new ShapeRecogniser()) : instance;
    //        }
    //    }
    //}
    #region Test for pen
    // A StylusPlugin that renders ink with a linear gradient brush effect.
    class CustomDynamicRenderer : DynamicRenderer
    {
        [ThreadStatic]
        static private Brush brush = null;

        [ThreadStatic]
        static private Pen pen = null;

        private Point prevPoint;

        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            // Allocate memory to store the previous point to draw from.
            prevPoint = new Point(double.NegativeInfinity, double.NegativeInfinity);
            base.OnStylusDown(rawStylusInput);
        }
        //protected override void OnDraw(System.Windows.Media.DrawingContext drawingContext, System.Windows.Input.StylusPointCollection stylusPoints, System.Windows.Media.Geometry geometry, System.Windows.Media.Brush fillBrush)
        //{


        //    ImageSource img = new BitmapImage(new Uri("pack://application:,,,/Resources/maobi.png"));

        //    //前一个点的绘制。
        //    Point prevPoint = new Point(double.NegativeInfinity,
        //                                double.NegativeInfinity);


        //    var w = Global.StrokeWidth + 15;    //输出时笔刷的实际大小


        //    Point pt = new Point(0, 0);
        //    Vector v = new Vector();            //前一个点与当前点的距离
        //    var subtractY = 0d;                 //当前点处前一点的Y偏移
        //    var subtractX = 0d;                 //当前点处前一点的X偏移
        //    var pointWidth = Global.StrokeWidth;
        //    double x = 0, y = 0;
        //    for (int i = 0; i < stylusPoints.Count; i++)
        //    {
        //        pt = (Point)stylusPoints[i];
        //        v = Point.Subtract(prevPoint, pt);

        //        Debug.WriteLine("X " + pt.X + "\t" + pt.Y);

        //        subtractY = (pt.Y - prevPoint.Y) / v.Length;    //设置stylusPoints两个点之间需要填充的XY偏移
        //        subtractX = (pt.X - prevPoint.X) / v.Length;

        //        if (w - v.Length < Global.StrokeWidth)          //控制笔刷大小
        //        {
        //            pointWidth = Global.StrokeWidth;
        //        }
        //        else
        //        {
        //            pointWidth = w - v.Length;                  //在两个点距离越大的时候，笔刷所展示的大小越小
        //        }


        //        for (double j = 0; j < v.Length; j = j + 1d)    //填充stylusPoints两个点之间
        //        {
        //            x = 0; y = 0;

        //            if (prevPoint.X == double.NegativeInfinity || prevPoint.Y == double.NegativeInfinity || double.PositiveInfinity == prevPoint.X || double.PositiveInfinity == prevPoint.Y)
        //            {
        //                y = pt.Y;
        //                x = pt.X;
        //            }
        //            else
        //            {
        //                y = prevPoint.Y + subtractY;
        //                x = prevPoint.X + subtractX;
        //            }

        //            drawingContext.DrawImage(img, new Rect(x - pointWidth / 2, y - pointWidth / 2, pointWidth, pointWidth));    //在当前点画笔刷图片
        //            prevPoint = new Point(x, y);


        //            if (double.IsNegativeInfinity(v.Length) || double.IsPositiveInfinity(v.Length))
        //            { break; }
        //        }
        //    }
        //    stylusPoints = null;
        //}
        protected override void OnDraw(DrawingContext drawingContext,
                                       StylusPointCollection stylusPoints,
                                       Geometry geometry, Brush fillBrush)
        {
            // Create a new Brush, if necessary.
            //brush ??= new LinearGradientBrush(Colors.Red, Colors.Blue, 20d);

            // Create a new Pen, if necessary.
            //pen ??= new Pen(brush, 2d);

            // Draw linear gradient ellipses between 
            // all the StylusPoints that have come in.
            for (int i = 0; i < stylusPoints.Count; i++)
            {
                Point pt = (Point)stylusPoints[i];
                Vector v = Point.Subtract(prevPoint, pt);

                // Only draw if we are at least 4 units away 
                // from the end of the last ellipse. Otherwise, 
                // we're just redrawing and wasting cycles.
                if (v.Length > 4)
                {
                    // Set the thickness of the stroke based 
                    // on how hard the user pressed.
                    double radius = stylusPoints[i].PressureFactor * 10d;
                    drawingContext.DrawEllipse(brush, pen, pt, radius, radius);
                    prevPoint = pt;
                }
            }
        }
    }
    public class Global
    {
        public static double StrokeWidth = 2.5;
    }
    public class CustomRenderingInkCanvas : InkCanvas
    {
        CustomDynamicRenderer customRenderer = new CustomDynamicRenderer();

        public CustomRenderingInkCanvas() : base()
        {
            // Use the custom dynamic renderer on the
            // custom InkCanvas.
            this.DynamicRenderer = customRenderer;
        }

        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            // Remove the original stroke and add a custom stroke.
            this.Strokes.Remove(e.Stroke);
            CustomStroke customStroke = new CustomStroke(e.Stroke.StylusPoints);
            this.Strokes.Add(customStroke);

            // Pass the custom stroke to base class' OnStrokeCollected method.
            InkCanvasStrokeCollectedEventArgs args =
                new InkCanvasStrokeCollectedEventArgs(customStroke);
            base.OnStrokeCollected(args);
        }
    }// A class for rendering custom strokes
    class CustomStroke : Stroke
    {
        Brush brush;
        Pen pen;

        public CustomStroke(StylusPointCollection stylusPoints)
            : base(stylusPoints)
        {
            // Create the Brush and Pen used for drawing.
            brush = new LinearGradientBrush(Colors.Red, Colors.Blue, 20d);
            pen = new Pen(brush, 2d);
        }
        //protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        //{


        //            ImageSource img = new BitmapImage(new Uri("pack://application:,,,/Resources/maobi.png"));

        //    //前一个点的绘制。
        //    Point prevPoint = new Point(double.NegativeInfinity,
        //                                double.NegativeInfinity);


        //    var w = Global.StrokeWidth + 15;    //输出时笔刷的实际大小


        //    Point pt = new Point(0, 0);
        //    Vector v = new Vector();            //前一个点与当前点的距离
        //    var subtractY = 0d;                 //当前点处前一点的Y偏移
        //    var subtractX = 0d;                 //当前点处前一点的X偏移
        //    var pointWidth = Global.StrokeWidth;
        //    double x = 0, y = 0;
        //    for (int i = 0; i < stylusPoints.Count; i++)
        //    {
        //        pt = (Point)stylusPoints[i];
        //        v = Point.Subtract(prevPoint, pt);

        //        Debug.WriteLine("X " + pt.X + "\t" + pt.Y);

        //        subtractY = (pt.Y - prevPoint.Y) / v.Length;    //设置stylusPoints两个点之间需要填充的XY偏移
        //        subtractX = (pt.X - prevPoint.X) / v.Length;

        //        if (w - v.Length < Global.StrokeWidth)          //控制笔刷大小
        //        {
        //            pointWidth = Global.StrokeWidth;
        //        }
        //        else
        //        {
        //            pointWidth = w - v.Length;                  //在两个点距离越大的时候，笔刷所展示的大小越小
        //        }


        //        for (double j = 0; j < v.Length; j = j + 1d)    //填充stylusPoints两个点之间
        //        {
        //            x = 0; y = 0;

        //            if (prevPoint.X == double.NegativeInfinity || prevPoint.Y == double.NegativeInfinity || double.PositiveInfinity == prevPoint.X || double.PositiveInfinity == prevPoint.Y)
        //            {
        //                y = pt.Y;
        //                x = pt.X;
        //            }
        //            else
        //            {
        //                y = prevPoint.Y + subtractY;
        //                x = prevPoint.X + subtractX;
        //            }

        //            drawingContext.DrawImage(img, new Rect(x - pointWidth / 2, y - pointWidth / 2, pointWidth, pointWidth));    //在当前点画笔刷图片
        //            prevPoint = new Point(x, y);


        //            if (double.IsNegativeInfinity(v.Length) || double.IsPositiveInfinity(v.Length))
        //            { break; }
        //        }
        //    }
        //    stylusPoints = null;
        //}
        protected override void DrawCore(DrawingContext drawingContext,
                                         DrawingAttributes drawingAttributes)
        {
            // Allocate memory to store the previous point to draw from.
            Point prevPoint = new Point(double.NegativeInfinity,
                                        double.NegativeInfinity);

            // Draw linear gradient ellipses between
            // all the StylusPoints in the Stroke.
            for (int i = 0; i < this.StylusPoints.Count; i++)
            {
                Point pt = (Point)this.StylusPoints[i];
                Vector v = Point.Subtract(prevPoint, pt);

                // Only draw if we are at least 4 units away
                // from the end of the last ellipse. Otherwise,
                // we're just redrawing and wasting cycles.
                if (v.Length > 4)
                {
                    // Set the thickness of the stroke
                    // based on how hard the user pressed.
                    double radius = this.StylusPoints[i].PressureFactor * 10d;
                    drawingContext.DrawEllipse(brush, pen, pt, radius, radius);
                    prevPoint = pt;
                }
            }
        }
    }
    #endregion
}
