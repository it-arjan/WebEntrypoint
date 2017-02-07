using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Messaging;
using WebEntryPoint.MQ;
using WebEntryPoint.ServiceCall;
using WebEntryPoint.WebSockets;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Runtime.Serialization;
using NLogWrapper;

namespace WebEntryPoint
{
    [Authorize]
    public class EntryQueueController: ApiController
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(EntryQueueController));
        //[EnableCors(origins: "http://local.frontend,https://local.frontend,http://ec2-52-57-195-49.eu-central-1.compute.amazonaws.com,https://ec2-52-57-195-49.eu-central-1.compute.amazonaws.com", headers: "*", methods: "*")]
        public IHttpActionResult Get()
        {
            var caller = User as ClaimsPrincipal;
            if (caller != null)
            {
                var subjectClaim = caller.FindFirst("sub");
                if (subjectClaim != null)
                {
                    return Json(new
                    {
                        message = "OK user",
                        client = caller.FindFirst("client_id").Value,
                        subject = subjectClaim.Value
                    });
                }
                else
                {
                    return Json(new
                    {
                        message = "OK computer",
                        client = caller.FindFirst("client_id").Value
                    });
                }
            }
            else
            {
                return Json(new
                {
                    message = "You seem to be anonymous..",
                });
            }
        }

        public async Task<IHttpActionResult> Post()
        {
            var postedstring = await Request.Content.ReadAsStringAsync();
            PostData received;
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(postedstring)))
            {
                // Deserialization from JSON  
                DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(PostData));
                received = (PostData)deserializer.ReadObject(ms);
                received.MessageId = Regex.Replace(received.MessageId, Helpers.RegEx.InvalidMessageIdChars, string.Empty);
            }
            var webTracer = new WebTracer(Helpers.Appsettings.SocketServerUrl());
            webTracer.Send(received.SocketToken, "WebApi: '{0}' received.", received.MessageId);

            _logger.Debug("local Token: {0}", getSessionToken());
            _logger.Debug("remote Token: {0}", received.SocketToken);

            var dataBag = new DataBag();
            dataBag.Label = received.MessageId + " - " + DateTime.Now.ToShortTimeString();
            dataBag.Id = received.MessageId;
            dataBag.PostBackUrl = received.PostBackUrl;
            dataBag.AddToContent(received.MessageId);
            dataBag.socketToken = received.SocketToken;

            //don't use frontend token to postbeack, it might get expired
            //dataBag.IdToken = getSessionToken();
            var msg = new Message();
            msg.Body = dataBag;

            var entryQueue = new MSMQWrapper(@".\Private$\webentry");
            entryQueue.SetFormatters(typeof(DataBag));
            entryQueue.Send(msg, dataBag.Label);

            webTracer.Send(received.SocketToken, "WebApi: dropped '{0}' into the queue.", received.MessageId);
            string resultMsg = string.Format("WebApi: '{0}' is inserted into entryQueue.", received.MessageId);

            return Json(new { message = resultMsg });
        }

        private string getSessionToken()
        {
            return Request.Headers.Authorization.Parameter;
        }

        [DataContract]
        private class PostData
        {
            [DataMember]
            public string MessageId { get; set; }
            [DataMember]
            public string PostBackUrl { get; set; }
            [DataMember]
            public string SocketToken { get; set; }
        }
    }
}
