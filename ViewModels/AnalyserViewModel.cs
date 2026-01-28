using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using PulseAPK.Services;
using PulseAPK.Utils;

namespace PulseAPK.ViewModels
{
    public partial class AnalyserViewModel : ObservableObject
    {
        private const int MaxLogCharacters = 200_000;
        private readonly Queue<string> _logLines = new Queue<string>();
        private readonly object _logLock = new object();
        private int _logCharCount;
        private bool _logFlushScheduled;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsHintVisible))]
        [NotifyPropertyChangedFor(nameof(HasProject))]
        private string _projectPath;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveReportCommand))]
        private string _consoleLog = Properties.Resources.WaitingForCommand;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunAnalysisCommand))]
        private bool _isRunning;

        private readonly SmaliAnalyserService _analyserService;
        private readonly ReportService _reportService;

        public bool IsHintVisible => string.IsNullOrEmpty(ProjectPath);
        public bool HasProject => !string.IsNullOrWhiteSpace(ProjectPath);

        public AnalyserViewModel()
        {
            _analyserService = new SmaliAnalyserService();
            _reportService = new ReportService();
        }

        partial void OnProjectPathChanged(string value)
        {
            RunAnalysisCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanRunAnalysis))]
        private async Task RunAnalysis()
        {
            if (string.IsNullOrWhiteSpace(ProjectPath))
            {
                MessageBoxUtils.ShowWarning(Properties.Resources.SelectProjectFolder, Properties.Resources.AnalyserHeader);
                return;
            }

            SetConsoleLog("");
            IsRunning = true;

            try
            {
                var result = await _analyserService.AnalyseProjectAsync(ProjectPath, AppendLog);

                AppendLog("");
                AppendLog("=== Analysis Results ===");
                AppendLog("");

                // Root Check Results
                if (result.ActiveCategories.Contains("root_check"))
                {
                    if (result.RootChecks.Any())
                    {
                        AppendLog(Properties.Resources.RootCheckFound);
                        AppendLog(Properties.Resources.FoundIn);
                        foreach (var finding in result.RootChecks)
                        {
                            AppendLog($"  - {GetRelativePath(finding.FilePath, ProjectPath)} (Line {finding.LineNumber}): {finding.Context}");
                        }
                    }
                    else
                    {
                        AppendLog(Properties.Resources.RootCheckNotFound);
                    }
                }
                else
                {
                    AppendLog("Root Check: N/A");
                }
                AppendLog("");

                // Emulator Check Results
                if (result.ActiveCategories.Contains("emulator_check"))
                {
                    if (result.EmulatorChecks.Any())
                    {
                        AppendLog(Properties.Resources.EmulatorCheckFound);
                        AppendLog(Properties.Resources.FoundIn);
                        foreach (var finding in result.EmulatorChecks)
                        {
                            AppendLog($"  - {GetRelativePath(finding.FilePath, ProjectPath)} (Line {finding.LineNumber}): {finding.Context}");
                        }
                    }
                    else
                    {
                        AppendLog(Properties.Resources.EmulatorCheckNotFound);
                    }
                }
                else
                {
                    AppendLog("Emulator Check: N/A");
                }
                AppendLog("");

                // Hardcoded Credentials Results
                if (result.ActiveCategories.Contains("hardcoded_creds"))
                {
                    if (result.HardcodedCredentials.Any())
                    {
                        AppendLog(Properties.Resources.CredentialsFound);
                        AppendLog(Properties.Resources.FoundIn);
                        foreach (var finding in result.HardcodedCredentials)
                        {
                            AppendLog($"  - {GetRelativePath(finding.FilePath, ProjectPath)} (Line {finding.LineNumber}): {finding.Context}");
                        }
                    }
                    else
                    {
                        AppendLog(Properties.Resources.CredentialsNotFound);
                    }
                }
                else
                {
                    AppendLog("Hardcoded Credentials: N/A");
                }
                AppendLog("");

                // SQL Queries Results
                if (result.ActiveCategories.Contains("sql_query"))
                {
                    if (result.SqlQueries.Any())
                    {
                        AppendLog(Properties.Resources.SqlQueriesFound);
                        AppendLog(Properties.Resources.FoundIn);
                        foreach (var finding in result.SqlQueries)
                        {
                            AppendLog($"  - {GetRelativePath(finding.FilePath, ProjectPath)} (Line {finding.LineNumber}): {finding.Context}");
                        }
                    }
                    else
                    {
                        AppendLog(Properties.Resources.SqlQueriesNotFound);
                    }
                }
                else
                {
                    AppendLog("SQL Queries: N/A");
                }
                AppendLog("");

                // HTTP/HTTPS URLs Results
                if (result.ActiveCategories.Contains("http_url"))
                {
                    if (result.HttpUrls.Any())
                    {
                        AppendLog(Properties.Resources.HttpUrlsFound);
                        AppendLog(Properties.Resources.FoundIn);
                        foreach (var finding in result.HttpUrls)
                        {
                            AppendLog($"  - {GetRelativePath(finding.FilePath, ProjectPath)} (Line {finding.LineNumber}): {finding.Context}");
                        }
                    }
                    else
                    {
                        AppendLog(Properties.Resources.HttpUrlsNotFound);
                    }
                }
                else
                {
                    AppendLog("HTTP/HTTPS URLs: N/A");
                }

                AppendLog("");
                AppendLog(Properties.Resources.AnalysisComplete);
                MessageBoxUtils.ShowInfo(Properties.Resources.AnalysisComplete);
            }
            catch (Exception ex)
            {
                AppendLog($"{Properties.Resources.AnalysisFailed}: {ex.Message}");
                MessageBoxUtils.ShowError(ex.Message);
            }
            finally
            {
                IsRunning = false;
                RunAnalysisCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanRunAnalysis()
        {
            return !IsRunning && HasProject;
        }

        private void AppendLog(string message)
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => AppendLogInternal(message)));
                return;
            }

            AppendLogInternal(message);
        }

        private void AppendLogInternal(string message)
        {
            lock (_logLock)
            {
                var sanitized = message ?? string.Empty;
                if (_logLines.Count == 0 && ConsoleLog == Properties.Resources.WaitingForCommand)
                {
                    _logLines.Clear();
                    _logCharCount = 0;
                }

                _logLines.Enqueue(sanitized);
                _logCharCount += sanitized.Length;

                TrimLogIfNeeded();
                ScheduleLogFlush();
            }
        }

        private void SetConsoleLog(string message)
        {
            lock (_logLock)
            {
                _logLines.Clear();
                _logCharCount = 0;

                var sanitized = message ?? string.Empty;
                _logLines.Enqueue(sanitized);
                _logCharCount = sanitized.Length;

                ScheduleLogFlush();
            }
        }

        private void ScheduleLogFlush()
        {
            if (_logFlushScheduled)
            {
                return;
            }

            _logFlushScheduled = true;
            if (Application.Current == null)
            {
                FlushLog();
                return;
            }

            Application.Current.Dispatcher.BeginInvoke(new Action(FlushLog));
        }

        private void FlushLog()
        {
            string logText;
            lock (_logLock)
            {
                logText = string.Join(Environment.NewLine, _logLines);
                _logFlushScheduled = false;
            }

            ConsoleLog = logText;
        }

        private void TrimLogIfNeeded()
        {
            var newlineLength = Environment.NewLine.Length;
            while (_logLines.Count > 0 && _logCharCount + ((_logLines.Count - 1) * newlineLength) > MaxLogCharacters)
            {
                var removed = _logLines.Dequeue();
                _logCharCount -= removed.Length;
            }
        }

        private string GetRelativePath(string fullPath, string basePath)
        {
            try
            {
                var baseUri = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
                var fullUri = new Uri(fullPath);
                return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
            }
            catch
            {
                return fullPath;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSaveReport))]
        private async Task SaveReport()
        {
            if (string.IsNullOrWhiteSpace(ProjectPath))
            {
                MessageBoxUtils.ShowWarning("No project loaded.", "Save Failed");
                return;
            }

            try
            {
                string folderName = new DirectoryInfo(ProjectPath).Name;
                string filePath = await _reportService.SaveReportAsync(ConsoleLog, folderName);
                MessageBoxUtils.ShowInfo($"Report saved successfully to:\n{filePath}", "Report Saved");
            }
            catch (Exception ex)
            {
                MessageBoxUtils.ShowError($"Failed to save report: {ex.Message}");
            }
        }

        private bool CanSaveReport()
        {
            return !string.IsNullOrWhiteSpace(ConsoleLog) && ConsoleLog != Properties.Resources.WaitingForCommand;
        }
    }
}
