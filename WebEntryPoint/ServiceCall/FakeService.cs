using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebEntryPoint.MQ;

namespace WebEntryPoint.ServiceCall
{
    class FakeService : WebService
    {
        public int DoneCount { get; set; }
        public FakeService(string name, int max) : base(name,max)
        {

        }

        public async override Task<DataBag>  Call(DataBag data)
        {
            return await SimulateServiceCall(data);
        }

        public async Task<DataBag> SimulateServiceCall(DataBag msgObj)
        {

            Random rnd = new Random();
            var delaySec = 2;
            var failFactor = 3;
            var failRate = Decimal.Round((decimal)failFactor / 10 * 100);

            await Task.Delay(rnd.Next(0, 1000 * delaySec));

            msgObj.Status = GetRandomStatus(failFactor);
            //if (nextStatus != ProcessStatus.ServiceSuccess)
            //{
            //    msgObj.Status = nextStatus;
            //}

            msgObj.AddToContent("{0}: FakeService. Delay: 0-{3} secs, Failrate: {4}%, returned {1} on attempt ({2}) .", msgObj.CurrentPhase, msgObj.Status, msgObj.TryCount, delaySec, failRate);

            return msgObj;
        }

        private ProcessStatus GetRandomStatus(int failFactor)
        {
            Random rnd = new Random();
            var moduloNr = rnd.Next(0, 10) % 10;
            if (moduloNr < failFactor)
            {
                return ProcessStatus.ServiceFailed;
            }
            else
            {
                return ProcessStatus.ServiceSuccess;
            }
        }

    }
}
