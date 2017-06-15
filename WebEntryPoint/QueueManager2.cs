using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Messaging;
using System.Threading;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using NLogWrapper;
using WebEntryPoint.WebSockets;
using EasyHttp.Http;
using System.Runtime.Serialization.Json;
using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using IdentityModel.Client;
using WebEntryPoint.ServiceCall;
using WebEntryPoint.Helpers;

namespace WebEntryPoint.MQ
{

    public class QueueManager2
    {
        public enum ExeModus { Sequential, Paralell };
        private ExeModus _exeModus = ExeModus.Paralell;
        private object _activeServiceMapperLock = new object();
        private bool _initialized;

        private MSMQWrapper _entryQ;
        private MSMQWrapper _exitQ;
        private MSMQWrapper _service1Q;
        private MSMQWrapper _service2Q;
        private MSMQWrapper _service3Q;
        private MSMQWrapper _cmdQ;
        private MSMQWrapper _cmdReplyQ;

        private Dictionary<QServiceConfig, IWebService> _serviceMap;
        private Dictionary<ProcessPhase, QServiceConfig> _activeServiceMapper;
        static ILogger _logger = LogManager.CreateLogger(typeof(QueueManager2), Helpers.ConfigSettings.LogLevel());
        private ISocketClient _webTracer;

        private IWebserviceFactory _wsFactory;

        private ITokenCache _tokenManager;
        Stopwatch _BatchTicker = new Stopwatch();

        public bool ProcessMsgPerMsg { get; set; }
        public bool UseTimedRetry { get; set; }

        public List<string> ProcessedList { get; set; }

        private int _doneCount;
        public int DoneCount {
            get { return _doneCount; }
            private set {
                _doneCount = value;
                if (DoneCount > 0 && DoneCount == AddCount)
                {
                    _BatchTicker.Stop();
                    //MessageBox.Show(string.Format("Done in  {0}", _ticker.Elapsed.ToString("mm\\:ss\\.fff")));
                }
            }
        }
        public int AddCount { get; private set; }
        object _processed = new object();

        public QueueManager2(string entry_Q, string service1_Q, string service2_Q, string service3_Q, string exit_Q, string cmd_Q, string cmdReply_Q,
                                IWebserviceFactory wsFactoryInject,
                                ITokenCache tokenManagerInject,
                                ISocketClient socketClientInject
            )
        {
            _wsFactory = wsFactoryInject;
            _tokenManager = tokenManagerInject;
            ProcessedList = new List<string>();
            _serviceMap = new Dictionary<QServiceConfig, IWebService>();
            _activeServiceMapper = new Dictionary<ProcessPhase, QServiceConfig>();
            _webTracer = socketClientInject;

            Init(entry_Q, service1_Q, service2_Q, service3_Q, exit_Q, cmd_Q, cmdReply_Q);
        }

        public delegate void EventHandlerWithQueue(object sender, ReceiveCompletedEventArgs e, MSMQWrapper queue);

        private string Init(string entry_Q, string service1_Q, string service2_Q, string service3_Q, string exit_Q, string cmd_Q, string cmdReply_Q)
        {
            if (_initialized)
            {
                return "Already initialized, call stop";
            }
            CheckQueuePaths(entry_Q, service1_Q, service2_Q, service3_Q, exit_Q, cmd_Q, cmdReply_Q);

            try
            {
                ProcessMsgPerMsg = true;
                UseTimedRetry = false;

                _cmdQ = new MSMQWrapper(cmd_Q);
                _cmdReplyQ = new MSMQWrapper(cmdReply_Q);

                // clean up the cmd queues
                _cmdQ.Q.Purge();
                _cmdReplyQ.Q.Purge();

                _entryQ = new MSMQWrapper(entry_Q);
                _service1Q = new MSMQWrapper(service1_Q);
                _service2Q = new MSMQWrapper(service2_Q);
                _service3Q = new MSMQWrapper(service3_Q);
                _exitQ = new MSMQWrapper(exit_Q);
                ConfigureQFormatters_Handlers();

                var serviceNr = QServiceConfig.Service1;
                while (serviceNr != QServiceConfig.Enum_End)
                {
                    _serviceMap.Add(serviceNr, _wsFactory.Create(serviceNr, _tokenManager));
                    serviceNr++;
                }
                ConfigureActiveServices(QServiceConfig.Service1, QServiceConfig.Service2, QServiceConfig.Service3);
                _initialized = true;
            }
            catch (Exception ex)
            {
                var ex2 = new Exception(
                    string.Format("Error intitializing QM : {0}, {1}, {2}, {3}, {4}", 
                                    entry_Q, service1_Q, service2_Q, service3_Q, exit_Q),
                                    ex
                    );
                throw ex2;
            }
            return null;

        }

