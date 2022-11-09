using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MerlinXX.Client
{
    public class MerlinException : Exception
    {
        public string ErrorCode { get; set; }

        public MerlinException(string ErrorCode, string Message):base(Message)
        {
            this.ErrorCode = ErrorCode;
        }

        public MerlinException(string ErrorCode, string Message, Exception InnerException):base(Message, InnerException)
        {
            this.ErrorCode = ErrorCode;
        }

        public override string Message
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("MerlinXX Exception");
                sb.AppendLine("Error Code: " + ErrorCode);
                sb.AppendLine(base.Message);

                return sb.ToString();
            }
        }
    }
}
