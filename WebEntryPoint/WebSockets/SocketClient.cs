using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLogWrapper;
using System.Threading;
using System.Net.WebSockets;

namespace WebEntryPoint.WebSockets
{
    public class SocketClient
    {
        private ClientWebSocket _wsClient;

        object _serializer = new object();
        private string _url;
        static ILogger _logger = LogManager.CreateLogger(typeof(SocketClient), Helpers.Appsettings.LogLevel());

        public SocketClient(string serverUrl)
        {
            _url = serverUrl;
            _logger.Info("Connecting to socket server '{0}'", _url);
            this.Connect(_url);
        }

        public void Send(string sessionToken, string msg, params object[] msgPars)
        {
            try
            {
                if (!this.Connected())
                {
                    _logger.Warn("SocketClient seems disconnected, reconnecting....");
                    this.Connect(_url);
                }

                lock (_serializer)
                {
                    var tokSrc = new CancellationTokenSource();

                    string total_msg = string.Format("{0}#-_-_-#-Queue Manager: {1}", sessionToken, string.Format(msg, msgPars));
                    var tsk = _wsClient.SendAsync(
                                   new ArraySegment<byte>(Encoding.UTF8.GetBytes(total_msg)),
                                                       WebSocketMessageType.Text,
                                                       true,
                                                       tokSrc.Token
                                                       );
                    tsk.Wait(); tsk.Dispose();
                    tokSrc.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error when sending msg '{2}' to socket on server {0}. Msg: {1}", _url, ex.Message, msg);
            }
        }
        public void Connect(string url)
        {
            lock (_serializer)
            { 
                if (!Connected())
                {
                    _logger.Info("Connecting to {0}", url);
                    _wsClient = new ClientWebSocket();

                    if (Helpers.Appsettings.Ssl())
                    {
                        _logger.Info("Loading certificate from store");
                        _wsClient.Options.ClientCertificates.Add(Helpers.Security.GetCertificateFromStore(Helpers.Appsettings.Hostname()));
                    }
                    var tokSrc = new CancellationTokenSource();
                    // cannot use await within lock, failr enough
                    var task = _wsClient.ConnectAsync(new Uri(url), tokSrc.Token);
                    task.Wait(); task.Dispose();

                    _logger.Info("Opened ClientWebSocket to {0}", url);
                    tokSrc.Dispose();
                }
            }
        }

        public void Close()
        {
            lock (_serializer)
            {
                if (_wsClient.State == WebSocketState.Open)
                {
                    var tokSrc = new CancellationTokenSource();
                    var tsk = _wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "", tokSrc.Token);
                    tsk.Wait(); tsk.Dispose();
                    tokSrc.Dispose();
                }
                _logger.Debug("ClientWebSocket CLOSED");
            }
        }

        public bool Connected()
        {
            return _wsClient != null && _wsClient.State == WebSocketState.Open;
        }
    }
}
