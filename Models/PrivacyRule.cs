using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TavrynexoAntiSpy.Models
{
    public enum RecommendationLevel
    {
        Recommended,
        Optional,
        Advanced
    }

    public enum RuleCategory
    {
        All,
        Telemetry,
        Services,
        CortanaAndSearch,
        AdvertisingAndTracking,
        LocationAndSensors,
        WindowsUpdate,
        DiagnosticsAndFeedback
    }

    public enum RuleTargetType
    {
        RegistryDWord,
        RegistryString,
        WindowsService,
        ScheduledTask,
        HostsDomain
    }

    public enum RegistryHiveType
    {
        LocalMachine,
        CurrentUser
    }

    public class PrivacyRule : INotifyPropertyChanged
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RuleCategory Category { get; set; }
        public RecommendationLevel Recommendation { get; set; }
        public RuleTargetType TargetType { get; set; }
        
        public RegistryHiveType Hive { get; set; } = RegistryHiveType.LocalMachine;
        public string KeyPath { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;
        public object DesiredValue { get; set; } = 0;
        public object? DefaultWindowsValue { get; set; } = 1;

        public string? ServiceName { get; set; }
        public string? TaskPath { get; set; }

        private bool _isApplied;
        public bool IsApplied
        {
            get => _isApplied;
            set
            {
                if (_isApplied != value)
                {
                    _isApplied = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusDisplay));
                }
            }
        }

        public string StatusDisplay => _isApplied ? "Protected" : "Exposed";

        private string? _lastVerificationMessage;
        public string? LastVerificationMessage
        {
            get => _lastVerificationMessage;
            set
            {
                _lastVerificationMessage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
