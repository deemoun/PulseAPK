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

            // Run all detection methods
            await Task.Run(() =>
            {
                result.RootChecks = DetectRootChecks(smaliFiles, logCallback);
                result.EmulatorChecks = DetectEmulatorChecks(smaliFiles, logCallback);
                result.HardcodedCredentials = DetectHardcodedCredentials(smaliFiles, logCallback);
                result.SqlQueries = DetectSqlQueries(smaliFiles, logCallback);
                result.HttpUrls = DetectHttpUrls(smaliFiles, logCallback);
            });

            return result;
        }

        private List<Finding> DetectRootChecks(string[] smaliFiles, Action<string> logCallback)
        {
            var findings = new List<Finding>();

            var patterns = new[]
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

            foreach (var file in smaliFiles)
            {
                try
                {
                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        foreach (var pattern in patterns)
                        {
                            if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                            {
                                findings.Add(new Finding
                                {
                                    FilePath = file,
                                    LineNumber = i + 1,
                                    Context = line.Trim()
                                });
                                break; // Only add one finding per line
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"Error reading file {file}: {ex.Message}");
                }
            }

            return findings;
        }

        private List<Finding> DetectEmulatorChecks(string[] smaliFiles, Action<string> logCallback)
        {
            var findings = new List<Finding>();

            var patterns = new[]
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

            foreach (var file in smaliFiles)
            {
                try
                {
                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        foreach (var pattern in patterns)
                        {
                            if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                            {
                                findings.Add(new Finding
                                {
                                    FilePath = file,
                                    LineNumber = i + 1,
                                    Context = line.Trim()
                                });
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"Error reading file {file}: {ex.Message}");
                }
            }

            return findings;
        }

        private List<Finding> DetectHardcodedCredentials(string[] smaliFiles, Action<string> logCallback)
        {
            var findings = new List<Finding>();

            var patterns = new[]
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

            foreach (var file in smaliFiles)
            {
                try
                {
                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        foreach (var pattern in patterns)
                        {
                            if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                            {
                                findings.Add(new Finding
                                {
                                    FilePath = file,
                                    LineNumber = i + 1,
                                    Context = line.Trim()
                                });
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"Error reading file {file}: {ex.Message}");
                }
            }

            return findings;
        }

        private List<Finding> DetectSqlQueries(string[] smaliFiles, Action<string> logCallback)
        {
            var findings = new List<Finding>();

            var patterns = new[]
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

            foreach (var file in smaliFiles)
            {
                try
                {
                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        foreach (var pattern in patterns)
                        {
                            if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                            {
                                findings.Add(new Finding
                                {
                                    FilePath = file,
                                    LineNumber = i + 1,
                                    Context = line.Trim()
                                });
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"Error reading file {file}: {ex.Message}");
                }
            }

            return findings;
        }

        private List<Finding> DetectHttpUrls(string[] smaliFiles, Action<string> logCallback)
        {
            var findings = new List<Finding>();

            var patterns = new[]
            {
                @"https?://[^\s""']+",
                @"const-string.*[""']https?://",
                @"URL.*http"
            };

            foreach (var file in smaliFiles)
            {
                try
                {
                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        foreach (var pattern in patterns)
                        {
                            if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                            {
                                findings.Add(new Finding
                                {
                                    FilePath = file,
                                    LineNumber = i + 1,
                                    Context = line.Trim()
                                });
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"Error reading file {file}: {ex.Message}");
                }
            }

            return findings;
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
