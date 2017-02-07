using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebEntryPoint.MQ;

namespace WebEntryPoint.ServiceCall
{
    public abstract class WebService
    {
        public WebService(string name, int max)
        {
            Name = name;
            maxConcurrrentRequests = max;
        }
        protected MSMQWrapper _myQueue;
        protected MSMQWrapper _nextQueue;
        public int activeCalls { get; set; }
        private int maxConcurrrentRequests;

        public bool MaxLoadReached()
        {
            return activeCalls < maxConcurrrentRequests;
        }
        public string Name { get; set; }
        public abstract Task<DataBag> Call(DataBag data);
    }
}
