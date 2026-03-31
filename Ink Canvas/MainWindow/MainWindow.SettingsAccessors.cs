using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        #region Settings Page Accessors

        private SettingsPage GetSettingsPage()
        {
            return FindName("SettingsPageView") as SettingsPage;
        }

        private dynamic GetSettingsElement(string name)
        {
            return GetSettingsPage()?.GetElement(name);
        }

        private dynamic SettingsBorderSettings => GetSettingsElement("BorderSettings");
        private dynamic SettingsToggleSwitchRunAtStartup => GetSettingsElement("ToggleSwitchRunAtStartup");
        private dynamic SettingsToggleSwitchAutoHideCanvas => GetSettingsElement("ToggleSwitchAutoHideCanvas");
        private dynamic SettingsToggleSwitchAutoEnterModeFinger => GetSettingsElement("ToggleSwitchAutoEnterModeFinger");
        public dynamic SettingsInkWidthSlider => GetSettingsElement("InkWidthSlider");
        private dynamic SettingsToggleSwitchShowCursor => GetSettingsElement("ToggleSwitchShowCursor");
        private dynamic SettingsComboBoxPenStyle => GetSettingsElement("ComboBoxPenStyle");
        private dynamic SettingsComboBoxEraserType => GetSettingsElement("ComboBoxEraserType");
        private dynamic SettingsComboBoxEraserSize => GetSettingsElement("ComboBoxEraserSize");
        private dynamic SettingsToggleSwitchHideStrokeWhenSelecting => GetSettingsElement("ToggleSwitchHideStrokeWhenSelecting");
        private dynamic SettingsToggleSwitchUsingWhiteboard => GetSettingsElement("ToggleSwitchUsingWhiteboard");
        private dynamic SettingsComboBoxHyperbolaAsymptoteOption => GetSettingsElement("ComboBoxHyperbolaAsymptoteOption");
        private dynamic SettingsToggleSwitchDisableLockSmithByDefault => GetSettingsElement("ToggleSwitchDisableLockSmithByDefault");
        private dynamic SettingsToggleSwitchEnableTwoFingerZoom => GetSettingsElement("ToggleSwitchEnableTwoFingerZoom");
        private dynamic SettingsToggleSwitchEnableTwoFingerTranslate => GetSettingsElement("ToggleSwitchEnableTwoFingerTranslate");
        private dynamic SettingsToggleSwitchEnableTwoFingerRotation => GetSettingsElement("ToggleSwitchEnableTwoFingerRotation");
        private dynamic SettingsToggleSwitchEnableTwoFingerRotationOnSelection => GetSettingsElement("ToggleSwitchEnableTwoFingerRotationOnSelection");
        private dynamic SettingsGroupBoxInkRecognition => GetSettingsElement("GroupBoxInkRecognition");
        private dynamic SettingsToggleSwitchEnableInkToShape => GetSettingsElement("ToggleSwitchEnableInkToShape");
        private dynamic SettingsGroupBoxAppearance => GetSettingsElement("GroupBoxAppearance");
        private dynamic SettingsToggleSwitchTransparentButtonBackground => GetSettingsElement("ToggleSwitchTransparentButtonBackground");
        private dynamic SettingsToggleSwitchShowButtonExit => GetSettingsElement("ToggleSwitchShowButtonExit");
        private dynamic SettingsToggleSwitchShowButtonEraser => GetSettingsElement("ToggleSwitchShowButtonEraser");
        private dynamic SettingsToggleSwitchShowButtonHideControl => GetSettingsElement("ToggleSwitchShowButtonHideControl");
        private dynamic SettingsToggleSwitchShowButtonLRSwitch => GetSettingsElement("ToggleSwitchShowButtonLRSwitch");
        private dynamic SettingsToggleSwitchShowButtonModeFinger => GetSettingsElement("ToggleSwitchShowButtonModeFinger");
        private dynamic SettingsSideControlOpacitySlider => GetSettingsElement("SideControlOpacitySlider");
        private dynamic SettingsGroupBoxAppearanceNewUI => GetSettingsElement("GroupBoxAppearanceNewUI");
        private dynamic SettingsToggleSwitchShowButtonPPTNavigation => GetSettingsElement("ToggleSwitchShowButtonPPTNavigation");
        private dynamic SettingsComboBoxTheme => GetSettingsElement("ComboBoxTheme");
        private dynamic SettingsToggleSwitchSupportPowerPoint => GetSettingsElement("ToggleSwitchSupportPowerPoint");
        private dynamic SettingsToggleSwitchSupportWPS => GetSettingsElement("ToggleSwitchSupportWPS");
        private dynamic SettingsToggleSwitchShowCanvasAtNewSlideShow => GetSettingsElement("ToggleSwitchShowCanvasAtNewSlideShow");
        private dynamic SettingsToggleSwitchEnableTwoFingerGestureInPresentationMode => GetSettingsElement("ToggleSwitchEnableTwoFingerGestureInPresentationMode");
        private dynamic SettingsToggleSwitchEnableFingerGestureSlideShowControl => GetSettingsElement("ToggleSwitchEnableFingerGestureSlideShowControl");
        private dynamic SettingsToggleSwitchAutoSaveScreenShotInPowerPoint => GetSettingsElement("ToggleSwitchAutoSaveScreenShotInPowerPoint");
        private dynamic SettingsToggleSwitchAutoSaveStrokesInPowerPoint => GetSettingsElement("ToggleSwitchAutoSaveStrokesInPowerPoint");
        private dynamic SettingsToggleSwitchNotifyPreviousPage => GetSettingsElement("ToggleSwitchNotifyPreviousPage");
        private dynamic SettingsToggleSwitchNotifyHiddenPage => GetSettingsElement("ToggleSwitchNotifyHiddenPage");
        private dynamic SettingsToggleSwitchNoStrokeClearInPowerPoint => GetSettingsElement("ToggleSwitchNoStrokeClearInPowerPoint");
        private dynamic SettingsToggleSwitchShowStrokeOnSelectInPowerPoint => GetSettingsElement("ToggleSwitchShowStrokeOnSelectInPowerPoint");
        private dynamic SettingsToggleSwitchIsSpecialScreen => GetSettingsElement("ToggleSwitchIsSpecialScreen");
        private dynamic SettingsTouchMultiplierSlider => GetSettingsElement("TouchMultiplierSlider");
        private dynamic SettingsTextBlockShowCalculatedMultiplier => GetSettingsElement("TextBlockShowCalculatedMultiplier");
        private dynamic SettingsToggleSwitchEraserBindTouchMultiplier => GetSettingsElement("ToggleSwitchEraserBindTouchMultiplier");
        private dynamic SettingsToggleSwitchIsQuadIR => GetSettingsElement("ToggleSwitchIsQuadIR");
        private dynamic SettingsToggleSwitchIsLogEnabled => GetSettingsElement("ToggleSwitchIsLogEnabled");
        private dynamic SettingsSymbolIconResetSuggestionComplete => GetSettingsElement("SymbolIconResetSuggestionComplete");
        private dynamic SettingsSymbolIconResetDefaultComplete => GetSettingsElement("SymbolIconResetDefaultComplete");
        private dynamic SettingsToggleSwitchAutoKillPptService => GetSettingsElement("ToggleSwitchAutoKillPptService");
        private dynamic SettingsToggleSwitchAutoKillEasiNote => GetSettingsElement("ToggleSwitchAutoKillEasiNote");
        private dynamic SettingsToggleSwitchSaveScreenshotsInDateFolders => GetSettingsElement("ToggleSwitchSaveScreenshotsInDateFolders");
        private dynamic SettingsToggleSwitchAutoSaveStrokesAtScreenshot => GetSettingsElement("ToggleSwitchAutoSaveStrokesAtScreenshot");
        private dynamic SettingsToggleSwitchAutoSaveStrokesAtClear => GetSettingsElement("ToggleSwitchAutoSaveStrokesAtClear");
        private dynamic SettingsToggleSwitchClearExitingWritingMode => GetSettingsElement("ToggleSwitchClearExitingWritingMode");
        private dynamic SettingsSideControlMinimumAutomationSlider => GetSettingsElement("SideControlMinimumAutomationSlider");
        private dynamic SettingsTextBlockVersion => GetSettingsElement("TextBlockVersion");
        private dynamic SettingsGroupBoxMASEZVersion => GetSettingsElement("GroupBoxMASEZVersion");
        private dynamic SettingsTextBlockMASEZNotice => GetSettingsElement("TextBlockMASEZNotice");

        public Visibility SettingsMasezVisibility => SettingsGroupBoxMASEZVersion?.Visibility ?? Visibility.Collapsed;

        #endregion
    }
}
