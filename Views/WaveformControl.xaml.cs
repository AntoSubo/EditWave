using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace EditWave.Views
{
    public partial class WaveformControl : UserControl
    {
        public static readonly DependencyProperty SamplesProperty =
            DependencyProperty.Register(nameof(Samples), typeof(float[]), typeof(WaveformControl),
                new PropertyMetadata(null, OnSamplesChanged));

        public float[] Samples
        {
            get => (float[])GetValue(SamplesProperty);
            set => SetValue(SamplesProperty, value);
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(nameof(Duration), typeof(double), typeof(WaveformControl),
                new PropertyMetadata(0.0));

        public double Duration
        {
            get => (double)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public event Action<double, double> SelectionChanged;

        private bool _isSelecting;
        private double _selectionStartX;
        private double _selectionEndX;
        private Rectangle _selectionRect;

        public WaveformControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += (_, __) => Redraw();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _selectionRect = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromArgb(80, 255, 64, 129)),
                Visibility = Visibility.Collapsed
            };
            WaveCanvas.Children.Add(_selectionRect);
        }

        private static void OnSamplesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaveformControl)d).Redraw();
        }

        private void Redraw()
        {
            WaveTop.Points.Clear();
            WaveBottom.Points.Clear();

            if (Samples == null || Samples.Length == 0)
                return;

            double w = WaveCanvas.ActualWidth;
            double h = WaveCanvas.ActualHeight;

            if (w <= 0 || h <= 0)
                return;

            int width = (int)w;
            int step = Samples.Length / width;
            if (step < 1) step = 1;

            double half = h / 2;

            for (int x = 0; x < width; x++)
            {
                float max = 0;
                float min = 0;

                int start = x * step;
                int end = start + step;
                if (end > Samples.Length) end = Samples.Length;

                for (int i = start; i < end; i++)
                {
                    float v = Samples[i];
                    if (v > max) max = v;
                    if (v < min) min = v;
                }

                double yMax = half - (max * half);
                double yMin = half - (min * half);

                WaveTop.Points.Add(new Point(x, yMax));
                WaveBottom.Points.Add(new Point(x, yMin));
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _isSelecting = true;
            _selectionStartX = e.GetPosition(WaveCanvas).X;
            _selectionEndX = _selectionStartX;
            UpdateSelectionRect();
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isSelecting)
            {
                _selectionEndX = e.GetPosition(WaveCanvas).X;
                UpdateSelectionRect();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;

                double startX = Math.Min(_selectionStartX, _selectionEndX);
                double endX = Math.Max(_selectionStartX, _selectionEndX);

                double startSeconds = (startX / WaveCanvas.ActualWidth) * Duration;
                double endSeconds = (endX / WaveCanvas.ActualWidth) * Duration;

                SelectionChanged?.Invoke(startSeconds, endSeconds);

                _selectionRect.Visibility = Visibility.Collapsed;
            }
            base.OnMouseLeftButtonUp(e);
        }

        private void UpdateSelectionRect()
        {
            if (WaveCanvas.ActualWidth <= 0) return;

            double startX = Math.Min(_selectionStartX, _selectionEndX);
            double endX = Math.Max(_selectionStartX, _selectionEndX);
            double width = endX - startX;

            _selectionRect.Width = width;
            _selectionRect.Height = WaveCanvas.ActualHeight;
            Canvas.SetLeft(_selectionRect, startX);
            Canvas.SetTop(_selectionRect, 0);
            _selectionRect.Visibility = Visibility.Visible;
        }

        public void ClearSelection()
        {
            if (_selectionRect != null)
                _selectionRect.Visibility = Visibility.Collapsed;
        }
    }
}