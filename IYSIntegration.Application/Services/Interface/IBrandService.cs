using IYSIntegration.Application.Base;
using IYSIntegration.Application.Request.Brand;
using IYSIntegration.Application.Response.Brand;

namespace IYSIntegration.Application.Services.Interface
{
    public interface IBrandService
    {
        Task<ResponseBase<List<Brand>>> GetBrands(GetBrandRequest request);
    }
}
