using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using Newtonsoft.Json;
using RestSharp;

namespace IYSIntegration.Application.Services;

public class SimpleRestClient : ISimpleRestClient
{
    private readonly RestClient _client;
    private string? _authScheme;
    private string? _authToken;

    public SimpleRestClient(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL boş olamaz.", nameof(baseUrl));

        _client = new RestClient(new RestClientOptions(baseUrl)
        {
            MaxTimeout = 30000,
            ThrowOnAnyError = false
        });
    }

    public SimpleRestClient AddAuthorization(string scheme, string token)
    {
        _authScheme = scheme;
        _authToken = token;
        return this;
    }

    private void ApplyAuthorization(RestRequest req)
    {
        if (!string.IsNullOrWhiteSpace(_authScheme) && !string.IsNullOrWhiteSpace(_authToken))
            req.AddOrUpdateHeader("Authorization", $"{_authScheme} {_authToken}");
    }

    // GET
    public async Task<ResponseBase<T>> GetAsync<T>(
        string path,
        IDictionary<string, string?>? query = null,
        CancellationToken ct = default)
    {
        var req = new RestRequest(path, Method.Get);
        ApplyDefaultsForGet(req);
        ApplyAuthorization(req);

        if (query != null)
            foreach (var q in query)
                req.AddQueryParameter(q.Key, q.Value ?? string.Empty);

        var resp = await _client.ExecuteAsync(req, ct);
        return ParseResponse<T>(resp);
    }

    // POST JSON
    public async Task<ResponseBase<TResp>> PostJsonAsync<TReq, TResp>(
        string path,
        TReq body,
        CancellationToken ct = default)
    {
        var req = new RestRequest(path, Method.Post);
        ApplyDefaultsForJson(req);
        ApplyAuthorization(req);
        req.AddStringBody(JsonConvert.SerializeObject(body), ContentType.Json);

        var resp = await _client.ExecuteAsync(req, ct);
        return ParseResponse<TResp>(resp);
    }

    // POST Form
    public async Task<ResponseBase<T>> PostFormAsync<T>(
        string path,
        IDictionary<string, string> form,
        CancellationToken ct = default)
    {
        var req = new RestRequest(path, Method.Post);
        ApplyDefaultsForForm(req);
        ApplyAuthorization(req);

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
                if (!string.IsNullOrEmpty(resp.Content))
                    result.AddMessage("raw", Truncate(resp.Content, 4000));
                return result;
            }
        }

        var code = (int)resp.StatusCode;
        var msg = string.IsNullOrWhiteSpace(resp.ErrorMessage)
            ? $"HTTP {code} {resp.StatusDescription}"
            : $"HTTP {code} {resp.StatusDescription} | {resp.ErrorMessage}";

        result.Error("HTTP_ERROR", msg);

        if (!string.IsNullOrWhiteSpace(resp.Content))
            result.AddMessage("raw", Truncate(resp.Content, 4000));

        return result;
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s.Substring(0, max);
}