        private void ConfigureQFormatters_Handlers()
        {
            _cmdQ.SetFormatters(typeof(CmdBag), typeof(string));
            _cmdQ.AddHandler(QueueCmdHandler);

            _cmdReplyQ.SetFormatters(typeof(CmdBag), typeof(string));
            // nothing is read from _cmdReplyQ

            _entryQ.SetFormatters(typeof(DataBag), typeof(string));
            _entryQ.AddHandler(EntryHandler);

            _service1Q.SetFormatters(typeof(DataBag), typeof(string));
            _service1Q.AddHandler(GenericHandler);

            _service2Q.SetFormatters(typeof(DataBag), typeof(string));
            _service2Q.AddHandler(GenericHandler);

            _service3Q.SetFormatters(typeof(DataBag), typeof(string));
            _service3Q.AddHandler(GenericHandler);

            _exitQ.SetFormatters(typeof(DataBag), typeof(string));
            _exitQ.AddHandler(ExitHandler);
        }

        private CmdBag GetActiveServices()
        {
            var result = new CmdBag();
            result.Service1Nr = _activeServiceMapper[ProcessPhase.Service1];
            result.Service2Nr = _activeServiceMapper[ProcessPhase.Service2];
            result.Service3Nr = _activeServiceMapper[ProcessPhase.Service3];
            return result;
        }

        private CmdBag ConfigureActiveServices(QServiceConfig service1Nr, QServiceConfig service2Nr, QServiceConfig service3Nr)
        {
            lock (_activeServiceMapperLock)
            {
                _activeServiceMapper.Clear();
                _activeServiceMapper.Add(ProcessPhase.Service1, service1Nr);
                _activeServiceMapper.Add(ProcessPhase.Service2, service2Nr);
                _activeServiceMapper.Add(ProcessPhase.Service3, service3Nr);
            }
            return GetActiveServices();
        }
        private void CheckQueuePaths(string entry_Q, string service1_Q, string service2_Q, string service3_Q, string exit_Q, string cmd_Q, string cmdReply_Q)
        {
            // The choice is not to auto-create queues

            string nonexist = string.Empty;
            if (!MessageQueue.Exists(entry_Q)) nonexist += entry_Q;
            if (!MessageQueue.Exists(service1_Q)) nonexist += ("'" + service1_Q);
            if (!MessageQueue.Exists(service2_Q)) nonexist += ("'" + service2_Q);
            if (!MessageQueue.Exists(service3_Q)) nonexist += ("'" + service3_Q);
            if (!MessageQueue.Exists(exit_Q)) nonexist += ("'" + exit_Q);
            if (!MessageQueue.Exists(cmd_Q)) nonexist += ("'" + cmd_Q);
            if (!MessageQueue.Exists(cmdReply_Q)) nonexist += ("'" + cmdReply_Q);

            if (nonexist != string.Empty)
            {
                var msg = string.Format("Following queuenames do not exist on the  machine: {0}", nonexist);

                _logger.Error(msg);
                throw new Exception(msg);
            }
        }

