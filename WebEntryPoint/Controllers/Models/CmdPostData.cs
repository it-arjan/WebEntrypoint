using System.Web.Http;

namespace WebEntryPoint
{
    public class CmdPostData
    {
        public string CmdType { get; set; }
        public string Service1Nr { get; set; }
        public string Service2Nr { get; set; }
        public string Service3Nr { get; set; }

        public string SocketAccessToken { get; set; }
        public string SocketQmFeed { get; set; }
        public string SocketApiFeed { get; set; }

        public string AspSessionId { get; set; }
        public string User { get; set; }
        public bool LogDropRequest { get; set; }
    }
}
