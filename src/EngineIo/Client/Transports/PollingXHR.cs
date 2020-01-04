﻿using EngineIo.ComponentEmitter;
using EngineIo.Modules;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Net.Http.Headers;

namespace EngineIo.Client.Transports
{
    public class PollingXHR : Polling
    {
        private XHRRequest sendXhr;

        public PollingXHR(Options options)
            : base(options)
        {
        }

        protected XHRRequest Request()
        {
            return Request(null);
        }

        protected XHRRequest Request(XHRRequest.RequestOptions opts)
        {
            if (opts == null)
            {
                opts = new XHRRequest.RequestOptions();
            }

            opts.Uri = Uri();

            var req = new XHRRequest(opts);

            req.On(EVENT_REQUEST_HEADERS, new EventRequestHeadersListener(this)).
                On(EVENT_RESPONSE_HEADERS, new EventResponseHeadersListener(this));

            return req;
        }

        private class EventRequestHeadersListener : IListener
        {
            private readonly PollingXHR _pollingXHR;

            public int Id { get; } = 0;

            public EventRequestHeadersListener(PollingXHR pollingXHR)
            {
                _pollingXHR = pollingXHR;
            }

            public void Call(params object[] args)
            {
                // Never execute asynchronously for support to modify headers.
                _pollingXHR.Emit(EVENT_RESPONSE_HEADERS, args[0]);
            }

            public int CompareTo(IListener other)
                => Id.CompareTo(other.Id);
        }

        private class EventResponseHeadersListener : IListener
        {
            private readonly PollingXHR _pollingXHR;

            public int Id { get; } = 0;

            public EventResponseHeadersListener(PollingXHR pollingXHR)
            {
                _pollingXHR = pollingXHR;
            }

            public void Call(params object[] args)
            {
                _pollingXHR.Emit(EVENT_REQUEST_HEADERS, args[0]);
            }

            public int CompareTo(IListener other)
                => Id.CompareTo(other.Id);
        }

        protected override void DoWrite(byte[] data, Action action)
        {
            var opts = new XHRRequest.RequestOptions
            {
                Method = "POST",
                Data = data,
                CookieHeaderValue = Cookie
            };

            var log = LogManager.GetLogger();
            log.Info("DoWrite data = " + data);

            // try
            // {
            //     var dataString = BitConverter.ToString(data);
            //     log.Info(string.Format("DoWrite data {0}", dataString));
            // }
            // catch (Exception e)
            // {
            //     log.Error(e);
            // }

            sendXhr = Request(opts);
            sendXhr.On(EVENT_SUCCESS, new SendEventSuccessListener(action));
            sendXhr.On(EVENT_ERROR, new SendEventErrorListener(this));
            sendXhr.Create();
        }

        private class SendEventErrorListener : IListener
        {
            private readonly PollingXHR _pollingXHR;

            public int Id { get; } = 0;

            public SendEventErrorListener(PollingXHR pollingXHR)
            {
                _pollingXHR = pollingXHR;
            }

            public void Call(params object[] args)
            {
                var err = args.Length > 0 && args[0] is Exception ? (Exception)args[0] : null;
                _pollingXHR.OnError("xhr post error", err);
            }

            public int CompareTo(IListener other)
                => Id.CompareTo(other.Id);
        }

        private class SendEventSuccessListener : IListener
        {
            private readonly Action _action;

            public int Id { get; } = 0;

            public SendEventSuccessListener(Action action)
            {
                _action = action;
            }

            public void Call(params object[] args)
            {
                _action?.Invoke();
            }

            public int CompareTo(IListener other)
                => Id.CompareTo(other.Id);
        }

        protected override void DoPoll()
        {
            var log = LogManager.GetLogger();
            log.Info("xhr poll");
            var opts = new XHRRequest.RequestOptions { CookieHeaderValue = Cookie };
            sendXhr = Request(opts);
            sendXhr.On(EVENT_DATA, new DoPollEventDataListener(this));
            sendXhr.On(EVENT_ERROR, new DoPollEventErrorListener(this));
            // sendXhr.Create();
            sendXhr.Create();
        }

        private class DoPollEventDataListener : IListener
        {
            private readonly PollingXHR _pollingXHR;

            public int Id { get; } = 0;

            public DoPollEventDataListener(PollingXHR pollingXHR)
            {
                _pollingXHR = pollingXHR;
            }

