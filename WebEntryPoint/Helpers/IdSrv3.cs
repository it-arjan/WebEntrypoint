using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebEntryPoint.Helpers
{
    static class IdSrv3
    {
        public const string ScopeMvcFrontEnd = "MvcFrontEnd";
        public const string ScopeEntryQueueApi = "EntryQueueApi";
        public const string ScopeNancyApi = "NancyApi";
        public const string ScopeServiceStackApi = "ServiceStackApi";
        public const string ScopeWcfService = "WcfService";

        public const int SessionSetting = 1; // search refs for all session times related settings
    }
}
