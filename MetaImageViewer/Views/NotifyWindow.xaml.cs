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

namespace MetaImageViewer.Views
{
    /// <summary>
    /// NotifyWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NotifyWindow : Window
    {
        public static readonly ICommand OpenCommand = new RoutedCommand(nameof(OpenCommand), typeof(NotifyWindow));

        public NotifyWindow()
        {
            InitializeComponent();
        }

        private void TerminateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ShowMainWindow(null);
        }

        private void pageRoot_Loaded(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ((App)Application.Current).ShowMainWindow(null);
        }
    }
}
