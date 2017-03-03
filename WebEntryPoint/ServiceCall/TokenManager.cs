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
        private Dictionary<string, string> _tokenMap;
        private object changeToken = new object();
        public TokenManager()
        {
            _tokenMap = new Dictionary<string, string>();
        }

        public string GetToken(string scope)
        {
            lock (changeToken)
            {
                var token = _tokenMap.ContainsKey(scope) ? _tokenMap[scope] : null;
                if (token != null && !Expired(token))
                {
                    _logger.Debug("GetToken: re-using existing token");
                }
                else
                {
                    _logger.Debug("GetToken: getting a new token");
                    _tokenMap[scope] = GetNewClientToken(scope).AccessToken;
                }
            }
            return _tokenMap[scope];
        }

        private bool Expired(string jwt)
        {
            _logger.Debug("Valid: Checking expiration of token {0}", jwt);
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

            _logger.Debug("Valid: the token is valid until {0}.", new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(payload.Exp));

            //=> Comparing the exp timestamp to the current timestamp
            var currentTimestamp = (ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

            var result = currentTimestamp + 10 > payload.Exp; // 10 sec is just a margin
            if (result) _logger.Debug("Valid: token expired.");
            else _logger.Debug("Valid: token still valid.");
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
