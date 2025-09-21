using System.Runtime.Serialization;

namespace IYS.Application.Services.Models.Response
{
    [DataContract]
    [Serializable]
    public class MailNotificationResponse
    {
        [DataMember]
        public MailNotificationResult Data { get; set; }

        [DataMember]
        public int Status { get; set; }
    }
    
    [DataContract]
    [Serializable]
    public class MailNotificationResult
    {
        [DataMember]
        public int? NotificationId { get; set; }
    }
}