using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLogWrapper;
using System.Net;
using Newtonsoft.Json;
using EasyHttp.Http;
using WebEntryPoint.Helpers;

namespace WebEntryPoint.ServiceCall
{
    public class PostBackService : WebService
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(PostBackService), Helpers.ConfigSettings.LogLevel());
        private ITokenCache _tokenCache;
        public string AuthScope { get; private set; }

        public PostBackService(string postbackUrlNotUsed, ITokenCache tokenManager, string scope) : base("PostBackService", postbackUrlNotUsed, 5)
        {
            _tokenCache = tokenManager;
            AuthScope = scope;
        }

        public override HttpStatusCode CallSync(DataBag data)
        {
            TryAccess(data);
            RemoteRequestLogger.Log(data.UserName, data.AspSessionId, data.ApiFeedToken, "todo", "application/json", "POST", "/Postbackdatas");
            var status = PostBackUsingEasyHttp(_tokenCache.GetToken(AuthScope), data.PostBackUrl, data.ApiFeedToken, new PostbackData(data));
            ReleaseAccess();
            if (status == HttpStatusCode.Unauthorized)
            {
                // unlikely, but theoretically possible
                _logger.Info("Postback Unauthorized, trying again once with a fresh token..");
                TryAccess(data);
                status = PostBackUsingEasyHttp(_tokenCache.GetToken(AuthScope), data.PostBackUrl, data.ApiFeedToken,new PostbackData(data));
                ReleaseAccess();
            }
            return status;
        }

        private HttpStatusCode PostBackUsingEasyHttp(string token, string postbackUrl, string apiFeedToken, PostbackData data)
        {
            HttpStatusCode result = HttpStatusCode.PreconditionFailed;
            try
            {
                _logger.Info("Postback url='{0}'", postbackUrl);
                _logger.Debug("post back values: {0}", JsonConvert.SerializeObject(data));
                var eHttp = new EasyHttp.Http.HttpClient();
                var auth_header = string.Format("Bearer {0}", token);

                eHttp.Request.AddExtraHeader("Authorization", auth_header);
                eHttp.Request.AddExtraHeader("X-socketToken", apiFeedToken);
                result = eHttp.Post(postbackUrl, data, HttpContentTypes.ApplicationJson).StatusCode;
                _logger.Info("Postback returned '{0}': (1)", result);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception! {0}", ex.Message);
            }
            return result;
        }

        public override string Description()
        {
            return string.Format("NO programmed delay or fails.");
        }
    }
}
