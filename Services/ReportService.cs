using System;
using System.IO;
using System.Threading.Tasks;

namespace PulseAPK.Services
{
    public class ReportService
    {
        private const string ReportsDirectoryName = "reports";

        public async Task<string> SaveReportAsync(string reportContent, string folderName)
        {
            try
            {
                // Create reports directory if it doesn't exist
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string reportsDirectory = Path.Combine(appDirectory, ReportsDirectoryName);

                if (!Directory.Exists(reportsDirectory))
                {
                    Directory.CreateDirectory(reportsDirectory);
                }

                // Format filename: [date]-[time]-[folder name].txt
                // Using a safe date format for filenames
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                string safeFolderName = GetSafeFilename(folderName);
                string filename = $"{timestamp}-{safeFolderName}.txt";
                string filePath = Path.Combine(reportsDirectory, filename);

                // Write content to file
                await File.WriteAllTextAsync(filePath, reportContent);

                return filePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save report: {ex.Message}", ex);
            }
        }

        private string GetSafeFilename(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
