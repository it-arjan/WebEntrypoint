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
using WebEntryPoint.Helpers;

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
            if (received.LogDropRequest)
            {
                RemoteRequestLogger.Log(received.UserName, received.AspSessionId, received.SocketAccessToken, received.SocketApiFeed, "todo", "application/json", "Post", "/EntryQueue");
            }
            var webTracer = new SocketClient(Helpers.ConfigSettings.SocketServerUrl());
            if (!string.IsNullOrEmpty(received.MessageId ))
            {
                int maxlength = 20;
                if (received.MessageId.Length > maxlength)
                {
                    received.MessageId = received.MessageId.Substring(0, maxlength);
                    resultMsg += string.Format("message truncated to {0} ", maxlength);
                }
                received.MessageId = Regex.Replace(received.MessageId, Helpers.RegEx.InvalidMessageIdChars, string.Empty);

                webTracer.Send(
                    received.SocketAccessToken, received.SocketQmFeed, 
                    string.Format("WebApi: '{0}' received, dropping it ({1}) times", received.MessageId, received.NrDrops)
                    );

                DropInQueue(received);
                resultMsg += string.Format(" Dropped '{0}' ({1}) times in the entryQueue.", received.MessageId, received.NrDrops);
                webTracer.Send(received.SocketAccessToken, received.SocketQmFeed, resultMsg);
            }
            else
            {
                resultMsg = string.Format("Queue manager webApi: Dropping nothing gets you nothing ;)", received.MessageId);
                webTracer.Send(received.SocketAccessToken, received.SocketQmFeed, resultMsg);
            }
            return Json(new { message = resultMsg });
        }

        private static void DropInQueue(EntryQueuePostData received)
        {
            for (int dropNr = 1; dropNr <= received.NrDrops; dropNr++)
            {
                var msgId = received.NrDrops == 1 ? received.MessageId : string.Format("{0}-{1}", dropNr, received.MessageId);
                var dataBag = new DataBag(received);
                dataBag.MessageId = msgId;
                var msg = new System.Messaging.Message();
                msg.Body = dataBag;

                var entryQueue = new MSMQWrapper(Helpers.ConfigSettings.EntryQueue());
                entryQueue.SetFormatters(typeof(DataBag));
                entryQueue.Send(msg, dataBag.Label);
            }
        }
    }
}
