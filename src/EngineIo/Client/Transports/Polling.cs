using System.Collections.Immutable;
using EngineIo.ComponentEmitter;
using EngineIo.Modules;
using EngineIo.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EngineIo.Client.Transports
{
    public class Polling : Transport
    {
        public const string NAME = "polling";

        public static readonly string EVENT_POLL = "poll";
        public static readonly string EVENT_POLL_COMPLETE = "pollComplete";

        private bool IsPolling = false;

        public Polling(Options opts)
            : base(opts)
        {
            Name = NAME;
        }

        protected override void DoOpen()
        {
            Poll();
        }

        public void Pause(Action onPause)
        {
            //var log = LogManager.GetLogger(Global.CallerName());

            ReadyState = ReadyStateEnum.PAUSED;
            Action pause = () =>
            {
                //log.Info("paused");
                ReadyState = ReadyStateEnum.PAUSED;
                onPause();
            };

            if (IsPolling || !Writable)
            {
                var total = new[] { 0 };


                if (IsPolling)
                {
                    //log.Info("we are currently polling - waiting to pause");
                    total[0]++;
                    Once(EVENT_POLL_COMPLETE, new PauseEventPollCompleteListener(total, pause));

                }

                if (!Writable)
                {
                    //log.Info("we are currently writing - waiting to pause");
                    total[0]++;
                    Once(EVENT_DRAIN, new PauseEventDrainListener(total, pause));
                }

            }
            else
            {
                pause();
            }
        }

        public void Resume()
        {
            if (ReadyState == ReadyStateEnum.PAUSED)
            {
                ReadyState = ReadyStateEnum.OPEN;
            }
        }

        private class PauseEventDrainListener : IListener
        {
            private readonly int[] _total;
            private readonly Action _pause;

            public int Id { get; } = 0;

            public PauseEventDrainListener(int[] total, Action pause)
            {
                _total = total;
                _pause = pause;
            }
            public void Call(params object[] args)
            {
                // var log = LogManager.GetLogger(Global.CallerName());
                // log.Info("pre-pause writing complete");

                if (--_total[0] == 0)
                {
                    _pause.Invoke();
                }
            }

            public int CompareTo(IListener other)
                => Id.CompareTo(other.Id);
        }

        private class PauseEventPollCompleteListener : IListener
        {
            private readonly int[] _total;
            private readonly Action _pause;

            public int Id { get; } = 0;

            public PauseEventPollCompleteListener(int[] total, Action pause)
            {
                _total = total;
                _pause = pause;
            }

            public void Call(params object[] args)
            {
                // var log = LogManager.GetLogger(Global.CallerName());
                // log.Info("pre-pause polling complete");

                if (--_total[0] == 0)
                {
                    _pause.Invoke();
                }
            }

            public int CompareTo(IListener other)
                => Id.CompareTo(other.Id);
        }

        private void Poll()
        {
            // var log = LogManager.GetLogger(Global.CallerName());
            // log.Info("polling");

            IsPolling = true;
            DoPoll();
            Emit(EVENT_POLL);
        }

        protected override void OnData(string data)
        {
            _onData(data);
        }

        protected override void OnData(byte[] data)
        {
            _onData(data);
        }

        private class DecodePayloadCallback : IDecodePayloadCallback
        {
            private readonly Polling _polling;

            public DecodePayloadCallback(Polling polling)
            {
                _polling = polling;
            }

            public bool Call(Packet packet, int index, int total)
            {
                if (_polling.ReadyState == ReadyStateEnum.OPENING)
                {
                    _polling.OnOpen();
                }

                if (packet.Type == Packet.CLOSE)
                {
                    _polling.OnClose();
                    return false;
                }

                _polling.OnPacket(packet);
                return true;
            }
        }

        private void _onData(object data)
        {
            var log = LogManager.GetLogger(Global.CallerName());

            log.Info(string.Format("polling got data {0}", data));
            var callback = new DecodePayloadCallback(this);
            if (data is string)
            {
                Parser.Parser.DecodePayload((string)data, callback);
            }
            else if (data is byte[])
            {
                Parser.Parser.DecodePayload((byte[])data, callback);
            }

            if (ReadyState != ReadyStateEnum.CLOSED)
            {
                IsPolling = false;
                log.Info("ReadyState != ReadyStateEnum.CLOSED");
                Emit(EVENT_POLL_COMPLETE);

                if (ReadyState == ReadyStateEnum.OPEN)
                {
                    Poll();
                }
                else
                {
                    log.Info(string.Format("ignoring poll - transport state {0}", ReadyState));
                }
            }
        }

        private class CloseListener : IListener
        {
            private readonly Polling _polling;

            public int Id { get; } = 0;

            public CloseListener(Polling polling)
            {
                _polling = polling;
            }

            public void Call(params object[] args)
            {
                // var log = LogManager.GetLogger(Global.CallerName());
                // log.Info("writing close packet");

                var packets = ImmutableList<Packet>.Empty;
                packets = packets.Add(new Packet(Packet.CLOSE));
                _polling.Write(packets);
            }

            public int CompareTo(IListener other)
                => Id.CompareTo(other.Id);
        }

        protected override void DoClose()
        {
            var log = LogManager.GetLogger(Global.CallerName());

            var closeListener = new CloseListener(this);

            if (ReadyState == ReadyStateEnum.OPEN)
            {
                log.Info("transport open - closing");
                closeListener.Call();
            }
            else
            {
                // in case we're trying to close while
                // handshaking is in progress (engine.io-client GH-164)
                log.Info("transport not open - deferring close");
                Once(EVENT_OPEN, closeListener);
            }
        }

        public class SendEncodeCallback : IEncodeCallback
        {
            private readonly Polling _polling;

            public SendEncodeCallback(Polling polling)
            {
                _polling = polling;
            }

            public void Call(object data)
            {
                // var log = LogManager.GetLogger(Global.CallerName());
                // log.Info("SendEncodeCallback data = " + data);

                var byteData = (byte[])data;
                _polling.DoWrite(byteData, () =>
                {
                    _polling.Writable = true;
                    _polling.Emit(EVENT_DRAIN);
                });
            }
        }

        protected override void Write(ImmutableList<Packet> packets)
        {
            var log = LogManager.GetLogger(Global.CallerName());
            log.Info("Write packets.Count = " + packets.Count);

            Writable = false;

            var callback = new SendEncodeCallback(this);
            Parser.Parser.EncodePayload(packets.ToArray(), callback);
        }

        public string Uri()
        {
            //var query = Query;
            var query = new Dictionary<string, string>(Query);
            //if (Query == null)
            //{
            //    query = new Dictionary<string, string>();
            //}
            string schema = Secure ? "https" : "http";
            string portString = "";

            if (TimestampRequests)
            {
                query.Add(TimestampParam, $"{DateTime.Now.Ticks}-{Timestamps++}");
            }

            query.Add("b64", "1");

            string _query = ParseQS.Encode(query);

            if (Port > 0 && (("https" == schema && Port != 443) || ("http" == schema && Port != 80)))
            {
                portString = ":" + Port;
            }

            if (_query.Length > 0)
            {
                _query = "?" + _query;
            }

            return schema + "://" + Hostname + portString + Path + _query;
        }

        protected virtual void DoWrite(byte[] data, Action action)
        {
        }

        protected virtual void DoPoll()
        {
        }
    }
}
