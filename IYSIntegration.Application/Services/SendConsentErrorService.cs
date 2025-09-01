using IYSIntegration.Application.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace IYSIntegration.Application.Services
{
    public class SendConsentErrorService
    {
        private readonly ILogger<SendConsentErrorService> _logger;
        private readonly IDbService _dbService;
        private readonly IConfiguration _configuration;
        private readonly IVirtualInterface _client;
        private string SmallDateFormat => "yyyy.MM.dd";

        public SendConsentErrorService(IConfiguration configuration, ILogger<SendConsentErrorService> logger, IDbService dbHelper, IVirtualInterface client)
        {
            _configuration = configuration;
            _logger = logger;
            _dbService = dbHelper;
            _client = client;
        }

        public async Task RunAsync()
        {
            try
            {
                _logger.LogInformation("SendConsentErrorService running at: {time}", DateTimeOffset.Now);

                var errorConsents = await _dbService.GetIYSConsentRequestErrors();
                if (errorConsents?.Count > 0)
                {
                    using (var excelPackage = new ExcelPackage())
                    {
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

                        var baseLogPath = _configuration.GetValue<string>("LogPath");
                        var filePath = Path.Combine(baseLogPath, "IYS_Consent_Error_" + dateString + ".xlsx");
                        var fileInfo = new FileInfo(filePath);
                        excelPackage.SaveAs(fileInfo);

                        var content = File.ReadAllBytes(filePath);
                        var templateParameters = new List<ArrayOfArrayOfKeyValueOfstringstringKeyValueOfstringstringKeyValueOfstringstring>();
                        templateParameters.Add(new ArrayOfArrayOfKeyValueOfstringstringKeyValueOfstringstringKeyValueOfstringstring { Key = "Today", Value = dateString });
                        var mailNotificationRequest = new MailNotificationRequest
                        {
                            ApplicationName = "IYS",
                            TemplateId = 3189,
                            Subject = _configuration.GetValue<string>("IYSErrorMail:Subject"),
                            TemplateBodyParameters = templateParameters.ToArray(),
                            To = _configuration.GetValue<string>("IYSErrorMail:To"),
                            From = _configuration.GetValue<string>("IYSErrorMail:From"),
                            FromDisplayName = _configuration.GetValue<string>("IYSErrorMail:FromDisplayName"),
                            TemplateName = "IYS Consent Error",
                        };

                        var attachmentRequest = new AttachmentRequest { };
                        attachmentRequest.Contents = content;
                        attachmentRequest.FileName = "IYS_Consent_Error_" + dateString + ".xlsx";
                        mailNotificationRequest.Attachments = new List<AttachmentRequest> { attachmentRequest }.ToArray();
                        _client.InsertMailNotificationNoToken(new InsertMailNotificationNoTokenRequest { request = mailNotificationRequest });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SendConsentErrorService Hata: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message ?? "None");
            }
        }
    }
}
