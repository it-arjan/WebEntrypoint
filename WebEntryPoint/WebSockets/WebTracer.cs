﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLogWrapper;

namespace WebEntryPoint.WebSockets
{
    public class WebTracer
    {
        private SocketClient _socket = new SocketClient();
        private string _url;
        static ILogger _logger = LogManager.CreateLogger(typeof(WebTracer));
        public WebTracer(string serverUrl)
        {
            _socket = new SocketClient();
            _url = serverUrl;
            _logger.Info("Will ask socketclient to connect to {0}", _url);
        }

        public void Send(string sessionToken, string msg, params object[] msgPars)
        {
            try
            {
                _socket.Connect(_url);
                var tsk = _socket.Send(sessionToken, msg, msgPars);
                tsk.Wait(); tsk.Dispose();
                _socket.Close();
            }
            catch (Exception ex)
            {
                _logger.Error("Error sending data to socket on server {0}. Msg: {1}", _url, ex.Message);
            }
        }
    }
}
