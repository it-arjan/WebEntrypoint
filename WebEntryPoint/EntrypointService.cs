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
        private static readonly NLogWrapper.ILogger _logger = LogManager.CreateLogger(typeof(EntrypointService), ConfigSettings.LogLevel());

        private Thread qmThread;

        public EntrypointService()
        {
            InitializeComponent();

            _logger.Debug("CurrentDirectory=" + AppDomain.CurrentDomain.BaseDirectory);
           
            CheckHealth();
        }

        private void CheckHealth()
        {
            _logger.Info("Checking config settings..");
            SettingsChecker.CheckPresenceAllPlainSettings(typeof( ConfigSettings));

            var serviceNr = QServiceConfig.Service1;
            while (serviceNr != QServiceConfig.Enum_End)
            {
                SettingsChecker.CheckPresenceAllSettingsForThisEnumval(typeof(ConfigSettings),typeof(QServiceConfig), serviceNr);
                serviceNr++;
            }

            _logger.Info("config settings seem ok..");
            _logger.Info("Url = {0}", ConfigSettings.HostUrl());
            _logger.Info("Socket server Url = {0}", ConfigSettings.SocketServerUrl());
            _logger.Info("Auth server Url= {0}", ConfigSettings.AuthUrl());

            _logger.Info("Configured Services:");
            serviceNr = QServiceConfig.Service1;
            while (serviceNr != QServiceConfig.Enum_End)
            {
                _logger.Info("========= Url: {0} Scope: {1}", ConfigSettings.ServiceX_Url(serviceNr), ConfigSettings.ServiceX_Scope(serviceNr));
                serviceNr++;
            }

            _logger.Debug("..done with config checks");
        }

        protected override void OnStart(string[] args)
        {
            _logger.Info("Starting OWIN http Server..");

            var url = ConfigSettings.HostUrl();
            _httpServer = WebApp.Start<HttpHost>(url);

            _logger.Info("Starting socket server..");

             _logger.Info("Starting queuemanager..");
            queueManager = new QueueManager2(
                ConfigSettings.EntryQueue(),
                ConfigSettings.Service1Queue(), ConfigSettings.Service2Queue(), ConfigSettings.Service3Queue(),
                ConfigSettings.ExitQueue(), ConfigSettings.CmdQueue(), ConfigSettings.CmdReplyQueue(),
                ConfigSettings.CheckinTokenQueue(),
                new WebserviceFactory(), new TokenCache(),
                new SocketServer(ConfigSettings.SocketServerUrl().Replace(ConfigSettings.Hostname(), "0.0.0.0")),
                new SocketClient(Helpers.ConfigSettings.SocketServerUrl())
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
