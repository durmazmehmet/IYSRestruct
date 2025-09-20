using IYS.Application.Services.Interface;
using IYS.Application.Services.Models.Base;
using IYS.Application.Services.Models.Request;
using IYS.Application.Services.Models.Response.Consent;
using IYS.Application.Services.Models.Response.Schedule;
using IYS.Application.Services.Models.Request;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace IYS.Application.Services
{
    public class SendConsentToSalesforceService
    {
        private readonly ILogger<SendConsentToSalesforceService> _logger;
        private readonly IDbService _dbService;
        private readonly IIysProxy _client;

        public SendConsentToSalesforceService(ILogger<SendConsentToSalesforceService> logger, IDbService dbHelper, IIysProxy client)
        {
            _logger = logger;
            _dbService = dbHelper;
            _client = client;
        }

        public async Task<ResponseBase<ScheduledJobStatistics>> RunAsync(int rowCount)
        {
            var response = new ResponseBase<ScheduledJobStatistics>();
            response.Success();
            int successCount = 0;
            int failedCount = 0;
            var results = new ConcurrentBag<LogResult>();

            _logger.LogInformation("SfConsentService running at: {time}", DateTimeOffset.Now);
            var consentRequests = await _dbService.GetPullConsentRequests(false, rowCount);
            if (consentRequests?.Count > 0)
            {
                var groupedConsents = consentRequests
                    .GroupBy(x => x.CompanyCode)
                    .ToList();

                _logger.LogInformation($"SfConsentService running at: {consentRequests.Count} records processing");
                foreach (var consentGroup in groupedConsents)
                {
                    var companyCode = consentGroup.Key;
                    var consentsInGroup = consentGroup.ToList();

                    try
                    {

                        var request = new SfConsentBase
                        {
                            CompanyCode = companyCode,
                            Consents = consentsInGroup.Select(consent =>
                            {
                                var recipient = consent.Recipient;
                                if (consent.Type != "EPOSTA" && recipient?.StartsWith("+90") == true)
                                {
                                    recipient = recipient.Substring(3);
                                }

                                return new Consent
                                {
                                    Id = consent.Id,
                                    ConsentDate = consent.ConsentDate,
                                    Source = consent.Source,
                                    Recipient = recipient,
                                    RecipientType = consent.RecipientType,
                                    Status = consent.Status,
                                    Type = consent.Type,
                                    RetailerCode = consent.RetailerCode,
                                    RetailerAccess = consent.RetailerAccess,
                                    CreationDate = null,
                                    TransactionId = null
                                };
                            }).ToList()
                        };

                        var addConsentResult = await _client.PostJsonAsync<SfConsentAddRequest, SfConsentAddResponse>("salesforce/AddConsent", new SfConsentAddRequest { Request = request });

                        var wsDescription = addConsentResult.Data?.WsDescription;
                        var successMessage = string.IsNullOrWhiteSpace(wsDescription) ? "Success" : wsDescription;
                        var failureMessage = !string.IsNullOrWhiteSpace(wsDescription)
                            ? wsDescription
                            : addConsentResult.OriginalError?.Message
                                ?? (addConsentResult.Messages != null && addConsentResult.Messages.Count > 0
                                    ? string.Join(" | ", addConsentResult.Messages.Select(kv => $"{kv.Key}:{kv.Value}"))
                                    : "Unknown error");

                        if (addConsentResult.IsSuccessful())
                        {
                            foreach (var consent in consentsInGroup)
                            {
                                var result = new SfConsentResult
                                {
                                    Id = consent.Id,
                                    IsSuccess = true,
                                    LogId = addConsentResult.LogId,
                                    Error = string.Empty,
                                };

                                await _dbService.UpdateSfConsentResponse(result);
                                successCount++;
                                results.Add(new LogResult
                                {
                                    Id = consent.Id,
                                    CompanyCode = companyCode,
                                    Status = "Success",
                                    Messages = new Dictionary<string, string> { { "Success", successMessage } }
                                });
                            }
                        }
                        else
                        {
                            response.Error();

                            foreach (var consent in consentsInGroup)
                            {
                                var result = new SfConsentResult
                                {
                                    Id = consent.Id,
                                    IsSuccess = false,
                                    LogId = addConsentResult.LogId,
                                    Error = failureMessage,
                                };

                                await _dbService.UpdateSfConsentResponse(result);
                                failedCount++;
                                results.Add(new LogResult
                                {
                                    Id = consent.Id,
                                    CompanyCode = companyCode,
                                    Status = "Failed",
                                    Messages = new Dictionary<string, string> { { "Error", failureMessage } }
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Error();
                        _logger.LogError("SfConsentService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");

                        foreach (var consent in consentsInGroup)
                        {
                            failedCount++;
                            results.Add(new LogResult
                            {
                                Id = consent.Id,
                                CompanyCode = companyCode,
                                Status = "Exception",
                                Messages = new Dictionary<string, string> { { "Exception", ex.Message } }
                            });
                        }
                    }
                }
            }

            foreach (var result in results)
            {
                response.AddMessage(result.GetMessages());
            }
            response.Data = new ScheduledJobStatistics
            {
                SuccessCount = successCount,
                FailedCount = failedCount
            };
            return response;
        }
    }
}
