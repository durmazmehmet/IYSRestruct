using IYSIntegration.Common.Base;
using IYSIntegration.Common.Worker.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace IYSIntegration.Common.Worker.Services
{
    public class SendConsentErrorService
    {
        private readonly ILogger<SendConsentErrorService> _logger;
        private readonly IWorkerDbHelper _dbHelper;
        private readonly IConfiguration _configuration;

        private string SmallDateFormat => "yyyy.MM.dd";

        public SendConsentErrorService(IConfiguration configuration, ILogger<SendConsentErrorService> logger, IWorkerDbHelper dbHelper)
        {
            _configuration = configuration;
            _logger = logger;
            _dbHelper = dbHelper;
        }

        public async Task<ResponseBase<ConsentErrorReport>> GetReportExcelAsync(DateTime date)
        {
            var response = new ResponseBase<ConsentErrorReport>();
            try
            {
                _logger.LogInformation("SendConsentErrorService running at: {time}", DateTimeOffset.Now);
                var errorConsents = await _dbHelper.GetIYSConsentRequestErrors(date);
                if (errorConsents?.Count > 0)
                {
                    using var excelPackage = new ExcelPackage();
                    var dateString = date.ToString(SmallDateFormat);
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

                    var content = excelPackage.GetAsByteArray();
                    response.Data = new ConsentErrorReport
                    {
                        ReportExcel = Convert.ToBase64String(content),
                        Count = errorConsents.Count
                    };
                }
                else
                {
                    response.Data = new ConsentErrorReport { ReportExcel = string.Empty, Count = 0 };
                }
            }
            catch (Exception ex)
            {
                response.Error("Exception", ex.Message);
                _logger.LogError("SendConsentErrorService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
            }
            return response;
        }

        public async Task<ResponseBase<List<Consent>>> GetReportJsonAsync(DateTime date)
        {
            var response = new ResponseBase<List<Consent>>();
            try
            {
                var errorConsents = await _dbHelper.GetIYSConsentRequestErrors(date);
                response.Data = errorConsents;
                if (errorConsents == null || errorConsents.Count == 0)
                {
                    response.AddMessage("Info", "No data");
                }
            }
            catch (Exception ex)
            {
                response.Error("Exception", ex.Message);
                _logger.LogError("SendConsentErrorService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
            }
            return response;
        }
    }
}
