using IYSIntegration.API.Interface;
using IYSIntegration.Common.Base;
using IYSIntegration.Common.Request;

namespace IYSIntegration.API.Service
{
    public class InfoService : IInfoService
    {
        private readonly IConfiguration _config;
        private readonly IRestClientHelper _clientHelper;
        private readonly string _baseUrl;
        public InfoService(IConfiguration config, IRestClientHelper clientHelper)
        {
            _config = config;
            _clientHelper = clientHelper;
            _baseUrl = _config.GetValue<string>($"BaseUrl");
        }

        public async Task<ResponseBase<List<City>>> GetCities(int iysCode)
        {
            string url = $"{_baseUrl}/info/cities";
            var iysRequest = new Common.Base.IysRequest<DummyRequest>
            {
                IysCode = iysCode,
                Url = $"{_baseUrl}/info/cities",
                Action = "Get Cities"
            };

            return await _clientHelper.Execute<List<City>, DummyRequest>(iysRequest);
        }

        public async Task<ResponseBase<City>> GetCity(int iysCode, string code)
        {
            var iysRequest = new Common.Base.IysRequest<DummyRequest>
            {
                IysCode = iysCode,
                Url = $"{_baseUrl}/info/cities/{code}",
                Action = "Get City"
            };

            return await _clientHelper.Execute<City, DummyRequest>(iysRequest);
        }

        public async Task<ResponseBase<Town>> GetTown(int iysCode, string townCode)
        {
            var iysRequest = new Common.Base.IysRequest<DummyRequest>
            {
                IysCode = iysCode,
                Url = $"{_baseUrl}/info/town/{townCode}",
                Action = "Get Town"
            };

            return await _clientHelper.Execute<Town, DummyRequest>(iysRequest);
        }

        public async Task<ResponseBase<List<Town>>> GetTowns(int iysCode)
        {
            var iysRequest = new Common.Base.IysRequest<DummyRequest>
            {
                IysCode = iysCode,
                Url = $"{_baseUrl}/info/town",
                Action = "Get Towns"
            };

            return await _clientHelper.Execute<List<Town>, DummyRequest>(iysRequest);
        }
    }
}
