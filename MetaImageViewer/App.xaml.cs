using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using MetaImageViewer.Views;
using RestoreWindowPlace;
using MetaImageViewer.Models;
using System.Reactive.Disposables;
using Reactive.Bindings.Extensions;

namespace MetaImageViewer
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public WindowPlace WindowPlacement { get; }

        //public string[] LaunchArgs { get; private set; }


        private const string mutexId = "ad6e4711-0ca9-4abe-b75e-0f5fcc876424";
        private const string pipeId = "bca7161c-20cd-4f98-b22b-6efc09b15ea6";
        private PipeServer server;
        private CompositeDisposable disposables;

        public App()
        {
            //var assembly = Assembly.GetEntryAssembly();
            //var directory = System.IO.Path.GetDirectoryName(assembly.Location);

            var dir = System.AppDomain.CurrentDomain.BaseDirectory;

            this.WindowPlacement = new WindowPlace(dir + @"placement.config");
            this.disposables = new CompositeDisposable();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            this.WindowPlacement.Save();
            this.server.Dispose();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var args = e.Args;

            if (args != null && args.Length > 0)
            {
                foreach (var file in args)
                {
                    //this.LaunchArgs = args;

                    this.ShowMainWindow(file);
                }
            }
            else
            {
                //this.ShowMainWindow(null);
            }

            if (this.server == null)
            {
                this.server = new PipeServer().AddTo(this.disposables);

                this.server.LineReceived
                    .ObserveOnUIDispatcher()
                    .Subscribe(file => this.ShowMainWindow(file), ex => this.Shutdown())
                    .AddTo(this.disposables);

                this.server.Activate(mutexId, pipeId);

            }
        }

        public void ShowMainWindow(string file)
        {

            var window = new MainWindow();
            window.ShowActivated = true;
            if (file != null)
            {
                window.ViewModel.LoadFiles(new[] { file });
            }
            window.Show();
        }
    }
}
