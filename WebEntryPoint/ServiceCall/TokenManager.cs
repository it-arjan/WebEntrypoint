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

        public TokenManager()
        {

        }

        public void SetToken(string jwt)
        {
            _jwt = jwt;
        }

        public string GetToken(string jwt, string scope)
        {
            if (!StillValid(_jwt)) _jwt = GetSiliconClientToken(scope).AccessToken;

            return _jwt;
        }

        private bool StillValid(string jwt)
        {
            // Pasted Code
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

            return currentTimestamp > (payload.Exp - 60); // notice the new -60, it's a margin of error in case of long request or bad time synchronization
        }

        private TokenResponse GetSiliconClientToken(string scope)
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
