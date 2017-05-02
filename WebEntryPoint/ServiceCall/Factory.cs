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
            var serviceType = Appsettings.ServiceX_Type(serviceNr).ToLower();
            var serviceName = Appsettings.ServiceX_Name(serviceNr).ToLower();
            var serviceUrl = Appsettings.ServiceX_Url(serviceNr);

            var serviceAuthScope = Appsettings.ServiceX_Scope(serviceNr);

            if (serviceType == "fake") return new FakeService(3);
            if (serviceType == "custom")
            {
                if (serviceName.ToLower() == "pc lookup") return new PcLookupService(serviceName, serviceUrl, serviceAuthScope);
                else throw new Exception(serviceName + ": Unknown Custom service");
            }
            else return new SimpleService(serviceName, serviceUrl, serviceAuthScope, manager);
        }
    }
}
