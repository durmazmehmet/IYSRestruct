using IYSIntegration.Application.Interface;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request.RetailerAccess;
using IYSIntegration.Common.Response.RetailerAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IYSIntegration.Application.Services
{
    public class RetailerAccessService : IRetailerAccessService
    {
        private readonly IConfiguration _config;
        private readonly IRestClientService _clientHelper;
        private readonly string _baseUrl;
        public RetailerAccessService(IConfiguration config, IRestClientService clientHelper)
        {
            _config = config;
            _clientHelper = clientHelper;
            _baseUrl = _config.GetValue<string>($"BaseUrl");
        }

        public async Task<ResponseBase<AddRetailerAccessResult>> AddRetailerAccess(AddRetailerAccessRequest request)
        {
            var iysRequest = new IysRequest<RetailerRecipientAccess>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents/retailers/access",
                Body = request.RetailerRecipientAccess,
                Action = "Add Retailer Access"
            };

            return await _clientHelper.Execute<AddRetailerAccessResult, RetailerRecipientAccess>(iysRequest);
        }

        public async Task<ResponseBase<DeleteAllRetailersAccessResult>> DeleteAllRetailersAccess(DeleteAllRetailersAccessRequest request)
        {
            var iysRequest = new IysRequest<RetailerRecipientAccess>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents/retailers/access/remove/all",
                Body = request.RetailerRecipientAccess,
                Action = "Delete All Retailers Access"
            };

            return await _clientHelper.Execute<DeleteAllRetailersAccessResult, RetailerRecipientAccess>(iysRequest);
        }

        public async Task<ResponseBase<UpdateRetailerAccessResult>> UpdateRetailerAccess(UpdateRetailerAccessRequest request)
        {
            var iysRequest = new IysRequest<RetailerRecipientAccess>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents/retailers/access",
                Body = request.RetailerRecipientAccess,
                Action = "Update Retailer Access"
            };

            return await _clientHelper.Execute<UpdateRetailerAccessResult, RetailerRecipientAccess>(iysRequest);
        }

        public async Task<ResponseBase<DeleteRetailerAccessResult>> DeleteRetailerAccess(DeleteRetailerAccessRequest request)
        {
            var iysRequest = new IysRequest<RetailerRecipientAccess>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consents/retailers/access/remove",
                Body = request.RetailerRecipientAccess,
                Action = "Delete Retailer Access"
            };

            return await _clientHelper.Execute<DeleteRetailerAccessResult, RetailerRecipientAccess>(iysRequest);
        }

        public async Task<ResponseBase<QueryRetailerAccessResult>> QueryRetailerAccess(QueryRetailerAccessRequest request)
        {
            // TODO: Query parametrelerinde pagesize olacak mı sorulacak
            var iysRequest = new IysRequest<RecipientKey>
            {
                IysCode = request.IysCode,
                Url = $"{_baseUrl}/sps/{request.IysCode}/brands/{request.BrandCode}/consent/retailers/access/list",
                Body = request.RecipientKey,
                Action = "Query Retailer Access"
            };

            return await _clientHelper.Execute<QueryRetailerAccessResult, RecipientKey>(iysRequest);
        }
    }
}
