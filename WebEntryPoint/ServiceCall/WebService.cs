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
        private object _lockActiveCalls= new object();
        public int MaxRetries { get; protected set; }
        public int ServiceLoad { get; set; }
        public int MaxLoad { get; protected set; }
        public string Url { get; private set; }

        public WebService(string name, string url, int maxLoad=3, int maxRetries =3)
        {
            Url = url;
            Name = name;
            MaxLoad = maxLoad;
            MaxRetries = maxRetries;
        }

        private void ChangeActiveCalls(int nr)
        {
            lock (_lockActiveCalls)
            {
                ServiceLoad += nr;
            }
        }

        protected void IncreaseServiceLoadSafe()
        {
            ChangeActiveCalls(1);
        }
        protected void DecreaseServiceLoadSafe()
        {
            ChangeActiveCalls(-1);
        }

        public bool MaxLoadReached()
        {
            return ServiceLoad > MaxLoad;
        }

        public string Name { get; set; }
        public abstract Task<DataBag> Call(DataBag data);
        public abstract string Description();
    }
}
