using System;
using System.Diagnostics;
using System.IO;
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

            var ubersignPath = GetUbersignPath();
            if (!File.Exists(ubersignPath))
            {
                throw new FileNotFoundException($"Could not find 'ubersign' in the application root at '{ubersignPath}'.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = ubersignPath,
                Arguments = $"\"{inputApk}\" \"{signedOutputApk}\"",
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

            return process.ExitCode;
        }

        private static string GetUbersignPath()
        {
            var root = string.IsNullOrWhiteSpace(AppDomain.CurrentDomain.BaseDirectory)
                ? Directory.GetCurrentDirectory()
                : AppDomain.CurrentDomain.BaseDirectory;

            var ubersignPath = Path.Combine(root, "ubersign");

            if (File.Exists(ubersignPath)) return ubersignPath;

            var windowsExecutable = Path.Combine(root, "ubersign.exe");
            return File.Exists(windowsExecutable) ? windowsExecutable : ubersignPath;
        }
    }
}
