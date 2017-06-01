using EasyHttp.Http;
using Newtonsoft.Json;
using NLogWrapper;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebEntryPoint.ServiceCall
{
    internal class SimpleService : WebService
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(SimpleService), Helpers.Appsettings.LogLevel());
        private TokenManager _tokenManager;
        public string MyScope { get; private set; }

        public SimpleService(string name, string url, string scope, TokenManager tokenManager): base(name, url, 3)
        {
            _tokenManager = tokenManager;
            MyScope = scope;
        }

        public async override Task<DataBag> Call(DataBag data)
        {
            var waitingTime = TryAccess();
            data.AddToLog("-Waited {0} msec, current service load = {1}", waitingTime.TotalMilliseconds, this.ServiceLoad); var token = _tokenManager.GetToken(MyScope);

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
 
                var resultStatus = exception ? System.Net.HttpStatusCode.ServiceUnavailable : eHttp.Response.StatusCode;
                var statusmsg = string.Format("Log msg: {0} returned {1} {2}", Url, resultStatus, exceptionMessage);
                _logger.Info(statusmsg);

                await Task.Delay(1); // quick hack to make function async

                var reponseMsg = string.Empty;
                var ReponseMsg = string.Empty;
                if (exception) ReponseMsg = exceptionMessage;
                else if (eHttp.Response.ContentType.Contains(HttpContentTypes.ApplicationJson))
                {
                    ReponseMsg = ParseResult(eHttp.Response.RawText);
                }
                else ReponseMsg = statusmsg;

                data.AddToLog(ReponseMsg);
                data.Status = resultStatus;
            }
            catch (System.Net.WebException ex)
            {
                exception = true;
                exceptionMessage = ex.Message;
            }
            finally
            {
                ReleaseAccess();
            }
            return data;
        }

        private async Task<DataBag> CallUsingHttpClient(DataBag data)
        {
            // HttpClient shows status 404 on the crash URL
            var exceptionMessage = string.Empty;
            var exception = false;
            using (var client = new System.Net.Http.HttpClient())
            {
                HttpResponseMessage response = null;
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenManager.GetToken(MyScope));
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(HttpContentTypes.ApplicationJson));
                try
                {
                    response = await client.GetAsync(Url);
                }
                catch (System.Net.WebException ex)
                {
                    exception = true;
                    exceptionMessage = ex.Message;
                }

                var resultStatus = exception ? System.Net.HttpStatusCode.ServiceUnavailable : response.StatusCode;
                var statusmsg = string.Format("Log msg: {0} returned {1} {2}", Url, resultStatus, exceptionMessage);
                _logger.Info(statusmsg);

                await Task.Delay(1); //change to calling service async
                var reponseMsg = string.Empty;
                var ReponseMsg = string.Empty;
                if (exception) ReponseMsg = exceptionMessage;
                else if (response.Headers.Contains("Accept"))
                {
                    ReponseMsg = ParseResult(await response.Content.ReadAsAsync<string>());
                }
                else ReponseMsg = statusmsg;
                data.AddToLog(ReponseMsg);
                data.Status = resultStatus;
            }
            return data;
        }
    

        private ByteArrayContent SerializeDataBag(DataBag msgObj)
        {
            var myContent = JsonConvert.SerializeObject(msgObj);
            var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
            return new ByteArrayContent(buffer);
        }

        private string ParseResult(string json)
        {
            var anoType = new { Message = "" };
            string result = null;
            try
            {
                var x = JsonConvert.DeserializeAnonymousType(json, anoType);
                result = string.IsNullOrEmpty(x.Message)? "Unexpected: the webservice response is empty!" : x.Message;
            }
            catch (Exception ex)
            {
                result = string.Format("Content-type says it's JSON but we are unable to serialize webservice response. {0}", ex.Message);
            }
            return result;
        }
        public override string Description()
        {
            return String.Format("Url={0}, \n\t\tmax load = {1}, max retries ={2}", Url, MaxLoad, MaxRetries);
        }
    }
}