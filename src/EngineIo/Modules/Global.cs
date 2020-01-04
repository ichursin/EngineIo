using System;

namespace EngineIo.Modules
{
    public static class Global
    {
        public static string EncodeURIComponent(string str)
        {
            // http://stackoverflow.com/a/4550600/1109316
            return Uri.EscapeDataString(str);
        }

        public static string DecodeURIComponent(string str)
        {
            return Uri.UnescapeDataString(str);
        }
    }
}
