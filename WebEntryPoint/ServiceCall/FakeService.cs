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
            await Task.Delay(rnd.Next(0, 2000));

            ProcessStatus nextStatus = GetRandomStatus();

            if (nextStatus != ProcessStatus.ServiceSuccess)
            {
                msgObj.Status = nextStatus;
            }
 

            msgObj.AddToContent("FakeService: call to {0} returned {1} on attempt ({2}) .", msgObj.CurrentPhase, msgObj.Status, msgObj.TryCount);
            if (msgObj.CurrentPhase != ProcessPhase.Completed) msgObj.AddToContent("Ready for {0}.", msgObj.CurrentPhase);

            return msgObj;
        }

        private ProcessStatus GetRandomStatus()
        {
            Random rnd = new Random();
            var moduloNr = rnd.Next(0, 20) % 10;
            if (moduloNr < 3)
            {
                return ProcessStatus.ServiceFailed;
            }
            else
            {
                return ProcessStatus.ReadyFor;
            }
        }

    }
}
