using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace MetaImageViewer
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public RestoreWindowPlace.RestoreWindowPlace WindowPlacement { get; }

        public string[] LaunchArgs { get; private set; }

        public App()
        {
            var assembly = Assembly.GetEntryAssembly();
            var directory = System.IO.Path.GetDirectoryName(assembly.Location);

            this.WindowPlacement = new RestoreWindowPlace.RestoreWindowPlace
                (directory + @"\placement.config");
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            this.WindowPlacement.Save();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var args = e.Args;

            if (args != null && args.Length > 0)
            {
                this.LaunchArgs = args;
            }
        }
    }
}
