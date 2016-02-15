using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Reactive.Disposables;
using System.Reactive.Linq;
//using Windows.Foundation;
using Boredbone.Utility.Extensions;
using Boredbone.XamlTools.Extensions;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media.Animation;

namespace MetaImageViewer.Views.Behaviors
{
    /// <summary>
    /// ScrollViewerでマウス操作の時も慣性スクロールを行うためのビヘイビア
    /// </summary>
    public class InertiaScrollViewerBehaviour : Behavior<ScrollViewer>, IDisposable
    {
        private Dictionary<string, IDisposable> Disposables { get; } = new Dictionary<string, IDisposable>();

        private Storyboard story;
        private bool isInartiaMoving;
        private bool isMouseCapturing;

        private Vector startOffset;

        private Vector startPosition;
        private Vector prevPosition;
        private Vector lastPosition;

        private double lastIntervalMilliSec = 1.0;

        private double endVelocity = 0.001;
        private double scrollableVelocityThreshold = 0.5;

        private double scrollableDisplacementThreshold = 3.0;

        private double maxTime = 2000.0;

        private const double _maxDamping = 7.0e-3;
        private const double _minDamping = 5.0e-4;

        private const double _maxDampingVelocity = 0.5;
        private const double _minDampingVelocity = 15.0;



        #region CurrentOffset

        public Point CurrentOffset
        {
            get { return (Point)GetValue(CurrentOffsetProperty); }
            set { SetValue(CurrentOffsetProperty, value); }
        }

        public static readonly DependencyProperty CurrentOffsetProperty =
            DependencyProperty.Register(nameof(CurrentOffset), typeof(Point), typeof(InertiaScrollViewerBehaviour),
            new PropertyMetadata(new Point(), new PropertyChangedCallback(OnCurrentOffsetChanged)));

        private static void OnCurrentOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisInstance = d as InertiaScrollViewerBehaviour;

            var viewer = thisInstance?.AssociatedObject ?? (d as ScrollViewer);

            if (viewer == null)
            {
                return;
            }

            var point = e.NewValue as Point?;

            if (point == null)
            {
                return;
            }

            viewer.ScrollToHorizontalOffset(point.Value.X);
            viewer.ScrollToVerticalOffset(point.Value.Y);

            //viewer.ChangeView(point.Value.X, point.Value.Y, null, !thisInstance.isInartiaMoving);
        }

        #endregion

        #region Damping

        public double MaxDamping
        {
            get { return (double)GetValue(MaxDampingProperty); }
            set { SetValue(MaxDampingProperty, value); }
        }

        public static readonly DependencyProperty MaxDampingProperty =
            DependencyProperty.Register(nameof(MaxDamping), typeof(double),
                typeof(InertiaScrollViewerBehaviour), new PropertyMetadata(_maxDamping));

        public double MinDamping
        {
            get { return (double)GetValue(MinDampingProperty); }
            set { SetValue(MinDampingProperty, value); }
        }

        public static readonly DependencyProperty MinDampingProperty =
            DependencyProperty.Register(nameof(MinDamping), typeof(double),
                typeof(InertiaScrollViewerBehaviour), new PropertyMetadata(_minDamping));


        public double MaxDampingVelocity
        {
            get { return (double)GetValue(MaxDampingVelocityProperty); }
            set { SetValue(MaxDampingVelocityProperty, value); }
        }

        public static readonly DependencyProperty MaxDampingVelocityProperty =
            DependencyProperty.Register(nameof(MaxDampingVelocity), typeof(double),
                typeof(InertiaScrollViewerBehaviour), new PropertyMetadata(_maxDampingVelocity));

        public double MinDampingVelocity
        {
            get { return (double)GetValue(MinDampingVelocityProperty); }
            set { SetValue(MinDampingVelocityProperty, value); }
        }

        public static readonly DependencyProperty MinDampingVelocityProperty =
            DependencyProperty.Register(nameof(MinDampingVelocity), typeof(double),
                typeof(InertiaScrollViewerBehaviour), new PropertyMetadata(_minDampingVelocity));


        #endregion

        /// <summary>
        /// アタッチ時の初期化処理
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();



            //破棄処理を登録
            Observable.FromEvent<RoutedEventHandler, RoutedEventArgs>(
              h => (sender, e) => h(e),
              h => AssociatedObject.Unloaded += h,
              h => AssociatedObject.Unloaded -= h)
              .Subscribe(e => this.Dispose())
              .AddTo(this.Disposables, "Unloaded");


            this.isInartiaMoving = false;
            this.isMouseCapturing = false;

            // マウスダウン、マウスアップ、マウスムーブのIObservable
            var mouseDown = Observable.FromEvent<MouseButtonEventHandler, MouseButtonEventArgs>(
                h => (s, e) => h(e),
                h => this.AssociatedObject.PreviewMouseLeftButtonDown += h,
                h => this.AssociatedObject.PreviewMouseLeftButtonDown -= h);

            var mouseMove = Observable.FromEvent<MouseEventHandler, MouseEventArgs>(
                h => (s, e) => h(e),
                h => this.AssociatedObject.MouseMove += h,
                h => this.AssociatedObject.MouseMove -= h);

            var mouseUp = Observable.FromEvent<MouseButtonEventHandler, MouseButtonEventArgs>(
                h => (s, e) => h(e),
                h => this.AssociatedObject.PreviewMouseLeftButtonUp += h,
                h => this.AssociatedObject.PreviewMouseLeftButtonUp -= h);



