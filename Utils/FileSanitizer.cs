using System;
using System.IO;

namespace PulseAPK.Utils
{
    public static class FileSanitizer
    {
        // ZIP Local File Header Signature: PK (0x50, 0x4B) then 0x03, 0x04
        private static readonly byte[] ZipSignature = { 0x50, 0x4B, 0x03, 0x04 };

        public static (bool IsValid, string Message) ValidateApk(string path)
        {
            return ValidateFile(path, ".apk");
        }

        public static (bool IsValid, string Message) ValidateJar(string path)
        {
            return ValidateFile(path, ".jar");
        }

        private static (bool IsValid, string Message) ValidateFile(string path, string expectedExtension)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return (false, "File path is empty.");
            }

            if (!File.Exists(path))
            {
                return (false, "File does not exist.");
            }

            if (!path.EndsWith(expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"File does not have the expected '{expectedExtension}' extension.");
            }

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (fs.Length < ZipSignature.Length)
                    {
                        return (false, "File is too small to be a valid archive.");
                    }

                    var buffer = new byte[ZipSignature.Length];
                    var bytesRead = fs.Read(buffer, 0, buffer.Length);

                    if (bytesRead != ZipSignature.Length)
                    {
                         return (false, "Could not read file header.");
                    }

                    for (int i = 0; i < ZipSignature.Length; i++)
                    {
                        if (buffer[i] != ZipSignature[i])
                        {
                            return (false, "File is not a valid ZIP archive (invalid magic numbers).");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                 return (false, $"Error reading file: {ex.Message}");
            }

            return (true, string.Empty);
        }
    }
}
