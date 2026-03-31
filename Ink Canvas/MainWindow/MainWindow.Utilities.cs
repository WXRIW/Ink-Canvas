using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Modern;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Application = System.Windows.Application;
using File = System.IO.File;
using Path = System.IO.Path;
using Point = System.Windows.Point;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
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

            if (myrp?.StatusCode != HttpStatusCode.OK)
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

        Color FloatBarForegroundColor = Color.FromRgb(102, 102, 102);
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

                FloatBarForegroundColor = (Color)Application.Current.FindResource("FloatBarForegroundColor");
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

                FloatBarForegroundColor = (Color)Application.Current.FindResource("FloatBarForegroundColor");
            }

            SymbolIconSelect.Foreground = new SolidColorBrush(FloatBarForegroundColor);
            SymbolIconDelete.Foreground = new SolidColorBrush(FloatBarForegroundColor);
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
                        if (inkCanvas.Visibility != Visibility.Visible || inkCanvas.Strokes.Count == 0 || !Settings.Automation.IsAutoSaveStrokesAtScreenshot) return;
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
            var size = System.Windows.Forms.SystemInformation.PrimaryMonitorSize;
            var rc = new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), new System.Drawing.Size(size.Width, size.Height));
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

            SetColors();
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
            ViewboxBtnColorBlackContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(ColorSwiftOpacityDurationOff)));
            ViewboxBtnColorBlueContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(ColorSwiftOpacityDurationOff)));
            ViewboxBtnColorGreenContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(ColorSwiftOpacityDurationOff)));
            ViewboxBtnColorRedContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(ColorSwiftOpacityDurationOff)));
            ViewboxBtnColorYellowContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(ColorSwiftOpacityDurationOff)));
            ViewboxBtnColorWhiteContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(ColorSwiftOpacityDurationOff)));

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

            BtnExit.Foreground = Brushes.White;
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            SetColors();
            SetColorByIndex();
            if (currentMode == 0 && inkCanvas.Strokes.Count == 0 && BtnPPTSlideShowEnd.Visibility != Visibility.Visible)
            {
                BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
            }
        }

        private void ImageEraser_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnErase_Click(BtnErase, e);

            ViewboxBtnColorBlackContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(ColorSwiftOpacityDurationOff)));
            ViewboxBtnColorBlueContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(ColorSwiftOpacityDurationOff)));
            ViewboxBtnColorGreenContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(ColorSwiftOpacityDurationOff)));
            ViewboxBtnColorRedContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(ColorSwiftOpacityDurationOff)));
            ViewboxBtnColorYellowContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(ColorSwiftOpacityDurationOff)));
            ViewboxBtnColorWhiteContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(ColorSwiftOpacityDurationOff)));

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

            SymbolIconEmoji.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Emoji;
        }

        void SymbolIconEmoji_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragDropInEffect = false;

            if (e is null || (downPos.X == e.GetPosition(null).X && downPos.Y == e.GetPosition(null).Y))
            {
                SetBorderFloatingBarMainControlsVisibility(!borderFloatingBarMainControlsVisibility);
            }

            GridForFloatingBarDraging.Visibility = Visibility.Collapsed;
            SymbolIconEmoji.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Emoji2;
        }

        bool borderFloatingBarMainControlsVisibility = true;
        void SetBorderFloatingBarMainControlsVisibility(bool isVisible, bool isAnimated = true)
        {
            borderFloatingBarMainControlsVisibility = isVisible;
            if (!isVisible)
            {
                BorderFloatingBarMainControls.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(isAnimated ? 100 : 0))
                {
                    EasingFunction = new PowerEase() { Power = 4, EasingMode = EasingMode.EaseOut },
                });
                BorderFloatingBarMainControls.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(isAnimated ? 100 : 0)));
            }
            else
            {
                BorderFloatingBarMainControls.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(isAnimated ? 160 : 0))
                {
                    EasingFunction = new PowerEase() { Power = 4, EasingMode = EasingMode.EaseOut },
                });
                BorderFloatingBarMainControls.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(isAnimated ? 160 : 0)));
            }
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
            catch (Exception ex)
            {
                ShowNotification($"墨迹保存失败：{ex.Message}");
            }
        }

        private void SymbolIconPin_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _lockSmith = !_lockSmith;
            if (_lockSmith) LockSmithSymbol.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.UnPin;
            else LockSmithSymbol.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Pin;
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

    }
}