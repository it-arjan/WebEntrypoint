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
        private static System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient(); //share httpClient to reduce overhead

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
                if (!PostalLookedUpBefore(postalLookup))
                {
                   var exceptionMessage = string.Empty;
                    var finalUrl = string.Format("{0}/?postcode={1}&number={2}", Url, postalLookup.postal, postalLookup.housenr);
                    data = await GetResultASync(data, finalUrl, "token_not_used");
                }
                else //postal already looked up
                {
                    ReponseMsg = "This postal is already looked up today by this asp session id";
                    resultStatus = HttpStatusCode.OK;
                    data.AddToLog(ReponseMsg);
                    data.Status = resultStatus;
                }
            }
            else
            {
                ReponseMsg = string.Format("{0} is not a postal code + housenr", data.MessageId);
                resultStatus = HttpStatusCode.OK;
                data.AddToLog(ReponseMsg);
                data.Status = resultStatus;
            }
            return data;
        }

        private async Task<DataBag> GetResultASync(DataBag dataBag, string methodUrl, string token)
        {
            HttpResponseMessage httpResponseMsg = null;
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(HttpContentTypes.ApplicationJson));
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
            var ReponseMsg = string.Empty;

            var resultStatus = System.Net.HttpStatusCode.Ambiguous;
            try
            {
                httpResponseMsg = await _httpClient.GetAsync(methodUrl);

                resultStatus = httpResponseMsg.StatusCode;
                var logMsg = string.Format("Log msg: {0} returned {1}", methodUrl, resultStatus);
                _logger.Info(logMsg);

                if (resultStatus == HttpStatusCode.OK)
                {
                    var json = await httpResponseMsg.Content.ReadAsStringAsync();
                    ReponseMsg = ParseJsonResult(json);
                }
                else ReponseMsg = logMsg;
            }
            catch (Exception ex)
            {
                ReponseMsg = ex.Message;
            }

            dataBag.AddToLog(ReponseMsg);
            dataBag.Status = resultStatus;
            return dataBag;
        }

        private string ParseJsonResult(string json)
        {
            string result = null;
            var jsonObj = JObject.Parse(json);
            if (jsonObj["_embedded"]["addresses"].Any())
            {
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
                result = string.Format("{0} {1}, {2}. Surface={3}m2, year {5}. This address has status '{4}'.",
                                        ad.Street, ad.Number, ad.City, ad.Surface, ad.Purpose, ad.Year);
            }
            else
            {
                result = string.Format("-No addres found for this postal.");
            }
            return result;
        }

        private bool PostalLookedUpBefore(PostalCodeLookup lookup)
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