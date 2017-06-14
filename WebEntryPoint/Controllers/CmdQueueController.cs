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
    public partial class CmdQueueController: ApiController
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(CmdQueueController), Helpers.ConfigSettings.LogLevel());
        private CmdBag _cmdResult_HOT = null;
        MSMQWrapper _cmdQueue = null;
        MSMQWrapper _cmdReplyQueue = null;
        private object _lockHOTResult = new object();

        public CmdQueueController()
        {
            _cmdQueue = new MSMQWrapper(Helpers.ConfigSettings.CmdQueue());
            _cmdQueue.SetFormatters(typeof(CmdBag));
            //_cmdQueue.AddHandler(QueueCmdHandler);

            _cmdReplyQueue = new MSMQWrapper(Helpers.ConfigSettings.CmdReplyQueue());
            _cmdReplyQueue.SetFormatters(typeof(CmdBag));
            _cmdReplyQueue.AddHandler(ReplyQueueCmdHandler);
        }

        private void ReplyQueueCmdHandler(object sender, ReceiveCompletedEventArgs e)
        {
            System.Messaging.Message msg = _cmdReplyQueue.Q.EndReceive(e.AsyncResult);
            CmdBag bag   = msg.Body as CmdBag;
            _cmdResult_HOT = new CmdBag(bag);
        }

        [Route("api/CmdQueue/{socketToken}/{cmdType}")]
        public IHttpActionResult Get(string socketToken, string cmdType)
        {
            // can be GetModus GetServiceConfig
            _logger.Debug("GET, Url data received: {0}, {1}", socketToken, cmdType);
            var bag = new CmdBag();
            bag.CmdType= StringToCmdType(cmdType);
            bag.SocketToken = socketToken;

            CmdBag resultBag = DropCmdandWaitForAnswer(bag);

            return Json(new { Message = resultBag.Message, CmdResult = resultBag.CmdResult });
        }
        //// COSR is enabled in  HttpHost, so we don;t need to manually enable options
        //public IHttpActionResult Options()
        //{
        //    return Json(new { message = "" });
        //}
        public IHttpActionResult Post(CmdPostData received)
        {
            _logger.Debug("POST, Data received: {0}", JsonConvert.SerializeObject(received));


            var cmdBag = ProcessPostedData(received); 
            CmdBag resultBag;
            if (cmdBag != null)
            {
                resultBag = DropCmdandWaitForAnswer(cmdBag);
                _logger.Debug("Done busy waiting, received={0}, result ={1}", JsonConvert.SerializeObject(received), JsonConvert.SerializeObject(resultBag));
            }
            else resultBag = new CmdBag { Message = "Error processing posted data", CmdResult = "" };
            return Json(new { Message = resultBag.Message, CmdResult = resultBag.CmdResult });
        }

        private CmdBag DropCmdandWaitForAnswer(CmdBag cmdBag)
        {
            CmdBag resultBag = null;
            lock (_lockHOTResult)
            {
                var msg = new System.Messaging.Message();
                msg.Body = cmdBag;

                _cmdQueue.Send(msg, "cmd");
                _cmdReplyQueue.Q.BeginReceive();

                _logger.Debug("Busy Waiting for command result");
                while (_cmdResult_HOT == null) Task.Delay(100).Wait();

                resultBag = _cmdResult_HOT;
                _cmdResult_HOT = null;
            }
            return resultBag;
        }

        private CmdBag ProcessPostedData(CmdPostData received)
        {
            var cmdBag = new CmdBag();
            cmdBag.SocketToken = received.SocketToken;

            var service1Nr = string.IsNullOrEmpty(received.Service1Nr) ? -1 : Convert.ToInt16(received.Service1Nr);
            var service2Nr = string.IsNullOrEmpty(received.Service2Nr) ? -1 : Convert.ToInt16(received.Service2Nr);
            var service3Nr = string.IsNullOrEmpty(received.Service3Nr) ? -1 : Convert.ToInt16(received.Service3Nr);

            if (!string.IsNullOrEmpty(received.CmdType))
            {
                cmdBag.CmdType = StringToCmdType(received.CmdType);
            }
            if (cmdBag.CmdType == CmdType.NotSet)
            {
                _logger.Debug("Cmd type still not set, determining from values.... ");
                cmdBag.CmdType = (service1Nr < 0 || service2Nr < 0 || service3Nr < 0)
                    ? CmdType.GetModus
                    : CmdType.GetServiceConfig;
                _logger.Debug("Determined Cmd type is {0} ", cmdBag.CmdType);
            }

            if (cmdBag.CmdType == CmdType.SetServiceConfig)
            {
                QServiceConfig s1 = (QServiceConfig)service1Nr;
                QServiceConfig s2 = (QServiceConfig)service2Nr;
                QServiceConfig s3 = (QServiceConfig)service3Nr;
                if (s1 > 0 && s2 > 0 && s3 > 0 &&
                    s1 < QServiceConfig.Enum_End && s2 < QServiceConfig.Enum_End && s3 < QServiceConfig.Enum_End)
                {
                    cmdBag.Service1Nr = s1;
                    cmdBag.Service2Nr = s2;
                    cmdBag.Service3Nr = s3;
                }
                else
                {
                    // Error
                    _logger.Error("cmdtype = SetServiceConfig but servicetype enum converstion failed. {0}, {1}, {2}", service1Nr, service2Nr, service3Nr);
                    return null;
                }

            }
            return cmdBag;
        }

        private CmdType StringToCmdType(string value)
        {
            try
            {
                return (CmdType)Enum.Parse(typeof(CmdType), value, ignoreCase: true);
            }
            catch (Exception ex)
            {
                _logger.Error("Error casting cmd type. Value {0}, message {1}", ex.Message, value);
                return CmdType.NotSet;
            }
        }
    }
}
