using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request;
using IYSIntegration.Common.Request.Retailer;
using IYSIntegration.Common.Response.Retailer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace IYSIntegration.Application.Services
{
    public class RetailerService : IRetailerService
    {
        private readonly IConfiguration _config;
        private readonly IRestClientService _clientHelper;
        private readonly string _baseUrl;

        public RetailerService(IConfiguration config, IRestClientService clientHelper)
        {
            _config = config;
            _clientHelper = clientHelper;
            _baseUrl = _config.GetValue<string>($"BaseUrl");
        }

        public async Task<ResponseBase<GetRetailerResponse>> GetRetailer(GetRetailerRequest request)
        {
            var iysRequest = new IysRequest<DummyRequest>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/retailers/{request.RetailerCode}",
                Action = "Get Retailer"
            };

            return await _clientHelper.Execute<GetRetailerResponse, DummyRequest>(iysRequest);
        }

        public async Task<ResponseBase<GetAllRetailersResponse>> GetAllRetailers(GetAllRetailersRequest request)
        {
            // TODO: Query parametrelerinde pagesize olacak mı sorulacak
            var iysRequest = new IysRequest<DummyRequest>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/retailers",
                Action = "Get All Retailers"
            };

            return await _clientHelper.Execute<GetAllRetailersResponse, DummyRequest>(iysRequest);
        }

        public async Task<ResponseBase<AddRetailerResponse>> AddRetailer(AddRetailerRequest request)
        {
            string url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/retailers";
            var iysRequest = new IysRequest<Retailer>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents/request",
                Body = request.Retailer,
                Action = "Add Retailer"
            };

            return await _clientHelper.Execute<AddRetailerResponse, Retailer>(iysRequest);
        }

        public async Task<ResponseBase<DeleteRetailerResponse>> DeleteRetailer(DeleteRetailerRequest request)
        {
            string url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/retailers/{request.RetailerCode}";
            var iysRequest = new IysRequest<DummyRequest>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/retailers/{request.RetailerCode}",
                Action = "Delete Retailer"
            };

            return await _clientHelper.Execute<DeleteRetailerResponse, DummyRequest>(iysRequest);
        }
    }
}
