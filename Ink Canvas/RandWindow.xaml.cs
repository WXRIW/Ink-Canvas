using Microsoft.VisualBasic; 
using ModernWpf.Controls; 
using System; 
using System.Collections.Generic; 
using System.IO; 
using System.Linq; 
using System.Threading; 
using System.Windows; 
using System.Windows.Input; 
namespace Ink_Canvas { 
    /// <summary> 
    /// Interaction logic for RandWindow.xaml 
    /// </summary> 
    public partial class RandWindow : Window { 
        public RandWindow() { 
            InitializeComponent(); 
        } 
        public RandWindow(bool IsAutoClose) { 
            InitializeComponent(); 
            isAutoClose = IsAutoClose; 
            new Thread(new ThreadStart(() => { 
                Thread.Sleep(100); 
                Application.Current.Dispatcher.Invoke(() => { 
                    BorderBtnRand_MouseUp(BorderBtnRand, null); 
                }); 
            })).Start(); 
        } 
        public static int randSeed = 0; 
        public bool isAutoClose = false; 
        public int TotalCount = 1; 
        public int PeopleCount = 60; 
        public List<string> Names = new List<string>(); 

        // 新增一个变量，用于存储抽奖的日期和时间
        public DateTime drawTime = DateTime.MinValue; 

        // 新增一个变量，用于存储已经抽到的人的索引
        public List<int> drawnIndexes = new List<int>(); 

