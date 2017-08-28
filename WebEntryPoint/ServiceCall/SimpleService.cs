using EasyHttp.Http;
using Newtonsoft.Json;
using NLogWrapper;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebEntryPoint.ServiceCall
{
    internal class SimpleService : WebService
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(SimpleService), Helpers.ConfigSettings.LogLevel());
        private ITokenCache _tokenManager;
        private static System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient(); //share httpClient to reduce overhead
        public string MyScope { get; private set; }

        public SimpleService(string name, string url, string scope, int maxload, ITokenCache tokenManager): 
            base(name, url, maxload)
        {
            _tokenManager = tokenManager;
            MyScope = scope;
        }

        public async override Task<DataBag> CallAsync(DataBag dataBag)
        {
            TryAccess(dataBag);

            var ReponseMsg = string.Empty;
            try
            {
                var token = _tokenManager.GetToken(MyScope);
                //await Task.Delay(1); // quick hack to make function compile async
                //dataBag = GetResultSync(dataBag, Url + dataBag.MessageId, token);
                await GetResultASync(dataBag, Url + dataBag.MessageId, token);
            }
            catch (System.Net.WebException ex)
            {
                dataBag.AddToLog( ex.Message);
            }
            finally
            {
                ReleaseAccess();
            }
            return dataBag;
        }

        private DataBag GetResultSync(DataBag dataBag, string methodUrl, string token)
        {
            var result = string.Empty;
            _logger.Info("Making get request to '{0}'", Url);
            var eHttp = new EasyHttp.Http.HttpClient();
            var auth_header = string.Format("Bearer {0}", token);

            eHttp.Request.AddExtraHeader("Authorization", auth_header);
            eHttp.Request.Accept = HttpContentTypes.ApplicationJson;
            eHttp.Get(methodUrl);

            var statusmsg = string.Format("Log msg: {0} returned {1}", Url, eHttp.Response.StatusCode);
            _logger.Info(statusmsg);

            if (eHttp.Response.ContentType.Contains(HttpContentTypes.ApplicationJson))
            {
                result = ParseJsonResult(eHttp.Response.RawText);
            }
            else result = statusmsg;

            dataBag.Status = eHttp.Response.StatusCode;
            dataBag.AddToLog(result);

            return dataBag;
        }

        private async Task<DataBag> GetResultASync(DataBag dataBag, string methodUrl, string token)
        {
            HttpResponseMessage httpResponseMsg = null;
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(HttpContentTypes.ApplicationJson));
            var ReponseMsg = string.Empty;

            var resultStatus = System.Net.HttpStatusCode.Ambiguous;
            try
            {
                httpResponseMsg = await _httpClient.GetAsync(methodUrl);

                resultStatus = httpResponseMsg.StatusCode;
                var logMsg = string.Format("Log msg: {0} returned {1}", methodUrl, resultStatus);
                _logger.Info(logMsg);

                 ReponseMsg = ParseJsonResult(await httpResponseMsg.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                ReponseMsg = ex.Message;
            }

            dataBag.AddToLog(ReponseMsg);
            dataBag.Status = resultStatus;
            return dataBag;
        }

        private ByteArrayContent SerializeDataBag(DataBag msgObj)
        {
            var myContent = JsonConvert.SerializeObject(msgObj);
            var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
            return new ByteArrayContent(buffer);
        }

        private string ParseJsonResult(string json)
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