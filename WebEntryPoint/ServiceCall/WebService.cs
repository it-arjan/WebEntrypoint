using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebEntryPoint.MQ;
using System.Threading;
using System.Net;

namespace WebEntryPoint.ServiceCall
{
    public abstract class WebService : IWebService
    {
        private object _safeAccessLock = new object();
        public int MaxRetries { get; protected set; }
        public int ServiceLoad { get; set; }
        public int WaitingQueueLength { get; set; }
        public int MaxLoad { get; protected set; }
        public string Url { get;  set; }
        protected Semaphore _accessSemaphore;

        public WebService(string name, string url, int maxLoad=3, int maxRetries =3)
        {
            Url = url;
            Name = name;
            MaxLoad = maxLoad;
            _accessSemaphore= new Semaphore(maxLoad, maxLoad);
            MaxRetries = maxRetries;
            ServiceLoad = 0;
            WaitingQueueLength = 0;
        }
        private void ChangeLoadSafe(int nr)
        {
            lock (_safeAccessLock)
            {
                ServiceLoad += nr;
            }
        }
        private void ChangeWaitingQueueSafe(int nr)
        {
            lock (_safeAccessLock)
            {
                WaitingQueueLength += nr;
            }
        }
        protected void TryAccess(DataBag dataBag)
        {
            dataBag.AddToLog("-Queueing up for {0}. \nCurrent load = {1}, ({2}) others are in line",this.Name, this.ServiceLoad, this.WaitingQueueLength);

            var startWait = DateTime.Now;
            ChangeWaitingQueueSafe(1);

            _accessSemaphore.WaitOne();

            ChangeWaitingQueueSafe(-1);
            ChangeLoadSafe(1);

            dataBag.AddToLog("-Waited {0} msec", (DateTime.Now - startWait).TotalMilliseconds);
        }
        protected void ReleaseAccess()
        {
            ChangeLoadSafe(-1);
            _accessSemaphore.Release();
        }

        public bool MaxLoadReached()
        {
            return ServiceLoad >= MaxLoad;
        }

        public string Name { get; set; }
        public abstract string Description();

        public virtual Task<DataBag> CallAsync(DataBag data)
        {
            throw new NotImplementedException();
        }

        public virtual HttpStatusCode CallSync(DataBag data)
        {
            throw new NotImplementedException();
        }
    }
}
