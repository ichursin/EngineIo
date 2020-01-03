using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace EngineIo.Modules
{
    public static class Global
    {
        private static readonly Regex _invalidCharacters = new Regex("([\ud800-\udbff](?![\udc00-\udfff]))|((?<![\ud800-\udbff])[\udc00-\udfff])", RegexOptions.Compiled);

        public static string EncodeURIComponent(string str)
        {
            // http://stackoverflow.com/a/4550600/1109316
            return Uri.EscapeDataString(str);
        }

        public static string DecodeURIComponent(string str)
        {
            return Uri.UnescapeDataString(str);
        }

        public static string CallerName([CallerMemberName]string caller = "", [CallerLineNumber]int number = 0, [CallerFilePath]string path = "")
        {
            var s = path.Split('\\');
            var fileName = s.LastOrDefault();
            if (path.Contains("SocketIo.Tests"))
            {
                path = "SocketIo.Tests";
            }
            else if (path.Contains("SocketIo"))
            {
                path = "SocketIo";
            }
            else if (path.Contains("EngineIo"))
            {
                path = "EngineIo";
            }

            return string.Format("{0}-{1}:{2}#{3}", path, fileName, caller, number);
        }

        // from http://stackoverflow.com/questions/8767103/how-to-remove-invalid-code-points-from-a-string
        public static string StripInvalidUnicodeCharacters(string str)
        {
            return _invalidCharacters.Replace(str, "");
        }
    }
}
