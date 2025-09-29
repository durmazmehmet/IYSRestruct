using IYS.Application.Services.Interface;
using IYS.Application.Services.Models.Base;
using IYS.Application.Services.Models.Request;
using IYS.Application.Services.Models.Response.Schedule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace IYS.Application.Services
{
    public class ErrorReportingService
    {
        private readonly ILogger<ErrorReportingService> _logger;
        private readonly IDbService _dbService;
        private readonly IConfiguration _configuration;
        private readonly IIysProxy _client;
        private string SmallDateFormat => "yyyy.MM.dd";

        public ErrorReportingService(ILogger<ErrorReportingService> logger, IDbService dbHelper, IConfiguration configuration, IIysProxy client)
        {
            _logger = logger;
            _dbService = dbHelper;
            _configuration = configuration;
            _client = client;
        }

        public async Task<ResponseBase<string>> GetErrorsExcelBase64Async(DateTime? date = null)
        {
            var response = new ResponseBase<string>();
            response.Success();
            ExcelPackage.License.SetNonCommercialOrganization("IYSIntegration");

            try
            {
                _logger.LogInformation("SendConsentErrorService.GetErrorsExcelBase64Async running at: {time}", DateTimeOffset.Now);

                var errorConsents = await _dbService.GetIYSConsentRequestErrors(date);
                if (errorConsents?.Count > 0)
                {
                    using (var excelPackage = new ExcelPackage())
                    {
                        var dateString =  date.HasValue ? date.Value.ToString(SmallDateFormat) : DateTime.Today.AddDays(-1).ToString(SmallDateFormat);
                        excelPackage.Workbook.Properties.Title = $"IYS Consent Errors: {dateString}";
                        excelPackage.Workbook.Worksheets.Add("Errors");
                        var excelWorksheet = excelPackage.Workbook.Worksheets[0];
                        excelWorksheet.Name = "Errors";

                        int rowIndex = 1;
                        int columnIndex = 1;
                        do
                        {
                            var cell = excelWorksheet.Cells[rowIndex, columnIndex];
                            var fill = cell.Style.Fill;
                            fill.PatternType = ExcelFillStyle.Solid;
                            fill.BackgroundColor.SetColor(Color.LightGray);
                            columnIndex++;
                        } while (columnIndex != 12);


                        columnIndex = 1;
                        excelWorksheet.Cells[1, columnIndex++].Value = "Id";
                        excelWorksheet.Cells[1, columnIndex++].Value = "Salesforce Id";
                        excelWorksheet.Cells[1, columnIndex++].Value = "Create Date";
                        excelWorksheet.Cells[1, columnIndex++].Value = "Company Code";
                        excelWorksheet.Cells[1, columnIndex++].Value = "Recipient";
                        excelWorksheet.Cells[1, columnIndex++].Value = "Recipient Type";
                        excelWorksheet.Cells[1, columnIndex++].Value = "Consent Date";
                        excelWorksheet.Cells[1, columnIndex++].Value = "Source";
                        excelWorksheet.Cells[1, columnIndex++].Value = "Status";
                        excelWorksheet.Cells[1, columnIndex++].Value = "Type";
                        excelWorksheet.Cells[1, columnIndex++].Value = "Error";

                        for (int i = 0; i < errorConsents.Count; i++)
                        {
                            columnIndex = 1;
                            var user = errorConsents[i];
                            excelWorksheet.Cells[i + 2, columnIndex++].Value = user.Id;
                            excelWorksheet.Cells[i + 2, columnIndex++].Value = user.SalesforceId;
                            excelWorksheet.Cells[i + 2, columnIndex++].Value = user.CreateDate;
                            excelWorksheet.Cells[i + 2, columnIndex++].Value = user.CompanyCode;
                            if (user.Type == "EPOSTA")
                            {
                                var index = user.Recipient.LastIndexOf("@");
                                if (index >= 2)
                                    excelWorksheet.Cells[i + 2, columnIndex++].Value = user.Recipient.Substring(0, 2) + new String('*', index - 2) + user.Recipient.Substring(index);
                            }
                            else
                            {
                                if (user.Recipient.Length >= 7)
                                {
                                    excelWorksheet.Cells[i + 2, columnIndex++].Value = user.Recipient.Substring(0, user.Recipient.Length - 7) + new string('*', 5) + user.Recipient.Substring(user.Recipient.Length - 2);
                                }
                            }
                            excelWorksheet.Cells[i + 2, columnIndex++].Value = user.RecipientType;
                            excelWorksheet.Cells[i + 2, columnIndex++].Value = user.ConsentDate;
                            excelWorksheet.Cells[i + 2, columnIndex++].Value = user.Source;
                            excelWorksheet.Cells[i + 2, columnIndex++].Value = user.Status;
                            excelWorksheet.Cells[i + 2, columnIndex++].Value = user.Type;
                            excelWorksheet.Cells[i + 2, columnIndex++].Value = user.BatchError;
                        }
                        excelWorksheet.Cells.AutoFitColumns();
                        var baseLogPath = _configuration.GetValue<string>("LogPath");

                        var filePath = Path.Combine(baseLogPath, "IYS_Consent_Error_" + dateString + ".xlsx");

                        var fileInfo = new FileInfo(filePath);
                        excelPackage.SaveAs(fileInfo);

                        var content = File.ReadAllBytes(filePath);

                        var fileName = $"IYS_Consent_Error_{dateString}.xlsx";


                        var soapXml = BuildSoapEnvelope(fileName, Convert.ToBase64String(content), dateString);

                        using var client = new HttpClient();
                        var request = new HttpRequestMessage(HttpMethod.Post,
                            $"{_configuration.GetValue<string>("IYSErrorMail:Url")}/Services/NotificationService.svc");
                        request.Headers.Add("SOAPAction", "\"http://tempuri.org/INotificationService/InsertMailNotificationsNoToken\"");
                        request.Content = new StringContent(soapXml, Encoding.UTF8, "text/xml");

                        var bresponse = await client.SendAsync(request);
                        var result = await bresponse.Content.ReadAsStringAsync();

                        if (bresponse.IsSuccessStatusCode)
                        {
                            var doc = XDocument.Parse(result);
                            XNamespace respNs = "http://schemas.datacontract.org/2004/07/SmartDMS.NotificationService.Model.Response";
                            var notifId = doc.Descendants(respNs + "NotificationId").FirstOrDefault()?.Value;     
                            response.Data = $"Notification Id:{notifId ?? "Mail Failed"}";
                            response.Status = ServiceResponseStatuses.Success;
                        }
                        else
                        {
                            throw new Exception($"IYS Error API failed with status {bresponse.StatusCode}: {result}");
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SendConsentErrorService.GetErrorsExcelBase64Async Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                response.Error();
                response.AddMessage("Hata", ex.Message);
            }

            return response;
        }

        private string BuildSoapEnvelope(string fileName, string base64Content, string fileDate)
        {
            var subject = _configuration.GetValue<string>("IYSErrorMail:Subject");
            var to = _configuration.GetValue<string>("IYSErrorMail:To");
            var from = _configuration.GetValue<string>("IYSErrorMail:From");
            var fromDisplayName = _configuration.GetValue<string>("IYSErrorMail:FromDisplayName");

            return $@"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:smar=""http://schemas.datacontract.org/2004/07/SmartDMS.NotificationService.Model.Request"" xmlns:arr=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
   <soapenv:Header/>
   <soapenv:Body>
      <tem:InsertMailNotificationsNoToken>
         <tem:request>
            <smar:MailNotificationRequest>
               <smar:ApplicationName>IYS</smar:ApplicationName>
               <smar:Attachments>
                  <smar:AttachmentRequest>
                     <smar:Contents>{base64Content}</smar:Contents>
                     <smar:FileName>{fileName}</smar:FileName>
                  </smar:AttachmentRequest>
               </smar:Attachments>
               <smar:From>{from}</smar:From>
               <smar:FromDisplayName>{fromDisplayName}</smar:FromDisplayName>
               <smar:TemplateBodyParameters>
                  <arr:KeyValueOfstringstring>
                     <arr:Key>Today</arr:Key>
                     <arr:Value>{fileDate}</arr:Value>
                  </arr:KeyValueOfstringstring>
               </smar:TemplateBodyParameters>
               <smar:TemplateId>3189</smar:TemplateId>
               <smar:Subject>{subject}</smar:Subject>
               <smar:To>{to}</smar:To>
            </smar:MailNotificationRequest>
         </tem:request>
      </tem:InsertMailNotificationsNoToken>
   </soapenv:Body>
</soapenv:Envelope>";


        }


        public async Task<ResponseBase<List<ConsentErrorModel>>> GetErrorsJsonAsync(DateTime? date = null)
        {
            var response = new ResponseBase<List<ConsentErrorModel>>();
            response.Success();

            try
            {
                _logger.LogInformation("SendConsentErrorService.GetErrorsJsonAsync running at: {time}", DateTimeOffset.Now);
                var errorConsents = await _dbService.GetIYSConsentRequestErrors(date) ?? new List<ConsentErrorModel>();
                PopulateBatchErrors(errorConsents);
                response.Success(errorConsents);
            }
            catch (Exception ex)
            {
                _logger.LogError("SendConsentErrorService.GetErrorsJsonAsync Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                response.AddMessage("Hata", ex.Message);
                response.Error();
            }

            return response;
        }

        public async Task<ResponseBase<ConsentErrorReportStatsResult>> GetErrorReportStatsAsync(DateTime? date = null)
        {
            var response = new ResponseBase<ConsentErrorReportStatsResult>();
            response.Success();

            try
            {
                _logger.LogInformation("SendConsentErrorService.GetErrorReportStatsAsync running at: {time}", DateTimeOffset.Now);
                var errorConsents = await _dbService.GetIYSConsentRequestErrors(date) ?? new List<ConsentErrorModel>();
                PopulateBatchErrors(errorConsents);

                var stats = errorConsents
                    .GroupBy(consent => string.IsNullOrWhiteSpace(consent.CompanyCode) ? "UNKNOWN" : consent.CompanyCode!)
                    .Select(group =>
                    {
                        var errorDetails = group
                            .SelectMany(consent => consent.BatchErrorModel?.Errors ?? Enumerable.Empty<ConsentBatchErrorItem>())
                            .ToList();

                        var codes = errorDetails
                            .GroupBy(detail => string.IsNullOrWhiteSpace(detail.Code) ? "UNKNOWN" : detail.Code!)
                            .Select(codeGroup => new ConsentErrorCodeStats
                            {
                                Code = codeGroup.Key,
                                Count = codeGroup.Count(),
                                Messages = codeGroup
                                    .Select(detail => detail.Message)
                                    .Where(message => !string.IsNullOrWhiteSpace(message))
                                    .Distinct()
                                    .OrderBy(message => message)
                                    .ToList()
                            })
                            .OrderBy(stat => stat.Code)
                            .ToList();

                        return new ConsentErrorCompanyStats
                        {
                            CompanyCode = group.Key,
                            ConsentCount = group.Count(),
                            ErrorCount = errorDetails.Count,
                            Codes = codes
                        };
                    })
                    .OrderByDescending(stat => stat.ErrorCount)
                    .ThenBy(stat => stat.CompanyCode)
                    .ToList();

                var requestedRangeEnd = date ?? DateTime.Today;
                var requestedRangeStart = requestedRangeEnd.AddDays(-1);

                var createDates = errorConsents
                    .Select(consent => consent.CreateDate)
                    .OrderBy(value => value)
                    .ToList();

                string dataRangeStart = null;
                string dataRangeEnd = null;

                if (createDates.Count > 0)
                {
                    dataRangeStart = createDates.First();
                    dataRangeEnd = createDates.Last();
                }

                var reportResult = new ConsentErrorReportStatsResult
                {
                    DataRangeStart = dataRangeStart,
                    DataRangeEnd = dataRangeEnd,
                    Companies = stats
                };

                response.Success(reportResult);
            }
            catch (Exception ex)
            {
                _logger.LogError("SendConsentErrorService.GetErrorReportStatsAsync Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                response.AddMessage("Hata", ex.Message);
                response.Error();
            }

            return response;
        }

        private void PopulateBatchErrors(IEnumerable<ConsentErrorModel> consents)
        {
            foreach (var consent in consents)
            {
                consent.BatchErrorModel = DeserializeBatchError(consent.BatchError);
            }
        }

        private ConsentBatchErrorModel? DeserializeBatchError(string? batchError)
        {
            if (string.IsNullOrWhiteSpace(batchError))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<ConsentBatchErrorModel>(batchError);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("SendConsentErrorService.DeserializeBatchError JsonException: {Message}. Content: {Content}", ex.Message, batchError);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("SendConsentErrorService.DeserializeBatchError Exception: {Message}. Content: {Content}", ex.Message, batchError);
            }

            return null;
        }
    }
}
