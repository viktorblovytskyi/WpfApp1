using Domain.Models;
using Domain.Enums;

namespace Domain.Intrefaces
{
    public interface ISpectrumDataProvider
    {
        SpectrumData FetchData();

        ProviderType ProviderType { get; }

        bool IsReady { get; }
    }
}
