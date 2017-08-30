using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLogWrapper;
using Fleck;
using System.Configuration;

namespace WebEntryPoint.WebSockets
{

    public class SocketServer : ISocketServer
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(SocketServer), Helpers.ConfigSettings.LogLevel());
        private static readonly NLogWrapper.ILogger _fleckLogger = LogManager.CreateLogger(typeof(FleckLog), Helpers.ConfigSettings.LogLevel());

        private WebSocketServer _socketServer;
        private List<IWebSocketConnection> _listenersWithOpenConnections;
        private string[] _listeningHostnamesList;
        private Dictionary<string, DateTime> _tokensCheckedIn;

        string _url = string.Empty;

        public SocketServer(string url)
        {
            _url = url;
            _listenersWithOpenConnections = new List<IWebSocketConnection>();
            _tokensCheckedIn = new Dictionary<string,DateTime>();
        }

        public void CheckinToken(string accessToken)
        {
            GroomCheckedTokens();
            if (!_tokensCheckedIn.ContainsKey(accessToken))
            {
                _logger.Debug("Registering new Token! {0}", accessToken);
                _tokensCheckedIn[accessToken] = DateTime.UtcNow.Date;
            }
        }

        private void GroomCheckedTokens()
        {
            // only serves to keep the list in decent size

            List<string> key2Remove = new List<string>();
            foreach (var pair in _tokensCheckedIn)
            {
                if (pair.Value != DateTime.UtcNow.Date)
                    key2Remove.Add(pair.Key);
            }

            key2Remove.ForEach(x => _tokensCheckedIn.Remove(x));
        }

        public void Start()
        {
            _logger.Info("Starting on ip:port {0}", _url);
            _socketServer = new WebSocketServer(_url);
            _listeningHostnamesList = Helpers.ConfigSettings.AllowedSocketListenerCsv().Split(',');
            _logger.Info("Listening hostnames found in '{0}'", Helpers.ConfigSettings.AllowedSocketListenerCsvKey);
            foreach (var hostname in _listeningHostnamesList)
            {
                _logger.Info("-{0}", hostname);
            }
            
            if (_url.Contains("wss"))
            {
                _logger.Info("Server loading certificate from the store");
                _socketServer.Certificate = Helpers.Security.GetCertificateFromStore(Helpers.ConfigSettings.Hostname());
            }
            _socketServer.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    if (Checkedin(socket.ConnectionInfo))
                    {
                        _logger.Trace("Socket Opened by Client: {0}", socket.ConnectionInfo.Origin);
                        if (IsListeningSocket(socket))
                        {
                            _listenersWithOpenConnections.Add(socket);
                            _logger.Debug("Client '{0}' added to sendlist", socket.ConnectionInfo.Origin);
                        }
                    }
                    else
                    {
                        // connections without a checkedin token are not allowed
                        socket.Close();
                    }
                };

                socket.OnClose = () =>
                {
                    _logger.Trace("Socket CLOSED by Client originating from: '{0}'", socket.ConnectionInfo.Origin);
                    _listenersWithOpenConnections.Remove(socket);
                };

                socket.OnMessage = message =>
                {
                    _logger.Trace("Message: '{0}'", message);
                    _logger.Debug("SendList: '{0}'", Helpers.ConfigSettings.AllowedSocketListenerCsv());
                    if (!_listenersWithOpenConnections.Any())
                    {
                        if (!InternalRequest(socket.ConnectionInfo.Host))
                            _logger.Warn("We have a message to send, but nobody to send it to!", socket.ConnectionInfo.Host);
                    }
                    else
                    {
                        _listenersWithOpenConnections.ForEach(s => s.Send(message));
                    }
                };
            });
        }

        private bool Checkedin(IWebSocketConnectionInfo ConnectionInfo)
        {
            // strange header, but using Sec-WebSocket-Protocol seems the only option to set header from javascript
            bool result = false;
            if (ConnectionInfo.Headers.ContainsKey("Sec-WebSocket-Protocol"))
            {
                var token = ConnectionInfo.Headers["Sec-WebSocket-Protocol"];
                result = _tokensCheckedIn.ContainsKey(token) && _tokensCheckedIn[token] == DateTime.UtcNow.Date;
            }
            return result;
        }

        private bool InternalRequest(string hostName)
        {
            return hostName != null && hostName.Contains(Helpers.ConfigSettings.Hostname());
        }

        private bool IsListeningSocket(IWebSocketConnection socket)
        {
            foreach (var hostname in _listeningHostnamesList)
            {
                if (socket.ConnectionInfo.Origin != null && socket.ConnectionInfo.Origin.ToLower().Contains(hostname.ToLower().Trim()))
                    return true;
            }
            return false;
        }

        public void WireFleckLogging()
        {
            FleckLog.Level = LogLevel.Error;

            FleckLog.LogAction = (level, message, ex) =>
            {
                switch (level)
                {
                    case LogLevel.Debug:
                        _fleckLogger.Debug(message);
                        if (ex != null)
                            _fleckLogger.Debug(ex.Message);
                        break;
                    case LogLevel.Error:
                        _fleckLogger.Error(message);
                        if (ex != null)
                            _fleckLogger.Error(ex.Message);
                        break;
                    case LogLevel.Warn:
                        _fleckLogger.Warn(message);
                        if (ex != null)
                            _fleckLogger.Warn(ex.Message);
                        break;
                    default:
                        _fleckLogger.Info(message);
                        if (ex != null)
                            _fleckLogger.Info(ex.Message);
                        break;
                }
            };
        }
   }
}
