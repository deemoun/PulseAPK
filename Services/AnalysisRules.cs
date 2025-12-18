using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PulseAPK.Services
{
    public class AnalysisRuleSet
    {
        [JsonPropertyName("library_paths")]
        public List<string> LibraryPaths { get; set; } = new();

        [JsonPropertyName("rules")]
        public List<AnalysisRule> Rules { get; set; } = new();
    }

    public class AnalysisRule
    {
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("regex_patterns")]
        public List<string> RegexPatterns { get; set; } = new();
    }

    public static class AnalysisRulesLoader
    {
        private const string RulesFileName = "smali_analysis_rules.json";

        public static AnalysisRuleSet LoadRules()
        {
            var filePath = ResolveRulesFilePath();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Analysis rules file not found at '{filePath}'.");
            }

            try
            {
                var fileContents = File.ReadAllText(filePath);
                var rules = JsonSerializer.Deserialize<AnalysisRuleSet>(fileContents, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (rules == null)
                {
                    throw new InvalidOperationException("Failed to parse analysis rules file.");
                }

                return rules;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid analysis rules format: {ex.Message}", ex);
            }
        }

        private static string ResolveRulesFilePath()
        {
            var baseDirectory = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
            var directPath = Path.Combine(baseDirectory, RulesFileName);

            if (File.Exists(directPath))
            {
                return directPath;
            }

            // When running from the build output, walk up to the project root.
            var projectRootPath = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", RulesFileName));
            if (File.Exists(projectRootPath))
            {
                return projectRootPath;
            }

            return directPath;
        }
    }
}
