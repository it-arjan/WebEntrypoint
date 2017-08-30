using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebEntryPoint.ServiceCall
{
    public enum ProcessStatus
    {
        ReadyFor = 0, ServiceFailed, ServiceSuccess
    }

    public enum ProcessPhase
    {
        Entry = 0, Service1, Service2, Service3, Completed
    }

    public class DataBag
    {
        public DataBag()
        {

            TryCount = 0;
            Status = HttpStatusCode.OK;
            CurrentPhase = ProcessPhase.Service1;
            LogRequests = true;
        }

        public DataBag(EntryQueuePostData transfer)
        {
            Label = transfer.MessageId + " - " + DateTime.Now.ToShortTimeString();
            MessageId = transfer.MessageId;
            PostBackUrl = transfer.PostBackUrl;
            SocketAccessToken = transfer.SocketAccessToken;
            SocketQmFeed = transfer.SocketQmFeed;
            SocketApiFeed = transfer.SocketApiFeed;
            SocketNotificationFeed = transfer.SocketNotificationFeed;
            doneToken = transfer.DoneToken;
            AspSessionId = transfer.AspSessionId;
            UserName = transfer.UserName;
            LogRequests = transfer.LogDropRequest;
            TryCount = 0;
            Status = HttpStatusCode.OK;
            CurrentPhase = ProcessPhase.Service1;
            Started = DateTime.Now;
        }

        public DateTime Started { get; set; }
        public string Duration { get; set; }
        public string Content { get; set; }
        public string Label { get; set; }
        public string MessageId { get; set; }
        public int TryCount { get; set; }

        public string SocketNotificationFeed { get; set; }
        public string SocketAccessToken { get; set; }
        public string SocketQmFeed { get; set; }
        public string SocketApiFeed { get; set; }
        public string doneToken { get; set; }
        public string AspSessionId{ get; set; }
        
        public string PostBackUrl { get; set; }
        public string UserName { get; set; }
        public bool LogRequests { get; set; }

        public string SiliconToken { get; set; }

        public HttpStatusCode Status { get; set; }

        public ProcessPhase CurrentPhase { get; set; }

        public bool Retry {
            get { return !Status.Equals(HttpStatusCode.OK) 
                    && !Status.Equals(HttpStatusCode.ServiceUnavailable)
                    && !Status.Equals(HttpStatusCode.NotFound);
            }
        }

        public void AddToLog(string msg, params object[] args)
        {
            Content += "\n";
            if (msg != null) Content += string.Format(msg, args);
            else Content += "AddToContent: attempting to add a NULL msg ...";
        }
        public void AddSeparator()
        {
            AddToLog("-----");
        }
        public void AddShortSeparator()
        {
            AddToLog("--");
        }
        public void AddStatusMsg()
        {
            AddToLog("Current http status is {0}", Status);
        }
    }

}
