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
using Newtonsoft.Json;

namespace WebEntryPoint
{
    //[Authorize]
    public class EntryQueueController: ApiController
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(EntryQueueController), Helpers.Appsettings.LogLevel());
        private string _ToggleResult = string.Empty;
        MSMQWrapper _cmdQueue = new MSMQWrapper(Helpers.Appsettings.CmdQueue());

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
        private void QueueCmdHandler(object sender, ReceiveCompletedEventArgs e)
        {
            System.Messaging.Message msg = _cmdQueue.Q.EndReceive(e.AsyncResult);
            DataBag dataBag = msg.Body as DataBag;
            _ToggleResult = dataBag.Content;
        }

        //[HttpPost]
        //[Route("cmd/toggle")]
        //[EnableCors(origins: "http://local.frontend,https://local.frontend", headers: "*", methods: "*")]
        public IHttpActionResult Options()
        {
            return Json(new { message = "" });
        }

        public IHttpActionResult Put(PostData received)
        {
            _logger.Debug("Toggled .. sending the msg..");
            var dataBag = new DataBag();
            dataBag.socketToken = received.SocketToken;

            var msg = new System.Messaging.Message();
            msg.Body = dataBag;

            _cmdQueue.SetFormatters(typeof(DataBag));
            _cmdQueue.AddHandler(QueueCmdHandler);

            _cmdQueue.Send(msg, dataBag.Label);
            // todo listen on another queue
            _logger.Debug("Waiting for toggle result");
            var result = _cmdQueue.Q.BeginReceive();

            while (string.IsNullOrEmpty(_ToggleResult)) Task.Delay(100).Wait();
            _logger.Debug("Toggle result received {0}", _ToggleResult);

            return Json(new { message = _ToggleResult });
        }
        
        public IHttpActionResult Post(PostData received)
        {
            _logger.Debug("Data received: {0}", JsonConvert.SerializeObject(received));

            received.MessageId = Regex.Replace(received.MessageId, Helpers.RegEx.InvalidMessageIdChars, string.Empty);

            var webTracer = new WebTracer(Helpers.Appsettings.SocketServerUrl());
            webTracer.Send(received.SocketToken, "WebApi: '{0}' received.", received.MessageId);


            var dataBag = new DataBag();
            dataBag.Label = received.MessageId + " - " + DateTime.Now.ToShortTimeString();
            dataBag.MessageId = received.MessageId;
            dataBag.PostBackUrl = received.PostBackUrl;
            dataBag.AddToContent("Service output log for '{0}'\n", received.MessageId);
            dataBag.socketToken = received.SocketToken;
            dataBag.doneToken = received.DoneToken;
            dataBag.UserName = received.UserName;
            dataBag.Started = DateTime.Now;

            var msg = new System.Messaging.Message();
            msg.Body = dataBag;

            var entryQueue = new MSMQWrapper(Helpers.Appsettings.EntryQueue());
            entryQueue.SetFormatters(typeof(DataBag));
            entryQueue.Send(msg, dataBag.Label);

            webTracer.Send(received.SocketToken, "WebApi: dropped '{0}' into the queue.", received.MessageId);
            string resultMsg = string.Format("WebApi: '{0}' is inserted into entryQueue.", received.MessageId);

            return Json(new { message = resultMsg });
        }

        private string GetOauthToken()
        {
            return Request.Headers.Authorization.Parameter;
        }

        public class PostData
        {
            public string MessageId { get; set; }
            public string PostBackUrl { get; set; }
            public string SocketToken { get; set; }
            public string DoneToken { get; set; }
            public string UserName { get; set; }
        }
    }
}
