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
        private List<IWebSocketConnection> _sendList;
        private string[] _listeningHostnamesList;

        public SocketServer()
        {
            _sendList = new List<IWebSocketConnection>();
        }

        public void Start(string url)
        {
            _logger.Info("Starting on ip:port {0}", url);
            _socketServer = new WebSocketServer(url);
            _listeningHostnamesList = Helpers.ConfigSettings.AllowedSocketListenerCsv().Split(',');
            _logger.Info("Listening hostnames found in '{0}'", Helpers.ConfigSettings.AllowedSocketListenerCsvKey);
            foreach (var hostname in _listeningHostnamesList)
            {
                _logger.Info("-{0}", hostname);
            }
            
            if (url.Contains("wss"))
            {
                _logger.Info("Server loading certificate from the store");
                _socketServer.Certificate = Helpers.Security.GetCertificateFromStore(Helpers.ConfigSettings.Hostname());
            }
            _socketServer.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    _logger.Trace("Socket Opened by Client: {0}", socket.ConnectionInfo.Origin);
                    if (IsListeningSocket(socket))
                    {
                        _sendList.Add(socket); 
                        _logger.Debug("Client '{0}' added to sendlist", socket.ConnectionInfo.Origin);
                    }
                };

                socket.OnClose = () =>
                {
                    _logger.Trace("Socket CLOSED by Client originating from: '{0}'", socket.ConnectionInfo.Origin);
                    if (IsListeningSocket(socket))
                    {
                        _sendList.Remove(socket);
                    }
                };

                socket.OnMessage = message =>
                {
                    _logger.Trace("Message: '{0}'", message);
                    _logger.Debug("SendList: '{0}'", Helpers.ConfigSettings.AllowedSocketListenerCsv());
                    if (!_sendList.Any())
                    {
                        if (!InternalRequest(socket.ConnectionInfo.Host))
                            _logger.Warn("We have a message to send, but nobody to send it to!", socket.ConnectionInfo.Host);
                    }
                    else
                    {
                        _sendList.ForEach(s => s.Send(message));
                    }
                };
            });
        }

        private bool InternalRequest(string hostName)
        {
            return hostName != null && hostName.Contains(Helpers.ConfigSettings.Hostname());
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
        private bool IsListeningSocket(IWebSocketConnection socket)
        {
            foreach (var hostname in _listeningHostnamesList)
            {
                if (socket.ConnectionInfo.Origin != null && socket.ConnectionInfo.Origin.ToLower().Contains(hostname.ToLower().Trim()))
                    return true;
            }
            return false;
        }
    }
}
