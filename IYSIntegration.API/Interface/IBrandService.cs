using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.Brand;
using IYSIntegration.Common.Response.Brand;

namespace IYSIntegration.API.Interface
{
    public interface IBrandService
    {
        Task<ResponseBase<List<Brand>>> GetBrands(GetBrandRequest request);
    }
}
