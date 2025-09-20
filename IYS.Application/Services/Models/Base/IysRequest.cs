using RestSharp;

namespace IYS.Application.Services.Models.Base
{
    public class IysRequest
    {
        public int IysCode { get; set; }
        public string Url { get; set; }
        public Method Method { get; set; }
    }

    public class IysRequest<T>
    {
        public int IysCode { get; set; }
        public string Url { get; set; }
        public Method Method { get; set; }
        public T Body { get; set; }
        public string Action { get; set; }
        public int? BatchId { get; set; }
    }
}
