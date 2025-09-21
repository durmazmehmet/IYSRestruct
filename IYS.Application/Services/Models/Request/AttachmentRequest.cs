using System.Runtime.Serialization;

namespace IYS.Application.Services.Models.Request
{
    [DataContract]
    [Serializable]
    public class AttachmentRequest
    {
        /// <summary>
        /// Dosya Adı
        /// </summary>
        [DataMember]
        public string FileName { get; set; }

        /// <summary>
        /// Byte array olarak içerik
        /// </summary>
        [DataMember]
        public byte[] Contents { get; set; }

        /// <summary>
        /// Dosya dizini/yolu
        /// </summary>
        [DataMember]
        public string FilePath { get; set; }

        /// <summary>
        /// Maile eklenecek olan dosyanın alınacağı network yolu
        /// </summary>
        [DataMember]
        public string UncPath { get; set; }

        /// <summary>
        /// Maile eklenecek olan dosyanın alınacağı web adresi
        /// </summary>
        [DataMember]
        public string WebUrl { get; set; }

        /// <summary>
        /// Dosya Azure Storage Service'de private bir şekilde host ediliyorsa, bu değer true set edilmelidir.
        /// </summary>
        [DataMember]
        public bool IsSecurelyStoredInAzure { get; set; }

        /// <summary>
        /// Dosyanın host edildiği storage account adı. Burada gönderilen değere göre Azure Keyvault üzerinden ilgili storage account'a ait Secret Key bilgisi alınır ve dosya bu Secret Key ile çekilir.
        /// </summary>
        [DataMember]
        public string AzureStorageAccountName { get; set; }
    }
}