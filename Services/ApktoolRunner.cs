using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace APKToolUI.Services
{
    public class ApktoolRunner
    {
        // TODO: Get from settings
        private const string ApktoolPath = "apktool.jar"; 

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
            var startInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar {ApktoolPath} {arguments}",
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
