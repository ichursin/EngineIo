using System;

namespace EngineIo.Client
{
    public class EngineIoException : Exception
    {
        public string Transport;
        public object code;

        public EngineIoException()
        {
        }

        public EngineIoException(string message)
            : base(message)
        {
        }

        public EngineIoException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}