using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebEntryPoint.Helpers;
using WebEntryPoint.MQ;

namespace WebEntryPoint.ServiceCall
{
    public class WebserviceFactory : IWebserviceFactory
    {
        public IWebService Create(QServiceConfig serviceNr, ITokenCache tokenManager )
        {
            var serviceType = ConfigSettings.ServiceX_Type(serviceNr).ToLower();
            var serviceName = ConfigSettings.ServiceX_Name(serviceNr).ToLower();
            var serviceUrl = ConfigSettings.ServiceX_Url(serviceNr);
            var serviceAuthScope = ConfigSettings.ServiceX_Scope(serviceNr);
            var serviceMaxload = ConfigSettings.ServiceX_Maxload(serviceNr);

            if (serviceType == "fake") return new FakeService(maxConcRequests: serviceMaxload, maxDelaySecs: 5, failFactor: 3);
            if (serviceType == "postback") return new PostBackService("provided.by.databag", tokenManager, serviceAuthScope);
            if (serviceType == "custom")
            {
                if (serviceName.ToLower() == "pc lookup") return new PcLookupService(serviceName, serviceUrl, serviceAuthScope);
                else throw new Exception(serviceName + ": Unknown Custom service");
            }
            else return new SimpleService(serviceName, serviceUrl, serviceAuthScope, serviceMaxload, tokenManager);
        }
    }
}
