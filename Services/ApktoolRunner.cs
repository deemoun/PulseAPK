using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace APKToolUI.Services
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

        public async Task RunDecompileAsync(string apkPath, string outputDir, bool decodeResources, bool decodeSources, bool keepOriginalManifest, bool keepUnknownFiles, bool forceOverwrite = false, CancellationToken cancellationToken = default)
        {
            var args = new StringBuilder("d");
            args.Append($" \"{apkPath}\"");
            args.Append($" -o \"{outputDir}\"");

            if (!decodeResources) args.Append(" -r");
            if (!decodeSources) args.Append(" -s");
            if (keepOriginalManifest) args.Append(" -m");
            if (keepUnknownFiles) args.Append(" -u");

            if (forceOverwrite)
            {
                args.Append(" -f"); // Force overwrite
            }

            await RunProcessAsync(args.ToString(), cancellationToken);
        }

        private async Task RunProcessAsync(string arguments, CancellationToken cancellationToken)
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
        }
    }
}
