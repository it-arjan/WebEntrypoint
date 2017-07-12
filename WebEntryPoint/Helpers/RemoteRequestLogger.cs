using EasyHttp.Http;
using EasyHttp.Infrastructure;
using Newtonsoft.Json;
using NLogWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebEntryPoint.Helpers
{
    public static class RemoteRequestLogger
    {
        private static ILogger _logger = LogManager.CreateLogger(typeof(RemoteRequestLogger), Helpers.ConfigSettings.LogLevel());
        public static void Log(string userName, string aspSessionId, string apiFeedToken, string fromIp, string contentype, string method, string path)
        {
            var logEntry = CreateApiLogEntryWithRequestData(userName, aspSessionId, fromIp, contentype, method, path);
            string url = string.Format("{0}/requestlog", Helpers.ConfigSettings.DataApiUrl());
            Post(url, apiFeedToken, JsonConvert.SerializeObject(logEntry));

        }
        private static Models.RequestLogEntry CreateApiLogEntryWithRequestData(string userName, string aspSessionId, string fromIp, string contentype, string method, string path)
        {
            return new Models.RequestLogEntry
            {
                User = userName,
                ContentType = contentype,
                Ip = fromIp,
                Method = method,
                Timestamp = DateTime.Now,
                Path = path,
                AspSessionId = aspSessionId
            };
        }
        private static void Post(string url, string apiFeedToken, string json)
        {
            try
            {
                var eHttp = new EasyHttp.Http.HttpClient();
                // for now: get new token every request
                // todo try to cache it in application["tokencache"] 
                var token = new ServiceCall.TokenCache().GetToken(Helpers.IdSrv3.ScopeFrontendDataApi);
                eHttp.Request.AddExtraHeader("Authorization", string.Format("bearer {0}", token));
                eHttp.Request.AddExtraHeader("X-socketToken", apiFeedToken);

                eHttp.Post(url, json, HttpContentTypes.ApplicationJson);
                if (eHttp.Response.StatusCode != System.Net.HttpStatusCode.OK)
                    throw new HttpException(eHttp.Response.StatusCode, eHttp.Response.StatusDescription);
            }
            catch (Exception ex){
                _logger.Error("logging request to {0} failed: {1}", url, ex.Message);
            }
        }
    }
}
