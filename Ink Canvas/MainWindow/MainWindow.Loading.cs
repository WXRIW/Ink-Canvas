using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Modern;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Application;
using File = System.IO.File;
using MessageBox = System.Windows.MessageBox;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
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
                                        SettingsGroupBoxMASEZVersion.Visibility = Visibility.Visible;
                                        SettingsTextBlockMASEZNotice.Text = str;
                                    });
                                }
                            }
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Version version = Assembly.GetExecutingAssembly().GetName().Version;
                        SettingsTextBlockVersion.Text = version.ToString();

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
                SettingsGroupBoxInkRecognition.Visibility = Visibility.Collapsed;
            }

            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            SystemEvents_UserPreferenceChanged(null, null);

            SettingsTextBlockVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LogHelper.WriteLogToFile("Ink Canvas Loaded", LogHelper.LogType.Event);

            PreloadIALibrary();

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

        private static void PreloadIALibrary()
        {
            GC.KeepAlive(typeof(InkAnalyzer));
            GC.KeepAlive(typeof(AnalysisAlternate));
            GC.KeepAlive(typeof(InkDrawingNode));
            var analyzer = new InkAnalyzer();
            analyzer.AddStrokes(new StrokeCollection() {
                new Stroke(new StylusPointCollection() {
                    new StylusPoint(114,514),
                    new StylusPoint(191,9810),
                    new StylusPoint(7,21),
                    new StylusPoint(123,789),
                })
            });
            analyzer.Analyze();
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
                SettingsToggleSwitchAutoEnterModeFinger.IsOn = true;
            }
            else
            {
                SettingsToggleSwitchAutoEnterModeFinger.IsOn = false;
            }
            if (Settings.Startup.IsAutoHideCanvas)
            {
                if (isStartup)
                {
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }
                SettingsToggleSwitchAutoHideCanvas.IsOn = true;
            }
            else
            {
                if (isStartup)
                {
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }
                SettingsToggleSwitchAutoHideCanvas.IsOn = false;
            }

            if (Settings.Appearance.IsShowEraserButton)
            {
                BtnErase.Visibility = Visibility.Visible;
                SettingsToggleSwitchShowButtonEraser.IsOn = true;
            }
            else
            {
                BtnErase.Visibility = Visibility.Collapsed;
                SettingsToggleSwitchShowButtonEraser.IsOn = false;
            }
            if (Settings.Appearance.IsShowExitButton)
            {
                BtnExit.Visibility = Visibility.Visible;
                SettingsToggleSwitchShowButtonExit.IsOn = true;
            }
            else
            {
                BtnExit.Visibility = Visibility.Collapsed;
                SettingsToggleSwitchShowButtonExit.IsOn = false;
            }

            PptNavigationBtn.Visibility =
                Settings.PowerPointSettings.IsShowPPTNavigation ? Visibility.Visible : Visibility.Collapsed;
            SettingsToggleSwitchShowButtonPPTNavigation.IsOn = Settings.PowerPointSettings.IsShowPPTNavigation;

            SettingsComboBoxTheme.SelectedIndex = Settings.Appearance.Theme;
            if (Settings.Appearance.IsShowHideControlButton)
            {
                BtnHideControl.Visibility = Visibility.Visible;
                SettingsToggleSwitchShowButtonHideControl.IsOn = true;
            }
            else
            {
                BtnHideControl.Visibility = Visibility.Collapsed;
                SettingsToggleSwitchShowButtonHideControl.IsOn = false;
            }
            if (Settings.Appearance.IsShowLRSwitchButton)
            {
                BtnSwitchSide.Visibility = Visibility.Visible;
                SettingsToggleSwitchShowButtonLRSwitch.IsOn = true;
            }
            else
            {
                BtnSwitchSide.Visibility = Visibility.Collapsed;
                SettingsToggleSwitchShowButtonLRSwitch.IsOn = false;
            }
            if (Settings.Appearance.IsShowModeFingerToggleSwitch)
            {
                StackPanelModeFinger.Visibility = Visibility.Visible;
                SettingsToggleSwitchShowButtonModeFinger.IsOn = true;
            }
            else
            {
                StackPanelModeFinger.Visibility = Visibility.Collapsed;
                SettingsToggleSwitchShowButtonModeFinger.IsOn = false;
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
                SettingsToggleSwitchSupportPowerPoint.IsOn = true;
                timerCheckPPT.Start();
            }
            else
            {
                SettingsToggleSwitchSupportPowerPoint.IsOn = false;
                timerCheckPPT.Stop();
            }
            if (Settings.PowerPointSettings.IsShowCanvasAtNewSlideShow)
            {
                SettingsToggleSwitchShowCanvasAtNewSlideShow.IsOn = true;
            }
            else
            {
                SettingsToggleSwitchShowCanvasAtNewSlideShow.IsOn = false;
            }

            if (Settings.Gesture == null)
            {
                Settings.Gesture = new Gesture();
            }
            if (Settings.Gesture.IsDisableLockSmithByDefault)
            {
                SettingsToggleSwitchDisableLockSmithByDefault.IsOn = true;
                _lockSmith = false;
                LockSmithSymbol.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Pin;
            }
            else
            {
                SettingsToggleSwitchDisableLockSmithByDefault.IsOn = false;
                _lockSmith = true;
                LockSmithSymbol.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.UnPin;
            }
            if (Settings.Gesture.IsEnableTwoFingerZoom)
            {
                SettingsToggleSwitchEnableTwoFingerZoom.IsOn = true;
            }
            else
            {
                SettingsToggleSwitchEnableTwoFingerZoom.IsOn = false;
            }
            if (Settings.Gesture.IsEnableTwoFingerTranslate)
            {
                SettingsToggleSwitchEnableTwoFingerTranslate.IsOn = true;
            }
            else
            {
                SettingsToggleSwitchEnableTwoFingerTranslate.IsOn = false;
            }
            if (Settings.Gesture.IsEnableTwoFingerRotation)
            {
                SettingsToggleSwitchEnableTwoFingerRotation.IsOn = true;
            }
            else
            {
                SettingsToggleSwitchEnableTwoFingerRotation.IsOn = false;
            }
            if (Settings.Gesture.IsEnableTwoFingerRotationOnSelection)
            {
                SettingsToggleSwitchEnableTwoFingerRotationOnSelection.IsOn = true;
            }
            else
            {
                SettingsToggleSwitchEnableTwoFingerRotationOnSelection.IsOn = false;
            }
            if (Settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode)
            {
                SettingsToggleSwitchEnableTwoFingerGestureInPresentationMode.IsOn = true;
            }
            else
            {
                SettingsToggleSwitchEnableTwoFingerGestureInPresentationMode.IsOn = false;
            }
            if (Settings.PowerPointSettings.IsEnableFingerGestureSlideShowControl)
            {
                SettingsToggleSwitchEnableFingerGestureSlideShowControl.IsOn = true;
            }
            else
            {
                SettingsToggleSwitchEnableFingerGestureSlideShowControl.IsOn = false;
            }

            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\InkCanvas" + ".lnk"))
            {
                SettingsToggleSwitchRunAtStartup.IsOn = true;
            }

            if (Settings.Canvas != null)
            {
                drawingAttributes.Height = Settings.Canvas.InkWidth;
                drawingAttributes.Width = Settings.Canvas.InkWidth;

                SettingsInkWidthSlider.Value = Settings.Canvas.InkWidth * 2;

                if (Settings.Canvas.IsShowCursor)
                {
                    SettingsToggleSwitchShowCursor.IsOn = true;
                    inkCanvas.ForceCursor = true;
                }
                else
                {
                    SettingsToggleSwitchShowCursor.IsOn = false;
                    inkCanvas.ForceCursor = false;
                }

                SettingsComboBoxPenStyle.SelectedIndex = Settings.Canvas.InkStyle;

                SettingsComboBoxEraserSize.SelectedIndex = Settings.Canvas.EraserSize;

                SettingsComboBoxHyperbolaAsymptoteOption.SelectedIndex = (int)Settings.Canvas.HyperbolaAsymptoteOption;
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
                    SettingsToggleSwitchAutoKillEasiNote.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchAutoKillEasiNote.IsOn = false;
                }

                if (Settings.Automation.IsAutoClearWhenExitingWritingMode)
                {
                    SettingsToggleSwitchClearExitingWritingMode.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchClearExitingWritingMode.IsOn = false;
                }


                if (Settings.Automation.IsAutoSaveStrokesAtClear)
                {
                    SettingsToggleSwitchAutoSaveStrokesAtClear.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchAutoSaveStrokesAtClear.IsOn = false;
                }



                if (Settings.Automation.IsAutoKillPptService)
                {
                    SettingsToggleSwitchAutoKillPptService.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchAutoKillPptService.IsOn = false;
                }

                if (Settings.Automation.IsSaveScreenshotsInDateFolders)
                {
                    SettingsToggleSwitchSaveScreenshotsInDateFolders.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchSaveScreenshotsInDateFolders.IsOn = false;
                }

                if (Settings.Automation.IsAutoSaveStrokesAtScreenshot)
                {
                    SettingsToggleSwitchAutoSaveStrokesAtScreenshot.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchAutoSaveStrokesAtScreenshot.IsOn = false;
                }

                if (Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint)
                {
                    SettingsToggleSwitchAutoSaveStrokesInPowerPoint.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchAutoSaveStrokesInPowerPoint.IsOn = false;
                }

                if (Settings.PowerPointSettings.IsNotifyPreviousPage)
                {
                    SettingsToggleSwitchNotifyPreviousPage.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchNotifyPreviousPage.IsOn = false;
                }

                if (Settings.PowerPointSettings.IsNotifyHiddenPage)
                {
                    SettingsToggleSwitchNotifyHiddenPage.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchNotifyHiddenPage.IsOn = false;
                }

                if (Settings.PowerPointSettings.IsNoClearStrokeOnSelectWhenInPowerPoint)
                {
                    SettingsToggleSwitchNoStrokeClearInPowerPoint.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchNoStrokeClearInPowerPoint.IsOn = false;
                }

                if (Settings.PowerPointSettings.IsShowStrokeOnSelectInPowerPoint)
                {
                    SettingsToggleSwitchShowStrokeOnSelectInPowerPoint.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchShowStrokeOnSelectInPowerPoint.IsOn = false;
                }

                if (Settings.PowerPointSettings.IsSupportWPS)
                {
                    SettingsToggleSwitchSupportWPS.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchSupportWPS.IsOn = false;
                }

                SettingsSideControlMinimumAutomationSlider.Value = Settings.Automation.MinimumAutomationStrokeNumber;

                if (Settings.Canvas.HideStrokeWhenSelecting)
                {
                    SettingsToggleSwitchHideStrokeWhenSelecting.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchHideStrokeWhenSelecting.IsOn = false;
                }

                if (Settings.Canvas.UsingWhiteboard)
                {
                    SettingsToggleSwitchUsingWhiteboard.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchUsingWhiteboard.IsOn = false;
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

                SettingsComboBoxEraserType.SelectedIndex = Settings.Canvas.EraserType;

                if (Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                {
                    SettingsToggleSwitchAutoSaveScreenShotInPowerPoint.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchAutoSaveScreenShotInPowerPoint.IsOn = false;
                }
            }
            else
            {
                Settings.Automation = new Automation();
            }

            if (Settings.Advanced != null)
            {
                SettingsTouchMultiplierSlider.Value = Settings.Advanced.TouchMultiplier;
                if (Settings.Advanced.IsLogEnabled)
                {
                    SettingsToggleSwitchIsLogEnabled.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchIsLogEnabled.IsOn = false;
                }
                if (Settings.Advanced.EraserBindTouchMultiplier)
                {
                    SettingsToggleSwitchEraserBindTouchMultiplier.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchEraserBindTouchMultiplier.IsOn = false;
                }

                if (Settings.Advanced.IsSpecialScreen)
                {
                    SettingsToggleSwitchIsSpecialScreen.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchIsSpecialScreen.IsOn = false;
                }
                SettingsTouchMultiplierSlider.Visibility = SettingsToggleSwitchIsSpecialScreen.IsOn ? Visibility.Visible : Visibility.Collapsed;

                SettingsToggleSwitchIsQuadIR.IsOn = Settings.Advanced.IsQuadIR;
            }
            else
            {
                Settings.Advanced = new Advanced();
            }

            if (Settings.InkToShape != null)
            {
                if (Settings.InkToShape.IsInkToShapeEnabled)
                {
                    SettingsToggleSwitchEnableInkToShape.IsOn = true;
                }
                else
                {
                    SettingsToggleSwitchEnableInkToShape.IsOn = false;
                }
            }
            else
            {
                Settings.InkToShape = new InkToShape();
            }
        }

        #endregion Definations and Loading

    }
}