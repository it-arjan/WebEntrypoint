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

    public class SocketServer
    {
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(SocketServer));
        private static readonly NLogWrapper.ILogger _fleckLogger = LogManager.CreateLogger(typeof(FleckLog));
        private WebSocketServer _socketServer;
        private List<IWebSocketConnection> _socketServerSendList;

        public SocketServer()
        {
            _socketServerSendList = new List<IWebSocketConnection>();
        }

        public void Start(string url)
        {
            _logger.Debug("Starting on ip:port {0}", url);
            _socketServer = new WebSocketServer(url);
            if (url.Contains("wss"))
            {
                _logger.Info("Server loading certificate from the store");
                _socketServer.Certificate = Helpers.Security.GetCertificateFromStore(Helpers.Appsettings.Hostname());
                _logger.Info("..certificate loaded");
            }
            _socketServer.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    _logger.Info("Socket Opened by Client: {0}", socket.ConnectionInfo.Origin);
                    if (IsListeningSocket(socket))
                    {
                        _socketServerSendList.Add(socket); 
                        _logger.Info("Client '{0}' added to sendlist", socket.ConnectionInfo.Origin);
                    }
                    else
                    {
                        _logger.Info("Client '{0}' *not* added to sendlist", socket.ConnectionInfo.Origin);
                    }
                };

                socket.OnClose = () =>
                {
                    _logger.Info("Socket CLOSED by Client originating from: '{0}'", socket.ConnectionInfo.Origin);
                    if (IsListeningSocket(socket))
                    {
                        _socketServerSendList.Remove(socket);
                    }
                };

                socket.OnMessage = message =>
                {
                    _logger.Info("Sending serverSendList: '{0}'", message);
                    if (!_socketServerSendList.Any())
                    {
                        _logger.Warn("Sendlist is empty, no messages will be sent, check app.config for {0}", Helpers.Appsettings.SocketListenersKey);
                    }
                    _socketServerSendList.ForEach(s => s.Send(message));
                };
            });
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
        private static bool IsListeningSocket(IWebSocketConnection socket)
        {
            var listenList = Helpers.Appsettings.SocketServerListenUrls().Split(',');
            foreach (var url in listenList)
            {
                if (socket.ConnectionInfo.Origin.Contains(url.Trim()))
                    return true;
            }
            return false;
        }
    }
}
