﻿using EngineIo.Client;
using System;
using Xunit;

namespace EngineIo.Tests.ClientTests
{
    public class UsageTest : Connection
    {
        [Fact]
        public void Usage1()
        {
            var options = CreateOptions();
            var socket = new Socket(options);

            //You can use `Socket` to connect:
            //var socket = new Socket("ws://localhost");
            socket.On(Socket.EVENT_OPEN, () =>
            {
                socket.Send("hi");
                socket.Close();
            });
            socket.Open();

            //System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Usage2()
        {
            var options = CreateOptions();
            var socket = new Socket(options);

            //Receiving data
            //var socket = new Socket("ws://localhost:3000");
            socket.On(Socket.EVENT_OPEN, () =>
            {
                socket.On(Socket.EVENT_MESSAGE, (data) => Console.WriteLine((string)data));
            });
            socket.Open();

            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
            socket.Close();
        }
    }
}