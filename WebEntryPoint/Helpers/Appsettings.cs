using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using WebEntryPoint.ServiceCall;

namespace WebEntryPoint.Helpers
{
    public enum QServiceConfig
    {
        None = 0, Service1, Service2, Service3, Service4, Service5, Service6, Service7, Enum_End
    }
    static public class Appsettings
    {
        public const string SiliconClientIdKey = "SiliconClientId";
        public const string SiliconClientSecretKey = "SiliconClientSecret";
        public const string SchemeKey = "scheme";
        public const string HostnameKey = "hostname";
        public const string PortKey = "port";
        public const string AuthServerKey = "authserver";

        public const string AllowedSocketListenerCsvKey = "websocket.listeners.csv";
        public const string SocketPortKey = "websocket.port";
        public const string SocketSchemeKey = "websocket.scheme";

        public const string EntryQueueKey = "entryQueue";
        public const string Service1QueueKey = "service1Queue";
        public const string Service2QueueKey = "service2Queue";
        public const string Service3QueueKey = "service3Queue";
        public const string ExitQueueKey = "exitQueue";
        public const string CmdQueueKey = "commandQueue";
        public const string CmdReplyQueueKey = "commandReplyQueue";

        public const string serviceXTypeKey = "service@-@.type";
        public const string serviceXNameKey = "service@-@.name";
        public const string serviceXHostnameKey = "service@-@.hostname.fullpath";
        public const string serviceXScopeKey = "service@-@.auth.scope";
        public const string LogLevelKey = "log.level";

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

        public static string AllowedSocketListenerCsv()
        {
            return ConfigurationManager.AppSettings.Get(AllowedSocketListenerCsvKey);
        }

        public static string LogLevel()
        {
            return ConfigurationManager.AppSettings.Get(LogLevelKey);
        }

        public static string SiliconClientId()
        {
            return ConfigurationManager.AppSettings.Get(SiliconClientIdKey);
        }

        public static string SiliconClientSecret()
        {
            return ConfigurationManager.AppSettings.Get(SiliconClientSecretKey);
        }

        public static string EntryQueue()
        {
            return ConfigurationManager.AppSettings.Get(EntryQueueKey);
        }
        public static string Service1Queue()
        {
            return ConfigurationManager.AppSettings.Get(Service1QueueKey);
        }
        public static string Service2Queue()
        {
            return ConfigurationManager.AppSettings.Get(Service2QueueKey);
        }
        public static string Service3Queue()
        {
            return ConfigurationManager.AppSettings.Get(Service3QueueKey);
        }
        public static string ExitQueue()
        {
            return ConfigurationManager.AppSettings.Get(ExitQueueKey);
        }

        public static string CmdQueue()
        {
            return ConfigurationManager.AppSettings.Get(CmdQueueKey);
        }
        public static string CmdReplyQueue()
        {
            return ConfigurationManager.AppSettings.Get(CmdReplyQueueKey);
        }

        public static string ReplaceInSettingKey(QServiceConfig phase, string replaceKey)
        {
            return replaceKey.Replace("@-@", ((int)phase).ToString());
        }

        public static string ServiceX_Type(QServiceConfig phase)
        {
            var settingKey = ReplaceInSettingKey(phase, serviceXTypeKey);
            var setting = ConfigurationManager.AppSettings.Get(settingKey);

            return setting;
        }
        public static string ServiceX_Name(QServiceConfig phase)
        {
            var settingKey = ReplaceInSettingKey(phase, serviceXNameKey);
            var setting = ConfigurationManager.AppSettings.Get(settingKey);

            return setting;
        }
        public static string ServiceX_Url(QServiceConfig phase)
        {
            var settingKey = ReplaceInSettingKey(phase, serviceXHostnameKey);
            var setting = ConfigurationManager.AppSettings.Get(settingKey);

            return setting?.ToLower() != "fake" ? string.Format("{0}://{1}/", Scheme(), setting) : "fake";
        }

        public static string ServiceX_Scope(QServiceConfig phase)
        {
            var settingKey = ReplaceInSettingKey(phase, serviceXScopeKey);
            var setting = ConfigurationManager.AppSettings.Get(settingKey);

            return setting;
        }
    }
}
