using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TavrynexoAntiSpy.Models;
using TavrynexoAntiSpy.Services;

namespace TavrynexoAntiSpy.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PrivacyRule> AllRules { get; } = new();
        public ObservableCollection<PrivacyRule> FilteredRules { get; } = new();
        public ObservableCollection<PrivacyRule> ProposedRulesToApply { get; } = new();
        public ObservableCollection<AuditLogEntry> AuditLogs => AuditLogger.LogEntries;

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set { if (_searchQuery != value) { _searchQuery = value; OnPropertyChanged(); FilterRules(); } }
        }

        private string _selectedNavSection = "Overview";
        public string SelectedNavSection
        {
            get => _selectedNavSection;
            set
            {
                if (_selectedNavSection != value)
                {
                    _selectedNavSection = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsRulesViewVisible));
                    OnPropertyChanged(nameof(IsSecurityViewVisible));
                    FilterRules();
                }
            }
        }

        public bool IsRulesViewVisible => SelectedNavSection != "Security" && SelectedNavSection != "AuditLogs";
        public bool IsSecurityViewVisible => SelectedNavSection == "Security";

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private int _privacyScore;
        public int PrivacyScore
        {
            get => _privacyScore;
            set { _privacyScore = value; OnPropertyChanged(); }
        }

        public string WindowsVersionInfo => $"{Environment.OSVersion.VersionString} ({(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")})";

        public ICommand ScanCommand { get; }
        public ICommand OpenReviewProposedCommand { get; }
        public ICommand RevertAllCommand { get; }
        public ICommand CreateRestorePointCommand { get; }
        public ICommand ToggleRuleCommand { get; }
        public ICommand SelectNavCommand { get; }

        public MainViewModel()
        {
            ScanCommand = new RelayCommand(async () => await ScanSystemAsync());
            OpenReviewProposedCommand = new RelayCommand(ApplyRecommended);
            RevertAllCommand = new RelayCommand(async () => await RevertAllToWindowsDefaultAsync());
            CreateRestorePointCommand = new RelayCommand(async () => await CreateRestorePointAsync());
            ToggleRuleCommand = new RelayCommand(async (param) => { if (param is PrivacyRule r) await ToggleRuleAsync(r); });
            SelectNavCommand = new RelayCommand((param) => { if (param is string s) SelectedNavSection = s; });

            foreach (var r in PrivacyRulesRepository.GetAllRules()) AllRules.Add(r);
            FilterRules();
            _ = ScanSystemAsync();
        }

        public async Task ScanSystemAsync()
        {
            StatusMessage = "Scanning system...";
            await Task.Run(() =>
            {
                foreach (var rule in AllRules)
                {
                    if (rule.TargetType == RuleTargetType.WindowsService && !string.IsNullOrEmpty(rule.ServiceName))
                    {
                        rule.IsApplied = ServiceManager.IsServiceDisabled(rule.ServiceName);
                        rule.LastVerificationMessage = rule.IsApplied ? "Service stopped & disabled" : "Service active";
                    }
                    else
                    {
                        var actualValue = RegistryManager.ReadValue(rule.Hive, rule.KeyPath, rule.ValueName);
                        rule.IsApplied = actualValue != null && actualValue.ToString() == rule.DesiredValue.ToString();
                        rule.LastVerificationMessage = rule.IsApplied ? "Protected" : "Exposed";
                    }
                }
            });
            UpdateMetrics();
            StatusMessage = "Scan complete.";
        }

        public async Task ToggleRuleAsync(PrivacyRule rule)
        {
            StatusMessage = $"Configuring {rule.Title}...";
            await Task.Run(() =>
            {
                if (!rule.IsApplied)
                {
                    if (rule.TargetType == RuleTargetType.WindowsService && !string.IsNullOrEmpty(rule.ServiceName))
                    {
                        var res = ServiceManager.DisableAndStopService(rule.ServiceName);
                        rule.IsApplied = res.Success;
                    }
                    else
                    {
                        var res = RegistryManager.WriteAndVerify(rule.Hive, rule.KeyPath, rule.ValueName, rule.DesiredValue);
                        rule.IsApplied = res.Success;
                    }
                }
                else
                {
                    if (rule.TargetType == RuleTargetType.WindowsService && !string.IsNullOrEmpty(rule.ServiceName))
                    {
                        var res = ServiceManager.EnableAndStartService(rule.ServiceName);
                        rule.IsApplied = !res.Success;
                    }
                    else
                    {
                        var res = RegistryManager.RevertValue(rule.Hive, rule.KeyPath, rule.ValueName, rule.DefaultWindowsValue);
                        rule.IsApplied = !res.Success;
                    }
                }
            });
            UpdateMetrics();
            StatusMessage = "Updated.";
        }

        public void ApplyRecommended()
        {
            var recommended = AllRules.Where(r => r.Recommendation == RecommendationLevel.Recommended && !r.IsApplied).ToList();
            foreach (var r in recommended) _ = ToggleRuleAsync(r);
            MessageBox.Show("Applied recommended settings.", "Tavrynexo AntiSpy", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async Task RevertAllToWindowsDefaultAsync()
        {
            foreach (var r in AllRules.Where(r => r.IsApplied)) await ToggleRuleAsync(r);
            MessageBox.Show("Restored defaults.", "Tavrynexo AntiSpy", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async Task CreateRestorePointAsync()
        {
            StatusMessage = "Creating restore point...";
            var (success, msg) = await RestorePointService.CreateRestorePointAsync("Tavrynexo Checkpoint");
            StatusMessage = msg;
            MessageBox.Show(msg, "Restore Point", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FilterRules()
        {
            FilteredRules.Clear();
            var query = AllRules.AsEnumerable();
            if (SelectedNavSection == "Telemetry") query = query.Where(r => r.Category == RuleCategory.Telemetry);
            else if (SelectedNavSection == "Services") query = query.Where(r => r.Category == RuleCategory.Services);
            else if (SelectedNavSection == "Advertising") query = query.Where(r => r.Category == RuleCategory.AdvertisingAndTracking);
            else if (SelectedNavSection == "Cortana") query = query.Where(r => r.Category == RuleCategory.CortanaAndSearch);
            else if (SelectedNavSection == "Location") query = query.Where(r => r.Category == RuleCategory.LocationAndSensors);
            else if (SelectedNavSection == "WindowsUpdate") query = query.Where(r => r.Category == RuleCategory.WindowsUpdate);

            if (!string.IsNullOrWhiteSpace(SearchQuery))
                query = query.Where(r => r.Title.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));

            foreach (var r in query) FilteredRules.Add(r);
        }

        private void UpdateMetrics()
        {
            int protectedCount = AllRules.Count(r => r.IsApplied);
            PrivacyScore = AllRules.Count > 0 ? (int)Math.Round((double)protectedCount / AllRules.Count * 100) : 0;
            OnPropertyChanged(nameof(PrivacyScore));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
