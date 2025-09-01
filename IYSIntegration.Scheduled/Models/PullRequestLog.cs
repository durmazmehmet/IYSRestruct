using IYSIntegration.Common.Base;
using System;

namespace IYSIntegration.Scheduled.Models
{
    public class PullRequestLog : ConsentParams
    {
        public string CompanyCode { get; set; }
        public string AfterId { get; set; }
        public DateTime UpdateDate { get; set; }
    }
}
