using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models.Base;
using Newtonsoft.Json;
using RestSharp;

namespace IYSIntegration.Application.Services;

public class IysProxy : IIysProxy
{
    private readonly RestClient _client;
    private readonly string _auth;
    public IysProxy(string baseUrl, string auth)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL boş olamaz.", nameof(baseUrl));

        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _client = new RestClient(new RestClientOptions(baseUrl)
        {
            MaxTimeout = 30000,
            ThrowOnAnyError = false
        });
    }

    public async Task<ResponseBase<TResp>> GetAsync<TResp>(
        string path,
        IDictionary<string, string?>? query = null,
        CancellationToken ct = default)
    {
        var req = new RestRequest(path, Method.Get);
        req.AddOrUpdateHeader("Accept", "application/json");
        req.AddOrUpdateHeader("Authorization", $"Basic {_auth}");

        if (query != null)
            foreach (var kv in query)
                req.AddQueryParameter(kv.Key, kv.Value ?? string.Empty);

        var resp = await _client.ExecuteAsync(req, ct);
        return ParseEnvelope<TResp>(resp);
    }

    public async Task<ResponseBase<TResp>> PostJsonAsync<TReq, TResp>(
        string path,
        TReq body,
        CancellationToken ct = default)
    {
        var req = new RestRequest(path, Method.Post);
        req.AddOrUpdateHeader("Accept", "application/json");
        req.AddOrUpdateHeader("Content-Type", "application/json");
        req.AddOrUpdateHeader("Authorization", $"Basic {_auth}");
        req.AddStringBody(JsonConvert.SerializeObject(body), ContentType.Json);

        var resp = await _client.ExecuteAsync(req, ct);
        return ParseEnvelope<TResp>(resp);
    }

    public async Task<ResponseBase<TResp>> PostFormAsync<TResp>(
        string path,
        IDictionary<string, string> form,
        CancellationToken ct = default)
    {
        var req = new RestRequest(path, Method.Post);
        req.AddOrUpdateHeader("Accept", "application/json");
        req.AddOrUpdateHeader("Content-Type", "application/x-www-form-urlencoded");
        req.AddOrUpdateHeader("Authorization", $"Basic {_auth}");

        foreach (var kv in form)
            req.AddParameter(kv.Key, kv.Value, ParameterType.GetOrPost);

        var resp = await _client.ExecuteAsync(req, ct);
        return ParseEnvelope<TResp>(resp);
    }

    public async Task<ResponseBase<TResp>> PutJsonAsync<TReq, TResp>(
        string path,
        TReq body,
        CancellationToken ct = default)
    {
        var req = new RestRequest(path, Method.Put);
        req.AddOrUpdateHeader("Accept", "application/json");
        req.AddOrUpdateHeader("Content-Type", "application/json");
        req.AddOrUpdateHeader("Authorization", $"Basic {_auth}");
        req.AddStringBody(JsonConvert.SerializeObject(body), ContentType.Json);

        var resp = await _client.ExecuteAsync(req, ct);
        return ParseEnvelope<TResp>(resp);
    }




    private static ResponseBase<T> ParseEnvelope<T>(RestResponse resp)
    {
        var code = (int)resp.StatusCode;
        var now = DateTime.UtcNow.ToString("o");
        var body = resp.Content;

        // 204 ise özel case
        if (code == 204)
        {
            return new ResponseBase<T>
            {
                HttpStatusCode = code,
                SendDate = now,
                Status = ServiceResponseStatuses.Success,
                Data = default,
                Messages = new Dictionary<string, string> { { "Info", "No content" } }
            };
        }

        try
        {
            var rb = JsonConvert.DeserializeObject<ResponseBase<T>>(body!);
            if (rb == null)
                return Fail<T>(code, "EMPTY_DESERIALIZED", "ResponseBase<T> null deserialize edildi.", now, body);

            if (rb.HttpStatusCode == 0) rb.HttpStatusCode = code;
            if (string.IsNullOrWhiteSpace(rb.SendDate)) rb.SendDate = now;
            rb.Messages ??= [];
            return rb;
        }
        catch (Exception ex)
        {
            return Fail<T>(code, "DESERIALIZE_ERROR", ex.Message, now, body);
        }
    }



    private static ResponseBase<T> Fail<T>(int httpCode, string key, string message, string nowIso, string? raw = null)
    {
        var r = new ResponseBase<T>
        {
            HttpStatusCode = httpCode,
            SendDate = nowIso,
            Status = ServiceResponseStatuses.Error,
            Messages = new Dictionary<string, string>()
        };
        r.AddMessage(key, message);
        if (!string.IsNullOrWhiteSpace(raw))
            r.AddMessage("raw", raw.Length > 4000 ? raw[..4000] : raw);
        return r;
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s.Substring(0, max);
}
