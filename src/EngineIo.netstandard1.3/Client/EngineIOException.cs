using System;

namespace Quobject.EngineIoClientDotNet.Client
{
    public class EngineIOException : Exception
    {
        public string Transport;
        public object code;

        public EngineIOException(string message)
            : base(message)
        {
        }

        public EngineIOException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}