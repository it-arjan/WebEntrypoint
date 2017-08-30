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
    public class SocketClient : ISocketClient
    {
        private ClientWebSocket _wsClient;

        object _serializer = new object();
        private string _url;
        static ILogger _logger = LogManager.CreateLogger(typeof(SocketClient), Helpers.ConfigSettings.LogLevel());

        public SocketClient(string serverUrl)
        {
            _url = serverUrl;
            _logger.Info("Connecting to socket server '{0}'", _url);
        }

        public void Send(string accessToken, string feedId, string msg)
        {
            try
            {
                if (!this.Connected())
                {
                    _logger.Debug("Connecting to socket server....");
                    this.Connect(_url, accessToken);
                }

                lock (_serializer)
                {
                    var tokSrc = new CancellationTokenSource();

                    string total_msg = string.Format("{0}#-_-_-#-Queue Manager: {1}", feedId, msg);
                    var tsk = _wsClient.SendAsync(
                                   new ArraySegment<byte>(Encoding.UTF8.GetBytes(total_msg)),
                                                       WebSocketMessageType.Text,
                                                       true,
                                                       tokSrc.Token
                                                       );
                    if (!tsk.IsFaulted) tsk.Wait();

                    tsk.Dispose();
                    tokSrc.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error when sending msg '{2}' to socket on server {0}. Msg: {1}", _url, ex.Message, msg);
            }
        }

        private void Connect(string url, string accessToken)
        {
            lock (_serializer)
            { 
                if (!this.Connected())
                {
                    _logger.Info("Connecting to {0}", url);
                    _wsClient = new ClientWebSocket();
                    _wsClient.Options.SetRequestHeader("Sec-WebSocket-Protocol", accessToken);

                    var tokSrc = new CancellationTokenSource();
                    var task = _wsClient.ConnectAsync(new Uri(url), tokSrc.Token);
                    if (!task.IsFaulted) task.Wait();
                    task.Dispose();

                    _logger.Info("Opened ClientWebSocket to {0}", url);
                    tokSrc.Dispose();
                }
            }
        }

        private void Close()
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

        private bool Connected()
        {
            return _wsClient != null && _wsClient.State == WebSocketState.Open;
        }
    }
}
