using System.Runtime.Serialization;

namespace IYS.Application.Services.Models.Request
{
    [DataContract]
    [Serializable]
    public class MailNotificationRequest
    {
        /// <summary>
        /// Bildirim Id
        /// </summary>
        [DataMember]
        public int? Id { get; set; }

        /// <summary>
        /// Gönderime başlama tarihi
        /// </summary>
        [DataMember]
        public DateTime? StartDateToSend { get; set; }

        /// <summary>
        /// Gönderim bitiş tarihi
        /// </summary>
        [DataMember]
        public DateTime? EndDateToSend { get; set; }

        /// <summary>
        /// Uygulama adı
        /// </summary>
        [DataMember]
        public string ApplicationName { get; set; }
        /// <summary>
        /// Metot adı
        /// </summary>
        [DataMember]
        public string MethodName { get; set; }
        /// <summary>
        /// Mailin kimden gönderileceği(mail adresi)
        /// </summary>
        [DataMember]
        public string From { get; set; }
        /// <summary>
        /// Mailin kimden gönderileceği(ad-soyad/grup ismi)
        /// </summary>
        [DataMember]
        public string FromDisplayName { get; set; }
        /// <summary>
        /// Mailin kimlere gönderileceği
        /// </summary>
        [DataMember]
        public string To { get; set; }
        /// <summary>
        /// Mail CC
        /// </summary>
        [DataMember]
        public string Cc { get; set; }
        /// <summary>
        /// Mail BCC
        /// </summary>
        [DataMember]
        public string Bcc { get; set; }
        /// <summary>
        /// Mail konusu
        /// </summary>
        [DataMember]
        public string Subject { get; set; }
        /// <summary>
        /// Mail içeriği
        /// </summary>
        [DataMember]
        public string Body { get; set; }
        /// <summary>
        /// Şablon Id si
        /// </summary>
        [DataMember]
        public int? TemplateId { get; set; }
        /// <summary>
        /// Şablon adı
        /// </summary>
        [DataMember]
        public string TemplateName { get; set; }
        /// <summary>
        /// Şube kodu
        /// </summary>
        [DataMember]
        public string BranchCode { get; set; }
        /// <summary>
        /// Marka Id
        /// </summary>
        [DataMember]
        public string MakeCode { get; set; }

        /// <summary>
        /// Bildirimi gönderen uygulamadaki bildirim Idsi - Dış Id
        /// </summary>
        [DataMember]
        public string ExternalId { get; set; }

        /// <summary>
        /// Bildirim öncelik seviyesi
        /// </summary>
        [DataMember]
        public int? NotificationPriorityLevel { get; set; }
        /// <summary>
        /// Şablon kullanılıyorsa konu içerisindeki alanlar için parametreler
        /// </summary>
        [DataMember]
        public Dictionary<string, string> TemplateSubjectParameters { get; set; }

        /// <summary>
        /// Şablon kullanılıyorsa içerik içerisindeki alanlar için parametreler
        /// </summary>
        [DataMember]
        public Dictionary<string, string> TemplateBodyParameters { get; set; }

        /// <summary>
        /// Maile eklenecek olan dosyalar
        /// </summary>
        [DataMember]
        public List<AttachmentRequest> Attachments { get; set; }

        /// <summary>
        /// Email gönderildikten sonra mailin eki saklansın mı yoksa silinsin mi
        /// </summary>
        [DataMember]
        public bool KeepAttachment { get; set; } = false;
    }
}