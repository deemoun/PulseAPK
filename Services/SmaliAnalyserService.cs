using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PulseAPK.Services
{
    public class SmaliAnalyserService
    {
        public async Task<AnalysisResult> AnalyseProjectAsync(string projectPath, Action<string> logCallback)
        {
            var result = new AnalysisResult();

            if (!Directory.Exists(projectPath))
            {
                throw new DirectoryNotFoundException($"Project path '{projectPath}' does not exist.");
            }

            // Ensure rules are initialized/loaded (this covers missing files/defaults)
            AnalysisRuleSet rules;
            try 
            {
                // Force a reload/check to ensure we have the latest
                rules = (AnalysisRuleSet)AnalysisRulesLoader.InitializeRules();
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"Warning: Failed to load custom rules: {ex.Message}. Using defaults.");
                rules = (AnalysisRuleSet)AnalysisRulesLoader.InitializeRules(); // This might retry or just return safe defaults if designed so
            }

            // Find all .smali files recursively
            var smaliFiles = Directory.GetFiles(projectPath, "*.smali", SearchOption.AllDirectories);

            if (smaliFiles.Length == 0)
            {
                throw new InvalidOperationException("No Smali files found in the project directory.");
            }

            logCallback?.Invoke($"Found {smaliFiles.Length} Smali files. Starting analysis...");
            if (rules.Rules.Count > 0)
            {
                logCallback?.Invoke($"Loaded {rules.Rules.Count} analysis categories.");
            }
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
                        AnalyzeFile(file, lines, result, rules);
                        
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

        private void AnalyzeFile(string filePath, string[] lines, AnalysisResult result, AnalysisRuleSet rules)
        {
            // Skip library classes
            if (IsLibraryClass(filePath, rules.LibraryPaths))
            {
                return;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                foreach (var rule in rules.Rules)
                {
                    if (CheckPatterns(line, rule.RegexPatterns))
                    {
                        var finding = new Finding
                        {
                            FilePath = filePath,
                            LineNumber = i + 1,
                            Context = line.Trim()
                        };

                        // Map known categories to the specific result lists
                        switch (rule.Category.ToLowerInvariant())
                        {
                            case "root_check":
                                result.RootChecks.Add(finding);
                                break;
                            case "emulator_check":
                                result.EmulatorChecks.Add(finding);
                                break;
                            case "hardcoded_creds":
                                result.HardcodedCredentials.Add(finding);
                                break;
                            case "sql_query":
                                result.SqlQueries.Add(finding);
                                break;
                            case "http_url":
                                result.HttpUrls.Add(finding);
                                break;
                            // Add other categories here if the UI supports them or use a generic list if available
                        }
                    }
                }
            }
        }

        private bool IsLibraryClass(string filePath, List<string> libraryPaths)
        {
            // Normalize path separators for comparison
            var normalizedPath = filePath.Replace("/", "\\").ToLowerInvariant();
            
            foreach (var pattern in libraryPaths)
            {
                if (normalizedPath.Contains(pattern.ToLowerInvariant()))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckPatterns(string line, List<string> patterns)
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
