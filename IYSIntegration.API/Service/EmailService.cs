using IYSIntegration.Common.Worker;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IYSIntegration.API.Service
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task SendMailAsync(string subject, string to, string from, string fromDisplayName, byte[] attachment, string attachmentName, IDictionary<string, string> templateParameters)
        {
            _logger.LogInformation("SendMailAsync called: {Subject} to {To}", subject, to);
            // TODO: Implement actual email sending
            return Task.CompletedTask;
        }
    }
}
