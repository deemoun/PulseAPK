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
            // Skip library classes according to DETECTION_RULES.md
            if (IsLibraryClass(filePath))
            {
                return;
            }

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

        private bool IsLibraryClass(string filePath)
        {
            // Normalize path separators for comparison
            var normalizedPath = filePath.Replace("/", "\\").ToLowerInvariant();
            
            // Library path patterns to filter out
            var libraryPatterns = new[]
            {
                "\\androidx\\",
                "\\kotlin\\",
                "\\kotlinx\\",
                "\\com\\google\\",
                "\\com\\squareup\\",
                "\\okhttp3\\",
                "\\okio\\",
                "\\retrofit2\\",
                "\\com\\android\\",
                "\\android\\support\\"
            };

            foreach (var pattern in libraryPatterns)
            {
                if (normalizedPath.Contains(pattern))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckPatterns(string line, string[] patterns)
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

        private string[] GetRootCheckPatterns()
        {
            return new[]
            {
                // Runtime.exec() with su/magisk/busybox/superuser/xposed
                @"Runtime.*exec.*[""'](su|magisk|busybox|superuser|xposed)[""']",
                
                // Root binary paths
                @"const-string.*[""/]system/(xbin|bin)/(su|magisk|busybox)[""']",
                @"const-string.*[""/]sbin/su[""']",
                
                // Known root management packages
                @"const-string.*[""']com\.topjohnwu\.magisk",
                @"const-string.*[""']eu\.chainfire\.supersu",
                @"const-string.*[""']com\.noshufou\.android\.su",
                @"const-string.*[""']com\.koushikdutta\.superuser",
                @"const-string.*[""']de\.robv\.android\.xposed",
                
                // Root-specific file paths
                @"const-string.*[""/]system/app/(Superuser|SuperSU|Magisk)",
                @"const-string.*[""/]data/local/tmp[""']",
                
                // Build.TAGS with root/debug indicators (only with Build context)
                @"Build.*TAGS.*test-keys",
                @"Build.*TAGS.*dev-keys",
                @"Build.*TAGS.*userdebug",
                
                // Root detection library names
                @"RootBeer",
                @"RootTools",
                @"isDeviceRooted",
                @"checkRootMethod"
            };
        }

        private string[] GetEmulatorCheckPatterns()
        {
            return new[]
            {
                // System property checks for emulator
                @"const-string.*[""']ro\.kernel\.qemu[""']",
                @"const-string.*[""']ro\.hardware[""'].*goldfish",
                @"const-string.*[""']ro\.product\.model[""'].*sdk",
                @"const-string.*[""']ro\.product\.model[""'].*Emulator",
                
                // Clear emulator product names
                @"const-string.*[""'](generic_x86|sdk_gphone|google_sdk|sdk.*x86)[""']",
                
                // Emulator software names
                @"const-string.*[""'](Genymotion|BlueStacks|NoxPlayer|MEmu|LDPlayer)[""']",
                
                // Fake IMEI patterns used by emulators
                @"const-string.*[""'](15555215554|15555215556|15555215558)[""']",
                @"const-string.*[""']0000000000000000[""']",
                
                // Build fingerprint checks (only with Build context)
                @"Build.*FINGERPRINT.*generic",
                @"Build.*FINGERPRINT.*test-keys",
                @"Build.*MODEL.*sdk",
                @"Build.*MODEL.*Emulator",
                
                // Emulator detection methods
                @"isEmulator",
                @"checkEmulator",
                @"detectEmulator"
            };
        }

        private string[] GetCredentialPatterns()
        {
            return new[]
            {
                // Authorization headers (clear indicators)
                @"const-string.*[""']Authorization:\s*(Bearer|Basic|Token)\s+[A-Za-z0-9\-_\.]+",
                @"const-string.*[""'](Bearer|Basic)\s+[A-Za-z0-9\-_\.]{20,}[""']",
                
                // API keys (long alphanumeric strings labeled as key/token)
                @"sput-object.*\.(api_?key|api_?token|auth_?token|access_?token)",
                @"const-string.*[A-Za-z0-9]{32,}.*\.(api_?key|token|secret)",
                
                // Base64-like secrets (avoid short strings and UI labels)
                @"const-string.*[""'][A-Za-z0-9+/]{40,}={0,2}[""']",
                
                // Long hex secrets (likely keys, not colors)
                @"const-string.*[""'][0-9A-Fa-f]{40,}[""']",
                
                // Password/secret assignment (avoid InputType/AutofillHint)
                @"\.field.*password.*Ljava/lang/String;.*[""'][^""']{6,}[""']",
                @"\.field.*secret.*Ljava/lang/String;.*[""'][^""']{6,}[""']",
                @"\.field.*api_?key.*Ljava/lang/String;.*[""'][^""']{10,}[""']",
                
                // Hardcoded basic auth credentials
                @"const-string.*[""'][a-zA-Z0-9_]+:[a-zA-Z0-9_]{8,}@",
                
                // AWS/cloud provider keys
                @"const-string.*[""'](AKIA|ASIA)[A-Z0-9]{16}[""']",
                @"const-string.*[""']AIza[A-Za-z0-9\-_]{35}[""']"
            };
        }

        private string[] GetSqlPatterns()
        {
            return new[]
            {
                // SQL keywords in string literals (check for actual SQL structure)
                @"const-string.*[""']\s*SELECT\s+\*?\s+(FROM|[a-zA-Z_])",
                @"const-string.*[""']\s*INSERT\s+INTO\s+",
                @"const-string.*[""']\s*UPDATE\s+\w+\s+SET\s+",
                @"const-string.*[""']\s*DELETE\s+FROM\s+",
                @"const-string.*[""']\s*CREATE\s+TABLE\s+",
                @"const-string.*[""']\s*DROP\s+TABLE\s+",
                @"const-string.*[""']\s*ALTER\s+TABLE\s+",
                
                // SQL with WHERE clause (strong indicator)
                @"const-string.*\s+WHERE\s+\w+\s*=",
                
                // SQLiteDatabase method calls (strong indicators)
                @"invoke-virtual.*SQLiteDatabase;->execSQL\(Ljava/lang/String",
                @"invoke-virtual.*SQLiteDatabase;->rawQuery\(Ljava/lang/String",
                @"invoke-virtual.*SQLiteDatabase;->compileStatement\("
            };
        }

        private string[] GetHttpUrlPatterns()
        {
            return new[]
            {
                // HTTP/HTTPS URLs in string literals
                @"const-string.*[""']https?://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,}[/\w\-\._~:/?#\[\]@!$&'()*+,;=]*[""']",
                
                // Interesting API endpoints
                @"https?://[^""']+/(api|v1|v2|v3)/",
                @"https?://[^""']+/(auth|login|signin|signup|register)/",
                @"https?://[^""']+/(user|account|profile)/",
                @"https?://[^""']+/(token|oauth|refresh)/",
                @"https?://[^""']+/(payment|checkout|billing)/",
                @"https?://[^""']+/(admin|dashboard)/",
                
                // Non-HTTPS URLs (potential security issue)
                @"const-string.*[""']http://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,}"
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
