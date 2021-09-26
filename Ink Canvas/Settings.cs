using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ink_Canvas
{
    public class Settings
    {
        [JsonProperty("behavior")]
        public Behavior Behavior { get; set; } = new Behavior();
        [JsonProperty("canvas")]
        public Canvas Canvas { get; set; } = new Canvas();
        [JsonProperty("gesture")]
        public Gesture Gesture { get; set; } = new Gesture();
        [JsonProperty("startup")]
        public Startup Startup { get; set; } = new Startup();
        [JsonProperty("appearance")]
        public Appearance Appearance { get; set; } = new Appearance();
    }

    public class Behavior
    {
        [JsonProperty("powerPointSupport")]
        public bool PowerPointSupport { get; set; } = true;
        [JsonProperty("isShowCanvasAtNewSlideShow")]
        public bool IsShowCanvasAtNewSlideShow { get; set; } = true;
    }

    public class Canvas
    {
        [JsonProperty("inkWidth")]
        public double InkWidth { get; set; } = 2.5;
        [JsonProperty("isShowCursor")]
        public bool IsShowCursor { get; set; } = false;
    }

    public class Gesture
    {
        [JsonProperty("isEnableTwoFingerRotation")]
        public bool IsEnableTwoFingerRotation { get; set; } = false;
        [JsonProperty("isEnableTwoFingerGestureInPresentationMode")]
        public bool IsEnableTwoFingerGestureInPresentationMode { get; set; } = false;
    }

    public class Startup
    {
        [JsonProperty("isAutoHideCanvas")]
        public bool IsAutoHideCanvas { get; set; } = false;
        [JsonProperty("isAutoEnterModeFinger")]
        public bool IsAutoEnterModeFinger { get; set; } = false;
    }

    public class Appearance
    {
        [JsonProperty("isTransparentButtonBackground")]
        public bool IsTransparentButtonBackground { get; set; } = true;
        [JsonProperty("isShowExitButton")]
        public bool IsShowExitButton { get; set; } = true;
        [JsonProperty("isShowEraserButton")]
        public bool IsShowEraserButton { get; set; } = true;
        [JsonProperty("isShowHideControlButton")]
        public bool IsShowHideControlButton { get; set; } = false;
        [JsonProperty("isShowLRSwitchButton")]
        public bool IsShowLRSwitchButton { get; set; } = false;
        [JsonProperty("isShowModeFingerToggleSwitch")]
        public bool IsShowModeFingerToggleSwitch { get; set; } = true;
    }
}
