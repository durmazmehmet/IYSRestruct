using IYSIntegration.Common.Base;

namespace IYSIntegration.Application.Interface
{
    public interface IInfoService
    {
        Task<ResponseBase<List<Town>>> GetTowns(int iysCode);
        Task<ResponseBase<Town>> GetTown(int iysCode, string code);
        Task<ResponseBase<List<City>>> GetCities(int iysCode);
        Task<ResponseBase<City>> GetCity(int iysCode, string code);
    }
}
