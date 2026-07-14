using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    public class MerlinException : Exception
    {
        public MerlinException() { }

        public MerlinException(string? message):base(message) { }

        public MerlinException(Exception innerException) : base("",innerException) { }

        public MerlinException(string? message, Exception innerException) : base(message, innerException) { }
    }
}
