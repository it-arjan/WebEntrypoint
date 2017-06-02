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
            // special queues are auto-created for this test

            var tokenMoq = new Mock<ITokenManager>();
            tokenMoq.Setup(tm => tm.GetToken("testscope")).Returns("autotest-Token");

            var postbackMoq = new Mock<IWebService>();
            postbackMoq.Setup(pb => pb.CallSync(It.IsAny<DataBag>())).Returns(System.Net.HttpStatusCode.OK);

            var wsFactMock = new Mock<IWebserviceFactory>();
            wsFactMock.Setup(fa => fa.Create(It.IsInRange(QServiceConfig.Service1, QServiceConfig.Service7, Range.Inclusive),
                                 It.IsAny<ITokenManager>()))
                                .Returns(new FakeService(3));

            wsFactMock.Setup(fa => fa.Create(QServiceConfig.Service8, It.IsAny<ITokenManager>())).Returns(postbackMoq.Object);

            var socketClientMoq = new Mock<ISocketClient>();
            socketClientMoq.Setup(sc => sc.Send("socketToken", "message"));

            var UnitTestQlist = SetupMessagequeues();

            var qm = new QueueManager2(
                UnitTestQlist[0],
                UnitTestQlist[1], UnitTestQlist[2], UnitTestQlist[3],
                UnitTestQlist[4],
                UnitTestQlist[5], UnitTestQlist[6],
                wsFactMock.Object, tokenMoq.Object, socketClientMoq.Object);

            qm.StartListening();

            DropMessage(UnitTestQlist);
            Task.Delay(5000).Wait();

            postbackMoq.Verify(pb => pb.CallSync(It.IsAny<DataBag>()), Times.AtLeastOnce());
        }

        private static void DropMessage(List<string> UnitTestQlist)
        {
            var dataBag = new DataBag();
            dataBag.Label = "UnitTest" + " - " + DateTime.Now.ToShortTimeString();
            dataBag.MessageId = "UnitTest";
            dataBag.UserName = "UnitTest";
            dataBag.Started = DateTime.Now;

            var msg = new Message();
            msg.Body = dataBag;

            var entryQueue = new MSMQWrapper(UnitTestQlist[0]);
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

