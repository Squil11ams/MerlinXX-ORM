using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    public class MerlinMappingException : MerlinException
    {
        /// <summary>
        /// The exception that caused the fallback condition attempt.
        /// </summary>
        public Exception? OriginalException { get; init; }

        public MerlinMappingException(string errorCode, string message, Exception innerException, Exception? originalException = null) 
            : base(errorCode, message, innerException)
        {
            OriginalException = originalException;
        }
    }
}
