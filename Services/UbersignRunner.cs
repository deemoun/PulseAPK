using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PulseAPK.Services
{
    public class UbersignRunner
    {
        public event Action<string>? OutputDataReceived;

        public async Task<int> RunSigningAsync(string inputApk, string signedOutputApk, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(inputApk))
            {
                throw new ArgumentException("Input APK path cannot be null or empty.", nameof(inputApk));
            }

            if (string.IsNullOrWhiteSpace(signedOutputApk))
            {
                throw new ArgumentException("Signed output APK path cannot be null or empty.", nameof(signedOutputApk));
            }

            if (!File.Exists(inputApk))
            {
                throw new FileNotFoundException($"Input APK '{inputApk}' was not found.");
            }

            var (ubersignPath, isJar) = GetUbersignPath();
            if (!File.Exists(ubersignPath))
            {
                throw new FileNotFoundException($"Could not find 'ubersign.jar' (or executable) in the application root at '{ubersignPath}'.");
            }

            var outputDirectory = Path.GetDirectoryName(signedOutputApk);
            outputDirectory ??= Directory.GetCurrentDirectory();
            Directory.CreateDirectory(outputDirectory);

            // When providing an output directory, ubersign does not allow the overwrite flag.
            var ubersignArguments = $"-a \"{inputApk}\" -o \"{outputDirectory}\" --allowResign";

            var startInfo = isJar
                ? new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar \"{ubersignPath}\" {ubersignArguments}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
                : new ProcessStartInfo
                {
                    FileName = ubersignPath,
                    Arguments = ubersignArguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

            using var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OutputDataReceived?.Invoke(e.Data);
                    Debug.WriteLine($"[UBERSIGN INFO] {e.Data}");
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OutputDataReceived?.Invoke(e.Data);
                    Debug.WriteLine($"[UBERSIGN ERROR] {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                return process.ExitCode;
            }

            var createdFile = FindSignedApk(inputApk, outputDirectory);

            if (string.IsNullOrWhiteSpace(createdFile))
            {
                OutputDataReceived?.Invoke("Signing completed but no signed APK was produced in the output directory.");
                return 1;
            }

            if (!string.Equals(createdFile, signedOutputApk, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(createdFile, signedOutputApk, true);
            }

            return process.ExitCode;
        }

        private static (string Path, bool IsJar) GetUbersignPath()
        {
            var root = string.IsNullOrWhiteSpace(AppDomain.CurrentDomain.BaseDirectory)
                ? Directory.GetCurrentDirectory()
                : AppDomain.CurrentDomain.BaseDirectory;

            var jarPath = Path.Combine(root, "ubersign.jar");
            if (File.Exists(jarPath)) return (jarPath, true);

            var ubersignPath = Path.Combine(root, "ubersign");
            if (File.Exists(ubersignPath)) return (ubersignPath, false);

            var windowsExecutable = Path.Combine(root, "ubersign.exe");
            return File.Exists(windowsExecutable) ? (windowsExecutable, false) : (jarPath, true);
        }

        private static string? FindSignedApk(string inputApk, string outputDirectory)
        {
            var inputFileName = Path.GetFileNameWithoutExtension(inputApk);
            var expectedNames = new[]
            {
                $"{inputFileName}-aligned-signed.apk",
                $"{inputFileName}-signed.apk"
            };

            return expectedNames
                .Select(name => Path.Combine(outputDirectory, name))
                .FirstOrDefault(File.Exists);
        }
    }
}
