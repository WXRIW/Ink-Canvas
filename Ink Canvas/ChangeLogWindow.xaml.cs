using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for ChangeLogWindow.xaml
    /// </summary>
    public partial class ChangeLogWindow : Window
    {
        public ChangeLogWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Version 2.1.0-beta
            TextBlockChangeLog.Text = "" +
                "1. 修复使用画板时无法使用翻页笔的问题\n" +
                "2. 支持修改画笔粗细，支持显示画笔指针\n" +
                "3. 支持双指缩放和拖动手势\n" +
                "4. 添加“选择墨迹”功能，选中后可以对墨迹进行拖动，拉伸等操作\n" +
                "5. 添加画图功能（直线、箭头、矩形、椭圆）";
            TextBlockSuggestion.Text = "打开设置，点击“重置”中的“重置设置为推荐设置”，以提升教学体验！";
        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
