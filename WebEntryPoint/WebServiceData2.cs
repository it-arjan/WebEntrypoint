using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebEntryPoint.MQ
{
    public enum MsgStatus2
    {
        ReadyFor = 0, ServiceTempDown, ServiceFailed, ServiceCompleted
    }

    public enum ProcessPhase2
    {
        Entry = 0, Service1, Service2, Service3, Completed
    }

    public class DataBag
    {
        public DataBag()
        {

            TryCount = 0;
            Status = MsgStatus2.ReadyFor;
            CurrentPhase = ProcessPhase2.Service1;
        }

        public string StartTime { get; set; }
        public string Duration { get; set; }
        public string Content { get; set; }
        public string Label { get; set; }
        public string Id { get; set; }
        public int TryCount { get; set; }
        public string socketToken { get; set; }
        public string PostBackUrl { get; set; }

        public MsgStatus2 Status { get; set; }
        public ProcessPhase2 CurrentPhase { get; set; }

        public bool Error { get { return Status.Equals(MsgStatus2.ServiceFailed) || Status.Equals(MsgStatus2.ServiceTempDown); } }

        public void AddToContent(string msg, params object[] args)
        {
            Content += "\n";
            Content += string.Format(msg, args);
        }

        public void NextService()
        {
            switch (CurrentPhase)
            {
                case ProcessPhase2.Entry:
                    CurrentPhase = ProcessPhase2.Service1;
                    Status = MsgStatus2.ReadyFor;
                    break;
                case ProcessPhase2.Service1:
                    CurrentPhase = ProcessPhase2.Service2;
                    Status = MsgStatus2.ReadyFor;
                    break;
                case ProcessPhase2.Service2:
                    CurrentPhase = ProcessPhase2.Service3;
                    Status = MsgStatus2.ReadyFor;
                    break;
                case ProcessPhase2.Service3:
                    CurrentPhase = ProcessPhase2.Completed;
                    Status = MsgStatus2.ServiceCompleted;
                    break;
                default:
                    throw new Exception("Impossible Current phase!");
            };
            TryCount = 0;
        }
    }

}
