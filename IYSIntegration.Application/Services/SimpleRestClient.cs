using IYSIntegration.Common.Base;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace IYSIntegration.Application.Services
{
    public class SimpleRestClient
    {
        private readonly HttpClient _httpClient;

        public SimpleRestClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task<ResponseBase<T>> GetAsync<T>(string url)
        {
            var httpResponse = await _httpClient.GetAsync(url);
            var content = await httpResponse.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<ResponseBase<T>>(content) ?? new ResponseBase<T>();
            response.HttpStatusCode = (int)httpResponse.StatusCode;
            return response;
        }
    }
}
