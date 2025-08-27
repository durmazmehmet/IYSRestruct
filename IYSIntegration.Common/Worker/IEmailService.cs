using System.Collections.Generic;
using System.Threading.Tasks;

namespace IYSIntegration.Common.Worker
{
    public interface IEmailService
    {
        Task SendMailAsync(string subject, string to, string from, string fromDisplayName, byte[] attachment, string attachmentName, IDictionary<string, string> templateParameters);
    }
}
