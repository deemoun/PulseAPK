using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace PulseAPK.Services
{
    public class ApktoolRunner
    {
        private readonly ISettingsService _settingsService;

        public event Action<string>? OutputDataReceived;

        public ApktoolRunner()
            : this(new SettingsService())
        {
        }

        public ApktoolRunner(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<int> RunDecompileAsync(string apkPath, string outputDir, bool decodeResources, bool decodeSources, bool keepOriginalManifest, bool forceOverwrite = false, CancellationToken cancellationToken = default)
        {
            var args = new StringBuilder("d");
            args.Append($" \"{apkPath}\"");
            args.Append($" -o \"{outputDir}\"");

            if (!decodeResources) args.Append(" -r");
            if (!decodeSources) args.Append(" -s");
            if (keepOriginalManifest) args.Append(" -m");

            if (forceOverwrite)
            {
                args.Append(" -f"); // Force overwrite
            }

            return await RunProcessAsync(args.ToString(), cancellationToken);
        }

        public async Task<int> RunBuildAsync(string projectPath, string outputApk, bool useAapt2,CancellationToken cancellationToken = default)
        {
            var args = new StringBuilder("b");
            args.Append($" \"{projectPath}\"");
            args.Append($" -o \"{outputApk}\"");

            if (useAapt2) args.Append(" --use-aapt2");

            // For now, these are the main ones.
            // In the future we could add --force-all using -f, but build command behavior varies.
            
            return await RunProcessAsync(args.ToString(), cancellationToken);
        }

        private async Task<int> RunProcessAsync(string arguments, CancellationToken cancellationToken)
        {
            var apktoolPath = _settingsService.Settings.ApktoolPath;

            if (string.IsNullOrWhiteSpace(apktoolPath))
            {
                throw new FileNotFoundException("Apktool path has not been configured.");
            }

            if (!File.Exists(apktoolPath))
            {
                throw new FileNotFoundException($"Apktool path '{apktoolPath}' does not exist.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{apktoolPath}\" {arguments}",
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
                    Debug.WriteLine($"[INFO] {e.Data}");
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OutputDataReceived?.Invoke(e.Data);
                    Debug.WriteLine($"[ERROR] {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode;
        }
    }
}
