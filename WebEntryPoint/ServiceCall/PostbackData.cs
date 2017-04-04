using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebEntryPoint.ServiceCall
{
    public class PostbackData
    {
        public PostbackData(DataBag databag)
        {
            MessageId = databag.MessageId;
            Content = databag.Content;
            Start = databag.Started;
            End = DateTime.Now;
            Duration = (decimal)(DateTime.Now - databag.Started).TotalSeconds;
            UserName = databag.UserName;
        }
        public string MessageId { get; set; }
        public string UserName { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public decimal Duration { get; set; }
        public string Content { get; set; }

    }
}
