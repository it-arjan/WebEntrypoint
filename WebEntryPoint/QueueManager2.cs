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

        static ILogger _logger = LogManager.CreateLogger(typeof(QueueManager2));
        private WebTracer _webTracer;
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
            _webTracer = new WebTracer(Helpers.Appsettings.SocketServerUrl());
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
                _service1Q.AddHandler(Service1Handler);

                _service2Q.SetFormatters(typeof(DataBag), typeof(string));
                _service2Q.AddHandler(Service2Handler);

                _service3Q.SetFormatters(typeof(DataBag), typeof(string));
                _service3Q.AddHandler(Service3Handler);

                _exitQ.SetFormatters(typeof(DataBag), typeof(string));
                _exitQ.AddHandler(ExitHandler);

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

            _webTracer.Send(msgObj.socketToken, "Dropped {0} in the Q for service1", msgObj.Id);

            if (!_ticker.IsRunning) _ticker.Start();
            AddCount++;
            // just drop in service1 Q
            _service1Q.Send(msg);
            _entryQ.BeginReceive(); 
        }

        private async void Service1Handler(object sender, ReceiveCompletedEventArgs e)
        {
            System.Messaging.Message msg = _service1Q.Q.EndReceive(e.AsyncResult);
            DataBag msgObj = msg.Body as DataBag;
            _webTracer.Send(msgObj.socketToken, "Service1Handler");

            if (ProcessMsgPerMsg) await ProcessMessageAsync(msg);
            else ProcessMessageAsync(msg);
            _service1Q.BeginReceive(); 
        }

        private async void Service2Handler(object sender, ReceiveCompletedEventArgs e)
        {
            System.Messaging.Message msg = _service2Q.Q.EndReceive(e.AsyncResult);
            DataBag msgObj = msg.Body as DataBag;
            _webTracer.Send(msgObj.socketToken, "Service2Handler");

            if (ProcessMsgPerMsg) await ProcessMessageAsync(msg);
            else ProcessMessageAsync(msg);
            _service2Q.BeginReceive();
        }

        private async void Service3Handler(object sender, ReceiveCompletedEventArgs e)
        {
            System.Messaging.Message msg = _service3Q.Q.EndReceive(e.AsyncResult);
            DataBag msgObj = msg.Body as DataBag;
            _webTracer.Send(msgObj.socketToken, "Service3Handler");

            if (ProcessMsgPerMsg) await ProcessMessageAsync(msg);
            else ProcessMessageAsync(msg);
            _service3Q.BeginReceive();
        }

        private void ExitHandler(object sender, ReceiveCompletedEventArgs e)
        {
            //send done notification to MVC app?
            System.Messaging.Message msg = _exitQ.Q.EndReceive(e.AsyncResult);
            DataBag msgObj = msg.Body as DataBag;

            _logger.Debug("calling postback '{0}'", msgObj.PostBackUrl);

            var token = GetClientToken();
            if (!token.IsError) PostBackUsingEasyHttp(token.AccessToken, msgObj);

            //await PostBackUsingHttpClient(msgObj);
            _exitQ.BeginReceive();
        }

        private void PostBackUsingEasyHttp(string token, DataBag msgObj)
        {
            var eHttp = new EasyHttp.Http.HttpClient();
            var auth_header = string.Format("Bearer {0}", token);

            eHttp.Request.AddExtraHeader("Authorization", auth_header);
            eHttp.Post(msgObj.PostBackUrl, msgObj, HttpContentTypes.ApplicationJson);

            _webTracer.Send(msgObj.socketToken, 
                "Posting back the result of {0} to {1} returned {2}",
                    msgObj.Id, msgObj.PostBackUrl, eHttp.Response.StatusCode);
        }

        static TokenResponse GetClientToken()
        {
            var client = new TokenClient(
                "http://local.identityserver:5001/connect/token",
                "webentrypoint_silicon",
                "F621F470-9731-4A25-80EF-67A6F7C5F4B8");

            var token = client.RequestClientCredentialsAsync("MvcFrontEnd").Result;
            if (token.IsError) _logger.Error("Error geting Token for Client MvcFrontEnd: {0} ", token.Error);

            return token;
        }

        private async Task PostBackUsingHttpClient(DataBag msgObj)
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", msgObj.socketToken);
 
                var myContent = JsonConvert.SerializeObject(msgObj);
                var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
                var byteContent = new ByteArrayContent(buffer);
                var response = await client.PostAsync(msgObj.PostBackUrl, byteContent);

                _logger.Debug("postback returned '{0}'", response.StatusCode);
                _webTracer.Send(msgObj.socketToken, "Posting back the result of {0} to {1} returned {2}",
                                        msgObj.Id, msgObj.PostBackUrl, response.StatusCode);
            }
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
            _service1Q.RemoveHandler(Service1Handler);
            _service2Q.RemoveHandler(Service2Handler);
            _service3Q.RemoveHandler(Service3Handler);
            _exitQ.RemoveHandler(ExitHandler);
            return null;
        }

       private MSMQWrapper GetQueue(ProcessPhase2 phase)
        {
            switch (phase)
            {
                case ProcessPhase2.Entry:
                    return _entryQ;
                case ProcessPhase2.Service1:
                    return _service1Q;
                case ProcessPhase2.Service2:
                    return _service2Q;
                case ProcessPhase2.Service3:
                    return _service3Q;
                case ProcessPhase2.Completed:
                    return _exitQ;
                default:
                    throw new Exception("Unknown process phase");
            }
        }

        private async Task ProcessMessageAsync(System.Messaging.Message msg)
        {
            DataBag msgObj = msg.Body as DataBag;
            if (msgObj != null)
            {
                _webTracer.Send(msgObj.socketToken, "Received {0}, {1}, processing..", msgObj.Status, msgObj.CurrentPhase);
                await SimulateServiceCall(msg); 
            }
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
            timer.Elapsed += (sender, e) => Timer_Elapsed(sender, e, msg); // #todo lookup what exctly this means
            timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e, System.Messaging.Message msg)
        {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Enabled = false;
            DataBag msgObj = msg.Body as DataBag;
            GetQueue(msgObj.CurrentPhase).Send(msg, msgObj.Label);
        }

        public async Task SimulateServiceCall(System.Messaging.Message msg)
        {
            DataBag msgObj = msg.Body as DataBag;
            msgObj.TryCount++;
            var logMsg = string.Format("Simulated (real call: todo) call to {0} returned {1} on attempt ({2}) .", msgObj.CurrentPhase, msgObj.Status, msgObj.TryCount);
            msgObj.AddToContent(logMsg);
            _webTracer.Send(msgObj.socketToken, 
               logMsg
                );
            Random rnd = new Random();
            await Task.Delay(rnd.Next(0, 2000));
            //await Task.Delay(1);
            MsgStatus2 nextStatus = GetRandomStatus(rnd.Next(0, 20) % 10);

            if (nextStatus != MsgStatus2.ReadyFor)
            {
                msgObj.Status = nextStatus;
            }
            else
            {
                msgObj.NextService();
                msgObj.TryCount = 0;
            }

            _webTracer.Send(msgObj.socketToken, "New status is {0}, {1}", msgObj.Status, msgObj.CurrentPhase);

            if (msgObj.CurrentPhase != ProcessPhase2.Completed) msgObj.AddToContent("Ready for {0}.", msgObj.CurrentPhase);
            else
            {
                lock (_processed)
                {
                    DoneCount++;
                    ProcessedList.Add(
                    string.Format("{0}: {1}, {2}", msgObj.Id, msgObj.CurrentPhase, msgObj.Status)
                    );
                }
            }
            if (msgObj.Error)
            {
                int delay = 1;
                _webTracer.Send(msgObj.socketToken, "{1} failed for {0}, Retry in {2} secs", msgObj.Id, msgObj.CurrentPhase, delay);
                if (UseTimedRetry) ReQueueWithTimedDelay(msg, delay);
                else await ReQueueWithTaskDelay(msg, delay);
            }
            else 
            {
                _webTracer.Send(msgObj.socketToken, "Sending {0} to the Queue for {1}", msgObj.Id, msgObj.CurrentPhase);
                GetQueue(msgObj.CurrentPhase).Send(msg, msgObj.Label);
            }
        }

        private MsgStatus2 GetRandomStatus(int randomNr)
        {
            if (randomNr == 0)
            {
                return MsgStatus2.ServiceTempDown;
            }
            else if (randomNr == 1)
            {
                return MsgStatus2.ServiceFailed;
            }
            else
            {
                return MsgStatus2.ReadyFor;
            }
        }

        public class EventWithMesaage : EventArgs
        {
            public System.Messaging.Message Msg { get; set; }
            public EventWithMesaage(object msg)
            {
                Msg = msg as System.Messaging.Message;

            }
        }
    }

}
