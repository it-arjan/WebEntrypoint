using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using Microsoft.Owin.Hosting;
using System.Net.Http;
using System.Configuration;
using NLogWrapper;

using System.Threading.Tasks;
using WebEntryPoint.MQ;
using System.Threading;


namespace WebEntryPoint
{
    public partial class EntrypointService : ServiceBase
    { 
        private IDisposable _httpServer;
        private QueueManager2 queueManager;

        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(EntrypointService));

        private string _entryQueue;
        private string _service1Queue;
        private string _service2Queue;
        private string _service3Queue;
        private string _exitQueue;

        private WebSockets.SocketServer _socketServer;

        public EntrypointService()
        {
            InitializeComponent();

            _logger.Info("CurrentDirectory=" + AppDomain.CurrentDomain.BaseDirectory);
            CheckHealth();
        }

        private void CheckHealth()
        {
            _logger.Debug("Checking config settings..");
            _entryQueue = ConfigurationManager.AppSettings.Get("entryQueue");
            _service1Queue = ConfigurationManager.AppSettings.Get("service1Queue");
            _service2Queue = ConfigurationManager.AppSettings.Get("service2Queue");
            _service3Queue = ConfigurationManager.AppSettings.Get("service3Queue");
            _exitQueue = ConfigurationManager.AppSettings.Get("exitQueue");

            if (_entryQueue == null ||
                _service1Queue == null ||
                _service2Queue == null ||
                _service3Queue == null ||
                _exitQueue == null) throw new Exception("one or more service queues are not defined in app.config");

            if (Helpers.Appsettings.SiliconClientId() == null) throw new Exception("setting 'SiliconClientId' is not present in app.config");
            if (Helpers.Appsettings.SiliconClientSecret() == null) throw new Exception("setting 'SiliconClientSecret' is not present in app.config");

            if (Helpers.Appsettings.Hostname() == null) throw new Exception(Helpers.Appsettings.HostnameKey + " is not present in app.config");
            if (Helpers.Appsettings.Port() == null) throw new Exception(Helpers.Appsettings.PortKey + " is not present in app.config");
            if (Helpers.Appsettings.Scheme() == null) throw new Exception(Helpers.Appsettings.SchemeKey + " is not present in app.config");
            if (Helpers.Appsettings.AuthServer() == null) throw new Exception(Helpers.Appsettings.AuthServerKey + " is not present in app.config");

            if (Helpers.Appsettings.SocketServerListenUrls() == null) throw new Exception("Websocket.Listeners not defined in app.config");

            _logger.Debug("config setting seem ok..");
            _logger.Debug("Url = {0}", Helpers.Appsettings.HostUrl());
            _logger.Debug("Socket server Url = {0}", Helpers.Appsettings.SocketServerUrl());
            _logger.Debug("Auth server Url= {0}", Helpers.Appsettings.AuthUrl());
            _logger.Debug("..done with config checks");
        }

        protected override void OnStart(string[] args)
        {
            _logger.Info("Starting OWIN http Server..");

            var url = Helpers.Appsettings.HostUrl();
            _httpServer = WebApp.Start<HttpHost>(url);
            _logger.Info("Listening on {0}", url);

            _logger.Info("Starting socket server.");
            _socketServer = new WebSockets.SocketServer();
            //_socketServer.WireFleckLogging();
            _socketServer.Start(Helpers.Appsettings.SocketServerUrl().Replace(Helpers.Appsettings.Hostname(), "0.0.0.0"));

            _logger.Info("Starting queuemanager in seprate thread.");
             queueManager = new QueueManager2(_entryQueue, _service1Queue, _service2Queue, _service3Queue, _exitQueue);
            Thread t = new Thread(queueManager.StartListening);
            t.Start();  
            _logger.Info("Queuemanager started.");

        }

        protected override void OnStop()
        {
            //_httpServer.Dispose(); //throws exception: being disposed
            queueManager.StopAll();
            queueManager = null;
        }
    }
}
