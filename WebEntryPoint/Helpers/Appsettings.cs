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
        public static bool Ssl()
        {
            return Scheme() == "https";
        }

        public static string HostUrl()
        {
            return string.Format("{0}://{1}:{2}/", Scheme(), Hostname(), Port());
        }

        public static string Scheme()
        {
            return ConfigurationManager.AppSettings.Get(SchemeKey());
        }
        public static string Port()
        {
            return ConfigurationManager.AppSettings.Get(PortKey());
        }
        public static string Hostname()
        {
            return ConfigurationManager.AppSettings.Get(HostnameKey());
        }
        
        public static string SocketServerUrlKey()
        {
            return "scheme";
        }
        public static string SchemeKey()
        {
            return "scheme";
        }
        public static string HostnameKey()
        {
            return "hostname";
        }
        public static string PortKey()
        {
            return "port";
        }
        public static string SocketPortKey()
        {
            return "websocket.port";
        }
        public static string AuthUrlKey()
        {
            return "authserver";
        }
        public static string SocketSchemeKey()
        {
            return "websocket.scheme";
        }
 
        public static string SocketServerUrl()
        {
            return string.Format("{0}://{1}:{2}/", SocketScheme(), Hostname(), SocketPort());
        }

        private static string SocketPort()
        {
            var key = SocketPortKey();
            return ConfigurationManager.AppSettings.Get(key);
        }

        public static string SocketScheme()
        {
            var key = SocketSchemeKey();
            return ConfigurationManager.AppSettings.Get(key);
        }
    
        public static string AuthUrl()
        {
            var key = AuthUrlKey();
            return ConfigurationManager.AppSettings.Get(key);
        }

        public static string SocketServerListenUrls()
        {
            return ConfigurationManager.AppSettings.Get("websocket.listeners.csv");
        }

        public static string SiliconClientId()
        {
            return ConfigurationManager.AppSettings.Get("SiliconClientId");
        }

        public static string SiliconClientSecret()
        {
            return ConfigurationManager.AppSettings.Get("SiliconClientSecret");
        }
    }
}
