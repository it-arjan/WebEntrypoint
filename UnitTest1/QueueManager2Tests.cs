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
    public class QueueManager2Tests
    {
        [TestMethod()]
        public void QueueManager2Test()
        {
            // This Unit test still depends on MSMQ, 
            // The queue used are auto-created for this test
            var factory = new MockRepository(MockBehavior.Loose) { DefaultValue = DefaultValue.Empty };
            Mock<IWebService> postbackMoq;
            List<string> UnitTestQlist;
            QueueManager2 qm;
            CreateQueueManagerWithMocks(factory, out postbackMoq, out UnitTestQlist, out qm);

            qm.StartListening();

            DropMessage(UnitTestQlist[0]);
            Task.Delay(5000).Wait(); // halleluja

            // TODO, see if it is possible to get the dropped databag from the postbackservice
            // to test the content fakeservice
            postbackMoq.Verify(pb => pb.CallSync(It.IsAny<DataBag>()), Times.AtLeastOnce());
        }

        private static void CreateQueueManagerWithMocks(MockRepository mockRep,
                                out Mock<IWebService> postbackMoq, out List<string> UnitTestQlist, out QueueManager2 qm)
        {
            var tokenMoq = mockRep.Create<ITokenManager>();
            tokenMoq.Setup(tm => tm.GetToken("testscope")).Returns("autotest-Token");

            postbackMoq = mockRep.Create< IWebService>();
            postbackMoq.Setup(pb => pb.CallSync(It.IsAny<DataBag>())).Returns(System.Net.HttpStatusCode.OK);

            var wsFactMock = mockRep.Create<IWebserviceFactory>();
            wsFactMock.Setup(fa => fa.Create(It.IsInRange(QServiceConfig.Service1, QServiceConfig.Service7, Range.Inclusive),
                                 It.IsAny<ITokenManager>()))
                                .Returns(new FakeService(3));

            wsFactMock.Setup(fa => fa.Create(QServiceConfig.Service8, It.IsAny<ITokenManager>())).Returns(postbackMoq.Object);

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

        private static void DropMessage(string queue)
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
            entryQueue.Send(msg, dataBag.Label);
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
            }
            return qlist;
        }

        [TestMethod()]
        public void ToggleModusTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetModusTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void StopAllTest()
        {
            Assert.Fail();
        }
    }
}

