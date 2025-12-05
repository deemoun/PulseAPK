using System;
using System.IO;

namespace APKToolUI.Utils
{
    public static class PathUtils
    {
        public static string GetDefaultDecompilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "decompiled");
        }
    }
}
