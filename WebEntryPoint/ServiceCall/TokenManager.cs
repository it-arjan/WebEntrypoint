using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using IdentityModel.Client;
using NLogWrapper;
namespace WebEntryPoint.ServiceCall
{
    public class TokenManager
    {
        private ILogger _logger = LogManager.CreateLogger(typeof(TokenManager));
        private string _jwt;
        private object changeToken = new object();
        public TokenManager()
        {
            _jwt = null;
        }

        public void SetToken(string freshJwt)
        {
            lock (changeToken)
            {
                // always set the frehToken if it's valid
                if (freshJwt != null && Valid(freshJwt))
                {
                    _jwt = freshJwt;
                    _logger.Debug("SetToken: Setting the fresh Token because it's good.");

                }
                else if (_jwt == null || !Valid(_jwt))
                {
                    _logger.Debug("SetToken: the fresh Token you're try to set is no good or expired: '{0}'. \n..getting a new one", freshJwt);
                    _jwt = GetNewClientToken("MvcFrontEnd").AccessToken;
                }
                else _logger.Debug("SetToken: freh token invalid but old token still valid.");
            }
        }

        public string GetToken()
        {
            // getting it for scope "MvcFrontEnd" should be okay as silicon client as access to all services
            lock (changeToken)
            {
                if (!Valid(_jwt))
                {
                    _logger.Debug("GetToken: getting new token");
                    _jwt = GetNewClientToken("MvcFrontEnd").AccessToken;
                }
                else _logger.Debug("GetToken: re-using existing token");


            }
            return _jwt;
        }

        private bool Valid(string jwt)
        {
            // #PastedCode
            //
            //=> Retrieve the 2nd part of the JWT token (this the JWT payload)
            var payloadBytes = jwt.Split('.')[1];

            //=> Padding the raw payload with "=" chars to reach a length that is multiple of 4
            var mod4 = payloadBytes.Length % 4;
            if (mod4 > 0) payloadBytes += new string('=', 4 - mod4);

            //=> Decoding the base64 string
            var payloadBytesDecoded = Convert.FromBase64String(payloadBytes);

            //=> Retrieve the "exp" property of the payload's JSON
            var payloadStr = Encoding.UTF8.GetString(payloadBytesDecoded, 0, payloadBytesDecoded.Length);
            var payload = JsonConvert.DeserializeAnonymousType(payloadStr, new { Exp = 0UL });

            //=> Comparing the exp timestamp to the current timestamp
            var currentTimestamp = (ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

            var result = currentTimestamp + 10 < payload.Exp; // 10 sec is just a margin

            if (!result) _logger.Debug("Valid: Existing token expired.");

            return result;
        }

        private TokenResponse GetNewClientToken(string scope)
        {
            var tokenUrl = string.Format("{0}connect/token", Helpers.Appsettings.AuthUrl());
            _logger.Debug("Getting a silicon client token at {0}", tokenUrl);
            var client = new TokenClient(tokenUrl, Helpers.Appsettings.SiliconClientId(), Helpers.Appsettings.SiliconClientSecret());

            var token = client.RequestClientCredentialsAsync(scope).Result;
            if (token.IsError) _logger.Error("Error getting Token for silicon Client: {0} ", token.Error);
            else _logger.Debug("Token obtained");

            return token;
        }
    }
}
