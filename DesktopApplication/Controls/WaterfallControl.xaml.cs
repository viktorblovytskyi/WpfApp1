using Domain.Models;
using System.Buffers;
using System.Buffers.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DesktopApplication.Controls
{
    public partial class WaterfallControl : UserControl
    {
        #region Constants

        private const double MinimumSize   = 10.0;
        private const int    BytesPerPixel = 4;
        private const int    PaletteSize   = 1024;

        #endregion        

        #region Fields

        private WriteableBitmap? _bitmap;
        private int  _bitmapWidth;
        private int  _bitmapHeight;
        private bool _isInitialized;
        private int  _updatesPending;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty PowerMinProperty =
            DependencyProperty.Register(
                nameof(PowerMin),
                typeof(double),
                typeof(WaterfallControl),
                new PropertyMetadata(-120.0, OnPowerRangeChanged));

        public static readonly DependencyProperty PowerMaxProperty =
            DependencyProperty.Register(
                nameof(PowerMax),
                typeof(double),
                typeof(WaterfallControl),
                new PropertyMetadata(-20.0, OnPowerRangeChanged));

        public static readonly DependencyProperty GradientStopsProperty =
            DependencyProperty.Register(
                nameof(GradientStops),
                typeof(GradientStopCollection),
                typeof(WaterfallControl),
                new PropertyMetadata(null, OnGradientStopsChanged));

        public static readonly DependencyProperty SpectrumDataProperty =
            DependencyProperty.Register(
                nameof(SpectrumData),
                typeof(SpectrumData),
                typeof(WaterfallControl),
                new PropertyMetadata(null, OnSpectrumDataChanged));

        #endregion

        #region Properties

        public double PowerMin
        {
            get => (double)GetValue(PowerMinProperty);
            set => SetValue(PowerMinProperty, value);
        }

        public double PowerMax
        {
            get => (double)GetValue(PowerMaxProperty);
            set => SetValue(PowerMaxProperty, value);
        }

        public GradientStopCollection? GradientStops
        {
            get => (GradientStopCollection?)GetValue(GradientStopsProperty);
            set => SetValue(GradientStopsProperty, value);
        }

        public SpectrumData? SpectrumData
        {
            get => (SpectrumData?)GetValue(SpectrumDataProperty);
            set => SetValue(SpectrumDataProperty, value);
        }

        #endregion

        public WaterfallControl()
        {
            InitializeComponent();

            // Default gradient if none specified.
            GradientStops = new GradientStopCollection
            {
                new GradientStop(Color.FromRgb(  0,   0, 255), 0.00),  // Blue
                new GradientStop(Color.FromRgb(  0, 255, 255), 0.25),  // Cyan
                new GradientStop(Color.FromRgb(  0, 255,   0), 0.50),  // Green
                new GradientStop(Color.FromRgb(255, 255,   0), 0.75),  // Yellow
                new GradientStop(Color.FromRgb(255,   0,   0), 1.00),  // Red
            };

            Loaded     += OnLoaded;
            SizeChanged += OnControlSizeChanged;
            WaterfallImage.SizeChanged += OnImageSizeChanged;
        }

        #region Palette

        private sealed record PaletteData(double PowerMin, double PowerMax, uint[] Colors);
        private volatile PaletteData? _paletteData;

        private static uint PackBgr32(Color c) =>
            (uint)(c.B | (c.G << 8) | (c.R << 16) | (255u << 24));

        private void RebuildPalette()
        {
            var stops = GradientStops;
            if (stops is null || stops.Count == 0) return;

            double powerMin = PowerMin;
            double powerMax = PowerMax;

            var colors = new uint[PaletteSize];
            for (int i = 0; i < PaletteSize; i++)
            {
                double t = i / (double)(PaletteSize - 1);
                colors[i] = PackBgr32(InterpolateGradient(stops, t));
            }

            _paletteData = new PaletteData(powerMin, powerMax, colors);
        }

        private static Color InterpolateGradient(GradientStopCollection stops, double offset)
        {
            offset = Math.Clamp(offset, 0.0, 1.0);

            for (int i = 0; i < stops.Count - 1; i++)
            {
                if (offset >= stops[i].Offset && offset <= stops[i + 1].Offset)
                {
                    double range = stops[i + 1].Offset - stops[i].Offset;
                    double t = range > 0 ? (offset - stops[i].Offset) / range : 0.0;
                    return LerpColor(stops[i].Color, stops[i + 1].Color, t);
                }
            }

            return stops[^1].Color;
        }

        private static Color LerpColor(Color from, Color to, double t) =>
            Color.FromRgb(
                (byte)(from.R + (to.R - from.R) * t),
                (byte)(from.G + (to.G - from.G) * t),
                (byte)(from.B + (to.B - from.B) * t));

        #endregion

        #region DP Callbacks

        private static void OnPowerRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WaterfallControl control)
                control.RebuildPalette();
        }

        private static void OnGradientStopsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WaterfallControl control)
                control.RebuildPalette();
        }

        private static void OnSpectrumDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WaterfallControl control && e.NewValue is SpectrumData data)
                _ = control.UpdateWaterfallAsync(data);
        }

        #endregion

        #region Event Handlers
        private void OnLoaded(object sender, RoutedEventArgs e) => InitializeBitmap();

        private void OnControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isInitialized && WaterfallImage.ActualWidth > MinimumSize && WaterfallImage.ActualHeight > MinimumSize)
                InitializeBitmap();
        }

        private void OnImageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > MinimumSize && e.NewSize.Height > MinimumSize && !_isInitialized)
                InitializeBitmap();
        }
        #endregion

        private void InitializeBitmap()
        {
            var (width, height) = GetEffectiveSize();
            if (width < MinimumSize || height < MinimumSize)
                return;

            _bitmapWidth  = (int)width;
            _bitmapHeight = (int)height;

            try
            {
                _bitmap = new WriteableBitmap(_bitmapWidth, _bitmapHeight, 96, 96, PixelFormats.Bgr32, null);
                WaterfallImage.Source = _bitmap;
                _isInitialized = true;
                ClearBitmap();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize waterfall bitmap: {ex.Message}");
            }
        }

        private (double Width, double Height) GetEffectiveSize()
        {
            double width  = WaterfallImage.ActualWidth;
            double height = WaterfallImage.ActualHeight;

            if (width < MinimumSize || height < MinimumSize)
            {
                if (WaterfallImage.Parent is FrameworkElement parent)
                {
                    width  = parent.ActualWidth;
                    height = parent.ActualHeight;
                }
            }

            return (width, height);
        }

        private void ClearBitmap()
        {
            if (_bitmap is null) return;

            int stride = _bitmapWidth * BytesPerPixel;
            byte[] empty = new byte[_bitmapHeight * stride];
            _bitmap.WritePixels(new Int32Rect(0, 0, _bitmapWidth, _bitmapHeight), empty, stride, 0);
        }

        public async Task UpdateWaterfallAsync(SpectrumData spectrumData)
        {
            if (_bitmap is null || _bitmapWidth < 1 || _bitmapHeight < 1 || _paletteData is null)
                return;

            if (Interlocked.CompareExchange(ref _updatesPending, 1, 0) != 0)
                return;

            var lineBuffer = ArrayPool<byte>.Shared.Rent(_bitmapWidth * BytesPerPixel);
            try
            {
                await Task.Run(() => FillLineBuffer(lineBuffer, spectrumData))
                          .ConfigureAwait(false);

                await Dispatcher.InvokeAsync(() => RenderLineToTop(lineBuffer), DispatcherPriority.Render);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(lineBuffer);

                Interlocked.Exchange(ref _updatesPending, 0);
            }
        }

        private void FillLineBuffer(byte[] buffer, SpectrumData spectrumData)
        {
            PaletteData? palette = _paletteData;
            if (palette is null) return;

            double powerMin = palette.PowerMin;
            double range    = palette.PowerMax - powerMin;
            int    lastIdx  = palette.Colors.Length - 1;

            for (int x = 0; x < _bitmapWidth; x++)
            {
                double peak  = SamplePeakPower(spectrumData, x, powerMin);
                int    index = (int)(Math.Clamp((peak - powerMin) / range, 0.0, 1.0) * lastIdx);

                BinaryPrimitives.WriteUInt32LittleEndian(
                    buffer.AsSpan(x * BytesPerPixel),
                    palette.Colors[index]);
            }
        }

        private double SamplePeakPower(SpectrumData spectrumData, int pixelX, double fallback)
        {
            int center = (int)((pixelX / (double)_bitmapWidth) * spectrumData.Length);
            int start  = Math.Max(0, center - 1);
            int end    = Math.Min(spectrumData.Length - 1, center + 1);

            double peak = fallback;
            for (int i = start; i <= end; i++)
                peak = Math.Max(peak, spectrumData[i]);

            return peak;
        }

        private void RenderLineToTop(byte[] lineBuffer)
        {
            if (_bitmap is null) return;

            try
            {
                _bitmap.Lock();
                ShiftBitmapDown();
                WriteTopLine(lineBuffer);
                _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmapWidth, _bitmapHeight));
            }
            finally
            {
                _bitmap.Unlock();
            }
        }

        private unsafe void ShiftBitmapDown()
        {
            byte* buffer = (byte*)_bitmap!.BackBuffer;
            int   stride = _bitmap.BackBufferStride;

            for (int y = _bitmapHeight - 1; y > 0; y--)
                Buffer.MemoryCopy(buffer + (y - 1) * stride, buffer + y * stride, stride, stride);
        }

        private unsafe void WriteTopLine(byte[] lineBuffer)
        {
            byte* buffer = (byte*)_bitmap!.BackBuffer;
            int   stride = _bitmap.BackBufferStride;

            fixed (byte* pLine = lineBuffer)
                Buffer.MemoryCopy(pLine, buffer, stride, _bitmapWidth * BytesPerPixel);
        }
    }
}
