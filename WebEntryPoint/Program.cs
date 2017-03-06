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
            var stacktrace = ex.StackTrace;
            var site = ex.TargetSite;
            string msg = ex.Message;
            while (ex.InnerException != null)
            {
                msg = string.Format("{0}\n{1}", msg, ex.InnerException.Message);
                ex = ex.InnerException;
                stacktrace += ex.StackTrace;
            }
            _logger.Error("FirstChanceException event raised in {0}: {1}\n site: {2}\n Stack: {3}", AppDomain.CurrentDomain.FriendlyName, msg, site, stacktrace);
        }
    }
}
