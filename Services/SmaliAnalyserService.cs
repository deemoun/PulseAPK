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
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // Check for root detection patterns
                if (CheckPatterns(line, GetRootCheckPatterns()))
                {
                    result.RootChecks.Add(new Finding
                    {
                        FilePath = filePath,
                        LineNumber = i + 1,
                        Context = line.Trim()
                    });
                }
                
                // Check for emulator detection patterns
                if (CheckPatterns(line, GetEmulatorCheckPatterns()))
                {
                    result.EmulatorChecks.Add(new Finding
                    {
                        FilePath = filePath,
                        LineNumber = i + 1,
                        Context = line.Trim()
                    });
                }
                
                // Check for hardcoded credentials patterns
                if (CheckPatterns(line, GetCredentialPatterns()))
                {
                    result.HardcodedCredentials.Add(new Finding
                    {
                        FilePath = filePath,
                        LineNumber = i + 1,
                        Context = line.Trim()
                    });
                }
                
                // Check for SQL query patterns
                if (CheckPatterns(line, GetSqlPatterns()))
                {
                    result.SqlQueries.Add(new Finding
                    {
                        FilePath = filePath,
                        LineNumber = i + 1,
                        Context = line.Trim()
                    });
                }
                
                // Check for HTTP/HTTPS URL patterns
                if (CheckPatterns(line, GetHttpUrlPatterns()))
                {
                    result.HttpUrls.Add(new Finding
                    {
                        FilePath = filePath,
                        LineNumber = i + 1,
                        Context = line.Trim()
                    });
                }
            }
        }

        private bool CheckPatterns(string line, string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private string[] GetRootCheckPatterns()
        {
            return new[]
            {
                @"/system/bin/su",
                @"/system/xbin/su",
                @"/sbin/su",
                @"/system/app/Superuser\.apk",
                @"RootBeer",
                @"RootTools",
                @"isDeviceRooted",
                @"checkRootMethod",
                @"detectRootManagementApps",
                @"detectPotentiallyDangerousApps",
                @"su\s*""",
                @"which\s+su"
            };
        }

        private string[] GetEmulatorCheckPatterns()
        {
            return new[]
            {
                @"goldfish",
                @"ro\.product\.model",
                @"ro\.build\.product",
                @"ro\.product\.device",
                @"generic",
                @"emulator",
                @"Android SDK built for x86",
                @"000000000000000",
                @"isEmulator",
                @"checkEmulator",
                @"detectEmulator",
                @"Build\.MODEL",
                @"Build\.MANUFACTURER",
                @"Build\.BRAND.*generic",
                @"Build\.DEVICE.*generic",
                @"Build\.PRODUCT.*sdk"
            };
        }

        private string[] GetCredentialPatterns()
        {
            return new[]
            {
                @"const-string.*[""']password[""']",
                @"const-string.*[""']pwd[""']",
                @"const-string.*[""']pass[""']",
                @"const-string.*[""']username[""']",
                @"const-string.*[""']user[""']",
                @"const-string.*[""']login[""']",
                @"const-string.*[""']admin[""']",
                @"const-string.*[""']auth[""']",
                @"const-string.*[""']token[""']",
                @"const-string.*[""']apikey[""']",
                @"const-string.*[""']api_key[""']",
                @"const-string.*[""']secret[""']"
            };
        }

        private string[] GetSqlPatterns()
        {
            return new[]
            {
                @"const-string.*[""'].*SELECT\s+",
                @"const-string.*[""'].*INSERT\s+INTO",
                @"const-string.*[""'].*UPDATE\s+",
                @"const-string.*[""'].*DELETE\s+FROM",
                @"const-string.*[""'].*CREATE\s+TABLE",
                @"const-string.*[""'].*DROP\s+TABLE",
                @"const-string.*[""'].*ALTER\s+TABLE",
                @"rawQuery",
                @"execSQL",
                @"SQLiteDatabase"
            };
        }

        private string[] GetHttpUrlPatterns()
        {
            return new[]
            {
                @"https?://[^\s""']+",
                @"const-string.*[""']https?://",
                @"URL.*http"
            };
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
