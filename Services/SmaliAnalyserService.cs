using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PulseAPK.Services
{
    public class SmaliAnalyserService
    {
        private readonly AnalysisRuleSet _ruleSet;

        public SmaliAnalyserService()
        {
            _ruleSet = AnalysisRulesLoader.LoadRules();
        }

        public async Task<AnalysisResult> AnalyseProjectAsync(string projectPath, Action<string> logCallback)
        {
            var result = new AnalysisResult();

            if (!Directory.Exists(projectPath))
            {
                throw new DirectoryNotFoundException($"Project path '{projectPath}' does not exist.");
            }

            // Find all .smali files recursively
            var smaliFiles = Directory.GetFiles(projectPath, "*.smali", SearchOption.AllDirectories);

            if (smaliFiles.Length == 0)
            {
                throw new InvalidOperationException("No Smali files found in the project directory.");
            }

            logCallback?.Invoke($"Found {smaliFiles.Length} Smali files. Starting analysis...");
            logCallback?.Invoke("");

            // Run all detection methods with progress reporting
            await Task.Run(() =>
            {
                int processedFiles = 0;
                int totalFiles = smaliFiles.Length;
                int reportInterval = totalFiles > 100 ? totalFiles / 20 : 10; // Report every 5% or every 10 files

                foreach (var file in smaliFiles)
                {
                    try
                    {
                        var lines = File.ReadAllLines(file);

                        // Run all detections on this file
                        AnalyzeFile(file, lines, result);

                        processedFiles++;

                        // Report progress periodically
                        if (processedFiles % reportInterval == 0 || processedFiles == totalFiles)
                        {
                            logCallback?.Invoke($"({processedFiles}/{totalFiles} files processed)");
                        }
                    }
                    catch (Exception ex)
                    {
                        logCallback?.Invoke($"Error reading file {file}: {ex.Message}");
                    }
                }
            });

            return result;
        }

        private void AnalyzeFile(string filePath, string[] lines, AnalysisResult result)
        {
            // Skip library classes according to the configured rules
            if (IsLibraryClass(filePath))
            {
                return;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                foreach (var rule in _ruleSet.Rules)
                {
                    if (CheckPatterns(line, rule.RegexPatterns))
                    {
                        AddFinding(result, rule.Category, filePath, i + 1, line.Trim());
                    }
                }
            }
        }

        private void AddFinding(AnalysisResult result, string category, string filePath, int lineNumber, string context)
        {
            switch (category.ToLowerInvariant())
            {
                case "root_check":
                    result.RootChecks.Add(new Finding { FilePath = filePath, LineNumber = lineNumber, Context = context });
                    break;
                case "emulator_check":
                    result.EmulatorChecks.Add(new Finding { FilePath = filePath, LineNumber = lineNumber, Context = context });
                    break;
                case "hardcoded_creds":
                    result.HardcodedCredentials.Add(new Finding { FilePath = filePath, LineNumber = lineNumber, Context = context });
                    break;
                case "sql_query":
                    result.SqlQueries.Add(new Finding { FilePath = filePath, LineNumber = lineNumber, Context = context });
                    break;
                case "http_url":
                    result.HttpUrls.Add(new Finding { FilePath = filePath, LineNumber = lineNumber, Context = context });
                    break;
            }
        }

        private bool IsLibraryClass(string filePath)
        {
            // Normalize path separators for comparison
            var normalizedPath = filePath.Replace("/", "\\").ToLowerInvariant();

            foreach (var pattern in _ruleSet.LibraryPaths)
            {
                if (normalizedPath.Contains(pattern.ToLowerInvariant()))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckPatterns(string line, IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
            {
                try
                {
                    if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Skip invalid regex patterns silently
                }
            }
            return false;
        }
    }

    public class AnalysisResult
    {
        public List<Finding> RootChecks { get; set; } = new List<Finding>();
        public List<Finding> EmulatorChecks { get; set; } = new List<Finding>();
        public List<Finding> HardcodedCredentials { get; set; } = new List<Finding>();
        public List<Finding> SqlQueries { get; set; } = new List<Finding>();
        public List<Finding> HttpUrls { get; set; } = new List<Finding>();
    }

    public class Finding
    {
        public string FilePath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string Context { get; set; } = string.Empty;
    }
}
