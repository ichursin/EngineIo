using EngineIo.Client;
using EngineIo.Client.Transports;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using Xunit;

namespace EngineIo.Tests.ClientTests
{
    public class BinaryPollingTest : Connection
    {
        //[Fact]
        //public void PingTest()
        //{
        //    var log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod());
        //    log.Info("Start");

        //    var binaryData = new byte[5];
        //    for (int i = 0; i < binaryData.Length; i++)
        //    {
        //        binaryData[i] = (byte)i;
        //    }

        //    var events = new ConcurrentQueue<object>();

        //    var options = CreateOptions();
        //    options.Transports = ImmutableList.Create<string>(Polling.NAME);

        //    var socket = new Socket(options);

        //    socket.On(Socket.EVENT_OPEN, () =>
        //    {
        //        log.Info("EVENT_OPEN");

        //        socket.Send(binaryData);
        //        socket.Send("cash money €€€");
        //    });

        //    socket.On(Socket.EVENT_MESSAGE, (d) =>
        //    {
        //        var data = d as string;
        //        log.Info(string.Format("EVENT_MESSAGE data ={0} d = {1} ", data, d));

        //        if (data == "hi")
        //        {
        //            return;
        //        }
        //        events.Enqueue(d);
        //        //socket.Close();
        //    });

        //    socket.Open();
        //    Task.Delay(20000).Wait();
        //    socket.Close();
        //    log.Info("ReceiveBinaryData end");

        //    var binaryData2 = new byte[5];
        //    for (int i = 0; i < binaryData2.Length; i++)
        //    {
        //        binaryData2[i] = (byte)(i + 1);
        //    }

        //    object result;
        //    events.TryDequeue(out result);
        //    Assert.Equal("1", "1");
        //}

        [Fact(Skip = "Should configure server side")]
        public void ReceiveBinaryData()
        {
            var manualResetEvent = new ManualResetEvent(false);
            var events = new ConcurrentQueue<object>();

            var binaryData = new byte[5];
            for (byte i = 0; i < binaryData.Length; i++)
            {
                binaryData[i] = i;
            }

            var options = CreateOptions();
            options.Transports = ImmutableList.Create(Polling.NAME);

            var socket = new Socket(options);

            socket.On(Socket.EVENT_OPEN, () =>
            {
                socket.Send(binaryData);
                //socket.Send("cash money €€€");
            });

            socket.On(Socket.EVENT_MESSAGE, (d) =>
            {
                var data = d as string;
                //log.Info(string.Format("EVENT_MESSAGE data ={0} d = {1} ", data, d));

                if (data == "hi")
                {
                    return;
                }
                events.Enqueue(d);
                manualResetEvent.Set();
            });

            socket.Open();
            manualResetEvent.WaitOne();
            socket.Close();
            //log.Info("ReceiveBinaryData end");

            var binaryData2 = new byte[5];
            for (int i = 0; i < binaryData2.Length; i++)
            {
                binaryData2[i] = (byte)(i + 1);
            }

            events.TryDequeue(out object result);
            Assert.Equal(binaryData, result);
        }

        [Fact(Skip = "Should configure server side")]
        public void ReceiveBinaryDataAndMultibyteUTF8String()
        {
            var manualResetEvent = new ManualResetEvent(false);

            var events = new ConcurrentQueue<object>();

            var stringData = "cash money €€€";
            var binaryData = new byte[] { 0, 1, 2, 3, 4 };

            var options = CreateOptions();
            options.Transports = ImmutableList.Create(Polling.NAME);

            var socket = new Socket(options);

            socket.On(Socket.EVENT_OPEN, () =>
            {
                socket.Send(binaryData);
                socket.Send(stringData);
            });

            socket.On(Socket.EVENT_MESSAGE, (d) =>
            {
                var data = d as string;
                //log.Info(string.Format("EVENT_MESSAGE data ={0} d = {1} ", data, d));

                if (data == "hi")
                {
                    return;
                }
                events.Enqueue(d);
                if (events.Count > 1)
                {
                    manualResetEvent.Set();
                }
            });

            socket.Open();
            manualResetEvent.WaitOne();
            socket.Close();
            var binaryData2 = new byte[5];
            for (int i = 0; i < binaryData2.Length; i++)
            {
                binaryData2[i] = (byte)(i + 1);
            }

            events.TryDequeue(out object result);
            Assert.Equal(binaryData, result);
            events.TryDequeue(out result);
            Assert.Equal(stringData, (string)result);
            socket.Close();
        }
    }
}