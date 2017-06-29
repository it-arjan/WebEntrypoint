using EasyHttp.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLogWrapper;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebEntryPoint.ServiceCall
{
    internal class PcLookupService : WebService
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(PcLookupService), Helpers.ConfigSettings.LogLevel());
        public string ApiKey { get; private set; }
        private List<Tuple<string, string>> postalsLookedUp;
        public PcLookupService(string name, string serviceUrl, string apiKey): base("Postal Code Lookup", serviceUrl, 3)
        {
            ApiKey = apiKey;
            postalsLookedUp = new List<Tuple<string, string>>();
        }

        public async override Task<DataBag> CallAsync(DataBag data)
        {
            var postalPart = string.Empty;
            var housenrPart = string.Empty;
            var isPostalCode = ExtractPostalCode(data.MessageId, out postalPart, out housenrPart);

            _logger.Info("Making get request to '{0}'", Url);

            var reponseMsg = string.Empty;
            var ReponseMsg = string.Empty;

            HttpStatusCode resultStatus = HttpStatusCode.PreconditionFailed;

            if (isPostalCode)
            {
                if (!PostalAlreadyLookedBefore(data.AspSessionId, postalPart, housenrPart))
                {
                    var eHttp = new EasyHttp.Http.HttpClient();
                    eHttp.Request.AddExtraHeader("X-Api-Key", ApiKey);
                    eHttp.Request.Accept = HttpContentTypes.ApplicationJson;

                    var exceptionMessage = string.Empty;
                    var exception = false;
                    try
                    {
                        var finalUrl = string.Format("{0}/?postcode={1}&number={2}", Url, postalPart, housenrPart);
                        eHttp.Get(finalUrl);
                        ReponseMsg = ParseJsonResult(eHttp.Response.RawText);
                    }
                    catch (System.Net.WebException ex)
                    {
                        exception = true;
                        ReponseMsg = ex.Message;
                    }

                    resultStatus = exception ? HttpStatusCode.ServiceUnavailable : eHttp.Response.StatusCode;
                    var statusmsg = string.Format("Log msg: {0} returned {1} {2}", Url, resultStatus, exceptionMessage);
                    _logger.Info(statusmsg);

                    await Task.Delay(1); // to make it async lol
                }
                else //postal already looked up
                {
                    ReponseMsg = "This postal is already looked up recently by this asp session id";
                }
            }
            else
            {
                ReponseMsg = string.Format("{0} is not a postal code + housenr", data.MessageId);
            }
            data.AddToLog(ReponseMsg);
            data.Status = resultStatus; 
            return data;
        }

        private string ParseJsonResult(string json)
        {
            string result = null;
            dynamic jsonObj = JObject.Parse(json);
            var address = jsonObj["_embedded"]["addresses"][0];
            var ad = new AddressData
            {
                Street = address.street,
                Number = address.number,
                City = address.city.label,
                Surface = address.surface,
                Purpose = address.purpose,
                Year = address.year
            };
            result = string.Format("{0} {1}, {2}. Surface={3}m2. The property was built in {5} and has a '{4}'.",
                                    ad.Street, ad.Number, ad.City, ad.Surface, ad.Purpose, ad.Year);
            return result;
        }

        private bool PostalAlreadyLookedBefore(string aspSessionId, string postalPart, string housenrPart)
        {
            return false;
            var check = new Tuple<string, string>(item1: postalPart, item2: housenrPart);

            return postalsLookedUp.Contains(check);
        }


        private bool ExtractPostalCode(string messageId, out string postal, out string housenr)
        {
            var workItem = messageId.Trim().ToUpper();
            bool result = Regex.IsMatch(workItem, Helpers.RegEx.isPostalCode);
            var match = Regex.Match(workItem, Helpers.RegEx.PostalGetPostal);
            postal = match.Value;
            match = Regex.Match(workItem, Helpers.RegEx.PostalGetHousenr);
            housenr = match.Value;
            return result;
            
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
                    //ReponseMsg = ParseJsonResult(await response.Content.ReadAsAsync<string>());
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

        public override string Description()
        {
            return String.Format("Url={0}, \n\t\tmax load = {1}, max retries ={2}", Url, MaxLoad, MaxRetries);
        }
    }

   public class AddressData
    {
        public string Street { get; set; }
        public string Number{ get; set; }
        public string City { get; set; }
        public string Purpose { get; set; }
        public string Surface { get; set; }
        public string Year { get; set; }
    }

    public class PostalCodeApiCity
    {
        public string Id { get; set; }
        public string Label { get; set; }
    }
}