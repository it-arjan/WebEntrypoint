using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;

namespace WebEntryPoint
{
    public class TestController: ApiController
    {
        [EnableCors(origins: "http://local.frontend,https://local.frontend,http://ec2-52-57-195-49.eu-central-1.compute.amazonaws.com,https://ec2-52-57-195-49.eu-central-1.compute.amazonaws.com", headers: "*", methods: "*")]
        public IHttpActionResult Get()
        {
            var caller = User as ClaimsPrincipal;
            var x = User;
            var subjectClaim = caller.FindFirst("sub");
            if (subjectClaim != null)
            {
                return Json(new
                {
                    message = "OK user",
                    client = caller.FindFirst("client_id").Value,
                    subject = subjectClaim.Value
                });
            }
            else
            {
                return Json(new
                {
                    message = "OK computer",
                    client = caller.FindFirst("client_id").Value
                });
            }
        }
    }
}
