using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ink_Canvas
{
    public partial class SettingsPage : UserControl
    {
        private void SCManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e) => ForwardToMainWindow(nameof(SCManipulationBoundaryFeedback), sender, e);
        private void BtnRestart_Click(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(BtnRestart_Click), sender, e);
        private void BtnExit_Click(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(BtnExit_Click), sender, e);
        private void ToggleSwitchRunAtStartup_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchRunAtStartup_Toggled), sender, e);
        private void ToggleSwitchAutoHideCanvas_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchAutoHideCanvas_Toggled), sender, e);
        private void ToggleSwitchAutoEnterModeFinger_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchAutoEnterModeFinger_Toggled), sender, e);
        private void InkWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ForwardToMainWindow(nameof(InkWidthSlider_ValueChanged), sender, e);
        private void ToggleSwitchShowCursor_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchShowCursor_Toggled), sender, e);
        private void ComboBoxPenStyle_SelectionChanged(object sender, SelectionChangedEventArgs e) => ForwardToMainWindow(nameof(ComboBoxPenStyle_SelectionChanged), sender, e);
        private void ComboBoxEraserType_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => ForwardToMainWindow(nameof(ComboBoxEraserType_OnSelectionChanged), sender, e);
        private void ComboBoxEraserSize_SelectionChanged(object sender, SelectionChangedEventArgs e) => ForwardToMainWindow(nameof(ComboBoxEraserSize_SelectionChanged), sender, e);
        private void ToggleSwitchHideStrokeWhenSelecting_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchHideStrokeWhenSelecting_Toggled), sender, e);
        private void ToggleSwitchUsingWhiteboard_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchUsingWhiteboard_Toggled), sender, e);
        private void ComboBoxHyperbolaAsymptoteOption_SelectionChanged(object sender, SelectionChangedEventArgs e) => ForwardToMainWindow(nameof(ComboBoxHyperbolaAsymptoteOption_SelectionChanged), sender, e);
        private void ToggleSwitchDisableLockSmithByDefault_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchDisableLockSmithByDefault_Toggled), sender, e);
        private void ToggleSwitchEnableTwoFingerZoom_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchEnableTwoFingerZoom_Toggled), sender, e);
        private void ToggleSwitchEnableTwoFingerTranslate_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchEnableTwoFingerTranslate_Toggled), sender, e);
        private void ToggleSwitchEnableTwoFingerRotation_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchEnableTwoFingerRotation_Toggled), sender, e);
        private void ToggleSwitchEnableInkToShape_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchEnableInkToShape_Toggled), sender, e);
        private void ToggleSwitchTransparentButtonBackground_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchTransparentButtonBackground_Toggled), sender, e);
        private void ToggleSwitchShowButtonExit_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchShowButtonExit_Toggled), sender, e);
        private void ToggleSwitchShowButtonEraser_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchShowButtonEraser_Toggled), sender, e);
        private void ToggleSwitchShowButtonHideControl_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchShowButtonHideControl_Toggled), sender, e);
        private void ToggleSwitchShowButtonLRSwitch_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchShowButtonLRSwitch_Toggled), sender, e);
        private void ToggleSwitchShowButtonModeFinger_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchShowButtonModeFinger_Toggled), sender, e);
        private void SideControlOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ForwardToMainWindow(nameof(SideControlOpacitySlider_ValueChanged), sender, e);
        private void ToggleSwitchShowButtonPPTNavigation_OnToggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchShowButtonPPTNavigation_OnToggled), sender, e);
        private void ComboBoxTheme_SelectionChanged(object sender, SelectionChangedEventArgs e) => ForwardToMainWindow(nameof(ComboBoxTheme_SelectionChanged), sender, e);
        private void ToggleSwitchSupportPowerPoint_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchSupportPowerPoint_Toggled), sender, e);
        private void ToggleSwitchSupportWPS_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchSupportWPS_Toggled), sender, e);
        private void ToggleSwitchShowCanvasAtNewSlideShow_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchShowCanvasAtNewSlideShow_Toggled), sender, e);
        private void ToggleSwitchEnableTwoFingerGestureInPresentationMode_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchEnableTwoFingerGestureInPresentationMode_Toggled), sender, e);
        private void ToggleSwitchEnableFingerGestureSlideShowControl_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchEnableFingerGestureSlideShowControl_Toggled), sender, e);
        private void ToggleSwitchAutoSaveScreenShotInPowerPoint_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchAutoSaveScreenShotInPowerPoint_Toggled), sender, e);
        private void ToggleSwitchAutoSaveStrokesInPowerPoint_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchAutoSaveStrokesInPowerPoint_Toggled), sender, e);
        private void ToggleSwitchNotifyPreviousPage_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchNotifyPreviousPage_Toggled), sender, e);
        private void ToggleSwitchNotifyHiddenPage_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchNotifyHiddenPage_Toggled), sender, e);
        private void ToggleSwitchNoStrokeClearInPowerPoint_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchNoStrokeClearInPowerPoint_Toggled), sender, e);
        private void ToggleSwitchShowStrokeOnSelectInPowerPoint_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchShowStrokeOnSelectInPowerPoint_Toggled), sender, e);
        private void ToggleSwitchIsSpecialScreen_OnToggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchIsSpecialScreen_OnToggled), sender, e);
        private void TouchMultiplierSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ForwardToMainWindow(nameof(TouchMultiplierSlider_ValueChanged), sender, e);
        private void BorderCalculateMultiplier_TouchDown(object sender, TouchEventArgs e) => ForwardToMainWindow(nameof(BorderCalculateMultiplier_TouchDown), sender, e);
        private void ToggleSwitchEraserBindTouchMultiplier_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchEraserBindTouchMultiplier_Toggled), sender, e);
        private void ToggleSwitchIsQuadIR_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchIsQuadIR_Toggled), sender, e);
        private void ToggleSwitchIsLogEnabled_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchIsLogEnabled_Toggled), sender, e);
        private void BtnResetToSuggestion_Click(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(BtnResetToSuggestion_Click), sender, e);
        private void BtnResetToDefault_Click(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(BtnResetToDefault_Click), sender, e);
        private void ToggleSwitchAutoKillPptService_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchAutoKillPptService_Toggled), sender, e);
        private void ToggleSwitchAutoKillEasiNote_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchAutoKillEasiNote_Toggled), sender, e);
        private void ToggleSwitchSaveScreenshotsInDateFolders_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchSaveScreenshotsInDateFolders_Toggled), sender, e);
        private void ToggleSwitchAutoSaveStrokesAtScreenshot_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchAutoSaveStrokesAtScreenshot_Toggled), sender, e);
        private void ToggleSwitchAutoSaveStrokesAtClear_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchAutoSaveStrokesAtClear_Toggled), sender, e);
        private void ToggleSwitchExitingWritingMode_Toggled(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(ToggleSwitchExitingWritingMode_Toggled), sender, e);
        private void SideControlMinimumAutomationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ForwardToMainWindow(nameof(SideControlMinimumAutomationSlider_ValueChanged), sender, e);
        private void HyperlinkSource_Click(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(HyperlinkSource_Click), sender, e);
        private void BtnSettings_Click(object sender, RoutedEventArgs e) => ForwardToMainWindow(nameof(BtnSettings_Click), sender, e);
    }
}
