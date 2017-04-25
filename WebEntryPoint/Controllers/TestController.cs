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
        public IHttpActionResult Get()
        {
            var caller = User as ClaimsPrincipal;
            if (caller == null)
            {
                return Json(new
                {
                    message = "Okay Anonymous.."
                });
            }
            else
            {
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
}
