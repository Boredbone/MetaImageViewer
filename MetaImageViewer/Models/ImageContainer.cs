using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Boredbone.XamlTools;
using Boredbone.XamlTools.Extensions;
using MetaImageViewer.Tools;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace MetaImageViewer.Models
{
    public class ImageContainer : DisposableBase
    {
        public ReactiveProperty<ImageSource> Image { get; }
        public ReactiveProperty<double> ZoomFactor { get; }
        private Image Source { get; }
        public string Path { get; set; }


        public ImageContainer(System.Drawing.Image image)
        {
            this.Source = image;
            this.Source.AddTo(this.Disposables);

            this.ZoomFactor = new ReactiveProperty<double>(1.0).AddTo(this.Disposables);

            this.Image = this.ZoomFactor
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
