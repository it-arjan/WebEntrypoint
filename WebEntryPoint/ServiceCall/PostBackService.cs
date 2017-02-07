using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebEntryPoint.ServiceCall
{
    public class PostBackService : WebService
    {
        public PostBackService(string name, int max) : base(name, max)
        {
        }

        public override Task<DataBag> Call(DataBag data)
        {
            throw new NotImplementedException();
        }
    }
}
