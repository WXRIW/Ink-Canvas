using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            //TextBlockChangeLog.Text = "" +
            //    "1. 修复使用画板时无法使用翻页笔的问题\n" +
            //    "2. 支持修改画笔粗细，支持显示画笔指针\n" +
            //    "3. 支持双指缩放和拖动手势\n" +
            //    "4. 添加“选择墨迹”功能，选中后可以对墨迹进行拖动，拉伸等操作\n" +
            //    "5. 添加画图功能（直线、箭头、矩形、椭圆）";
            //TextBlockSuggestion.Text = "打开设置，点击“重置”中的“重置设置为推荐设置”，以提升教学体验！";

            //Version 2.1.1-release
            //TextBlockChangeLog.Text = "" +
            //    "1. 修复部分情况下幻灯片放映翻页时墨迹保留的问题\n" +
            //    "2. 支持选中后的缩放和拖动\n" +
            //    "3. 修复部分模式下自动橡皮失效的问题\n" +
            //    "4. 修复幻灯片放映时的部分问题";
            //TextBlockSuggestion.Text = "打开设置，点击“重置”中的“重置设置为推荐设置”，以提升教学体验！";

            //Version 2.1.2-release
            TextBlockChangeLog.Text = "" +
                "1. 支持多页白板/黑板\n" +
                "2. 支持撤销\n" +
                "3. 双击清屏即可在清屏的同时隐藏画板\n" +
                "4. 对深浅主题下墨迹颜色的优化\n" +
                "\n" +
                "目前基本可以取代“希沃白板”";
            TextBlockSuggestionTitle.Visibility = Visibility.Collapsed;
            //TextBlockSuggestion.Text = "老师讲评试卷可以点击右侧的背景和深色按钮，即可启动黑板功能（同样支持用笔来擦除），\n双指可以缩放和拖动，左边也会有工具栏方便画图形。";

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            LabelVersion.Content = "Version: " + version.ToString();
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
