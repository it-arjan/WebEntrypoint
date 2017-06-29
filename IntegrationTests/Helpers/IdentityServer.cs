using IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;

namespace IntegrationTests.Helpers
{
    public static class IdentityServer
    {
        public const string ScopeMcvFrontEndHuman = "mvc-frontend-human";

        public const string ScopeMvcFrontEnd = "mvc-frontend-silicon";
        public const string ScopeEntryQueueApi = "entry-queue-api";
        public const string ScopeNancyApi = "nancy-api";
        public const string ScopeFrontendDataApi = "frontend-data-api";
        public const string ScopeServiceStackApi = "servicestack-api";
        public const string ScopeWcfService = "wcf-service";
        public const string ScopeMsWebApi = "ms-webapi2";

        public static string UniqueClaimOfAntiForgeryToken = "given_name";

        public static int SessionRefreshTimeoutSecs = 3600;

        public static TokenResponse NewSiliconClientToken(string scope)
        {
            var tokenUrl = string.Format("{0}connect/token", TestSettings.AuthUrl());
            Debug.Print(tokenUrl);
            Console.Write(tokenUrl);
            var client = new TokenClient(tokenUrl, TestSettings.SiliconClientId(), TestSettings.SiliconClientSecret());

            var token = client.RequestClientCredentialsAsync(scope).Result;
            if (token.IsError) Console.WriteLine( "Error Getting a Silicon Token for scope " + scope);
            return token;
        }

    }
}