        public void StartListening()
        {
            if (!_initialized) throw new Exception("Queuemanager not initialized.");

            _cmdQ.BeginReceive();
            _entryQ.BeginReceive();
            _service1Q.BeginReceive();
            _service2Q.BeginReceive();
            _service3Q.BeginReceive(); 
            _exitQ.BeginReceive();

            _logger.Info("Queue manager listening on queues {0}, {1}, {2}, {3}, {4}. \nCommand queue = {5}", 
                _entryQ.Q.FormatName, _service1Q.Q.FormatName, _service2Q.Q.FormatName, _service3Q.Q.FormatName, _exitQ.Q.FormatName, _cmdQ.Q.FormatName);
        }

        public ExeModus ToggleModus()
        {
            _exeModus = RunsParalell() ? ExeModus.Sequential : ExeModus.Paralell;
            return GetModus();
        }

        public ExeModus GetModus()
        {
            return _exeModus;
        }

        private bool RunsParalell()
        {
            return _exeModus == ExeModus.Paralell;
        }

        private void QueueCmdHandler(object sender, ReceiveCompletedEventArgs e)
        {
            System.Messaging.Message msg = _cmdQ.Q.EndReceive(e.AsyncResult);
            CmdBag bag = msg.Body as CmdBag;
            string logMsg = string.Empty;
            switch (bag.CmdType)
            {
                case CmdType.GetModus:
                    bag.CmdResult = GetModus().ToString();
                    logMsg = string.Format("modus is {0}", bag.CmdResult);
                    bag.Message = logMsg;
                    _logger.Debug(logMsg);
                    break;
                case CmdType.ToggleModus:
                    bag.CmdResult = ToggleModus().ToString();
                    logMsg = string.Format("modus now {0}", bag.CmdResult);
                    bag.Message = logMsg;
                    _logger.Debug(logMsg);

                    _webTracer.Send(bag.SocketToken, logMsg);
                    _logger.Debug(logMsg);
                    break;
                case CmdType.GetServiceConfig:
                    CmdBag newBag = GetActiveServices();
                    bag.Service1Nr = newBag.Service1Nr;
                    bag.Service2Nr = newBag.Service2Nr;
                    bag.Service3Nr = newBag.Service3Nr;
                    bag.CmdResult = string.Format("{0},{1},{2}", (int)bag.Service1Nr, (int)bag.Service2Nr, (int)bag.Service3Nr);

                    logMsg = string.Format("Current service config is {0}, {1}, {2}", bag.Service1Nr, bag.Service2Nr, bag.Service3Nr);
                    _webTracer.Send(bag.SocketToken, logMsg);
                    bag.Message = logMsg;
                    break;

                case CmdType.SetServiceConfig:
                    CmdBag newBag2 = ConfigureActiveServices(bag.Service1Nr, bag.Service2Nr, bag.Service3Nr);
                    bag.Service1Nr = newBag2.Service1Nr;
                    bag.Service2Nr = newBag2.Service2Nr;
                    bag.Service3Nr = newBag2.Service3Nr;

                    bag.CmdResult = string.Format("{0},{1},{2}", (int)bag.Service1Nr, (int)bag.Service2Nr, (int)bag.Service3Nr);
                    logMsg = string.Format("Service config set to {0}, {1}, {2}", bag.Service1Nr, bag.Service2Nr, bag.Service3Nr);
                    _webTracer.Send(bag.SocketToken, logMsg);
                    bag.Message = logMsg;
                    break;
            }

            _cmdReplyQ.Send(msg);
            _cmdQ.BeginReceive();
        }

        private void EntryHandler(object sender, ReceiveCompletedEventArgs e)
        {
            System.Messaging.Message msg = _entryQ.Q.EndReceive(e.AsyncResult);
            DataBag msgObj = msg.Body as DataBag;
            _logger.Debug("EntryHandler: picking up {0}", msgObj.MessageId);
            msgObj.AddToLog("Service log for '{0}'", msgObj.MessageId);
            ProcessPhase phase = ProcessPhase.Service1;
            while (phase != ProcessPhase.Completed)
            {
                msgObj.AddToLog("{0}: {1}, {2}", phase, GetService(phase).Name, GetService(phase).Description());
                msgObj.AddShortSeparator();
                phase++;
            }           
            msgObj.AddSeparator();
            //if (!_BatchTicker.IsRunning) _BatchTicker.Start();
            //AddCount++;

            _service1Q.Send(msg);
            _webTracer.Send(msgObj.socketToken, "Entry: Dropped {0} in the Queue for service.1", msgObj.MessageId);

            _entryQ.BeginReceive(); 
        }

