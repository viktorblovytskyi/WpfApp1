using Domain.Intrefaces;
using Domain.Models;
using Domain.Enums;

namespace Infrastructure.Providers
{
    public class RandomSpectrumProvider : ISpectrumDataProvider
    {
        private readonly Random _random;
        private readonly int _dataPointsCount;
        private readonly double _minimumPowerLevel;
        private readonly double _maximumPowerLevel;
        private readonly double _baseNoiseLevel;
        private readonly double _noiseAmplitude;
        private readonly SignalConfiguration[] _signalConfigurations;

        public ProviderType ProviderType => ProviderType.RandomSpectrumProvider;
        public bool IsReady => true;


        public RandomSpectrumProvider(
            int dataPointsCount,
            double minimumPowerLevel,
            double maximumPowerLevel,
            double baseNoiseLevel,
            double noiseAmplitude,
            SignalConfiguration[]? signalConfigurations = null)
        {
            _random = new Random();
            _dataPointsCount = dataPointsCount;
            _minimumPowerLevel = minimumPowerLevel;
            _maximumPowerLevel = maximumPowerLevel;
            _baseNoiseLevel = baseNoiseLevel;
            _noiseAmplitude = noiseAmplitude;
            _signalConfigurations = signalConfigurations ?? GetDefaultSignalConfigurations();
        }

        private SignalConfiguration[] GetDefaultSignalConfigurations()
        {
            return new[]
            {
                new SignalConfiguration(
                    StartPosition: 0.3,
                    EndPosition: 0.35,
                    Amplitude: 30.0),
                new SignalConfiguration(
                    StartPosition: 0.5,
                    EndPosition: 0.52,
                    Amplitude: 40.0),
                new SignalConfiguration(
                    StartPosition: 0.7,
                    EndPosition: 0.75,
                    Amplitude: 35.0)
            };
        }

        public SpectrumData FetchData()
        {
            double[] powerLevels = new double[_dataPointsCount];

            for (int i = 0; i < _dataPointsCount; i++)
            {
                double noiseValue = (_random.NextDouble() - 0.5) * _noiseAmplitude;
                double signalValue = CalculateSignalValue(i);

                powerLevels[i] = Math.Clamp(
                    _baseNoiseLevel + noiseValue + signalValue,
                    _minimumPowerLevel,
                    _maximumPowerLevel);
            }

            return new SpectrumData(powerLevels);
        }

        private double CalculateSignalValue(int dataPointIndex)
        {
            double signalValue = 0.0;

            foreach (var signalConfig in _signalConfigurations)
            {
                int startIndex = (int)(_dataPointsCount * signalConfig.StartPosition);
                int endIndex = (int)(_dataPointsCount * signalConfig.EndPosition);

                if (dataPointIndex > startIndex && dataPointIndex < endIndex)
                {
                    double signalWidth = endIndex - startIndex;
                    double normalizedPosition = (dataPointIndex - startIndex) / signalWidth;
                    signalValue += signalConfig.Amplitude * Math.Sin(normalizedPosition * Math.PI);
                }
            }

            return signalValue;
        }        
    }
}
