using IYSIntegration.Application.Error;

namespace IYSIntegration.Application.Base
{
    public enum ServiceResponseStatuses : int
    {
        Error = 0,
        Success = 1,
        Waiting = 2
    }

    public class ResponseBase
    {

    }

    public class ResponseBase<T> : ResponseBase
    {
        public ResponseBase()
        {
            this.Status = ServiceResponseStatuses.Success;
            Messages = new Dictionary<string, string>();
        }

        public int HttpStatusCode { get; set; }
        public GenericError OriginalError { get; set; }
        public string SendDate { get; set; }
        public long Id { get; set; }
        public long LogId { get; set; }
        public ServiceResponseStatuses Status { get; set; }
        public T Data { get; set; }
        public Dictionary<string, string> Messages { get; set; }

        public static ResponseBase<T> CreateResponse(T data)
        {
            var response = new ResponseBase<T> { Status = ServiceResponseStatuses.Success, Data = data };
            return response;
        }

        public void AddMessage(string messageKey, string message)
        {
            if (this.Messages == null)
            {
                this.Messages = new Dictionary<string, string>();
            }

            if (!this.Messages.ContainsKey(messageKey))
            {
                this.Messages.Add(messageKey, message);
            }
        }

        public bool IsSuccessful()
        {
            return this.Status == ServiceResponseStatuses.Success;
        }

        public void Error()
        {
            this.Status = ServiceResponseStatuses.Error;
        }

        public void Error(string messageKey, string message)
        {
            this.Error();
            this.AddMessage(messageKey, message);
        }

        /// <summary>
        /// Mesaj ve hata kodu ile hatalı statü çeker
        /// </summary>
        public void Error(Dictionary<string, string> messages)
        {
            this.Error();
            foreach (KeyValuePair<string, string> keyValuePair in messages)
            {
                this.AddMessage(keyValuePair.Key, keyValuePair.Value);
            }
        }

        /// <summary>
        /// Baraşılı işlem
        /// </summary>
        public void Success()
        {
            this.Status = ServiceResponseStatuses.Success;
        }

        /// <summary>
        /// Başarılı işlem data set edilyor
        /// </summary>
        /// <param name="data"></param>
        public void Success(T data)
        {
            this.Data = data;
            this.Success();
        }

        /// <summary>
        /// Başarılı işlem data set edilyor
        /// </summary>
        /// <param name="data"></param>
        /// <param name="messageList"></param>
        public void Success(T data, Dictionary<string, string> messageList)
        {
            this.Data = data;
            this.Messages = messageList;
            this.Success();
        }

        /// <summary>
        /// Başarılı işlem data set edilyor ve hata mesajları siliniyor
        /// </summary>
        /// <param name="data"></param>
        /// <param name="clearMessages"></param>
        public void Success(T data, bool clearMessages)
        {
            this.Data = data;
            if (clearMessages)
            {
                this.Messages.Clear();
            }
            this.Success();
        }
    }
}
