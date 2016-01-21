using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// AboutWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            this.versionText.Text = ver.ToString();

            var buildDateTime = new DateTime(2000, 1, 1, 0, 0, 0);
            buildDateTime = buildDateTime.AddDays(ver.Build);
            buildDateTime = buildDateTime.AddSeconds(ver.Revision * 2);
            this.buildDate.Text = buildDateTime.ToString();
        }

        private void Hyperlink_RequestNavigate
            (object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
