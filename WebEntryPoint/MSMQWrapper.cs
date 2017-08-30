using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Messaging;
using System.Configuration;

namespace WebEntryPoint.MQ
{
    /*
     * This is just a wrapper for convenience and serving as documentation, 
     * goal is NOT to completely encapusulate MSMQ
     * MSMQ often fails (without msg) on incorrect usage, 
     * This behavior is often not documented
     * This wrapper class serves to handle these cases
     * 
     * */
    public class MSMQWrapper
    {
        public MessageQueue Q { get; set;} // make public in case something is not wrapped
        public string Name { get; set; }
        public MSMQWrapper(string qname)
        {
            Name = qname;
            Q = new MessageQueue(qname);
        }
        public bool Transactional { get { return Q.Transactional; } }
        //private List<Type> _formatterTypes { get; set; }
        public void SetFormatters(params Type[] types)
        {
            Q.Formatter = new XmlMessageFormatter(types);
        }

        public void AddHandler(QueueManager2.EventHandlerWithQueue handler, MSMQWrapper queue)
        {
            if (!Q.Transactional)
            {
                Q.ReceiveCompleted += (sender, e) => handler(sender, e, queue);
            }
            else throw new MsMQQueueUsageException(string.Format("{0}: Setting Receivecomplete handler for non-transactional queues breaks the thing ", Name));
        }

        public void RemoveHandler(QueueManager2.EventHandlerWithQueue handler)
        {
            Q.ReceiveCompleted -= (sender, e) => handler(sender, e, this);
        }

        public void AddHandler(ReceiveCompletedEventHandler handler)
        {
            if (!Q.Transactional)
            {
                // (sender, e) => handler(sender, e, queue)
                Q.ReceiveCompleted += handler;
            }
            else throw new MsMQQueueUsageException(string.Format("{0}: Setting Receivecomplete handler for non-transactional queues breaks the thing ", Name));
        }


        public void RemoveHandler(ReceiveCompletedEventHandler handler)
        {
            Q.ReceiveCompleted -= handler;
        }

        public void Send(Message msg, string label = null)
        {
            var msgLabel = label != null ? label : Q.Transactional ? "msg sent transactional" : "msg sent non-transactional";
            if (Q.Transactional)
            {
                // we must use transactions in transactional queue, otherwise msg do not arrive
                MessageQueueTransaction transaction = new MessageQueueTransaction();
                try
                {
                    transaction.Begin();
                    Q.Send(msg, msgLabel, transaction);
                    transaction.Commit();
                }
                catch (System.Exception e)
                {
                    transaction.Abort();
                    throw e;
                }
                finally
                {
                    transaction.Dispose();
                }
            }
            else
            {
                Q.Send(msg, msgLabel); 
            }
        }
        public void BeginReceive()
        {
            if (!Q.Transactional)
            {
                Q.BeginReceive();
            }
            else throw new MsMQQueueUsageException(string.Format("{0}: Calling BeginReceive on transactional queues does nothing ", Name));
        }

        public Message EndReceive(IAsyncResult asyncResult)
        {
            return Q.EndReceive(asyncResult);
        }
    }
    public class MsMQQueueUsageException : Exception
    {
        public MsMQQueueUsageException(string message) : base(message)
        {

        }
    }
}
