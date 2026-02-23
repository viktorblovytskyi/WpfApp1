using Domain.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DesktopApplication.Controls
{
    public partial class SpectrumControl : UserControl
    {
        #region Constants
        private const double MinimumCanvasSize = 10.0;
        private const double SizeChangeThreshold = 50.0;
        private const double LabelVerticalOffset = 8.0;
        private const double LabelMinTopMargin = 2.0;
        private const double LabelBottomMargin = 25.0;
        private const double FrequencyLabelBottomMargin = 18.0;
        private const double FrequencyLabelOffset = 3.0;
        private const double FrequencyLabelLeftMargin = 45.0;
        private const double FrequencyLabelRightOffset = 25.0;
        private const double PowerLabelLeftMargin = 5.0;
        private const double SpectrumPathThickness = 1.5;
        private const double EdgeLineThickness = 1.5;
        private const double GridLineThickness = 0.5;
        private const int LabelFontSize = 11;
        #endregion

        #region Fields
        private bool _isGridInitialized;
        private Size _lastGridSize;
        private SemaphoreSlim _updateSemaphore = new(1, 1);
        #endregion

        #region Static Resources
        private static readonly Pen SpectrumPen;
        private static readonly Pen GridPen;
        private static readonly Pen AxisPen;
        private static readonly Typeface LabelTypeface;
        private static readonly Brush LabelBrush;
        private static readonly Brush LabelBackground;

        static SpectrumControl()
        {
            var spectrumBrush = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            spectrumBrush.Freeze();
            SpectrumPen = new Pen(spectrumBrush, SpectrumPathThickness);
            SpectrumPen.Freeze();

            var gridBrush = new SolidColorBrush(Color.FromArgb(120, 140, 140, 140));
            gridBrush.Freeze();
            GridPen = new Pen(gridBrush, GridLineThickness);
            GridPen.Freeze();

            var axisBrush = new SolidColorBrush(Color.FromArgb(150, 150, 150, 150));
            axisBrush.Freeze();
            AxisPen = new Pen(axisBrush, EdgeLineThickness);
            AxisPen.Freeze();

            LabelBrush = new SolidColorBrush(Color.FromRgb(180, 180, 180));
            LabelBrush.Freeze();

            LabelBackground = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));
            LabelBackground.Freeze();

            LabelTypeface = new Typeface("Segoe UI");
        }
        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty PowerMinProperty =
            DependencyProperty.Register(
                nameof(PowerMin),
                typeof(double),
                typeof(SpectrumControl),
                new PropertyMetadata(-120.0, OnGridParameterChanged));

        public static readonly DependencyProperty PowerMaxProperty =
            DependencyProperty.Register(
                nameof(PowerMax),
                typeof(double),
                typeof(SpectrumControl),
                new PropertyMetadata(-20.0, OnGridParameterChanged));

        public static readonly DependencyProperty FreqMinProperty =
            DependencyProperty.Register(
                nameof(FreqMin),
                typeof(double),
                typeof(SpectrumControl),
                new PropertyMetadata(90.0, OnGridParameterChanged));

        public static readonly DependencyProperty FreqMaxProperty =
            DependencyProperty.Register(
                nameof(FreqMax),
                typeof(double),
                typeof(SpectrumControl),
                new PropertyMetadata(110.0, OnGridParameterChanged));

        public static readonly DependencyProperty PowerGridLinesProperty =
            DependencyProperty.Register(
                nameof(PowerGridLines),
                typeof(int),
                typeof(SpectrumControl),
                new PropertyMetadata(6, OnGridParameterChanged));

        public static readonly DependencyProperty FreqGridLinesProperty =
            DependencyProperty.Register(
                nameof(FreqGridLines),
                typeof(int),
                typeof(SpectrumControl),
                new PropertyMetadata(11, OnGridParameterChanged));

        public static readonly DependencyProperty SpectrumDataProperty =
            DependencyProperty.Register(
                nameof(SpectrumData),
                typeof(SpectrumData),
                typeof(SpectrumControl),
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

        public double FreqMin
        {
            get => (double)GetValue(FreqMinProperty);
            set => SetValue(FreqMinProperty, value);
        }

        public double FreqMax
        {
            get => (double)GetValue(FreqMaxProperty);
            set => SetValue(FreqMaxProperty, value);
        }

        public int PowerGridLines
        {
            get => (int)GetValue(PowerGridLinesProperty);
            set => SetValue(PowerGridLinesProperty, value);
        }

        public int FreqGridLines
        {
            get => (int)GetValue(FreqGridLinesProperty);
            set => SetValue(FreqGridLinesProperty, value);
        }

        public SpectrumData? SpectrumData
        {
            get => (SpectrumData?)GetValue(SpectrumDataProperty);
            set => SetValue(SpectrumDataProperty, value);
        }

        #endregion       

        public SpectrumControl()
        {
            InitializeComponent();
            Loaded += SpectrumControl_Loaded;
            SizeChanged += SpectrumControl_SizeChanged;
        }

        private static void OnGridParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SpectrumControl control && control.IsLoaded)
            {
                control.DrawSpectrumGrid();
            }
        }

        private static void OnSpectrumDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SpectrumControl control && e.NewValue is SpectrumData spectrumData)
            {
                _ = control.UpdateSpectrumAsync(spectrumData);
            }
        }

        private void SpectrumControl_Loaded(object sender, RoutedEventArgs e)
        {
            DrawSpectrumGrid();
        }

        private void SpectrumControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isGridInitialized ||
                Math.Abs(e.NewSize.Width - _lastGridSize.Width) > SizeChangeThreshold ||
                Math.Abs(e.NewSize.Height - _lastGridSize.Height) > SizeChangeThreshold)
            {
                DrawSpectrumGrid();
                _lastGridSize = e.NewSize;
            }
        }

        private void DrawSpectrumGrid()
        {
            double width = RootGrid.ActualWidth;
            double height = RootGrid.ActualHeight;

            if (width < MinimumCanvasSize || height < MinimumCanvasSize)
                return;
            
            using (DrawingContext dc = GridHost.GetDrawingContext())
            {
                DrawPowerGrid(dc, width, height);
                DrawFrequencyGrid(dc, width, height);
            }

            _isGridInitialized = true;
        }

        private enum GridOrientation
        {
            Horizontal,
            Vertical
        }

        private void DrawPowerGrid(DrawingContext dc, double width, double height)
        {
            DrawGrid(dc, width, height, GridOrientation.Horizontal, 
                PowerMin, PowerMax, PowerGridLines, "dBm", 
                PowerLabelLeftMargin, LabelVerticalOffset, LabelMinTopMargin, LabelBottomMargin);
        }

        private void DrawFrequencyGrid(DrawingContext dc, double width, double height)
        {
            DrawGrid(dc, width, height, GridOrientation.Vertical, 
                FreqMin, FreqMax, FreqGridLines, "MHz", 
                FrequencyLabelOffset, FrequencyLabelBottomMargin, FrequencyLabelLeftMargin, FrequencyLabelRightOffset);
        }

        private void DrawGrid(DrawingContext dc, double width, double height, GridOrientation orientation,
                                double min, double max, int gridLines, string centerLineUnit,
                                double labelPrimaryMargin, double labelSecondaryMargin, double labelStartMargin, double labelEndMargin)
        {
            double range = max - min;
            double step = range / (gridLines - 1);

            for (int i = 0; i < gridLines; i++)
            {
                double value = min + i * step;
                double normalized = (value - min) / range;
                
                bool isEdgeLine = (i == 0 || i == gridLines - 1);
                Pen pen = isEdgeLine ? AxisPen : GridPen;

                Point start, end;
                double labelLeft, labelTop;

                switch (orientation)
                {
                    case GridOrientation.Horizontal:
                    {
                        double y = height - (normalized * height);
                        start = new Point(0, y);
                        end = new Point(width, y);

                        bool isCenterLine = (i == gridLines / 2);
                        string text = isCenterLine ? $"{value:F0} {centerLineUnit}" : $"{value:F0}";

                        var formattedText = CreateFormattedText(text);

                        labelTop = y - labelSecondaryMargin;
                        labelTop = Math.Clamp(labelTop, labelStartMargin, height - labelEndMargin);
                        labelLeft = labelPrimaryMargin;

                        dc.DrawLine(pen, start, end);
                        dc.DrawRectangle(LabelBackground, null, new Rect(labelLeft, labelTop, formattedText.Width, formattedText.Height));
                        dc.DrawText(formattedText, new Point(labelLeft, labelTop));
                        break;
                    }
                    case GridOrientation.Vertical:
                    {
                        double x = normalized * width;
                        start = new Point(x, 0);
                        end = new Point(x, height);

                        bool isCenterLine = Math.Abs(value - (min + max) / 2) < 0.1;
                        string text = isCenterLine ? $"{value:F0} {centerLineUnit}" : $"{value:F0}";

                        var formattedText = CreateFormattedText(text);

                        labelLeft = x + labelPrimaryMargin;
                        if (i == 0) labelLeft = labelStartMargin;
                        if (i == gridLines - 1) labelLeft = x - labelEndMargin;
                        labelTop = height - labelSecondaryMargin;

                        dc.DrawLine(pen, start, end);
                        dc.DrawRectangle(LabelBackground, null, new Rect(labelLeft, labelTop, formattedText.Width, formattedText.Height));
                        dc.DrawText(formattedText, new Point(labelLeft, labelTop));
                        break;
                    }
                }
            }
        }

        private FormattedText CreateFormattedText(string text)
        {
            return new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                LabelTypeface,
                LabelFontSize,
                LabelBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        public async Task UpdateSpectrumAsync(SpectrumData spectrumData)
        {
            double width = RootGrid.ActualWidth;
            double height = RootGrid.ActualHeight;

            if (width < 1 || height < 1)
                return;

            if (!await _updateSemaphore.WaitAsync(0))
                return;

            try
            {
                double powerMin = PowerMin;
                double powerMax = PowerMax;

                var geometry = await Task.Run(() =>
                    BuildSpectrumGeometry(spectrumData, width, height, powerMin, powerMax));

                if (geometry != null)
                {
                    using DrawingContext dc = SpectrumHost.GetDrawingContext();
                    dc.DrawGeometry(null, SpectrumPen, geometry);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating spectrum: {ex.Message}");
            }
            finally
            {
                _updateSemaphore.Release();
            }
        }

        private static StreamGeometry? BuildSpectrumGeometry(SpectrumData spectrumData, double width, double height, double powerMin, double powerMax)
        {
            try
            {
                var geometry = new StreamGeometry();
                int dataLength = spectrumData.Length;
                double powerRange = powerMax - powerMin;

                using (StreamGeometryContext ctx = geometry.Open())
                {
                    for (int i = 0; i < dataLength; i++)
                    {
                        double x = (i / (double)(dataLength - 1)) * width;
                        double normalizedPower = (spectrumData[i] - powerMin) / powerRange;
                        double y = height - (normalizedPower * height);
                        var point = new Point(x, y);

                        if (i == 0)
                            ctx.BeginFigure(point, isFilled: false, isClosed: false);
                        else
                            ctx.LineTo(point, isStroked: true, isSmoothJoin: false);
                    }
                }

                geometry.Freeze();
                return geometry;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating spectrum geometry: {ex.Message}");
                return null;
            }
        }
    }
}
