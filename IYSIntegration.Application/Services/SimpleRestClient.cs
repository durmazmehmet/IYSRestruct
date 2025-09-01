using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;

namespace IYSIntegration.Application.Services;

public sealed class SalesforceClient(IConfiguration c) : MiniRest(c["BaseSfProxyUrl"]!){}
public sealed class IysClient(IConfiguration c) : MiniRest(c["BaseIysProxyUrl"]!){}

public class MiniRest
{
    private readonly RestClient _client;

    public MiniRest(string baseUrl, int timeoutMs = 30000)
    {
        _client = new RestClient(new RestClientOptions(baseUrl)
        {
            MaxTimeout = timeoutMs,
            ThrowOnAnyError = false
        });
    }

    /*

    */
    /// <summary>
    /// var api = new MiniRest("https://my-proxy.local");
    /// var list = await api.GetAsync<MyDto[]>("api/items", new Dictionary<string, string?>
    /// {
    ///    ["q"] = "memo",
    ///    ["take"] = "10"
    /// });
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <param name="query"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<ApiResult<T>> GetAsync<T>(
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
        return Parse<T>(resp);
    }

    /// <summary>
    /// var save = await api.PostJsonAsync<object, SaveResp>("api/save", new { name = "Memo", ok = true });
    /// </summary>
    /// <typeparam name="TReq"></typeparam>
    /// <typeparam name="TResp"></typeparam>
    /// <param name="path"></param>
    /// <param name="body"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<ApiResult<TResp>> PostJsonAsync<TReq, TResp>(
        string path,
        TReq body,
        CancellationToken ct = default)
    {
        var req = new RestRequest(path, Method.Post);
        ApplyDefaultsForJson(req);

        req.AddStringBody(JsonConvert.SerializeObject(body), ContentType.Json);

        var resp = await _client.ExecuteAsync(req, ct);
        return Parse<TResp>(resp);
    }


    /// <summary>
    /// var sf = new MiniRest(_config.GetValue<string>("Salesforce:BaseUrl"));
    /// var tokenRes = await sf.PostFormAsync<SfToken>(
    ///     "oauth2/token",
    /// new Dictionary<string, string>
    ///     {
    ///         ["username"] = cred.Username,
    ///         ["password"] = cred.Password,
    ///        ["grant_type"] = cred.GrantType,
    ///        ["client_id"] = cred.ClientId,
    ///         ["client_secret"] = cred.ClientSecret,
    /// });
    /// if (!tokenRes.Status) throw new Exception(tokenRes.Message);
    /// var token = tokenRes.Data!;
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <param name="form"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<ApiResult<T>> PostFormAsync<T>(
        string path,
        IDictionary<string, string> form,
        CancellationToken ct = default)
    {
        var req = new RestRequest(path, Method.Post);
        ApplyDefaultsForForm(req);

        foreach (var kv in form)
            req.AddParameter(kv.Key, kv.Value, ParameterType.GetOrPost);

        var resp = await _client.ExecuteAsync(req, ct);
        return Parse<T>(resp);
    }

    // ---- Defaults by method ----
    private static void ApplyDefaultsForGet(RestRequest req)
    {
        req.AddOrUpdateHeader("Accept", "application/json");
        // Başka otomatik header eklemeyeceğiz.
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

    // ---- Parse ----
    private static ApiResult<T> Parse<T>(RestResponse resp)
    {
        if (resp.IsSuccessful && !string.IsNullOrWhiteSpace(resp.Content))
        {
            try
            {
                var data = JsonConvert.DeserializeObject<T>(resp.Content!);
                return ApiResult<T>.Ok((int)resp.StatusCode, data!);
            }
            catch (Exception ex)
            {
                return ApiResult<T>.Fail((int)resp.StatusCode, $"JSON parse error: {ex.Message}", resp.Content);
            }
        }

        var code = (int)resp.StatusCode;
        var msg = string.IsNullOrWhiteSpace(resp.ErrorMessage)
            ? $"HTTP {code} {resp.StatusDescription}"
            : $"HTTP {code} {resp.StatusDescription} | {resp.ErrorMessage}";

        return ApiResult<T>.Fail(code, msg, resp.Content);
    }
}

public sealed record ApiResult<T>(
    bool Status,
    int StatusCode,
    string Message,
    T? Data,
    string? Raw)
{
    public static ApiResult<T> Ok(int statusCode, T data)
        => new(true, statusCode, "", data, null);

    public static ApiResult<T> Fail(int statusCode, string message, string? raw = null)
        => new(false, statusCode, message, default, raw);
}
