using System.Web.Http;

namespace WebEntryPoint
{
    public class EntryQueuePostData
    {
        public string MessageId { get; set; }
        public string PostBackUrl { get; set; }
        
        public string SocketAccessToken { get; set; }
        public string SocketQmFeed { get; set; }
        public string SocketNotificationFeed { get; set; }
        public string SocketApiFeed { get; set; }

        public string DoneToken { get; set; }
        public string UserName { get; set; }
        public string AspSessionId { get; set; }
        public int NrDrops { get; set; }
        public bool LogDropRequest { get; set; }
    }
}
