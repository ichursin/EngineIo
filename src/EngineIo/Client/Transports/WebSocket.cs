using EngineIo.Modules;
using EngineIo.Parser;
using System;
using System.Collections.Generic;
using WebSocket4Net;

namespace EngineIo.Client.Transports
{
    public class WebSocket : Transport
    {
        public const string NAME = "websocket";

        private WebSocket4Net.WebSocket ws;
        private List<KeyValuePair<string, string>> Cookies;
        private List<KeyValuePair<string, string>> MyExtraHeaders;

        public WebSocket(Options opts)
            : base(opts)
        {
            Name = NAME;

            Cookies = new List<KeyValuePair<string, string>>();
            foreach (var cookie in opts.Cookies)
            {
                Cookies.Add(new KeyValuePair<string, string>(cookie.Key, cookie.Value));
            }

            MyExtraHeaders = new List<KeyValuePair<string, string>>();
            foreach (var header in opts.ExtraHeaders)
            {
                MyExtraHeaders.Add(new KeyValuePair<string, string>(header.Key, header.Value));
            }
        }

        protected override void DoOpen()
        {
            var log = LogManager.GetLogger();
            log.Info("DoOpen uri =" + Uri());

            ws = new WebSocket4Net.WebSocket(Uri(), "", Cookies, MyExtraHeaders)
            {
                EnableAutoSendPing = false
            };

            if (ServerCertificate.Ignore)
            {
                var security = ws.Security;

                if (security != null)
                {
                    security.AllowUnstrustedCertificate = true;
                    security.AllowNameMismatchCertificate = true;
                }
            }

            ws.Opened += ws_Opened;
            ws.Closed += ws_Closed;
            ws.MessageReceived += ws_MessageReceived;
            ws.DataReceived += ws_DataReceived;
            ws.Error += ws_Error;

            ws.Open();
        }

        void ws_DataReceived(object sender, DataReceivedEventArgs e)
        {
            var log = LogManager.GetLogger();
            log.Info("ws_DataReceived " + e.Data);

            OnData(e.Data);
        }

        private void ws_Opened(object sender, EventArgs e)
        {
            var log = LogManager.GetLogger();
            log.Info("ws_Opened " + ws.SupportBinary);

            OnOpen();
        }

        void ws_Closed(object sender, EventArgs e)
        {
            var log = LogManager.GetLogger();
            log.Info("ws_Closed");

            ws.Opened -= ws_Opened;
            ws.Closed -= ws_Closed;
            ws.MessageReceived -= ws_MessageReceived;
            ws.DataReceived -= ws_DataReceived;
            ws.Error -= ws_Error;

            OnClose();
        }

        void ws_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var log = LogManager.GetLogger();
            log.Info("ws_MessageReceived e.Message= " + e.Message);

            OnData(e.Message);
        }

        void ws_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            OnError("websocket error", e.Exception);
        }

        protected override void Write(IList<Packet> packets)
        {
            Writable = false;

            foreach (var packet in packets)
            {
                Parser.Parser.EncodePacket(packet, new WriteEncodeCallback(this));
            }

            // fake drain
            // defer to next tick to allow Socket to clear writeBuffer
            //EasyTimer.SetTimeout(() =>
            //{
            Writable = true;
            Emit(EVENT_DRAIN);
            //}, 1);
        }

        public class WriteEncodeCallback : IEncodeCallback
        {
            private readonly WebSocket _webSocket;

            public WriteEncodeCallback(WebSocket webSocket)
            {
                _webSocket = webSocket;
            }

            public void Call(object data)
            {
                // var log = LogManager.GetLogger();

                if (data is string message)
                {
                    _webSocket.ws.Send(message);
                }

                if (data is byte[] bytes)
                {
                    _webSocket.ws.Send(bytes, 0, bytes.Length);

                    // try
                    // {
                    //     var dataString = BitConverter.ToString(d);
                    //     // log.Info(string.Format("WriteEncodeCallback byte[] data {0}", dataString));
                    // }
                    // catch (Exception e)
                    // {
                    //     log.Error(e);
                    // }
                }
            }
        }

        protected override void DoClose()
        {
            if (ws != null)
            {
                try
                {
                    ws.Close();
                }
                catch (Exception e)
                {
                    var log = LogManager.GetLogger();
                    log.Info("DoClose ws.Close() Exception= " + e.Message);
                }
            }
        }

        public string Uri()
        {
            var query = Query ?? new Dictionary<string, string>();
            var schema = IsSecure ? "wss" : "ws";
            var portString = "";

            if (TimestampRequests)
            {
                query.Add(TimestampParam, $"{DateTime.Now.Ticks}-{Timestamps++}");
            }

            var _query = ParseQS.Encode(query);

            if (Port > 0 && (("wss" == schema && Port != 443) || ("ws" == schema && Port != 80)))
            {
                portString = ":" + Port;
            }

            if (_query.Length > 0)
            {
                _query = "?" + _query;
            }

            return schema + "://" + Hostname + portString + Path + _query;
        }
    }
}
