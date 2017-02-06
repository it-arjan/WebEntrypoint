using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NLogWrapper;
using System.Threading;
using System.Security.Claims;

namespace WebEntryPoint
{

    public class LogRequestMessageHandler : DelegatingHandler
    {
        private static ILogger _logger = LogManager.CreateLogger(typeof(LogRequestMessageHandler));
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var cp = request.GetRequestContext().Principal as ClaimsPrincipal;
                var userName = "Anonymous";
                if (cp != null)
                {
                    userName = cp.FindFirst("sub") != null ? cp.FindFirst("sub").Value : "Authenticated user, sub claim not set";
                }
                var referrer = request.GetOwinContext().Request.RemoteIpAddress;
                referrer = referrer ?? "unknown";
                _logger.Debug("incoming request for {0} by user {1}. Referrer: {2}", request.RequestUri, userName, referrer);
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting the principal");
            }
            return base.SendAsync(request, cancellationToken);
        }

        public string GetClientIpAddress(HttpRequestMessage request)
        {
            string HttpContext = "MS_HttpContext";
            if (request.Properties.ContainsKey(HttpContext))
            {
                dynamic ctx = request.Properties[HttpContext];
                if (ctx != null)
                {
                    return ctx.Request.UserHostAddress;
                }
            }
            return null;
        }
    }

}
