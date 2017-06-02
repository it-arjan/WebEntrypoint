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
using WebEntryPoint.Helpers;
using WebEntryPoint.ServiceCall;
using WebEntryPoint.WebSockets;

namespace WebEntryPoint
{
    public partial class EntrypointService : ServiceBase
    { 
        private IDisposable _httpServer;
        private QueueManager2 queueManager;

        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(EntrypointService), Appsettings.LogLevel());

        private Thread qmThread;
        private WebSockets.ISocketServer _socketServer;

        public EntrypointService()
        {
            InitializeComponent();

            _logger.Debug("CurrentDirectory=" + AppDomain.CurrentDomain.BaseDirectory);
           
            CheckHealth();
        }

        private void CheckHealth()
        {
            _logger.Info("Checking config settings..");

            var missingQueues = string.Empty;
            // Option: See if all appsetting-checks can be generated using reflection
            if (Appsettings.EntryQueue() == null) missingQueues += Appsettings.EntryQueueKey + ", ";
            if (Appsettings.Service1Queue() == null) missingQueues += Appsettings.Service1QueueKey + ", ";
            if (Appsettings.Service2Queue() == null) missingQueues += Appsettings.Service2QueueKey + ", ";
            if (Appsettings.Service3Queue() == null) missingQueues += Appsettings.Service3QueueKey + ", ";
            if (Appsettings.ExitQueue() == null) missingQueues += ", " + Appsettings.ExitQueueKey + ", ";
            if (Appsettings.CmdQueue() == null) missingQueues += ", " + Appsettings.CmdQueueKey;
            if (Appsettings.CmdReplyQueue() == null) missingQueues += ", " + Appsettings.CmdReplyQueueKey;

            if (missingQueues != string.Empty) throw new Exception("The following queue-settings are not defined in app.config: " + missingQueues);

            if (Appsettings.SiliconClientId() == null) throw new Exception("setting 'SiliconClientId' is not present in app.config");
            if (Appsettings.SiliconClientSecret() == null) throw new Exception("setting 'SiliconClientSecret' is not present in app.config");

            if (Appsettings.Hostname() == null) throw new Exception(Appsettings.HostnameKey + " is not present in app.config");
            if (Appsettings.Port() == null) throw new Exception(Appsettings.PortKey + " is not present in app.config");
            if (Appsettings.Scheme() == null) throw new Exception(Appsettings.SchemeKey + " is not present in app.config");
            if (Appsettings.AuthServer() == null) throw new Exception(Appsettings.AuthServerKey + " is not present in app.config");

            if (Appsettings.AllowedSocketListenerCsv() == null) throw new Exception("Websocket.Listeners not defined in app.config");

            var serviceNr = QServiceConfig.Service1;
            while (serviceNr != QServiceConfig.Enum_End)
            {
                var key = Appsettings.ReplaceInSettingKey(serviceNr, Appsettings.serviceXHostnameKey);
                if (ConfigurationManager.AppSettings.Get(key) == null) throw new Exception("Key not present:" + key);
                else _logger.Debug("{0} has url {1}", serviceNr, Appsettings.ServiceX_Url(serviceNr));
                key = Appsettings.ReplaceInSettingKey(serviceNr, Appsettings.serviceXScopeKey);
                if (ConfigurationManager.AppSettings.Get(key) == null) throw new Exception("Key not present:" + key);
                else _logger.Debug("{0} has scope {1}", serviceNr, Appsettings.ServiceX_Scope(serviceNr));
                serviceNr++;
            }

            _logger.Info("config settings seem ok..");
            _logger.Info("Url = {0}", Appsettings.HostUrl());
            _logger.Info("Socket server Url = {0}", Appsettings.SocketServerUrl());
            _logger.Info("Auth server Url= {0}", Appsettings.AuthUrl());

            _logger.Info("Configured Services:");
            serviceNr = QServiceConfig.Service1;
            while (serviceNr != QServiceConfig.Enum_End)
            {
                _logger.Info("========= Url: {0} Scope: {1}", Appsettings.ServiceX_Url(serviceNr), Appsettings.ServiceX_Scope(serviceNr));
                serviceNr++;
            }

            _logger.Debug("..done with config checks");
        }

        protected override void OnStart(string[] args)
        {
            _logger.Info("Starting OWIN http Server..");

            var url = Appsettings.HostUrl();
            _httpServer = WebApp.Start<HttpHost>(url);

            _logger.Info("Starting socket server..");
            _socketServer = new WebSockets.SocketServer();
            //_socketServer.WireFleckLogging();
            _socketServer.Start(Appsettings.SocketServerUrl().Replace(Appsettings.Hostname(), "0.0.0.0"));

            _logger.Info("Starting queuemanager..");
            queueManager = new QueueManager2(
                Appsettings.EntryQueue(),
                Appsettings.Service1Queue(), Appsettings.Service2Queue(), Appsettings.Service3Queue(),
                Appsettings.ExitQueue(), Appsettings.CmdQueue(), Appsettings.CmdReplyQueue(),
                new WebserviceFactory(), new TokenManager(), new SocketClient(Helpers.Appsettings.SocketServerUrl())
                 );
            qmThread = new Thread(queueManager.StartListening);

            qmThread.Start();  
            _logger.Info("Startup completed.");

        }

        protected override void OnStop()
        {
            //_httpServer.Dispose(); //throws exception: being disposed
            queueManager.StopAll();
            queueManager = null;
        }
    }
}
