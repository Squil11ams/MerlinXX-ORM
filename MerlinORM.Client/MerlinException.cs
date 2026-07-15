using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MerlinORM.Client
{
    public class MerlinException : Exception
    {
        public string ModuleCode { get; private set; }
        public string ModuleName { get; private set; }
        public int ErrorNumber { get; private set; }
        public string SafeMsgRaw { get; private set; }
        public string ErrorCode { get; private set; }
        public string UserSafeMessage { get; private set; }


        public MerlinException() { }

        public MerlinException(string errorCode, Exception? innerException = null) : base(null, innerException)
        {
            ProcessErrorCode(errorCode);
        }

        public MerlinException(string errorCode, string message, Exception? innerException = null) : base(message, innerException)
        {
            ProcessErrorCode(errorCode);
        }


        private void ProcessErrorCode(string errorCode)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
            {
                ModuleCode = "SYS";
                ErrorNumber = 0;
                ModuleName = "System";
                SafeMsgRaw = "An unexpected system error occurred.";
                UserSafeMessage = $"[{ModuleName}] {SafeMsgRaw}";
                return;
            }

            // 1. Decode Error Code Into Module / ErrorNumber
            var parts = errorCode.Split('-');

            if (parts.Length != 3)
            {
                ModuleCode = "SYS";
                ErrorNumber = 0;
                ModuleName = "System";
                SafeMsgRaw = "An unexpected system error occurred.";
                UserSafeMessage = $"[{ModuleName}] {SafeMsgRaw}";
                return;
            }

            ModuleCode = parts[1].ToUpperInvariant();
            ErrorCode = errorCode.ToUpperInvariant();

            if (int.TryParse(parts[2], out int errorNum))
            {
                ErrorNumber = errorNum;
            }
            else
            {
                ErrorNumber = 0;
            }

            // 2. Resolve Module Name
            ModuleName = ErrorModules.ResourceManager.GetString(ModuleCode) ?? ModuleCode;

            // 3. Resolve Raw Safe Message
            var resourceKey = $"MERLIN_{ModuleCode}_{ErrorNumber}";
            SafeMsgRaw = ErrorCodes.ResourceManager.GetString(resourceKey)
                                ?? "An unexpected system operation error occurred.";

            // 4. Build Formatted Safe Message
            UserSafeMessage = $"[{ModuleName}] {SafeMsgRaw} (Error Code: {ErrorCode})";
        }
    }
}
