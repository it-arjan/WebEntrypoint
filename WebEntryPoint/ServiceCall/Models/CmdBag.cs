using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebEntryPoint.Helpers;

namespace WebEntryPoint.ServiceCall
{

    public enum CmdType
    {
        NotSet = 0, GetModus, ToggleModus, GetServiceConfig, SetServiceConfig
    }
    public enum CmdStatus
    {
        Ok, Error
    }

    public class CmdBag
    {
        public CmdBag()
        {
            Status = CmdStatus.Ok;
        }

        public CmdBag(CmdBag clone)
        {
            CmdType = clone.CmdType;
            Status = clone.Status;
            Service1Nr = clone.Service1Nr;
            Service2Nr = clone.Service3Nr;
            Service3Nr = clone.Service3Nr;
            CmdResult = clone.CmdResult;
            Message = clone.Message;
            SocketToken = clone.SocketToken;
        }

        public CmdType CmdType { get; set; }
        public CmdStatus Status { get; set; }

        public QServiceConfig Service1Nr { get; set; }
        public QServiceConfig Service2Nr { get; set; }
        public QServiceConfig Service3Nr { get; set; }

        public string CmdResult { get; set; }
        public string Message { get; set; }

        public string SocketToken { get; set; }
        
    }

}
