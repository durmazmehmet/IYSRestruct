using IYSIntegration.Application.Services.Models.Base;

namespace IYSIntegration.Application.Services.Models
{
    public class PullRequestLog : ConsentParams
    {
        public string CompanyCode { get; set; }
        public string AfterId { get; set; }
        public DateTime UpdateDate { get; set; }
    }
}
