using System;
using System.Media;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for StopwatchWindow.xaml
    /// </summary>
    public partial class CountdownTimerWindow : Window
    {
        public CountdownTimerWindow()
        {
            InitializeComponent();

            timer.Elapsed += Timer_Elapsed;
            timer.Interval = 50;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!isTimerRunning || isPaused)
            {
                timer.Stop();
                return;
            }

            TimeSpan timeSpan = DateTime.Now - startTime;
            TimeSpan totalTimeSpan = new TimeSpan(hour, minute, second);
            TimeSpan leftTimeSpan = totalTimeSpan - timeSpan;
            if (leftTimeSpan.Milliseconds > 0) leftTimeSpan += new TimeSpan(0, 0, 1);
            double spentTimePercent = timeSpan.TotalMilliseconds / (totalSeconds * 1000.0);
            Application.Current.Dispatcher.Invoke(() =>
            {
                ProcessBarTime.CurrentValue = 1 - spentTimePercent;
                TextBlockHour.Text = leftTimeSpan.Hours.ToString("00");
                TextBlockMinute.Text = leftTimeSpan.Minutes.ToString("00");
                TextBlockSecond.Text = leftTimeSpan.Seconds.ToString("00");
                TbCurrentTime.Text = leftTimeSpan.ToString(@"hh\:mm\:ss");
                if (spentTimePercent >= 1)
                {
                    ProcessBarTime.CurrentValue = 0;
                    TextBlockHour.Text = "00";
                    TextBlockMinute.Text = "00";
                    TextBlockSecond.Text = "00";
                    timer.Stop();
                    isTimerRunning = false;
                    SymbolIconStart.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Play;
                    BtnStartCover.Visibility = Visibility.Visible;
                    TextBlockHour.Foreground = new SolidColorBrush(StringToColor("#FF5B5D5F"));
                    BorderStopTime.Visibility = Visibility.Collapsed;
                }
            });
            if (spentTimePercent >= 1)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    //Play sound
                    player.Stream = Properties.Resources.TimerDownNotice;
                    player.Play();
                });
            }
        }

        SoundPlayer player = new SoundPlayer();

        int hour = 0;
        int minute = 1;
        int second = 0;
        int totalSeconds = 60;

        DateTime startTime = DateTime.Now;
        DateTime pauseTime = DateTime.Now;

        bool isTimerRunning = false;
        bool isPaused = false;

        Timer timer = new Timer();

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isTimerRunning) return;
            if (ProcessBarTime.Visibility == Visibility.Visible && isTimerRunning == false)
            {
                ProcessBarTime.Visibility = Visibility.Collapsed;
                GridAdjustHour.Visibility = Visibility.Visible;
                TextBlockHour.Foreground = Brushes.Black;
            }
            else
            {
                ProcessBarTime.Visibility = Visibility.Visible;
                GridAdjustHour.Visibility = Visibility.Collapsed;
                TextBlockHour.Foreground = new SolidColorBrush(StringToColor("#FF5B5D5F"));

                if (hour == 0 && minute == 0 && second == 0)
                {
                    second = 1;
                    TextBlockSecond.Text = second.ToString("00");
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            hour++;
            if (hour >= 100) hour = 0;
            TextBlockHour.Text = hour.ToString("00");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            hour += 5;
            if (hour >= 100) hour = 0;
            TextBlockHour.Text = hour.ToString("00");
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            hour--;
            if (hour < 0) hour = 99;
            TextBlockHour.Text = hour.ToString("00");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            hour -= 5;
            if (hour < 0) hour = 99;
            TextBlockHour.Text = hour.ToString("00");
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            minute++;
            if (minute >= 60) minute = 0;
            TextBlockMinute.Text = minute.ToString("00");
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            minute += 5;
            if (minute >= 60) minute = 0;
            TextBlockMinute.Text = minute.ToString("00");
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            minute--;
            if (minute < 0) minute = 59;
            TextBlockMinute.Text = minute.ToString("00");
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            minute -= 5;
            if (minute < 0) minute = 59;
            TextBlockMinute.Text = minute.ToString("00");
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            second += 5;
            if (second >= 60) second = 0;
            TextBlockSecond.Text = second.ToString("00");
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            second++;
            if (second >= 60) second = 0;
            TextBlockSecond.Text = second.ToString("00");
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            second--;
            if (second < 0) second = 59;
            TextBlockSecond.Text = second.ToString("00");
        }

        private void Button_Click_11(object sender, RoutedEventArgs e)
        {
            second -= 5;
            if (second < 0) second = 59;
            TextBlockSecond.Text = second.ToString("00");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ProcessBarTime.Visibility = Visibility.Visible;
            GridAdjustHour.Visibility = Visibility.Collapsed;
            BorderStopTime.Visibility = Visibility.Collapsed;
        }

        private void BtnFullscreen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
                SymbolIconFullscreen.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.BackToWindow;
            }
            else
            {
                WindowState = WindowState.Normal;
                SymbolIconFullscreen.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.FullScreen;
            }
        }

        private void BtnReset_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isTimerRunning)
            {
                TextBlockHour.Text = hour.ToString("00");
                TextBlockMinute.Text = minute.ToString("00");
                TextBlockSecond.Text = second.ToString("00");
                BtnResetCover.Visibility = Visibility.Visible;
                BtnStartCover.Visibility = Visibility.Collapsed;
                BorderStopTime.Visibility = Visibility.Collapsed;
                TextBlockHour.Foreground = new SolidColorBrush(StringToColor("#FF5B5D5F"));
                return;
            }
            else if (isTimerRunning && isPaused)
            {
                TextBlockHour.Text = hour.ToString("00");
                TextBlockMinute.Text = minute.ToString("00");
                TextBlockSecond.Text = second.ToString("00");
                BtnResetCover.Visibility = Visibility.Visible;
                BtnStartCover.Visibility = Visibility.Collapsed;
                BorderStopTime.Visibility = Visibility.Collapsed;
                TextBlockHour.Foreground = new SolidColorBrush(StringToColor("#FF5B5D5F"));
                SymbolIconStart.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Play;
                isTimerRunning = false;
                timer.Stop();
                isPaused = false;
                ProcessBarTime.CurrentValue = 0;
                ProcessBarTime.IsPaused = false;
            }
            else
            {
                UpdateStopTime();
                startTime = DateTime.Now;
                Timer_Elapsed(timer, null);
            }
        }

        void UpdateStopTime()
        {
            TimeSpan totalTimeSpan = new TimeSpan(hour, minute, second);
            TextBlockStopTime.Text = (startTime + totalTimeSpan).ToString("t");
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

            return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]); //#FFFFFFFF
        }

        private static byte toByte(char c)
        {
            byte b = (byte)"0123456789ABCDEF".IndexOf(c);
            return b;
        }

        private void BtnStart_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isPaused && isTimerRunning)
            {
                //继续
                startTime += DateTime.Now - pauseTime;
                ProcessBarTime.IsPaused = false;
                TextBlockHour.Foreground = Brushes.Black;
                SymbolIconStart.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Pause;
                isPaused = false;
                timer.Start();
                UpdateStopTime();
                BorderStopTime.Visibility = Visibility.Visible;
            }
            else if (isTimerRunning)
            {
                //暂停
                pauseTime = DateTime.Now;
                ProcessBarTime.IsPaused = true;
                TextBlockHour.Foreground = new SolidColorBrush(StringToColor("#FF5B5D5F"));
                SymbolIconStart.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Play;
                BorderStopTime.Visibility = Visibility.Collapsed;
                isPaused = true;
                timer.Stop();
            }
            else
            {
                //从头开始
                startTime = DateTime.Now;
                totalSeconds = ((hour * 60) + minute) * 60 + second;
                ProcessBarTime.IsPaused = false;
                TextBlockHour.Foreground = Brushes.Black;
                SymbolIconStart.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Pause;
                BtnResetCover.Visibility = Visibility.Collapsed;

                if (totalSeconds <= 10)
                {
                    timer.Interval = 20;
                }
                else if (totalSeconds <= 60)
                {
                    timer.Interval = 30;
                }
                else if (totalSeconds <= 120)
                {
                    timer.Interval = 50;
                }
                else
                {
                    timer.Interval = 100;
                }

                isPaused = false;
                isTimerRunning = true;
                timer.Start();
                UpdateStopTime();
                BorderStopTime.Visibility = Visibility.Visible;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isTimerRunning = false;
        }

        private void BtnClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private bool _isInCompact = false;

        private void BtnMinimal_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isInCompact)
            {
                Width = 1100;
                Height = 700;
                BigViewController.Visibility = Visibility.Visible;
                TbCurrentTime.Visibility = Visibility.Collapsed;

                // Set to center
                double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
                double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
                Left = (screenWidth / 2) - (Width / 2);
                Top = (screenHeight / 2) - (Height / 2);
            }
            else
            {
                if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal;
                Width = 400;
                Height = 250;
                BigViewController.Visibility = Visibility.Collapsed;
                TbCurrentTime.Visibility = Visibility.Visible;
            }

            _isInCompact = !_isInCompact;
        }

        private void WindowDragMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}