            mouseMove
                .SkipUntil(mouseDown.Do(e => this.OnDown(e)))
                .TakeUntil(mouseUp.Do(e => this.OnUp(e)))
                .Finally(() =>
                {
                    if (this.AssociatedObject?.IsMouseCaptured == true)
                    {
                        this.AssociatedObject.ReleaseMouseCapture();
                    }
                    this.isMouseCapturing = false;
                })
                .Repeat()
                .TimeInterval()
                .Subscribe(e =>
                {
                    this.lastIntervalMilliSec = e.Interval.TotalMilliseconds;
                    this.OnMove(e.Value);
                })
                .AddTo(this.Disposables, "Drag");

        }

        /// <summary>
        /// ダウン時動作
        /// </summary>
        /// <param name="e"></param>
        private void OnDown(MouseButtonEventArgs e)
        {
            // マウスムーブをマウスダウンまでスキップ。マウスダウン時にマウスをキャプチャ

            this.story?.Pause();

            var position = (Vector)e.GetPosition(this.AssociatedObject);

            this.startPosition = position;
            this.prevPosition = position;
            this.lastPosition = position;

            this.startOffset = new Vector(this.AssociatedObject.HorizontalOffset, this.AssociatedObject.VerticalOffset);


            this.isInartiaMoving = false;
            this.isMouseCapturing = false;

            //if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                this.AssociatedObject.CaptureMouse();//.CapturePointer(e.Pointer);
                this.isMouseCapturing = true;
            }
        }

        /// <summary>
        /// 移動中動作
        /// </summary>
        /// <param name="e"></param>
        private void OnMove(MouseEventArgs e)
        {
            this.isInartiaMoving = false;

            if (!this.isMouseCapturing)
            {
                return;
            }

            var point = (Vector)e.GetPosition(this.AssociatedObject);
            this.prevPosition = this.lastPosition;
            this.lastPosition = point;


            this.CurrentOffset = (Point)(this.startOffset - point + this.startPosition);
        }

        /// <summary>
        /// アップ時動作
        /// </summary>
        /// <param name="e"></param>
        private void OnUp(MouseButtonEventArgs e)
        {
            if (this.AssociatedObject?.IsMouseCaptured == true)
            {
                this.AssociatedObject.ReleaseMouseCapture();
            }


            //this.ReleaseMouseCapture();

            this.story?.Pause();

            if (!this.isMouseCapturing)
            {
                return;
            }

            var velocity = (lastPosition - prevPosition) / lastIntervalMilliSec;


            var displacement = this.startPosition - this.lastPosition;
            var vo = velocity.Length;

            if (vo > this.scrollableVelocityThreshold
                && displacement.Length > scrollableDisplacementThreshold)
            {
                var a = -Math.Log(vo / this.endVelocity);

                var damping = this.CalcDamping(vo);

                var time = Math.Min(-a / damping * vo / (vo - this.endVelocity), maxTime);

                var start = this.startOffset + displacement;
                var goal = start - velocity / damping;


                // 慣性スクロールアニメーション開始
                var storyboard = new Storyboard();
                storyboard.Children.Add(new PointAnimation()
                {
                    From = (Point)start,
                    To = (Point)goal,
                    Duration = new Duration(TimeSpan.FromMilliseconds(time)),
                    EasingFunction = new ExponentialEase()
                    {
                        Exponent = a,
                        EasingMode = EasingMode.EaseIn
                    },
                    //EnableDependentAnimation = true,
                });
                Storyboard.SetTarget(storyboard, this);
                Storyboard.SetTargetProperty(storyboard, new PropertyPath(nameof(CurrentOffset)));

                this.story = storyboard;
                
                storyboard.Begin();


            }

            this.isMouseCapturing = false;
        }

        /// <summary>
        /// 初速によって減衰係数を変更
        /// </summary>
        /// <param name="velocity"></param>
        /// <returns></returns>
        private double CalcDamping(double velocity)
        {
            if (velocity < MaxDampingVelocity)
            {
                return MaxDamping;
            }
            else if (velocity < MinDampingVelocity)
            {
                return (MinDamping - MaxDamping) / (MinDampingVelocity - MaxDampingVelocity)
                    * (velocity - MaxDampingVelocity) + MaxDamping;
            }
            else
            {
                return MinDamping;
            }
        }

        

        public void Dispose()
        {
            this.Disposables.ForEach(y => y.Value.Dispose());
            this.Disposables.Clear();
        }

        // ExponentialEase, EasingMode = EaseIn
        // のとき，
        // p(t) = po + dp(1 - exp(a * t / T)) / (1 - exp(a))
        // の関数に従って動く
        // po : 初期値
        // dp : 最終値 - 初期値
        // a: 係数
        // T : 動作時間

        // このとき，速度v(t)は以下の様になる
        // v(t) = dp(t) / dt = -a * dp / T / (1 - exp(a)) * exp(a * t / T)

        // v(t)の初期値と最終値をそれぞれ
        // v(0) = vo
        // v(T) = vc
        // (ただしvo > vc > 0)
        // とすると，vc = vo * exp(a)であるので
        // a = -ln(vo / vc)
        // である．

        // パラメータbを
        // b = -a / T
        // とおき，exp(a) << 1であるとすると，
        // vo = dp * b
        // であるので
        // dp = vo / b
        // となる．

        // また，
        // vc = -a / b * vo / T / (1 - exp(a)) * exp(a)
        // より
        // T = -a / b * vo / (vo - vc)
        // である．

    }
}
