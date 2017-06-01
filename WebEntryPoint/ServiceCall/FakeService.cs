using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebEntryPoint.MQ;

namespace WebEntryPoint.ServiceCall
{
    class FakeService : WebService
    {
        public int DoneCount { get; set; }
        private int _maxDelaySecs;
        private int _failFactor;
        private decimal _failRate;

        public FakeService(int maxConcRequests, int maxDelaySecs=2, int failFactor=3) : base("FakeService", "fake-url", maxConcRequests)
        {
            _maxDelaySecs = maxDelaySecs;
            _failFactor = failFactor;
            _failRate = Decimal.Round((decimal)_failFactor / 10 * 100);
        }

        public async override Task<DataBag>  Call(DataBag dataBag)
        {
            var waitingTime = TryAccess();
            dataBag.AddToLog("-Waited {0} msec, current service load = {1}", waitingTime.TotalMilliseconds, this.ServiceLoad);
            var result = await SimulateServiceCall(new ServiceCallDataBag {input=dataBag.MessageId });
            ReleaseAccess();

            dataBag.Status = result.status;
            dataBag.AddToLog("{0}: {3} returned {1} on attempt ({2}). max service load={4}", dataBag.CurrentPhase, dataBag.Status, dataBag.TryCount, this.Name, MaxLoad);
            return dataBag;
        }

        public async Task<ServiceCallDataBag> SimulateServiceCall(ServiceCallDataBag sDataBag)
        {
            Random rnd = new Random();
            await Task.Delay(rnd.Next(0, 1000 * _maxDelaySecs));
            sDataBag.status = GetRandomHttpStatus(_failFactor);
            return sDataBag;
        }

        private HttpStatusCode GetRandomHttpStatus(int failFactor)
        {
            Random rnd = new Random();
            var moduloNr = rnd.Next(0, 10) % 10;

            if (moduloNr < failFactor)
            {
                return HttpStatusCode.InternalServerError;
            }
            else if (moduloNr == failFactor)
            {
                return HttpStatusCode.NotFound;
            }
            else
            {
                return HttpStatusCode.OK;
            }
        }

        public override string Description()
        {
            return string.Format("programmed delay: 0-{0} secs, fail rate: {1}%", _maxDelaySecs, _failRate);
        }
    }
}
