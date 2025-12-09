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
            // Library prefixes to filter out
            var libraryPrefixes = new[]
            {
                "Landroidx/",
                "Lkotlin/",
                "Lkotlinx/",
                "Lcom/google/",
                "Lcom/squareup/",
                "Lokhttp3/",
                "Lokio/",
                "Lretrofit2/"
            };

            // Extract class name from file path
            var fileName = Path.GetFileName(filePath);
            foreach (var prefix in libraryPrefixes)
            {
                if (filePath.Contains(prefix.Replace("/", "\\")))
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
                // Exec calls to su/busybox/magisk/xposed binaries
                @"invoke-static.*Runtime.*getRuntime.*exec.*(su|magisk|busybox|superuser|xposed)",
                @"const-string.*(/system/xbin/su|/system/bin/su|/sbin/su|/system/bin/magisk|/system/bin/busybox)",
                
                // Checks for known root packages or file paths
                @"const-string.*(com\.topjohnwu\.magisk|eu\.chainfire\.supersu|com\.noshufou\.android\.su|com\.koushikdutta\.superuser|org\.meowcat\.edxposed|de\.robv\.android\.xposed\.installer)",
                @"const-string.*(/data/local/tmp|/data/local|/system/app/Superuser|/system/app/SuperSU|/system/app/Magisk|/system/xbin/which)",
                
                // Build.* inspection for root/emulator tags
                @"const-string.*(test-keys|test_keys|dev-keys|userdebug|eng)",
                @"invoke-static.*Landroid/os/Build.*get(Tags|Fingerprint|Brand|Model|Manufacturer|Product)",
                
                // Busybox or su presence via which or file existence
                @"const-string.*""which""",
                @"const-string.*""su""",
                
                // Legacy patterns for backwards compatibility
                @"RootBeer",
                @"RootTools",
                @"isDeviceRooted",
                @"checkRootMethod",
                @"detectRootManagementApps",
                @"detectPotentiallyDangerousApps"
            };
        }

        private string[] GetEmulatorCheckPatterns()
        {
            return new[]
            {
                // System property checks for emulator indicators
                @"const-string.*ro\.kernel\.qemu",
                @"const-string.*(ro\.product\.(manufacturer|brand|model|device|name)|ro\.hardware|ro\.serial|gsm\.operator\.numeric)",
                
                // Comparison against emulator fingerprints, models, or brands
                @"const-string.*(generic|sdk|google_sdk|vbox|test-keys|emulator|goldfish|ranchu|sdk_gphone|Genymotion|BlueStacks|Nox|MuMu|LDPlayer)",
                @"invoke-static.*Landroid/os/Build.*(FINGERPRINT|MODEL|BRAND|PRODUCT|HARDWARE|DEVICE)",
                
                // Fake device ID/phone number checks
                @"const-string.*(15555215554|15555215556|15555215558|000000000000000|eMulator|android-emulator)",
                
                // Legacy patterns
                @"Android SDK built for x86",
                @"isEmulator",
                @"checkEmulator",
                @"detectEmulator",
                @"Build\.MODEL",
                @"Build\.MANUFACTURER"
            };
        }

        private string[] GetCredentialPatterns()
        {
            return new[]
            {
                // Identifier names suggesting credentials - simplified
                @"(password|passwd|pwd|secret|token|apikey|api_key|auth|login|username|email).*=.*""[^""]{4,}""",
                
                // Authorization headers with embedded tokens
                @"const-string.*""Authorization:? (Bearer|Basic|Token)",
                @"const-string.*""(Bearer|Basic) [A-Za-z0-9\-_.:+/]{8,}""",
                
                // Long random-looking tokens/keys (base64-like or hex)
                @"const-string.*""[A-Za-z0-9+/]{16,}=*""",
                @"const-string.*""[A-Fa-f0-9]{32,}""",
                
                // Legacy patterns
                @"const-string.*password",
                @"const-string.*username",
                @"const-string.*admin",
                @"const-string.*apikey",
                @"const-string.*api_key"
            };
        }

        private string[] GetSqlPatterns()
        {
            return new[]
            {
                // Raw SQL statements in strings
                @"const-string.*\s*(SELECT|INSERT|UPDATE|DELETE|CREATE TABLE|DROP TABLE|ALTER TABLE)\b",
                @"const-string.*\s*(FROM|WHERE|VALUES|SET)\b",
                
                // Exec/compile of SQL APIs with raw strings
                @"invoke-virtual.*Landroid/database/sqlite/SQLiteDatabase.*(execSQL|rawQuery|compileStatement)",
                @"invoke-interface.*Landroid/database/Cursor.*(rawQuery|execSQL)",
                
                // Legacy patterns
                @"rawQuery",
                @"execSQL",
                @"SQLiteDatabase"
            };
        }

        private string[] GetHttpUrlPatterns()
        {
            return new[]
            {
                // Capture HTTP/HTTPS URLs
                @"const-string.*""https?://[^""]+""",
                
                // Legacy patterns
                @"https?://[^\s""']+",
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
