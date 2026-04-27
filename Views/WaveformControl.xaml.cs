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
        private double _zoom = 1.0;
        private int _visibleStart;
        private int _visibleEnd;

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
        private bool _isPanning;
        private double _panStartX;
        private int _panStartIndex;

        public WaveformControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += (_, __) => UpdateVisibleRange();
            MouseWheel += OnMouseWheel;
            MouseRightButtonDown += OnRightMouseDown;
            MouseMove += OnMouseMovePan;
            MouseRightButtonUp += OnRightMouseUp;
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
            var control = (WaveformControl)d;
            control._zoom = 1.0;
            control.UpdateVisibleRange();
            control.Redraw();
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                double oldZoom = _zoom;
                if (e.Delta > 0)
                    _zoom *= 1.2;
                else
                    _zoom /= 1.2;
                _zoom = Math.Max(1.0, Math.Min(50.0, _zoom));

                if (Math.Abs(oldZoom - _zoom) > 0.001)
                {
                    double posX = e.GetPosition(WaveCanvas).X;
                    int centerSample = GetSampleIndexFromX(posX);
                    UpdateVisibleRange(centerSample);
                    Redraw();
                }
                e.Handled = true;
            }
        }

        private void OnRightMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isPanning = true;
            _panStartX = e.GetPosition(WaveCanvas).X;
            _panStartIndex = _visibleStart;
            Cursor = Cursors.ScrollAll;
            e.Handled = true;
        }

        private void OnMouseMovePan(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                double deltaX = e.GetPosition(WaveCanvas).X - _panStartX;
                if (WaveCanvas.ActualWidth > 0)
                {
                    int sampleDelta = (int)((deltaX / WaveCanvas.ActualWidth) * (_visibleEnd - _visibleStart));
                    int newStart = _panStartIndex - sampleDelta;
                    if (newStart < 0) newStart = 0;
                    int total = Samples?.Length ?? 0;
                    int visibleSamples = _visibleEnd - _visibleStart;
                    if (newStart + visibleSamples > total) newStart = total - visibleSamples;
                    if (newStart < 0) newStart = 0;
                    _visibleStart = newStart;
                    _visibleEnd = _visibleStart + visibleSamples;
                    Redraw();
                }
                e.Handled = true;
            }
        }

        private void OnRightMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            Cursor = Cursors.Arrow;
            e.Handled = true;
        }

        private int GetSampleIndexFromX(double x)
        {
            if (WaveCanvas.ActualWidth <= 0 || Samples == null) return 0;
            double relative = x / WaveCanvas.ActualWidth;
            return _visibleStart + (int)(relative * (_visibleEnd - _visibleStart));
        }

        private void UpdateVisibleRange(int? centerSample = null)
        {
            if (Samples == null || Samples.Length == 0 || WaveCanvas.ActualWidth <= 0)
                return;

            int totalSamples = Samples.Length;
            double samplesPerPixel = _zoom;
            int visibleSamples = (int)(WaveCanvas.ActualWidth * samplesPerPixel);
            if (visibleSamples >= totalSamples)
            {
                _visibleStart = 0;
                _visibleEnd = totalSamples;
                return;
            }

            if (centerSample.HasValue)
            {
                int center = centerSample.Value;
                int half = visibleSamples / 2;
                int start = center - half;
                if (start < 0) start = 0;
                if (start + visibleSamples > totalSamples) start = totalSamples - visibleSamples;
                _visibleStart = start;
                _visibleEnd = _visibleStart + visibleSamples;
            }
            else
            {
                int oldStart = _visibleStart;
                int oldEnd = _visibleEnd;
                if (oldEnd <= 0 || oldStart >= totalSamples)
                {
                    _visibleStart = 0;
                    _visibleEnd = visibleSamples;
                }
                else
                {
                    int newStart = oldStart;
                    if (newStart + visibleSamples > totalSamples) newStart = totalSamples - visibleSamples;
                    if (newStart < 0) newStart = 0;
                    _visibleStart = newStart;
                    _visibleEnd = _visibleStart + visibleSamples;
                }
            }
        }

        private void Redraw()
        {
            WaveTop.Points.Clear();
            WaveBottom.Points.Clear();

            if (Samples == null || Samples.Length == 0 || WaveCanvas.ActualWidth <= 0)
                return;

            double w = WaveCanvas.ActualWidth;
            double h = WaveCanvas.ActualHeight;
            double half = h / 2;

            int visibleSamples = _visibleEnd - _visibleStart;
            if (visibleSamples <= 0)
            {
                UpdateVisibleRange();
                visibleSamples = _visibleEnd - _visibleStart;
                if (visibleSamples <= 0) return;
            }

            for (int x = 0; x < (int)w; x++)
            {
                int sampleStart = _visibleStart + (int)((double)x / w * visibleSamples);
                int sampleEnd = _visibleStart + (int)((double)(x + 1) / w * visibleSamples);
                if (sampleStart >= Samples.Length) break;
                if (sampleEnd > Samples.Length) sampleEnd = Samples.Length;
                if (sampleStart >= sampleEnd) continue;

                float max = float.MinValue;
                float min = float.MaxValue;
                for (int i = sampleStart; i < sampleEnd; i++)
                {
                    float v = Samples[i];
                    if (v > max) max = v;
                    if (v < min) min = v;
                }
                if (max < min) { max = 0; min = 0; }

                double yMax = half - (max * half);
                double yMin = half - (min * half);
                WaveTop.Points.Add(new Point(x, yMax));
                WaveBottom.Points.Add(new Point(x, yMin));
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                double x = e.GetPosition(WaveCanvas).X;
                double seconds = (x / WaveCanvas.ActualWidth) * Duration;
                SelectionChanged?.Invoke(seconds, seconds);
                return;
            }
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

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            _zoom = 1.0;
            UpdateVisibleRange();
            Redraw();
            base.OnMouseDoubleClick(e);
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