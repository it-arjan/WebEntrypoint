using System.Web.Http;

namespace WebEntryPoint
{
    public class EntryQueuePostData
    {
        public string MessageId { get; set; }
        public string PostBackUrl { get; set; }
        public string SocketToken { get; set; }
        public string NotificationToken { get; set; }
            
        public string DoneToken { get; set; }
        public string UserName { get; set; }
        public string AspSessionId { get; set; }
            
    }
}