            public void Call(params object[] args)
            {
                var arg = args.Length > 0 ? args[0] : null;

                if (arg is string)
                {
                    _pollingXHR.OnData((string)arg);
                }
                else if (arg is byte[])
                {
                    _pollingXHR.OnData((byte[])arg);
                }
            }

            public int CompareTo(IListener other)
                => Id.CompareTo(other.Id);
        }

        private class DoPollEventErrorListener : IListener
        {
            private readonly PollingXHR _pollingXHR;

            public int Id { get; } = 0;

            public DoPollEventErrorListener(PollingXHR pollingXHR)
            {
                _pollingXHR = pollingXHR;
            }

            public void Call(params object[] args)
            {
                var err = args.Length > 0 && args[0] is Exception ? (Exception)args[0] : null;
                _pollingXHR.OnError("xhr poll error", err);
            }

            public int CompareTo(IListener other)
                => Id.CompareTo(other.Id);
        }

        public class XHRRequest : Emitter
        {
            private readonly string Method;
            private readonly string Uri;
            private readonly byte[] Data;
            private readonly string CookieHeaderValue;
            private readonly IDictionary<string, string> ExtraHeaders;

            public XHRRequest(RequestOptions options)
            {
                Method = options.Method ?? "GET";
                Uri = options.Uri;
                Data = options.Data;
                CookieHeaderValue = options.CookieHeaderValue;
                ExtraHeaders = options.ExtraHeaders;
            }

            public void Create()
            {
                var httpMethod = Method == "POST" ? HttpMethod.Post : HttpMethod.Get;
                var dataToSend = Data ?? Encoding.UTF8.GetBytes("");

                Task.Run(async () =>
                {
                    try
                    {
                        using (var httpClientHandler = new HttpClientHandler())
                        {
                            if (ServerCertificate.Ignore)
                            {
                                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                            }

                            using (var client = new HttpClient(httpClientHandler))
                            {
                                using (var httpContent = new ByteArrayContent(dataToSend))
                                {
                                    if (Method == "POST")
                                    {
                                        httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                                    }

                                    var request = new HttpRequestMessage(httpMethod, Uri)
                                    {
                                        Content = httpContent
                                    };

                                    if (!string.IsNullOrEmpty(CookieHeaderValue))
                                    {
                                        httpContent.Headers.Add(@"Cookie", CookieHeaderValue);
                                    }

                                    if (ExtraHeaders != null)
                                    {
                                        foreach (var header in ExtraHeaders)
                                        {
                                            httpContent.Headers.Add(header.Key, header.Value);
                                        }
                                    }


                                    using (HttpResponseMessage response = await client.SendAsync(request))
                                    {
                                        response.EnsureSuccessStatusCode();
                                        var contentType = response.Content.Headers.GetValues("Content-Type").Aggregate("", (acc, x) => acc + x).Trim();

                                        if (contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var responseContent = await response.Content.ReadAsByteArrayAsync();
                                            OnData(responseContent);
                                        }
                                        else
                                        {
                                            var responseContent = await response.Content.ReadAsStringAsync();
                                            OnData(responseContent);
                                        }

                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        OnError(e);
                    }
                }).Wait();
            }

            private void OnSuccess()
            {
                Emit(EVENT_SUCCESS);
            }

            private void OnData(string data)
            {
                // var log = LogManager.GetLogger();
                // log.Info("OnData string = " + data);

                Emit(EVENT_DATA, data);
                OnSuccess();
            }

            private void OnData(byte[] data)
            {
                // var log = LogManager.GetLogger();
                // log.Info(string.Format("OnData byte[] ={0}", System.Text.Encoding.UTF8.GetString(data, 0, data.Length)));

                Emit(EVENT_DATA, data);
                OnSuccess();
            }

            private void OnError(Exception err)
            {
                Emit(EVENT_ERROR, err);
            }

            private void OnRequestHeaders(IDictionary<string, string> headers)
            {
                Emit(EVENT_REQUEST_HEADERS, headers);
            }

            private void OnResponseHeaders(IDictionary<string, string> headers)
            {
                Emit(EVENT_RESPONSE_HEADERS, headers);
            }

            public class RequestOptions
            {
                public string Uri;
                public string Method;
                public byte[] Data;
                public string CookieHeaderValue;
                public Dictionary<string, string> ExtraHeaders;
            }
        }
    }
}