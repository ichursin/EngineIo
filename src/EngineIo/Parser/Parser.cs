using System;
using System.Collections.Generic;

namespace EngineIo.Parser
{
    /// <remarks>
    /// This is the JavaScript parser for the engine.io protocol encoding, 
    /// shared by both engine.io-client and engine.io.
    /// <see href="https://github.com/Automattic/engine.io-parser">https://github.com/Automattic/engine.io-parser</see>
    /// </remarks>
    public static class Parser
    {
        public static readonly int Protocol = 3;

        private static readonly IDictionary<string, byte> _packets = new Dictionary<string, byte>()
        {
            {Packet.OPEN, 0},
            {Packet.CLOSE, 1},
            {Packet.PING, 2},
            {Packet.PONG, 3},
            {Packet.MESSAGE, 4},
            {Packet.UPGRADE, 5},
            {Packet.NOOP, 6}
        };

        private static readonly IDictionary<byte, string> _packetsList = new Dictionary<byte, string>();

        static Parser()
        {
            foreach (var entry in _packets)
            {
                _packetsList.Add(entry.Value, entry.Key);
            }
        }

        public static void EncodePacket(Packet packet, IEncodeCallback callback)
        {
            packet.Encode(callback);
        }

        public static Packet DecodePacket(string data, bool utf8decode = false)
        {
            return Packet.DecodePacket(data, utf8decode);
        }

        public static Packet DecodePacket(byte[] data)
        {
            var type = data[0];
            var payload = data[1..];

            return new Packet(_packetsList[type], payload);
        }

        public static void EncodePayload(Packet[] packets, IEncodeCallback callback)
        {
            Packet.EncodePayload(packets, callback);
        }

        public static void DecodePayload(string data, IDecodePayloadCallback callback)
        {
            Packet.DecodePayload(data, callback);
        }

        public static void DecodePayload(byte[] data, IDecodePayloadCallback callback)
        {
            Packet.DecodePayload(data, callback);
        }
    }
}
