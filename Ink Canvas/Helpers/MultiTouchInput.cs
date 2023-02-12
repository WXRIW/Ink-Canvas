using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Helpers
{
    public class VisualCanvas : FrameworkElement
    {
        protected override Visual GetVisualChild(int index)
        {
            return Visual;
        }

        protected override int VisualChildrenCount => 1;

        public VisualCanvas(DrawingVisual visual)
        {
            Visual = visual;
            AddVisualChild(visual);
        }

        public DrawingVisual Visual { get; }
    }

    /// <summary>
    ///     用于显示笔迹的类
    /// </summary>
    public class StrokeVisual : DrawingVisual
    {
        /// <summary>
        ///     创建显示笔迹的类
        /// </summary>
        public StrokeVisual() : this(new DrawingAttributes()
        {
            Color = Colors.Red,
            //FitToCurve = true,
            Width = 3,
            Height = 3
        })
        {
        }

        /// <summary>
        ///     创建显示笔迹的类
        /// </summary>
        /// <param name="drawingAttributes"></param>
        public StrokeVisual(DrawingAttributes drawingAttributes)
        {
            _drawingAttributes = drawingAttributes;
        }

        /// <summary>
        ///     设置或获取显示的笔迹
        /// </summary>
        public Stroke Stroke { set; get; }

        /// <summary>
        ///     在笔迹中添加点
        /// </summary>
        /// <param name="point"></param>
        public void Add(StylusPoint point)
        {
            if (Stroke == null)
            {
                var collection = new StylusPointCollection { point };
                Stroke = new Stroke(collection) { DrawingAttributes = _drawingAttributes };
            }
            else
            {
                Stroke.StylusPoints.Add(point);
            }
        }

        /// <summary>
        ///     重新画出笔迹
        /// </summary>
        public void Redraw()
        {
            try
            {
                using (var dc = RenderOpen())
                {
                    Stroke.Draw(dc);
                }
            }
            catch { }
        }

        private readonly DrawingAttributes _drawingAttributes;
    }
}
