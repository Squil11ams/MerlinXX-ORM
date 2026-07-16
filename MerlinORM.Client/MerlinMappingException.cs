using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    /// <summary>
    /// Mapping exception
    /// </summary>
    public class MerlinMappingException : MerlinException
    {
        /// <summary>
        /// The exception that caused the fallback condition attempt.
        /// </summary>
        public Exception? OriginalException { get; init; }

        /// <summary>
        /// Mapping Exception, covers standard mapping issues and fallback mapping issues.
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="message"></param>
        /// <param name="innerException">The exception that triggered this exception</param>
        /// <param name="originalException">The exception that triggered the fallback method.</param>
        public MerlinMappingException(string errorCode, string message, Exception innerException, Exception? originalException = null) 
            : base(errorCode, message, innerException)
        {
            OriginalException = originalException;
        }
    }
}
