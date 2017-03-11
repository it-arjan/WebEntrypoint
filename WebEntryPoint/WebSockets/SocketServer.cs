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
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(SocketServer), Helpers.Appsettings.LogLevel());
        private static readonly NLogWrapper.ILogger _fleckLogger = LogManager.CreateLogger(typeof(FleckLog), Helpers.Appsettings.LogLevel());
        private WebSocketServer _socketServer;
        private List<IWebSocketConnection> LegalSocketList;
        private string[] _listenList;

        public SocketServer()
        {
            LegalSocketList = new List<IWebSocketConnection>();
        }

        public void Start(string url)
        {
            _logger.Info("Starting on ip:port {0}", url);
            _socketServer = new WebSocketServer(url);
            _listenList = Helpers.Appsettings.AllowedSocketListenerCsv().Split(',');
            _logger.Info("Listening hostnames found in '{0}'", Helpers.Appsettings.AllowedSocketListenerCsvKey);
            foreach (var hostname in _listenList)
            {
                _logger.Info("-{0}", hostname);
            }
            
            if (url.Contains("wss"))
            {
                _logger.Info("Server loading certificate from the store");
                _socketServer.Certificate = Helpers.Security.GetCertificateFromStore(Helpers.Appsettings.Hostname());
            }
            _socketServer.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    _logger.Trace("Socket Opened by Client: {0}", socket.ConnectionInfo.Origin);
                    if (IsListeningSocket(socket))
                    {
                        LegalSocketList.Add(socket); 
                        _logger.Debug("Client '{0}' added to sendlist", socket.ConnectionInfo.Origin);
                    }
                    else
                    {
                        _logger.Debug("Client '{0}' *not* added to sendlist", socket.ConnectionInfo.Origin);
                    }
                };

                socket.OnClose = () =>
                {
                    _logger.Trace("Socket CLOSED by Client originating from: '{0}'", socket.ConnectionInfo.Origin);
                    if (IsListeningSocket(socket))
                    {
                        LegalSocketList.Remove(socket);
                    }
                };

                socket.OnMessage = message =>
                {
                    _logger.Trace("Message: '{0}'", message);
                    _logger.Debug("SendList: '{0}'", Helpers.Appsettings.AllowedSocketListenerCsv());
                    if (!LegalSocketList.Any() 
                        && socket.ConnectionInfo.Host != null
                        && !socket.ConnectionInfo.Host.Contains(Helpers.Appsettings.Hostname())
                        )
                    {
                        _logger.Warn("Request from {0} not allowed in web.config", socket.ConnectionInfo.Host);
                    }
                    LegalSocketList.ForEach(s => s.Send(message));
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
        private bool IsListeningSocket(IWebSocketConnection socket)
        {
            foreach (var hostname in _listenList)
            {
                if (socket.ConnectionInfo.Origin != null && socket.ConnectionInfo.Origin.ToLower().Contains(hostname.ToLower().Trim()))
                    return true;
            }
            return false;
        }
    }
}