        //private async void GenericHandler(object sender, ReceiveCompletedEventArgs e, MSMQWrapper queue)
        private async void GenericHandler(object sender, ReceiveCompletedEventArgs e)
        {
            var bareQ = ((MessageQueue)sender);
            System.Messaging.Message msg = bareQ.EndReceive(e.AsyncResult);
            DataBag dataBag = msg.Body as DataBag;

            if (dataBag != null)
            {
                string logMsg = string.Format("Received '{0}' from '{1}'", dataBag.MessageId, bareQ.Path);
                _logger.Debug(logMsg);
                _webTracer.Send(dataBag.socketToken, logMsg);

                string LogMsg = string.Empty;
                bool paralell = DetermineModeForThisCall(dataBag, out LogMsg) == ExeModus.Paralell;
                dataBag.AddToLog(LogMsg);

                if (paralell)
                {
                    bareQ.BeginReceive();
                    await ProcessMessageAsync(msg);
                }
                else
                {
                    await ProcessMessageAsync(msg);
                    bareQ.BeginReceive();
                }
            }
            else
            {
                _logger.Error("Conversion to DataBag Object failed!");
            }
        }

        private ExeModus DetermineModeForThisCall(DataBag dataBag, out string msg)
        {
            var result = ExeModus.Sequential;
            var resultMsg = string.Empty;
            var paralell = this.RunsParalell();
            if (paralell)
            {
                //code from before the semaphore
                var SemQtooLong = GetService(dataBag.CurrentPhase).SemaphoreQueueLength > 1;
                result = SemQtooLong ? ExeModus.Sequential : ExeModus.Paralell;

                resultMsg = string.Format("{0}-Wrapper: ", GetService(dataBag.CurrentPhase).Name);
                if (SemQtooLong)
                {
                    resultMsg += string.Format("Handling this request Sequential to reduce the service load",
                      GetService(dataBag.CurrentPhase).ServiceLoad);
                }
                else resultMsg += "Handling request Paralell";
            }
            else resultMsg += "Handling request Sequential";
            msg = resultMsg;
            return result;
        }

        private void ExitHandler(object sender, ReceiveCompletedEventArgs e)
        {

            System.Messaging.Message msg = _exitQ.Q.EndReceive(e.AsyncResult);
            DataBag msgObj = msg.Body as DataBag;

            _webTracer.Send(msgObj.socketToken,
                "Exit handler: received '{0}' as completed, posting it back..", msgObj.MessageId);

            var postbackService = _wsFactory.Create(QServiceConfig.Service8, _tokenManager);
            postbackService.Url = msgObj.PostBackUrl;
            var status = postbackService.CallSync(msgObj); // #TODO call Async
            _webTracer.Send(msgObj.socketToken, "Postback returned {0}", status);
            if (status == HttpStatusCode.OK) _webTracer.Send(msgObj.socketToken, msgObj.doneToken);
            _exitQ.BeginReceive();
        }

        public int StopAll()
        {
            if (!_initialized)
            {
                return -1;
            }
            _initialized = false;

            _cmdQ.RemoveHandler(QueueCmdHandler);
            _cmdQ.Q.Dispose();

            _cmdReplyQ.RemoveHandler(QueueCmdHandler);
            _cmdReplyQ.Q.Dispose();

            _entryQ.RemoveHandler(EntryHandler);
            _entryQ.Q.Dispose();

            _service1Q.RemoveHandler(GenericHandler);
            _service1Q.Q.Dispose();

            _service2Q.RemoveHandler(GenericHandler);
            _service2Q.Q.Dispose();

            _service3Q.RemoveHandler(GenericHandler);
            _service3Q.Q.Dispose();

            _exitQ.RemoveHandler(ExitHandler);
            _exitQ.Q.Dispose();

            return 0;
        }

