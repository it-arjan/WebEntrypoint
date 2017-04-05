﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using NLogWrapper;

namespace WebEntryPoint
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static ILogger _logger = LogManager.CreateLogger(typeof(Program), Helpers.Appsettings.LogLevel());
        static void Main()
        {
            AppDomain.CurrentDomain.FirstChanceException += LogException;
            
            AppDomain.CurrentDomain.UnhandledException += LogException;
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new EntrypointService()
            };
            ServiceBase.Run(ServicesToRun);
        }
        private static void LogException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex != null) Log("UnhandledException", "Errorrr"); 
            else Log("UnhandledException", "Cast of exception object to exceptin FAILED");
        }

        private static void LogException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            Log("FirstChanceException", e.Exception.ToString());
        }

        private static void Log(string type, string exceptionInfo)
        {
            _logger.Error("{0} event raised in {1}: \n \t\t{1}", type, AppDomain.CurrentDomain.FriendlyName, exceptionInfo);
        }
    }
}
