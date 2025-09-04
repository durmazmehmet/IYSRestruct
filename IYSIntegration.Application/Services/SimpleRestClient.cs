using IYSIntegration.Application.Base;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System;

namespace IYSIntegration.Application.Services
{
    public sealed class SalesforceClient(IConfiguration c) : SimpleRestClient(c["BaseSfProxyUrl"]!) { }
    public sealed class IysClient(IConfiguration c) : SimpleRestClient(c["BaseIysProxyUrl"]!) { }

    public class SimpleRestClient
    {
        private readonly RestClient _client;

        public SimpleRestClient(string baseUrl, int timeoutMs = 30000)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL boş olamaz.", nameof(baseUrl));

            _client = new RestClient(new RestClientOptions(baseUrl)
            {
                MaxTimeout = timeoutMs,
                ThrowOnAnyError = false
            });
        }

        /// <summary>
        /// var list = await api.GetAsync<MyDto[]>("api/items", new Dictionary&lt;string,string?&gt; { ["q"]="memo" });
        /// </summary>
        public async Task<ResponseBase<T>> GetAsync<T>(
            string path,
            IDictionary<string, string?>? query = null,
            CancellationToken ct = default)
        {
            var req = new RestRequest(path, Method.Get);
            ApplyDefaultsForGet(req);

            if (query != null)
                foreach (var q in query)
                    req.AddQueryParameter(q.Key, q.Value ?? string.Empty);

            var resp = await _client.ExecuteAsync(req, ct);
            return ParseResponse<T>(resp);
        }

        /// <summary>
        /// var save = await api.PostJsonAsync&lt;Req,Resp&gt;("api/save", new { name="Memo" });
        /// </summary>
        public async Task<ResponseBase<TResp>> PostJsonAsync<TReq, TResp>(
            string path,
            TReq body,
            CancellationToken ct = default)
        {
            var req = new RestRequest(path, Method.Post);
            ApplyDefaultsForJson(req);
            req.AddStringBody(JsonConvert.SerializeObject(body), ContentType.Json);

            var resp = await _client.ExecuteAsync(req, ct);
            return ParseResponse<TResp>(resp);
        }

        /// <summary>
        /// var tokenRes = await api.PostFormAsync&lt;SfToken&gt;("oauth2/token", formDict);
        /// </summary>
        public async Task<ResponseBase<T>> PostFormAsync<T>(
            string path,
            IDictionary<string, string> form,
            CancellationToken ct = default)
        {
            var req = new RestRequest(path, Method.Post);
            ApplyDefaultsForForm(req);

            foreach (var kv in form)
                req.AddParameter(kv.Key, kv.Value, ParameterType.GetOrPost);

            var resp = await _client.ExecuteAsync(req, ct);
            return ParseResponse<T>(resp);
        }

        // ---- Defaults by method ----
        private static void ApplyDefaultsForGet(RestRequest req)
        {
            req.AddOrUpdateHeader("Accept", "application/json");
        }

        private static void ApplyDefaultsForJson(RestRequest req)
        {
            req.AddOrUpdateHeader("Accept", "application/json");
            req.AddOrUpdateHeader("Content-Type", "application/json");
        }

        private static void ApplyDefaultsForForm(RestRequest req)
        {
            req.AddOrUpdateHeader("Accept", "application/json");
            req.AddOrUpdateHeader("Content-Type", "application/x-www-form-urlencoded");
        }

        // ---- Parse to ResponseBase<T> ----
        private static ResponseBase<T> ParseResponse<T>(RestResponse resp)
        {
            var result = new ResponseBase<T>
            {
                HttpStatusCode = (int)resp.StatusCode,
                SendDate = DateTime.UtcNow.ToString("o") // ISO 8601
            };

            // Başarılı + içerik var → JSON parse etmeyi dene
            if (resp.IsSuccessful && !string.IsNullOrWhiteSpace(resp.Content))
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<T>(resp.Content!);
                    result.Success(data!);
                    return result;
                }
                catch (Exception ex)
                {
                    result.Error("JSON_PARSE_ERROR", $"JSON parse error: {ex.Message}");
                    // Ham içerik de dursun istersen:
                    if (!string.IsNullOrEmpty(resp.Content))
                        result.AddMessage("raw", Truncate(resp.Content, 4000));
                    return result;
                }
            }

            // Başarısız veya boş içerik
            var code = (int)resp.StatusCode;
            var msg = string.IsNullOrWhiteSpace(resp.ErrorMessage)
                ? $"HTTP {code} {resp.StatusDescription}"
                : $"HTTP {code} {resp.StatusDescription} | {resp.ErrorMessage}";

            result.Error("HTTP_ERROR", msg);

            // Hata gövdesini ek bilgi olarak mesajlara koy (çok uzun olmaması için kısalt)
            if (!string.IsNullOrWhiteSpace(resp.Content))
                result.AddMessage("raw", Truncate(resp.Content, 4000));

            return result;
        }

        private static string Truncate(string s, int max)
            => s.Length <= max ? s : s.Substring(0, max);
    }
}
