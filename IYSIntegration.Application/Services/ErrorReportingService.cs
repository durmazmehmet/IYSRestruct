using IYSIntegration.Application.Services.Interface;
using IYSIntegration.Application.Services.Models;
using IYSIntegration.Application.Services.Models.Base;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Linq;
using System.Drawing;

namespace IYSIntegration.Application.Services
{
    public class ErrorReportingService
    {
        private readonly ILogger<ErrorReportingService> _logger;
        private readonly IDbService _dbService;
        private string SmallDateFormat => "yyyy.MM.dd";

        public ErrorReportingService(ILogger<ErrorReportingService> logger, IDbService dbHelper)
        {
            _logger = logger;
            _dbService = dbHelper;
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
                        var queryDate = (date ?? DateTime.Today).ToString(SmallDateFormat);
                        excelPackage.Workbook.Properties.Title = $"IYS Consent Errors: {queryDate}";
                        var excelWorksheet = excelPackage.Workbook.Worksheets.Add("Errors");

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
                            excelWorksheet.Cells[i + 2, columnIndex++].Value = user.CreateDate?.ToString("yyyy-MM-dd HH:mm:ss");
                            excelWorksheet.Cells[i + 2, columnIndex++].Value = user.CompanyCode;
                            if (user.Type == "EPOSTA")
                            {
                                var index = user.Recipient.LastIndexOf("@");
                                if (index >= 2)
                                    excelWorksheet.Cells[i + 2, columnIndex++].Value = user.Recipient.Substring(0, 2) + new string('*', index - 2) + user.Recipient.Substring(index);
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

                        var bytes = excelPackage.GetAsByteArray();
                        response.Success(Convert.ToBase64String(bytes));
                    }
                }
                response.Success(string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError("SendConsentErrorService.GetErrorsExcelBase64Async Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                response.Error();
                response.AddMessage("Hata", ex.Message);
            }

            return response;
        }

        public async Task<ResponseBase<List<Consent>>> GetErrorsJsonAsync(DateTime? date = null)
        {
            var response = new ResponseBase<List<Consent>>();
            response.Success();

            try
            {
                _logger.LogInformation("SendConsentErrorService.GetErrorsJsonAsync running at: {time}", DateTimeOffset.Now);
                var errorConsents = await _dbService.GetIYSConsentRequestErrors(date) ?? new List<Consent>();
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

        public async Task<ResponseBase<List<ConsentErrorCompanyStats>>> GetErrorReportStatsAsync(DateTime? date = null)
        {
            var response = new ResponseBase<List<ConsentErrorCompanyStats>>();
            response.Success();

            try
            {
                _logger.LogInformation("SendConsentErrorService.GetErrorReportStatsAsync running at: {time}", DateTimeOffset.Now);
                var errorConsents = await _dbService.GetIYSConsentRequestErrors(date) ?? new List<Consent>();
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

                response.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError("SendConsentErrorService.GetErrorReportStatsAsync Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
                response.AddMessage("Hata", ex.Message);
                response.Error();
            }

            return response;
        }

        private void PopulateBatchErrors(IEnumerable<Consent> consents)
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
