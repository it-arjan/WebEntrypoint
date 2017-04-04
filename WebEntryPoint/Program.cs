using System;
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
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new EntrypointService()
            };
            ServiceBase.Run(ServicesToRun);
        }

        private static void LogException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            Exception ex = e.Exception;

            _logger.Error("FirstChanceException event raised in {0}: \n \t\t{1}", AppDomain.CurrentDomain.FriendlyName, ex.ToString());
        }
    }
}
