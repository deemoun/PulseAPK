using System;
using System.Collections.Generic;
using System.IO;
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
                rules = (AnalysisRuleSet)AnalysisRulesLoader.InitializeRules();
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
                foreach (var rule in rules.Rules)
                {
                    result.ActiveCategories.Add(rule.Category);
                }
            }
            logCallback?.Invoke("");

            var regexCache = BuildRegexCache(rules, logCallback);
            var categoryOrder = BuildCategoryOrder(rules, regexCache);
            var deduplicationSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                        // Run all detections on this file
                        AnalyzeFile(file, result, rules, regexCache, categoryOrder, deduplicationSet);
                        
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

        private void AnalyzeFile(
            string filePath,
            AnalysisResult result,
            AnalysisRuleSet rules,
            Dictionary<string, List<Regex>> regexCache,
            IReadOnlyList<string> categoryOrder,
            HashSet<string> deduplicationSet)
        {
            // Skip library classes
            if (IsLibraryClass(filePath, rules.LibraryPaths))
            {
                return;
            }

            var lineNumber = 0;
            foreach (var line in File.ReadLines(filePath))
            {
                lineNumber++;

                foreach (var category in categoryOrder)
                {
                    if (!regexCache.TryGetValue(category, out var cachedPatterns))
                    {
                        continue;
                    }

                    if (CheckPatterns(line, cachedPatterns))
                    {
                        AddFinding(category, filePath, lineNumber, line, result, deduplicationSet);
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

        private Dictionary<string, List<Regex>> BuildRegexCache(AnalysisRuleSet rules, Action<string> logCallback)
        {
            var cache = new Dictionary<string, List<Regex>>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < rules.Rules.Count; i++)
            {
                var rule = rules.Rules[i];
                var category = rule.Category ?? string.Empty;
                if (!cache.TryGetValue(category, out var compiledPatterns))
                {
                    compiledPatterns = new List<Regex>();
                    cache[category] = compiledPatterns;
                }

                foreach (var pattern in rule.RegexPatterns)
                {
                    try
                    {
                        compiledPatterns.Add(new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled));
                    }
                    catch (ArgumentException ex)
                    {
                        logCallback?.Invoke($"Warning: Skipping invalid regex pattern '{pattern}' in category '{rule.Category}': {ex.Message}");
                    }
                }
            }

            return cache;
        }

        private List<string> BuildCategoryOrder(AnalysisRuleSet rules, Dictionary<string, List<Regex>> regexCache)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var orderedCategories = new List<string>();

            foreach (var rule in rules.Rules)
            {
                var category = rule.Category ?? string.Empty;
                if (regexCache.ContainsKey(category) && seen.Add(category))
                {
                    orderedCategories.Add(category);
                }
            }

            return orderedCategories;
        }

        private void AddFinding(
            string category,
            string filePath,
            int lineNumber,
            string line,
            AnalysisResult result,
            HashSet<string> deduplicationSet)
        {
            var normalizedPath = Path.GetFullPath(filePath).Replace('\\', '/');
            var deduplicationKey = $"{category}|{normalizedPath}|{lineNumber}";
            if (!deduplicationSet.Add(deduplicationKey))
            {
                return;
            }

            var finding = new Finding
            {
                FilePath = filePath,
                LineNumber = lineNumber,
                Context = line.Trim()
            };

            switch (category.ToLowerInvariant())
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

        private bool CheckPatterns(string line, List<Regex> patterns)
        {
            foreach (var pattern in patterns)
            {
                if (pattern.IsMatch(line))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class AnalysisResult
    {
        public HashSet<string> ActiveCategories { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
