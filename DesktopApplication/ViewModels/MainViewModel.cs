using DesktopApplication.Core;
using Domain.Intrefaces;
using Domain.Models;
using System.Windows.Input;
using System.Windows.Threading;

namespace DesktopApplication.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region Constants
        private const int UPDATE_RATE = 20;
        #endregion

        #region Fields
        private readonly ISpectrumService _spectrumService;
        private readonly DispatcherTimer _timer;
        private bool _isRunning;

        private SpectrumData? _currentSpectrum;
        #endregion

        #region Properties and Commands
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }

        public bool IsRunning
        {
            get => _isRunning;
            private set => SetProperty(ref _isRunning, value);
        }

        public SpectrumData? CurrentSpectrum
        {
            get => _currentSpectrum;
            private set => SetProperty(ref _currentSpectrum, value);
        }
        #endregion

        #region Events
        public event EventHandler<SpectrumData>? SpectrumUpdated;
        #endregion

        public MainViewModel(ISpectrumService spectrumService)
        {
            _spectrumService = spectrumService ?? throw new ArgumentNullException(nameof(spectrumService));
                        
            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / UPDATE_RATE)
            };
            
            _timer.Tick += Timer_Tick;

            StartCommand = new RelayCommand(_ => Start());
            StopCommand = new RelayCommand(_ => Stop());

#if false
            SpectrumUpdated += OnSpectrumUpdated;
#endif
        }

        private void Start()
        {
            if(IsRunning)
                return;

            IsRunning = true;
            
            _timer.Start();
        }

        private void Stop()
        {
            if(!IsRunning)
                return;

            IsRunning = false;
            
            _timer.Stop();
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                var spectrum = await Task.Run(() =>
                    _spectrumService.GetSpectrumData());

                CurrentSpectrum = spectrum;
                SpectrumUpdated?.Invoke(this, spectrum);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating spectrum: {ex.Message}");
            }
        }

        private void OnSpectrumUpdated(object? sender, SpectrumData spectrumData)
        {
            double min = spectrumData.PowerLevels.Min();
            double max = spectrumData.PowerLevels.Max();
            double avg = spectrumData.PowerLevels.Average();

            System.Diagnostics.Debug.WriteLine($"[{spectrumData.Timestamp:HH:mm:ss.fff}] Spectrum Data:");
            System.Diagnostics.Debug.WriteLine($"  Points: {spectrumData.Length}");
            System.Diagnostics.Debug.WriteLine($"  Power: Min={min:F2} dBm, Max={max:F2} dBm, Avg={avg:F2} dBm");
            System.Diagnostics.Debug.WriteLine($"  Range: {min:F2} to {max:F2} dBm (Δ={max - min:F2} dB)");
            System.Diagnostics.Debug.WriteLine("");
        }
    }
}
