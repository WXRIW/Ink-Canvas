using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;

namespace Ink_Canvas.Helpers
{
    public class InkRecognizeHelper
    {
        //识别形状
        public static ShapeRecognizeResult RecognizeShape(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0)
                return default;

            var analyzer = new InkAnalyzer();
            analyzer.AddStrokes(strokes);
            analyzer.SetStrokesType(strokes, System.Windows.Ink.StrokeType.Drawing);

            AnalysisAlternate analysisAlternate = null;
            int strokesCount = strokes.Count;
            var sfsaf = analyzer.Analyze();
            if (sfsaf.Successful)
            {
                var alternates = analyzer.GetAlternates();
                if (alternates.Count > 0)
                {
                    while ((!alternates[0].Strokes.Contains(strokes.Last()) ||
                        !IsContainShapeType(((InkDrawingNode)alternates[0].AlternateNodes[0]).GetShapeName()))
                        && strokesCount >= 2)
                    {
                        analyzer.RemoveStroke(strokes[strokes.Count - strokesCount]);
                        strokesCount--;
                        sfsaf = analyzer.Analyze();
                        if (sfsaf.Successful)
                        {
                            alternates = analyzer.GetAlternates();
                        }
                    }
                    analysisAlternate = alternates[0];
                }
            }

            analyzer.Dispose();

            if (analysisAlternate != null && analysisAlternate.AlternateNodes.Count > 0)
            {
                var node = analysisAlternate.AlternateNodes[0] as InkDrawingNode;
                return new ShapeRecognizeResult(node.Centroid, node.HotPoints, analysisAlternate, node);
            }

            return default;
        }

        public static bool IsContainShapeType(string name)
        {
            if (name.Contains("Triangle") || name.Contains("Circle") ||
                name.Contains("Rectangle") || name.Contains("Diamond") ||
                name.Contains("Parallelogram") || name.Contains("Square")
                || name.Contains("Ellipse"))
            {
                return true;
            }
            return false;
        }
    }

    //Recognizer 的实现

    public enum RecognizeLanguage
    {
        SimplifiedChinese = 0x0804,
        TraditionalChinese = 0x7c03,
        English = 0x0809
    }

    public class ShapeRecognizeResult
    {
        public ShapeRecognizeResult(Point centroid, PointCollection hotPoints, AnalysisAlternate analysisAlternate, InkDrawingNode node)
        {
            Centroid = centroid;
            HotPoints = hotPoints;
            AnalysisAlternate = analysisAlternate;
            InkDrawingNode = node;
        }

        public AnalysisAlternate AnalysisAlternate { get; }

        public Point Centroid { get; set; }

        public PointCollection HotPoints { get; }

        public InkDrawingNode InkDrawingNode { get; }
    }

    /// <summary>
    /// 图形识别类
    /// </summary>
    //public class ShapeRecogniser
    //{
    //    public InkAnalyzer _inkAnalyzer = null;

    //    private ShapeRecogniser()
    //    {
    //        this._inkAnalyzer = new InkAnalyzer
    //        {
    //            AnalysisModes = AnalysisModes.AutomaticReconciliationEnabled
    //        };
    //    }

    //    /// <summary>
    //    /// 根据笔迹集合返回图形名称字符串
    //    /// </summary>
    //    /// <param name="strokeCollection"></param>
    //    /// <returns></returns>
    //    public InkDrawingNode Recognition(StrokeCollection strokeCollection)
    //    {
    //        if (strokeCollection == null)
    //        {
    //            //MessageBox.Show("dddddd");
    //            return null;
    //        }

    //        InkDrawingNode result = null;
    //        try
    //        {
    //            this._inkAnalyzer.AddStrokes(strokeCollection);
    //            if (this._inkAnalyzer.Analyze().Successful)
    //            {
    //                result = _internalAnalyzer(this._inkAnalyzer);
    //                this._inkAnalyzer.RemoveStrokes(strokeCollection);
    //            }
    //        }
    //        catch (System.Exception ex)
    //        {
    //            //result = ex.Message;
    //            System.Diagnostics.Debug.WriteLine(ex.Message);
    //        }

    //        return result;
    //    }

    //    /// <summary>
    //    /// 实现笔迹的分析，返回图形对应的字符串
    //    /// 你在实际的应用中根据返回的字符串来生成对应的Shape
    //    /// </summary>
    //    /// <param name="ink"></param>
    //    /// <returns></returns>
    //    private InkDrawingNode _internalAnalyzer(InkAnalyzer ink)
    //    {
    //        try
    //        {
    //            ContextNodeCollection nodecollections = ink.FindNodesOfType(ContextNodeType.InkDrawing);
    //            foreach (ContextNode node in nodecollections)
    //            {
    //                InkDrawingNode drawingNode = node as InkDrawingNode;
    //                if (drawingNode != null)
    //                {
    //                    return drawingNode;//.GetShapeName();
    //                }
    //            }
    //        }
    //        catch (System.Exception ex)
    //        {
    //            System.Diagnostics.Debug.WriteLine(ex.Message);
    //        }

    //        return null;
    //    }


    //    private static ShapeRecogniser instance = null;
    //    public static ShapeRecogniser Instance
    //    {
    //        get
    //        {
    //            return instance == null ? (instance = new ShapeRecogniser()) : instance;
    //        }
    //    }
    //}


    //用于自动控制其他形状相对于圆的位置

    public class Circle
    {
        public Circle(Point centroid, double r, Stroke stroke)
        {
            Centroid = centroid;
            R = r;
            Stroke = stroke;
        }

        public Point Centroid { get; set; }

        public double R { get; set; }

        public Stroke Stroke { get; set; }
    }
}
