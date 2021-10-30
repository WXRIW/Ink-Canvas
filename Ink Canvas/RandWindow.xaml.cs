using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        public int PeopleCount = 60;
        public List<string> Names = new List<string>();

        private void BorderBtnAdd_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (TotalCount >= PeopleCount) return;
            TotalCount++;
            LabelNumberCount.Content = TotalCount.ToString();
            SymbolIconStart.Symbol = Symbol.People;
        }

        private void BorderBtnMinus_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (TotalCount < 2) return;
            TotalCount--;
            LabelNumberCount.Content = TotalCount.ToString();
            if (TotalCount == 1)
            {
                SymbolIconStart.Symbol = Symbol.Contact;
            }
        }

        private void BorderBtnRand_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Random random = new Random();
            string outputString = "";
            List<string> outputs = new List<string>();
            List<int> rands = new List<int>();

            LabelOutput2.Visibility = Visibility.Collapsed;
            LabelOutput3.Visibility = Visibility.Collapsed;
            BorderBtnRandCover.Visibility = Visibility.Visible;

            new Thread(new ThreadStart(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    int rand = random.Next(1, PeopleCount);
                    while (rands.Contains(rand))
                    {
                        rand = random.Next(1, PeopleCount);
                    }
                    rands.Add(rand);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (Names.Count != 0)
                        {
                            LabelOutput.Content = Names[rand];
                        }
                        else
                        {
                            LabelOutput.Content = rand.ToString();
                        }
                    });

                    Thread.Sleep(150);
                }

                rands = new List<int>();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < TotalCount; i++)
                    {
                        int rand = random.Next(1, PeopleCount);
                        while (rands.Contains(rand))
                        {
                            rand = random.Next(1, PeopleCount);
                        }
                        rands.Add(rand);

                        if (Names.Count != 0)
                        {
                            outputs.Add(Names[rand]);
                            outputString += Names[rand] + Environment.NewLine;
                        }
                        else
                        {
                            outputs.Add(rand.ToString());
                            outputString += rand.ToString() + Environment.NewLine;
                        }
                    }
                    if (TotalCount <= 5)
                    {
                        LabelOutput.Content = outputString.ToString().Trim();
                    }
                    else if (TotalCount <= 10)
                    {
                        LabelOutput2.Visibility = Visibility.Visible;
                        outputString = "";
                        for (int i = 0; i < (outputs.Count + 1) / 2; i++)
                        {
                            outputString += outputs[i].ToString() + Environment.NewLine;
                        }
                        LabelOutput.Content = outputString.ToString().Trim();
                        outputString = "";
                        for (int i = (outputs.Count + 1) / 2; i < outputs.Count; i++)
                        {
                            outputString += outputs[i].ToString() + Environment.NewLine;
                        }
                        LabelOutput2.Content = outputString.ToString().Trim();
                    }
                    else
                    {
                        LabelOutput2.Visibility = Visibility.Visible;
                        LabelOutput3.Visibility = Visibility.Visible;
                        outputString = "";
                        for (int i = 0; i < (outputs.Count + 1) / 3; i++)
                        {
                            outputString += outputs[i].ToString() + Environment.NewLine;
                        }
                        LabelOutput.Content = outputString.ToString().Trim();
                        outputString = "";
                        for (int i = (outputs.Count + 1) / 3; i < (outputs.Count + 1) * 2 / 3; i++)
                        {
                            outputString += outputs[i].ToString() + Environment.NewLine;
                        }
                        LabelOutput2.Content = outputString.ToString().Trim();
                        outputString = "";
                        for (int i = (outputs.Count + 1) * 2 / 3; i < outputs.Count; i++)
                        {
                            outputString += outputs[i].ToString() + Environment.NewLine;
                        }
                        LabelOutput3.Content = outputString.ToString().Trim();
                    }
                    BorderBtnRandCover.Visibility = Visibility.Collapsed;
                });
            })).Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Names = new List<string>();
            if (File.Exists(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Names.txt"))
            {
                string[] fileNames = File.ReadAllLines(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Names.txt");

                //Fix emtpy lines
                foreach (string s in fileNames)
                {
                    if (s != "") Names.Add(s);
                }

                PeopleCount = Names.Count();
                TextBlockPeopleCount.Text = PeopleCount.ToString();
            }
        }

        private void BorderBtnHelp_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show("如需显示姓名，请在程序目录下新建 Names.txt，并将姓名输入，一行一个。");
            new NamesInputWindow().ShowDialog();
            Window_Loaded(this, null);
        }

        private void BtnClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}
