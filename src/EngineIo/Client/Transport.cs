using System.Text;
using System.Collections.Immutable;
using EngineIo.ComponentEmitter;
using EngineIo.Modules;
using EngineIo.Parser;
using System;
using System.Collections.Generic;


namespace EngineIo.Client
{
    public abstract class Transport : Emitter
    {
        protected enum ReadyStateEnum
        {
            OPENING,
            OPEN,
            CLOSED,
            PAUSED
        }

        public static readonly string EVENT_OPEN = "open";
        public static readonly string EVENT_CLOSE = "close";
        public static readonly string EVENT_PACKET = "packet";
        public static readonly string EVENT_DRAIN = "drain";
        public static readonly string EVENT_ERROR = "error";
        public static readonly string EVENT_SUCCESS = "success";
        public static readonly string EVENT_DATA = "data";
        public static readonly string EVENT_REQUEST_HEADERS = "requestHeaders";
        public static readonly string EVENT_RESPONSE_HEADERS = "responseHeaders";

        protected static int Timestamps = 0;

        private bool _writeable;
        public bool Writable
        {
            get => _writeable;

            set
            {
                var log = LogManager.GetLogger();
                log.Info(string.Format("Writable: {0} sid={1}", value, Socket.Id));
                _writeable = value;
            }
        }

        public int MyProperty { get; set; }

        public string Name;
        public IDictionary<string, string> Query;

        protected bool IsSecure;
        protected bool TimestampRequests;
        protected int Port;
        protected string Path;
        protected string Hostname;
        protected string TimestampParam;
        protected Socket Socket;
        protected bool Agent = false;
        protected bool ForceBase64 = false;
        protected bool ForceJsonp = false;
        protected string Cookie;

        protected IDictionary<string, string> ExtraHeaders;


        protected ReadyStateEnum ReadyState = ReadyStateEnum.CLOSED;

        protected Transport(Options options)
        {
            Path = options.Path;
            Hostname = options.Hostname;
            Port = options.Port;
            IsSecure = options.IsSecure;
            Query = options.Query;
            TimestampParam = options.TimestampParam;
            TimestampRequests = options.TimestampRequests;
            Socket = options.Socket;
            Agent = options.Agent;
            ForceBase64 = options.ForceBase64;
            ForceJsonp = options.ForceJsonp;
            Cookie = options.GetCookiesAsString();
            ExtraHeaders = options.ExtraHeaders;
        }

        protected Transport OnError(string message, Exception exception)
        {
            Exception err = new EngineIOException(message, exception);
            Emit(EVENT_ERROR, err);
            return this;
        }

        protected void OnOpen()
        {
            ReadyState = ReadyStateEnum.OPEN;
            Writable = true;
            Emit(EVENT_OPEN);
        }

        protected void OnClose()
        {
            ReadyState = ReadyStateEnum.CLOSED;
            Emit(EVENT_CLOSE);
        }


        protected virtual void OnData(string data)
        {
            OnPacket(Parser.Parser.DecodePacket(data));
        }

        protected virtual void OnData(byte[] data)
        {
            OnPacket(Parser.Parser.DecodePacket(data));
        }

        protected void OnPacket(Packet packet)
        {
            Emit(EVENT_PACKET, packet);
        }


        public Transport Open()
        {
            if (ReadyState == ReadyStateEnum.CLOSED)
            {
                ReadyState = ReadyStateEnum.OPENING;
                DoOpen();
            }

            return this;
        }

        public Transport Close()
        {
            if (ReadyState == ReadyStateEnum.OPENING || ReadyState == ReadyStateEnum.OPEN)
            {
                DoClose();
                OnClose();
            }
            return this;
        }

        public Transport Send(ImmutableList<Packet> packets)
        {
            var log = LogManager.GetLogger();
            log.Info("Send called with packets.Count: " + packets.Count);
            var count = packets.Count;
            if (ReadyState == ReadyStateEnum.OPEN)
            {
                //PollTasks.Exec((n) =>
                //{
                Write(packets);
                //});
            }
            else
            {
                throw new EngineIOException("Transport not open");
                //log.Info("Transport not open");
            }
            return this;
        }

        protected abstract void DoOpen();

        protected abstract void DoClose();

        protected abstract void Write(IList<Packet> packets);

        public class Options
        {
            public bool Agent = false;
            public bool ForceBase64 = false;
            public bool ForceJsonp = false;
            public string Hostname;
            public string Path;
            public string TimestampParam;
            public bool IsSecure = false;
            public bool TimestampRequests = true;
            public int Port;
            public int PolicyPort;
            public IDictionary<string, string> Query;
            public bool IgnoreServerCertificateValidation = false;
            internal Socket Socket;
            public IDictionary<string, string> Cookies = new Dictionary<string, string>();
            public IDictionary<string, string> ExtraHeaders = new Dictionary<string, string>();

            public string GetCookiesAsString()
            {
                var result = new StringBuilder();
                var first = true;
                foreach (var item in Cookies)
                {
                    if (!first)
                    {
                        result.Append("; ");
                    }
                    result.AppendFormat("{0}={1}", item.Key, item.Value);
                    first = false;
                }

                return result.ToString();
            }
        }
    }
}
