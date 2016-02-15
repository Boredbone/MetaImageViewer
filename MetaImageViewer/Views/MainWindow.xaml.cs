using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
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
using Reactive.Bindings.Extensions;

namespace MetaImageViewer.Views
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; }
        private CompositeDisposable Disposables { get; } 

        public MainWindow()
        {
            InitializeComponent();
            this.Disposables = new CompositeDisposable();

            this.ViewModel = (MainWindowViewModel)this.DataContext;
            this.ViewModel.AddTo(this.Disposables);

            Disposable.Create(() => this.DataContext = null).AddTo(this.Disposables);

            this.inertiaBehavior.AddTo(this.Disposables);

            var app = (App)Application.Current;
            app.WindowPlacement.Register(this, "MainWindow");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Disposables.Dispose();
        }

        


        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var focused = FocusManager.GetFocusedElement(this);
            if (focused == null || !(focused is TextBox))
            {
                switch (e.Key)
                {
                    case Key.Right:
                        this.ViewModel.NextCommand.Execute();
                        e.Handled = true;
                        break;
                    case Key.Left:
                        this.ViewModel.PrevCommand.Execute();
                        e.Handled = true;
                        break;
                }
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.ViewModel.Zoom(e.Delta / 12.0);
            e.Handled = true;
        }
        

        private void Button_Click(object sender, RoutedEventArgs e)
            => new AboutWindow() { Owner = this }.ShowDialog();
        
    }
}
