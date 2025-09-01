using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request;
using IYSIntegration.Common.Request.Brand;
using IYSIntegration.Common.Response.Brand;
using Microsoft.Extensions.Configuration;
namespace IYSIntegration.Application.Services
{
    public class BrandService : IBrandService
    {
        private readonly IConfiguration _config;
        private readonly IRestClientService _clientHelper;
        private readonly string _baseUrl;
        public BrandService(IConfiguration config, IRestClientService clientHelper)
        {
            _config = config;
            _clientHelper = clientHelper;
            _baseUrl = _config.GetValue<string>($"BaseUrl");
        }

        public async Task<ResponseBase<List<Brand>>> GetBrands(GetBrandRequest request)
        {
            var iysRequest = new IysRequest<DummyRequest>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands",
                Action = "Get Brands"
            };

            return await _clientHelper.Execute<List<Brand>, DummyRequest>(iysRequest);
        }
    }
}
