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
    public class TokenCache : ITokenCache
    {
        private ILogger _logger = LogManager.CreateLogger(typeof(TokenCache), Helpers.ConfigSettings.LogLevel());
        private Dictionary<string, string> _tokenMap;
        private object changeToken = new object();
        public TokenCache()
        {
            _tokenMap = new Dictionary<string, string>();
        }

        public string GetToken(string scope)
        {
            _logger.Debug("GetToken called with scope '{0}'", scope);
            lock (changeToken)
            {
                var token = _tokenMap.ContainsKey(scope) ? _tokenMap[scope] : null;
                if (token != null && !Expired(token, scope))
                {
                    _logger.Debug("GetToken: re-using existing token");
                }
                else
                {
                    _logger.Debug("GetToken: getting a new token for scope {0}", scope);
                    _tokenMap[scope] = GetNewClientToken(scope).AccessToken;
                }
            }
            return _tokenMap[scope];
        }

        private bool Expired(string jwt, string scope)
        {
            _logger.Debug("Checking expiration of token({1}) {0}", jwt, scope);
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


            var date1970CET = new DateTime(1970, 1, 1, 0, 0, 0).AddHours(1);
            _logger.Debug("Expired Check: the token({1}) is valid until {0}.", date1970CET.AddSeconds(payload.Exp), scope);

            //=> Get the current timestamp
            var currentTimestamp = (ulong)(DateTime.UtcNow.AddHours(1) - date1970CET).TotalSeconds;
            // Compare
            var isExpired = currentTimestamp + 10 > payload.Exp; // 10 sec = margin
            var logMsg = isExpired  ? string.Format("Expired Check: token({0}) is expired.", scope)
                                    : string.Format("Expired Check: token({0}) still valid.", scope);
            _logger.Info(logMsg);

            return isExpired;
        }

        private TokenResponse GetNewClientToken(string scope)
        {
            var tokenUrl = string.Format("{0}connect/token", Helpers.ConfigSettings.AuthUrl());
            _logger.Info("Getting a silicon client token at {0}", tokenUrl);
            var client = new TokenClient(tokenUrl, Helpers.ConfigSettings.SiliconClientId(), Helpers.ConfigSettings.SiliconClientSecret());

            var token = client.RequestClientCredentialsAsync(scope).Result;
            if (token.IsError) _logger.Error("Error getting Token for silicon Client: {0} ", token.Error);

            return token;
        }
    }
}
