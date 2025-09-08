using IYSIntegration.Application.Services.Models.Error;

namespace IYSIntegration.Application.Services.Models.Base
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
            Status = ServiceResponseStatuses.Success;
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
            if (Messages == null)
            {
                Messages = new Dictionary<string, string>();
            }

            if (!Messages.ContainsKey(messageKey))
            {
                Messages.Add(messageKey, message);
            }
        }

        public void AddMessage(Dictionary<string, string> messagesToAdd)
        {
            if (messagesToAdd == null || messagesToAdd.Count == 0)
                return;

            if (Messages == null)
                Messages = new Dictionary<string, string>();

            foreach (var kvp in messagesToAdd)
            {
                if (!Messages.ContainsKey(kvp.Key))
                {
                    Messages[kvp.Key] = kvp.Value;
                }
                else
                {
                    Messages[kvp.Key] = $"{Messages[kvp.Key]}, {kvp.Value}";
                }
            }
        }


        public bool IsSuccessful()
        {
            return Status == ServiceResponseStatuses.Success;
        }

        public void Error()
        {
            Status = ServiceResponseStatuses.Error;
            HttpStatusCode = 500;
        }

        public void Error(string messageKey, string message)
        {
            Error();
            AddMessage(messageKey, message);
        }

        /// <summary>
        /// Mesaj ve hata kodu ile hatalı statü çeker
        /// </summary>
        public void Error(Dictionary<string, string> messages)
        {
            Error();
            foreach (KeyValuePair<string, string> keyValuePair in messages)
            {
                AddMessage(keyValuePair.Key, keyValuePair.Value);
            }
        }

        /// <summary>
        /// Baraşılı işlem
        /// </summary>
        public void Success()
        {
            Status = ServiceResponseStatuses.Success;
            HttpStatusCode = 200;
        }

        /// <summary>
        /// Başarılı işlem data set edilyor
        /// </summary>
        /// <param name="data"></param>
        public void Success(T data)
        {
            Data = data;
            Success();
        }

        /// <summary>
        /// Başarılı işlem data set edilyor
        /// </summary>
        /// <param name="data"></param>
        /// <param name="messageList"></param>
        public void Success(T data, Dictionary<string, string> messageList)
        {
            Data = data;
            Messages = messageList;
            Success();
        }

        /// <summary>
        /// Başarılı işlem data set edilyor ve hata mesajları siliniyor
        /// </summary>
        /// <param name="data"></param>
        /// <param name="clearMessages"></param>
        public void Success(T data, bool clearMessages)
        {
            Data = data;
            if (clearMessages)
            {
                Messages.Clear();
            }
            Success();
        }
    }
}
