using System;

namespace EngineIo.Client
{
    public class EngineIOException : Exception
    {
        public string Transport;
        public object code;

        public EngineIOException()
        {
        }

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