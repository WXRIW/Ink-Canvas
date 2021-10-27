using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for RandWindow.xaml
    /// </summary>
    public partial class RandWindow : Window
    {
        public RandWindow()
        {
            InitializeComponent();
        }

        public int TotalCount = 1;
        public List<string> Names = new List<string>();

        private void BorderBtnAdd_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TotalCount++;
            LabelNumberCount.Content = TotalCount.ToString();
        }

        private void BorderBtnMinus_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (TotalCount < 2) return;
            TotalCount--;
            LabelNumberCount.Content = TotalCount.ToString();
        }

        private void BorderBtnRand_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Random random = new Random();
            string outputString = "";
            for (int i = 0; i < TotalCount; i++)
            {
                int maxN = 60;
                if (Names.Count != 0)
                {
                    maxN = Names.Count;
                }
                int rand = random.Next(1, maxN);
                if (Names.Count != 0)
                {
                    outputString += Names[rand] + Environment.NewLine;
                }
                else
                {
                    outputString += rand.ToString() + Environment.NewLine;
                }
            }
            LabelOutput.Content = outputString.ToString().Trim();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists("Names.txt"))
            {
                string[] fileNames = File.ReadAllLines("Names.txt");

                //Fix emtpy lines
                foreach (string s in fileNames)
                {
                    if (s != "") Names.Add(s);
                }
            }
        }

        private void BorderBtnHelp_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("如需显示姓名，请在程序目录下新建 Names.txt，并将姓名输入，一行一个。");
        }

        private void BtnClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}
