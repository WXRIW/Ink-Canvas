using Ink_Canvas.Helpers;
using IWshRuntimeLibrary;
using Microsoft.Office.Interop.PowerPoint;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using ModernWpf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Application = System.Windows.Application;
using File = System.IO.File;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using Point = System.Windows.Point;
using Timer = System.Timers.Timer;

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
            BorderDrawShape.Visibility = Visibility.Collapsed;
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

            if (App.StartArgs.Contains("-b")) //-b border
            {
                AllowsTransparency = false;
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                Background = new SolidColorBrush(StringToColor("#FFF2F2F2"));
                Topmost = false;
            }

            if (!App.StartArgs.Contains("-o")) //-old ui
            {
                GroupBoxAppearance.Visibility = Visibility.Collapsed;
                ViewBoxStackPanelMain.Visibility = Visibility.Collapsed;
                ViewBoxStackPanelShapes.Visibility = Visibility.Collapsed;
                HideSubPanels();

                ViewboxFloatingBar.Margin = new Thickness((SystemParameters.WorkArea.Width - 284) / 2, SystemParameters.WorkArea.Height - 80, -2000, -200);
            }
            else
            {
                GroupBoxAppearanceNewUI.Visibility = Visibility.Collapsed;
                ViewboxFloatingBar.Visibility = Visibility.Collapsed;
                GridForRecoverOldUI.Visibility = Visibility.Collapsed;
            }

            if (File.Exists("debug.ini")) Label.Visibility = Visibility.Visible;

            InitTimers();
            timeMachine.OnRedoStateChanged += TimeMachine_OnRedoStateChanged;
            timeMachine.OnUndoStateChanged += TimeMachine_OnUndoStateChanged;
            inkCanvas.Strokes.StrokesChanged += StrokesOnStrokesChanged;

            Microsoft.Win32.SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
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
                // 希沃相关： easinote swenserver RemoteProcess EasiNote.MediaHttpService smartnote.cloud EasiUpdate smartnote EasiUpdate3 EasiUpdate3Protect SeewoP2P CefSharp.BrowserSubprocess SeewoUploadService
                string arg = "/F";
                if (Settings.Automation.IsAutoKillPptService)
                {
                    Process[] processes = Process.GetProcessesByName("PPTService");
                    if (processes.Length > 0)
                    {
                        arg += " /IM PPTService.exe";
                    }
                    processes = Process.GetProcessesByName("SeewoIwbAssistant");
                    if (processes.Length > 0)
                    {
                        arg += " /IM SeewoIwbAssistant.exe" +
                            " /IM Sia.Guard";
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

                        BtnSwitch_Click(BtnSwitch, null);
                        MessageBox.Show("“希沃白板 5”已自动关闭");
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
        //ApplicationGesture lastApplicationGesture = ApplicationGesture.AllGestures;
        DateTime lastGestureTime = DateTime.Now;
        private void InkCanvas_Gesture(object sender, InkCanvasGestureEventArgs e)
        {
            ReadOnlyCollection<GestureRecognitionResult> gestures = e.GetGestureRecognitionResults();
            try
            {
                foreach (GestureRecognitionResult gest in gestures)
                {
                    //Trace.WriteLine(string.Format("Gesture: {0}, Confidence: {1}", gest.ApplicationGesture, gest.RecognitionConfidence));
                    if (StackPanelPPTControls.Visibility == Visibility.Visible)
                    {
                        if (gest.ApplicationGesture == ApplicationGesture.Left)
                        {
                            BtnPPTSlidesDown_Click(BtnPPTSlidesDown, null);
                        }
                        if (gest.ApplicationGesture == ApplicationGesture.Right)
                        {
                            BtnPPTSlidesUp_Click(BtnPPTSlidesUp, null);
                        }
                    }
                }
            }
            catch { }
        }

        private void inkCanvas_EditingModeChanged(object sender, RoutedEventArgs e)
        {
            var inkCanvas1 = sender as InkCanvas;
            if (inkCanvas1 == null) return;
            if (Settings.Canvas.IsShowCursor)
            {
                if (inkCanvas1.EditingMode == InkCanvasEditingMode.Ink || drawingShapeMode != 0)
                {
                    inkCanvas1.ForceCursor = true;
                }
                else
                {
                    inkCanvas1.ForceCursor = false;
                }
            }
            else
            {
                inkCanvas1.ForceCursor = false;
            }
            if (inkCanvas1.EditingMode == InkCanvasEditingMode.Ink) forcePointEraser = !forcePointEraser;

            if (inkCanvas.EditingMode == InkCanvasEditingMode.Select)
            {
                SymbolIconSelect.Foreground = new SolidColorBrush(Color.FromRgb(0, 136, 255));
            }
            else
            {
                SymbolIconSelect.Foreground = new SolidColorBrush(toolBarForegroundColor);
            }
        }

        #endregion Ink Canvas

        #region Hotkeys

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (StackPanelPPTControls.Visibility != Visibility.Visible || currentMode != 0) return;
            if (e.Delta >= 120)
            {
                BtnPPTSlidesUp_Click(BtnPPTSlidesUp, null);
            }
            else if (e.Delta <= -120)
            {
                BtnPPTSlidesDown_Click(BtnPPTSlidesDown, null);
            }
        }

        private void Main_Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (StackPanelPPTControls.Visibility != Visibility.Visible || currentMode != 0) return;

            if (e.Key == Key.Down || e.Key == Key.PageDown || e.Key == Key.Right || e.Key == Key.N || e.Key == Key.Space)
            {
                BtnPPTSlidesDown_Click(BtnPPTSlidesDown, null);
            }
            if (e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Left || e.Key == Key.P)
            {
                BtnPPTSlidesUp_Click(BtnPPTSlidesUp, null);
            }
        }

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
            BtnPPTSlideShowEnd_Click(BtnPPTSlideShowEnd, null);
        }

        private void KeyChangeToDrawTool(object sender, ExecutedRoutedEventArgs e)
        {
            if (inkCanvas.Visibility == Visibility.Collapsed)
            {
                BtnHideInkCanvas_Click(sender, e);
            }
        }

        private void KeyChangeToSelect(object sender, ExecutedRoutedEventArgs e)
        {
            if (inkCanvas.Visibility == Visibility.Visible)
            {
                BtnHideInkCanvas_Click(sender, e);
            }
        }

        private void KeyChangeToEraser(object sender, ExecutedRoutedEventArgs e)
        {
            if (ImageEraserMask.Visibility == Visibility.Visible)
            {
                BtnColorRed_Click(sender, null);
            }
            else
            {
                BtnErase_Click(sender, e);
            }
        }

        private void KeyCapture(object sender, ExecutedRoutedEventArgs e)
        {
            BtnScreenshot_Click(sender, e);
        }

        private void KeyDrawLine(object sender, ExecutedRoutedEventArgs e)
        {
            BtnDrawLine_Click(lastMouseDownSender, e);
        }

        private void KeyHide(object sender, ExecutedRoutedEventArgs e)
        {
            SymbolIconEmoji_MouseUp(sender, null);
        }

        #endregion Hotkeys

        #region TimeMachine

        private enum CommitReason
        {
            UserInput,
            CodeInput,
            ShapeDrawing,
            ShapeRecognition,
            ClearingCanvas,
            Rotate
        }

        private CommitReason _currentCommitType = CommitReason.UserInput;
        private bool IsEraseByPoint => inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint;
        private StrokeCollection ReplacedStroke;
        private StrokeCollection AddedStroke;
        private StrokeCollection CuboidStrokeCollection;
        private TimeMachine timeMachine = new TimeMachine();

        private void TimeMachine_OnUndoStateChanged(bool status)
        {
            var result = status ? Visibility.Visible : Visibility.Collapsed;
            BtnUndo.Visibility = result;
            BtnUndo.IsEnabled = status;
        }

        private void TimeMachine_OnRedoStateChanged(bool status)
        {
            var result = status ? Visibility.Visible : Visibility.Collapsed;
            BtnRedo.Visibility = result;
            BtnRedo.IsEnabled = status;
        }

        private void StrokesOnStrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (_currentCommitType == CommitReason.CodeInput || _currentCommitType == CommitReason.ShapeDrawing) return;
            if (_currentCommitType == CommitReason.Rotate)
            {
                timeMachine.CommitStrokeRotateHistory(e.Removed, e.Added);
                return;
            }
            if ((e.Added.Count != 0 || e.Removed.Count != 0) && IsEraseByPoint)
            {
                if (AddedStroke == null) AddedStroke = new StrokeCollection();
                if (ReplacedStroke == null) ReplacedStroke = new StrokeCollection();
                AddedStroke.Add(e.Added);
                ReplacedStroke.Add(e.Removed);
                return;
            }
            if (e.Added.Count != 0)
            {
                if (_currentCommitType == CommitReason.ShapeRecognition)
                {
                    timeMachine.CommitStrokeShapeHistory(ReplacedStroke, e.Added);
                    ReplacedStroke = null;
                    return;
                }
                else
                {
                    timeMachine.CommitStrokeUserInputHistory(e.Added);
                    return;
                }
            }

            if (e.Removed.Count != 0)
            {
                if (_currentCommitType == CommitReason.ShapeRecognition)
                {
                    ReplacedStroke = e.Removed;
                    return;
                }
                else if (!IsEraseByPoint || _currentCommitType == CommitReason.ClearingCanvas)
                {
                    timeMachine.CommitStrokeEraseHistory(e.Removed);
                    return;
                }
            }
        }

        #endregion

        #region Definations and Loading

        public static Settings Settings = new Settings();
        public static string settingsFileName = "Settings.json";
        bool isLoaded = false;
        //bool isAutoUpdateEnabled = false;

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
                        //isAutoUpdateEnabled = true;

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
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Version version = Assembly.GetExecutingAssembly().GetName().Version;
                        TextBlockVersion.Text = version.ToString();

                        string lastVersion = "";
                        if (response.Contains("Special Version") && !File.Exists(App.RootPath + "Versions.ini"))
                        {
                            LogHelper.WriteLogToFile("Welcome Window Show Dialog", LogHelper.LogType.Event);

                            if (response.Contains("Special Version Alhua"))
                            {
                                WelcomeWindow.IsNewBuilding = true;
                            }
                            new WelcomeWindow().ShowDialog();
                        }
                        else
                        {
                            try
                            {
                                lastVersion = File.ReadAllText(App.RootPath + "Versions.ini");
                            }
                            catch { }
                            if (response.Contains("Special Version") && !lastVersion.Contains("NewWelcomeConfigured"))
                            {
                                LogHelper.WriteLogToFile("Welcome Window Show Dialog (Second time)", LogHelper.LogType.Event);

                                if (response.Contains("Special Version Alhua"))
                                {
                                    WelcomeWindow.IsNewBuilding = true;
                                }
                                new WelcomeWindow().ShowDialog();
                            }
                            try
                            {
                                lastVersion = File.ReadAllText(App.RootPath + "Versions.ini");
                            }
                            catch { }
                            if (!lastVersion.Contains(version.ToString()))
                            {
                                //LogHelper.WriteLogToFile("Change Log Window Show Dialog", LogHelper.LogType.Event);
                                //new ChangeLogWindow().ShowDialog();
                                lastVersion += "\n" + version.ToString();
                                File.WriteAllText(App.RootPath + "Versions.ini", lastVersion.Trim());
                            }
                        }
                    });
                }
                catch { }
            })).Start();

            loadPenCanvas();

            //加载设置
            LoadSettings();
            if (Environment.Is64BitProcess)
            {
                GroupBoxInkRecognition.Visibility = Visibility.Collapsed;
            }

            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            SystemEvents_UserPreferenceChanged(null, null);

            TextBlockVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LogHelper.WriteLogToFile("Ink Canvas Loaded", LogHelper.LogType.Event);
            isLoaded = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LogHelper.WriteLogToFile("Ink Canvas closing", LogHelper.LogType.Event);
            if (!CloseIsFromButton)
            {
                e.Cancel = true;
                if (MessageBox.Show("是否继续关闭 Ink Canvas 画板，这将丢失当前未保存的工作。", "Ink Canvas 画板", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                {
                    if (MessageBox.Show("真的狠心关闭 Ink Canvas 画板吗？", "Ink Canvas 画板", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.OK)
                    {
                        if (MessageBox.Show("是否取消关闭 Ink Canvas 画板？", "Ink Canvas 画板", MessageBoxButton.OKCancel, MessageBoxImage.Error) != MessageBoxResult.OK)
                        {
                            e.Cancel = false;
                        }
                    }
                }
            }
            if (e.Cancel)
            {
                LogHelper.WriteLogToFile("Ink Canvas closing cancelled", LogHelper.LogType.Event);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            LogHelper.WriteLogToFile("Ink Canvas closed", LogHelper.LogType.Event);
        }

        private void LoadSettings(bool isStartup = true)
        {
            if (File.Exists(App.RootPath + settingsFileName))
            {
                try
                {
                    string text = File.ReadAllText(App.RootPath + settingsFileName);
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
                if (isStartup)
                {
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }
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

            PptNavigationBtn.Visibility =
                Settings.PowerPointSettings.IsShowPPTNavigation ? Visibility.Visible : Visibility.Collapsed;
            ToggleSwitchShowButtonPPTNavigation.IsOn = Settings.PowerPointSettings.IsShowPPTNavigation;

            ComboBoxTheme.SelectedIndex = Settings.Appearance.Theme;
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

            if (Settings.PowerPointSettings.PowerPointSupport)
            {
                ToggleSwitchSupportPowerPoint.IsOn = true;
                timerCheckPPT.Start();
            }
            else
            {
                ToggleSwitchSupportPowerPoint.IsOn = false;
                timerCheckPPT.Stop();
            }
            if (Settings.PowerPointSettings.IsShowCanvasAtNewSlideShow)
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
            if (Settings.Gesture.IsEnableTwoFingerZoom)
            {
                ToggleSwitchEnableTwoFingerZoom.IsOn = true;
            }
            else
            {
                ToggleSwitchEnableTwoFingerZoom.IsOn = false;
            }
            if (Settings.Gesture.IsEnableTwoFingerTranslate)
            {
                ToggleSwitchEnableTwoFingerTranslate.IsOn = true;
            }
            else
            {
                ToggleSwitchEnableTwoFingerTranslate.IsOn = false;
            }
            if (Settings.Gesture.IsEnableTwoFingerRotation)
            {
                ToggleSwitchEnableTwoFingerRotation.IsOn = true;
            }
            else
            {
                ToggleSwitchEnableTwoFingerRotation.IsOn = false;
            }
            if (Settings.Gesture.IsEnableTwoFingerRotationOnSelection)
            {
                ToggleSwitchEnableTwoFingerRotationOnSelection.IsOn = true;
            }
            else
            {
                ToggleSwitchEnableTwoFingerRotationOnSelection.IsOn = false;
            }
            if (Settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode)
            {
                ToggleSwitchEnableTwoFingerGestureInPresentationMode.IsOn = true;
            }
            else
            {
                ToggleSwitchEnableTwoFingerGestureInPresentationMode.IsOn = false;
            }
            if (Settings.PowerPointSettings.IsEnableFingerGestureSlideShowControl)
            {
                ToggleSwitchEnableFingerGestureSlideShowControl.IsOn = true;
            }
            else
            {
                ToggleSwitchEnableFingerGestureSlideShowControl.IsOn = false;
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

                ComboBoxPenStyle.SelectedIndex = Settings.Canvas.InkStyle;

                ComboBoxEraserSize.SelectedIndex = Settings.Canvas.EraserSize;

                ComboBoxHyperbolaAsymptoteOption.SelectedIndex = (int)Settings.Canvas.HyperbolaAsymptoteOption;
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

                if (Settings.Automation.IsAutoClearWhenExitingWritingMode)
                {
                    ToggleSwitchClearExitingWritingMode.IsOn = true;
                }
                else
                {
                    ToggleSwitchClearExitingWritingMode.IsOn = false;
                }


                if (Settings.Automation.IsAutoSaveStrokesAtClear)
                {
                    ToggleSwitchAutoSaveStrokesAtClear.IsOn = true;
                }
                else
                {
                    ToggleSwitchAutoSaveStrokesAtClear.IsOn = false;
                }



                if (Settings.Automation.IsAutoKillPptService)
                {
                    ToggleSwitchAutoKillPptService.IsOn = true;
                }
                else
                {
                    ToggleSwitchAutoKillPptService.IsOn = false;
                }

                if (Settings.Automation.IsSaveScreenshotsInDateFolders)
                {
                    ToggleSwitchSaveScreenshotsInDateFolders.IsOn = true;
                }
                else
                {
                    ToggleSwitchSaveScreenshotsInDateFolders.IsOn = false;
                }

                if (Settings.Automation.IsAutoSaveStrokesAtScreenshot)
                {
                    ToggleSwitchAutoSaveStrokesAtScreenshot.IsOn = true;
                }
                else
                {
                    ToggleSwitchAutoSaveStrokesAtScreenshot.IsOn = false;
                }

                if (Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint)
                {
                    ToggleSwitchAutoSaveStrokesInPowerPoint.IsOn = true;
                }
                else
                {
                    ToggleSwitchAutoSaveStrokesInPowerPoint.IsOn = false;
                }

                if (Settings.PowerPointSettings.IsNotifyPreviousPage)
                {
                    ToggleSwitchNotifyPreviousPage.IsOn = true;
                }
                else
                {
                    ToggleSwitchNotifyPreviousPage.IsOn = false;
                }

                if (Settings.PowerPointSettings.IsNotifyHiddenPage)
                {
                    ToggleSwitchNotifyHiddenPage.IsOn = true;
                }
                else
                {
                    ToggleSwitchNotifyHiddenPage.IsOn = false;
                }

                if (Settings.PowerPointSettings.IsNoClearStrokeOnSelectWhenInPowerPoint)
                {
                    ToggleSwitchNoStrokeClearInPowerPoint.IsOn = true;
                }
                else
                {
                    ToggleSwitchNoStrokeClearInPowerPoint.IsOn = false;
                }

                if (Settings.PowerPointSettings.IsShowStrokeOnSelectInPowerPoint)
                {
                    ToggleSwitchShowStrokeOnSelectInPowerPoint.IsOn = true;
                }
                else
                {
                    ToggleSwitchShowStrokeOnSelectInPowerPoint.IsOn = false;
                }

                if (Settings.PowerPointSettings.IsSupportWPS)
                {
                    ToggleSwitchSupportWPS.IsOn = true;
                }
                else
                {
                    ToggleSwitchSupportWPS.IsOn = false;
                }

                SideControlMinimumAutomationSlider.Value = Settings.Automation.MinimumAutomationStrokeNumber;

                if (Settings.Canvas.HideStrokeWhenSelecting)
                {
                    ToggleSwitchHideStrokeWhenSelecting.IsOn = true;
                }
                else
                {
                    ToggleSwitchHideStrokeWhenSelecting.IsOn = false;
                }

                if (Settings.Canvas.UsingWhiteboard)
                {
                    ToggleSwitchUsingWhiteboard.IsOn = true;
                }
                else
                {
                    ToggleSwitchUsingWhiteboard.IsOn = false;
                }
                if (Settings.Canvas.UsingWhiteboard)
                {
                    BtnSwitchTheme.Content = "深色";
                    BtnSwitchTheme_Click(null, null);
                }

                switch (Settings.Canvas.EraserType)
                {
                    case 1:
                        forcePointEraser = true;
                        break;
                    case 2:
                        forcePointEraser = false;
                        break;
                }

                ComboBoxEraserType.SelectedIndex = Settings.Canvas.EraserType;

                if (Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                {
                    ToggleSwitchAutoSaveScreenShotInPowerPoint.IsOn = true;
                }
                else
                {
                    ToggleSwitchAutoSaveScreenShotInPowerPoint.IsOn = false;
                }
            }
            else
            {
                Settings.Automation = new Automation();
            }

            if (Settings.Advanced != null)
            {
                TouchMultiplierSlider.Value = Settings.Advanced.TouchMultiplier;
                if (Settings.Advanced.IsLogEnabled)
                {
                    ToggleSwitchIsLogEnabled.IsOn = true;
                }
                else
                {
                    ToggleSwitchIsLogEnabled.IsOn = false;
                }
                if (Settings.Advanced.EraserBindTouchMultiplier)
                {
                    ToggleSwitchEraserBindTouchMultiplier.IsOn = true;
                }
                else
                {
                    ToggleSwitchEraserBindTouchMultiplier.IsOn = false;
                }

                if (Settings.Advanced.IsSpecialScreen)
                {
                    ToggleSwitchIsSpecialScreen.IsOn = true;
                }
                else
                {
                    ToggleSwitchIsSpecialScreen.IsOn = false;
                }
                TouchMultiplierSlider.Visibility = ToggleSwitchIsSpecialScreen.IsOn ? Visibility.Visible : Visibility.Collapsed;

                ToggleSwitchIsQuadIR.IsOn = Settings.Advanced.IsQuadIR;
            }
            else
            {
                Settings.Advanced = new Advanced();
            }

            if (Settings.InkToShape != null)
            {
                if (Settings.InkToShape.IsInkToShapeEnabled)
                {
                    ToggleSwitchEnableInkToShape.IsOn = true;
                }
                else
                {
                    ToggleSwitchEnableInkToShape.IsOn = false;
                }
            }
            else
            {
                Settings.InkToShape = new InkToShape();
            }
        }

        #endregion Definations and Loading

        #region Right Side Panel

        public static bool CloseIsFromButton = false;
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            CloseIsFromButton = true;
            Close();
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(System.Windows.Forms.Application.ExecutablePath, "-m");

            CloseIsFromButton = true;
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
            forcePointEraser = !forcePointEraser;
            switch (Settings.Canvas.EraserType)
            {
                case 1:
                    forcePointEraser = true;
                    break;
                case 2:
                    forcePointEraser = false;
                    break;
            }
            inkCanvas.EraserShape = forcePointEraser ? new EllipseStylusShape(50, 50) : new EllipseStylusShape(5, 5);
            inkCanvas.EditingMode =
                forcePointEraser ? InkCanvasEditingMode.EraseByPoint : InkCanvasEditingMode.EraseByStroke;
            drawingShapeMode = 0;
            GeometryDrawingEraser.Brush = forcePointEraser
                ? new SolidColorBrush(Color.FromRgb(0x23, 0xA9, 0xF2))
                : new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
            ImageEraser.Visibility = Visibility.Collapsed;
            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = false;
            BorderClearInDelete.Visibility = Visibility.Collapsed;

            if (inkCanvas.Strokes.Count != 0)
            {
                int whiteboardIndex = CurrentWhiteboardIndex;
                if (currentMode == 0)
                {
                    whiteboardIndex = 0;
                }
                strokeCollections[whiteboardIndex] = inkCanvas.Strokes.Clone();

            }

            ClearStrokes(false);
            inkCanvas.Children.Clear();

            CancelSingleFingerDragMode();
        }

        private void BtnClear_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
        }

        private void CancelSingleFingerDragMode()
        {
            if (ToggleSwitchDrawShapeBorderAutoHide.IsOn)
            {
                BorderDrawShape.Visibility = Visibility.Collapsed;
            }
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            //Label.Content = "isSingleFingerDragMode=" + isSingleFingerDragMode.ToString();
            if (isSingleFingerDragMode)
            {
                BtnFingerDragMode_Click(BtnFingerDragMode, null);
            }
            isLongPressSelected = false;
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
                    GridBackgroundCover.Visibility = Visibility.Collapsed;

                    SaveStrokes(true);
                    ClearStrokes(true);
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
                        GridBackgroundCover.Visibility = Visibility.Collapsed;

                        SaveStrokes();
                        ClearStrokes(true);
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
                        ClearStrokes(true);
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
                GridBackgroundCover.Background = new SolidColorBrush(StringToColor("#FFF2F2F2"));
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
                GridBackgroundCover.Background = new SolidColorBrush(StringToColor("#FF1A1A1A"));
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
            if (!Settings.Appearance.IsTransparentButtonBackground)
            {
                ToggleSwitchTransparentButtonBackground_Toggled(ToggleSwitchTransparentButtonBackground, null);
            }
        }

        int BoundsWidth = 5;
        private void ToggleSwitchModeFinger_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitchAutoEnterModeFinger.IsOn = ToggleSwitchModeFinger.IsOn;
            if (ToggleSwitchModeFinger.IsOn)
            {
                BoundsWidth = 15; //35
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
                if (Settings.Canvas.HideStrokeWhenSelecting)
                {
                    inkCanvas.Visibility = Visibility.Visible;
                    inkCanvas.IsHitTestVisible = true;
                }
                else
                {
                    inkCanvas.IsHitTestVisible = true;
                    inkCanvas.Visibility = Visibility.Visible;
                }
                GridBackgroundCoverHolder.Visibility = Visibility.Visible;
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

                if (ImageEraserMask.Visibility == Visibility.Visible)
                    BtnColorRed_Click(sender, null);

                if (GridBackgroundCover.Visibility == Visibility.Collapsed)
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


                // Auto-clear Strokes
                // 很烦, 要重新来, 要等待截图完成再清理笔记
                if (BtnPPTSlideShowEnd.Visibility != Visibility.Visible)
                {
                    if (isLoaded && Settings.Automation.IsAutoClearWhenExitingWritingMode)
                    {
                        if (inkCanvas.Strokes.Count > 0)
                        {
                            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count >
                                Settings.Automation.MinimumAutomationStrokeNumber)
                            {
                                SaveScreenShot(true);
                            }

                            BtnClear_Click(BtnClear, null);
                        }
                    }
                    if (Settings.Canvas.HideStrokeWhenSelecting)
                        inkCanvas.Visibility = Visibility.Collapsed;
                    else
                    {
                        inkCanvas.IsHitTestVisible = false;
                        inkCanvas.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    if (isLoaded && Settings.Automation.IsAutoClearWhenExitingWritingMode && !Settings.PowerPointSettings.IsNoClearStrokeOnSelectWhenInPowerPoint)
                    {
                        if (inkCanvas.Strokes.Count > 0)
                        {
                            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count >
                                Settings.Automation.MinimumAutomationStrokeNumber)
                            {
                                SaveScreenShot(true);
                            }

                            BtnClear_Click(BtnClear, null);
                        }
                    }


                    if (Settings.PowerPointSettings.IsShowStrokeOnSelectInPowerPoint)
                    {
                        inkCanvas.Visibility = Visibility.Visible;
                        inkCanvas.IsHitTestVisible = true;
                    }
                    else
                    {
                        if (Settings.Canvas.HideStrokeWhenSelecting)
                            inkCanvas.Visibility = Visibility.Collapsed;
                        else
                        {
                            inkCanvas.IsHitTestVisible = false;
                            inkCanvas.Visibility = Visibility.Visible;
                        }
                    }
                }



                Main_Grid.Background = Brushes.Transparent;


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

            if (Main_Grid.Background == Brushes.Transparent)
            {
                StackPanelCanvasControls.Visibility = Visibility.Collapsed;
                StackPanelCanvacMain.Visibility = Visibility.Visible;
            }
            else
            {
                StackPanelCanvasControls.Visibility = Visibility.Visible;
                StackPanelCanvacMain.Visibility = Visibility.Collapsed;
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
            //EraserContainer.Background = null;
            ImageEraser.Visibility = Visibility.Visible;
            if (Main_Grid.Background == Brushes.Transparent)
            {
                if (currentMode == 1)
                {
                    currentMode = 0;
                    GridBackgroundCover.Visibility = Visibility.Collapsed;
                }
                BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
            }

            StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
            if (strokes.Count != 0)
            {
                foreach (Stroke stroke in strokes)
                {
                    try
                    {
                        stroke.DrawingAttributes.Color = inkCanvas.DefaultDrawingAttributes.Color;
                    }
                    catch { }
                }
            }
            else
            {
                inkCanvas.IsManipulationEnabled = true;
                drawingShapeMode = 0;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                CancelSingleFingerDragMode();
                forceEraser = false;

                // 改变选中提示
                ViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
                ViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
                ViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
                ViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
                ViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
                ViewboxBtnColorWhiteContent.Visibility = Visibility.Collapsed;
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
                    case 5:
                        ViewboxBtnColorWhiteContent.Visibility = Visibility.Visible;
                        break;
                }
            }

            isLongPressSelected = false;
        }

        private void BtnColorBlack_Click(object sender, RoutedEventArgs e)
        {
            inkColor = 0;
            forceEraser = false;
            inkCanvas.DefaultDrawingAttributes.Color = Colors.Black;

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

        #region Multi-Touch

        bool isInMultiTouchMode = false;
        private void BorderMultiTouchMode_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isInMultiTouchMode)
            {
                inkCanvas.StylusDown -= MainWindow_StylusDown;
                inkCanvas.StylusMove -= MainWindow_StylusMove;
                inkCanvas.StylusUp -= MainWindow_StylusUp;
                inkCanvas.TouchDown -= MainWindow_TouchDown;
                inkCanvas.TouchDown += Main_Grid_TouchDown;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                inkCanvas.Children.Clear();
                isInMultiTouchMode = false;
                SymbolIconMultiTouchMode.Symbol = ModernWpf.Controls.Symbol.People;
            }
            else
            {
                inkCanvas.StylusDown += MainWindow_StylusDown;
                inkCanvas.StylusMove += MainWindow_StylusMove;
                inkCanvas.StylusUp += MainWindow_StylusUp;
                inkCanvas.TouchDown -= Main_Grid_TouchDown;
                inkCanvas.TouchDown += MainWindow_TouchDown;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.Children.Clear();
                isInMultiTouchMode = true;
                SymbolIconMultiTouchMode.Symbol = ModernWpf.Controls.Symbol.Contact;
            }
        }

        private void MainWindow_TouchDown(object sender, TouchEventArgs e)
        {
            double boundWidth = e.GetTouchPoint(null).Bounds.Width;
            if (boundWidth > 20)
            {
                inkCanvas.EraserShape = new EllipseStylusShape(boundWidth, boundWidth);
                TouchDownPointsList[e.TouchDevice.Id] = InkCanvasEditingMode.EraseByPoint;
                inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            }
            else
            {
                TouchDownPointsList[e.TouchDevice.Id] = InkCanvasEditingMode.None;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
            }
        }

        private void MainWindow_StylusDown(object sender, StylusDownEventArgs e)
        {
            TouchDownPointsList[e.StylusDevice.Id] = InkCanvasEditingMode.None;
        }

        private void MainWindow_StylusUp(object sender, StylusEventArgs e)
        {
            try
            {
                inkCanvas.Strokes.Add(GetStrokeVisual(e.StylusDevice.Id).Stroke);
                inkCanvas.Children.Remove(GetVisualCanvas(e.StylusDevice.Id));

                inkCanvas_StrokeCollected(inkCanvas, new InkCanvasStrokeCollectedEventArgs(GetStrokeVisual(e.StylusDevice.Id).Stroke));
            }
            catch (Exception ex)
            {
                Label.Content = ex.ToString();
            }
            try
            {
                StrokeVisualList.Remove(e.StylusDevice.Id);
                VisualCanvasList.Remove(e.StylusDevice.Id);
                TouchDownPointsList.Remove(e.StylusDevice.Id);
                if (StrokeVisualList.Count == 0 || VisualCanvasList.Count == 0 || TouchDownPointsList.Count == 0)
                {
                    inkCanvas.Children.Clear();
                    StrokeVisualList.Clear();
                    VisualCanvasList.Clear();
                    TouchDownPointsList.Clear();
                }
            }
            catch { }
        }

        private void MainWindow_StylusMove(object sender, StylusEventArgs e)
        {
            try
            {
                if (GetTouchDownPointsList(e.StylusDevice.Id) != InkCanvasEditingMode.None) return;
                try
                {
                    if (e.StylusDevice.StylusButtons[1].StylusButtonState == StylusButtonState.Down) return;
                }
                catch { }
                var strokeVisual = GetStrokeVisual(e.StylusDevice.Id);
                var stylusPointCollection = e.GetStylusPoints(this);
                foreach (var stylusPoint in stylusPointCollection)
                {
                    strokeVisual.Add(new StylusPoint(stylusPoint.X, stylusPoint.Y, stylusPoint.PressureFactor));
                }

                strokeVisual.Redraw();
            }
            catch { }
        }

        private StrokeVisual GetStrokeVisual(int id)
        {
            if (StrokeVisualList.TryGetValue(id, out var visual))
            {
                return visual;
            }

            var strokeVisual = new StrokeVisual(inkCanvas.DefaultDrawingAttributes.Clone());
            StrokeVisualList[id] = strokeVisual;
            StrokeVisualList[id] = strokeVisual;
            var visualCanvas = new VisualCanvas(strokeVisual);
            VisualCanvasList[id] = visualCanvas;
            inkCanvas.Children.Add(visualCanvas);

            return strokeVisual;
        }

        private VisualCanvas GetVisualCanvas(int id)
        {
            if (VisualCanvasList.TryGetValue(id, out var visualCanvas))
            {
                return visualCanvas;
            }
            return null;
        }

        private InkCanvasEditingMode GetTouchDownPointsList(int id)
        {
            if (TouchDownPointsList.TryGetValue(id, out var inkCanvasEditingMode))
            {
                return inkCanvasEditingMode;
            }
            return inkCanvas.EditingMode;
        }

        private Dictionary<int, InkCanvasEditingMode> TouchDownPointsList { get; } = new Dictionary<int, InkCanvasEditingMode>();
        private Dictionary<int, StrokeVisual> StrokeVisualList { get; } = new Dictionary<int, StrokeVisual>();
        private Dictionary<int, VisualCanvas> VisualCanvasList { get; } = new Dictionary<int, VisualCanvas>();

        #endregion

        int lastTouchDownTime = 0, lastTouchUpTime = 0;

        Point iniP = new Point(0, 0);
        bool isLastTouchEraser = false;
        private bool forcePointEraser = true;

        private void Main_Grid_TouchDown(object sender, TouchEventArgs e)
        {
            BorderClearInDelete.Visibility = Visibility.Collapsed;
            if (ToggleSwitchDrawShapeBorderAutoHide.IsOn)
            {
                BorderDrawShape.Visibility = Visibility.Collapsed;
            }

            if (NeedUpdateIniP())
            {
                iniP = e.GetTouchPoint(inkCanvas).Position;
            }
            if (drawingShapeMode == 9 && isFirstTouchCuboid == false)
            {
                MouseTouchMove(iniP);
            }
            inkCanvas.Opacity = 1;
            double boundsWidth = GetTouchBoundWidth(e);
            var eraserMultiplier = 1d;
            if (!Settings.Advanced.EraserBindTouchMultiplier && Settings.Advanced.IsSpecialScreen) eraserMultiplier = 1 / Settings.Advanced.TouchMultiplier;
            if (boundsWidth > BoundsWidth)
            {
                isLastTouchEraser = true;
                if (drawingShapeMode == 0 && forceEraser) return;
                if (boundsWidth > BoundsWidth * 2.5)
                {
                    double k = 1;
                    switch (Settings.Canvas.EraserSize)
                    {
                        case 0:
                            k = 0.5;
                            break;
                        case 1:
                            k = 0.8;
                            break;
                        case 3:
                            k = 1.25;
                            break;
                        case 4:
                            k = 1.8;
                            break;
                    }
                    inkCanvas.EraserShape = new EllipseStylusShape(boundsWidth * 1.5 * k * eraserMultiplier, boundsWidth * 1.5 * k * eraserMultiplier);
                    inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                }
                else
                {
                    if (StackPanelPPTControls.Visibility == Visibility.Visible && inkCanvas.Strokes.Count == 0 && Settings.PowerPointSettings.IsEnableFingerGestureSlideShowControl)
                    {
                        isLastTouchEraser = false;
                        inkCanvas.EditingMode = InkCanvasEditingMode.GestureOnly;
                        inkCanvas.Opacity = 0.1;
                    }
                    else
                    {
                        //inkCanvas.EraserShape = new RectangleStylusShape(8, 8); //old old
                        //inkCanvas.EraserShape = forcePointEraser ? new EllipseStylusShape(50, 50) : new EllipseStylusShape(5, 5); //last
                        //inkCanvas.EraserShape = new EllipseStylusShape(boundsWidth * 1.5, boundsWidth * 1.5); //old old
                        //inkCanvas.EditingMode = forcePointEraser ? InkCanvasEditingMode.EraseByPoint : InkCanvasEditingMode.EraseByStroke; //last
                        inkCanvas.EraserShape = new EllipseStylusShape(5, 5);
                        inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    }
                }
            }
            else
            {
                isLastTouchEraser = false;
                inkCanvas.EraserShape = forcePointEraser ? new EllipseStylusShape(50, 50) : new EllipseStylusShape(5, 5);
                if (forceEraser) return;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
        }

        public double GetTouchBoundWidth(TouchEventArgs e)
        {
            var args = e.GetTouchPoint(null).Bounds;
            double value;
            if (!Settings.Advanced.IsQuadIR) value = args.Width;
            else value = Math.Sqrt(args.Width * args.Height); //四边红外
            if (Settings.Advanced.IsSpecialScreen) value *= Settings.Advanced.TouchMultiplier;
            return value;
        }

        //记录触摸设备ID
        private List<int> dec = new List<int>();
        //中心点
        System.Windows.Point centerPoint;
        InkCanvasEditingMode lastInkCanvasEditingMode = InkCanvasEditingMode.Ink;
        bool isSingleFingerDragMode = false;

        //防止衣服误触造成的墨迹消失

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
            if (dec.Count > 1 || isSingleFingerDragMode || !Settings.Gesture.IsEnableTwoFingerGesture)
            {
                if (isInMultiTouchMode || !Settings.Gesture.IsEnableTwoFingerGesture) return;
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
            inkCanvas.Opacity = 1;
            if (dec.Count == 0)
            {
                if (lastTouchDownStrokeCollection.Count() != inkCanvas.Strokes.Count() &&
                    !(drawingShapeMode == 9 && !isFirstTouchCuboid))
                {
                    int whiteboardIndex = CurrentWhiteboardIndex;
                    if (currentMode == 0)
                    {
                        whiteboardIndex = 0;
                    }
                    strokeCollections[whiteboardIndex] = lastTouchDownStrokeCollection;
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

        private void Main_Grid_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (isInMultiTouchMode || !Settings.Gesture.IsEnableTwoFingerGesture) return;
            if ((dec.Count >= 2 && (Settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode || StackPanelPPTControls.Visibility != Visibility.Visible || StackPanelPPTButtons.Visibility == Visibility.Collapsed)) || isSingleFingerDragMode)
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
                if (Settings.Gesture.IsEnableTwoFingerTranslate)
                    m.Translate(trans.X, trans.Y);  // 移动
                if (Settings.Gesture.IsEnableTwoFingerRotation)
                    m.RotateAt(rotate, center.X, center.Y);  // 旋转
                if (Settings.Gesture.IsEnableTwoFingerZoom)
                    m.ScaleAt(scale.X, scale.Y, center.X, center.Y);  // 缩放

                StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
                if (strokes.Count != 0)
                {
                    foreach (Stroke stroke in strokes)
                    {
                        stroke.Transform(m, false);

                        foreach (Circle circle in circles)
                        {
                            if (stroke == circle.Stroke)
                            {
                                circle.R = GetDistance(circle.Stroke.StylusPoints[0].ToPoint(), circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].ToPoint()) / 2;
                                circle.Centroid = new Point((circle.Stroke.StylusPoints[0].X + circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].X) / 2,
                                                            (circle.Stroke.StylusPoints[0].Y + circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].Y) / 2);
                                break;
                            }
                        }

                        if (Settings.Gesture.IsEnableTwoFingerZoom)
                        {
                            try
                            {
                                stroke.DrawingAttributes.Width *= md.Scale.X;
                                stroke.DrawingAttributes.Height *= md.Scale.Y;
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    foreach (Stroke stroke in inkCanvas.Strokes)
                    {
                        stroke.Transform(m, false);

                        if (Settings.Gesture.IsEnableTwoFingerZoom)
                        {
                            try
                            {
                                stroke.DrawingAttributes.Width *= md.Scale.X;
                                stroke.DrawingAttributes.Height *= md.Scale.Y;
                            }
                            catch { }
                        }
                    }
                    foreach (Circle circle in circles)
                    {
                        circle.R = GetDistance(circle.Stroke.StylusPoints[0].ToPoint(), circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].ToPoint()) / 2;
                        circle.Centroid = new Point((circle.Stroke.StylusPoints[0].X + circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].X) / 2,
                                                    (circle.Stroke.StylusPoints[0].Y + circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].Y) / 2);
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
            if (!isLoaded) return;

            Settings.PowerPointSettings.IsSupportWPS = ToggleSwitchSupportWPS.IsOn;
            SaveSettingsToFile();
        }

        public static bool isWPSSupportOn => Settings.PowerPointSettings.IsSupportWPS;

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

                if (pptApplication == null) return;
                //BtnCheckPPT.Visibility = Visibility.Collapsed;

                // 跳转到上次播放页
                if (Settings.PowerPointSettings.IsNotifyPreviousPage)
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        string defaultFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                                                   @"\Ink Canvas Strokes\Auto Saved\Presentations\";
                        string folderPath = defaultFolderPath + presentation.Name + "_" + presentation.Slides.Count;
                        if (File.Exists(folderPath + "/Position"))
                        {
                            if (int.TryParse(File.ReadAllText(folderPath + "/Position"), out var page))
                            {
                                if (page <= 0) return;
                                new YesOrNoNotificationWindow($"上次播放到了第 {page} 页, 是否立即跳转", () =>
                                {
                                    if (pptApplication.SlideShowWindows.Count >= 1)
                                    {
                                        // 如果已经播放了的话, 跳转
                                        presentation.SlideShowWindow.View.GotoSlide(page);
                                    }
                                    else
                                    {
                                        presentation.Windows[1].View.GotoSlide(page);
                                    }
                                }).ShowDialog();
                            }
                        }
                    }, DispatcherPriority.Normal);


                //检查是否有隐藏幻灯片
                if (Settings.PowerPointSettings.IsNotifyHiddenPage)
                {
                    bool isHaveHiddenSlide = false;
                    foreach (Slide slide in slides)
                    {
                        if (slide.SlideShowTransition.Hidden == Microsoft.Office.Core.MsoTriState.msoTrue)
                        {
                            isHaveHiddenSlide = true;
                            break;
                        }
                    }

                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (isHaveHiddenSlide && !IsShowingRestoreHiddenSlidesWindow)
                        {
                            IsShowingRestoreHiddenSlidesWindow = true;
                            new YesOrNoNotificationWindow("检测到此演示文档中包含隐藏的幻灯片，是否取消隐藏？",
                                () =>
                                {
                                    foreach (Slide slide in slides)
                                    {
                                        if (slide.SlideShowTransition.Hidden ==
                                            Microsoft.Office.Core.MsoTriState.msoTrue)
                                        {
                                            slide.SlideShowTransition.Hidden =
                                                Microsoft.Office.Core.MsoTriState.msoFalse;
                                        }
                                    }
                                }).ShowDialog();
                        }



                        BtnPPTSlideShow.Visibility = Visibility.Visible;
                    }, DispatcherPriority.Normal);
                }

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
            pptApplication.PresentationClose -= PptApplication_PresentationClose;
            pptApplication.SlideShowBegin -= PptApplication_SlideShowBegin;
            pptApplication.SlideShowNextSlide -= PptApplication_SlideShowNextSlide;
            pptApplication.SlideShowEnd -= PptApplication_SlideShowEnd;
            pptApplication = null;
            timerCheckPPT.Start();
            Application.Current.Dispatcher.Invoke(() =>
            {
                BtnPPTSlideShow.Visibility = Visibility.Collapsed;
                BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;
            });
        }

        bool isPresentationHaveBlackSpace = false;


        private string pptName = null;
        //bool isButtonBackgroundTransparent = true; //此变量仅用于保存用于幻灯片放映时的优化
        private void PptApplication_SlideShowBegin(SlideShowWindow Wn)
        {
            LogHelper.WriteLogToFile("PowerPoint Application Slide Show Begin", LogHelper.LogType.Event);
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 退出画板模式
                BtnSwitch_Click(null, null);
                //调整颜色
                double screenRatio = SystemParameters.PrimaryScreenWidth / SystemParameters.PrimaryScreenHeight;
                if (Math.Abs(screenRatio - 16.0 / 9) <= -0.01)
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
                else if (screenRatio == -256 / 135)
                {

                }

                slidescount = Wn.Presentation.Slides.Count;
                previousSlideID = 0;
                memoryStreams = new MemoryStream[slidescount + 2];

                pptName = Wn.Presentation.Name;
                LogHelper.NewLog("Name: " + Wn.Presentation.Name);
                LogHelper.NewLog("Slides Count: " + slidescount.ToString());

                //检查是否有已有墨迹，并加载
                if (Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint)
                {
                    string defaultFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Ink Canvas Strokes\Auto Saved\Presentations\";
                    if (Directory.Exists(defaultFolderPath + Wn.Presentation.Name + "_" + Wn.Presentation.Slides.Count))
                    {
                        LogHelper.WriteLogToFile("Found saved strokes", LogHelper.LogType.Trace);
                        FileInfo[] files = new DirectoryInfo(defaultFolderPath + Wn.Presentation.Name + "_" + Wn.Presentation.Slides.Count).GetFiles();
                        int count = 0;
                        foreach (FileInfo file in files)
                        {
                            int i = -1;
                            try
                            {
                                i = int.Parse(System.IO.Path.GetFileNameWithoutExtension(file.Name));
                                //var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
                                //MemoryStream ms = new MemoryStream(File.ReadAllBytes(file.FullName));
                                //new StrokeCollection(fs).Save(ms);
                                //ms.Position = 0;
                                memoryStreams[i] = new MemoryStream(File.ReadAllBytes(file.FullName));
                                memoryStreams[i].Position = 0;
                                count++;
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLogToFile(string.Format("Failed to load strokes on Slide {0}\n{1}", i, ex.ToString()), LogHelper.LogType.Error);
                            }
                        }
                        LogHelper.WriteLogToFile(string.Format("Loaded {0} saved strokes", count.ToString()));
                    }
                }

                pointDesktop = new Point(ViewboxFloatingBar.Margin.Left, ViewboxFloatingBar.Margin.Top);
                pointPPT = new Point(-1, -1);

                StackPanelPPTControls.Visibility = Visibility.Visible;
                BtnPPTSlideShow.Visibility = Visibility.Collapsed;
                BtnPPTSlideShowEnd.Visibility = Visibility.Visible;
                ViewBoxStackPanelMain.Margin = new Thickness(10, 10, 10, 10);

                if (Settings.PowerPointSettings.IsShowCanvasAtNewSlideShow && Main_Grid.Background == Brushes.Transparent)
                {
                    if (currentMode != 0)
                    {
                        currentMode = 0;
                        GridBackgroundCover.Visibility = Visibility.Collapsed;

                        //SaveStrokes();
                        ClearStrokes(true);

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

                ClearStrokes(true);

                BorderFloatingBarMainControls.Visibility = Visibility.Visible;
                BorderPenColorRed_MouseUp(BorderPenColorRed, null);

                if (Settings.PowerPointSettings.IsShowCanvasAtNewSlideShow == false)
                {
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }

                isEnteredSlideShowEndEvent = false;
                PptNavigationTextBlock.Text = $"{Wn.View.CurrentShowPosition}/{Wn.Presentation.Slides.Count}";
                LogHelper.NewLog("PowerPoint Slide Show Loading process complete");

                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(100);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewboxFloatingBar.Margin = new Thickness((SystemParameters.PrimaryScreenWidth - ViewboxFloatingBar.ActualWidth) / 2, SystemParameters.PrimaryScreenHeight - 60, -2000, -200);
                    });
                })).Start();
            });
            //previousSlideID = Wn.View.CurrentShowPosition;
            ////检查是否有已有墨迹，并加载当前页
            //if (Settings.Automation.IsAutoSaveStrokesInPowerPoint)
            //{
            //    try
            //    {
            //        if (memoryStreams[Wn.View.CurrentShowPosition].Length > 0)
            //        {
            //            Application.Current.Dispatcher.Invoke(() =>
            //            {
            //                inkCanvas.Strokes = new System.Windows.Ink.StrokeCollection(memoryStreams[Wn.View.CurrentShowPosition]);
            //            });
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        LogHelper.WriteLogToFile(string.Format("Failed to load strokes for current slide (Slide {0})\n{1}", Wn.View.CurrentShowPosition, ex.ToString()), LogHelper.LogType.Error);
            //    }
            //}
        }

        bool isEnteredSlideShowEndEvent = false; //防止重复调用本函数导致墨迹保存失效
        private void PptApplication_SlideShowEnd(Presentation Pres)
        {
            LogHelper.WriteLogToFile(string.Format("PowerPoint Slide Show End"), LogHelper.LogType.Event);
            if (isEnteredSlideShowEndEvent)
            {
                LogHelper.WriteLogToFile("Detected previous entrance, returning");
                return;
            }
            isEnteredSlideShowEndEvent = true;
            if (Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint)
            {
                string defaultFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Ink Canvas Strokes\Auto Saved\Presentations\";
                string folderPath = defaultFolderPath + Pres.Name + "_" + Pres.Slides.Count;
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                File.WriteAllText(folderPath + "/Position", previousSlideID.ToString());
                for (int i = 1; i <= Pres.Slides.Count; i++)
                {
                    if (memoryStreams[i] != null)
                    {
                        try
                        {
                            if (memoryStreams[i].Length > 8)
                            {
                                byte[] srcBuf = new Byte[memoryStreams[i].Length];
                                //MessageBox.Show(memoryStreams[i].Length.ToString());
                                int byteLength = memoryStreams[i].Read(srcBuf, 0, srcBuf.Length);
                                File.WriteAllBytes(folderPath + @"\" + i.ToString("0000") + ".icstk", srcBuf);
                                LogHelper.WriteLogToFile(string.Format("Saved strokes for Slide {0}, size={1}, byteLength={2}", i.ToString(), memoryStreams[i].Length, byteLength));
                            }
                            else
                            {
                                File.Delete(folderPath + @"\" + i.ToString("0000") + ".icstk");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile(string.Format("Failed to save strokes for Slide {0}\n{1}", i, ex.ToString()), LogHelper.LogType.Error);
                            File.Delete(folderPath + @"\" + i.ToString("0000") + ".icstk");
                        }
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                isPresentationHaveBlackSpace = false;

                if (BtnSwitchTheme.Content.ToString() == "深色")
                {
                    //Light
                    BtnExit.Foreground = Brushes.Black;
                    SymbolIconBtnColorBlackContent.Foreground = Brushes.White;
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                }
                else
                {
                    //Dark
                }

                BtnPPTSlideShow.Visibility = Visibility.Visible;
                BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;
                StackPanelPPTControls.Visibility = Visibility.Collapsed;
                ViewBoxStackPanelMain.Margin = new Thickness(10, 10, 10, 55);

                if (currentMode != 0)
                {
                    currentMode = 0;
                    GridBackgroundCover.Visibility = Visibility.Collapsed;

                    //SaveStrokes();
                    ClearStrokes(true);
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


                ClearStrokes(true);

                if (Main_Grid.Background != Brushes.Transparent)
                {
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }

                if (pointDesktop != new Point(-1, -1))
                {
                    ViewboxFloatingBar.Margin = new Thickness(pointDesktop.X, pointDesktop.Y, -2000, -200);
                }
            });
        }

        int previousSlideID = 0;
        MemoryStream[] memoryStreams = new MemoryStream[50];

        private void PptApplication_SlideShowNextSlide(SlideShowWindow Wn)
        {
            LogHelper.WriteLogToFile(string.Format("PowerPoint Next Slide (Slide {0})", Wn.View.CurrentShowPosition), LogHelper.LogType.Event);
            if (Wn.View.CurrentShowPosition != previousSlideID)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MemoryStream ms = new MemoryStream();
                    inkCanvas.Strokes.Save(ms);
                    ms.Position = 0;
                    memoryStreams[previousSlideID] = ms;

                    if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber && Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint && !_isPptClickingBtnTurned)
                        SaveScreenShot(true, Wn.Presentation.Name + "/" + Wn.View.CurrentShowPosition);
                    _isPptClickingBtnTurned = false;

                    ClearStrokes(true);
                    timeMachine.ClearStrokeHistory();

                    try
                    {
                        if (memoryStreams[Wn.View.CurrentShowPosition] != null && memoryStreams[Wn.View.CurrentShowPosition].Length > 0)
                        {
                            inkCanvas.Strokes.Add(new StrokeCollection(memoryStreams[Wn.View.CurrentShowPosition]));
                        }
                    }
                    catch
                    { }

                    PptNavigationTextBlock.Text = $"{Wn.View.CurrentShowPosition}/{Wn.Presentation.Slides.Count}";
                });
                previousSlideID = Wn.View.CurrentShowPosition;

            }
        }

        private bool _isPptClickingBtnTurned = false;

        private void BtnPPTSlidesUp_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == 1)
            {
                GridBackgroundCover.Visibility = Visibility.Collapsed;
                currentMode = 0;
            }

            _isPptClickingBtnTurned = true;
            if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
                Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                SaveScreenShot(true, pptApplication.SlideShowWindows[1].Presentation.Name + "/" + pptApplication.SlideShowWindows[1].View.CurrentShowPosition);
            try
            {
                new Thread(new ThreadStart(() =>
                {
                    pptApplication.SlideShowWindows[1].Activate();
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
                GridBackgroundCover.Visibility = Visibility.Collapsed;
                currentMode = 0;
            }
            _isPptClickingBtnTurned = true;
            if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
                Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                SaveScreenShot(true, pptApplication.SlideShowWindows[1].Presentation.Name + "/" + pptApplication.SlideShowWindows[1].View.CurrentShowPosition);
            try
            {
                new Thread(new ThreadStart(() =>
                {
                    pptApplication.SlideShowWindows[1].Activate();
                    pptApplication.SlideShowWindows[1].View.Next();
                })).Start();
            }
            catch
            {
                StackPanelPPTControls.Visibility = Visibility.Collapsed;
            }
        }


        private async void PPTNavigationBtn_Click(object sender, MouseButtonEventArgs e)
        {
            Main_Grid.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
            BtnHideInkCanvas_Click(sender, e);
            pptApplication.SlideShowWindows[1].SlideNavigation.Visible = true;
            // 控制居中
            if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
            {
                if (ViewboxFloatingBar.Margin == new Thickness((SystemParameters.PrimaryScreenWidth - ViewboxFloatingBar.ActualWidth) / 2, SystemParameters.PrimaryScreenHeight - 60, -2000, -200))
                {
                    await Task.Delay(100);
                    ViewboxFloatingBar.Margin = new Thickness((SystemParameters.PrimaryScreenWidth - ViewboxFloatingBar.ActualWidth) / 2, SystemParameters.PrimaryScreenHeight - 60, -2000, -200);
                }
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
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    MemoryStream ms = new MemoryStream();
                    inkCanvas.Strokes.Save(ms);
                    ms.Position = 0;
                    memoryStreams[pptApplication.SlideShowWindows[1].View.CurrentShowPosition] = ms;
                    timeMachine.ClearStrokeHistory();
                }
                catch { }
            });
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

            Settings.PowerPointSettings.PowerPointSupport = ToggleSwitchSupportPowerPoint.IsOn;
            SaveSettingsToFile();

            if (Settings.PowerPointSettings.PowerPointSupport)
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

            Settings.PowerPointSettings.IsShowCanvasAtNewSlideShow = ToggleSwitchShowCanvasAtNewSlideShow.IsOn;
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
        private void ToggleSwitchShowButtonPPTNavigation_OnToggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.PowerPointSettings.IsShowPPTNavigation = ToggleSwitchShowButtonPPTNavigation.IsOn;
            SaveSettingsToFile();

            PptNavigationBtn.Visibility =
                Settings.PowerPointSettings.IsShowPPTNavigation ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ComboBoxTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Appearance.Theme = ComboBoxTheme.SelectedIndex;
            SystemEvents_UserPreferenceChanged(null, null);
            SaveSettingsToFile();
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

        private void ComboBoxEraserSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Canvas.EraserSize = ComboBoxEraserSize.SelectedIndex;
            SaveSettingsToFile();
        }
        
        
        private void ComboBoxEraserType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Canvas.EraserType = ComboBoxEraserType.SelectedIndex;
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

        private void ComboBoxHyperbolaAsymptoteOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Canvas.HyperbolaAsymptoteOption = (OptionalOperation)ComboBoxHyperbolaAsymptoteOption.SelectedIndex;
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

        private void ToggleSwitchSaveScreenshotsInDateFolders_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Automation.IsSaveScreenshotsInDateFolders = ToggleSwitchSaveScreenshotsInDateFolders.IsOn;
            SaveSettingsToFile();
        }

        private void ToggleSwitchAutoSaveStrokesAtScreenshot_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Automation.IsAutoSaveStrokesAtScreenshot = ToggleSwitchAutoSaveStrokesAtScreenshot.IsOn;
            ToggleSwitchAutoSaveStrokesAtClear.Header =
                ToggleSwitchAutoSaveStrokesAtScreenshot.IsOn ? "清屏时自动截图并保存墨迹" : "清屏时自动截图";
            SaveSettingsToFile();
        }

        private void ToggleSwitchAutoSaveStrokesAtClear_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Automation.IsAutoSaveStrokesAtClear = ToggleSwitchAutoSaveStrokesAtClear.IsOn;
            SaveSettingsToFile();
        }


        private void ToggleSwitchExitingWritingMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Automation.IsAutoClearWhenExitingWritingMode = ToggleSwitchClearExitingWritingMode.IsOn;
            SaveSettingsToFile();
        }

        private void ToggleSwitchHideStrokeWhenSelecting_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Canvas.HideStrokeWhenSelecting = ToggleSwitchHideStrokeWhenSelecting.IsOn;
            SaveSettingsToFile();
        }

        private void ToggleSwitchUsingWhiteboard_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Canvas.UsingWhiteboard = ToggleSwitchUsingWhiteboard.IsOn;
            if (!Settings.Canvas.UsingWhiteboard)
            {
                BtnSwitchTheme.Content = "浅色";
            }
            else
            {
                BtnSwitchTheme.Content = "深色";
            }
            BtnSwitchTheme_Click(sender, e);
            SaveSettingsToFile();
        }

        private void ToggleSwitchAutoSaveStrokesInPowerPoint_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint = ToggleSwitchAutoSaveStrokesInPowerPoint.IsOn;
            SaveSettingsToFile();
        }

        private void ToggleSwitchNotifyPreviousPage_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.PowerPointSettings.IsNotifyPreviousPage = ToggleSwitchNotifyPreviousPage.IsOn;
            SaveSettingsToFile();
        }

        private void ToggleSwitchNotifyHiddenPage_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.PowerPointSettings.IsNotifyHiddenPage = ToggleSwitchNotifyHiddenPage.IsOn;
            SaveSettingsToFile();
        }

        private void ToggleSwitchNoStrokeClearInPowerPoint_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.PowerPointSettings.IsNoClearStrokeOnSelectWhenInPowerPoint = ToggleSwitchNoStrokeClearInPowerPoint.IsOn;
            SaveSettingsToFile();
        }


        private void ToggleSwitchShowStrokeOnSelectInPowerPoint_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.PowerPointSettings.IsShowStrokeOnSelectInPowerPoint = ToggleSwitchShowStrokeOnSelectInPowerPoint.IsOn;
            SaveSettingsToFile();
        }

        private void SideControlMinimumAutomationSlider_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Automation.MinimumAutomationStrokeNumber = (int)SideControlMinimumAutomationSlider.Value;
            SaveSettingsToFile();
        }

        private void ToggleSwitchAutoSaveScreenShotInPowerPoint_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint = ToggleSwitchAutoSaveScreenShotInPowerPoint.IsOn;
            SaveSettingsToFile();
        }

        #endregion

        #region Gesture


        private void ToggleSwitchEnableFingerGestureSlideShowControl_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.PowerPointSettings.IsEnableFingerGestureSlideShowControl = ToggleSwitchEnableFingerGestureSlideShowControl.IsOn;

            SaveSettingsToFile();
        }

        private void ToggleSwitchEnableTwoFingerZoom_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Gesture.IsEnableTwoFingerZoom = ToggleSwitchEnableTwoFingerZoom.IsOn;

            SaveSettingsToFile();
        }

        private void ToggleSwitchEnableTwoFingerTranslate_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Gesture.IsEnableTwoFingerTranslate = ToggleSwitchEnableTwoFingerTranslate.IsOn;

            SaveSettingsToFile();
        }

        private void ToggleSwitchEnableTwoFingerRotation_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Gesture.IsEnableTwoFingerRotation = ToggleSwitchEnableTwoFingerRotation.IsOn;
            Settings.Gesture.IsEnableTwoFingerRotationOnSelection = ToggleSwitchEnableTwoFingerRotationOnSelection.IsOn;

            SaveSettingsToFile();
        }

        private void ToggleSwitchEnableTwoFingerGestureInPresentationMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode = ToggleSwitchEnableTwoFingerGestureInPresentationMode.IsOn;

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
            new Thread(new ThreadStart(() =>
            {
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
            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(5000);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SymbolIconResetDefaultComplete.Visibility = Visibility.Collapsed;
                });
            })).Start();
        }
        #endregion

        #region Ink To Shape

        private void ToggleSwitchEnableInkToShape_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.InkToShape.IsInkToShapeEnabled = ToggleSwitchEnableInkToShape.IsOn;
            SaveSettingsToFile();
        }

        #endregion

        #region Advanced

        private void ToggleSwitchIsSpecialScreen_OnToggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Advanced.IsSpecialScreen = ToggleSwitchIsSpecialScreen.IsOn;
            TouchMultiplierSlider.Visibility = ToggleSwitchIsSpecialScreen.IsOn ? Visibility.Visible : Visibility.Collapsed;
            SaveSettingsToFile();
        }

        private void TouchMultiplierSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isLoaded) return;
            Settings.Advanced.TouchMultiplier = e.NewValue;
            SaveSettingsToFile();
        }

        private void BorderCalculateMultiplier_TouchDown(object sender, TouchEventArgs e)
        {
            var args = e.GetTouchPoint(null).Bounds;
            double value;
            if (!Settings.Advanced.IsQuadIR) value = args.Width;
            else value = Math.Sqrt(args.Width * args.Height); //四边红外

            TextBlockShowCalculatedMultiplier.Text = (5 / (value * 1.1)).ToString();
        }

        private void ToggleSwitchEraserBindTouchMultiplier_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Advanced.EraserBindTouchMultiplier = ToggleSwitchEraserBindTouchMultiplier.IsOn;
            SaveSettingsToFile();
        }

        private void ToggleSwitchIsQuadIR_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Advanced.IsQuadIR = ToggleSwitchIsQuadIR.IsOn;
            SaveSettingsToFile();
        }

        private void ToggleSwitchIsLogEnabled_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Advanced.IsLogEnabled = ToggleSwitchIsLogEnabled.IsOn;
            SaveSettingsToFile();
        }

        #endregion

        public static void SaveSettingsToFile()
        {
            string text = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            try
            {
                File.WriteAllText(App.RootPath + settingsFileName, text);
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
            if (inkCanvas.GetSelectedStrokes().Count != 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                inkCanvas.Select(new StrokeCollection());
            }
            _currentCommitType = CommitReason.CodeInput;
            var item = timeMachine.Undo();
            if (item.CommitType == TimeMachineHistoryType.UserInput)
            {
                if (!item.StrokeHasBeenCleared)
                {
                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (!inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Add(strokes);
                    }
                }
                else
                {
                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Remove(strokes);
                    }
                }
            }
            else if (item.CommitType == TimeMachineHistoryType.ShapeRecognition)
            {
                if (item.StrokeHasBeenCleared)
                {

                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Remove(strokes);
                    }
                    foreach (var strokes in item.ReplacedStroke)
                    {
                        if (!inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Add(strokes);
                    }
                }
                else
                {
                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (!inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Add(strokes);
                    }
                    foreach (var strokes in item.ReplacedStroke)
                    {
                        if (inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Remove(strokes);
                    }
                }
            }
            else if (item.CommitType == TimeMachineHistoryType.Rotate)
            {
                if (item.StrokeHasBeenCleared)
                {

                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Remove(strokes);
                    }
                    foreach (var strokes in item.ReplacedStroke)
                    {
                        if (!inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Add(strokes);
                    }
                }
                else
                {
                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (!inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Add(strokes);
                    }
                    foreach (var strokes in item.ReplacedStroke)
                    {
                        if (inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Remove(strokes);
                    }
                }
            }
            else if (item.CommitType == TimeMachineHistoryType.Clear)
            {
                if (!item.StrokeHasBeenCleared)
                {
                    if (item.CurrentStroke != null)
                    {
                        foreach (var currentStroke in item.CurrentStroke)
                        {
                            if (!inkCanvas.Strokes.Contains(currentStroke)) inkCanvas.Strokes.Add(currentStroke);
                        }

                    }
                    if (item.ReplacedStroke != null)
                    {
                        foreach (var replacedStroke in item.ReplacedStroke)
                        {
                            if (inkCanvas.Strokes.Contains(replacedStroke)) inkCanvas.Strokes.Remove(replacedStroke);
                        }
                    }

                }
                else
                {
                    if (item.ReplacedStroke != null)
                    {
                        foreach (var replacedStroke in item.ReplacedStroke)
                        {
                            if (!inkCanvas.Strokes.Contains(replacedStroke)) inkCanvas.Strokes.Add(replacedStroke);
                        }
                    }
                    if (item.CurrentStroke != null)
                    {
                        foreach (var currentStroke in item.CurrentStroke)
                        {
                            if (inkCanvas.Strokes.Contains(currentStroke)) inkCanvas.Strokes.Remove(currentStroke);
                        }
                    }
                }
            }
            _currentCommitType = CommitReason.UserInput;
        }

        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.GetSelectedStrokes().Count != 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                inkCanvas.Select(new StrokeCollection());
            }

            _currentCommitType = CommitReason.CodeInput;
            var item = timeMachine.Redo();
            if (item.CommitType == TimeMachineHistoryType.UserInput)
            {
                if (!item.StrokeHasBeenCleared)
                {
                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (!inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Add(strokes);
                    }
                }
                else
                {
                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Remove(strokes);
                    }
                }
            }
            else if (item.CommitType == TimeMachineHistoryType.ShapeRecognition)
            {
                if (item.StrokeHasBeenCleared)
                {

                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Remove(strokes);
                    }
                    foreach (var strokes in item.ReplacedStroke)
                    {
                        if (!inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Add(strokes);
                    }
                }
                else
                {
                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (!inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Add(strokes);
                    }
                    foreach (var strokes in item.ReplacedStroke)
                    {
                        if (inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Remove(strokes);
                    }
                }
            }
            else if (item.CommitType == TimeMachineHistoryType.Rotate)
            {
                if (item.StrokeHasBeenCleared)
                {

                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Remove(strokes);
                    }
                    foreach (var strokes in item.ReplacedStroke)
                    {
                        if (!inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Add(strokes);
                    }
                }
                else
                {
                    foreach (var strokes in item.CurrentStroke)
                    {
                        if (!inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Add(strokes);
                    }
                    foreach (var strokes in item.ReplacedStroke)
                    {
                        if (inkCanvas.Strokes.Contains(strokes))
                            inkCanvas.Strokes.Remove(strokes);
                    }
                }
            }
            else if (item.CommitType == TimeMachineHistoryType.Clear)
            {
                if (!item.StrokeHasBeenCleared)
                {
                    if (item.CurrentStroke != null)
                    {
                        foreach (var currentStroke in item.CurrentStroke)
                        {
                            if (!inkCanvas.Strokes.Contains(currentStroke)) inkCanvas.Strokes.Add(currentStroke);
                        }

                    }
                    if (item.ReplacedStroke != null)
                    {
                        foreach (var replacedStroke in item.ReplacedStroke)
                        {
                            if (inkCanvas.Strokes.Contains(replacedStroke)) inkCanvas.Strokes.Remove(replacedStroke);
                        }
                    }

                }
                else
                {
                    if (item.ReplacedStroke != null)
                    {
                        foreach (var replacedStroke in item.ReplacedStroke)
                        {
                            if (!inkCanvas.Strokes.Contains(replacedStroke)) inkCanvas.Strokes.Add(replacedStroke);
                        }
                    }
                    if (item.CurrentStroke != null)
                    {
                        foreach (var currentStroke in item.CurrentStroke)
                        {
                            if (inkCanvas.Strokes.Contains(currentStroke)) inkCanvas.Strokes.Remove(currentStroke);
                        }
                    }
                }
            }
            _currentCommitType = CommitReason.UserInput;
        }

        private void Btn_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!isLoaded) return;
            try
            {
                if (((Button)sender).IsEnabled)
                {
                    ((UIElement)((Button)sender).Content).Opacity = 1;
                }
                else
                {
                    ((UIElement)((Button)sender).Content).Opacity = 0.25;
                }
            }
            catch { }
        }
        #endregion Other Controls

        #endregion Left Side Panel

        #region Selection Gestures

        #region Floating Control

        object lastBorderMouseDownObject;

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastBorderMouseDownObject = sender;
        }

        bool isStrokeSelectionCloneOn = false;
        private void BorderStrokeSelectionClone_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            if (isStrokeSelectionCloneOn)
            {
                BorderStrokeSelectionClone.Background = Brushes.Transparent;

                isStrokeSelectionCloneOn = false;
            }
            else
            {
                BorderStrokeSelectionClone.Background = new SolidColorBrush(StringToColor("#FF1ED760"));

                isStrokeSelectionCloneOn = true;
            }
        }

        private void BorderStrokeSelectionCloneToNewBoard_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            var strokes = inkCanvas.GetSelectedStrokes();
            inkCanvas.Select(new StrokeCollection());
            strokes = strokes.Clone();
            BtnWhiteBoardAdd_Click(null, null);
            inkCanvas.Strokes.Add(strokes);
        }

        private void BorderStrokeSelectionDelete_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject == sender)
            {
                SymbolIconDelete_MouseUp(sender, e);
            }
        }

        private void GridPenWidthDecrease_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            foreach (Stroke stroke in inkCanvas.GetSelectedStrokes())
            {
                stroke.DrawingAttributes.Width *= 0.8;
                stroke.DrawingAttributes.Height *= 0.8;
            }
        }

        private void GridPenWidthIncrease_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            foreach (Stroke stroke in inkCanvas.GetSelectedStrokes())
            {
                stroke.DrawingAttributes.Width *= 1.25;
                stroke.DrawingAttributes.Height *= 1.25;
            }
        }

        private void GridPenWidthRestore_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            foreach (Stroke stroke in inkCanvas.GetSelectedStrokes())
            {
                stroke.DrawingAttributes.Width = inkCanvas.DefaultDrawingAttributes.Width;
                stroke.DrawingAttributes.Height = inkCanvas.DefaultDrawingAttributes.Height;
            }
        }

        private void ImageFlipHorizontal_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            Matrix m = new Matrix();

            // Find center of element and then transform to get current location of center
            FrameworkElement fe = e.Source as FrameworkElement;
            Point center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
            center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
            center = m.Transform(center);  // 转换为矩阵缩放和旋转的中心点

            // Update matrix to reflect translation/rotation
            m.ScaleAt(-1, 1, center.X, center.Y);  // 缩放

            StrokeCollection targetStrokes = inkCanvas.GetSelectedStrokes();
            StrokeCollection resultStrokes = targetStrokes.Clone();
            foreach (Stroke stroke in resultStrokes)
            {
                stroke.Transform(m, false);
            }
            _currentCommitType = CommitReason.Rotate;
            inkCanvas.Strokes.Replace(targetStrokes, resultStrokes);
            _currentCommitType = CommitReason.UserInput;
            isProgramChangeStrokeSelection = true;
            inkCanvas.Select(resultStrokes);
            isProgramChangeStrokeSelection = false;

            //updateBorderStrokeSelectionControlLocation();
        }

        private void ImageFlipVertical_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            Matrix m = new Matrix();

            // Find center of element and then transform to get current location of center
            FrameworkElement fe = e.Source as FrameworkElement;
            Point center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
            center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
            center = m.Transform(center);  // 转换为矩阵缩放和旋转的中心点

            // Update matrix to reflect translation/rotation
            m.ScaleAt(1, -1, center.X, center.Y);  // 缩放

            StrokeCollection targetStrokes = inkCanvas.GetSelectedStrokes();
            StrokeCollection resultStrokes = targetStrokes.Clone();
            foreach (Stroke stroke in resultStrokes)
            {
                stroke.Transform(m, false);
            }
            _currentCommitType = CommitReason.Rotate;
            inkCanvas.Strokes.Replace(targetStrokes, resultStrokes);
            _currentCommitType = CommitReason.UserInput;
            isProgramChangeStrokeSelection = true;
            inkCanvas.Select(resultStrokes);
            isProgramChangeStrokeSelection = false;
        }

        private void ImageRotate45_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            Matrix m = new Matrix();

            // Find center of element and then transform to get current location of center
            FrameworkElement fe = e.Source as FrameworkElement;
            Point center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
            center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
            center = m.Transform(center);  // 转换为矩阵缩放和旋转的中心点

            // Update matrix to reflect translation/rotation
            m.RotateAt(45, center.X, center.Y);  // 旋转

            StrokeCollection targetStrokes = inkCanvas.GetSelectedStrokes();
            StrokeCollection resultStrokes = targetStrokes.Clone();
            foreach (Stroke stroke in resultStrokes)
            {
                stroke.Transform(m, false);
            }
            _currentCommitType = CommitReason.Rotate;
            inkCanvas.Strokes.Replace(targetStrokes, resultStrokes);
            _currentCommitType = CommitReason.UserInput;
            isProgramChangeStrokeSelection = true;
            inkCanvas.Select(resultStrokes);
            isProgramChangeStrokeSelection = false;
        }

        private void ImageRotate90_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            Matrix m = new Matrix();

            // Find center of element and then transform to get current location of center
            FrameworkElement fe = e.Source as FrameworkElement;
            Point center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
            center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
            center = m.Transform(center);  // 转换为矩阵缩放和旋转的中心点

            // Update matrix to reflect translation/rotation
            m.RotateAt(90, center.X, center.Y);  // 旋转

            StrokeCollection targetStrokes = inkCanvas.GetSelectedStrokes();
            StrokeCollection resultStrokes = targetStrokes.Clone();
            foreach (Stroke stroke in resultStrokes)
            {
                stroke.Transform(m, false);
            }
            _currentCommitType = CommitReason.Rotate;
            inkCanvas.Strokes.Replace(targetStrokes, resultStrokes);
            _currentCommitType = CommitReason.UserInput;
            isProgramChangeStrokeSelection = true;
            inkCanvas.Select(resultStrokes);
            isProgramChangeStrokeSelection = false;
        }

        #endregion


        bool isGridInkCanvasSelectionCoverMouseDown = false;
        StrokeCollection StrokesSelectionClone = new StrokeCollection();

        private void GridInkCanvasSelectionCover_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isGridInkCanvasSelectionCoverMouseDown = true;
        }

        private void GridInkCanvasSelectionCover_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isGridInkCanvasSelectionCoverMouseDown)
            {
                isGridInkCanvasSelectionCoverMouseDown = false;
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 0;
            inkCanvas.IsManipulationEnabled = false;
            if (inkCanvas.EditingMode == InkCanvasEditingMode.Select)
            {
                if (inkCanvas.GetSelectedStrokes().Count == inkCanvas.Strokes.Count)
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    inkCanvas.IsManipulationEnabled = true;
                }
                else
                {
                    inkCanvas.Select(inkCanvas.Strokes);
                }
            }
            else
            {
                inkCanvas.EditingMode = InkCanvasEditingMode.Select;

            }
        }

        double BorderStrokeSelectionControlWidth = 490.0;
        double BorderStrokeSelectionControlHeight = 80.0;
        bool isProgramChangeStrokeSelection = false;

        private void inkCanvas_SelectionChanged(object sender, EventArgs e)
        {
            if (isProgramChangeStrokeSelection) return;
            if (inkCanvas.GetSelectedStrokes().Count == 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }
            else
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Visible;
                BorderStrokeSelectionClone.Background = Brushes.Transparent;
                isStrokeSelectionCloneOn = false;
                updateBorderStrokeSelectionControlLocation();
            }
        }

        private void updateBorderStrokeSelectionControlLocation()
        {
            double borderLeft = (inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Right - BorderStrokeSelectionControlWidth) / 2;
            double borderTop = inkCanvas.GetSelectionBounds().Bottom + 15;
            if (borderLeft < 0) borderLeft = 0;
            if (borderTop < 0) borderTop = 0;
            if (Width - borderLeft < BorderStrokeSelectionControlWidth || double.IsNaN(borderLeft)) borderLeft = Width - BorderStrokeSelectionControlWidth;
            if (Height - borderTop < BorderStrokeSelectionControlHeight || double.IsNaN(borderTop)) borderTop = Height - BorderStrokeSelectionControlHeight;
            BorderStrokeSelectionControl.Margin = new Thickness(borderLeft, borderTop, 0, 0);
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
            try
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
                    m.ScaleAt(scale.X, scale.Y, center.X, center.Y);  // 缩放

                    StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
                    if (StrokesSelectionClone.Count != 0)
                    {
                        strokes = StrokesSelectionClone;
                    }
                    else if (Settings.Gesture.IsEnableTwoFingerRotationOnSelection)
                    {
                        m.RotateAt(rotate, center.X, center.Y);  // 旋转
                    }
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

                    updateBorderStrokeSelectionControlLocation();
                }
            }
            catch { }
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

                if (isStrokeSelectionCloneOn)
                {
                    StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
                    isProgramChangeStrokeSelection = true;
                    inkCanvas.Select(new StrokeCollection());
                    StrokesSelectionClone = strokes.Clone();
                    inkCanvas.Select(strokes);
                    isProgramChangeStrokeSelection = false;
                    inkCanvas.Strokes.Add(StrokesSelectionClone);
                }
            }
        }

        private void GridInkCanvasSelectionCover_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            dec.Remove(e.TouchDevice.Id);
            if (dec.Count >= 1) return;
            isProgramChangeStrokeSelection = false;
            if (lastTouchPointOnGridInkCanvasCover == e.GetTouchPoint(null).Position)
            {
                if (lastTouchPointOnGridInkCanvasCover.X < inkCanvas.GetSelectionBounds().Left ||
                    lastTouchPointOnGridInkCanvasCover.Y < inkCanvas.GetSelectionBounds().Top ||
                    lastTouchPointOnGridInkCanvasCover.X > inkCanvas.GetSelectionBounds().Right ||
                    lastTouchPointOnGridInkCanvasCover.Y > inkCanvas.GetSelectionBounds().Bottom)
                {
                    inkCanvas.Select(new StrokeCollection());
                    StrokesSelectionClone = new StrokeCollection();
                }
            }
            else if (inkCanvas.GetSelectedStrokes().Count == 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                StrokesSelectionClone = new StrokeCollection();
            }
            else
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Visible;
                StrokesSelectionClone = new StrokeCollection();
            }
        }

        #endregion Selection Gestures

        #region Shape Drawing

        #region Floating Bar Control

        private void ImageDrawShape_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (BorderDrawShape.Visibility == Visibility.Visible)
            {
                BorderDrawShape.Visibility = Visibility.Collapsed;
            }
            else
            {
                BorderDrawShape.Visibility = Visibility.Visible;
            }
        }

        #endregion Floating Bar Control

        int drawingShapeMode = 0;
        bool isLongPressSelected = false; // 用于存是否是“选中”状态，便于后期抬笔后不做切换到笔的处理

        #region Buttons

        private void SymbolIconPinBorderDrawShape_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            ToggleSwitchDrawShapeBorderAutoHide.IsOn = !ToggleSwitchDrawShapeBorderAutoHide.IsOn;

            if (ToggleSwitchDrawShapeBorderAutoHide.IsOn)
            {
                ((ModernWpf.Controls.SymbolIcon)sender).Symbol = ModernWpf.Controls.Symbol.Pin;
            }
            else
            {
                ((ModernWpf.Controls.SymbolIcon)sender).Symbol = ModernWpf.Controls.Symbol.UnPin;
            }
        }

        object lastMouseDownSender = null;
        DateTime lastMouseDownTime = DateTime.MinValue;

        private async void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastMouseDownSender = sender;
            lastMouseDownTime = DateTime.Now;

            await Task.Delay(500);

            if (lastMouseDownSender == sender)
            {
                lastMouseDownSender = null;
                var dA = new DoubleAnimation(1, 0.3, new Duration(TimeSpan.FromMilliseconds(100)));
                ((UIElement)sender).BeginAnimation(OpacityProperty, dA);

                forceEraser = true;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.IsManipulationEnabled = true;
                if (sender == ImageDrawLine)
                {
                    drawingShapeMode = 1;
                }
                else if (sender == ImageDrawDashedLine)
                {
                    drawingShapeMode = 8;
                }
                else if (sender == ImageDrawDotLine)
                {
                    drawingShapeMode = 18;
                }
                else if (sender == ImageDrawArrow)
                {
                    drawingShapeMode = 2;
                }
                else if (sender == ImageDrawParallelLine)
                {
                    drawingShapeMode = 15;
                }
                isLongPressSelected = true;
                if (isSingleFingerDragMode)
                {
                    BtnFingerDragMode_Click(BtnFingerDragMode, null);
                }
            }
        }

        private void BtnPen_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = false;
            drawingShapeMode = 0;
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            isLongPressSelected = false;
        }

        private void BtnDrawLine_Click(object sender, EventArgs e)
        {
            if (lastMouseDownSender == sender)
            {
                forceEraser = true;
                drawingShapeMode = 1;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.IsManipulationEnabled = true;
                CancelSingleFingerDragMode();
            }
            lastMouseDownSender = null;
            if (isLongPressSelected)
            {
                BorderDrawShape.Visibility = Visibility.Collapsed;
                var dA = new DoubleAnimation(1, 1, new Duration(TimeSpan.FromMilliseconds(0)));
                ImageDrawLine.BeginAnimation(OpacityProperty, dA);
            }
        }

        private void BtnDrawDashedLine_Click(object sender, EventArgs e)
        {
            if (lastMouseDownSender == sender)
            {
                forceEraser = true;
                drawingShapeMode = 8;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.IsManipulationEnabled = true;
                CancelSingleFingerDragMode();
            }
            lastMouseDownSender = null;
            if (isLongPressSelected)
            {
                BorderDrawShape.Visibility = Visibility.Collapsed;
                var dA = new DoubleAnimation(1, 1, new Duration(TimeSpan.FromMilliseconds(0)));
                ImageDrawDashedLine.BeginAnimation(OpacityProperty, dA);
            }
        }

        private void BtnDrawDotLine_Click(object sender, EventArgs e)
        {
            if (lastMouseDownSender == sender)
            {
                forceEraser = true;
                drawingShapeMode = 18;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.IsManipulationEnabled = true;
                CancelSingleFingerDragMode();
            }
            lastMouseDownSender = null;
            if (isLongPressSelected)
            {
                BorderDrawShape.Visibility = Visibility.Collapsed;
                var dA = new DoubleAnimation(1, 1, new Duration(TimeSpan.FromMilliseconds(0)));
                ImageDrawDotLine.BeginAnimation(OpacityProperty, dA);
            }
        }

        private void BtnDrawArrow_Click(object sender, EventArgs e)
        {
            if (lastMouseDownSender == sender)
            {
                forceEraser = true;
                drawingShapeMode = 2;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.IsManipulationEnabled = true;
                CancelSingleFingerDragMode();
            }
            lastMouseDownSender = null;
            if (isLongPressSelected)
            {
                BorderDrawShape.Visibility = Visibility.Collapsed;
                var dA = new DoubleAnimation(1, 1, new Duration(TimeSpan.FromMilliseconds(0)));
                ImageDrawArrow.BeginAnimation(OpacityProperty, dA);
            }
        }

        private void BtnDrawParallelLine_Click(object sender, EventArgs e)
        {
            if (lastMouseDownSender == sender)
            {
                forceEraser = true;
                drawingShapeMode = 15;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.IsManipulationEnabled = true;
                CancelSingleFingerDragMode();
            }
            lastMouseDownSender = null;
            if (isLongPressSelected)
            {
                BorderDrawShape.Visibility = Visibility.Collapsed;
                var dA = new DoubleAnimation(1, 1, new Duration(TimeSpan.FromMilliseconds(0)));
                ImageDrawParallelLine.BeginAnimation(OpacityProperty, dA);
            }
        }

        private void BtnDrawCoordinate1_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 11;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawCoordinate2_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 12;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawCoordinate3_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 13;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawCoordinate4_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 14;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawCoordinate5_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 17;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawRectangle_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 3;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawRectangleCenter_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 19;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawEllipse_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 4;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawCircle_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 5;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawCenterEllipse_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 16;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawCenterEllipseWithFocalPoint_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 23;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawDashedCircle_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 10;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawHyperbola_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 24;
            drawMultiStepShapeCurrentStep = 0;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawHyperbolaWithFocalPoint_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 25;
            drawMultiStepShapeCurrentStep = 0;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawParabola1_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 20;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawParabolaWithFocalPoint_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 22;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawParabola2_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 21;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawCylinder_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 6;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawCone_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 7;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        private void BtnDrawCuboid_Click(object sender, EventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 9;
            isFirstTouchCuboid = true;
            CuboidFrontRectIniP = new Point();
            CuboidFrontRectEndP = new Point();
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
        }

        #endregion

        private void inkCanvas_TouchMove(object sender, TouchEventArgs e)
        {
            if (isSingleFingerDragMode) return;
            if (drawingShapeMode != 0)
            {
                if (isLastTouchEraser)
                {
                    return;
                }
                //EraserContainer.Background = null;
                ImageEraser.Visibility = Visibility.Visible;
                if (isWaitUntilNextTouchDown) return;
                if (dec.Count > 1)
                {
                    isWaitUntilNextTouchDown = true;
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStroke);
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
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

        int drawMultiStepShapeCurrentStep = 0; //多笔完成的图形 当前所处在的笔画
        StrokeCollection drawMultiStepShapeSpecialStrokeCollection = new StrokeCollection(); //多笔完成的图形 当前所处在的笔画
        //double drawMultiStepShapeSpecialParameter1 = 0.0; //多笔完成的图形 特殊参数 通常用于表示a
        //double drawMultiStepShapeSpecialParameter2 = 0.0; //多笔完成的图形 特殊参数 通常用于表示b
        double drawMultiStepShapeSpecialParameter3 = 0.0; //多笔完成的图形 特殊参数 通常用于表示k

        private void MouseTouchMove(Point endP)
        {
            List<System.Windows.Point> pointList;
            StylusPointCollection point;
            Stroke stroke;
            StrokeCollection strokes = new StrokeCollection();
            Point newIniP = iniP;
            switch (drawingShapeMode)
            {
                case 1:
                    _currentCommitType = CommitReason.ShapeDrawing;
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
                case 8:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    strokes.Add(GenerateDashedLineStrokeCollection(iniP, endP));
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 18:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    strokes.Add(GenerateDotLineStrokeCollection(iniP, endP));
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 2:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    double w = 30, h = 10;
                    double theta = Math.Atan2(iniP.Y - endP.Y, iniP.X - endP.X);
                    double sint = Math.Sin(theta);
                    double cost = Math.Cos(theta);

                    pointList = new List<Point>
                    {
                        new Point(iniP.X, iniP.Y),
                        new Point(endP.X , endP.Y),
                        new Point(endP.X + (w * cost - h * sint), endP.Y + (w * sint + h * cost)),
                        new Point(endP.X,endP.Y),
                        new Point(endP.X + (w * cost + h * sint), endP.Y - (h * cost - w * sint))
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
                case 15:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    double d = GetDistance(iniP, endP);
                    if (d == 0) return;
                    double sinTheta = (iniP.Y - endP.Y) / d;
                    double cosTheta = (endP.X - iniP.X) / d;
                    double tanTheta = Math.Abs(sinTheta / cosTheta);
                    double x = 25;
                    if (Math.Abs(tanTheta) < 1.0 / 12)
                    {
                        sinTheta = 0;
                        cosTheta = 1;
                        endP.Y = iniP.Y;
                    }
                    if (tanTheta < 0.63 && tanTheta > 0.52) //30
                    {
                        sinTheta = sinTheta / Math.Abs(sinTheta) * 0.5;
                        cosTheta = cosTheta / Math.Abs(cosTheta) * 0.866;
                        endP.Y = iniP.Y - d * sinTheta;
                        endP.X = iniP.X + d * cosTheta;
                    }
                    if (tanTheta < 1.08 && tanTheta > 0.92) //45
                    {
                        sinTheta = sinTheta / Math.Abs(sinTheta) * 0.707;
                        cosTheta = cosTheta / Math.Abs(cosTheta) * 0.707;
                        endP.Y = iniP.Y - d * sinTheta;
                        endP.X = iniP.X + d * cosTheta;
                    }
                    if (tanTheta < 1.95 && tanTheta > 1.63) //60
                    {
                        sinTheta = sinTheta / Math.Abs(sinTheta) * 0.866;
                        cosTheta = cosTheta / Math.Abs(cosTheta) * 0.5;
                        endP.Y = iniP.Y - d * sinTheta;
                        endP.X = iniP.X + d * cosTheta;
                    }
                    if (Math.Abs(cosTheta / sinTheta) < 1.0 / 12)
                    {
                        endP.X = iniP.X;
                        sinTheta = 1;
                        cosTheta = 0;
                    }
                    strokes.Add(GenerateLineStroke(new Point(iniP.X - 3 * x * sinTheta, iniP.Y - 3 * x * cosTheta), new Point(endP.X - 3 * x * sinTheta, endP.Y - 3 * x * cosTheta)));
                    strokes.Add(GenerateLineStroke(new Point(iniP.X - x * sinTheta, iniP.Y - x * cosTheta), new Point(endP.X - x * sinTheta, endP.Y - x * cosTheta)));
                    strokes.Add(GenerateLineStroke(new Point(iniP.X + x * sinTheta, iniP.Y + x * cosTheta), new Point(endP.X + x * sinTheta, endP.Y + x * cosTheta)));
                    strokes.Add(GenerateLineStroke(new Point(iniP.X + 3 * x * sinTheta, iniP.Y + 3 * x * cosTheta), new Point(endP.X + 3 * x * sinTheta, endP.Y + 3 * x * cosTheta)));
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 11:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    strokes.Add(GenerateArrowLineStroke(new Point(2 * iniP.X - (endP.X - 20), iniP.Y), new Point(endP.X, iniP.Y)));
                    strokes.Add(GenerateArrowLineStroke(new Point(iniP.X, 2 * iniP.Y - (endP.Y + 20)), new Point(iniP.X, endP.Y)));
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 12:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    if (Math.Abs(iniP.X - endP.X) < 0.01) return;
                    strokes.Add(GenerateArrowLineStroke(new Point(iniP.X + (iniP.X - endP.X) / Math.Abs(iniP.X - endP.X) * 25, iniP.Y), new Point(endP.X, iniP.Y)));
                    strokes.Add(GenerateArrowLineStroke(new Point(iniP.X, 2 * iniP.Y - (endP.Y + 20)), new Point(iniP.X, endP.Y)));
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 13:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    if (Math.Abs(iniP.Y - endP.Y) < 0.01) return;
                    strokes.Add(GenerateArrowLineStroke(new Point(2 * iniP.X - (endP.X - 20), iniP.Y), new Point(endP.X, iniP.Y)));
                    strokes.Add(GenerateArrowLineStroke(new Point(iniP.X, iniP.Y + (iniP.Y - endP.Y) / Math.Abs(iniP.Y - endP.Y) * 25), new Point(iniP.X, endP.Y)));
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 14:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return;
                    strokes.Add(GenerateArrowLineStroke(new Point(iniP.X + (iniP.X - endP.X) / Math.Abs(iniP.X - endP.X) * 25, iniP.Y), new Point(endP.X, iniP.Y)));
                    strokes.Add(GenerateArrowLineStroke(new Point(iniP.X, iniP.Y + (iniP.Y - endP.Y) / Math.Abs(iniP.Y - endP.Y) * 25), new Point(iniP.X, endP.Y)));
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 17:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    strokes.Add(GenerateArrowLineStroke(new Point(iniP.X, iniP.Y), new Point(iniP.X + Math.Abs(endP.X - iniP.X), iniP.Y)));
                    strokes.Add(GenerateArrowLineStroke(new Point(iniP.X, iniP.Y), new Point(iniP.X, iniP.Y - Math.Abs(endP.Y - iniP.Y))));
                    d = (Math.Abs(iniP.X - endP.X) + Math.Abs(iniP.Y - endP.Y)) / 2;
                    strokes.Add(GenerateArrowLineStroke(new Point(iniP.X, iniP.Y), new Point(iniP.X - d / 1.76, iniP.Y + d / 1.76)));
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 3:
                    _currentCommitType = CommitReason.ShapeDrawing;
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
                case 19:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    double a = iniP.X - endP.X;
                    double b = iniP.Y - endP.Y;
                    pointList = new List<System.Windows.Point>{
                        new System.Windows.Point(iniP.X - a, iniP.Y - b),
                        new System.Windows.Point(iniP.X - a, iniP.Y + b),
                        new System.Windows.Point(iniP.X + a, iniP.Y + b),
                        new System.Windows.Point(iniP.X + a, iniP.Y - b),
                        new System.Windows.Point(iniP.X - a, iniP.Y - b)
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
                    _currentCommitType = CommitReason.ShapeDrawing;
                    pointList = GenerateEllipseGeometry(iniP, endP);
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
                case 5:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    double R = GetDistance(iniP, endP);
                    pointList = GenerateEllipseGeometry(new Point(iniP.X - R, iniP.Y - R), new Point(iniP.X + R, iniP.Y + R));
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
                case 16:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    double halfA = endP.X - iniP.X;
                    double halfB = endP.Y - iniP.Y;
                    pointList = GenerateEllipseGeometry(new Point(iniP.X - halfA, iniP.Y - halfB), new Point(iniP.X + halfA, iniP.Y + halfB));
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
                case 23:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    a = Math.Abs(endP.X - iniP.X);
                    b = Math.Abs(endP.Y - iniP.Y);
                    pointList = GenerateEllipseGeometry(new Point(iniP.X - a, iniP.Y - b), new Point(iniP.X + a, iniP.Y + b));
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke);
                    double c = Math.Sqrt(Math.Abs(a * a - b * b));
                    StylusPoint stylusPoint;
                    if (a > b)
                    {
                        stylusPoint = new StylusPoint(iniP.X + c, iniP.Y, (float)1.0);
                        point = new StylusPointCollection();
                        point.Add(stylusPoint);
                        stroke = new Stroke(point)
                        {
                            DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                        };
                        strokes.Add(stroke.Clone());
                        stylusPoint = new StylusPoint(iniP.X - c, iniP.Y, (float)1.0);
                        point = new StylusPointCollection();
                        point.Add(stylusPoint);
                        stroke = new Stroke(point)
                        {
                            DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                        };
                        strokes.Add(stroke.Clone());
                    }
                    else if (a < b)
                    {
                        stylusPoint = new StylusPoint(iniP.X, iniP.Y - c, (float)1.0);
                        point = new StylusPointCollection();
                        point.Add(stylusPoint);
                        stroke = new Stroke(point)
                        {
                            DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                        };
                        strokes.Add(stroke.Clone());
                        stylusPoint = new StylusPoint(iniP.X, iniP.Y + c, (float)1.0);
                        point = new StylusPointCollection();
                        point.Add(stylusPoint);
                        stroke = new Stroke(point)
                        {
                            DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                        };
                        strokes.Add(stroke.Clone());
                    }
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 10:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    R = GetDistance(iniP, endP);
                    strokes = GenerateDashedLineEllipseStrokeCollection(new Point(iniP.X - R, iniP.Y - R), new Point(iniP.X + R, iniP.Y + R));
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 24:
                case 25:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    //双曲线 x^2/a^2 - y^2/b^2 = 1
                    if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return;
                    var pointList2 = new List<Point>();
                    var pointList3 = new List<Point>();
                    var pointList4 = new List<Point>();
                    if (drawMultiStepShapeCurrentStep == 0)
                    {
                        //第一笔：画渐近线
                        double k = Math.Abs((endP.Y - iniP.Y) / (endP.X - iniP.X));
                        strokes.Add(GenerateDashedLineStrokeCollection(new Point(2 * iniP.X - endP.X, 2 * iniP.Y - endP.Y), endP));
                        strokes.Add(GenerateDashedLineStrokeCollection(new Point(2 * iniP.X - endP.X, endP.Y), new Point(endP.X, 2 * iniP.Y - endP.Y)));
                        drawMultiStepShapeSpecialParameter3 = k;
                        drawMultiStepShapeSpecialStrokeCollection = strokes;
                    }
                    else
                    {
                        //第二笔：画双曲线
                        double k = drawMultiStepShapeSpecialParameter3;
                        bool isHyperbolaFocalPointOnXAxis = Math.Abs((endP.Y - iniP.Y) / (endP.X - iniP.X)) < k;
                        if (isHyperbolaFocalPointOnXAxis) { // 焦点在 x 轴上
                            a = Math.Sqrt(Math.Abs((endP.X - iniP.X) * (endP.X - iniP.X) - (endP.Y - iniP.Y) * (endP.Y - iniP.Y) / (k * k)));
                            b = a * k;
                            pointList = new List<Point>();
                            for (double i = a; i <= Math.Abs(endP.X - iniP.X); i += 0.5)
                            {
                                double rY = Math.Sqrt(Math.Abs(k * k * i * i - b * b));
                                pointList.Add(new Point(iniP.X + i, iniP.Y - rY));
                                pointList2.Add(new Point(iniP.X + i, iniP.Y + rY));
                                pointList3.Add(new Point(iniP.X - i, iniP.Y - rY));
                                pointList4.Add(new Point(iniP.X - i, iniP.Y + rY));
                            }
                        } else { // 焦点在 y 轴上
                            a = Math.Sqrt(Math.Abs((endP.Y - iniP.Y) * (endP.Y - iniP.Y) - (endP.X - iniP.X) * (endP.X - iniP.X) * (k * k)));
                            b = a / k;
                            pointList = new List<Point>();
                            for (double i = a; i <= Math.Abs(endP.Y - iniP.Y); i += 0.5)
                            {
                                double rX = Math.Sqrt(Math.Abs(i * i / k / k - b * b));
                                pointList.Add(new Point(iniP.X - rX, iniP.Y + i));
                                pointList2.Add(new Point(iniP.X + rX, iniP.Y + i));
                                pointList3.Add(new Point(iniP.X - rX, iniP.Y - i));
                                pointList4.Add(new Point(iniP.X + rX, iniP.Y - i));
                            }
                        }
                        try
                        {
                            point = new StylusPointCollection(pointList);
                            stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                            strokes.Add(stroke.Clone());
                            point = new StylusPointCollection(pointList2);
                            stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                            strokes.Add(stroke.Clone());
                            point = new StylusPointCollection(pointList3);
                            stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                            strokes.Add(stroke.Clone());
                            point = new StylusPointCollection(pointList4);
                            stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                            strokes.Add(stroke.Clone());
                            if (drawingShapeMode == 25)
                            {
                                //画焦点
                                c = Math.Sqrt(a * a + b * b);
                                stylusPoint = isHyperbolaFocalPointOnXAxis ? new StylusPoint(iniP.X + c, iniP.Y, (float)1.0) : new StylusPoint(iniP.X, iniP.Y + c, (float)1.0);
                                point = new StylusPointCollection();
                                point.Add(stylusPoint);
                                stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                                strokes.Add(stroke.Clone());
                                stylusPoint = isHyperbolaFocalPointOnXAxis ? new StylusPoint(iniP.X - c, iniP.Y, (float)1.0) : new StylusPoint(iniP.X, iniP.Y - c, (float)1.0);
                                point = new StylusPointCollection();
                                point.Add(stylusPoint);
                                stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                                strokes.Add(stroke.Clone());
                            }
                        }
                        catch
                        {
                            return;
                        }
                    }
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 20:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    //抛物线 y=ax^2
                    if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return;
                    a = (iniP.Y - endP.Y) / ((iniP.X - endP.X) * (iniP.X - endP.X));
                    pointList = new List<Point>();
                    pointList2 = new List<Point>();
                    for (double i = 0.0; i <= Math.Abs(endP.X - iniP.X); i += 0.5)
                    {
                        pointList.Add(new Point(iniP.X + i, iniP.Y - a * i * i));
                        pointList2.Add(new Point(iniP.X - i, iniP.Y - a * i * i));
                    }
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    point = new StylusPointCollection(pointList2);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 21:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    //抛物线 y^2=ax
                    if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return;
                    a = (iniP.X - endP.X) / ((iniP.Y - endP.Y) * (iniP.Y - endP.Y));
                    pointList = new List<Point>();
                    pointList2 = new List<Point>();
                    for (double i = 0.0; i <= Math.Abs(endP.Y - iniP.Y); i += 0.5)
                    {
                        pointList.Add(new Point(iniP.X - a * i * i, iniP.Y + i));
                        pointList2.Add(new Point(iniP.X - a * i * i, iniP.Y - i));
                    }
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    point = new StylusPointCollection(pointList2);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 22:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    //抛物线 y^2=ax, 含焦点
                    if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return;
                    double p = (iniP.Y - endP.Y) * (iniP.Y - endP.Y) / (2 * (iniP.X - endP.X));
                    a = 0.5 / p;
                    pointList = new List<Point>();
                    pointList2 = new List<Point>();
                    for (double i = 0.0; i <= Math.Abs(endP.Y - iniP.Y); i += 0.5)
                    {
                        pointList.Add(new Point(iniP.X - a * i * i, iniP.Y + i));
                        pointList2.Add(new Point(iniP.X - a * i * i, iniP.Y - i));
                    }
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    point = new StylusPointCollection(pointList2);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    stylusPoint = new StylusPoint(iniP.X - p / 2, iniP.Y, (float)1.0);
                    point = new StylusPointCollection();
                    point.Add(stylusPoint);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 6:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    newIniP = iniP;
                    if (iniP.Y > endP.Y)
                    {
                        newIniP = new Point(iniP.X, endP.Y);
                        endP = new Point(endP.X, iniP.Y);
                    }
                    double topA = Math.Abs(newIniP.X - endP.X);
                    double topB = topA / 2.646;
                    //顶部椭圆
                    pointList = GenerateEllipseGeometry(new Point(newIniP.X, newIniP.Y - topB / 2), new Point(endP.X, newIniP.Y + topB / 2));
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    //底部椭圆
                    pointList = GenerateEllipseGeometry(new Point(newIniP.X, endP.Y - topB / 2), new Point(endP.X, endP.Y + topB / 2), false, true);
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    strokes.Add(GenerateDashedLineEllipseStrokeCollection(new Point(newIniP.X, endP.Y - topB / 2), new Point(endP.X, endP.Y + topB / 2), true, false));
                    //左侧
                    pointList = new List<System.Windows.Point>{
                        new System.Windows.Point(newIniP.X, newIniP.Y),
                        new System.Windows.Point(newIniP.X, endP.Y)
                    };
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    //右侧
                    pointList = new List<System.Windows.Point>{
                        new System.Windows.Point(endP.X, newIniP.Y),
                        new System.Windows.Point(endP.X, endP.Y)
                    };
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 7:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    if (iniP.Y > endP.Y)
                    {
                        newIniP = new Point(iniP.X, endP.Y);
                        endP = new Point(endP.X, iniP.Y);
                    }
                    double bottomA = Math.Abs(newIniP.X - endP.X);
                    double bottomB = bottomA / 2.646;
                    //底部椭圆
                    pointList = GenerateEllipseGeometry(new Point(newIniP.X, endP.Y - bottomB / 2), new Point(endP.X, endP.Y + bottomB / 2), false, true);
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    strokes.Add(GenerateDashedLineEllipseStrokeCollection(new Point(newIniP.X, endP.Y - bottomB / 2), new Point(endP.X, endP.Y + bottomB / 2), true, false));
                    //左侧
                    pointList = new List<System.Windows.Point>{
                        new System.Windows.Point((newIniP.X + endP.X) / 2, newIniP.Y),
                        new System.Windows.Point(newIniP.X, endP.Y)
                    };
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    //右侧
                    pointList = new List<System.Windows.Point>{
                        new System.Windows.Point((newIniP.X + endP.X) / 2, newIniP.Y),
                        new System.Windows.Point(endP.X, endP.Y)
                    };
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                    try
                    {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch { }
                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 9:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    if (isFirstTouchCuboid)
                    {
                        //分开画线条方便后期单独擦除某一条棱
                        strokes.Add(GenerateLineStroke(new Point(iniP.X, iniP.Y), new Point(iniP.X, endP.Y)));
                        strokes.Add(GenerateLineStroke(new Point(iniP.X, endP.Y), new Point(endP.X, endP.Y)));
                        strokes.Add(GenerateLineStroke(new Point(endP.X, endP.Y), new Point(endP.X, iniP.Y)));
                        strokes.Add(GenerateLineStroke(new Point(iniP.X, iniP.Y), new Point(endP.X, iniP.Y)));
                        try
                        {
                            inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                        }
                        catch { }
                        lastTempStrokeCollection = strokes;
                        inkCanvas.Strokes.Add(strokes);
                        CuboidFrontRectIniP = iniP;
                        CuboidFrontRectEndP = endP;
                    }
                    else
                    {
                        d = CuboidFrontRectIniP.Y - endP.Y;
                        if (d < 0) d = -d; //就是懒不想做反向的，不要让我去做，想做自己做好之后 Pull Request
                        a = CuboidFrontRectEndP.X - CuboidFrontRectIniP.X; //正面矩形长
                        b = CuboidFrontRectEndP.Y - CuboidFrontRectIniP.Y; //正面矩形宽

                        //横上
                        Point newLineIniP = new Point(CuboidFrontRectIniP.X + d, CuboidFrontRectIniP.Y - d);
                        Point newLineEndP = new Point(CuboidFrontRectEndP.X + d, CuboidFrontRectIniP.Y - d);
                        pointList = new List<System.Windows.Point> { newLineIniP, newLineEndP };
                        point = new StylusPointCollection(pointList);
                        stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                        strokes.Add(stroke.Clone());
                        //横下 (虚线)
                        newLineIniP = new Point(CuboidFrontRectIniP.X + d, CuboidFrontRectEndP.Y - d);
                        newLineEndP = new Point(CuboidFrontRectEndP.X + d, CuboidFrontRectEndP.Y - d);
                        strokes.Add(GenerateDashedLineStrokeCollection(newLineIniP, newLineEndP));
                        //斜左上
                        newLineIniP = new Point(CuboidFrontRectIniP.X, CuboidFrontRectIniP.Y);
                        newLineEndP = new Point(CuboidFrontRectIniP.X + d, CuboidFrontRectIniP.Y - d);
                        pointList = new List<System.Windows.Point> { newLineIniP, newLineEndP };
                        point = new StylusPointCollection(pointList);
                        stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                        strokes.Add(stroke.Clone());
                        //斜右上
                        newLineIniP = new Point(CuboidFrontRectEndP.X, CuboidFrontRectIniP.Y);
                        newLineEndP = new Point(CuboidFrontRectEndP.X + d, CuboidFrontRectIniP.Y - d);
                        pointList = new List<System.Windows.Point> { newLineIniP, newLineEndP };
                        point = new StylusPointCollection(pointList);
                        stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                        strokes.Add(stroke.Clone());
                        //斜左下 (虚线)
                        newLineIniP = new Point(CuboidFrontRectIniP.X, CuboidFrontRectEndP.Y);
                        newLineEndP = new Point(CuboidFrontRectIniP.X + d, CuboidFrontRectEndP.Y - d);
                        strokes.Add(GenerateDashedLineStrokeCollection(newLineIniP, newLineEndP));
                        //斜右下
                        newLineIniP = new Point(CuboidFrontRectEndP.X, CuboidFrontRectEndP.Y);
                        newLineEndP = new Point(CuboidFrontRectEndP.X + d, CuboidFrontRectEndP.Y - d);
                        pointList = new List<System.Windows.Point> { newLineIniP, newLineEndP };
                        point = new StylusPointCollection(pointList);
                        stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                        strokes.Add(stroke.Clone());
                        //竖左 (虚线)
                        newLineIniP = new Point(CuboidFrontRectIniP.X + d, CuboidFrontRectIniP.Y - d);
                        newLineEndP = new Point(CuboidFrontRectIniP.X + d, CuboidFrontRectEndP.Y - d);
                        strokes.Add(GenerateDashedLineStrokeCollection(newLineIniP, newLineEndP));
                        //竖右
                        newLineIniP = new Point(CuboidFrontRectEndP.X + d, CuboidFrontRectIniP.Y - d);
                        newLineEndP = new Point(CuboidFrontRectEndP.X + d, CuboidFrontRectEndP.Y - d);
                        pointList = new List<System.Windows.Point> { newLineIniP, newLineEndP };
                        point = new StylusPointCollection(pointList);
                        stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                        strokes.Add(stroke.Clone());

                        try
                        {
                            inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                        }
                        catch { }
                        lastTempStrokeCollection = strokes;
                        inkCanvas.Strokes.Add(strokes);
                    }
                    break;
            }
        }

        bool isFirstTouchCuboid = true;
        Point CuboidFrontRectIniP = new Point();
        Point CuboidFrontRectEndP = new Point();

        private void Main_Grid_TouchUp(object sender, TouchEventArgs e)
        {
            inkCanvas_MouseUp(sender, null);
            if (dec.Count == 0)
            {
                isWaitUntilNextTouchDown = false;
            }
        }
        Stroke lastTempStroke = null;
        StrokeCollection lastTempStrokeCollection = new StrokeCollection();
        bool isWaitUntilNextTouchDown = false;
        private List<System.Windows.Point> GenerateEllipseGeometry(System.Windows.Point st, System.Windows.Point ed, bool isDrawTop = true, bool isDrawBottom = true)
        {
            double a = 0.5 * (ed.X - st.X);
            double b = 0.5 * (ed.Y - st.Y);
            List<System.Windows.Point> pointList = new List<System.Windows.Point>();
            if (isDrawTop && isDrawBottom)
            {
                for (double r = 0; r <= 2 * Math.PI; r = r + 0.01)
                {
                    pointList.Add(new System.Windows.Point(0.5 * (st.X + ed.X) + a * Math.Cos(r), 0.5 * (st.Y + ed.Y) + b * Math.Sin(r)));
                }
            }
            else
            {
                if (isDrawBottom)
                {
                    for (double r = 0; r <= Math.PI; r = r + 0.01)
                    {
                        pointList.Add(new System.Windows.Point(0.5 * (st.X + ed.X) + a * Math.Cos(r), 0.5 * (st.Y + ed.Y) + b * Math.Sin(r)));
                    }
                }
                if (isDrawTop)
                {
                    for (double r = Math.PI; r <= 2 * Math.PI; r = r + 0.01)
                    {
                        pointList.Add(new System.Windows.Point(0.5 * (st.X + ed.X) + a * Math.Cos(r), 0.5 * (st.Y + ed.Y) + b * Math.Sin(r)));
                    }
                }
            }
            return pointList;
        }

        private StrokeCollection GenerateDashedLineEllipseStrokeCollection(System.Windows.Point st, System.Windows.Point ed, bool isDrawTop = true, bool isDrawBottom = true)
        {
            double a = 0.5 * (ed.X - st.X);
            double b = 0.5 * (ed.Y - st.Y);
            double step = 0.05;
            List<System.Windows.Point> pointList = new List<System.Windows.Point>();
            StylusPointCollection point;
            Stroke stroke;
            StrokeCollection strokes = new StrokeCollection();
            if (isDrawBottom)
            {
                for (double i = 0.0; i < 1.0; i += step * 1.66)
                {
                    pointList = new List<Point>();
                    for (double r = Math.PI * i; r <= Math.PI * (i + step); r = r + 0.01)
                    {
                        pointList.Add(new System.Windows.Point(0.5 * (st.X + ed.X) + a * Math.Cos(r), 0.5 * (st.Y + ed.Y) + b * Math.Sin(r)));
                    }
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                }
            }
            if (isDrawTop)
            {
                for (double i = 1.0; i < 2.0; i += step * 1.66)
                {
                    pointList = new List<Point>();
                    for (double r = Math.PI * i; r <= Math.PI * (i + step); r = r + 0.01)
                    {
                        pointList.Add(new System.Windows.Point(0.5 * (st.X + ed.X) + a * Math.Cos(r), 0.5 * (st.Y + ed.Y) + b * Math.Sin(r)));
                    }
                    point = new StylusPointCollection(pointList);
                    stroke = new Stroke(point)
                    {
                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                    };
                    strokes.Add(stroke.Clone());
                }
            }
            return strokes;
        }

        private Stroke GenerateLineStroke(System.Windows.Point st, System.Windows.Point ed)
        {
            List<System.Windows.Point> pointList = new List<System.Windows.Point>();
            StylusPointCollection point;
            Stroke stroke;
            pointList = new List<System.Windows.Point>{
                new System.Windows.Point(st.X, st.Y),
                new System.Windows.Point(ed.X, ed.Y)
            };
            point = new StylusPointCollection(pointList);
            stroke = new Stroke(point)
            {
                DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
            };
            return stroke;
        }

        private Stroke GenerateArrowLineStroke(System.Windows.Point st, System.Windows.Point ed)
        {
            List<System.Windows.Point> pointList = new List<System.Windows.Point>();
            StylusPointCollection point;
            Stroke stroke;

            double w = 20, h = 7;
            double theta = Math.Atan2(st.Y - ed.Y, st.X - ed.X);
            double sint = Math.Sin(theta);
            double cost = Math.Cos(theta);

            pointList = new List<Point>
            {
                new Point(st.X, st.Y),
                new Point(ed.X , ed.Y),
                new Point(ed.X + (w * cost - h * sint), ed.Y + (w * sint + h * cost)),
                new Point(ed.X,ed.Y),
                new Point(ed.X + (w * cost + h * sint), ed.Y - (h * cost - w * sint))
            };
            point = new StylusPointCollection(pointList);
            stroke = new Stroke(point)
            {
                DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
            };
            return stroke;
        }

        private StrokeCollection GenerateDashedLineStrokeCollection(System.Windows.Point st, System.Windows.Point ed)
        {
            double step = 5;
            List<System.Windows.Point> pointList = new List<System.Windows.Point>();
            StylusPointCollection point;
            Stroke stroke;
            StrokeCollection strokes = new StrokeCollection();
            double d = GetDistance(st, ed);
            double sinTheta = (ed.Y - st.Y) / d;
            double cosTheta = (ed.X - st.X) / d;
            for (double i = 0.0; i < d; i += step * 2.76)
            {
                pointList = new List<System.Windows.Point>{
                    new System.Windows.Point(st.X + i * cosTheta, st.Y + i * sinTheta),
                    new System.Windows.Point(st.X + Math.Min(i + step, d) * cosTheta, st.Y + Math.Min(i + step, d) * sinTheta)
                };
                point = new StylusPointCollection(pointList);
                stroke = new Stroke(point)
                {
                    DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                };
                strokes.Add(stroke.Clone());
            }
            return strokes;
        }

        private StrokeCollection GenerateDotLineStrokeCollection(System.Windows.Point st, System.Windows.Point ed)
        {
            double step = 3;
            List<System.Windows.Point> pointList = new List<System.Windows.Point>();
            StylusPointCollection point;
            Stroke stroke;
            StrokeCollection strokes = new StrokeCollection();
            double d = GetDistance(st, ed);
            double sinTheta = (ed.Y - st.Y) / d;
            double cosTheta = (ed.X - st.X) / d;
            for (double i = 0.0; i < d; i += step * 2.76)
            {
                var stylusPoint = new StylusPoint(st.X + i * cosTheta, st.Y + i * sinTheta, (float)0.8);
                point = new StylusPointCollection();
                point.Add(stylusPoint);
                stroke = new Stroke(point)
                {
                    DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                };
                strokes.Add(stroke.Clone());
            }
            return strokes;
        }

        bool isMouseDown = false;
        private void inkCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
            if (NeedUpdateIniP())
            {
                iniP = e.GetPosition(inkCanvas);
            }
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
            if (drawingShapeMode == 5)
            {
                Circle circle = new Circle(new Point(), 0, lastTempStroke);
                circle.R = GetDistance(circle.Stroke.StylusPoints[0].ToPoint(), circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].ToPoint()) / 2;
                circle.Centroid = new Point((circle.Stroke.StylusPoints[0].X + circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].X) / 2,
                                            (circle.Stroke.StylusPoints[0].Y + circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].Y) / 2);
                circles.Add(circle);
            }
            if (drawingShapeMode != 9 && drawingShapeMode != 0 && drawingShapeMode != 24 && drawingShapeMode != 25)
            {
                if (isLongPressSelected)
                {

                }
                else
                {
                    BtnPen_Click(null, null); //画完一次还原到笔模式
                }
            }
            if (drawingShapeMode == 9)
            {
                if (isFirstTouchCuboid)
                {
                    if (CuboidStrokeCollection == null) CuboidStrokeCollection = new StrokeCollection();
                    isFirstTouchCuboid = false;
                    Point newIniP = new Point(Math.Min(CuboidFrontRectIniP.X, CuboidFrontRectEndP.X), Math.Min(CuboidFrontRectIniP.Y, CuboidFrontRectEndP.Y));
                    Point newEndP = new Point(Math.Max(CuboidFrontRectIniP.X, CuboidFrontRectEndP.X), Math.Max(CuboidFrontRectIniP.Y, CuboidFrontRectEndP.Y));
                    CuboidFrontRectIniP = newIniP;
                    CuboidFrontRectEndP = newEndP;
                    CuboidStrokeCollection.Add(lastTempStrokeCollection);
                }
                else
                {
                    BtnPen_Click(null, null); //画完还原到笔模式
                    if (_currentCommitType == CommitReason.ShapeDrawing)
                    {
                        CuboidStrokeCollection.Add(lastTempStrokeCollection);
                        _currentCommitType = CommitReason.UserInput;
                        timeMachine.CommitStrokeUserInputHistory(CuboidStrokeCollection);
                        CuboidStrokeCollection = null;
                    }
                }
            }
            if (drawingShapeMode == 24 || drawingShapeMode == 25)
            {
                if (drawMultiStepShapeCurrentStep == 0)
                {
                    drawMultiStepShapeCurrentStep = 1;
                }
                else
                {
                    drawMultiStepShapeCurrentStep = 0;
                    if (drawMultiStepShapeSpecialStrokeCollection != null)
                    {
                        bool opFlag = false;
                        switch (Settings.Canvas.HyperbolaAsymptoteOption)
                        {
                            case OptionalOperation.Yes:
                                opFlag = true;
                                break;
                            case OptionalOperation.No:
                                opFlag = false;
                                break;
                            case OptionalOperation.Ask:
                                opFlag = MessageBox.Show("是否移除渐近线？", "Ink Canvas", MessageBoxButton.YesNo) != MessageBoxResult.Yes;
                                break;
                        };
                        if (!opFlag)
                        {
                            inkCanvas.Strokes.Remove(drawMultiStepShapeSpecialStrokeCollection);
                        }
                    }
                    BtnPen_Click(null, null); //画完还原到笔模式
                }
            }
            isMouseDown = false;
            if (ReplacedStroke != null || AddedStroke != null)
            {
                timeMachine.CommitStrokeEraseHistory(ReplacedStroke, AddedStroke);
                AddedStroke = null;
                ReplacedStroke = null;
            }
            if (_currentCommitType == CommitReason.ShapeDrawing && drawingShapeMode != 9)
            {
                _currentCommitType = CommitReason.UserInput;
                StrokeCollection collection;
                if (lastTempStrokeCollection != null && lastTempStrokeCollection.Count > 0)
                {
                    collection = lastTempStrokeCollection;
                }
                else
                {
                    collection = new StrokeCollection() { lastTempStroke };
                }
                timeMachine.CommitStrokeUserInputHistory(collection);
            }
            lastTempStroke = null;
            lastTempStrokeCollection = null;
        }

        private bool NeedUpdateIniP()
        {
            if (drawingShapeMode == 24 || drawingShapeMode == 25)
            {
                if (drawMultiStepShapeCurrentStep == 1) return false;
            }
            return true;
        }

        #endregion Shape Drawing

        #region Whiteboard Controls

        StrokeCollection[] strokeCollections = new StrokeCollection[101];
        bool[] whiteboadLastModeIsRedo = new bool[101];
        StrokeCollection lastTouchDownStrokeCollection = new StrokeCollection();

        int CurrentWhiteboardIndex = 1;
        int WhiteboardTotalCount = 1;
        TimeMachineHistory[][] TimeMachineHistories = new TimeMachineHistory[101][]; //最多99页，0用来存储非白板时的墨迹以便还原

        private void SaveStrokes(bool isBackupMain = false)
        {
            if (isBackupMain)
            {
                var timeMachineHistory = timeMachine.ExportTimeMachineHistory();
                TimeMachineHistories[0] = timeMachineHistory;
                timeMachine.ClearStrokeHistory();

            }
            else
            {
                var timeMachineHistory = timeMachine.ExportTimeMachineHistory();
                TimeMachineHistories[CurrentWhiteboardIndex] = timeMachineHistory;
                timeMachine.ClearStrokeHistory();
            }
        }

        private void ClearStrokes(bool isErasedByCode)
        {

            _currentCommitType = CommitReason.ClearingCanvas;
            if (isErasedByCode) _currentCommitType = CommitReason.CodeInput;
            inkCanvas.Strokes.Clear();
            _currentCommitType = CommitReason.UserInput;
        }

        private void RestoreStrokes(bool isBackupMain = false)
        {
            try
            {
                if (TimeMachineHistories[CurrentWhiteboardIndex] == null) return; //防止白板打开后不居中
                if (isBackupMain)
                {
                    _currentCommitType = CommitReason.CodeInput;
                    timeMachine.ImportTimeMachineHistory(TimeMachineHistories[0]);
                    foreach (var item in TimeMachineHistories[0])
                    {
                        if (item.CommitType == TimeMachineHistoryType.UserInput)
                        {
                            if (!item.StrokeHasBeenCleared)
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                            }
                            else
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                            }
                        }
                        else if (item.CommitType == TimeMachineHistoryType.ShapeRecognition)
                        {
                            if (item.StrokeHasBeenCleared)
                            {

                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                            }
                            else
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                            }
                        }
                        else if (item.CommitType == TimeMachineHistoryType.Rotate)
                        {
                            if (item.StrokeHasBeenCleared)
                            {

                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                            }
                            else
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                            }
                        }
                        else if (item.CommitType == TimeMachineHistoryType.Clear)
                        {
                            if (!item.StrokeHasBeenCleared)
                            {
                                if (item.CurrentStroke != null)
                                {
                                    foreach (var currentStroke in item.CurrentStroke)
                                    {
                                        if (!inkCanvas.Strokes.Contains(currentStroke)) inkCanvas.Strokes.Add(currentStroke);
                                    }

                                }
                                if (item.ReplacedStroke != null)
                                {
                                    foreach (var replacedStroke in item.ReplacedStroke)
                                    {
                                        if (inkCanvas.Strokes.Contains(replacedStroke)) inkCanvas.Strokes.Remove(replacedStroke);
                                    }
                                }

                            }
                            else
                            {
                                if (item.ReplacedStroke != null)
                                {
                                    foreach (var replacedStroke in item.ReplacedStroke)
                                    {
                                        if (!inkCanvas.Strokes.Contains(replacedStroke)) inkCanvas.Strokes.Add(replacedStroke);
                                    }
                                }
                                if (item.CurrentStroke != null)
                                {
                                    foreach (var currentStroke in item.CurrentStroke)
                                    {
                                        if (inkCanvas.Strokes.Contains(currentStroke)) inkCanvas.Strokes.Remove(currentStroke);
                                    }
                                }
                            }
                        }
                        _currentCommitType = CommitReason.UserInput;
                    }
                }
                else
                {
                    _currentCommitType = CommitReason.CodeInput;
                    timeMachine.ImportTimeMachineHistory(TimeMachineHistories[CurrentWhiteboardIndex]);
                    foreach (var item in TimeMachineHistories[CurrentWhiteboardIndex])
                    {
                        if (item.CommitType == TimeMachineHistoryType.UserInput)
                        {
                            if (!item.StrokeHasBeenCleared)
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                            }
                            else
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                            }
                        }
                        else if (item.CommitType == TimeMachineHistoryType.ShapeRecognition)
                        {
                            if (item.StrokeHasBeenCleared)
                            {

                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                            }
                            else
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                            }
                        }
                        else if (item.CommitType == TimeMachineHistoryType.Rotate)
                        {
                            if (item.StrokeHasBeenCleared)
                            {

                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                            }
                            else
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                            }
                        }
                        else if (item.CommitType == TimeMachineHistoryType.Clear)
                        {
                            if (!item.StrokeHasBeenCleared)
                            {
                                if (item.CurrentStroke != null)
                                {
                                    foreach (var currentStroke in item.CurrentStroke)
                                    {
                                        if (!inkCanvas.Strokes.Contains(currentStroke)) inkCanvas.Strokes.Add(currentStroke);
                                    }

                                }
                                if (item.ReplacedStroke != null)
                                {
                                    foreach (var replacedStroke in item.ReplacedStroke)
                                    {
                                        if (inkCanvas.Strokes.Contains(replacedStroke)) inkCanvas.Strokes.Remove(replacedStroke);
                                    }
                                }

                            }
                            else
                            {
                                if (item.ReplacedStroke != null)
                                {
                                    foreach (var replacedStroke in item.ReplacedStroke)
                                    {
                                        if (!inkCanvas.Strokes.Contains(replacedStroke)) inkCanvas.Strokes.Add(replacedStroke);
                                    }
                                }
                                if (item.CurrentStroke != null)
                                {
                                    foreach (var currentStroke in item.CurrentStroke)
                                    {
                                        if (inkCanvas.Strokes.Contains(currentStroke)) inkCanvas.Strokes.Remove(currentStroke);
                                    }
                                }
                            }
                        }
                    }
                    _currentCommitType = CommitReason.UserInput;
                }
            }
            catch { }
        }

        private void BtnWhiteBoardSwitchPrevious_Click(object sender, EventArgs e)
        {
            if (CurrentWhiteboardIndex <= 1) return;

            SaveStrokes();

            ClearStrokes(true);
            CurrentWhiteboardIndex--;

            RestoreStrokes();

            UpdateIndexInfoDisplay();
        }

        private void BtnWhiteBoardSwitchNext_Click(object sender, EventArgs e)
        {
            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
            {
                SaveScreenShot(true);
                if (Settings.Automation.IsAutoSaveStrokesAtScreenshot) SaveInkCanvasStrokes(false);
            }
            if (CurrentWhiteboardIndex >= WhiteboardTotalCount)
            {
                BtnWhiteBoardAdd_Click(sender, e);
                return;
            }
            SaveStrokes();


            ClearStrokes(true);
            CurrentWhiteboardIndex++;

            RestoreStrokes();

            UpdateIndexInfoDisplay();
        }

        private void BtnWhiteBoardAdd_Click(object sender, EventArgs e)
        {
            if (WhiteboardTotalCount >= 99) return;
            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
            {
                SaveScreenShot(true);
                if (Settings.Automation.IsAutoSaveStrokesAtScreenshot) SaveInkCanvasStrokes(false);
            }
            SaveStrokes();
            ClearStrokes(true);

            WhiteboardTotalCount++;
            CurrentWhiteboardIndex++;

            if (CurrentWhiteboardIndex != WhiteboardTotalCount)
            {
                for (int i = WhiteboardTotalCount; i > CurrentWhiteboardIndex; i--)
                {
                    TimeMachineHistories[i] = TimeMachineHistories[i - 1];
                }
            }

            UpdateIndexInfoDisplay();

            if (WhiteboardTotalCount >= 99) BtnWhiteBoardAdd.IsEnabled = false;
        }

        private void BtnWhiteBoardDelete_Click(object sender, RoutedEventArgs e)
        {
            ClearStrokes(true);

            if (CurrentWhiteboardIndex != WhiteboardTotalCount)
            {
                for (int i = CurrentWhiteboardIndex; i <= WhiteboardTotalCount; i++)
                {
                    TimeMachineHistories[i] = TimeMachineHistories[i + 1];
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

        #region Simulate Pen Pressure & Ink To Shape

        StrokeCollection newStrokes = new StrokeCollection();
        List<Circle> circles = new List<Circle>();

        //此函数中的所有代码版权所有 WXRIW，在其他项目中使用前必须提前联系（wxriw@outlook.com），谢谢！
        private void inkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            try
            {
                inkCanvas.Opacity = 1;
                if (Settings.InkToShape.IsInkToShapeEnabled && !Environment.Is64BitProcess)
                {
                    void InkToShapeProcess()
                    {
                        try
                        {
                            newStrokes.Add(e.Stroke);
                            if (newStrokes.Count > 4) newStrokes.RemoveAt(0);
                            for (int i = 0; i < newStrokes.Count; i++)
                            {
                                if (!inkCanvas.Strokes.Contains(newStrokes[i])) newStrokes.RemoveAt(i--);
                            }
                            for (int i = 0; i < circles.Count; i++)
                            {
                                if (!inkCanvas.Strokes.Contains(circles[i].Stroke)) circles.RemoveAt(i);
                            }
                            var strokeReco = new StrokeCollection();
                            var result = InkRecognizeHelper.RecognizeShape(newStrokes);
                            for (int i = newStrokes.Count - 1; i >= 0; i--)
                            {
                                strokeReco.Add(newStrokes[i]);
                                var newResult = InkRecognizeHelper.RecognizeShape(strokeReco);
                                if (newResult.InkDrawingNode.GetShapeName() == "Circle" || newResult.InkDrawingNode.GetShapeName() == "Ellipse")
                                {
                                    result = newResult;
                                    break;
                                }
                                //Label.Visibility = Visibility.Visible;
                                Label.Content = circles.Count.ToString() + "\n" + newResult.InkDrawingNode.GetShapeName();
                            }
                            if (result.InkDrawingNode.GetShapeName() == "Circle")
                            {
                                var shape = result.InkDrawingNode.GetShape();
                                if (shape.Width > 75)
                                {
                                    foreach (Circle circle in circles)
                                    {
                                        //判断是否画同心圆
                                        if (Math.Abs(result.Centroid.X - circle.Centroid.X) / shape.Width < 0.12 &&
                                            Math.Abs(result.Centroid.Y - circle.Centroid.Y) / shape.Width < 0.12)
                                        {
                                            result.Centroid = circle.Centroid;
                                            break;
                                        }
                                        else
                                        {
                                            double d = (result.Centroid.X - circle.Centroid.X) * (result.Centroid.X - circle.Centroid.X) +
                                               (result.Centroid.Y - circle.Centroid.Y) * (result.Centroid.Y - circle.Centroid.Y);
                                            d = Math.Sqrt(d);
                                            //判断是否画外切圆
                                            double x = shape.Width / 2.0 + circle.R - d;
                                            if (Math.Abs(x) / shape.Width < 0.1)
                                            {
                                                double sinTheta = (result.Centroid.Y - circle.Centroid.Y) / d;
                                                double cosTheta = (result.Centroid.X - circle.Centroid.X) / d;
                                                double newX = result.Centroid.X + x * cosTheta;
                                                double newY = result.Centroid.Y + x * sinTheta;
                                                result.Centroid = new Point(newX, newY);
                                            }
                                            //判断是否画外切圆
                                            x = Math.Abs(circle.R - shape.Width / 2.0) - d;
                                            if (Math.Abs(x) / shape.Width < 0.1)
                                            {
                                                double sinTheta = (result.Centroid.Y - circle.Centroid.Y) / d;
                                                double cosTheta = (result.Centroid.X - circle.Centroid.X) / d;
                                                double newX = result.Centroid.X + x * cosTheta;
                                                double newY = result.Centroid.Y + x * sinTheta;
                                                result.Centroid = new Point(newX, newY);
                                            }
                                        }
                                    }

                                    Point iniP = new Point(result.Centroid.X - shape.Width / 2, result.Centroid.Y - shape.Height / 2);
                                    Point endP = new Point(result.Centroid.X + shape.Width / 2, result.Centroid.Y + shape.Height / 2);
                                    var pointList = GenerateEllipseGeometry(iniP, endP);
                                    var point = new StylusPointCollection(pointList);
                                    var stroke = new Stroke(point)
                                    {
                                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                                    };
                                    circles.Add(new Circle(result.Centroid, shape.Width / 2.0, stroke));
                                    SetNewBackupOfStroke();
                                    _currentCommitType = CommitReason.ShapeRecognition;
                                    inkCanvas.Strokes.Remove(result.InkDrawingNode.Strokes);
                                    inkCanvas.Strokes.Add(stroke);
                                    _currentCommitType = CommitReason.UserInput;
                                    newStrokes = new StrokeCollection();
                                }
                            }
                            else if (result.InkDrawingNode.GetShapeName().Contains("Ellipse"))
                            {
                                var shape = result.InkDrawingNode.GetShape();
                                //var shape1 = result.InkDrawingNode.GetShape();
                                //shape1.Fill = Brushes.Gray;
                                //Canvas.Children.Add(shape1);
                                var p = result.InkDrawingNode.HotPoints;
                                double a = GetDistance(p[0], p[2]) / 2; //长半轴
                                double b = GetDistance(p[1], p[3]) / 2; //短半轴
                                if (a < b)
                                {
                                    double t = a;
                                    a = b;
                                    b = t;
                                }

                                result.Centroid = new Point((p[0].X + p[2].X) / 2, (p[0].Y + p[2].Y) / 2);
                                bool needRotation = true;

                                if (shape.Width > 75 || shape.Height > 75 && p.Count == 4)
                                {
                                    Point iniP = new Point(result.Centroid.X - shape.Width / 2, result.Centroid.Y - shape.Height / 2);
                                    Point endP = new Point(result.Centroid.X + shape.Width / 2, result.Centroid.Y + shape.Height / 2);

                                    foreach (Circle circle in circles)
                                    {
                                        //判断是否画同心椭圆
                                        if (Math.Abs(result.Centroid.X - circle.Centroid.X) / a < 0.2 &&
                                            Math.Abs(result.Centroid.Y - circle.Centroid.Y) / a < 0.2)
                                        {
                                            result.Centroid = circle.Centroid;
                                            iniP = new Point(result.Centroid.X - shape.Width / 2, result.Centroid.Y - shape.Height / 2);
                                            endP = new Point(result.Centroid.X + shape.Width / 2, result.Centroid.Y + shape.Height / 2);

                                            //再判断是否与圆相切
                                            if (Math.Abs(a - circle.R) / a < 0.2)
                                            {
                                                if (shape.Width >= shape.Height)
                                                {
                                                    iniP.X = result.Centroid.X - circle.R;
                                                    endP.X = result.Centroid.X + circle.R;
                                                    iniP.Y = result.Centroid.Y - b;
                                                    endP.Y = result.Centroid.Y + b;
                                                }
                                                else
                                                {
                                                    iniP.Y = result.Centroid.Y - circle.R;
                                                    endP.Y = result.Centroid.Y + circle.R;
                                                    iniP.X = result.Centroid.X - a;
                                                    endP.X = result.Centroid.X + a;
                                                }
                                            }
                                            break;
                                        }
                                        else if (Math.Abs(result.Centroid.X - circle.Centroid.X) / a < 0.2)
                                        {
                                            double sinTheta = Math.Abs(circle.Centroid.Y - result.Centroid.Y) / circle.R;
                                            double cosTheta = Math.Sqrt(1 - sinTheta * sinTheta);
                                            double newA = circle.R * cosTheta;
                                            if (circle.R * sinTheta / circle.R < 0.9 && a / b > 2 && Math.Abs(newA - a) / newA < 0.3)
                                            {
                                                iniP.X = circle.Centroid.X - newA;
                                                endP.X = circle.Centroid.X + newA;
                                                iniP.Y = result.Centroid.Y - newA / 5;
                                                endP.Y = result.Centroid.Y + newA / 5;

                                                double topB = endP.Y - iniP.Y;

                                                SetNewBackupOfStroke();
                                                _currentCommitType = CommitReason.ShapeRecognition;
                                                inkCanvas.Strokes.Remove(result.InkDrawingNode.Strokes);
                                                newStrokes = new StrokeCollection();

                                                var _pointList = GenerateEllipseGeometry(iniP, endP, false, true);
                                                var _point = new StylusPointCollection(_pointList);
                                                var _stroke = new Stroke(_point)
                                                {
                                                    DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                                                };
                                                var _dashedLineStroke = GenerateDashedLineEllipseStrokeCollection(iniP, endP, true, false);
                                                StrokeCollection strokes = new StrokeCollection()
                                                {
                                                    _stroke,
                                                    _dashedLineStroke
                                                };
                                                inkCanvas.Strokes.Add(strokes);
                                                _currentCommitType = CommitReason.UserInput;
                                                return;
                                            }
                                        }
                                        else if (Math.Abs(result.Centroid.Y - circle.Centroid.Y) / a < 0.2)
                                        {
                                            double cosTheta = Math.Abs(circle.Centroid.X - result.Centroid.X) / circle.R;
                                            double sinTheta = Math.Sqrt(1 - cosTheta * cosTheta);
                                            double newA = circle.R * sinTheta;
                                            if (circle.R * sinTheta / circle.R < 0.9 && a / b > 2 && Math.Abs(newA - a) / newA < 0.3)
                                            {
                                                iniP.X = result.Centroid.X - newA / 5;
                                                endP.X = result.Centroid.X + newA / 5;
                                                iniP.Y = circle.Centroid.Y - newA;
                                                endP.Y = circle.Centroid.Y + newA;
                                                needRotation = false;
                                            }
                                        }
                                    }

                                    //纠正垂直与水平关系
                                    var newPoints = FixPointsDirection(p[0], p[2]);
                                    p[0] = newPoints[0];
                                    p[2] = newPoints[1];
                                    newPoints = FixPointsDirection(p[1], p[3]);
                                    p[1] = newPoints[0];
                                    p[3] = newPoints[1];

                                    var pointList = GenerateEllipseGeometry(iniP, endP);
                                    var point = new StylusPointCollection(pointList);
                                    var stroke = new Stroke(point)
                                    {
                                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                                    };

                                    if (needRotation)
                                    {
                                        Matrix m = new Matrix();
                                        FrameworkElement fe = e.Source as FrameworkElement;
                                        double tanTheta = (p[2].Y - p[0].Y) / (p[2].X - p[0].X);
                                        double theta = Math.Atan(tanTheta);
                                        m.RotateAt(theta * 180.0 / Math.PI, result.Centroid.X, result.Centroid.Y);
                                        stroke.Transform(m, false);
                                    }

                                    SetNewBackupOfStroke();
                                    _currentCommitType = CommitReason.ShapeRecognition;
                                    inkCanvas.Strokes.Remove(result.InkDrawingNode.Strokes);
                                    inkCanvas.Strokes.Add(stroke);
                                    _currentCommitType = CommitReason.UserInput;
                                    GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                                    newStrokes = new StrokeCollection();
                                }
                            }
                            else if (result.InkDrawingNode.GetShapeName().Contains("Triangle"))
                            {
                                var shape = result.InkDrawingNode.GetShape();
                                var p = result.InkDrawingNode.HotPoints;
                                if ((Math.Max(Math.Max(p[0].X, p[1].X), p[2].X) - Math.Min(Math.Min(p[0].X, p[1].X), p[2].X) >= 100 ||
                                    Math.Max(Math.Max(p[0].Y, p[1].Y), p[2].Y) - Math.Min(Math.Min(p[0].Y, p[1].Y), p[2].Y) >= 100) && result.InkDrawingNode.HotPoints.Count == 3)
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
                                    //pointList.Add(p[0]);
                                    var point = new StylusPointCollection(pointList);
                                    var stroke = new Stroke(GenerateFakePressureTriangle(point))
                                    {
                                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                                    };
                                    SetNewBackupOfStroke();
                                    _currentCommitType = CommitReason.ShapeRecognition;
                                    inkCanvas.Strokes.Remove(result.InkDrawingNode.Strokes);
                                    inkCanvas.Strokes.Add(stroke);
                                    _currentCommitType = CommitReason.UserInput;
                                    GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                                    newStrokes = new StrokeCollection();
                                }
                            }
                            else if (result.InkDrawingNode.GetShapeName().Contains("Rectangle") ||
                                     result.InkDrawingNode.GetShapeName().Contains("Diamond") ||
                                     result.InkDrawingNode.GetShapeName().Contains("Parallelogram") ||
                                     result.InkDrawingNode.GetShapeName().Contains("Square"))
                            {
                                var shape = result.InkDrawingNode.GetShape();
                                var p = result.InkDrawingNode.HotPoints;
                                if ((Math.Max(Math.Max(Math.Max(p[0].X, p[1].X), p[2].X), p[3].X) - Math.Min(Math.Min(Math.Min(p[0].X, p[1].X), p[2].X), p[3].X) >= 100 ||
                                    Math.Max(Math.Max(Math.Max(p[0].Y, p[1].Y), p[2].Y), p[3].Y) - Math.Min(Math.Min(Math.Min(p[0].Y, p[1].Y), p[2].Y), p[3].Y) >= 100) && result.InkDrawingNode.HotPoints.Count == 4)
                                {
                                    //纠正垂直与水平关系
                                    var newPoints = FixPointsDirection(p[0], p[1]);
                                    p[0] = newPoints[0];
                                    p[1] = newPoints[1];
                                    newPoints = FixPointsDirection(p[1], p[2]);
                                    p[1] = newPoints[0];
                                    p[2] = newPoints[1];
                                    newPoints = FixPointsDirection(p[2], p[3]);
                                    p[2] = newPoints[0];
                                    p[3] = newPoints[1];
                                    newPoints = FixPointsDirection(p[3], p[0]);
                                    p[3] = newPoints[0];
                                    p[0] = newPoints[1];

                                    var pointList = p.ToList();
                                    pointList.Add(p[0]);
                                    var point = new StylusPointCollection(pointList);
                                    var stroke = new Stroke(GenerateFakePressureRectangle(point))
                                    {
                                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                                    };
                                    SetNewBackupOfStroke();
                                    _currentCommitType = CommitReason.ShapeRecognition;
                                    inkCanvas.Strokes.Remove(result.InkDrawingNode.Strokes);
                                    inkCanvas.Strokes.Add(stroke);
                                    _currentCommitType = CommitReason.UserInput;
                                    GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                                    newStrokes = new StrokeCollection();
                                }
                            }
                        }
                        catch { }
                    }
                    InkToShapeProcess();
                }

                // 检查是否是压感笔书写
                foreach (StylusPoint stylusPoint in e.Stroke.StylusPoints)
                {
                    if (stylusPoint.PressureFactor != 0.5 && stylusPoint.PressureFactor != 0)
                    {
                        return;
                    }
                }


                try
                {
                    if (e.Stroke.StylusPoints.Count > 3)
                    {
                        Random random = new Random();
                        double _speed = GetPointSpeed(e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint(), e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint(), e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint());

                        RandWindow.randSeed = (int)(_speed * 100000 * 1000);
                    }
                }
                catch { }

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
                            if (n == 1) return;
                            if (n >= x)
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
                                x = (int)(1000 / k); // 取 1000 ms 内的点
                            }

                            if (n >= x)
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
            catch { }
        }

        private void SetNewBackupOfStroke()
        {
            lastTouchDownStrokeCollection = inkCanvas.Strokes.Clone();
            int whiteboardIndex = CurrentWhiteboardIndex;
            if (currentMode == 0)
            {
                whiteboardIndex = 0;
            }
            strokeCollections[whiteboardIndex] = lastTouchDownStrokeCollection;
        }

        public double GetDistance(Point point1, Point point2)
        {
            return Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) + (point1.Y - point2.Y) * (point1.Y - point2.Y));
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
            else if (Math.Abs(p1.Y - p2.Y) / Math.Abs(p1.X - p2.X) > 8)
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

        public StylusPointCollection GenerateFakePressureTriangle(StylusPointCollection points)
        {
            var newPoint = new StylusPointCollection();
            newPoint.Add(new StylusPoint(points[0].X, points[0].Y, (float)0.4));
            var cPoint = GetCenterPoint(points[0], points[1]);
            newPoint.Add(new StylusPoint(cPoint.X, cPoint.Y, (float)0.8));
            newPoint.Add(new StylusPoint(points[1].X, points[1].Y, (float)0.4));
            newPoint.Add(new StylusPoint(points[1].X, points[1].Y, (float)0.4));
            cPoint = GetCenterPoint(points[1], points[2]);
            newPoint.Add(new StylusPoint(cPoint.X, cPoint.Y, (float)0.8));
            newPoint.Add(new StylusPoint(points[2].X, points[2].Y, (float)0.4));
            newPoint.Add(new StylusPoint(points[2].X, points[2].Y, (float)0.4));
            cPoint = GetCenterPoint(points[2], points[0]);
            newPoint.Add(new StylusPoint(cPoint.X, cPoint.Y, (float)0.8));
            newPoint.Add(new StylusPoint(points[0].X, points[0].Y, (float)0.4));
            return newPoint;
        }

        public StylusPointCollection GenerateFakePressureRectangle(StylusPointCollection points)
        {
            var newPoint = new StylusPointCollection();
            newPoint.Add(new StylusPoint(points[0].X, points[0].Y, (float)0.4));
            var cPoint = GetCenterPoint(points[0], points[1]);
            newPoint.Add(new StylusPoint(cPoint.X, cPoint.Y, (float)0.8));
            newPoint.Add(new StylusPoint(points[1].X, points[1].Y, (float)0.4));
            newPoint.Add(new StylusPoint(points[1].X, points[1].Y, (float)0.4));
            cPoint = GetCenterPoint(points[1], points[2]);
            newPoint.Add(new StylusPoint(cPoint.X, cPoint.Y, (float)0.8));
            newPoint.Add(new StylusPoint(points[2].X, points[2].Y, (float)0.4));
            newPoint.Add(new StylusPoint(points[2].X, points[2].Y, (float)0.4));
            cPoint = GetCenterPoint(points[2], points[3]);
            newPoint.Add(new StylusPoint(cPoint.X, cPoint.Y, (float)0.8));
            newPoint.Add(new StylusPoint(points[3].X, points[3].Y, (float)0.4));
            newPoint.Add(new StylusPoint(points[3].X, points[3].Y, (float)0.4));
            cPoint = GetCenterPoint(points[3], points[0]);
            newPoint.Add(new StylusPoint(cPoint.X, cPoint.Y, (float)0.8));
            newPoint.Add(new StylusPoint(points[0].X, points[0].Y, (float)0.4));
            return newPoint;
        }

        public Point GetCenterPoint(Point point1, Point point2)
        {
            return new Point((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2);
        }

        public StylusPoint GetCenterPoint(StylusPoint point1, StylusPoint point2)
        {
            return new StylusPoint((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2);
        }

        #endregion

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

        #region Auto Theme

        Color toolBarForegroundColor = Color.FromRgb(102, 102, 102);
        private void SetTheme(string theme)
        {
            if (theme == "Light")
            {
                ResourceDictionary rd1 = new ResourceDictionary() { Source = new Uri("Resources/Styles/Light.xaml", UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries.Add(rd1);

                ResourceDictionary rd2 = new ResourceDictionary() { Source = new Uri("Resources/DrawShapeImageDictionary.xaml", UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries.Add(rd2);

                ResourceDictionary rd3 = new ResourceDictionary() { Source = new Uri("Resources/SeewoImageDictionary.xaml", UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries.Add(rd3);

                ResourceDictionary rd4 = new ResourceDictionary() { Source = new Uri("Resources/IconImageDictionary.xaml", UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries.Add(rd4);

                ThemeManager.SetRequestedTheme(window, ElementTheme.Light);

                toolBarForegroundColor = (Color)Application.Current.FindResource("ToolBarForegroundColor");
            }
            else if (theme == "Dark")
            {
                ResourceDictionary rd1 = new ResourceDictionary() { Source = new Uri("Resources/Styles/Dark.xaml", UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries.Add(rd1);

                ResourceDictionary rd2 = new ResourceDictionary() { Source = new Uri("Resources/DrawShapeImageDictionary.xaml", UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries.Add(rd2);

                ResourceDictionary rd3 = new ResourceDictionary() { Source = new Uri("Resources/SeewoImageDictionary.xaml", UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries.Add(rd3);

                ResourceDictionary rd4 = new ResourceDictionary() { Source = new Uri("Resources/IconImageDictionary.xaml", UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries.Add(rd4);

                ThemeManager.SetRequestedTheme(window, ElementTheme.Dark);

                toolBarForegroundColor = (Color)Application.Current.FindResource("ToolBarForegroundColor");
            }

            SymbolIconSelect.Foreground = new SolidColorBrush(toolBarForegroundColor);
            SymbolIconDelete.Foreground = new SolidColorBrush(toolBarForegroundColor);
        }

        private void SystemEvents_UserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            switch (Settings.Appearance.Theme)
            {
                case 0:
                    SetTheme("Light");
                    break;
                case 1:
                    SetTheme("Dark");
                    break;
                case 2:
                    if (IsSystemThemeLight()) SetTheme("Light");
                    else SetTheme("Dark");
                    break;
            }
        }

        private bool IsSystemThemeLight()
        {
            bool light = false;
            try
            {
                RegistryKey registryKey = Registry.CurrentUser;
                RegistryKey themeKey = registryKey.OpenSubKey("software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
                int keyValue = 0;
                if (themeKey != null)
                {
                    keyValue = (int)themeKey.GetValue("SystemUsesLightTheme");
                }
                if (keyValue == 1) light = true;
            }
            catch { }
            return light;
        }
        #endregion

        #endregion Functions

        #region Screenshot

        private void BtnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            bool isHideNotification = false;
            if (sender is bool) isHideNotification = (bool)sender;

            GridNotifications.Visibility = Visibility.Collapsed;

            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(20);
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                            SaveScreenShot(isHideNotification, $"{pptName}/{previousSlideID}_{DateTime.Now:HH-mm-ss}");
                        else
                            SaveScreenShot(isHideNotification);
                    });
                }
                catch
                {
                    if (!isHideNotification)
                    {
                        ShowNotification("截图保存失败");
                    }
                }

                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (inkCanvas.Visibility != Visibility.Visible || inkCanvas.Strokes.Count == 0) return;
                        SaveInkCanvasStrokes(false);
                    });
                }
                catch { }

                if (isHideNotification)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        BtnClear_Click(BtnClear, null);
                    });
                }
            })).Start();
        }

        private void SaveScreenShot(bool isHideNotification, string fileName = null)
        {
            System.Drawing.Rectangle rc = System.Windows.Forms.SystemInformation.VirtualScreen;
            var bitmap = new System.Drawing.Bitmap(rc.Width, rc.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (System.Drawing.Graphics memoryGrahics = System.Drawing.Graphics.FromImage(bitmap))
            {
                memoryGrahics.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, System.Drawing.CopyPixelOperation.SourceCopy);
            }

            if (Settings.Automation.IsSaveScreenshotsInDateFolders)
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = DateTime.Now.ToString("HH-mm-ss");
                var savePath =
                    $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)}\Ink Canvas Screenshots\{DateTime.Now.Date:yyyyMMdd}\{fileName}.png";


                if (!Directory.Exists(Path.GetDirectoryName(savePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                }

                bitmap.Save(savePath, ImageFormat.Png);

                if (!isHideNotification)
                {
                    ShowNotification("截图成功保存至 " + savePath);
                }
            }
            else
            {
                if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Ink Canvas Screenshots"))
                {
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) +
                                              @"\Ink Canvas Screenshots");
                }

                bitmap.Save(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) +
                            @"\Ink Canvas Screenshots\" + DateTime.Now.ToString("u").Replace(':', '-') + ".png", ImageFormat.Png);

                if (!isHideNotification)
                {
                    ShowNotification("截图成功保存至 " + Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) +
                                     @"\Ink Canvas Screenshots\" + DateTime.Now.ToString("u").Replace(':', '-') + ".png");
                }
            }
        }

        #endregion

        #region Notification

        int lastNotificationShowTime = 0;
        int notificationShowTime = 2500;

        public static void ShowNewMessage(string notice, bool isShowImmediately = true)
        {
            (Application.Current?.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow)?.ShowNotification(notice, isShowImmediately);
        }

        public void ShowNotification(string notice, bool isShowImmediately = true)
        {
            lastNotificationShowTime = Environment.TickCount;

            GridNotifications.Visibility = Visibility.Visible;
            //GridNotifications.Opacity = 1;
            TextBlockNotice.Text = notice;

            new Thread(new ThreadStart(() =>
            {
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

        private void AppendNotification(string notice)
        {
            TextBlockNotice.Text = TextBlockNotice.Text + Environment.NewLine + notice;
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
            new CountdownTimerWindow().Show();
        }

        private void BtnRand_Click(object sender, RoutedEventArgs e)
        {
            StackPanelToolButtons.Visibility = Visibility.Collapsed;
            new RandWindow().Show();
        }

        #endregion Tools

        #region Float Bar

        private void HideSubPanels()
        {
            BorderClearInDelete.Visibility = Visibility.Collapsed;
            BorderTools.Visibility = Visibility.Collapsed;
        }

        private void BorderPenColorBlack_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnColorBlack_Click(BtnColorBlack, null);
            HideSubPanels();
        }

        private void BorderPenColorRed_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnColorRed_Click(BtnColorRed, null);
            HideSubPanels();
        }

        private void BorderPenColorGreen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnColorGreen_Click(BtnColorGreen, null);
            HideSubPanels();
        }

        private void BorderPenColorBlue_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnColorBlue_Click(BtnColorBlue, null);
            HideSubPanels();
        }

        private void BorderPenColorYellow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnColorYellow_Click(BtnColorYellow, null);
            HideSubPanels();
        }

        private void BorderPenColorWhite_MouseUp(object sender, MouseButtonEventArgs e)
        {
            inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FFFEFEFE");
            inkColor = 5;
            ColorSwitchCheck();
            HideSubPanels();
        }

        private void SymbolIconUndo_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnUndo_Click(BtnUndo, null);
            HideSubPanels();
        }

        private void SymbolIconRedo_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnRedo_Click(BtnRedo, null);
            HideSubPanels();
        }

        private async void SymbolIconCursor_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode != 0)
            {
                ImageBlackboard_MouseUp(null, null);
            }
            else
            {
                BtnHideInkCanvas_Click(BtnHideInkCanvas, null);

                if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                {
                    if (ViewboxFloatingBar.Margin == new Thickness((SystemParameters.PrimaryScreenWidth - ViewboxFloatingBar.ActualWidth) / 2, SystemParameters.PrimaryScreenHeight - 60, -2000, -200))
                    {
                        await Task.Delay(100);
                        ViewboxFloatingBar.Margin = new Thickness((SystemParameters.PrimaryScreenWidth - ViewboxFloatingBar.ActualWidth) / 2, SystemParameters.PrimaryScreenHeight - 60, -2000, -200);
                    }
                }
            }
        }

        private void SymbolIconDelete_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender != lastBorderMouseDownObject) return;
            if (inkCanvas.GetSelectedStrokes().Count > 0)
            {
                inkCanvas.Strokes.Remove(inkCanvas.GetSelectedStrokes());
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }
            else if (inkCanvas.Strokes.Count > 0)
            {
                if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
                {
                    if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                        SaveScreenShot(true, $"{pptName}/{previousSlideID}_{DateTime.Now:HH-mm-ss}");
                    else
                        SaveScreenShot(true);
                }
                BtnClear_Click(BtnClear, null);
            }
            else
            {
                if (currentMode == 0 && BtnPPTSlideShowEnd.Visibility != Visibility.Visible)
                {
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }
            }
        }

        private void SymbolIconSettings_Click(object sender, RoutedEventArgs e)
        {
            BtnSettings_Click(BtnSettings, null);
            HideSubPanels();
        }

        private void SymbolIconSelect_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnSelect_Click(BtnSelect, null);

            ImageEraser.Visibility = Visibility.Visible;
            ViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorWhiteContent.Visibility = Visibility.Collapsed;

            HideSubPanels();
        }

        private void SymbolIconScreenshot_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnScreenshot_Click(BtnScreenshot, null);
        }

        Point pointDesktop = new Point(-1, -1); //用于记录上次进入PPT或白板时的坐标
        Point pointPPT = new Point(-1, -1); //用于记录上次在PPT中打开白板时的坐标

        private void ImageBlackboard_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (currentMode == 0)
            {
                //进入黑板
                Topmost = false;

                if (BtnPPTSlideShowEnd.Visibility == Visibility.Collapsed)
                {
                    pointDesktop = new Point(ViewboxFloatingBar.Margin.Left, ViewboxFloatingBar.Margin.Top);
                }
                else
                {
                    pointPPT = new Point(ViewboxFloatingBar.Margin.Left, ViewboxFloatingBar.Margin.Top);
                }
                //ViewboxFloatingBar.Margin = new Thickness(10, SystemParameters.PrimaryScreenHeight - 60, -2000, -200);

                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(100);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewboxFloatingBar.Margin = new Thickness((SystemParameters.PrimaryScreenWidth - ViewboxFloatingBar.ActualWidth) / 2, SystemParameters.PrimaryScreenHeight - 60, -2000, -200);
                    });
                })).Start();
                if (Settings.Canvas.UsingWhiteboard)
                {
                    BorderPenColorBlack_MouseUp(BorderPenColorBlack, null);
                }
                else
                {
                    BorderPenColorWhite_MouseUp(BorderPenColorWhite, null);
                }
            }
            else
            {
                //关闭黑板
                Topmost = true;

                if (isInMultiTouchMode) BorderMultiTouchMode_MouseUp(null, null);

                if (BtnPPTSlideShowEnd.Visibility == Visibility.Collapsed)
                {
                    if (pointDesktop != new Point(-1, -1))
                    {
                        ViewboxFloatingBar.Margin = new Thickness(pointDesktop.X, pointDesktop.Y, -2000, -200);
                        pointDesktop = new Point(-1, -1);
                    }
                }
                else
                {
                    new Thread(new ThreadStart(() =>
                    {
                        Thread.Sleep(100);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ViewboxFloatingBar.Margin = new Thickness((SystemParameters.PrimaryScreenWidth - ViewboxFloatingBar.ActualWidth) / 2, SystemParameters.PrimaryScreenHeight - 60, -2000, -200);
                        });
                    })).Start();
                }
                BorderPenColorRed_MouseUp(BorderPenColorRed, null);
            }

            BtnSwitch_Click(BtnSwitch, null);

            if (currentMode == 0 && inkCanvas.Strokes.Count == 0 && BtnPPTSlideShowEnd.Visibility != Visibility.Visible)
            {
                BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
            }

            BtnExit.Foreground = Brushes.White;
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
        }

        private void ImageEraser_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnErase_Click(BtnErase, e);

            ViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorWhiteContent.Visibility = Visibility.Collapsed;

            HideSubPanels();
        }

        private void ImageCountdownTimer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            BorderTools.Visibility = Visibility.Collapsed;
            BtnCountdownTimer_Click(BtnCountdownTimer, null);
        }

        private void SymbolIconRand_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            BorderTools.Visibility = Visibility.Collapsed;
            BtnRand_Click(BtnRand, null);
        }

        private void SymbolIconRandOne_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            BorderTools.Visibility = Visibility.Collapsed;
            new RandWindow(true).ShowDialog();
        }

        private void GridInkReplayButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            BorderTools.Visibility = Visibility.Collapsed;
            BorderDrawShape.Visibility = Visibility.Collapsed;

            InkCanvasForInkReplay.Visibility = Visibility.Visible;
            inkCanvas.Visibility = Visibility.Collapsed;
            isStopInkReplay = false;
            InkCanvasForInkReplay.Strokes.Clear();
            StrokeCollection strokes = inkCanvas.Strokes.Clone();
            if (inkCanvas.GetSelectedStrokes().Count != 0)
            {
                strokes = inkCanvas.GetSelectedStrokes().Clone();
            }
            int k = 1, i = 0;
            new Thread(new ThreadStart(() =>
            {
                foreach (Stroke stroke in strokes)
                {
                    //Thread.Sleep(100);
                    //Application.Current.Dispatcher.Invoke(() =>
                    //{
                    //    InkCanvasForInkReplay.Strokes.Add(stroke);
                    //});
                    StylusPointCollection stylusPoints = new StylusPointCollection();
                    if (stroke.StylusPoints.Count == 629) //圆或椭圆
                    {
                        Stroke s = null;
                        foreach (StylusPoint stylusPoint in stroke.StylusPoints)
                        {
                            if (i++ >= 50)
                            {
                                i = 0;
                                Thread.Sleep(10);
                                if (isStopInkReplay) return;
                            }
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    InkCanvasForInkReplay.Strokes.Remove(s);
                                }
                                catch { }
                                stylusPoints.Add(stylusPoint);
                                s = new Stroke(stylusPoints.Clone());
                                s.DrawingAttributes = stroke.DrawingAttributes;
                                InkCanvasForInkReplay.Strokes.Add(s);
                            });
                        }
                    }
                    else
                    {
                        Stroke s = null;
                        foreach (StylusPoint stylusPoint in stroke.StylusPoints)
                        {
                            if (i++ >= k)
                            {
                                i = 0;
                                Thread.Sleep(10);
                                if (isStopInkReplay) return;
                            }
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    InkCanvasForInkReplay.Strokes.Remove(s);
                                }
                                catch { }
                                stylusPoints.Add(stylusPoint);
                                s = new Stroke(stylusPoints.Clone());
                                s.DrawingAttributes = stroke.DrawingAttributes;
                                InkCanvasForInkReplay.Strokes.Add(s);
                            });
                        }
                    }
                }
                Thread.Sleep(100);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
                    inkCanvas.Visibility = Visibility.Visible;
                });
            })).Start();
        }
        bool isStopInkReplay = false;
        private void InkCanvasForInkReplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
                inkCanvas.Visibility = Visibility.Visible;
                isStopInkReplay = true;
            }
        }

        private void SymbolIconTools_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (BorderTools.Visibility == Visibility.Visible)
            {
                BorderTools.Visibility = Visibility.Collapsed;
            }
            else
            {
                BorderTools.Visibility = Visibility.Visible;
            }
        }

        #region Drag

        bool isDragDropInEffect = false;
        Point pos = new Point();
        Point downPos = new Point();

        void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragDropInEffect)
            {
                FrameworkElement currEle = sender as FrameworkElement;
                double xPos = e.GetPosition(null).X - pos.X + currEle.Margin.Left;
                double yPos = e.GetPosition(null).Y - pos.Y + currEle.Margin.Top;
                currEle.Margin = new Thickness(xPos, yPos, 0, 0);
                pos = e.GetPosition(null);
            }
        }

        void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            FrameworkElement fEle = sender as FrameworkElement;
            isDragDropInEffect = true;
            pos = e.GetPosition(null);
            fEle.CaptureMouse();
            fEle.Cursor = Cursors.Hand;
        }

        void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragDropInEffect)
            {
                FrameworkElement ele = sender as FrameworkElement;
                isDragDropInEffect = false;
                ele.ReleaseMouseCapture();
            }
        }


        void SymbolIconEmoji_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragDropInEffect)
            {
                double xPos = e.GetPosition(null).X - pos.X + ViewboxFloatingBar.Margin.Left;
                double yPos = e.GetPosition(null).Y - pos.Y + ViewboxFloatingBar.Margin.Top;
                ViewboxFloatingBar.Margin = new Thickness(xPos, yPos, -2000, -200);
                pos = e.GetPosition(null);
            }
        }

        void SymbolIconEmoji_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragDropInEffect = true;
            pos = e.GetPosition(null);
            downPos = e.GetPosition(null);
            GridForFloatingBarDraging.Visibility = Visibility.Visible;

            SymbolIconEmoji.Symbol = ModernWpf.Controls.Symbol.Emoji;
        }

        void SymbolIconEmoji_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragDropInEffect = false;

            if (e is null || (downPos.X == e.GetPosition(null).X && downPos.Y == e.GetPosition(null).Y))
            {
                if (BorderFloatingBarMainControls.Visibility == Visibility.Visible)
                {
                    BorderFloatingBarMainControls.Visibility = Visibility.Collapsed;
                }
                else
                {
                    BorderFloatingBarMainControls.Visibility = Visibility.Visible;
                }
            }

            GridForFloatingBarDraging.Visibility = Visibility.Collapsed;
            SymbolIconEmoji.Symbol = ModernWpf.Controls.Symbol.Emoji2;
        }

        #endregion


        private void GridPPTControlPrevious_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnPPTSlidesUp_Click(BtnPPTSlidesUp, null);
        }

        private void GridPPTControlNext_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnPPTSlidesDown_Click(BtnPPTSlidesDown, null);
        }

        private void ImagePPTControlEnd_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnPPTSlideShowEnd_Click(BtnPPTSlideShowEnd, null);
        }

        #endregion

        #region Save & Open

        private void SymbolIconSaveStrokes_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender || inkCanvas.Visibility != Visibility.Visible) return;

            BorderTools.Visibility = Visibility.Collapsed;

            GridNotifications.Visibility = Visibility.Collapsed;

            SaveInkCanvasStrokes();
        }

        private void SaveInkCanvasStrokes(bool newNotice = true)
        {
            try
            {
                if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Ink Canvas Strokes\User Saved"))
                {
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Ink Canvas Strokes\User Saved");
                }

                FileStream fs = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                    @"\Ink Canvas Strokes\User Saved\" + DateTime.Now.ToString("u").Replace(':', '-') + ".icstk", FileMode.Create); //Ink Canvas STroKes
                inkCanvas.Strokes.Save(fs);

                if (newNotice)
                {
                    ShowNotification("墨迹成功保存至 " + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                        @"\Ink Canvas Strokes\User Saved\" + DateTime.Now.ToString("u").Replace(':', '-') + ".icstk");
                }
                else
                {
                    AppendNotification("墨迹成功保存至 " + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                        @"\Ink Canvas Strokes\User Saved\" + DateTime.Now.ToString("u").Replace(':', '-') + ".icstk");
                }
            }
            catch
            {
                ShowNotification("墨迹保存失败");
            }
        }

        private void SymbolIconOpenStrokes_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            BorderTools.Visibility = Visibility.Collapsed;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            string defaultFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Ink Canvas Strokes\User Saved";
            if (Directory.Exists(defaultFolderPath))
            {
                openFileDialog.InitialDirectory = defaultFolderPath;
            }
            openFileDialog.Title = "打开墨迹文件";
            openFileDialog.Filter = "Ink Canvas Strokes File (*.icstk)|*.icstk";
            if (openFileDialog.ShowDialog() == true)
            {
                LogHelper.WriteLogToFile(string.Format("Strokes Insert: Name: {0}", openFileDialog.FileName), LogHelper.LogType.Event);
                try
                {
                    var fileStreamHasNoStroke = false;
                    using (var fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                    {
                        var strokes = new StrokeCollection(fs);
                        fileStreamHasNoStroke = strokes.Count == 0;
                        if (!fileStreamHasNoStroke)
                        {
                            ClearStrokes(true);
                            timeMachine.ClearStrokeHistory();
                            inkCanvas.Strokes.Add(strokes);
                            LogHelper.NewLog(string.Format("Strokes Insert: Strokes Count: {0}", inkCanvas.Strokes.Count.ToString()));
                        }
                    }
                    if (fileStreamHasNoStroke)
                    {
                        using (var ms = new MemoryStream(File.ReadAllBytes(openFileDialog.FileName)))
                        {
                            ms.Seek(0, SeekOrigin.Begin);
                            var strokes = new StrokeCollection(ms);
                            ClearStrokes(true);
                            timeMachine.ClearStrokeHistory();
                            inkCanvas.Strokes.Add(strokes);
                            LogHelper.NewLog(string.Format("Strokes Insert (2): Strokes Count: {0}", strokes.Count.ToString()));
                        }
                    }

                    if (inkCanvas.Visibility != Visibility.Visible)
                    {
                        SymbolIconCursor_Click(sender, null);
                    }
                }
                catch
                {
                    ShowNotification("墨迹打开失败");
                }
            }
        }



        #endregion

        #region Multi-finger Inking


        #endregion

    }

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
