namespace IYS.Application.Services.Models.Base
{
    public class ConsentRequestLog : Consent
    {
        public int IysCode { get; set; }
        public int BrandCode { get; set; }
        public long Id { get; set; }
    }
}
