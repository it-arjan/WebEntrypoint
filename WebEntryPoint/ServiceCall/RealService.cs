using EasyHttp.Http;
using Newtonsoft.Json;
using NLogWrapper;
using System;
using System.Threading.Tasks;

namespace WebEntryPoint.ServiceCall
{
    internal class RealService : WebService
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(RealService), Helpers.Appsettings.LogLevel());
        private TokenManager _tokenManager;
        public string AuthScope { get; private set; }

        public RealService(string serviceUrl, TokenManager tokenManager, string scope): base("Real Service", serviceUrl, 3)
        {
            _tokenManager = tokenManager;
            AuthScope = scope;
        }

        public async override Task<DataBag> Call(DataBag data)
        {
            var token = _tokenManager.GetToken(AuthScope);

            _logger.Info("Making get request to '{0}'", Url);
            var eHttp = new EasyHttp.Http.HttpClient();
            var auth_header = string.Format("Bearer {0}", token);

            eHttp.Request.AddExtraHeader("Authorization", auth_header);
            eHttp.Request.Accept= HttpContentTypes.ApplicationJson;
            var exceptionMessage = string.Empty;
            var exception = false;
            try
            {
                eHttp.Get(Url + data.MessageId);
            }
            catch (System.Net.WebException ex)
            {
                exception = true;
                exceptionMessage = ex.Message;
            }

            var resultStatus = exception ? System.Net.HttpStatusCode.ServiceUnavailable : eHttp.Response.StatusCode;
            var statusmsg = string.Format("Log msg: {0} returned {1} {2}", Url, resultStatus, exceptionMessage);
            _logger.Info(statusmsg);

            await Task.Delay(1); //change to calling service async
            var reponseMsg = string.Empty;
            var ReponseMsg = string.Empty;
            if (!exception && eHttp.Response.ContentType.Contains(HttpContentTypes.ApplicationJson))
            {
                reponseMsg = ParseResult(eHttp.Response.RawText);
            }
            else ReponseMsg = exception ? exceptionMessage : reponseMsg;

            if (string.IsNullOrEmpty(ReponseMsg)) ReponseMsg = statusmsg;

            data.AddToContent(ReponseMsg);
            data.Status = resultStatus;
            return data;
        }

        private string ParseResult(string json)
        {
            var anoType = new { Message = "" };
            string result = null;
            try
            {
                var x = JsonConvert.DeserializeAnonymousType(json, anoType);
                result = x.Message;
            }
            catch (Exception ex)
            {
                result = string.Format("Content-type says it's JSON but QM is unable to serialize webservice response. {0}", ex.Message);
            }
            return result;
        }
        public override string Description()
        {
            return String.Format("Service runs at {0}, \n\t\tmax load = {1}, max retries ={2}", Url, MaxLoad, MaxRetries);
        }
    }
}