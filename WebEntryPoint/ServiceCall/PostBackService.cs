using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLogWrapper;
using System.Net;
using Newtonsoft.Json;
using EasyHttp.Http;

namespace WebEntryPoint.ServiceCall
{
    public class PostBackService : WebService
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(PostBackService), Helpers.Appsettings.LogLevel());
        private TokenManager _tokenManager;
        public string AuthScope { get; private set; }

        public PostBackService(string postbackUrl, TokenManager tokenManager, string scope) : base("PostBackService", postbackUrl, 1)
        {
            _tokenManager = tokenManager;
            AuthScope = scope;
        }

        public HttpStatusCode CallSync(DataBag data)
        {
            var status = PostBackUsingEasyHttp(_tokenManager.GetToken(AuthScope), data.PostBackUrl, new PostbackData(data));
            if (status == HttpStatusCode.Unauthorized)
            {
                // unlikely, but theoretically possible
                _logger.Info("Unauthorized, try again once with a fresh token..");
                status = PostBackUsingEasyHttp(_tokenManager.GetToken(AuthScope), data.PostBackUrl, new PostbackData(data));
            }
            return status;
        }

        private HttpStatusCode PostBackUsingEasyHttp(string token, string postbackUrl, PostbackData data)
        {
            _logger.Info("Postback url='{0}'", postbackUrl);
            _logger.Debug("post back values: {0}", JsonConvert.SerializeObject(data));
            var eHttp = new EasyHttp.Http.HttpClient();
            var auth_header = string.Format("Bearer {0}", token);

            eHttp.Request.AddExtraHeader("Authorization", auth_header);
            var result = eHttp.Post(postbackUrl, data, HttpContentTypes.ApplicationJson).StatusCode;
            _logger.Info("Postback returned '{0}': (1)", result);

            return result;
        }

        public override string Description()
        {
            return string.Format("NO programmed delay or fails.");
        }

        public override Task<DataBag> Call(DataBag data)
        {
            throw new NotImplementedException();
        }
    }
}