        private MSMQWrapper GetQueue(ProcessPhase phase)
        {
            switch (phase)
            {
                case ProcessPhase.Entry:
                    return _entryQ;
                case ProcessPhase.Service1:
                    return _service1Q;
                case ProcessPhase.Service2:
                    return _service2Q;
                case ProcessPhase.Service3:
                    return _service3Q;
                case ProcessPhase.Completed:
                    return _exitQ;
                default:
                    throw new Exception("Unknown process phase");
            }
        }

        private async Task ProcessMessageAsync(System.Messaging.Message msg)
        {
            var dataBag = msg.Body as DataBag;
            var service = GetService(dataBag.CurrentPhase);

            _webTracer.Send(dataBag.socketToken, "Calling '{0}' with '{1}'", service.Name, dataBag.MessageId);

            dataBag.TryCount++;
            dataBag = await service.CallAsync(dataBag);

            msg.Body = dataBag; 
            var retry = dataBag.Retry;
            if (retry)
            {
                if (dataBag.TryCount < service.MaxRetries)
                {
                    int delay = 1;
                    _webTracer.Send(dataBag.socketToken, "{0} failed for {1}, Retry in {2} secs", service.Name, dataBag.MessageId, delay);
                    dataBag.AddToLog("retry in {0} secs", delay);
                    if (UseTimedRetry) ReQueueWithTimedDelay(msg, delay);
                    else await ReQueueWithTaskDelay(msg, delay);
                }
                else
                {
                    dataBag.Status = HttpStatusCode.ServiceUnavailable;
                    dataBag.AddToLog("{0} failed too many times, max-retries={1}. Skipping...", service.Url, service.MaxRetries);
                    retry = false;
                }
            }

            if (!retry)
            {
                dataBag.AddSeparator();
                var oldPhase = dataBag.CurrentPhase;
                dataBag.CurrentPhase++;
                dataBag.Status = HttpStatusCode.OK;
                dataBag.TryCount = 0;
                _webTracer.Send(dataBag.socketToken, "Call to {1} (={3}) succeeded, dropping {0} in the MQ for {2}", dataBag.MessageId, oldPhase, dataBag.CurrentPhase, service.Name);
                GetQueue(dataBag.CurrentPhase).Send(msg, dataBag.Label);
            }
        }

        private IWebService GetService(ProcessPhase phase)
        {
            IWebService result;
            lock (_activeServiceMapperLock)
            {
                result = _serviceMap[_activeServiceMapper[phase]];
            }
            return result;
        }

        private ProcessPhase NextPhase(DataBag bag )
        {
            switch (bag.CurrentPhase)
            {
                case ProcessPhase.Entry:
                    return ProcessPhase.Service1;
                case ProcessPhase.Service1:
                    return ProcessPhase.Service2;
                case ProcessPhase.Service2:
                    return ProcessPhase.Service3;
                case ProcessPhase.Service3:
                    return ProcessPhase.Completed;
                default:
                    throw new Exception("Impossible Current phase -> " + bag.CurrentPhase);
            };
        }
        private async Task ReQueueWithTaskDelay(System.Messaging.Message msg, int secs)
        {
            await Task.Delay(1000 * secs);
            DataBag msgObj = msg.Body as DataBag;
            GetQueue(msgObj.CurrentPhase).Send(msg, msgObj.Label);

        }

        private void ReQueueWithTimedDelay(System.Messaging.Message msg, int secs)
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 1000 * secs;
            timer.Elapsed += (sender, e) => Timer_Elapsed(sender, e, msg); // #todo check this syntax
            timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e, System.Messaging.Message msg)
        {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Enabled = false;
            DataBag msgObj = msg.Body as DataBag;
            GetQueue(msgObj.CurrentPhase).Send(msg, msgObj.Label);
        }


    }

}
