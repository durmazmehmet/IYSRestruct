using IYS.Application.Services.Models.Base;

namespace IYS.Application.Services.Models.Response.Schedule
{
    public class PullRequestLog : ConsentParams
    {
        public string CompanyCode { get; set; }
        public string AfterId { get; set; }
        public DateTime UpdateDate { get; set; }
    }
}
