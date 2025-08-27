using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace IYSIntegration.Common.Worker.Services
{
    public class SendConsentErrorService
    {
        private readonly ILogger<SendConsentErrorService> _logger;
        private readonly IWorkerDbHelper _dbHelper;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        private string SmallDateFormat => "yyyy.MM.dd";

        public SendConsentErrorService(IConfiguration configuration, ILogger<SendConsentErrorService> logger, IWorkerDbHelper dbHelper, IEmailService emailService)
        {
            _configuration = configuration;
            _logger = logger;
            _dbHelper = dbHelper;
            _emailService = emailService;
        }

        public async Task ProcessAsync()
        {
            try
            {
                _logger.LogInformation("SendConsentErrorService running at: {time}", DateTimeOffset.Now);

                var errorConsents = await _dbHelper.GetIYSConsentRequestErrors();
                if (errorConsents?.Count > 0)
                {
                    using var excelPackage = new ExcelPackage();
                    var dateString = DateTime.Today.AddDays(-1).ToString(SmallDateFormat);
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
                    var templateParameters = new Dictionary<string, string> { { "Today", dateString } };
                    await _emailService.SendMailAsync(
                        _configuration.GetValue<string>("IYSErrorMail:Subject"),
                        _configuration.GetValue<string>("IYSErrorMail:To"),
                        _configuration.GetValue<string>("IYSErrorMail:From"),
                        _configuration.GetValue<string>("IYSErrorMail:FromDisplayName"),
                        content,
                        "IYS_Consent_Error_" + dateString + ".xlsx",
                        templateParameters);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SendConsentErrorService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
            }
        }
    }
}
