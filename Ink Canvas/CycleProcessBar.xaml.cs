using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ink_Canvas.ProcessBars
{
    /// <summary>
    /// CycleProcessBar1.xaml 的交互逻辑
    /// </summary>
    public partial class CycleProcessBar : UserControl
    {
        public CycleProcessBar()
        {
            InitializeComponent();
            IsPaused = false;
        }

        public bool IsPaused
        {
            set { SetRingColor(value); }
        }

        private void SetRingColor(bool isPaused)
        {
            if (isPaused)
            {
                myCycleProcessBar.Stroke = new SolidColorBrush(StringToColor("#FF1A71C8"));
            }
            else
            {
                myCycleProcessBar.Stroke = new SolidColorBrush(StringToColor("#FF0067C1"));
            }
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
            return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);//#FFFFFFFF
        }

        private static byte toByte(char c)
        {
            byte b = (byte)"0123456789ABCDEF".IndexOf(c);
            return b;
        }

        public double CurrentValue
        {
            set { SetValue(value); }
        }

        /// <summary>
        /// 设置百分百，输入小数，自动乘100
        /// </summary>
        /// <param name="percentValue"></param>
        private void SetValue(double percentValue)
        {
            /*****************************************
              方形矩阵边长为34，半长为17
              环形半径为14，所以距离边框3个像素
              环形描边3个像素
            ******************************************/
            double angel = percentValue * 360; //角度

            double radius = 14; //环形半径

            //起始点
            double leftStart = 17;
            double topStart = 3;

            //结束点
            double endLeft = 0;
            double endTop = 0;

            if (percentValue == 0) myCycleProcessBar.Visibility = Visibility.Hidden;
            else myCycleProcessBar.Visibility = Visibility.Visible;


            //数字显示
            lbValue.Content = (percentValue * 100).ToString("0") + "%";

            /***********************************************
            * 整个环形进度条使用Path来绘制，采用三角函数来计算
            * 环形根据角度来分别绘制，以90度划分，方便计算比例
            ***********************************************/

            bool isLagreCircle = false; //是否优势弧，即大于180度的弧形

            //小于90度
            if (angel <= 90)
            {
                /*****************
                          *
                          *   *
                          * * ra
                   * * * * * * * * *
                          *
                          *
                          *
                ******************/
                double ra = (90 - angel) * Math.PI / 180; //弧度
                endLeft = leftStart + Math.Cos(ra) * radius; //余弦横坐标
                endTop = topStart + radius - Math.Sin(ra) * radius; //正弦纵坐标
            }

            else if (angel <= 180)
            {
                /*****************
                          *
                          *  
                          * 
                   * * * * * * * * *
                          * * ra
                          *  *
                          *   *
                ******************/

                double ra = (angel - 90) * Math.PI / 180; //弧度
                endLeft = leftStart + Math.Cos(ra) * radius; //余弦横坐标
                endTop = topStart + radius + Math.Sin(ra) * radius;//正弦纵坐标
            }

            else if (angel <= 270)
            {
                /*****************
                          *
                          *  
                          * 
                   * * * * * * * * *
                        * *
                       *ra*
                      *   *
                ******************/
                isLagreCircle = true; //优势弧
                double ra = (angel - 180) * Math.PI / 180;
                endLeft = leftStart - Math.Sin(ra) * radius;
                endTop = topStart + radius + Math.Cos(ra) * radius;
            }

            else if (angel < 360)
            {
                /*****************
                      *   *
                       *  *  
                     ra * * 
                   * * * * * * * * *
                          *
                          *
                          *
                ******************/
                isLagreCircle = true; //优势弧
                double ra = (angel - 270) * Math.PI / 180;
                endLeft = leftStart - Math.Cos(ra) * radius;
                endTop = topStart + radius - Math.Sin(ra) * radius;
            }
            else
            {
                isLagreCircle = true; //优势弧
                endLeft = leftStart - 0.001; //不与起点在同一点，避免重叠绘制出非环形
                endTop = topStart;
            }

            Point arcEndPt = new Point(endLeft, endTop); //结束点
            Size arcSize = new Size(radius, radius);
            SweepDirection direction = SweepDirection.Clockwise; //顺时针弧形
            //弧形
            ArcSegment arcsegment = new ArcSegment(arcEndPt, arcSize, 0, isLagreCircle, direction, true);

            //形状集合
            PathSegmentCollection pathsegmentCollection = new PathSegmentCollection();
            pathsegmentCollection.Add(arcsegment);

            //路径描述
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = new Point(leftStart, topStart); //起始地址
            pathFigure.Segments = pathsegmentCollection;

            //路径描述集合
            PathFigureCollection pathFigureCollection = new PathFigureCollection();
            pathFigureCollection.Add(pathFigure);

            //复杂形状
            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures = pathFigureCollection;

            //Data赋值
            myCycleProcessBar.Data = pathGeometry;
            //达到100%则闭合整个
            if (angel == 360)
            {
                myCycleProcessBar.Data = Geometry.Parse(myCycleProcessBar.Data.ToString() + " z");
            }
        }
    }
}