namespace IYS.Application.Services.Models.Base
{
    public class CacheSettings
    {
        public double SlidingExpiration { get; set; }
        public int Db { get; set; }
        public string? ConnectionString { get; set; }
    }
}
