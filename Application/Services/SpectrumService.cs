using Domain.Intrefaces;
using Domain.Models;

namespace ApplicationLayer.Services
{
    public class SpectrumService : ISpectrumService 
    {
        private readonly ISpectrumDataProvider _spectrumDataProvider;

        public SpectrumService(ISpectrumDataProvider dataProvider)
        {
            _spectrumDataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        }

        public SpectrumData GetSpectrumData()
        {
            if (!_spectrumDataProvider.IsReady)
            {
                throw new InvalidOperationException($"Spectrum data provider '{_spectrumDataProvider.ProviderType}' is not ready.");
            }

            return _spectrumDataProvider.FetchData();
        }
    }
}
