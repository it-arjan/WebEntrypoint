using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebEntryPoint.ServiceCall
{
    public class ServiceCallDataBag
    {
        public string input { get; set; }
        public string output { get; set; }
        public HttpStatusCode status { get; set; }
    }
}
