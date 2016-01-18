using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Boredbone.XamlTools.Extensions;
using Boredbone.XamlTools.ViewModel;
using MetaImageViewer.Tools;
using Reactive.Bindings;
using System.Reactive.Linq;
using System.Reactive;
using Reactive.Bindings.Extensions;
using System.Windows;
using Boredbone.Utility.Extensions;
using Microsoft.Win32;

namespace MetaImageViewer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {


        public ReactiveProperty<ImageContainer> Image { get; }
        public ReactiveProperty<double> ZoomFactor { get; }

        private ReactiveProperty<string[]> Files { get; }
        private ReactiveProperty<int> CurrentIndex { get; }

        public ReactiveCommand OpenCommand { get; }
        public ReactiveCommand PrevCommand { get; }
        public ReactiveCommand NextCommand { get; }


        public MainWindowViewModel()
        {
            this.Files = new ReactiveProperty<string[]>().AddTo(this.Disposables);

            this.Image = new ReactiveProperty<ImageContainer>().AddTo(this.Disposables);

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
                    var index = array.FindIndex(y => y.Equals(this.Image.Value?.Name));

                    return (index >= 0) ? index : 0;
                })
                .ToReactiveProperty()
                .AddTo(this.Disposables);


            this.ZoomFactor = this.Image
                .Select(y => (y?.ZoomFactor.Value ?? 1.0) * 100)
                .ToReactiveProperty(100.0)
                .AddTo(this.Disposables);


            //this.Image
            //    .Zip(this.Image.Skip(1), (Old, New) => new { Old, New })
            //    .Subscribe(a => a.Old?.Dispose());

            this.Image.Pairwise().Subscribe(y => y.OldItem?.Dispose()).AddTo(this.Disposables);

            this.ZoomFactor
                //.Throttle(TimeSpan.FromMilliseconds(200))
                .Where(_ => this.Image.Value != null)
                .ObserveOnUIDispatcher()
                .Subscribe(y => this.Image.Value.ZoomFactor.Value = y / 100.0)
                .AddTo(this.Disposables);

            //this.Image.Scan((o, n) => o).Subscribe(o => o?.Dispose()).AddTo(this.Disposables);

            this.OpenCommand = new ReactiveCommand()
                .WithSubscribe(_ =>
                {
                    var dialog = new OpenFileDialog();
                    dialog.Filter = $"Windows Metafile(*.emf;*.wmf)|*.emf;*.wmf|All Files(*.*)|*.*";
                    dialog.Multiselect = true;

                    if (dialog.ShowDialog() == true)
                    {
                        this.LoadFiles(dialog.FileNames);
                    }
                }, this.Disposables);

            var fileSelectable = this.Files
                .Select(y => y != null && y.Length > 1)
                .ObserveOnUIDispatcher();

            this.PrevCommand = fileSelectable
                .ToReactiveCommand()
                .WithSubscribe(_ => this.ChangeFile(-1), this.Disposables);

            this.NextCommand = fileSelectable
                .ToReactiveCommand()
                .WithSubscribe(_ => this.ChangeFile(1), this.Disposables);


            var launchArgs = ((App)Application.Current).LaunchArgs;
            if (launchArgs != null)
            {
                this.LoadFiles(launchArgs.Where(y => y.Length > 0).ToArray());
            }
        }

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
            //this.Image.Value= this.Files.Value[index]
        }


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

        private void LoadImage(string path)
        {

            try
            {
                var image = System.Drawing.Image.FromFile(path);

                this.Image.Value
                    = new ImageContainer(image) { Name = path, };
            }
            catch
            {
                this.Image.Value = null;
            }
        }


        //private BitmapSource LoadFromFile(string path, double rate)
        //{
        //    using (var image = System.Drawing.Image.FromFile(path))
        //    using (var canvas = new Bitmap((int)(image.Size.Width * rate), (int)(image.Size.Height * rate)))
        //    using (var graphics = Graphics.FromImage(canvas))
        //    {
        //        graphics.DrawImage(image, 0, 0, (int)(image.Size.Width * rate), (int)(image.Size.Height * rate));
        //        return canvas.ToWPFBitmap();
        //    }
        //}
    }

    public class ImageContainer : ViewModelBase
    {
        public ReactiveProperty<ImageSource> Image { get; }
        public ReactiveProperty<double> ZoomFactor { get; }
        private Image Source { get; }
        public string Name { get; set; }


        public ImageContainer(System.Drawing.Image image)
        {
            this.Source = image;
            this.Source.AddTo(this.Disposables);

            this.ZoomFactor = new ReactiveProperty<double>(1.0).AddTo(this.Disposables);


            this.Image = this.ZoomFactor
                //.Throttle(TimeSpan.FromMilliseconds(200))
                //.ObserveOnUIDispatcher()
                .Select(_ => this.DecodeImage())
                .ToReactiveProperty()
                .AddTo(this.Disposables);
        }

        private ImageSource DecodeImage()
        {
            try
            {
                var width = (int)(this.Source.Width * this.ZoomFactor.Value);
                var height = (int)(this.Source.Height * this.ZoomFactor.Value);


                //using (var image = System.Drawing.Image.FromFile(path))
                using (var canvas = new Bitmap(width, height))
                using (var graphics = Graphics.FromImage(canvas))
                {
                    graphics.DrawImage(this.Source, 0, 0, width, height);
                    return canvas.ToWPFBitmap();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}

