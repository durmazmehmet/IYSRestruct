namespace IYS.Application.Services.Models.Request
{
    public class BaseMailRequest
    {
        public required byte[] FileData { get; set; }
        public required string FileDate { get; set; }
    }
}