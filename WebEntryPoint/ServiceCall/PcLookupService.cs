using EasyHttp.Http;
using Newtonsoft.Json;
using NLogWrapper;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebEntryPoint.ServiceCall
{
    internal class PcLookupService : WebService
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(PcLookupService), Helpers.ConfigSettings.LogLevel());
        public string ApiKey { get; private set; }

        public PcLookupService(string name, string serviceUrl, string apiKey): base("Postal Code Lookup", serviceUrl, 3)
        {
            ApiKey = apiKey;
        }

        public async override Task<DataBag> CallAsync(DataBag data)
        {
            _logger.Info("Making get request to '{0}'", Url);
            throw new Exception("TODO");
            var eHttp = new EasyHttp.Http.HttpClient();
            var auth_header = string.Format("Bearer {0}", ApiKey);

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

            await Task.Delay(1); // to make it async lol
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
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiKey);
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