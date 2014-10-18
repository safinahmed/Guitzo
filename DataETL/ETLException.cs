using System;

namespace DataETLTest 
{
    public class ETLException : Exception
    {
        public ETLException()
        {
        }

        public ETLException(string message) : base(message)
        {
        }

        public ETLException(string message, Exception exception) : base(message, exception)
        {
        }
    }
}
