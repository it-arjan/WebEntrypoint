using System;

namespace WebEntryPoint.Helpers.Models
{
    internal class RequestLogEntry
    {
        public object User { get; set; }
        public string ContentType { get; set; }
        public string Ip { get; set; }
        public object Method { get; set; }
        public DateTime Timestamp { get; set; }
        public object Path { get; set; }
        public string AspSessionId { get; set; }
    }
}