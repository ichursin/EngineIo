using EngineIo.Client;
using EngineIo.Client.Transports;
using EngineIo.ComponentEmitter;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EngineIo_Tests.ClientTests
{
    public class ConnectionTest : Connection
    {
        private ManualResetEvent _manualResetEvent = null;
        private Socket socket;
        public string Message;

        [Fact]
        public void ConnectToLocalhost()
        {
            _manualResetEvent = new ManualResetEvent(false);

            socket = new Socket(CreateOptions());
            socket.On(Socket.EVENT_OPEN, new TestListener());
            socket.On(Socket.EVENT_MESSAGE, new MessageListener(socket, this));
            socket.Open();
            _manualResetEvent.WaitOne();
            socket.Close();
            Assert.Equal("hi", Message);
        }

        public class TestListener : IListener
        {
            public int Id { get; } = 0;

            public void Call(params object[] args)
            {
                // log.Info("open");
            }

            public int CompareTo(IListener other)
                => Id.CompareTo(other.Id);
        }

        public class MessageListener : IListener
        {
            private readonly Socket _socket;
            private readonly ConnectionTest _connectionTest;
            public int Id { get; } = 0;

            public MessageListener(Socket socket)
            {
                _socket = socket;
            }

            public MessageListener(Socket socket, ConnectionTest connectionTest)
            {
                _socket = socket;
                _connectionTest = connectionTest;
            }

            public void Call(params object[] args)
            {
                //log.Info("message = " + args[0]);
                _connectionTest.Message = (string)args[0];
                _connectionTest._manualResetEvent.Set();
            }

            public int CompareTo(IListener other)
                => Id.CompareTo(other.Id);
        }

        [Fact]
        public void ConnectToLocalhost2()
        {
            _manualResetEvent = new ManualResetEvent(false);
            Message = "";

            var options = CreateOptions();
            options.Transports = ImmutableList.Create<string>(Polling.NAME);
            socket = new Socket(options);

            //socket = new Socket(CreateOptions());
            socket.On(Socket.EVENT_OPEN, () =>
            {
                //log.Info("open");
                //socket.Send("test send");
            });
            socket.On(Socket.EVENT_MESSAGE, (d) =>
            {
                var data = (string)d;

                //log.Info("message2 = " + data);
                Message = data;
                _manualResetEvent.Set();
            });

            socket.Open();
            _manualResetEvent.WaitOne();
            socket.Close();
            Assert.Equal("hi", Message);
        }

        [Fact]
        public void TestmultibyteUtf8StringsWithPolling()
        {
            _manualResetEvent = new ManualResetEvent(false);

            const string SendMessage = "cash money €€€";

            socket = new Socket(CreateOptions());
            socket.On(Socket.EVENT_OPEN, () =>
            {
                //log.Info("open");

                socket.Send(SendMessage);
            });
            socket.On(Socket.EVENT_MESSAGE, (d) =>
            {
                var data = (string)d;

                //log.Info("TestMessage data = " + data);

                if (data == "hi")
                {
                    return;
                }

                Message = data;
                _manualResetEvent.Set();
            });

            socket.Open();
            _manualResetEvent.WaitOne();
            socket.Close();
            //log.Info("TestmultibyteUtf8StringsWithPolling Message = " + Message);
            Assert.Equal(SendMessage, Message);
        }

        [Fact]
        public void Testemoji()
        {
            _manualResetEvent = new ManualResetEvent(false);
            const string SendMessage = "\uD800-\uDB7F\uDB80-\uDBFF\uDC00-\uDFFF\uE000-\uF8FF";

            var options = CreateOptions();
            socket = new Socket(options);

            socket.On(Socket.EVENT_OPEN, () =>
            {
                //log.Info("open");

                socket.Send(SendMessage);
            });

            socket.On(Socket.EVENT_MESSAGE, (d) =>
            {
                var data = (string)d;

                //log.Info(Socket.EVENT_MESSAGE);

                if (data == "hi")
                {
                    return;
                }

                Message = data;
                _manualResetEvent.Set();
            });

            socket.Open();
            _manualResetEvent.WaitOne();
            socket.Close();

            Assert.True(SendMessage == Message);
        }

        [Fact]
        public async Task NotSendPacketsIfSocketCloses()
        {
            var noPacket = true;

            socket = new Socket(CreateOptions());
            socket.On(Socket.EVENT_OPEN, () =>
            {
                noPacket = true;
            });

            socket.Open();
            socket.On(Socket.EVENT_PACKET_CREATE, () =>
            {
                noPacket = false;
                // log.Info("NotSendPacketsIfSocketCloses EVENT_PACKET_CREATE noPacket = " + noPacket);
            });
            socket.Close();
            await Task.Delay(1000);
            // log.Info("NotSendPacketsIfSocketCloses end noPacket = " + noPacket);
            Assert.True(noPacket);
        }
    }
}