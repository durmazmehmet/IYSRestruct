using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request;
using IYSIntegration.Common.Request.Retailer;
using IYSIntegration.Common.Response.Retailer;

namespace IYSIntegration.Application.Service
{
    public class RetailerService : IRetailerService
    {
        private readonly IConfiguration _config;
        private readonly IRestClientHelper _clientHelper;
        private readonly string _baseUrl;

        public RetailerService(IConfiguration config, IRestClientHelper clientHelper)
        {
            _config = config;
            _clientHelper = clientHelper;
            _baseUrl = _config.GetValue<string>($"BaseUrl");
        }

        public async Task<ResponseBase<GetRetailerResponse>> GetRetailer(GetRetailerRequest request)
        {
            var iysRequest = new Common.Base.IysRequest<DummyRequest>
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
            var iysRequest = new Common.Base.IysRequest<DummyRequest>
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
            var iysRequest = new Common.Base.IysRequest<Common.Base.Retailer>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents/request",
                Body = request.Retailer,
                Action = "Add Retailer"
            };

            return await _clientHelper.Execute<AddRetailerResponse, Common.Base.Retailer>(iysRequest);
        }

        public async Task<ResponseBase<DeleteRetailerResponse>> DeleteRetailer(DeleteRetailerRequest request)
        {
            string url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/retailers/{request.RetailerCode}";
            var iysRequest = new Common.Base.IysRequest<DummyRequest>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/retailers/{request.RetailerCode}",
                Action = "Delete Retailer"
            };

            return await _clientHelper.Execute<DeleteRetailerResponse, DummyRequest>(iysRequest);
        }
    }
}