        private void BorderBtnAdd_MouseUp(object sender, MouseButtonEventArgs e) { 
            if (TotalCount >= PeopleCount) return; 
            TotalCount++; 
            LabelNumberCount.Content = TotalCount.ToString(); 
            SymbolIconStart.Symbol = Symbol.People; 
        } 
        private void BorderBtnMinus_MouseUp(object sender, MouseButtonEventArgs e) { 
            if (TotalCount < 2) return; 
            TotalCount--; 
            LabelNumberCount.Content = TotalCount.ToString(); 
            if (TotalCount == 1) { 
                SymbolIconStart.Symbol = Symbol.Contact; 
            } 
        } 
        private void BorderBtnRand_MouseUp(object sender, MouseButtonEventArgs e) { 
            Random random = new Random();// randSeed + DateTime.Now.Millisecond / 10 % 10); 
            string outputString = ""; 
            List<string> outputs = new List<string>(); 
            List<int> rands = new List<int>(); 
            LabelOutput2.Visibility = Visibility.Collapsed; 
            LabelOutput3.Visibility = Visibility.Collapsed; 
            BorderBtnRandCover.Visibility = Visibility.Visible; 
            new Thread(new ThreadStart(() => { 
                for (int i = 0; i < 5; i++) { 
                    int rand = random.Next(1, PeopleCount + 1); 
                    while (rands.Contains(rand)) { 
                        rand = random.Next(1, PeopleCount + 1); 
                    } 
                    rands.Add(rand); 
                    if (rands.Count >= PeopleCount) rands = new List<int>(); 
                    Application.Current.Dispatcher.Invoke(() => { 
                        if (Names.Count != 0) { 
                            LabelOutput.Content = Names[rand - 1]; 
                        } else { 
                            LabelOutput.Content = rand.ToString(); 
                        } 
                    }); 
                    Thread.Sleep(150); 
                } 
                rands = new List<int>(); 
                Application.Current.Dispatcher.Invoke(() => { 

                    // 新增一个判断，如果当前的时间和上次抽奖的时间相差超过30分钟，就重置已抽到的人的索引，并写入文件
                    if (DateTime.Now - drawTime > TimeSpan.FromMinutes(30)) {
                        drawnIndexes = new List<int>();
                        File.WriteAllText(App.RootPath + "DrawTime.txt", DateTime.Now.ToString());
                    }

                    for (int i = 0; i < TotalCount; i++) { 
                        int rand = random.Next(1, PeopleCount + 1); 

                        // 修改一个判断，如果已经抽到的人的索引包含当前的随机数，就重新生成随机数
                        while (rands.Contains(rand) || drawnIndexes.Contains(rand)) { 
                            rand = random.Next(1, PeopleCount + 1); 
                        } 
                        rands.Add(rand); 
                        drawnIndexes.Add(rand); // 将当前的随机数添加到已抽到的人的索引中
                        if (rands.Count >= PeopleCount) rands = new List<int>(); 
                        if (Names.Count != 0) { 
                            outputs.Add(Names[rand - 1]); 
                            outputString += Names[rand - 1] + Environment.NewLine; 
                        } else { 
                            outputs.Add(rand.ToString()); 
                            outputString += rand.ToString() + Environment.NewLine; 
                        } 
                    } 
                    if (TotalCount <= 5) { 
                        LabelOutput.Content = outputString.ToString().Trim(); 
                    } else if (TotalCount <= 10) { 
                        LabelOutput2.Visibility = Visibility.Visible; 
                        outputString = ""; 
                        for (int i = 0; i < (outputs.Count + 1) / 2; i++) { 
                            outputString += outputs[i].ToString() + Environment.NewLine; 
                        } 
                        LabelOutput.Content = outputString.ToString().Trim(); 
                        outputString = ""; 
                        for (int i = (outputs.Count + 1) / 2; i < outputs.Count; i++) { 
                            outputString += outputs[i].ToString() + Environment.NewLine; 
                        } 
                        LabelOutput2.Content = outputString.ToString().Trim(); 
                    } else { 
                        LabelOutput2.Visibility = Visibility.Visible; 
                        LabelOutput3.Visibility = Visibility.Visible; 
                        outputString = ""; 
                        for (int i = 0; i < (outputs.Count + 1) / 3; i++) { 
                            outputString += outputs[i].ToString() + Environment.NewLine; 
                        } 
                        LabelOutput.Content = outputString.ToString().Trim(); 
                        outputString = ""; 
                        for (int i = (outputs.Count + 1) / 3; i < (outputs.Count + 1) * 2 / 3; i++) { 
                            outputString += outputs[i].ToString() + Environment.NewLine; 
                        } 
                        LabelOutput2.Content = outputString.ToString().Trim(); 
                        outputString = ""; 
                        for (int i = (outputs.Count + 1) * 2 / 3; i < outputs.Count; i++) { 
                            outputString += outputs[i].ToString() + Environment.NewLine; 
                        } 
                        LabelOutput3.Content = outputString.ToString().Trim(); 
                    } 
                    BorderBtnRandCover.Visibility = Visibility.Collapsed; 
                    if (isAutoClose) { 
                        new Thread(new ThreadStart(() => { 
                            Thread.Sleep(1500); 
                            Application.Current.Dispatcher.Invoke(() => { 
                                Close(); 
                            }); 
                        })).Start(); 
                    } 
                }); 
            })).Start(); 
        } 
        private void Window_Loaded(object sender, RoutedEventArgs e) { 
            Names = new List<string>(); 
            if (File.Exists(App.RootPath + "Names.txt")) { 
                string[] fileNames = File.ReadAllLines(App.RootPath + "Names.txt"); 
                string[] replaces = new string[0]; 
                if (File.Exists(App.RootPath + "Replace.txt")) { 
                    replaces = File.ReadAllLines(App.RootPath + "Replace.txt"); 
                } 
                //Fix emtpy lines 
                foreach (string str in fileNames) { 
                    string s = str; 
                    //Make replacement 
                    foreach (string replace in replaces) { 
                        if (s == Strings.Left(replace, replace.IndexOf("-->"))) { 
                            s = Strings.Mid(replace, replace.IndexOf("-->") + 4); 
                        } 
                    } 
                    if (s != "") Names.Add(s); 
                } 
                PeopleCount = Names.Count(); 
                TextBlockPeopleCount.Text = PeopleCount.ToString(); 
                if (PeopleCount == 0) { 
                    PeopleCount = 60; 
                    TextBlockPeopleCount.Text = "点击此处以导入名单"; 
                } 
            } 

            // 新增一个判断，如果存在抽奖时间的文件，就读取文件中的时间，并赋值给drawTime变量
            if (File.Exists(App.RootPath + "DrawTime.txt")) {
                drawTime = DateTime.Parse(File.ReadAllText(App.RootPath + "DrawTime.txt"));
            }
        } 
        private void BorderBtnHelp_MouseUp(object sender, MouseButtonEventArgs e) { 
            //MessageBox.Show("如需显示姓名，请在程序目录下新建 Names.txt，并将姓名输入，一行一个。"); 
            new NamesInputWindow().ShowDialog(); 
            Window_Loaded(this, null); 
        } 
        private void BtnClose_MouseUp(object sender, MouseButtonEventArgs e) { 
            Close(); 
        } 
    } 
} 
