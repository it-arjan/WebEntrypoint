using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace WebEntryPoint.Helpers
{
    static class Appsettings
    {
        public static string GetCertificatePfxPath(string name)
        {
            return ConfigurationManager.AppSettings.Get("Websocket.certificatePfxPath");
        }

        public static bool Ssl()
        {
            var key = HostKey();
            return ConfigurationManager.AppSettings.Get("facing").Contains("https");
        }

        public static string HostUrl() 
        {
            var key = HostKey();
            return ConfigurationManager.AppSettings.Get(key);
        }

        public static string HostKey()
        {
            return string.Format("facing.{0}.hosturl", ConfigurationManager.AppSettings.Get("facing").ToLower());
        }

        public static string SocketServerUrl()
        {
            var key = SocketServerUrlKey();
            return ConfigurationManager.AppSettings.Get(key);
        }

        public static string SocketServerUrlKey()
        {
            return string.Format("facing.{0}.socketserver.url", ConfigurationManager.AppSettings.Get("facing").ToLower());
        }
        public static string SocketServerListenUrls()
        {
            var key = SocketServerUrlKey();
            return ConfigurationManager.AppSettings.Get("Websocket.ListenUrls");
        }
        public static string AuthUrl()
        {
            var key = AuthUrlKey();
            return ConfigurationManager.AppSettings.Get(key);
        }

        public static string AuthUrlKey()
        {
            return string.Format("facing.{0}.authserver", ConfigurationManager.AppSettings.Get("facing").ToLower());
        }
    }
}
