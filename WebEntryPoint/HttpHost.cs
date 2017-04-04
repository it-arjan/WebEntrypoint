using Owin;
using System.Web.Http;
using NLogWrapper;
using IdentityServer3.AccessTokenValidation;
using System.Security.Cryptography.X509Certificates;
using System.Web.Http.Cors;

namespace WebEntryPoint
{
    public class HttpHost
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        private ILogger _logger = LogManager.CreateLogger(typeof(HttpHost), Helpers.Appsettings.LogLevel());

        public void Configuration(IAppBuilder appBuilder)
        {
            appBuilder.UseIdentityServerBearerTokenAuthentication(new IdentityServerBearerTokenAuthenticationOptions
            {
                Authority = Helpers.Appsettings.AuthUrl(),
                ValidationMode = ValidationMode.Local, //local for jwt tokene, server-endpoint for referece tokenes
                RequiredScopes = new[] {Helpers.IdSrv3.ScopeEntryQueueApi } 
            });

            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            string urlCsv = AllowedCorsUrls();

            var corsAttr = new EnableCorsAttribute(urlCsv, "*", "*");
            config.EnableCors(corsAttr);

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // wire up logging
            config.MessageHandlers.Add(new LogRequestMessageHandler());

            // require authentication for all controllers 
            //NOTNOW
            //config.Filters.Add(new AuthorizeAttribute());

            appBuilder.UseWebApi(config);
            _logger.Info("startup executed");
        }

        private static string AllowedCorsUrls()
        {
            var allowedCorsHostnames = Helpers.Appsettings.AllowedSocketListenerCsv().Split(new[] { "," }, System.StringSplitOptions.RemoveEmptyEntries);
            var urlCsv = string.Empty;

            foreach (var hostName in allowedCorsHostnames)
                urlCsv = urlCsv == string.Empty
                    ? string.Format("{0}://{1}", Helpers.Appsettings.Scheme(), hostName)
                    : string.Format("{0}, {1}://{2}", urlCsv, Helpers.Appsettings.Scheme(), hostName);
            return urlCsv;
        }
    }
}