using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
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
                SymbolIconMultiTouchMode.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.People;
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
                SymbolIconMultiTouchMode.Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol.Contact;
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
        private bool _lockSmith = false; //临时停用双指手势

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
            if (isInMultiTouchMode || !Settings.Gesture.IsEnableTwoFingerGesture || _lockSmith) return;
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

    }
}