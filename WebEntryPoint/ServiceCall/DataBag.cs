using System;
using System.Collections.Generic;
using System.Linq;
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
            Status = ProcessStatus.ReadyFor;
            CurrentPhase = ProcessPhase.Service1;
        }

        public string StartTime { get; set; }
        public string Duration { get; set; }
        public string Content { get; set; }
        public string Label { get; set; }
        public string Id { get; set; }
        public int TryCount { get; set; }
        public string socketToken { get; set; }
        public string PostBackUrl { get; set; }

        public string IdToken { get; set; }
        public ProcessStatus Status { get; set; }
        public ProcessPhase CurrentPhase { get; set; }

        public bool Error { get { return Status.Equals(ProcessStatus.ServiceFailed); } }

        public void AddToContent(string msg, params object[] args)
        {
            Content += "\n";
            Content += string.Format(msg, args);
        }

        public void NextService()
        {
            switch (CurrentPhase)
            {
                case ProcessPhase.Entry:
                    CurrentPhase = ProcessPhase.Service1;
                    Status = ProcessStatus.ReadyFor;
                    break;
                case ProcessPhase.Service1:
                    CurrentPhase = ProcessPhase.Service2;
                    Status = ProcessStatus.ReadyFor;
                    break;
                case ProcessPhase.Service2:
                    CurrentPhase = ProcessPhase.Service3;
                    Status = ProcessStatus.ReadyFor;
                    break;
                case ProcessPhase.Service3:
                    CurrentPhase = ProcessPhase.Completed;
                    Status = ProcessStatus.ServiceSuccess;
                    break;
                default:
                    throw new Exception("Impossible Current phase!");
            };
            TryCount = 0;
        }
    }

}
