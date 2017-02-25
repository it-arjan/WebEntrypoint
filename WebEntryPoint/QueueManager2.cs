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
namespace WebEntryPoint.MQ
{
    public class QueueManager2
    {
        //public delegate void MessageReceivedEventHandler(object sender, MessageEventArgs args);
        private bool _initialized;

        private MSMQWrapper _entryQ;
        private MSMQWrapper _exitQ;
        private MSMQWrapper _service1Q;
        private MSMQWrapper _service2Q;
        private MSMQWrapper _service3Q;

        private Dictionary<ProcessPhase, WebService> _serviceMap;
        static ILogger _logger = LogManager.CreateLogger(typeof(QueueManager2));
        private WebTracer _webTracer;

        private TokenManager _tokenManager;
        Stopwatch _ticker = new Stopwatch();

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
                    _ticker.Stop();
                    //MessageBox.Show(string.Format("Done in  {0}", _ticker.Elapsed.ToString("mm\\:ss\\.fff")));
                }
            }
        }
        public int AddCount { get; private set; }
        object _processed = new object();

        public QueueManager2(string entry_Q, string service1_Q, string service2_Q, string service3_Q, string exit_Q)
        {
            ProcessedList = new List<string>();
            _serviceMap = new Dictionary<ProcessPhase, WebService>();
            _webTracer = new WebTracer(Helpers.Appsettings.SocketServerUrl());
            _tokenManager = new TokenManager();

            Init(entry_Q, service1_Q, service2_Q, service3_Q, exit_Q);
        }

        public void ResetCounters()
        {
            lock (_processed)
            {
                DoneCount = 0;
                AddCount = 0;
                _ticker.Reset();
                ProcessedList.Clear();
            }
        }

        public delegate void EventHandlerWithQueue(object sender, ReceiveCompletedEventArgs e, MSMQWrapper queue);

        public string Init(string entry_Q, string service1_Q, string service2_Q, string service3_Q, string exit_Q)
        {
            if (_initialized)
            {
                return "Already initialized, call stop";
            }
            CheckQueuePaths(entry_Q, service1_Q, service2_Q, service3_Q,exit_Q);

            try
            {
                ProcessMsgPerMsg = true;
                UseTimedRetry = false;

                _entryQ = new MSMQWrapper(entry_Q);
                _service1Q = new MSMQWrapper(service1_Q);
                _service2Q = new MSMQWrapper(service2_Q);
                _service3Q = new MSMQWrapper(service3_Q);
                _exitQ = new MSMQWrapper(exit_Q);

                _entryQ.SetFormatters(typeof(DataBag), typeof(string));
                _entryQ.AddHandler(EntryHandler);

                _service1Q.SetFormatters(typeof(DataBag), typeof(string));
                _service1Q.AddHandler(GenericHandler, _service1Q);

                _service2Q.SetFormatters(typeof(DataBag), typeof(string));
                _service2Q.AddHandler(GenericHandler, _service2Q);

                _service3Q.SetFormatters(typeof(DataBag), typeof(string));
                _service3Q.AddHandler(GenericHandler, _service3Q);

                _exitQ.SetFormatters(typeof(DataBag), typeof(string));
                _exitQ.AddHandler(ExitHandler);

                // adding service mapping
                _serviceMap.Add(ProcessPhase.Service1, Factory.Create(ProcessPhase.Service1));
                _serviceMap.Add(ProcessPhase.Service2, Factory.Create(ProcessPhase.Service2));
                _serviceMap.Add(ProcessPhase.Service3, Factory.Create(ProcessPhase.Service3));
                _serviceMap.Add(ProcessPhase.Completed, Factory.Create(ProcessPhase.Completed));

                _initialized = true;
            }
            catch (Exception ex)
            {
                var ex2 = new Exception(
                    string.Format("Error intitializing one of the queues : {0}, {1}, {2}, {3}, {4}", 
                                    entry_Q, service1_Q, service2_Q, service3_Q, exit_Q),
                                    ex
                    );
                throw ex2;
            }
            return null;

        }

        private void CheckQueuePaths(string entry_Q, string service1_Q, string service2_Q, string service3_Q, string exit_Q)
        {
            // The choice is not to auto-create queues

            string nonexist = string.Empty;
            if (!MessageQueue.Exists(entry_Q)) nonexist += entry_Q;
            if (!MessageQueue.Exists(service1_Q)) nonexist += ("'" + service1_Q);
            if (!MessageQueue.Exists(service2_Q)) nonexist += ("'" + service2_Q);
            if (!MessageQueue.Exists(service3_Q)) nonexist += ("'" + service3_Q);
            if (!MessageQueue.Exists(exit_Q)) nonexist += ("'" + exit_Q);

            if (nonexist != string.Empty)
            {
                var ex2 = new Exception(
                    string.Format("Following queuenames do not exist on the  machine: {0}",
                                    nonexist)
                    );
            }
        }

        public void StartListening()
        {
            if (!_initialized) throw new Exception("Queuemanager not initialized.");

            _entryQ.BeginReceive();
            _service1Q.BeginReceive();
            _service2Q.BeginReceive();
            _service3Q.BeginReceive();
            _exitQ.BeginReceive();

            _logger.Info("Queue manager listening on queues {0}, {1}, {2}, {3}, {4}", 
                _entryQ.Q.FormatName, _service1Q.Q.FormatName, _service2Q.Q.FormatName, _service3Q.Q.FormatName, _exitQ.Q.FormatName);
        }

        private void EntryHandler(object sender, ReceiveCompletedEventArgs e)
        {
            System.Messaging.Message msg = _entryQ.Q.EndReceive(e.AsyncResult);
            DataBag msgObj = msg.Body as DataBag;

            _tokenManager.SetToken(msgObj.SiliconToken);
            if (!_ticker.IsRunning) _ticker.Start();
            AddCount++;

            _service1Q.Send(msg);
            _webTracer.Send(msgObj.socketToken, "EntryHandler: Dropped {0} in the Q for service1", msgObj.Id);

            _entryQ.BeginReceive(); 
        }

        private async void GenericHandler(object sender, ReceiveCompletedEventArgs e, MSMQWrapper queue)
        {
            // we could also cast the sender to a msmq, but not to MSMQWrapper, so we use EndReceive
            System.Messaging.Message msg = queue.Q.EndReceive(e.AsyncResult);
            DataBag msgObj = msg.Body as DataBag;
            _webTracer.Send(msgObj.socketToken, "Geneneric Handler received '{0}' from queue '{1}'", msgObj.Id, queue.Name);

            await ProcessMessageAsync(msg);
            queue.BeginReceive();
        }

        private void ExitHandler(object sender, ReceiveCompletedEventArgs e)
        {
            //send done notification to MVC app?
            System.Messaging.Message msg = _exitQ.Q.EndReceive(e.AsyncResult);
            DataBag msgObj = msg.Body as DataBag;

            _webTracer.Send(msgObj.socketToken,
                "Received '{0}' as completed, posting it back to you ", msgObj.Id);

            var status = PostBackUsingEasyHttp(_tokenManager.GetToken(), msgObj);
            _webTracer.Send(msgObj.socketToken,
                "Postback returned {0}", status);
            _exitQ.BeginReceive();
        }

        private HttpStatusCode PostBackUsingEasyHttp(string token, DataBag msgObj)
        {
            _logger.Debug("Calling postback '{0}'", msgObj.PostBackUrl);
            var eHttp = new EasyHttp.Http.HttpClient();
            var auth_header = string.Format("Bearer {0}", token);

            eHttp.Request.AddExtraHeader("Authorization", auth_header);
            var result= eHttp.Post(msgObj.PostBackUrl, msgObj, HttpContentTypes.ApplicationJson).StatusCode;
            _logger.Debug("Postback returned '{0}': (1)", result);

            return result;
        }

        private async Task PostBackUsingHttpClient(DataBag msgObj)
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", msgObj.socketToken);
 
                var byteContent = SerializeDataBag(msgObj);
                var response = await client.PostAsync(msgObj.PostBackUrl, byteContent);

                _logger.Debug("postback returned '{0}'", response.StatusCode);
                _webTracer.Send(msgObj.socketToken, "Posting back the result of {0} to {1} returned {2}",
                                        msgObj.Id, msgObj.PostBackUrl, response.StatusCode);
            }
        }

        private ByteArrayContent SerializeDataBag(DataBag msgObj)
        {
            var myContent = JsonConvert.SerializeObject(msgObj);
            var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
            return new ByteArrayContent(buffer);
        }

        public string StopAll()
        {
            if (!_initialized)
            {
                return "Not running";
            }
            _initialized = false;

            ResetCounters();

            _entryQ.RemoveHandler(EntryHandler);
            _service1Q.RemoveHandler(GenericHandler);
            _service2Q.RemoveHandler(GenericHandler);
            _service3Q.RemoveHandler(GenericHandler);
            _exitQ.RemoveHandler(ExitHandler);
            return null;
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
            DataBag dataBag = msg.Body as DataBag;
            if (dataBag != null)
            {
                _webTracer.Send(dataBag.socketToken, "Calling '{0}' with '{1}'", dataBag.CurrentPhase, dataBag.Id);

                var service = _serviceMap[dataBag.CurrentPhase];
                dataBag.TryCount++;
                dataBag = await service.Call(dataBag);

                msg.Body = dataBag;

                if (dataBag.Error)
                {
                    int delay = 1;
                    _webTracer.Send(dataBag.socketToken, "Call to {1} (={2}) failed for {0}, Retry in {3} secs", dataBag.Id, dataBag.CurrentPhase, service.Name, delay);
                    if (UseTimedRetry) ReQueueWithTimedDelay(msg, delay);
                    else await ReQueueWithTaskDelay(msg, delay);
                }
                else
                {
                    var oldPhase = dataBag.CurrentPhase;
                    dataBag.NextService();
                    dataBag.TryCount = 0;
                    _webTracer.Send(dataBag.socketToken, "Call to {1} (={3}) succeeded, dropping {0} in the MSMQ for {2}", dataBag.Id, oldPhase, dataBag.CurrentPhase, service.Name);
                    GetQueue(dataBag.CurrentPhase).Send(msg, dataBag.Label);
                }
            }
            else _webTracer.Send(dataBag.socketToken, "Conversion to DataBag Object failed!");
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
            timer.Elapsed += (sender, e) => Timer_Elapsed(sender, e, msg); // #todo lookup what exctly this syntax means
            timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e, System.Messaging.Message msg)
        {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Enabled = false;
            DataBag msgObj = msg.Body as DataBag;
            GetQueue(msgObj.CurrentPhase).Send(msg, msgObj.Label);
        }

         public class EventWithQueue : EventArgs
        {
            public MSMQWrapper queue{ get; set; }
            public EventWithQueue(object q)
            {
                queue = q as MSMQWrapper;

            }
        }
    }

}
