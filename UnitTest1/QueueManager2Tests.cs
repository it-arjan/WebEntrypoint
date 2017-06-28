using WebEntryPoint.ServiceCall;
using WebEntryPoint.WebSockets;
using WebEntryPoint.Helpers;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;
using System.Messaging;
using System.Collections.Generic;

namespace WebEntryPoint.MQ.Tests
{
    [TestClass()]
    // TODO see why running ind test succceeds, but when running all tests, some tests rund out of time
    // test manager Overhead?
    public class QueueManager2Tests
    {
        // These Unit tests still depends on MSMQ, the queues are auto-created
        // see no point in simulating MSMQ

        [TestMethod()]
        public void DropOneMessage()
        {
            var factory = new MockRepository(MockBehavior.Loose) { DefaultValue = DefaultValue.Empty };
            Mock<IWebService> postbackMoq;
            List<string> UnitTestQlist;
            QueueManager2 qm;
            MockupQueueManager(factory, out postbackMoq, out UnitTestQlist, out qm);

            qm.StartListening();
            if (qm.GetModus() != QueueManager2.ExeModus.Sequential) qm.ToggleModus();
            DropMessages(UnitTestQlist[0], 1);
            Task.Delay(3000).Wait();  // 2 sec is long enough to run the test manually
            qm.StopAll();

            // TODO, see if it is possible to get the dropped databag parameter from the CallSync method
            // to test the content that fakeservice added
            postbackMoq.Verify(pb => pb.CallSync(It.IsAny<DataBag>()), Times.Exactly(1));
        }
        [TestMethod()]
        public void DropMoreMessagesInParallellMode()
        {
            var factory = new MockRepository(MockBehavior.Loose) { DefaultValue = DefaultValue.Empty };
            Mock<IWebService> postbackMoq;
            List<string> UnitTestQlist;
            QueueManager2 qm;
            MockupQueueManager(factory, out postbackMoq, out UnitTestQlist, out qm);

            qm.StartListening();
            Assert.IsTrue(qm.GetModus() == QueueManager2.ExeModus.Paralell);

            DropMessages(UnitTestQlist[0], 30);
            Task.Delay(20000).Wait();  // 20 sec is long enough to run the test manually
            qm.StopAll();

            postbackMoq.Verify(pb => pb.CallSync(It.IsAny<DataBag>()), Times.Exactly(30));
        }

        [TestMethod()]
        public void DropMoreMessagesInSequentialMode()
        {
            var factory = new MockRepository(MockBehavior.Loose) { DefaultValue = DefaultValue.Empty };
            Mock<IWebService> postbackMoq;
            List<string> UnitTestQlist;
            QueueManager2 qm;
            MockupQueueManager(factory, out postbackMoq, out UnitTestQlist, out qm);

            qm.StartListening();

            qm.ToggleModus();
            Assert.IsTrue(qm.GetModus() == QueueManager2.ExeModus.Sequential, "modus not seq after toggle");

            DropMessages(UnitTestQlist[0], 5);
            Task.Delay(5000).Wait(); // 5 sec is long enough to run the test manually

            qm.StopAll();
            postbackMoq.Verify(pb => pb.CallSync(It.IsAny<DataBag>()), Times.Exactly(5));
        }

        private static void MockupQueueManager(MockRepository mockRep,
                                out Mock<IWebService> postbackMoq, out List<string> UnitTestQlist, out QueueManager2 qm)
        {
            var tokenMoq = mockRep.Create<ITokenCache>();
            tokenMoq.Setup(tm => tm.GetToken("testscope")).Returns("autotest-Token");

            postbackMoq = mockRep.Create<IWebService>();
            postbackMoq.Setup(pb => pb.CallSync(
                    It.IsAny<DataBag>()))
                .Returns(System.Net.HttpStatusCode.OK);

            var wsFactMock = mockRep.Create<IWebserviceFactory>();

            wsFactMock.Setup(fa => fa.Create(
                It.IsInRange(QServiceConfig.Service1, QServiceConfig.Service7, Range.Inclusive),
                        It.IsAny<ITokenCache>()))
                    .Returns(new FakeService(maxConcRequests: 150, maxDelaySecs: 1, failFactor:0));

            wsFactMock.Setup(fa => fa.Create(
                QServiceConfig.Service8, 
                        It.IsAny<ITokenCache>()))
                    .Returns(postbackMoq.Object);

            var socketClientMoq = mockRep.Create<ISocketClient>();
            socketClientMoq.Setup(sc => sc.Send("socketToken", "message"));

            UnitTestQlist = SetupMessagequeues();

            qm = new QueueManager2(
                UnitTestQlist[0],
                UnitTestQlist[1], UnitTestQlist[2], UnitTestQlist[3],
                UnitTestQlist[4],
                UnitTestQlist[5], UnitTestQlist[6],
                wsFactMock.Object, tokenMoq.Object, socketClientMoq.Object);
        }

