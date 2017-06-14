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
    public class WebserviceSettingsController: ApiController
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(WebserviceSettingsController), Helpers.ConfigSettings.LogLevel());
        private string _ToggleResult = string.Empty;
        MSMQWrapper _cmdQueue = new MSMQWrapper(Helpers.ConfigSettings.CmdQueue());
        public int Delay { get; private set; }
        public int FailRate { get; private set;}
        public WebserviceSettingsController()
        {
            Delay = 50;
            FailRate = 30;
        }
        public IHttpActionResult Get()
        {
            return Json(new { Delay =50, FailRate=30 });
        }

        public IHttpActionResult Post(SettingData data)
        {
            Delay = data.Delay;
            FailRate = data.FailRate;
            return Json(new { Delay = Delay, FailRate = FailRate });
        }

        [Authorize] //?
        public class SettingData
        {
            public int Delay{ get; set; }
            public int FailRate{ get; set; }
        }
    }
}
