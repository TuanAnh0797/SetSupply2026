using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace APP.ComponentUI
{
    /// <summary>
    /// Interaction logic for RealtimeClock.xaml
    /// </summary>
    public partial class RealtimeClock : UserControl, INotifyPropertyChanged
    {
        private DispatcherTimer _timer;
        private DateTime _currentDateTime;

        #region Dependency Properties

        // Background, Border properties
        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
            "Background", typeof(Brush), typeof(RealtimeClock), new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty BorderBrushProperty = DependencyProperty.Register(
            "BorderBrush", typeof(Brush), typeof(RealtimeClock), new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty BorderThicknessProperty = DependencyProperty.Register(
            "BorderThickness", typeof(Thickness), typeof(RealtimeClock), new PropertyMetadata(new Thickness(0)));

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            "CornerRadius", typeof(CornerRadius), typeof(RealtimeClock), new PropertyMetadata(new CornerRadius(0)));

        public static readonly DependencyProperty ClockPaddingProperty = DependencyProperty.Register(
            "ClockPadding", typeof(Thickness), typeof(RealtimeClock), new PropertyMetadata(new Thickness(8)));

        // Time properties
        public static readonly DependencyProperty TimeFontSizeProperty = DependencyProperty.Register(
            "TimeFontSize", typeof(double), typeof(RealtimeClock), new PropertyMetadata(30.0));

        public static readonly DependencyProperty TimeFontWeightProperty = DependencyProperty.Register(
            "TimeFontWeight", typeof(FontWeight), typeof(RealtimeClock), new PropertyMetadata(FontWeights.Bold));

        public static readonly DependencyProperty TimeForegroundProperty = DependencyProperty.Register(
            "TimeForeground", typeof(Brush), typeof(RealtimeClock), new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty TimeHorizontalAlignmentProperty = DependencyProperty.Register(
            "TimeHorizontalAlignment", typeof(HorizontalAlignment), typeof(RealtimeClock), new PropertyMetadata(HorizontalAlignment.Center));

        public static readonly DependencyProperty TimeFormatProperty = DependencyProperty.Register(
            "TimeFormat", typeof(string), typeof(RealtimeClock), new PropertyMetadata("HH:mm:ss"));

        // Date properties
        public static readonly DependencyProperty ShowDateProperty = DependencyProperty.Register(
            "ShowDate", typeof(bool), typeof(RealtimeClock), new PropertyMetadata(true));

        public static readonly DependencyProperty DateFontSizeProperty = DependencyProperty.Register(
            "DateFontSize", typeof(double), typeof(RealtimeClock), new PropertyMetadata(24.0));

        public static readonly DependencyProperty DateFontWeightProperty = DependencyProperty.Register(
            "DateFontWeight", typeof(FontWeight), typeof(RealtimeClock), new PropertyMetadata(FontWeights.Normal));

        public static readonly DependencyProperty DateForegroundProperty = DependencyProperty.Register(
            "DateForeground", typeof(Brush), typeof(RealtimeClock), new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty DateHorizontalAlignmentProperty = DependencyProperty.Register(
            "DateHorizontalAlignment", typeof(HorizontalAlignment), typeof(RealtimeClock), new PropertyMetadata(HorizontalAlignment.Center));

        public static readonly DependencyProperty DateFormatProperty = DependencyProperty.Register(
            "DateFormat", typeof(string), typeof(RealtimeClock), new PropertyMetadata("dd/MM/yyyy"));

        // Day of Week properties
        public static readonly DependencyProperty ShowDayOfWeekProperty = DependencyProperty.Register(
            "ShowDayOfWeek", typeof(bool), typeof(RealtimeClock), new PropertyMetadata(true));

        public static readonly DependencyProperty DayFontSizeProperty = DependencyProperty.Register(
            "DayFontSize", typeof(double), typeof(RealtimeClock), new PropertyMetadata(24.0));

        public static readonly DependencyProperty DayFontWeightProperty = DependencyProperty.Register(
            "DayFontWeight", typeof(FontWeight), typeof(RealtimeClock), new PropertyMetadata(FontWeights.Normal));

        public static readonly DependencyProperty DayForegroundProperty = DependencyProperty.Register(
            "DayForeground", typeof(Brush), typeof(RealtimeClock), new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty DayHorizontalAlignmentProperty = DependencyProperty.Register(
            "DayHorizontalAlignment", typeof(HorizontalAlignment), typeof(RealtimeClock), new PropertyMetadata(HorizontalAlignment.Center));

        public static readonly DependencyProperty UpdateIntervalProperty = DependencyProperty.Register(
            "UpdateInterval", typeof(int), typeof(RealtimeClock), new PropertyMetadata(1000, OnUpdateIntervalChanged));

        #endregion

        #region Properties

        public new Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public new Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public new Thickness BorderThickness
        {
            get => (Thickness)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Thickness ClockPadding
        {
            get => (Thickness)GetValue(ClockPaddingProperty);
            set => SetValue(ClockPaddingProperty, value);
        }

        public double TimeFontSize
        {
            get => (double)GetValue(TimeFontSizeProperty);
            set => SetValue(TimeFontSizeProperty, value);
        }

        public FontWeight TimeFontWeight
        {
            get => (FontWeight)GetValue(TimeFontWeightProperty);
            set => SetValue(TimeFontWeightProperty, value);
        }

        public Brush TimeForeground
        {
            get => (Brush)GetValue(TimeForegroundProperty);
            set => SetValue(TimeForegroundProperty, value);
        }

        public HorizontalAlignment TimeHorizontalAlignment
        {
            get => (HorizontalAlignment)GetValue(TimeHorizontalAlignmentProperty);
            set => SetValue(TimeHorizontalAlignmentProperty, value);
        }

        public string TimeFormat
        {
            get => (string)GetValue(TimeFormatProperty);
            set => SetValue(TimeFormatProperty, value);
        }

        public bool ShowDate
        {
            get => (bool)GetValue(ShowDateProperty);
            set => SetValue(ShowDateProperty, value);
        }

        public double DateFontSize
        {
            get => (double)GetValue(DateFontSizeProperty);
            set => SetValue(DateFontSizeProperty, value);
        }

        public FontWeight DateFontWeight
        {
            get => (FontWeight)GetValue(DateFontWeightProperty);
            set => SetValue(DateFontWeightProperty, value);
        }

        public Brush DateForeground
        {
            get => (Brush)GetValue(DateForegroundProperty);
            set => SetValue(DateForegroundProperty, value);
        }

        public HorizontalAlignment DateHorizontalAlignment
        {
            get => (HorizontalAlignment)GetValue(DateHorizontalAlignmentProperty);
            set => SetValue(DateHorizontalAlignmentProperty, value);
        }

        public string DateFormat
        {
            get => (string)GetValue(DateFormatProperty);
            set => SetValue(DateFormatProperty, value);
        }

        public bool ShowDayOfWeek
        {
            get => (bool)GetValue(ShowDayOfWeekProperty);
            set => SetValue(ShowDayOfWeekProperty, value);
        }

        public double DayFontSize
        {
            get => (double)GetValue(DayFontSizeProperty);
            set => SetValue(DayFontSizeProperty, value);
        }

        public FontWeight DayFontWeight
        {
            get => (FontWeight)GetValue(DayFontWeightProperty);
            set => SetValue(DayFontWeightProperty, value);
        }

        public Brush DayForeground
        {
            get => (Brush)GetValue(DayForegroundProperty);
            set => SetValue(DayForegroundProperty, value);
        }

        public HorizontalAlignment DayHorizontalAlignment
        {
            get => (HorizontalAlignment)GetValue(DayHorizontalAlignmentProperty);
            set => SetValue(DayHorizontalAlignmentProperty, value);
        }

        public int UpdateInterval
        {
            get => (int)GetValue(UpdateIntervalProperty);
            set => SetValue(UpdateIntervalProperty, value);
        }

        // Display properties
        public string CurrentTime => _currentDateTime.ToString(TimeFormat);
        public string CurrentDate => _currentDateTime.ToString(DateFormat);
        public string CurrentDayOfWeek => _currentDateTime.ToString("dddd", CultureInfo.CurrentCulture);

        #endregion

        #region Constructor and Timer

        public RealtimeClock()
        {
            InitializeComponent();
            InitializeTimer();
            UpdateDateTime();
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(UpdateInterval);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDateTime();
        }

        private void UpdateDateTime()
        {
            _currentDateTime = DateTime.Now;
            OnPropertyChanged(nameof(CurrentTime));
            OnPropertyChanged(nameof(CurrentDate));
            OnPropertyChanged(nameof(CurrentDayOfWeek));
        }

        private static void OnUpdateIntervalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RealtimeClock clock && clock._timer != null)
            {
                clock._timer.Interval = TimeSpan.FromMilliseconds((int)e.NewValue);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        #region Cleanup
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            //this.Unloaded += UserControl_Unloaded;
        }
        //private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        //{
        //    _timer?.Stop();
        //    _timer = null;
        //}
        #endregion
    }
}
