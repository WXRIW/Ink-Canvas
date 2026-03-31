using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using File = System.IO.File;

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

            SettingsBorderSettings.Opacity = 0;
            SettingsBorderSettings.Visibility = Visibility.Collapsed;
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
                SettingsGroupBoxAppearance.Visibility = Visibility.Collapsed;
                ViewBoxStackPanelMain.Visibility = Visibility.Collapsed;
                ViewBoxStackPanelShapes.Visibility = Visibility.Collapsed;
                HideSubPanels();

                ViewboxFloatingBar.Margin = new Thickness((SystemParameters.WorkArea.Width - 284) / 2, SystemParameters.WorkArea.Height - 80, -2000, -200);
            }
            else
            {
                SettingsGroupBoxAppearanceNewUI.Visibility = Visibility.Collapsed;
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
    }
}