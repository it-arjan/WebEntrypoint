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
        private ClientWebSocket wsClient;
        private ILogger _logger = LogManager.CreateLogger(typeof(SocketClient));
        public SocketClient()
        {
            
        }
        public void Connect(string url)
        {
            _logger.Debug("Connecting to {0}", url);
            wsClient = new ClientWebSocket();
            
            if (Helpers.Appsettings.Ssl())
            {
                _logger.Debug("Loading certificate from store");
                wsClient.Options.ClientCertificates.Add(Helpers.Security.GetCertificateFromStore("local.entrypoint"));
                wsClient.Options.ClientCertificates.Add(Helpers.Security.GetCertificateFromStore("local.frontend"));
            }
            var tokSrc = new CancellationTokenSource();
            //change to await
            var task = wsClient.ConnectAsync(new Uri(url), tokSrc.Token);
            task.Wait(); task.Dispose();

            _logger.Debug("Opened WebSocket to {0}", url);
            _logger.Debug("SubProtocol: {0}", wsClient.SubProtocol ?? "-none-");
            tokSrc.Dispose();
        }


        public async Task Send(string sessionToken, string msg, params object[] msgPars)
        {
            var tokSrc = new CancellationTokenSource();

            string total_msg = string.Format("{0}#-_-_-#-Remote QueueManger: {1}", sessionToken, string.Format(msg, msgPars));
            await wsClient.SendAsync(
                           new ArraySegment<byte>(Encoding.UTF8.GetBytes(total_msg)),
                                               WebSocketMessageType.Text,
                                               true,
                                               tokSrc.Token
                                               );
            tokSrc.Dispose();
        }

        public async void Close()
        {
            if (wsClient.State == WebSocketState.Open)
            {
                var tokSrc = new CancellationTokenSource();
                await wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "", tokSrc.Token);
                tokSrc.Dispose();
            }
            _logger.Debug("WebSocket CLOSED");
        }
    }
}