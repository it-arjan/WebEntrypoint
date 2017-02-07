﻿using Owin;
using System.Web.Http;
using NLogWrapper;
using IdentityServer3.AccessTokenValidation;
using System.Security.Cryptography.X509Certificates;

namespace WebEntryPoint
{
    public class HttpHost
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        private ILogger _logger = LogManager.CreateLogger(typeof(HttpHost));

        public void Configuration(IAppBuilder appBuilder)
        {
            _logger.Info("Configuring to accept access tokens from identityserver and require a scope of 'EntryQueueApi'..");

            appBuilder.UseIdentityServerBearerTokenAuthentication(new IdentityServerBearerTokenAuthenticationOptions
            {
                Authority = Helpers.Appsettings.AuthUrl(),
                ValidationMode = ValidationMode.Local, //local for jwt tokene, server-endpoint for referece tokenes
                RequiredScopes = new[] { "EntryQueueApi" }
            });

            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            config.EnableCors();

            config.MapHttpAttributeRoutes(); //in case you use [] routes
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            
            // wire up logging
            config.MessageHandlers.Add(new LogRequestMessageHandler());
            
            // require authentication for all controllers 
            //NOTNOW
            //config.Filters.Add(new AuthorizeAttribute());

            appBuilder.UseWebApi(config);
        }
    }
}