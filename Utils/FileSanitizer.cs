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

        public static (bool IsValid, string Message) ValidateUbersign(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return (false, "File path is empty.");
            }

            if (!File.Exists(path))
            {
                return (false, "File does not exist.");
            }

            var extension = Path.GetExtension(path);
            if (extension.Equals(".jar", StringComparison.OrdinalIgnoreCase))
            {
                return ValidateJar(path);
            }

            return (false, "File must be a .jar file.");
        }

        public static (bool IsValid, string Message) ValidateProjectFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return (false, "Folder path is empty.");
            }

            if (!Directory.Exists(path))
            {
                return (false, "Folder does not exist.");
            }

            // A valid apktool project usually has apktool.yml AND AndroidManifest.xml
            // But sometimes apktool.yml might be missing or optional depending on version logic? 
            // The user prompt said: "presence of required files like AndroidManifest.xml, smali directories, resources"
            // Let's check for AndroidManifest.xml as the bare minimum for an Android project.

            var manifestPath = Path.Combine(path, "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                return (false, "Folder is missing 'AndroidManifest.xml'.");
            }
            
            // We could check for apktool.yml too, but apktool might reconstruct it or work without it for some raw sources? 
            // Safest bet is likely apktool.yml if it was decompiled by apktool.
            var apktoolYml = Path.Combine(path, "apktool.yml");
            if (!File.Exists(apktoolYml))
            {
                // Warning or error? Let's treat it as error for "Decompiled with apktool" context.
                return (false, "Folder is missing 'apktool.yml'. Is this a valid decompiled project?");
            }

            return (true, string.Empty);
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
