using System;

namespace TavrynexoAntiSpy.Models
{
    public class AuditLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string RuleId { get; set; } = string.Empty;
        public string RuleTitle { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;

        public override string ToString()
        {
            var status = IsSuccess ? "[SUCCESS]" : "[FAILED]";
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {status} {ActionType} on '{RuleTitle}': {Message}";
        }
    }
}
