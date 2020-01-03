﻿using EngineIo.Modules;
using Xunit;


namespace EngineIo.Tests.ModulesTests
{
    public class Utf8Tests
    {
        private static readonly Data[] DATA = new Data[]
        {
            // 1-byte
            new Data(0x0000, "\x00", "\x00"),
            new Data(0x005c, "\u005C\u005C", "\u005C\u005C"), // = backslash
            new Data(0x007f, "\u007F", "\u007F"),
            // 2-byte
            new Data(0x0080, "\u0080", "\u00C2\u0080"),
            new Data(0x05CA, "\u05CA", "\u00D7\u008A"),
            new Data(0x07FF, "\u07FF", "\u00DF\u00BF"),
            // 3-byte
            new Data(0x0800, "\u0800", "\u00E0\u00A0\u0080"),
            new Data(0x2C3C, "\u2C3C", "\u00E2\u00B0\u00BC"),
            new Data(0x07FF, "\uFFFF", "\u00EF\u00BF\u00BF"),
            // unmatched surrogate halves
            // high surrogates: 0xD800 to 0xDBFF
            new Data(0xD800, "\uD800", "\u00ED\u00A0\u0080"),
            new Data("High surrogate followed by another high surrogate",
                "\uD800\uD800", "\u00ED\u00A0\u0080\u00ED\u00A0\u0080"),
            new Data("High surrogate followed by a symbol that is not a surrogate",
                "\uD800A", "\u00ED\u00A0\u0080A"),
            new Data(
                "Unmatched high surrogate, followed by a surrogate pair, followed by an unmatched high surrogate",
                "\uD800\uD834\uDF06\uD800", "\u00ED\u00A0\u0080\u00F0\u009D\u008C\u0086\u00ED\u00A0\u0080"),
            new Data(0xD9AF, "\uD9AF", "\u00ED\u00A6\u00AF"),
            new Data(0xDBFF, "\uDBFF", "\u00ED\u00AF\u00BF"),
            // low surrogates: 0xDC00 to 0xDFFF
            new Data(0xDC00, "\uDC00", "\u00ED\u00B0\u0080"),
            new Data("Low surrogate followed by another low surrogate",
                "\uDC00\uDC00", "\u00ED\u00B0\u0080\u00ED\u00B0\u0080"),
            new Data("Low surrogate followed by a symbol that is not a surrogate",
                "\uDC00A", "\u00ED\u00B0\u0080A"),
            new Data(
                "Unmatched low surrogate, followed by a surrogate pair, followed by an unmatched low surrogate",
                "\uDC00\uD834\uDF06\uDC00", "\u00ED\u00B0\u0080\u00F0\u009D\u008C\u0086\u00ED\u00B0\u0080"),
            new Data(0xDEEE, "\uDEEE", "\u00ED\u00BB\u00AE"),
            new Data(0xDFFF, "\uDFFF", "\u00ED\u00BF\u00BF"),
            // 4-byte
            new Data(0x010000, "\uD800\uDC00", "\u00F0\u0090\u0080\u0080"),
            new Data(0x01D306, "\uD834\uDF06", "\u00F0\u009D\u008C\u0086"),
            new Data(0x010FFF, "\uDBFF\uDFFF", "\u00F4\u008F\u00BF\u00BF"),
        };

        [Fact]
        public void ShouldCorrectEncode()
        {
            foreach (var data in DATA)
            {
                Assert.Equal(data.Encoded, UTF8.Encode(data.Decoded));
            }
        }

        [Fact(Skip = "Should configure server side")]
        public void ShouldCorrectDecode()
        {
            foreach (var data in DATA)
            {
                Assert.Equal(data.Decoded, UTF8.Decode(data.Encoded));
            }

            Assert.Throws<UTF8Exception>(delegate { UTF8.Decode("\uFFFF"); });
            Assert.Throws<UTF8Exception>(delegate { UTF8.Decode("\xE9\x00\x00"); });
            Assert.Throws<UTF8Exception>(delegate { UTF8.Decode("\xC2\uFFFF"); });
            Assert.Throws<UTF8Exception>(delegate { UTF8.Decode("\xF0\x9D"); });
        }

        private class Data
        {
            public string Description { get; }
            public string Decoded { get; }
            public string Encoded { get; }

            public Data(int codePoint, string decoded, string encoded)
                : this($"U+{codePoint.ToString("X4").ToUpper()}", decoded, encoded)
            {
            }

            public Data(string description, string decoded, string encoded)
            {
                Description = description;
                Decoded = decoded;
                Encoded = encoded;
            }
        }
    }
}
