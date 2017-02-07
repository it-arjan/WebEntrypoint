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

            if (ConfigurationManager.AppSettings.Get("facing") == null) throw new Exception("setting 'facing' is not present in app.config");

            if (Helpers.Appsettings.HostUrl() == null) throw new Exception(Helpers.Appsettings.HostKey() + " is not present in app.config");
            if (Helpers.Appsettings.AuthUrl() == null) throw new Exception(Helpers.Appsettings.AuthUrlKey() + " is not present in app.config");
            if (Helpers.Appsettings.SocketServerUrl() == null) throw new Exception(Helpers.Appsettings.SocketServerUrlKey() + " is not present in app.config");

            if (ConfigurationManager.AppSettings["Websocket.ListenUrls"] == null) throw new Exception("Websocket.ListenUrls not defined in app.config");
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
            _socketServer.Start(Helpers.Appsettings.SocketServerUrl().Replace("local.entrypoint", "0.0.0.0"));

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
