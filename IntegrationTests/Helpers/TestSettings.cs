using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace IntegrationTests.Helpers
{
    public static class TestSettings
    {
        public const string SiliconClientIdKey = "silicon.client.Id";
        public const string SiliconClientSecretKey = "silicon.client.secret";
        public const string FrontendUrlKey = "frontend.url";
        public const string EntrypointUrlKey = "entrypoint.url";
        public const string AuthUrlKey = "auth.url";

        public static string SiliconClientId()
        {
            return ConfigurationManager.AppSettings.Get(SiliconClientIdKey);
        }

        public static string SiliconClientSecret()
        {
            return ConfigurationManager.AppSettings.Get(SiliconClientSecretKey);
        }
        public static string FrontendUrl()
        {
            return ConfigurationManager.AppSettings.Get(FrontendUrlKey); ;
        }
        public static string AuthUrl()
        {
            return ConfigurationManager.AppSettings.Get(AuthUrlKey);
        }
        public static string EntrypointUrl()
        {
            return ConfigurationManager.AppSettings.Get(EntrypointUrlKey);
        }
    }
}
