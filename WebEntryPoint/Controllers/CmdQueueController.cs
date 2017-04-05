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
    public class CmdQueueController: ApiController
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(CmdQueueController), Helpers.Appsettings.LogLevel());
        private string _ToggleResult = string.Empty;
        MSMQWrapper _cmdQueue = new MSMQWrapper(Helpers.Appsettings.CmdQueue());

        private void QueueCmdHandler(object sender, ReceiveCompletedEventArgs e)
        {
            System.Messaging.Message msg = _cmdQueue.Q.EndReceive(e.AsyncResult);
            DataBag dataBag = msg.Body as DataBag;
            _ToggleResult = dataBag.Content;
        }
        public IHttpActionResult Get()
        {
            return Json(new { message = "Hello" });
        }
        //// COSR is enabled in  HttpHost
        //public IHttpActionResult Options()
        //{
        //    return Json(new { message = "" });
        //}

        public IHttpActionResult Post(CmdPostData received)
        {
            _logger.Debug("Toggled, data ={0}\n .. sending the msg..");
            _logger.Debug("Data received: {0}", JsonConvert.SerializeObject(received));
            var dataBag = new DataBag();
            dataBag.socketToken = received.SocketToken;

            var msg = new System.Messaging.Message();
            msg.Body = dataBag;

            _cmdQueue.SetFormatters(typeof(DataBag));
            _cmdQueue.AddHandler(QueueCmdHandler);

            _cmdQueue.Send(msg, dataBag.Label);
            // todo listen on another queue
            _logger.Debug("Busy Waiting for toggle result");
            var result = _cmdQueue.Q.BeginReceive();

            while (string.IsNullOrEmpty(_ToggleResult)) Task.Delay(100).Wait();

            _logger.Debug("Toggle result received {0}", _ToggleResult);

            return Json(new { message = _ToggleResult });
        }

        public class CmdPostData
        {
            public string MessageId { get; set; }
            public string PostBackUrl { get; set; }
            public string SocketToken { get; set; }
            public string DoneToken { get; set; }
            public string UserName { get; set; }
        }
    }
}
