using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EasyHttp.Http;
using IntegrationTests.Helpers;
using IdentityModel.Client;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public void WebEntrypoint_DropMessage()
        {
            // get an accesstoken
            var _entryQueueToken = Helpers.IdentityServer.NewSiliconClientToken(Helpers.IdentityServer.ScopeEntryQueueApi);
            Assert.IsFalse(_entryQueueToken.IsError);

            var messageId = Guid.NewGuid().ToString();
            // drop a message in entry point api
 
            var apiUrl = string.Format("{0}/api/entryqueue", TestSettings.EntrypointUrl());

            var auth_header = string.Format("bearer {0}", _entryQueueToken.AccessToken);
            //_logger.Debug(string.Format("Calling {0} with token: {1}", apiUrl, auth_header));

            var easyHttp = new HttpClient();

            easyHttp.Request.AddExtraHeader("Authorization", auth_header);
            easyHttp.Request.Accept = HttpContentTypes.ApplicationJson;

            var data = new WebEntryPoint.EntryQueuePostData();
            data.MessageId = messageId;
            data.PostBackUrl = string.Format("{0}/postback/", Helpers.TestSettings.DataApiUrl());
            data.SocketToken = "";
            data.DoneToken = "";
            data.UserName = "AutoTest";
            data.NrDrops = 0; // should be changed to 1

            easyHttp.Post(apiUrl, data, "application/json");
            // fetch the messageID from postbackapi every 10 sec for 20 times
            var getResultUrl = string.Format("{0}/postback/today", Helpers.TestSettings.DataApiUrl());
            var apiToken = Helpers.IdentityServer.NewSiliconClientToken(Helpers.IdentityServer.ScopeNancyApi);
            bool messageFound = false;
            var loops = 0;
            bool error = false;
            easyHttp = new HttpClient();
            easyHttp.Request.AddExtraHeader("Authorization", string.Format("bearer {0}", apiToken.AccessToken));
            easyHttp.Request.Accept = HttpContentTypes.ApplicationJson;

            while (!messageFound && loops < 5 && !error)
            {
                loops++;
                Task.Delay(5000).Wait();
                easyHttp.Get(getResultUrl);
                error = easyHttp.Response.StatusCode != System.Net.HttpStatusCode.OK;
                messageFound = easyHttp.Response.RawText.Contains(messageId);
            }

            Assert.IsTrue(messageFound);
        }

        [TestMethod]
        public void WebEntrypoint_ConfigureServices()
        {
            var _entryQueueToken = Helpers.IdentityServer.NewSiliconClientToken(Helpers.IdentityServer.ScopeEntryQueueApi);
            Assert.IsFalse(_entryQueueToken.IsError, "Getting the silicon token failed for scope " + IdentityServer.ScopeEntryQueueApi);

            var CmdUrl = string.Format("{0}/api/cmdqueue", TestSettings.EntrypointUrl());

            var auth_header = string.Format("bearer {0}", _entryQueueToken.AccessToken);

            var easyHttp = new HttpClient();

            easyHttp.Request.AddExtraHeader("Authorization", auth_header);
            easyHttp.Request.Accept = HttpContentTypes.ApplicationJson;

            var data = new WebEntryPoint.CmdPostData();
            data.CmdType = "SetServiceConfig";
            data.Service1Nr = "1";
            data.Service2Nr = "3";
            data.Service3Nr = "4";
            easyHttp.Post(CmdUrl, data, "application/json");

            bool ok = easyHttp.Response.RawText.Contains("Service1") &&
                easyHttp.Response.RawText.Contains("Service3") &&
                easyHttp.Response.RawText.Contains("Service4");

            Assert.IsTrue(ok, "Setting service config did dnot return the expected result");

            easyHttp = new HttpClient();

            easyHttp.Request.AddExtraHeader("Authorization", auth_header);
            easyHttp.Request.Accept = HttpContentTypes.ApplicationJson;

            data = new WebEntryPoint.CmdPostData();
            data.CmdType = "GetServiceConfig";

            easyHttp.Post(CmdUrl, data, "application/json");

            ok = easyHttp.Response.RawText.Contains("Service1") &&
                easyHttp.Response.RawText.Contains("Service3") &&
                easyHttp.Response.RawText.Contains("Service4");

            Assert.IsTrue(ok, "Getting service config did dnot return the expected result");
        }
    }
}
