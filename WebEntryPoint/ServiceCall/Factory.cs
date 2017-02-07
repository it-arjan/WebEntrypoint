using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebEntryPoint.MQ;

namespace WebEntryPoint.ServiceCall
{
    static class Factory
    {
        public static WebService Create(ProcessPhase phase )
        {
            return new FakeService("FakeService", 2);
        }
    }
}
