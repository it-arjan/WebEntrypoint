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

    [Authorize]
    public partial class EntryQueueController: ApiController
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(EntryQueueController), Helpers.ConfigSettings.LogLevel());
        private string _ToggleResult = string.Empty;
        MSMQWrapper _cmdQueue = new MSMQWrapper(Helpers.ConfigSettings.CmdQueue());

        public IHttpActionResult Post(EntryQueuePostData received)
        {
            _logger.Debug("Data received: {0}", JsonConvert.SerializeObject(received));
            string resultMsg = string.Empty;
            var webTracer = new SocketClient(Helpers.ConfigSettings.SocketServerUrl()); 
            if (!string.IsNullOrEmpty(received.MessageId ))
            {
                received.MessageId = Regex.Replace(received.MessageId, Helpers.RegEx.InvalidMessageIdChars, string.Empty);
                webTracer.Send(received.SocketToken, "WebApi: '{0}' received, dropping it ({1}) times", received.MessageId, received.NrDrops);
                for (int dropNr = 1; dropNr <= received.NrDrops; dropNr++)
                {
                    var msgId = received.NrDrops ==1? received.MessageId: string.Format("{0}-{1}", dropNr, received.MessageId);
                    var dataBag = new DataBag();
                    dataBag.Label = received.MessageId + " - " + DateTime.Now.ToShortTimeString();
                    dataBag.MessageId = msgId;
                    dataBag.PostBackUrl = received.PostBackUrl;
                    dataBag.socketToken = received.SocketToken;
                    dataBag.notificationToken = received.NotificationToken;
                    dataBag.doneToken = received.DoneToken;
                    dataBag.AspSessionId = received.AspSessionId;
                    dataBag.UserName = received.UserName;
                    dataBag.Started = DateTime.Now;

                    var msg = new System.Messaging.Message();
                    msg.Body = dataBag;

                    var entryQueue = new MSMQWrapper(Helpers.ConfigSettings.EntryQueue());
                    entryQueue.SetFormatters(typeof(DataBag));
                    entryQueue.Send(msg, dataBag.Label);
                }
                resultMsg = string.Format("Queue manager webApi: Dropped '{0}' ({1}) times in the entryQueue.", received.MessageId, received.NrDrops);
                webTracer.Send(received.SocketToken, resultMsg);
            }
            else
            {
                resultMsg = string.Format("Queue manager webApi: Dropping nothing gets you nothing ;)", received.MessageId);
                webTracer.Send(received.SocketToken, resultMsg);

            }
            return Json(new { message = resultMsg });
        }
    }
}
