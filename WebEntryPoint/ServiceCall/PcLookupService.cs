using EasyHttp.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLogWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private List<PostalCodeLookup> postalsDone;
        DateTime LastPostalsDoneGroom;
        public PcLookupService(string name, string serviceUrl, string apiKey): base("Postal Code Lookup", serviceUrl, 3)
        {
            ApiKey = apiKey;
            postalsDone = new List<PostalCodeLookup>();
            LastPostalsDoneGroom = DateTime.Now.AddDays(-1);
        }

        public async override Task<DataBag> CallAsync(DataBag data)
        {
            var postalPart = string.Empty;
            var housenrPart = string.Empty;
            var postalLookup = ExtractPostalCode(data.AspSessionId, data.MessageId);

            _logger.Info("Making get request to '{0}'", Url);

            var reponseMsg = string.Empty;
            var ReponseMsg = string.Empty;

            HttpStatusCode resultStatus = HttpStatusCode.PreconditionFailed;

            if (postalLookup != null)
            {
                if (!PostalAlreadyLookedBefore(postalLookup))
                {
                    var eHttp = new EasyHttp.Http.HttpClient();
                    eHttp.Request.AddExtraHeader("X-Api-Key", ApiKey);
                    eHttp.Request.Accept = HttpContentTypes.ApplicationJson;

                    var exceptionMessage = string.Empty;
                    var exception = false;
                    try
                    {
                        var finalUrl = string.Format("{0}/?postcode={1}&number={2}", Url, postalLookup.postal, postalLookup.housenr);
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
                    ReponseMsg = "This postal is already looked up today by this asp session id";
                    resultStatus = HttpStatusCode.OK;
                }
            }
            else
            {
                ReponseMsg = string.Format("{0} is not a postal code + housenr", data.MessageId);
                resultStatus = HttpStatusCode.OK;
            }
            data.AddToLog(ReponseMsg);
            data.Status = resultStatus; 
            return data;
        }

        private string ParseJsonResult(string json)
        {
            string result = null;
            var jsonObj = JObject.Parse(json);
            dynamic dyn = jsonObj["_embedded"]["addresses"][0];
            var ad = new AddressData
            {
                Street = dyn.street,
                Number = dyn.number,
                City = dyn.city.label,
                Surface = dyn.surface,
                Purpose = dyn.purpose,
                Year = dyn.year
            };
            result = string.Format("{0} {1}, {2}. Surface={3}m2. The property was built in {5} and has a '{4}'.",
                                    ad.Street, ad.Number, ad.City, ad.Surface, ad.Purpose, ad.Year);
            return result;
        }

        private bool PostalAlreadyLookedBefore(PostalCodeLookup lookup)
        {
            GroomPostalsDone();
            var result = postalsDone.Where(p => p.date == DateTime.Now.Date
                                                && p.aspSessionId == lookup.aspSessionId
                                                && p.postal == lookup.postal
                                                && p.housenr == lookup.housenr).Any();
            if (!result) postalsDone.Add(lookup);
            return result;
        }

        private void GroomPostalsDone()
        {
            if (LastPostalsDoneGroom < DateTime.Now.Date)
            {
                var oldOnes = postalsDone.Where(p => p.date < DateTime.Now.Date).ToList(); ;
                oldOnes.ForEach(p => postalsDone.Remove(p));
                LastPostalsDoneGroom = DateTime.Now.Date;
            }
        }

        private PostalCodeLookup ExtractPostalCode(string sessionId, string messageId)
        {
            var workItem = messageId.Trim().ToUpper();
            PostalCodeLookup result = null;
            if (Regex.IsMatch(workItem, Helpers.RegEx.isPostalCode))
            {
                result = new PostalCodeLookup();
                result.date = DateTime.Now.Date;
                result.aspSessionId = sessionId;
                var match = Regex.Match(workItem, Helpers.RegEx.PostalGetPostal);
                result.postal= match.Value;
                match = Regex.Match(workItem, Helpers.RegEx.PostalGetHousenr);
                result.housenr = match.Value;
            }
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

    public class PostalCodeLookup
    {
        public DateTime date { get; set; }
        public string aspSessionId { get; set; }
        public string postal { get; set; }
        public string housenr { get; set; }
    }
}