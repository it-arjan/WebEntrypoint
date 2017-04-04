using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebEntryPoint.Helpers
{
    static class IdSrv3
    { 
        public const string ScopeMcvFrontEndHuman = "mvc-frontend-human";

        public const string ScopeMvcFrontEnd = "mvc-frontend-silicon";
        public const string ScopeEntryQueueApi = "entry-queue-api";
        public const string ScopeNancyApi = "nancy-api";
        public const string ScopeServiceStackApi = "servicestack-api";
        public const string ScopeWcfService = "wcf-service";
        public const string ScopeMsWebApi = "ms-webapi2";

        public const int SessionSetting = 1; // search refs for all session times related settings
    }
}
