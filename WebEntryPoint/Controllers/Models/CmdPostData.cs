﻿using System.Web.Http;

namespace WebEntryPoint
{
    public class CmdPostData
    {
        public string CmdType { get; set; }
        public string Service1Nr { get; set; }
        public string Service2Nr { get; set; }
        public string Service3Nr { get; set; }
        public string SocketToken { get; set; }
        public string AspSessionId { get; set; }
        public string ApiFeedToken { get; set; }
        public string User { get; set; }
    }
}
