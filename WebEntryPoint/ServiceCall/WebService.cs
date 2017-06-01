using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebEntryPoint.MQ;
using System.Threading;

namespace WebEntryPoint.ServiceCall
{
    public abstract class WebService
    {
        private object _lockActiveCalls = new object();
        public int MaxRetries { get; protected set; }
        public int ServiceLoad { get; set; }
        public int MaxLoad { get; protected set; }
        public string Url { get; private set; }
        protected Semaphore _accessSemaphore;

        public WebService(string name, string url, int maxLoad=3, int maxRetries =3)
        {
            Url = url;
            Name = name;
            MaxLoad = maxLoad;
            _accessSemaphore= new Semaphore(maxLoad, maxLoad);
            MaxRetries = maxRetries;
            ServiceLoad = 0;
        }
        private void ChangeLoadSafe(int nr)
        {
            lock (_lockActiveCalls)
            {
                ServiceLoad += nr;
            }
        }
        protected TimeSpan TryAccess()
        {
            var startWait = DateTime.Now;
            _accessSemaphore.WaitOne();
            ChangeLoadSafe(1);
            return DateTime.Now - startWait;
        }
        protected void ReleaseAccess()
        {
            ChangeLoadSafe(-1);
            _accessSemaphore.Release();
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
