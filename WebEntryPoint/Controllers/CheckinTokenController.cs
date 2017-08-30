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
    public class CheckinTokenController : ApiController
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(CheckinTokenController), Helpers.ConfigSettings.LogLevel());
        MSMQWrapper _checkinTokenQueue = null;

        public CheckinTokenController()
        {
            _checkinTokenQueue = new MSMQWrapper(Helpers.ConfigSettings.CheckinTokenQueue());
            _checkinTokenQueue.SetFormatters(typeof(string));
        }
        public IHttpActionResult Get() {
            return Content(System.Net.HttpStatusCode.OK, "YES!!!!");
        }

        public IHttpActionResult Post()
        {
            string token = Request.Content.ReadAsStringAsync().Result;
            _logger.Debug("POST, Data received: {0}", token);

            var msg = new System.Messaging.Message();
            msg.Body = token;
            _checkinTokenQueue.Send(msg, "CheckinToken");
            return Json(new { Message = "access Token dropped into the queue" });
        }



    }
}
