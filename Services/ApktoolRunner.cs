using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace APKToolUI.Services
{
    public class ApktoolRunner
    {
        private readonly ISettingsService _settingsService;

        public ApktoolRunner()
            : this(new SettingsService())
        {
        }

        public ApktoolRunner(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task RunDecompileAsync(string apkPath, string outputDir, bool decodeResources, bool decodeSources)
        {
            var args = new StringBuilder("d");
            args.Append($" \"{apkPath}\"");
            args.Append($" -o \"{outputDir}\"");

            if (!decodeResources) args.Append(" -r");
            if (!decodeSources) args.Append(" -s");

            args.Append(" -f"); // Force overwrite

            await RunProcessAsync(args.ToString());
        }

        private async Task RunProcessAsync(string arguments)
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
                if (e.Data != null)
                {
                    // TODO: Log to console
                    Debug.WriteLine($"[INFO] {e.Data}");
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    // TODO: Log to console
                    Debug.WriteLine($"[ERROR] {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();
        }
    }
}
