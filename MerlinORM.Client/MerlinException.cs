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


        public string ToHTMLString(bool includeDiagnostics = false)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<div class=\"merlin-exception\">");

            sb.AppendLine($"<h3>{HtmlEncode(ErrorCode)}</h3>");

            sb.AppendLine("<table>");
            sb.AppendLine($"<tr><td><strong>Type</strong></td><td>{HtmlEncode(this.GetType().Name)}</td></tr>");
            sb.AppendLine($"<tr><td><strong>Module</strong></td><td>{HtmlEncode(ModuleName)}</td></tr>");
            sb.AppendLine($"<tr><td><strong>Message</strong></td><td>{HtmlEncode(UserSafeMessage)}</td></tr>");

            if (!string.IsNullOrWhiteSpace(Message))
            {
                sb.AppendLine($"<tr><td><strong>Notes</strong></td><td>{HtmlEncode(Message)}</td></tr>");
            }

            sb.AppendLine("</table>");

            if (includeDiagnostics)
            {
                sb.AppendLine("<hr/>");
                sb.AppendLine("<h4>Diagnostics</h4>");

                sb.AppendLine("<table>");

                sb.AppendLine($"<tr><td><strong>Type Full</strong></td><td>{HtmlEncode(this.GetType().FullName)}</td></tr>");

                if (TargetSite != null)
                {
                    sb.AppendLine(
                        $"<tr><td><strong>Target</strong></td><td>{HtmlEncode(TargetSite.ToString())}</td></tr>");
                }

                if (!string.IsNullOrWhiteSpace(Source))
                {
                    sb.AppendLine(
                        $"<tr><td><strong>Source</strong></td><td>{HtmlEncode(Source)}</td></tr>");
                }

                sb.AppendLine("</table>");

                if (InnerException != null)
                {
                    sb.AppendLine("<h4>Inner Exception</h4>");
                    sb.AppendLine($"<pre>{HtmlEncode(InnerException.ToString())}</pre>");
                }

                sb.AppendLine("<h4>Stack Trace</h4>");
                sb.AppendLine($"<pre>{HtmlEncode(StackTrace ?? string.Empty)}</pre>");
            }

            sb.AppendLine("</div>");

            return sb.ToString();
        }

        private static string HtmlEncode(string? value)
        {
            return System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
        }

        public string ToString(bool includeDiagnostics)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Exception Type: {GetType().Name}");
            sb.AppendLine($"Code: {ErrorCode}");
            sb.AppendLine($"Module: {ModuleName}");
            sb.AppendLine($"Message: {UserSafeMessage}");

            if (!string.IsNullOrWhiteSpace(Message))
            {
                sb.AppendLine($"Notes: {Message}");
            }

            if (includeDiagnostics)
            {
                sb.AppendLine($"Type: {GetType().FullName}");
                sb.AppendLine($"Target: {TargetSite}");
                sb.AppendLine($"Source: {Source}");

                if (InnerException != null)
                {
                    sb.AppendLine("[Inner Exception]");
                    sb.AppendLine(InnerException.ToString());
                }

                sb.AppendLine("[Stack Trace]");
                sb.AppendLine(StackTrace);
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return ToString(false);
        }
    }
}
