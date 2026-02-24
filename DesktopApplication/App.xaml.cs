using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ApplicationLayer.Services;
using Infrastructure.Providers;
using Domain.Intrefaces;
using DesktopApplication.ViewModels;

namespace DesktopApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            services.AddSingleton<ISpectrumDataProvider>(sp =>
                new RandomSpectrumProvider(
                    dataPointsCount: 1024,
                    minimumPowerLevel: -120.0,
                    maximumPowerLevel: -20.0,
                    baseNoiseLevel: -100.0,
                    noiseAmplitude: 10.0));

            services.AddSingleton<ISpectrumService, SpectrumService>();

            services.AddTransient<MainViewModel>();
            services.AddSingleton<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            (_serviceProvider as IDisposable)?.Dispose();

            base.OnExit(e);
        }
    }
}
