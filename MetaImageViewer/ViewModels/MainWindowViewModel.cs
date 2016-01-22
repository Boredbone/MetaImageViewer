using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Boredbone.Utility.Extensions;
using Boredbone.XamlTools;
using Boredbone.XamlTools.Extensions;
using MetaImageViewer.Models;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace MetaImageViewer.ViewModels
{
    public class MainWindowViewModel : DisposableBase
    {
        public ReactiveProperty<ImageContainer> Image { get; }
        public ReactiveProperty<double> ZoomFactor { get; }

        private ReactiveProperty<string[]> Files { get; }
        private ReactiveProperty<int> CurrentIndex { get; }

        public ReactiveCommand OpenCommand { get; }
        public ReactiveCommand PrevCommand { get; }
        public ReactiveCommand NextCommand { get; }

        public ReactiveCommand LoadFilesCommand { get; }
        public ReactiveCommand ResetZoomCommand { get; }


        public MainWindowViewModel()
        {
            // loaded files
            this.Files = new ReactiveProperty<string[]>().AddTo(this.Disposables);

            // image
            this.Image = new ReactiveProperty<ImageContainer>().AddTo(this.Disposables);

            // dispose old image
            this.Image.Pairwise().Subscribe(y => y.OldItem?.Dispose()).AddTo(this.Disposables);

            // change index when the file list is changed
            this.CurrentIndex = this.Files
                .Buffer(this.Image)
                .Where(y => y.Count > 0)
                .ObserveOnUIDispatcher()
                .Select(buf =>
                {
                    var array = buf.Last();
                    if (array == null || array.Length <= 1)
                    {
                        return 0;
                    }
                    var index = array.FindIndex(y => y.Equals(this.Image.Value?.Path));

                    return (index >= 0) ? index : 0;
                })
                .ToReactiveProperty()
                .AddTo(this.Disposables);

            // reset ZoomFactor when the Image is changed
            this.ZoomFactor = this.Image
                .Select(y => (y?.ZoomFactor.Value ?? 1.0) * 100)
                .ToReactiveProperty(100.0)
                .AddTo(this.Disposables);

            // transfer ZoomFactor from view to model
            this.ZoomFactor
                .Where(_ => this.Image.Value != null)
                .ObserveOnUIDispatcher()
                .Subscribe(y => this.Image.Value.ZoomFactor.Value = y / 100.0)
                .AddTo(this.Disposables);
            
            // file open
            this.OpenCommand = new ReactiveCommand()
                .WithSubscribe(_ =>
                {
                    var dialog = new OpenFileDialog()
                    {
                        Filter = $"Windows Metafile(*.emf;*.wmf)|*.emf;*.wmf|All Files(*.*)|*.*",
                        Multiselect = true,
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        this.LoadFiles(dialog.FileNames);
                    }
                }, this.Disposables);

            var fileSelectable = this.Files
                .Select(y => y != null && y.Length > 1)
                .ObserveOnUIDispatcher();

            // previous file
            this.PrevCommand = fileSelectable
                .ToReactiveCommand()
                .WithSubscribe(_ => this.ChangeFile(-1), this.Disposables);

            // next file
            this.NextCommand = fileSelectable
                .ToReactiveCommand()
                .WithSubscribe(_ => this.ChangeFile(1), this.Disposables);


            this.LoadFilesCommand = new ReactiveCommand()
                .WithSubscribe(y =>
                {
                    var files = y as string[];
                    if (files != null)
                    {
                        this.LoadFiles(files);
                    }
                }, this.Disposables);

            // Set ZoomFactor to 100%
            this.ResetZoomCommand = new ReactiveCommand()
                .WithSubscribe(_ => this.ZoomFactor.Value = 100.0, this.Disposables);

            // show requested image
            var launchArgs = ((App)Application.Current).LaunchArgs;
            if (launchArgs != null)
            {
                this.LoadFiles(launchArgs.Where(y => y.Length > 0).ToArray());
            }
        }

        /// <summary>
        /// Change File
        /// </summary>
        /// <param name="increment"></param>
        private void ChangeFile(int increment)
        {
            var length = this.Files.Value.Length;
            if (length <= 1)
            {
                return;
            }
            var index = this.CurrentIndex.Value + increment;
            while (index < 0)
            {
                index += length;
            }
            index = index % length;
            this.CurrentIndex.Value = index;
            this.LoadImage(this.Files.Value[index]);
        }

        /// <summary>
        /// Load Files
        /// </summary>
        /// <param name="files"></param>
        public void LoadFiles(string[] files)
        {
            if (files == null || files.Length <= 0)
            {
                return;
            }

            var path = files[0];

            if (files.Length > 1)
            {
                this.Files.Value = files;
            }
            else
            {
                Task.Run(() =>
                {
                    var list = System.IO.Directory.GetFiles(
                        System.IO.Path.GetDirectoryName(path), "*.emf", System.IO.SearchOption.TopDirectoryOnly);
                    this.Files.Value = list.OrderBy(y => y).ToArray();
                });
            }

            this.LoadImage(path);
        }

        /// <summary>
        /// Load image from file
        /// </summary>
        /// <param name="path"></param>
        private void LoadImage(string path)
        {
            try
            {
                var image = System.Drawing.Image.FromFile(path);

                this.Image.Value
                    = new ImageContainer(image) { Path = path, };
            }
            catch
            {
                this.Image.Value = null;
            }
        }

        /// <summary>
        /// Change ZoomFactor
        /// </summary>
        /// <param name="increment"></param>
        public void Zoom(double increment)
        {
            var current = this.ZoomFactor.Value;

            if (current > 0 && increment < 0)
            {
                while (current + increment <= 0)
                {
                    increment /= 10.0;
                }
            }
            this.ZoomFactor.Value += increment;
        }

        ///// <summary>
        ///// Set ZoomFactor to 100%
        ///// </summary>
        //public void ResetZoom()
        //{
        //    this.ZoomFactor.Value = 100.0;
        //}
    }
}

