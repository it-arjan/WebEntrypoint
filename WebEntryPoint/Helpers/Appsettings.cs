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
        public const string SocketListenersKey = "websocket.listeners.csv";
        public const string SiliconClientIdKey = "SiliconClientId";
        public const string SiliconClientSecretKey = "SiliconClientSecret";
        public const string SchemeKey = "scheme";
        public const string HostnameKey = "hostname";
        public const string PortKey = "port";
        public const string AuthServerKey = "authserver";
        public const string SocketPortKey = "websocket.port";
        public const string SocketSchemeKey = "websocket.scheme";

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
            return ConfigurationManager.AppSettings.Get(SchemeKey);
        }
        public static string Port()
        {
            return ConfigurationManager.AppSettings.Get(PortKey);
        }
        public static string Hostname()
        {
            return ConfigurationManager.AppSettings.Get(HostnameKey);
        }

        public static string AuthUrl()
        {
            return string.Format("{0}://{1}/", Scheme(), AuthServer());
        }
        public static string SocketServerUrl()
        {
            return string.Format("{0}://{1}:{2}/", SocketScheme(), Hostname(), SocketPort());
        }

        private static string SocketPort()
        {
            return ConfigurationManager.AppSettings.Get(SocketPortKey);
        }

        public static string SocketScheme()
        {
            return ConfigurationManager.AppSettings.Get(SocketSchemeKey);
        }
        public static string AuthServer()
        {
            return ConfigurationManager.AppSettings.Get(AuthServerKey);
        }

        public static string SocketServerListenUrls()
        {
            return ConfigurationManager.AppSettings.Get(SocketListenersKey);
        }

        public static string SiliconClientId()
        {
            return ConfigurationManager.AppSettings.Get(SiliconClientIdKey);
        }

        public static string SiliconClientSecret()
        {
            return ConfigurationManager.AppSettings.Get(SiliconClientSecretKey);
        }
    }
}
