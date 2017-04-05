using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebEntryPoint.Helpers;
using WebEntryPoint.MQ;

namespace WebEntryPoint.ServiceCall
{
    static class Factory
    {
        public static WebService Create(QServiceConfig serviceNr, TokenManager manager )
        {
            var serviceUrl = Appsettings.ServiceX_Url(serviceNr);
            var serviceAuthScope = Appsettings.ServiceX_Scope(serviceNr);

            if (serviceUrl == "fake") return new FakeService(3);
            else return new RealService(serviceUrl, manager, serviceAuthScope);
        }
    }
}
