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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MetaImageViewer.ViewModels;

namespace MetaImageViewer.Views
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        private MainWindowViewModel ViewModel { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this.ViewModel = this.DataContext as MainWindowViewModel;

            var app = ((App)Application.Current);
            app.WindowPlacement.Register(this, "MainWindow");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            (this.DataContext as IDisposable)?.Dispose();
            this.DataContext = null;
        }


        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                this.ViewModel?.LoadFiles(files);
            }
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var delta = e.Delta / 12.0;
            this.ViewModel.Zoom(delta);
            //
            //var current = this.ViewModel.ZoomFactor.Value;
            //
            //if (current > 0 && delta < 0)
            //{
            //    while (current + delta <= 0)
            //    {
            //        delta /= 10.0;
            //    }
            //}
            //this.ViewModel.ZoomFactor.Value += delta;
            e.Handled = true;
        }
        

        private void ScrollViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Right:
                    this.ViewModel.NextCommand.Execute();
                    break;
                case Key.Left:
                    this.ViewModel.PrevCommand.Execute();
                    break;
            }
        }

        private void ScrollViewer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.ViewModel.ResetZoom();
        }
    }
}