        private static void DropMessages(string queue, int nr)
        {
            var dataBag = new DataBag();
            dataBag.Label = "UnitTest" + " - " + DateTime.Now.ToShortTimeString();
            dataBag.MessageId = "UnitTest";
            dataBag.UserName = "UnitTest";
            dataBag.Started = DateTime.Now;

            var msg = new Message();
            msg.Body = dataBag;

            var entryQueue = new MSMQWrapper(queue);
            entryQueue.SetFormatters(typeof(DataBag));
            for (var i = 0; i < nr; i++)
            {
                entryQueue.Send(msg, dataBag.Label);
            }
        }
        private static void DropMessages(MSMQWrapper queue, int nr)
        {
            var dataBag = new DataBag();
            dataBag.Label = "UnitTest" + " - " + DateTime.Now.ToShortTimeString();
            dataBag.MessageId = "UnitTest";
            dataBag.UserName = "UnitTest";
            dataBag.Started = DateTime.Now;

            var msg = new Message();
            msg.Body = dataBag;

            //var entryQueue = new MSMQWrapper(queue);
            queue.SetFormatters(typeof(DataBag));
            for (var i = 0; i < nr; i++)
            {
                queue.Send(msg, dataBag.Label);
            }
        }
        private static List<string> SetupMessagequeues()
        {
            var qlist = new List<string>
            {
                @".\Private$\autoTestEntry",
                @".\Private$\autoTestService1",@".\Private$\autoTestService2",@".\Private$\autoTestService3",
                @".\Private$\autoTestExit",
                @".\Private$\autoTestCmd",@".\Private$\autoTestCmdReply"
            };
            foreach (var q in qlist)
            {
                if (!MessageQueue.Exists(q))
                {
                    MessageQueue.Create(q);
                }
                else
                {
                    var x = new MessageQueue(q);
                    x.Purge();
                }
            }
            return qlist;
        }

        [TestMethod()]
        public void ToggleModusTest()
        {
            var factory = new MockRepository(MockBehavior.Loose) { DefaultValue = DefaultValue.Empty };
            Mock<IWebService> postbackMoq;
            List<string> UnitTestQlist;
            QueueManager2 qm;
            MockupQueueManager(factory, out postbackMoq, out UnitTestQlist, out qm);

            var oldModus = qm.GetModus();
            qm.ToggleModus();
            Assert.IsTrue(oldModus != qm.GetModus());
        }

        [TestMethod()]
        public void StopAllTest()
        {
            var factory = new MockRepository(MockBehavior.Loose) { DefaultValue = DefaultValue.Empty };
            Mock<IWebService> postbackMoq;
            List<string> UnitTestQlist;

            QueueManager2 qm;
            MockupQueueManager(factory, out postbackMoq, out UnitTestQlist, out qm);
            qm.StartListening();

            var result = qm.StopAll();
            Assert.IsTrue(result == 0, "stopall returned " + result);

            DropMessages(UnitTestQlist[0], 1); 
            Task.Delay(100).Wait();

            var Q = new MSMQWrapper(UnitTestQlist[0]);
            var nr_messages = Q.Q.GetAllMessages().Length;
            Assert.IsTrue(nr_messages == 1, "nr msgs != 1 but " + nr_messages);
        }

        [TestMethod()]
        public void Bare_MSMQ_OnlyDisposeCancelsBeginReceive()
        {
            // Bare MSMQ test
            var qname = @".\Private$\autoTestEntry";
            if (!MessageQueue.Exists(qname))
            {
                MessageQueue.Create(qname);
            }

            var Q = new MessageQueue(qname);
            Q.Purge();
 
            DropMessages(qname, 1); Task.Delay(100).Wait();
            var nr_messages = Q.GetAllMessages().Length;
            Assert.IsTrue(nr_messages == 1, "nr msgs != 1 but " + nr_messages); // This one stays

            Q.BeginReceive();
            nr_messages = Q.GetAllMessages().Length;
            Assert.IsTrue(nr_messages == 0, "nr msgs != 0 but " + nr_messages);

            Q.Dispose(); // only Dispose cancels beginreceive

            DropMessages(qname, 1); Task.Delay(100).Wait();
            var Q2 = new MessageQueue(qname);
            nr_messages = Q2.GetAllMessages().Length;
            Assert.IsTrue(nr_messages == 1, "nr msgs != 1 but " + nr_messages);

        }
    }
}

