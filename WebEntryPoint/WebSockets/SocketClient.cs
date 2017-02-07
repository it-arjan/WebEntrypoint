using System;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using NLogWrapper;

namespace WebEntryPoint.WebSockets
{
    class SocketClient
    {
        private ClientWebSocket _wsClient;
        private ILogger _logger = LogManager.CreateLogger(typeof(SocketClient));
        object _serializer = new object();
        public SocketClient()
        {

        }

        public void Connect(string url)
        {
            lock (_serializer)
            {
                if (!Connected())
                {
                    _logger.Debug("Connecting to {0}", url);
                    _wsClient = new ClientWebSocket();

                    if (Helpers.Appsettings.Ssl())
                    {
                        _logger.Debug("Loading certificate from store");
                        _wsClient.Options.ClientCertificates.Add(Helpers.Security.GetCertificateFromStore("local.entrypoint"));
                        _wsClient.Options.ClientCertificates.Add(Helpers.Security.GetCertificateFromStore("local.frontend"));
                    }
                    var tokSrc = new CancellationTokenSource();
                    //cannot use await within lock
                    var task = _wsClient.ConnectAsync(new Uri(url), tokSrc.Token);
                    task.Wait(); task.Dispose();

                    _logger.Debug("Opened ClientWebSocket to {0}", url);
                    _logger.Debug("SubProtocol: {0}", _wsClient.SubProtocol ?? "-none-");
                    tokSrc.Dispose();
                }
            }
        }

        public void Send(string sessionToken, string msg, params object[] msgPars)
        {
            lock (_serializer)
            {
                var tokSrc = new CancellationTokenSource();

                string total_msg = string.Format("{0}#-_-_-#-Remote QueueManger: {1}", sessionToken, string.Format(msg, msgPars));
                var tsk=_wsClient.SendAsync(
                               new ArraySegment<byte>(Encoding.UTF8.GetBytes(total_msg)),
                                                   WebSocketMessageType.Text,
                                                   true,
                                                   tokSrc.Token
                                                   );
                tsk.Wait();tsk.Dispose();
                tokSrc.Dispose();
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