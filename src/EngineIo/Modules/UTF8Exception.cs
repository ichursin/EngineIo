using System;

namespace EngineIo.Modules
{
    public class UTF8Exception : Exception
    {
        public UTF8Exception()
        {
        }

        public UTF8Exception(string message)
            : base(message)
        {
        }

        public UTF8Exception(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
